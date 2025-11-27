using Client.MirControls;
using Client.MirNetwork;
using Client.MirObjects;
using Client.MirScenes.Dialogs;
using C = ClientPackets;
using S = ServerPackets;

namespace Client.MirScenes
{
    /// <summary>
    /// Equips the amulets and poisons consumed by Taoist spells before the cast is sent.
    /// One preparation may be in flight per actor. Every item step waits for its server
    /// response before the next one is planned, so a failed or conflicting manual action
    /// never results in casting without the required materials.
    /// </summary>
    internal sealed class SpellMaterialPreparation
    {
        private sealed class Requirement
        {
            public readonly int[] Shapes;
            public readonly int Count;

            public Requirement(int[] shapes, int count)
            {
                Shapes = shapes;
                Count = count;
            }

            public bool IsMetBy(UserItem item)
            {
                return item != null && item.Info.Type == ItemType.Amulet && item.Count >= Count &&
                       Shapes.Contains(item.Info.Shape);
            }
        }

        private sealed class PendingCast
        {
            public UserObject Actor;
            public ClientMagic Magic;
            public byte MagicKey;
            public Spell Spell;
            public MapObject Target;
            public Point Location;
            public MirDirection Direction;
            public int MapIndex;
            public bool Canceled;
            public bool Ready;
            public long StepDeadline;
            public PendingStep Step;
        }

        private sealed class PendingStep
        {
            public bool Merge;
            public MirGridType GridFrom;
            public MirGridType GridTo;
            public ulong IDFrom;
            public ulong IDTo;
            public int SlotTo;
        }

        private static readonly int[] NormalAmuletShapes = { 0 };
        private static readonly int[] GreenPoisonShapes = { 1 };
        private static readonly int[] AnyPoisonShapes = { 1, 2 };
        private static readonly int[] ReincarnationAmuletShapes = { 3 };
        private static readonly EquipmentSlot[] MaterialSlots = { EquipmentSlot.Amulet, EquipmentSlot.BraceletR };

        private const long StepTimeout = 5000;

        private readonly GameScene _scene;
        private readonly Dictionary<UserObject, PendingCast> _pendingCasts = new Dictionary<UserObject, PendingCast>();

        public SpellMaterialPreparation(GameScene scene)
        {
            _scene = scene;
        }

        /// <summary>
        /// Called by the real cast path after cooldown, mana, targeting and range checks.
        /// Returns true when the caller must not dispatch the cast yet: either a preparation
        /// was started, or another preparation is already pending and this request is a
        /// duplicate/superseded key press.
        /// </summary>
        public bool TryBegin(ClientMagic magic, UserObject actor, MapObject target, Point location, MirDirection direction)
        {
            if (actor == null || actor.Class != MirClass.Taoist) return false;

            Requirement[] requirements = GetRequirements(magic.Spell);
            if (requirements == null) return false;
            PendingCast cast;
            if (_pendingCasts.TryGetValue(actor, out cast))
            {
                if (cast.MagicKey == magic.Key && cast.Spell == magic.Spell)
                    return true;

                // Keep the already-sent item step, but replace the waiting spell intent.
                cast.Magic = magic;
                cast.MagicKey = magic.Key;
                cast.Spell = magic.Spell;
                cast.Target = target;
                cast.Location = location;
                cast.Direction = direction;
                cast.MapIndex = _scene.MapControl.Index;
                cast.Canceled = false;
                cast.Ready = false;
                actor.ResetNextMagic();
                return true;
            }

            if (MaterialSlots.Any(slot => GetEquipmentCell(actor, (int)slot)?.Locked == true))
            {
                actor.ResetNextMagic();
                return true;
            }

            if (HasAllMaterials(actor.Equipment, requirements)) return false;

            actor.ClearMagic();

            cast = new PendingCast
            {
                Actor = actor,
                Magic = magic,
                MagicKey = magic.Key,
                Spell = magic.Spell,
                Target = target,
                Location = location,
                Direction = direction,
                MapIndex = _scene.MapControl.Index,
            };

            _pendingCasts[actor] = cast;

            if (!TrySendNextStep(cast, requirements))
            {
                _pendingCasts.Remove(actor);
                actor.ClearMagic();
            }

            return true;
        }

