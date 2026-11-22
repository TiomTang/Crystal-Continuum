using System.Drawing;
using System.Threading;
﻿using Server.MirDatabase;
using Server.MirEnvir;
using Server.MirObjects.Monsters;
using S = ServerPackets;

namespace Server.MirObjects
{
    public abstract class MapObject
    {
        protected MapObject()
        {
            Stats = new StatView(effectiveStats);
        }

        protected static MessageQueue MessageQueue
        {
            get { return MessageQueue.Instance; }
        }

        protected static Envir Envir
        {
            get { return Envir.Main; }
        }

        public readonly uint ObjectID = Envir.ObjectID;

        public abstract ObjectType Race { get; }

        public abstract string Name { get; set; }

        public long ExplosionInflictedTime;
        public int ExplosionInflictedStage;

        private int SpawnThread = -1;

        private Map _currentMap;
        public Map CurrentMap
        {
            set
            {
                _currentMap = value;
                CurrentMapIndex = _currentMap != null ? _currentMap.Info.Index : 0;
            }
            get { return _currentMap; }
        }

        public abstract int CurrentMapIndex { get; set; }
        public abstract Point CurrentLocation { get; set; }
        public abstract MirDirection Direction { get; set; }

        public abstract ushort Level { get; set; }

        public abstract int Health { get; }
        public abstract int MaxHealth { get; }
        public byte PercentHealth
        {
            get { return (byte)(Health / (float)MaxHealth * 100); }
        }

        public byte Light;
        public int AttackSpeed;

        protected long brownTime;
        public virtual long BrownTime
        {
            get { return brownTime; }
            set { brownTime = value; }
        }

        public long CellTime, PKPointTime, LastHitTime, EXPOwnerTime;
        public Color NameColour = Color.White;

        public bool Dead, Undead, Harvested, AutoRev;

        public List<KeyValuePair<string, string>> NPCVar = new List<KeyValuePair<string, string>>();

        public virtual int PKPoints { get; set; }

        public ushort PotHealthAmount, PotManaAmount, HealAmount, VampAmount;

        public bool CoolEye;
        private bool _hidden;

        public bool Hidden
        {
            get
            {
                return _hidden;
            }
            set
            {
                if (_hidden == value) return;
                _hidden = value;
                CurrentMap.Broadcast(new S.ObjectHidden { ObjectID = ObjectID, Hidden = value }, CurrentLocation);
            }
        }

        private bool _observer;
        public bool Observer
        {
            get
            {
                return _observer;
            }
            set
            {
                if (_observer == value) return;
                _observer = value;
                if (!_observer)
                    BroadcastInfo();
                else
                    Broadcast(new S.ObjectRemove { ObjectID = ObjectID });
            }
        }

        #region Sneaking
        private bool _sneakingActive;
        public bool SneakingActive
        {
            get { return _sneakingActive; }
            set
            {
                if (_sneakingActive == value) return;
                _sneakingActive = value;

                Observer = _sneakingActive;
            }
        }

        private bool _sneaking;
        public bool Sneaking
        {
            get { return _sneaking; }
            set { _sneaking = value; SneakingActive = value; }
        }
        #endregion

        public MapObject _target;
        public virtual MapObject Target
        {
            get { return _target; }
            set
            {
                if (_target == value) return;
                _target = value;
            }

        }

        protected MapObject master;
        public virtual MapObject Master
        {
            get { return master; }
            set
            {
                if (master == value) return;
                master = value;
                SyncLoopMembership();
            }
        }

        public MapObject LastHitter, EXPOwner, Owner;
        public long ExpireTime, OwnerTime;
        public int OperateDelay = 100;

        private long _operateTime;

        /// <summary>
        /// Deadline used by both work loops. It runs through a property so a deadline pulled
        /// forward from another thread (a player hitting this object sets it to zero) is re-parked
        /// in its partition wheel immediately; as a plain field it would have waited for the bucket
        /// its old deadline pointed at, up to a few seconds of missing reaction.
        /// </summary>
        public long OperateTime
        {
            get { return Volatile.Read(ref _operateTime); }
            set
            {
                if (Interlocked.Exchange(ref _operateTime, value) == value) return;

                var wheel = Wheel;
                if (wheel != null) wheel.Reslot(this);
            }
        }

        /// <summary>Set while a partition pass is processing this object; the pass re-parks it after.</summary>
        internal bool WheelDetached;

        private MobWheel Wheel
        {
            get
            {
                if (NodeThreaded == null) return null;

                var threads = Envir.MobThreads;
                if (threads == null || SpawnThread < 0 || SpawnThread >= threads.Length) return null;

                var thread = threads[SpawnThread];
                return thread == null ? null : thread.Wheel;
            }
        }

        /// <summary>
        /// Raw attribute layer. Level, equipment, set bonuses and buffs accumulate
        /// here; this is the only layer any code writes to.
        /// </summary>
        protected Stats BaseStats = new Stats();

