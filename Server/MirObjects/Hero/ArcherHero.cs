using Server.MirDatabase;
using Server.MirEnvir;
using System.Buffers;
using System.Drawing;

namespace Server.MirObjects
{
    public class ArcherHero : HeroObject
    {
        public ArcherHero(CharacterInfo info, PlayerObject owner) : base(info, owner) { }
        protected override int KiteRadius => 3;

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;

            if (HasRangedSpell)
                return TargetDistance <= ViewRange;

            return Target.CurrentLocation != CurrentLocation && Functions.InRange(CurrentLocation, Target.CurrentLocation, 1);
        }

        protected override void ProcessFriend()
        {
            if (!CanCast) return;

            if (Target != null)
            {
                if (!HasBuff(BuffType.Concentration))
                {
                    UserMagic magic = GetMagic(Spell.Concentration);
                    if (CanUseMagic(magic))
                    {
                        BeginMagic(magic.Spell, Direction, ObjectID, CurrentLocation);
                        return;
                    }
                }
            }
        }

        protected override MapObject SelectTarget(List<MapObject> candidates)
        {
            if (Owner.Info.HeroBehaviour != HeroBehaviour.Custom) return base.SelectTarget(candidates);

            MapObject best = null;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (best == null || candidates[i].PercentHealth < best.PercentHealth)
                    best = candidates[i];
            }

            return best;
        }

        protected override void ProcessAttack()
        {
            if (!CanCast || Target == null || Target.Dead) return;
            if (!HasRangedSpell) return;
            TargetDistance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            UserMagic magic;

            if (InAttackRange())
            {
                magic = GetMagic(Spell.PoisonShot);
                if (CanUseMagic(magic))
                {
                    if (!Target.PoisonList.Any(p => p.PType == PoisonType.Green))
                    {
                        if (!HasBuff(BuffType.PoisonShot))
                        {
                            BeginMagic(magic.Spell, Direction, Target.ObjectID, Target.CurrentLocation);
                            return;
                        }
                    }
                }

                if (GetElementalOrbCount() < 1 || GetElementalOrbCount() > 3)
                {
                    magic = GetMagic(Spell.ElementalShot);
                    if (CanUseMagic(magic))
                    {
                        BeginMagic(magic.Spell, Direction, Target.ObjectID, Target.CurrentLocation);
                        return;
                    }
                }

                magic = GetMagic(Spell.StraightShot);
                if (CanUseMagic(magic))
                {
                    BeginMagic(magic.Spell, Direction, Target.ObjectID, Target.CurrentLocation);
                    return;
                }

                magic = GetMagic(Spell.None);
                {
                    return;
                }
            }

            magic = GetMagic(Spell.None);
            {
                return;
            }
        }

        protected override void ProcessTarget()
        {
            int distanceToPlayer = Functions.MaxDistance(CurrentLocation, Owner.CurrentLocation);

            if (Owner.PMode == PetMode.FocusMasterTarget && Target != null && !Target.IsAttackTarget(Owner))
            {
                Target = null;
                return;
            }

            if (CanMove && Owner.Info.HeroBehaviour == HeroBehaviour.CounterAttack && (Target == null || !CanAttack) && distanceToPlayer > 2)
            {
                MoveTo(Owner.Back);
                return;
            }

            if (Target == null || !CanAttack) return;

            if (HasClassWeapon && CanCast && NextMagicSpell != Spell.None)
            {
                Magic(NextMagicSpell, NextMagicDirection, NextMagicTargetID, NextMagicLocation);
                NextMagicSpell = Spell.None;
            }

            TargetDistance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);

            if (DefensiveBehaviour && HasClassWeapon && TryStepAwayFromThreats(KiteRadius))
                return;

            if ((CanAttack && Target != null && HasClassWeapon && (NextMagicSpell == Spell.None || !HasRangedSpell || !CanCast)))
            {
                Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
                RangeAttack(Direction, Target.CurrentLocation, Target.ObjectID);

                if (Target.Dead)
                {
                    FindTarget();
                }
                return;
            }

            if (CanAttack && (!HasWeapon || (HasWeapon && !HasClassWeapon)))
            {
                if (TargetDistance >= 1 && InAttackRange())
                {
                    Attack();

                    if (Target.Dead)
                    {
                        FindTarget();
                        return;
                    }
                }
                else
                {
                    MoveTo(Target.CurrentLocation);
                    return;
                }
            }
        }
        private bool HasRangedSpell => Info.Magics.Select(x => x.Spell).Intersect(Globals.RangedSpells).Any();
        public bool HasWeapon => Info.Equipment[(int)EquipmentSlot.Weapon] != null && Info.Equipment[(int)EquipmentSlot.Weapon].CurrentDura > 0;
        public bool HasClassWeapon
        {
            get
            {
                var classweapon = Info.Equipment[(int)EquipmentSlot.Weapon];
                return classweapon != null && classweapon.Info.RequiredClass == RequiredClass.Archer && classweapon.CurrentDura > 0;
            }
        }
    }
}