        /// <summary>
        /// Intercepts key presses while a preparation is pending. Repeating the same spell
        /// is ignored; a different spell supersedes the pending cast without disturbing the
        /// item step that is already in flight.
        /// </summary>
        public bool TryHandlePending(ClientMagic magic, UserObject actor)
        {
            if (actor == null) return false;

            PendingCast cast;
            if (!_pendingCasts.TryGetValue(actor, out cast)) return false;

            if (cast.MagicKey == magic.Key && cast.Spell == magic.Spell && !cast.Canceled)
                return true;

            Cancel(actor);
            return false;
        }

        /// <summary>
        /// Guards against a response that never arrives. The item cells stay locked on
        /// purpose: the late response must still reach the normal handler, which applies the
        /// swap and unlocks the cells before the player can use them again.
        /// </summary>
        public void Process()
        {
            List<UserObject> timedOut = null;

            foreach (KeyValuePair<UserObject, PendingCast> pair in _pendingCasts)
            {
                PendingCast cast = pair.Value;
                if (cast.Step == null || CMain.Time < cast.StepDeadline) continue;

                if (timedOut == null) timedOut = new List<UserObject>();
                timedOut.Add(pair.Key);
            }

            if (timedOut == null) return;

            for (int i = 0; i < timedOut.Count; i++)
            {
                _pendingCasts.Remove(timedOut[i]);
                timedOut[i].ClearMagic();
            }
        }

        public void Cancel(UserObject actor)
        {
            if (actor == null) return;

            PendingCast cast;
            if (!_pendingCasts.TryGetValue(actor, out cast)) return;

            cast.Canceled = true;

            if (cast.Step == null) _pendingCasts.Remove(actor);
        }

        public bool IsPendingEquip(S.EquipItem p)
        {
            UserObject actor = GetActor(p.Grid, p.ToHero);
            if (actor == null) return false;

            PendingCast cast;
            if (!_pendingCasts.TryGetValue(actor, out cast)) return false;
            if (cast.Step == null || cast.Step.Merge) return false;

            return cast.Step.GridFrom == p.Grid && cast.Step.IDFrom == p.UniqueID && cast.Step.SlotTo == p.To;
        }

        public bool IsPendingMerge(S.MergeItem p)
        {
            UserObject actor = GetActor(p.GridFrom, p.GridTo);
            if (actor == null) return false;

            PendingCast cast;
            if (!_pendingCasts.TryGetValue(actor, out cast)) return false;
            if (cast.Step == null || !cast.Step.Merge) return false;

            return cast.Step.GridFrom == p.GridFrom && cast.Step.GridTo == p.GridTo &&
                   cast.Step.IDFrom == p.IDFrom && cast.Step.IDTo == p.IDTo;
        }

        /// <summary>
        /// Called by the item response handlers after they have applied (or rejected) the
        /// swap and unlocked the cells. The next step is only planned from the confirmed state.
        /// </summary>
        public void CompleteEquipStep(S.EquipItem p, bool success)
        {
            UserObject actor = GetActor(p.Grid, p.ToHero);
            if (actor == null || !IsPendingEquip(p)) return;

            CompleteStep(actor, success);
        }

        public void CompleteMergeStep(S.MergeItem p, bool success)
        {
            UserObject actor = GetActor(p.GridFrom, p.GridTo);
            if (actor == null || !IsPendingMerge(p)) return;

            CompleteStep(actor, success);
        }

        private void CompleteStep(UserObject actor, bool success)
        {
            PendingCast cast;
            if (!_pendingCasts.TryGetValue(actor, out cast) || cast.Step == null) return;

            cast.Step = null;

            if (!success || cast.Canceled || !Validate(cast))
            {
                _pendingCasts.Remove(actor);
                cast.Actor.ClearMagic();
                return;
            }

            Requirement[] requirements = GetRequirements(cast.Spell);
            if (requirements == null)
            {
                _pendingCasts.Remove(actor);
                cast.Actor.ClearMagic();
                return;
            }

            if (HasAllMaterials(cast.Actor.Equipment, requirements))
            {
                Resume(cast);
                return;
            }

            if (!TrySendNextStep(cast, requirements))
            {
                _pendingCasts.Remove(actor);
                cast.Actor.ClearMagic();
            }
        }