        private readonly Stats effectiveStats = new Stats();

        /// <summary>
        /// Effective attribute layer: <see cref="BaseStats"/> with every modifier folded
        /// in. All readers use this layer, so no call site needs to know which stat
        /// currently has a modifier. It is a read-only projection on purpose: writing
        /// here would be undone by the next rebuild and hide the mistake, so changes go
        /// through <see cref="BaseStats"/> plus <see cref="RebuildEffectiveStats"/>.
        /// </summary>
        public StatView Stats { get; }

        /// <summary>
        /// Raw layer for cross-object reads that must not follow modifiers, such as pets
        /// inheriting their owner's base attributes.
        /// </summary>
        internal Stats RawStats => BaseStats;

        // Attribute modifiers, collected from their sources on every rebuild. Only Mul
        // rules live here; percentage bonuses arrive as whole-percent fields inside
        // BaseStats and are mapped onto their target stat below.
        protected readonly List<StatModifier> Modifiers = new List<StatModifier>();

        /// <summary>
        /// Whole-percent fields in the raw table and the stat each one increases. Adding a
        /// new percentage-based attribute means adding one line here; the fold, the
        /// network payload and the client all follow it without further changes.
        /// </summary>
        private static readonly (Stat Rate, Stat Target)[] PctFromRate =
        {
            (Stat.HPRatePercent, Stat.HP),
            (Stat.MPRatePercent, Stat.MP),
            (Stat.MaxACRatePercent, Stat.MaxAC),
            (Stat.MaxMACRatePercent, Stat.MaxMAC),
            (Stat.MaxDCRatePercent, Stat.MaxDC),
            (Stat.MaxMCRatePercent, Stat.MaxMC),
            (Stat.MaxSCRatePercent, Stat.MaxSC),
            // Points, not milliseconds: Stats[Stat.AttackSpeed] is a count where more is
            // faster, while MapObject.AttackSpeed is the millisecond interval where less
            // is faster. A positive percentage here speeds the target up.
            (Stat.AttackSpeedRatePercent, Stat.AttackSpeed),
        };

        protected float ArmourMulFactor = 1F;

        private bool? redArmourActive;

        public List<MonsterObject> Pets = new List<MonsterObject>();
        public virtual List<Buff> Buffs { get; set; } = new List<Buff>();

        public List<PlayerObject> GroupMembers;

        public virtual AttackMode AMode { get; set; }
        public virtual PetMode PMode { get; set; }

        private bool _inSafeZone;
        public bool InSafeZone {
            get { return _inSafeZone; }
            set
            {
                if (_inSafeZone == value) return;
                _inSafeZone = value;
                OnSafeZoneChanged();
            }
        }

        public float DamageRate; //recieved not given

        public virtual List<Poison> PoisonList { get; set; } = new List<Poison>();
        public PoisonType CurrentPoison = PoisonType.None;
        public List<DelayedAction> ActionList = new List<DelayedAction>();


        public LinkedListNode<MapObject> Node;
        public LinkedListNode<MapObject> NodeThreaded;
        public LinkedListNode<MapObject> NodeMainLoop;
        public long RevTime;

        public virtual bool Blocking
        {
            get { return true; }
        }

        public Point Front
        {
            get { return Functions.PointMove(CurrentLocation, Direction, 1); }
        }

        public Point Back
        {
            get { return Functions.PointMove(CurrentLocation, Direction, -1); }

        }

        public virtual void Process()
        {
            if (Master != null && Master.Node == null) Master = null;
            if (LastHitter != null && LastHitter.Node == null) LastHitter = null;
            if (EXPOwner != null && EXPOwner.Node == null) EXPOwner = null;
            if (Target != null && (Target.Node == null || Target.Dead)) Target = null;
            if (Owner != null && Owner.Node == null) Owner = null;

            if (PKPoints > 0 && Envir.Time > PKPointTime)
            {
                PKPointTime = Envir.Time + Settings.PKDelay * Settings.Second;
                PKPoints--;
            }

            if (LastHitter != null && Envir.Time > LastHitTime)
            {
                LastHitter = null;
            }

            if (EXPOwner != null && Envir.Time > EXPOwnerTime)
            {
                EXPOwner = null;
            }

            // Drain this batch in order without skipping adjacent due actions or chasing new ones.
            int remaining = ActionList.Count;
            for (int i = 0; i < ActionList.Count && remaining-- > 0;)
            {
                var action = ActionList[i];
                if (Envir.Time < action.Time)
                {
                    i++;
                    continue;
                }

                // The callback may despawn this object and clear its action list.
                ActionList.RemoveAt(i);
                Process(action);
            }
        }

        public virtual void OnSafeZoneChanged()
        {

        }

        public abstract void SetOperateTime();

