using System.Drawing;
using Server.MirEnvir;
using Shared;

namespace Server.MirObjects
{
    /// <summary>
    /// Pet side of MonsterObject. Crystal does not model pets as a separate type: taming and
    /// summoning both assign Master on an existing monster, and a tame returns to the wild by
    /// clearing it again. Everything that depends on the owner's stance lives here so the rules
    /// stay in one place instead of being re-derived from Master.PMode at every call site.
    /// </summary>
    public partial class MonsterObject
    {
        /// <summary>True when this object has an owner (summoned, tamed or hired).</summary>
        public bool IsPet
        {
            get { return Master != null; }
        }

        /// <summary>True for a temporary tame, which returns to the wild when TameTime expires.</summary>
        public bool IsTamed
        {
            get { return Master != null && TameTime > 0; }
        }

        /// <summary>True when the owner's stance lets pets walk.</summary>
        public bool MasterAllowsMove
        {
            get
            {
                return Master == null ||
                    Master.PMode == PetMode.MoveOnly ||
                    Master.PMode == PetMode.Both ||
                    Master.PMode == PetMode.FocusMasterTarget;
            }
        }

        /// <summary>True when the owner's stance lets pets attack.</summary>
        public bool MasterAllowsAttack
        {
            get
            {
                return Master == null ||
                    Master.PMode == PetMode.AttackOnly ||
                    Master.PMode == PetMode.Both ||
                    Master.PMode == PetMode.FocusMasterTarget;
            }
        }

        /// <summary>True when the owner's stance lets pets pick their own targets.</summary>
        public bool MasterAllowsSearch
        {
            get
            {
                return Master == null ||
                    Master.PMode == PetMode.AttackOnly ||
                    Master.PMode == PetMode.Both;
            }
        }

        /// <summary>True when the owner's stance makes pets drop their target (follow and rest).</summary>
        public bool MasterClearsTarget
        {
            get
            {
                return Master != null &&
                    (Master.PMode == PetMode.MoveOnly || Master.PMode == PetMode.None);
            }
        }

        /// <summary>
        /// Owner-facing state for this tick: forced recall when the maps differ, unfreezing once the
        /// pet is back on the owner's map, recall when the owner walks or teleports away, and
        /// clearing the target in follow and rest stances. Returns false when the pet must stay
        /// frozen and skip the rest of its AI.
        /// </summary>
        protected bool ProcessPetStatus()
        {
            if (Master != null && Master.CurrentMap != null)
            {
                bool masterAllowsPets = !Master.CurrentMap.Info.NoPets || IgnoresNoPetRestriction;
                bool needsRecall = CurrentMap != Master.CurrentMap;

                // If frozen AND on different map AND master allows pets — force recall
                if (Frozen && needsRecall && masterAllowsPets)
                {
                    PetRecall();
                    return false;
                }

                // If frozen but already on correct map — unfreeze
                if (Frozen && !needsRecall && masterAllowsPets)
                {
                    Frozen = false;
                    PMode = PetMode.Both;
                }
            }

            // Still frozen = do nothing
            if (Frozen) return false;

            if (Master == null) return true;

            Map masterMap = Master.CurrentMap;
            Point masterLocation = Master.CurrentLocation;

            // Recall follows the owner, it is not a movement order: a pet told not to move still has
            // to come along through a map change or when the owner walks out of data range, otherwise
            // it is stranded for good. NoPets maps are the only exception, and PetRecall freezes the
            // pet there instead of refusing to move it.
            if (masterMap != null &&
                (CurrentMap != masterMap || !Functions.InRange(CurrentLocation, masterLocation, Globals.DataRange)))
            {
                PetRecall();
            }

            if (MasterClearsTarget)
            {
                Target = null;
            }

            return true;
        }

        /// <summary>
        /// The player a pet master belongs to. A hero fights for its owner, so the owner, the hero
        /// and the pets of both resolve to one side instead of being treated as separate factions.
        /// </summary>
        private static PlayerObject GetSideOwner(MapObject master)
        {
            if (master is HeroObject hero) return hero.Owner;

            return master as PlayerObject;
        }

        /// <summary>
        /// True when both masters fight for the same player. Comparing the raw Master references
        /// made the hero's pets and the owner's pets consider each other enemies.
        /// </summary>
        private static bool IsSameSide(MapObject master, MapObject other)
        {
            if (master == other) return true;

            PlayerObject owner = GetSideOwner(master);

            return owner != null && owner == GetSideOwner(other);
        }

        /// <summary>
        /// True when the object is already part of the fight the master's side is in: it is
        /// attacking the master, the linked hero or one of the side's pets, or one of the side's
        /// pets is attacking it. Summons used to look at the master and the master's pets only, so
        /// a monster the owner's hero was fighting counted as unrelated: the summon walked back and
        /// stood next to its owner while the hero carried on alone.
        /// </summary>
        private static bool IsFightingSameSide(MapObject master, MapObject monster)
        {
            if (master == null || monster == null) return false;

            // A player and its hero are one side whichever of the two holds the master reference.
            MapObject linked = null;

            if (master is PlayerObject player) linked = player.Hero;
            else if (master is HeroObject hero) linked = hero.Owner;

            if (monster.Target == master) return true;
            if (linked != null && monster.Target == linked) return true;

            if (IsSidePetFighting(master, monster)) return true;

            return linked != null && IsSidePetFighting(linked, monster);
        }

        /// <summary>
        /// True when one of the side's pets is the object, is attacking it, or is its target.
        /// </summary>
        private static bool IsSidePetFighting(MapObject side, MapObject monster)
        {
            for (int i = 0; i < side.Pets.Count; i++)
            {
                MonsterObject pet = side.Pets[i];

                if (pet == monster || pet.Target == monster || monster.Target == pet) return true;
            }

            return false;
        }
    }
}