        private void Resume(PendingCast cast)
        {
            if (cast.Actor == MapObject.User)
            {
                // Network responses run before User.ProcessFrames. Defer until CheckInput
                // so ProcessFrames cannot discard the newly queued spell on this frame.
                cast.Ready = true;
                return;
            }

            Dispatch(cast);
        }

        public bool TryResumePlayer()
        {
            PendingCast cast;
            if (!_pendingCasts.TryGetValue(MapObject.User, out cast) || !cast.Ready) return false;
            Dispatch(cast);
            return true;
        }

        private void Dispatch(PendingCast cast)
        {
            _pendingCasts.Remove(cast.Actor);

            if (!Validate(cast))
            {
                cast.Actor.ClearMagic();
                return;
            }

            // Re-enter the normal magic path with the snapshot target/direction; it repeats
            // the cooldown, mana and range checks and sends the cast exactly once.
            _scene.MapControl.UsePreparedMagic(cast.Magic, cast.Actor, cast.Target, cast.Location, cast.Direction);
            cast.Actor.ClearMagic();
        }

        private bool Validate(PendingCast cast)
        {
            if (cast == null || cast.Canceled) return false;

            UserObject actor = cast.Actor;
            if (actor == null || actor.Class != MirClass.Taoist || actor.RidingMount || actor.Fishing) return false;
            if (!actor.HasClassWeapon && actor.Weapon >= 0) return false;
            if (CMain.Time < actor.BlizzardStopTime || CMain.Time < actor.ReincarnationStopTime) return false;

            MapObject visibleActor = actor == MapObject.Hero ? MapObject.HeroObject : actor;
            if (visibleActor == null || visibleActor.Dead) return false;

            if (actor == MapObject.Hero)
            {
                if (MapObject.HeroObject == null) return false;
            }
            else if (actor != MapObject.User)
            {
                return false;
            }

            if (_scene.MapControl == null || _scene.MapControl.Index != cast.MapIndex) return false;

            if (visibleActor.Poison.HasFlag(PoisonType.Stun) || visibleActor.Poison.HasFlag(PoisonType.Frozen) ||
                visibleActor.Poison.HasFlag(PoisonType.Paralysis) || visibleActor.Poison.HasFlag(PoisonType.LRParalysis))
                return false;

            ClientMagic magic = null;

            for (int i = 0; i < actor.Magics.Count; i++)
            {
                if (actor.Magics[i].Key != cast.MagicKey) continue;
                magic = actor.Magics[i];
                break;
            }

            if (magic == null || magic.Spell != cast.Spell) return false;
            cast.Magic = magic;

            if (cast.Target == null) return true;

            MapObject current;
            if (!MapControl.Objects.TryGetValue(cast.Target.ObjectID, out current) || current != cast.Target) return false;

            if (cast.Spell == Spell.Reincarnation)
                return cast.Target.Dead && cast.Target.Race == ObjectType.Player;

            return !cast.Target.Dead && cast.Target.Race != ObjectType.Item && cast.Target.Race != ObjectType.Merchant;
        }

        private bool TrySendNextStep(PendingCast cast, Requirement[] requirements)
        {
            UserItem[] equipment = cast.Actor.Equipment;
            bool[] claimed = new bool[equipment.Length];
            List<Requirement> missing = new List<Requirement>();

            for (int i = 0; i < requirements.Length; i++)
            {
                bool found = false;

                for (int s = 0; s < MaterialSlots.Length; s++)
                {
                    int slot = (int)MaterialSlots[s];
                    if (claimed[slot] || !requirements[i].IsMetBy(equipment[slot])) continue;

                    claimed[slot] = true;
                    found = true;
                    break;
                }

                if (!found) missing.Add(requirements[i]);
            }

            if (missing.Count == 0)
            {
                Resume(cast);
                return true;
            }

            Requirement requirement = missing[0];
            int targetSlot = ChooseSlot(cast, requirement, claimed);
            if (targetSlot < 0) return false;

            UserItem target = equipment[targetSlot];

            if (target != null && target.Info.Type == ItemType.Amulet && requirement.Shapes.Contains(target.Info.Shape))
            {
                if (TryMergeInto(cast, requirement, targetSlot)) return true;
            }

            return TryEquipInto(cast, requirement, targetSlot);
        }