        public int GetAttackPower(int min, int max)
        {
            if (min < 0) min = 0;
            if (min > max) max = min;

            if (Stats[Stat.Luck] > 0)
            {
                if (Stats[Stat.Luck] > Envir.Random.Next(Settings.MaxLuck))
                    return max;
            }
            else if (Stats[Stat.Luck] < 0)
            {
                if (Stats[Stat.Luck] < -Envir.Random.Next(Settings.MaxLuck))
                    return min;
            }

            return Envir.Random.Next(min, max + 1);
        }

        /// <summary>
        /// Armour penalty applied per active red poison. Players lose 10%, monsters 50%.
        /// </summary>
        protected virtual float RedPoisonArmourPenalty => StatMath.PlayerRedPoisonArmourPenalty;

        /// <summary>
        /// Rebuilds the poison-derived modifiers. ProcessPoison calls this every tick;
        /// the cached flag leaves the list untouched when the state did not change.
        /// </summary>
        protected void SetRedArmourPoison(bool active)
        {
            if (redArmourActive == active) return;

            redArmourActive = active;

            Modifiers.RemoveAll(m => m.Source.Kind == ModSourceKind.Poison);

            if (active)
            {
                float factor = 1F - RedPoisonArmourPenalty;
                ModSource source = new ModSource(ModSourceKind.Poison, (int)PoisonType.Red);

                Modifiers.Add(new StatModifier(Stat.MinAC, StatModOp.Mul, factor, source));
                Modifiers.Add(new StatModifier(Stat.MaxAC, StatModOp.Mul, factor, source));
                Modifiers.Add(new StatModifier(Stat.MinMAC, StatModOp.Mul, factor, source));
                Modifiers.Add(new StatModifier(Stat.MaxMAC, StatModOp.Mul, factor, source));
            }

            ArmourMulFactor = StatMath.MulFactor(Modifiers, Stat.MinAC);

            RebuildEffectiveStats();

            OnStatMultipliersChanged();
        }

        /// <summary>
        /// Rebuilds the effective layer from the raw layer in three stages, in this order:
        /// each percentage field is applied once with integer arithmetic, the class caps are
        /// applied, then every Mul rule is folded and truncated. Both stages truncate on
        /// their own boundary, and the client repeats the same order on the values the
        /// server sends it.
        /// </summary>
        protected void RebuildEffectiveStats()
        {
            effectiveStats.Clear();
            effectiveStats.Add(BaseStats);

            for (int i = 0; i < PctFromRate.Length; i++)
            {
                int percent = BaseStats[PctFromRate[i].Rate];

                if (percent == 0) continue;

                Stat target = PctFromRate[i].Target;

                effectiveStats[target] += (effectiveStats[target] * percent) / 100;
            }

            ApplyStatCaps(effectiveStats);

            for (int i = 0; i < Modifiers.Count; i++)
            {
                StatModifier modifier = Modifiers[i];

                if (modifier.Op != StatModOp.Mul) continue;

                float factor = StatMath.MulFactor(Modifiers, modifier.Stat);

                effectiveStats[modifier.Stat] = StatMath.Truncate(effectiveStats[modifier.Stat] * factor);
            }
        }

        /// <summary>
        /// Class-specific clamping applied between the percentage and multiplier stages.
        /// Objects without caps leave the table untouched.
        /// </summary>
        protected virtual void ApplyStatCaps(Stats target) { }

        /// <summary>
        /// Aggregates both stages into one entry per affected stat. The three lists stay
        /// index-aligned: a stat present for either stage appears once, with a zero
        /// percentage and an identity factor when that stage does not touch it.
        /// </summary>
        public void BuildStatModifiers(List<Stat> stats, List<int> pctPercents, List<float> factors)
        {
            for (int i = 0; i < PctFromRate.Length; i++)
            {
                int percent = BaseStats[PctFromRate[i].Rate];

                if (percent == 0) continue;

                int index = stats.IndexOf(PctFromRate[i].Target);

                if (index < 0)
                {
                    stats.Add(PctFromRate[i].Target);
                    pctPercents.Add(percent);
                    factors.Add(1F);
                }
                else
                {
                    pctPercents[index] += percent;
                }
            }

            for (int i = 0; i < Modifiers.Count; i++)
            {
                StatModifier modifier = Modifiers[i];

                if (modifier.Op != StatModOp.Mul) continue;

                int index = stats.IndexOf(modifier.Stat);

                if (index < 0)
                {
                    stats.Add(modifier.Stat);
                    pctPercents.Add(0);
                    factors.Add(modifier.Value);
                }
                else
                {
                    factors[index] *= modifier.Value;
                }
            }
        }

        /// <summary>
        /// Called whenever the aggregated multipliers change. Human objects forward it
        /// to their client; monsters have no consumer.
        /// </summary>
        protected virtual void OnStatMultipliersChanged() { }

        public int GetRangeAttackPower(int min, int max, int range)
        {
            //maxRange = highest possible damage
            //minRange = lowest possible damage

            decimal x = ((decimal)min / (Globals.MaxAttackRange)) * (Globals.MaxAttackRange - range);

            min -= (int)Math.Floor(x);

            return GetAttackPower(min, max);
        }

        public int GetDefencePower(int min, int max)
        {
            if (min < 0) min = 0;
            if (min > max) max = min;

            return Envir.Random.Next(min, max + 1);
        }

        public virtual void Remove(HumanObject player)
        {
            player.Enqueue(new S.ObjectRemove { ObjectID = ObjectID });
        }
        public virtual void Add(HumanObject player)
        {
            if (player.Race != ObjectType.Player) return;

            if (Race == ObjectType.Merchant)
            {
                NPCObject npc = (NPCObject)this;
                npc.CheckVisible((PlayerObject)player, true);
                return;
            }

            player.Enqueue(GetInfo());

            //if (Race == ObjectType.Player)
            //{
            //    PlayerObject me = (PlayerObject)this;
            //    player.Enqueue(me.GetInfoEx(player));
            //}
            //else
            //{
            //    player.Enqueue(GetInfo());
            //}
        }
        public virtual void Remove(MonsterObject monster)
        {

        }
        public virtual void Add(MonsterObject monster)
        {

        }

        public abstract void Process(DelayedAction action);


        public bool CanFly(Point target)
        {
            Point location = CurrentLocation;

            while (location != target)
            {
                MirDirection dir = Functions.DirectionFromPoint(location, target);

                location = Functions.PointMove(location, dir, 1);

                if (location.X < 0 || location.Y < 0 || location.X >= CurrentMap.Width || location.Y >= CurrentMap.Height) return false;

                if (!CurrentMap.GetCell(location).Valid) return false;
            }

            return true;
        }

        /// <summary>
        /// Keeps both scanning structures aligned with who actually runs where: everything that is
        /// not a monster owned by a partition thread (see Envir.WorkLoop). Pets stay in the main
        /// list, so taming and untaming must move membership through the Master setter.
        /// Callers run on the main thread; partition threads only despawn non-pet monsters, which
        /// never belong to this list.
        /// </summary>
        private void SyncLoopMembership()
        {
            if (Node == null) return;

            bool scanInMainLoop = !Settings.Multithreaded || Race != ObjectType.Monster || master != null;
            if (scanInMainLoop)
            {
                if (NodeMainLoop == null) NodeMainLoop = Envir.MainLoopObjects.AddLast(this);
            }
            else if (NodeMainLoop != null)
            {
                Envir.MainLoopObjects.Remove(NodeMainLoop);
                NodeMainLoop = null;
            }

            // A plain monster is parked in its partition wheel; pets and everything else stay on
            // the main list above, so the two structures never hold the same object.
            if (!scanInMainLoop)
            {
                if (NodeThreaded == null)
                {
                    if (SpawnThread < 0) SpawnThread = CurrentMap.Thread;
                    Envir.MobThreads[SpawnThread].Wheel.Add(this);
                }
            }
            else if (NodeThreaded != null)
            {
                Envir.MobThreads[SpawnThread].Wheel.Remove(this);
            }
        }

        public virtual void Spawned()
        {
            if (Node != null)
                throw new InvalidOperationException("Node is not null, Object already spawned");

            Node = Envir.Objects.AddLast(this);
            if ((Race == ObjectType.Monster) && Settings.Multithreaded)
                SpawnThread = CurrentMap.Thread;

            SyncLoopMembership();

            // Set after membership so the wheel parks the object by its real deadline.
            OperateTime = Envir.Time + Envir.Random.Next(OperateDelay);

            BroadcastInfo();
            BroadcastHealthChange();

            InSafeZone = CurrentMap != null && CurrentMap.GetSafeZone(CurrentLocation) != null;
        }
        public virtual void Despawn()
        {
            if (Node == null)
                throw new InvalidOperationException("Node is null, Object already Despawned");
            
            Broadcast(new S.ObjectRemove { ObjectID = ObjectID });
            Envir.Objects.Remove(Node);
            if (NodeMainLoop != null)
            {
                Envir.MainLoopObjects.Remove(NodeMainLoop);
                NodeMainLoop = null;
            }
            if (NodeThreaded != null)
            {
                Envir.MobThreads[SpawnThread].Wheel.Remove(this);
            }

            ActionList.Clear();

            for (int i = Pets.Count - 1; i >= 0; i--)
                Pets[i].Die();

            Node = null;
        }