        private int ChooseSlot(PendingCast cast, Requirement requirement, bool[] claimed)
        {
            UserItem[] equipment = cast.Actor.Equipment;
            int replaceSlot = -1;

            for (int i = 0; i < MaterialSlots.Length; i++)
            {
                int slot = (int)MaterialSlots[i];
                if (claimed[slot]) continue;

                MirItemCell cell = GetEquipmentCell(cast.Actor, slot);
                if (cell == null || cell.Locked) continue;
                UserItem item = equipment[slot];

                if (item == null) return slot;

                if (item.Info.Type == ItemType.Amulet && requirement.Shapes.Contains(item.Info.Shape))
                {
                    if (CanMergeInto(cast, requirement, item)) return slot;
                    if (!item.Cursed && replaceSlot < 0) replaceSlot = slot;
                }
                else if (!item.Cursed && replaceSlot < 0)
                {
                    replaceSlot = slot;
                }
            }

            return replaceSlot;
        }

        private bool TryMergeInto(PendingCast cast, Requirement requirement, int targetSlot)
        {
            UserItem target = cast.Actor.Equipment[targetSlot];
            UserItem source = FindMergeSource(cast.Actor, requirement, target);
            if (source == null) return false;

            MirItemCell fromCell = GetInventoryCell(cast.Actor, source.UniqueID);
            MirItemCell toCell = GetEquipmentCell(cast.Actor, targetSlot);
            if (fromCell == null || toCell == null || fromCell.Locked || toCell.Locked) return false;

            fromCell.Locked = true;
            toCell.Locked = true;

            MirGridType gridFrom = IsHero(cast.Actor) ? MirGridType.HeroInventory : MirGridType.Inventory;
            MirGridType gridTo = IsHero(cast.Actor) ? MirGridType.HeroEquipment : MirGridType.Equipment;

            cast.Step = new PendingStep
            {
                Merge = true,
                GridFrom = gridFrom,
                GridTo = gridTo,
                IDFrom = source.UniqueID,
                IDTo = target.UniqueID,
                SlotTo = targetSlot,
            };
            cast.StepDeadline = CMain.Time + StepTimeout;

            Network.Enqueue(new C.MergeItem { GridFrom = gridFrom, GridTo = gridTo, IDFrom = source.UniqueID, IDTo = target.UniqueID });
            return true;
        }

        private bool TryEquipInto(PendingCast cast, Requirement requirement, int targetSlot)
        {
            UserItem target = cast.Actor.Equipment[targetSlot];

            // The server refuses to move a cursed equipped item back to the bag, so a swap
            // that would need to do that is abandoned before any packet is sent.
            if (target != null && target.Cursed) return false;

            UserItem source = FindInventoryMaterial(cast.Actor, requirement);
            if (source == null) return false;

            MirItemCell fromCell = GetInventoryCell(cast.Actor, source.UniqueID);
            MirItemCell toCell = GetEquipmentCell(cast.Actor, targetSlot);
            if (fromCell == null || toCell == null || fromCell.Locked || toCell.Locked) return false;

            fromCell.Locked = true;
            toCell.Locked = true;

            MirGridType grid = IsHero(cast.Actor) ? MirGridType.HeroInventory : MirGridType.Inventory;

            cast.Step = new PendingStep
            {
                Merge = false,
                GridFrom = grid,
                IDFrom = source.UniqueID,
                SlotTo = targetSlot,
            };
            cast.StepDeadline = CMain.Time + StepTimeout;

            Network.Enqueue(new C.EquipItem { Grid = grid, UniqueID = source.UniqueID, To = targetSlot });
            return true;
        }

        private bool CanMergeInto(PendingCast cast, Requirement requirement, UserItem target)
        {
            return FindMergeSource(cast.Actor, requirement, target) != null;
        }