        public MapObject FindObject(uint targetID, int dist)
        {
            for (int d = 0; d <= dist; d++)
            {
                for (int y = CurrentLocation.Y - d; y <= CurrentLocation.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = CurrentLocation.X - d; x <= CurrentLocation.X + d; x += Math.Abs(y - CurrentLocation.Y) == d ? 1 : d * 2)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;

                        Cell cell = CurrentMap.GetCell(x, y);
                        if (!cell.Valid || cell.Objects == null) continue;

                        for (int i = 0; i < cell.Objects.Count; i++)
                        {
                            MapObject ob = cell.Objects[i];
                            if (ob.ObjectID != targetID) continue;

                            return ob;
                        }
                    }
                }
            }
            return null;
        }

        public virtual void Broadcast(Packet p)
        {
            if (p == null || CurrentMap == null) return;

            for (int i = CurrentMap.Players.Count - 1; i >= 0; i--)
            {
                PlayerObject player = CurrentMap.Players[i];
                if (player == this) continue;

                if (Functions.InRange(CurrentLocation, player.CurrentLocation, Globals.DataRange))
                    player.Enqueue(p);
            }
        }

        public virtual void BroadcastInfo()
        {
            Broadcast(GetInfo());
            return;
        }

        public bool IsAttackTarget(MapObject attacker)
        {
            if (attacker == null || attacker.Node == null) return false;
            if (Dead || attacker == this) return false;

            var flag = true;
            if (Race == ObjectType.Monster)
            {
                // Check if we are a training AI - we can be attacked in safezones
                if (((MonsterObject)this).Info.AI == 56)
                    flag = false;
            }
            if (flag && (InSafeZone || attacker.InSafeZone)) return false;

            switch (attacker.Race)
            {
                case ObjectType.Player:
                    return IsAttackTarget((PlayerObject)attacker);
                case ObjectType.Hero:
                    return IsAttackTarget((HeroObject)attacker);
                case ObjectType.Monster:
                    return IsAttackTarget((MonsterObject)attacker);
                default:
                    throw new NotSupportedException();
            }
        }

        public abstract bool IsAttackTarget(HumanObject attacker);
        public abstract bool IsAttackTarget(MonsterObject attacker);
        public abstract int Attacked(HumanObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true);
        public abstract int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility);

        public virtual int GetArmour(DefenceType type, MapObject attacker, out bool hit)
        {
            hit = true;
            switch (type)
            {
                case DefenceType.ACAgility:
                    if (Envir.Random.Next(Stats[Stat.Agility] + 1) > attacker.Stats[Stat.Accuracy])
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        hit = false;
                    }
                    break;
                case DefenceType.MACAgility:
                    if (Envir.Random.Next(Settings.MagicResistWeight) < Stats[Stat.MagicResist])
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        hit = false;
                    }
                    if (Envir.Random.Next(Stats[Stat.Agility] + 1) > attacker.Stats[Stat.Accuracy])
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        hit = false;
                    }
                    break;
                case DefenceType.MAC:
                    if (Envir.Random.Next(Settings.MagicResistWeight) < Stats[Stat.MagicResist])
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        hit = false;
                    }
                    break;
                case DefenceType.Agility:
                    if (Envir.Random.Next(Stats[Stat.Agility] + 1) > attacker.Stats[Stat.Accuracy])
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        hit = false;
                    }
                    break;
            }
            return RollTypeArmour(type);
        }

        /// <summary>
        /// Applies the incoming damage multiplier. Every attack path funnels through here
        /// so a new damage-taken modifier cannot be missed by one entry point.
        /// </summary>
        protected int ScaleIncomingDamage(int damage)
        {
            return (int)Math.Max(int.MinValue, (Math.Min(int.MaxValue, (decimal)(damage * DamageRate))));
        }

        /// <summary>
        /// A landed hit clears low-level paralysis, on every attack path.
        /// </summary>
        protected void ClearParalysisOnHit()
        {
            for (int i = PoisonList.Count - 1; i >= 0; i--)
            {
                if (PoisonList[i].PType != PoisonType.LRParalysis) continue;

                PoisonList.RemoveAt(i);
                OperateTime = 0;
            }
        }

        /// <summary>
        /// Rolls the effective armour for a defence type. One integer value is rolled out
        /// of the raw range and the multiplier is applied to that single value, which is
        /// the original rule: a state multiplier shrinks the result of a roll, not the
        /// range it was rolled from. Agility and agility+armour types share the physical
        /// range, magic types share the magic range, and a pure agility defence carries no
        /// armour value.
        /// </summary>
        protected int RollTypeArmour(DefenceType type)
        {
            switch (type)
            {
                case DefenceType.ACAgility:
                case DefenceType.AC:
                    return StatMath.Truncate(GetAttackPower(BaseStats[Stat.MinAC], BaseStats[Stat.MaxAC]) * ArmourMulFactor);
                case DefenceType.MACAgility:
                case DefenceType.MAC:
                    return StatMath.Truncate(GetAttackPower(BaseStats[Stat.MinMAC], BaseStats[Stat.MaxMAC]) * ArmourMulFactor);
                default:
                    return 0;
            }
        }

        public virtual void ApplyNegativeEffects(HumanObject attacker, DefenceType type, ushort levelOffset)
        {
            if (attacker.SpecialMode.HasFlag(SpecialItemMode.Paralize) && type != DefenceType.MAC && type != DefenceType.MACAgility && 1 == Envir.Random.Next(1, 15))
            {
                ApplyPoison(new Poison { PType = PoisonType.Paralysis, Duration = 5, TickSpeed = 1000 }, attacker);
            }
            if ((attacker.Stats[Stat.Freezing] > 0) && (Settings.PvpCanFreeze || Race != ObjectType.Player) && type != DefenceType.MAC && type != DefenceType.MACAgility)
            {
                if ((Envir.Random.Next(Settings.FreezingAttackWeight) < attacker.Stats[Stat.Freezing]) && (Envir.Random.Next(levelOffset) == 0))
                    ApplyPoison(new Poison { PType = PoisonType.Slow, Duration = Math.Min(10, (3 + Envir.Random.Next(attacker.Stats[Stat.Freezing]))), TickSpeed = 1000 }, attacker);
            }
            if (attacker.Stats[Stat.PoisonAttack] > 0 && type != DefenceType.MAC && type != DefenceType.MACAgility)
            {
                if ((Envir.Random.Next(Settings.PoisonAttackWeight) < attacker.Stats[Stat.PoisonAttack]) && (Envir.Random.Next(levelOffset) == 0))
                    ApplyPoison(new Poison { PType = PoisonType.Green, Duration = 5, TickSpeed = 1000, Value = Math.Min(10, 3 + Envir.Random.Next(attacker.Stats[Stat.PoisonAttack])) }, attacker);
            }
        }

        public abstract int Struck(int damage, DefenceType type = DefenceType.ACAgility);

        public bool IsFriendlyTarget(MapObject ally)
        {
            switch (ally.Race)
            {
                case ObjectType.Player:
                    return IsFriendlyTarget((PlayerObject)ally);
                case ObjectType.Hero:
                    return IsFriendlyTarget((HeroObject)ally);
                case ObjectType.Monster:
                    return IsFriendlyTarget((MonsterObject)ally);
                default:
                    throw new NotSupportedException();
            }
        }

        public abstract bool IsFriendlyTarget(HumanObject ally);
        public abstract bool IsFriendlyTarget(MonsterObject ally);

        public abstract void ReceiveChat(string text, ChatType type);

        public abstract Packet GetInfo();

        public virtual void WinExp(uint amount, uint targetLevel = 0)
        {


        }

        public virtual bool CanGainGold(uint gold)
        {
            return false;
        }
        public virtual void WinGold(uint gold)
        {

        }

        public virtual bool Harvest(PlayerObject player) { return false; }

        public abstract void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true);

        public virtual Buff AddBuff(BuffType type, MapObject owner, int duration, Stats stats, bool refreshStats = true, bool updateOnly = false, params int[] values)
        {
            if (!HasBuff(type, out Buff buff))
            {
                buff = new Buff(type)
                {
                    Caster = owner,
                    ObjectID = ObjectID,
                    ExpireTime = duration,
                    LastTime = Envir.Time,
                    Stats = stats
                };

                Buffs.Add(buff);
            }
            else
            {
                if (!updateOnly)
                {
                    switch (buff.StackType)
                    {
                        case BuffStackType.ResetDuration:
                            {
                                buff.ExpireTime = duration;
                            }
                            break;
                        case BuffStackType.StackDuration:
                            {
                                buff.ExpireTime += duration;
                            }
                            break;
                        case BuffStackType.StackStat:
                            {
                                if (stats != null)
                                {
                                    buff.Stats.Add(stats);
                                }
                            }
                            break;
                        case BuffStackType.StackStatAndDuration:
                            {
                                if (stats != null)
                                {
                                    buff.Stats.Add(stats);
                                }

                                buff.ExpireTime += duration;
                            }
                            break;
                        case BuffStackType.ResetStat:
                        {
                            if (stats != null)
                            {
                                buff.Stats = stats;
                            }
                        }
                            break;
                        case BuffStackType.ResetStatAndDuration:
                        {
                            buff.ExpireTime = duration;
                            if (stats != null)
                            {
                                buff.Stats = stats;
                            }
                        }
                            break;
                        case BuffStackType.Infinite:
                        case BuffStackType.None:
                            break;
                    }
                }
            }

            if (buff.Properties.HasFlag(BuffProperty.PauseInSafeZone) && InSafeZone)
            {
                buff.Paused = true;
            }

            buff.Stats ??= new Stats();
            buff.Values = values ?? new int[0];

            if (buff.Caster?.Node == null)
                buff.Caster = owner;

            switch (buff.Type)
            {
                case BuffType.MoonLight:
                case BuffType.DarkBody:
                    Hidden = true;
                    Sneaking = true;
                    HideFromTargets();
                    break;
                case BuffType.Hiding:
                case BuffType.ClearRing:
                    Hidden = true;
                    HideFromTargets();
                    break;
            }

            return buff;
        }

        public virtual void RemoveBuff(BuffType b)
        {
            for (int i = 0; i < Buffs.Count; i++)
            {
                if (Buffs[i].Type != b) continue;

                Buffs[i].FlagForRemoval = true;
                Buffs[i].Paused = false;
                Buffs[i].ExpireTime = 0;

                switch(b)
                {
                    case BuffType.Hiding:
                    case BuffType.MoonLight:
                    case BuffType.DarkBody:
                        if (!HasAnyBuffs(b, BuffType.ClearRing, BuffType.Hiding, BuffType.MoonLight, BuffType.DarkBody))
                        {
                            Hidden = false;
                        }
                        break;
                }
            }
        }
        public bool HasBuff(BuffType type)
        {
            for (int i = 0; i < Buffs.Count; i++)
            {
                if (Buffs[i].Type != type) continue;
                return true;
            }
            return false;
        }
        public bool HasBuff(BuffType type, out Buff buff)
        {
            for (int i = 0; i < Buffs.Count; i++)
            {
                if (Buffs[i].Type != type) continue;

                buff = Buffs[i];
                return true;
            }

            buff = null;
            return false;
        }

        public bool HasAnyBuffs(BuffType exceptBuff, params BuffType[] types)
        {
            return Buffs.Select(x => x.Type).Except(new List<BuffType> { exceptBuff }).Intersect(types).Any();
        }

        public virtual void PauseBuff(Buff b)
        {
            if (b.Paused) return;

            b.Paused = true;
        }

        public virtual void UnpauseBuff(Buff b)
        {
            if (!b.Paused) return;

            b.Paused = false;
        }

        protected void HideFromTargets()
        {
            for (int y = CurrentLocation.Y - Globals.DataRange; y <= CurrentLocation.Y + Globals.DataRange; y++)
            {
                if (y < 0) continue;
                if (y >= CurrentMap.Height) break;

                for (int x = CurrentLocation.X - Globals.DataRange; x <= CurrentLocation.X + Globals.DataRange; x++)
                {
                    if (x < 0) continue;
                    if (x >= CurrentMap.Width) break;
                    if (x < 0 || x >= CurrentMap.Width) continue;

                    Cell cell = CurrentMap.GetCell(x, y);

                    if (!cell.Valid || cell.Objects == null) continue;

                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        MapObject ob = cell.Objects[i];
                        if (ob.Race != ObjectType.Monster) continue;

                        if (ob.Target == this && (!ob.CoolEye || ob.Level < Level)) ob.Target = null;
                    }
                }
            }
        }

        public bool CheckStacked()
        {
            Cell cell = CurrentMap.GetCell(CurrentLocation);

            if (cell.Objects != null)
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    MapObject ob = cell.Objects[i];
                    if (ob == this || !ob.Blocking) continue;
                    return true;
                }

            return false;
        }

        public virtual bool Teleport(Map temp, Point location, bool effects = true, byte effectnumber = 0)
        {
            if (temp == null || !temp.ValidPoint(location)) return false;

            CurrentMap.RemoveObject(this);
            if (effects) Broadcast(new S.ObjectTeleportOut {ObjectID = ObjectID, Type = effectnumber});
            Broadcast(new S.ObjectRemove {ObjectID = ObjectID});
            
            CurrentMap = temp;
            CurrentLocation = location;

            InTrapRock = false;

            CurrentMap.AddObject(this);
            BroadcastInfo();

            if (effects) Broadcast(new S.ObjectTeleportIn { ObjectID = ObjectID, Type = effectnumber });
            
            BroadcastHealthChange();
            
            return true;
        }

        public virtual bool TeleportRandom(int attempts, int distance, Map map = null)
        {
            if (map == null) map = CurrentMap;
            if (map.Cells == null) return false;
            if (map.WalkableCells.Count == 0) return false;

            int cellIndex = Envir.Random.Next(map.WalkableCells.Count);

            return Teleport(map, map.WalkableCells[cellIndex]);
        }

        public Point GetRandomPoint(int attempts, int distance, Map map)
        {
            byte edgeoffset = 0;

            if (map.Width < 150)
            {
                if (map.Height < 30) edgeoffset = 2;
                else edgeoffset = 20;
            }

            for (int i = 0; i < attempts; i++)
            {
                Point location;

                if (distance <= 0)
                    location = new Point(edgeoffset + Envir.Random.Next(map.Width - edgeoffset), edgeoffset + Envir.Random.Next(map.Height - edgeoffset)); //Can adjust Random Range...
                else
                    location = new Point(CurrentLocation.X + Envir.Random.Next(-distance, distance + 1),
                                         CurrentLocation.Y + Envir.Random.Next(-distance, distance + 1));


                if (map.ValidPoint(location)) return location;
            }

            return new Point(0, 0);
        }

        public virtual void BroadcastHealthChange()
        {
            if (Race != ObjectType.Player && Race != ObjectType.Monster) return;

            byte time = Math.Min(byte.MaxValue, (byte)Math.Max(5, (RevTime - Envir.Time) / 1000));
            Packet p = new S.ObjectHealth { ObjectID = ObjectID, Percent = PercentHealth, Expire = time };

            if (Envir.Time < RevTime)
            {
                CurrentMap.Broadcast(p, CurrentLocation);
                return;
            }

            if (Race == ObjectType.Monster && !AutoRev && Master == null) return;

            if (Race == ObjectType.Player)
            {
                if (GroupMembers != null) //Send HP to group
                {
                    for (int i = 0; i < GroupMembers.Count; i++)
                    {
                        PlayerObject member = GroupMembers[i];

                        if (this == member) continue;
                        if (member.CurrentMap != CurrentMap || !Functions.InRange(member.CurrentLocation, CurrentLocation, Globals.DataRange)) continue;
                        member.Enqueue(p);
                    }
                }

                return;
            }

            if (Master != null && Master.Race == ObjectType.Player)
            {
                PlayerObject player = (PlayerObject)Master;

                player.Enqueue(p);

                if (player.GroupMembers != null) //Send pet HP to group
                {
                    for (int i = 0; i < player.GroupMembers.Count; i++)
                    {
                        PlayerObject member = player.GroupMembers[i];

                        if (player == member) continue;

                        if (member.CurrentMap != CurrentMap || !Functions.InRange(member.CurrentLocation, CurrentLocation, Globals.DataRange)) continue;
                        member.Enqueue(p);
                    }
                }
            }


            if (EXPOwner != null && EXPOwner.Race == ObjectType.Player)
            {
                PlayerObject player = (PlayerObject)EXPOwner;

                if (player.IsMember(Master)) return;
                
                player.Enqueue(p);

                if (player.GroupMembers != null)
                {
                    for (int i = 0; i < player.GroupMembers.Count; i++)
                    {
                        PlayerObject member = player.GroupMembers[i];

                        if (player == member) continue;
                        if (member.CurrentMap != CurrentMap || !Functions.InRange(member.CurrentLocation, CurrentLocation, Globals.DataRange)) continue;
                        member.Enqueue(p);
                    }
                }
            }
        }

        public void BroadcastDamageIndicator(DamageType type, int damage = 0)
        {
            Packet p = new S.DamageIndicator { ObjectID = ObjectID, Damage = damage, Type = type };

            if (Race == ObjectType.Player)
            {
                PlayerObject player = (PlayerObject)this;
                player.Enqueue(p);
            }
            Broadcast(p);
        }

        public abstract void Die();

        public abstract int Pushed(MapObject pusher, MirDirection dir, int distance);

        public bool IsMember(MapObject member)
        {
            if (member == this) return true;
            if (GroupMembers == null || member == null) return false;

            for (int i = 0; i < GroupMembers.Count; i++)
                if (GroupMembers[i] == member) return true;

            return false;
        }

        public abstract void SendHealth(HumanObject player);

        public bool InTrapRock
        {
            set
            {
                if (this is PlayerObject)
                {
                    var player = (PlayerObject)this;
                    player.Enqueue(new S.InTrapRock { Trapped = value });
                }
            }
            get
            {
                Point checklocation;

                for (int i = 0; i <= 6; i += 2)
                {
                    checklocation = Functions.PointMove(CurrentLocation, (MirDirection)i, 1);

                    if (checklocation.X < 0) continue;
                    if (checklocation.X >= CurrentMap.Width) continue;
                    if (checklocation.Y < 0) continue;
                    if (checklocation.Y >= CurrentMap.Height) continue;

                    Cell cell = CurrentMap.GetCell(checklocation.X, checklocation.Y);
                    if (!cell.Valid || cell.Objects == null) continue;

                    for (int j = 0; j < cell.Objects.Count; j++)
                    {
                        MapObject ob = cell.Objects[j];
                        switch (ob.Race)
                        {
                            case ObjectType.Monster:
                                if (ob is TrapRock)
                                {
                                    TrapRock rock = (TrapRock)ob;
                                    if (rock.Dead) continue;
                                    if (rock.Target != this) continue;
                                    if (!rock.Visible) continue;
                                }
                                else continue;

                                return true;
                            default:
                                continue;
                        }
                    }
                }
                return false;
            }
        }

    }

    public class Poison
    {
        private MapObject owner;
        public MapObject Owner
        {
            get 
            { 
                return owner switch
                {
                    HeroObject hero => hero.Owner,
                    _ => owner
                };
            }
            set { owner = value; }
        }
        public PoisonType PType;
        public int Value;
        public long Duration, Time, TickTime, TickSpeed;

        public Poison() { }

        public Poison(BinaryReader reader)
        {
            Owner = null;
            PType = (PoisonType)reader.ReadByte();
            Value = reader.ReadInt32();
            Duration = reader.ReadInt64();
            Time = reader.ReadInt64();
            TickTime = reader.ReadInt64();
            TickSpeed = reader.ReadInt64();
        }
    }
}