        private UserItem FindMergeSource(UserObject actor, Requirement requirement, UserItem target)
        {
            int needed = requirement.Count - target.Count;
            if (needed <= 0) return null;

            for (int i = 0; i < actor.Inventory.Length; i++)
            {
                UserItem item = actor.Inventory[i];
                if (item == null || !requirement.Shapes.Contains(item.Info.Shape)) continue;
                if (item.Info != target.Info || !item.CanStackWith(target)) continue;
                if (item.Count < needed) continue;
                if (!CanUseInventoryItem(actor, item)) continue;

                return item;
            }

            return null;
        }

        private UserItem FindInventoryMaterial(UserObject actor, Requirement requirement)
        {
            for (int i = 0; i < actor.Inventory.Length; i++)
            {
                UserItem item = actor.Inventory[i];
                if (!requirement.IsMetBy(item)) continue;
                if (!CanUseInventoryItem(actor, item)) continue;

                return item;
            }

            return null;
        }

        private bool CanUseInventoryItem(UserObject actor, UserItem item)
        {
            MirItemCell cell = GetInventoryCell(actor, item.UniqueID);
            return cell != null && !cell.Locked;
        }

        private MirItemCell GetInventoryCell(UserObject actor, ulong uniqueID)
        {
            if (IsHero(actor))
                return _scene.HeroInventoryDialog?.GetCell(uniqueID) ?? _scene.HeroBeltDialog?.GetCell(uniqueID);

            return _scene.InventoryDialog?.GetCell(uniqueID) ?? _scene.BeltDialog?.GetCell(uniqueID);
        }

        private MirItemCell GetEquipmentCell(UserObject actor, int slot)
        {
            CharacterDialog dialog = IsHero(actor) ? _scene.HeroDialog : _scene.CharacterDialog;
            if (dialog == null || slot < 0 || slot >= dialog.Grid.Length) return null;

            return dialog.Grid[slot];
        }

        private static bool IsHero(UserObject actor)
        {
            return actor == MapObject.Hero;
        }

        private static UserObject GetActor(MirGridType grid, bool toHero)
        {
            return toHero || grid == MirGridType.HeroInventory || grid == MirGridType.HeroEquipment
                ? MapObject.Hero
                : MapObject.User;
        }

        private static UserObject GetActor(MirGridType gridFrom, MirGridType gridTo)
        {
            return gridFrom == MirGridType.HeroInventory || gridFrom == MirGridType.HeroEquipment ||
                   gridTo == MirGridType.HeroInventory || gridTo == MirGridType.HeroEquipment
                ? MapObject.Hero
                : MapObject.User;
        }

        private static bool HasAllMaterials(UserItem[] equipment, Requirement[] requirements)
        {
            for (int i = 0; i < requirements.Length; i++)
            {
                bool found = false;

                for (int s = 0; s < equipment.Length; s++)
                {
                    if (!requirements[i].IsMetBy(equipment[s])) continue;

                    found = true;
                    break;
                }

                if (!found) return false;
            }

            return true;
        }

        private static Requirement[] GetRequirements(Spell spell)
        {
            switch (spell)
            {
                case Spell.SoulFireBall:
                case Spell.SummonSkeleton:
                case Spell.Hiding:
                case Spell.Hallucination:
                case Spell.MassHiding:
                case Spell.SoulShield:
                case Spell.BlessedArmour:
                case Spell.TrapHexagon:
                case Spell.UltimateEnhancer:
                case Spell.Curse:
                case Spell.Plague:
                    return new[] { new Requirement(NormalAmuletShapes, 1) };
                case Spell.SummonShinsu:
                    return new[] { new Requirement(NormalAmuletShapes, 5) };
                case Spell.SummonHolyDeva:
                    return new[] { new Requirement(NormalAmuletShapes, 2) };
                case Spell.Reincarnation:
                    return new[] { new Requirement(ReincarnationAmuletShapes, 1) };
                case Spell.Poisoning:
                    return new[] { new Requirement(AnyPoisonShapes, 1) };
                case Spell.PoisonCloud:
                    return new[] { new Requirement(NormalAmuletShapes, 5), new Requirement(GreenPoisonShapes, 5) };
                default:
                    return null;
            }
        }
    }
}
