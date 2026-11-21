using System.Drawing;
﻿using Server.MirDatabase;
using Server.MirEnvir;
using Server.MirNetwork;
using Server.MirObjects.Monsters;
using System.Numerics;
using S = ServerPackets;

namespace Server.MirObjects
{
    public class HumanObject : MapObject
    {
        public override ObjectType Race
        {
            get { return ObjectType.Player; }
        }

        public CharacterInfo Info;

        protected MirConnection connection;
        public virtual MirConnection Connection
        {
            get { return connection; }
            set { connection = value; }
        }
        public override string Name
        {
            get { return Info.Name; }
            set { /*Check if Name exists.*/ }
        }
        public override int CurrentMapIndex
        {
            get { return Info.CurrentMapIndex; }
            set { Info.CurrentMapIndex = value; }
        }
        public override Point CurrentLocation
        {
            get { return Info.CurrentLocation; }
            set { Info.CurrentLocation = value; }
        }
        public override MirDirection Direction
        {
            get { return Info.Direction; }
            set { Info.Direction = value; }
        }
        public override ushort Level
        {
            get { return Info.Level; }
            set { Info.Level = value; }
        }
        public override int Health
        {
            get { return HP; }
        }
        public override int MaxHealth
        {
            get { return Stats[Stat.HP]; }
        }
        public int HP
        {
            get { return Info.HP; }
            set { Info.HP = value; }
        }
        public int MP
        {
            get { return Info.MP; }
            set { Info.MP = value; }
        }
        public override AttackMode AMode
        {
            get { return Info.AMode; }
            set { Info.AMode = value; }
        }
        public override PetMode PMode
        {
            get { return Info.PMode; }
            set { Info.PMode = value; }
        }
        public long Experience
        {
            set { Info.Experience = value; }
            get { return Info.Experience; }
        }

        public long MaxExperience;
        public byte Hair
        {
            get { return Info.Hair; }
            set { Info.Hair = value; }
        }
        public MirClass Class
        {
            get { return Info.Class; }
        }
        public MirGender Gender
        {
            get { return Info.Gender; }
        }
        public override List<Buff> Buffs
        {
            get { return Info.Buffs; }
            set { Info.Buffs = value; }
        }
        public override List<Poison> PoisonList
        {
            get { return Info.Poisons; }
            set { Info.Poisons = value; }
        }

        public bool RidingMount;
        public MountInfo Mount
        {
            get { return Info.Mount; }
        }        

        public Reporting Report;
        public virtual bool CanMove
        {
            get
            {
                return !Dead && Envir.Time >= ActionTime && !CurrentPoison.HasFlag(PoisonType.Paralysis) && !CurrentPoison.HasFlag(PoisonType.LRParalysis) && !CurrentPoison.HasFlag(PoisonType.Frozen);
            }
        }
        public virtual bool CanWalk
        {
            get
            {
                return !Dead && Envir.Time >= ActionTime && !InTrapRock && !CurrentPoison.HasFlag(PoisonType.Paralysis) && !CurrentPoison.HasFlag(PoisonType.LRParalysis) && !CurrentPoison.HasFlag(PoisonType.Frozen);
            }
        }
        public virtual bool CanRun
        {
            get
            {
                return !Dead && Envir.Time >= ActionTime && (_stepCounter > 0 || FastRun) && (!Sneaking || ActiveSwiftFeet) && CurrentBagWeight <= Stats[Stat.BagWeight] && !CurrentPoison.HasFlag(PoisonType.Paralysis) && !CurrentPoison.HasFlag(PoisonType.LRParalysis) && !CurrentPoison.HasFlag(PoisonType.Frozen);
            }
        }
        public virtual bool CanAttack
        {
            get
            {
                return !Dead && Envir.Time >= ActionTime && Envir.Time >= AttackTime && !CurrentPoison.HasFlag(PoisonType.Paralysis) && !CurrentPoison.HasFlag(PoisonType.LRParalysis) && !CurrentPoison.HasFlag(PoisonType.Frozen) && !CurrentPoison.HasFlag(PoisonType.Dazed) && Mount.CanAttack;
            }
        }
        public bool CanRegen
        {
            get
            {
                return Envir.Time >= RegenTime;
            }
        }
        protected virtual bool CanCast
        {
            get
            {
                return !Dead && Envir.Time >= ActionTime && Envir.Time >= SpellTime && !CurrentPoison.HasFlag(PoisonType.Stun) && !CurrentPoison.HasFlag(PoisonType.Dazed) &&
                    !CurrentPoison.HasFlag(PoisonType.Paralysis) && !CurrentPoison.HasFlag(PoisonType.Frozen) && Mount.CanAttack;
            }
        }

        protected bool CheckCellTime = true;

        public short TransformType;
        public short Looks_Armour = 0, Looks_Weapon = -1, Looks_WeaponEffect = 0;
        public byte Looks_Wings = 0;

        public int CurrentHandWeight,
                   CurrentWearWeight,
                   CurrentBagWeight;

        public bool HasElemental;
        public int ElementsLevel;

        public bool Stacking;
        public bool IsGM, GMNeverDie, GMGameMaster;
        public bool HasUpdatedBaseStats = true;

        public virtual int PotionBeltMinimum => 0;
        public virtual int PotionBeltMaximum => 4;
        public virtual int AmuletBeltMinimum => 4;
        public virtual int AmuletBeltMaximum => 6;
        public virtual int BeltSize => 6;

        public LevelEffects LevelEffects = LevelEffects.None;

        public const long LoyaltyDelay = 1000, ItemExpireDelay = 60000, DuraDelay = 10000, RegenDelay = 10000, PotDelay = 200, HealDelay = 600, VampDelay = 500, MoveDelay = 600;
        public long StruckTime, RunTime, ActionTime, AttackTime, RegenTime, SpellTime, StackingTime, IncreaseLoyaltyTime, ItemExpireTime, TorchTime, DuraTime, PotTime, HealTime, VampTime, LogTime, DecreaseLoyaltyTime, SearchTime, CarryBuffTime;

        protected int _stepCounter, _runCounter;

        private GuildObject myGuild = null;
        public virtual GuildObject MyGuild
        {
            get { return myGuild; }
            set { myGuild = value; }
        }
        public GuildRank MyGuildRank = null;

        public IntelligentCreatureType SummonedCreatureType = IntelligentCreatureType.None;
        public bool CreatureSummoned;

        public SpecialItemMode SpecialMode;

        public List<ItemSets> ItemSets = new List<ItemSets>();
        public List<EquipmentSlot> MirSet = new List<EquipmentSlot>();

        public bool FatalSword, Slaying, TwinDrakeBlade, FlamingSword, MPEater, Hemorrhage, CounterAttack;
        public int MPEaterCount, HemorrhageAttackCount;
        public long FlamingSwordTime, CounterAttackTime;
        public bool ActiveBlizzard, ActiveReincarnation, ActiveSwiftFeet, ReincarnationReady;
        public HumanObject ReincarnationTarget, ReincarnationHost;
        public long ReincarnationExpireTime;

        public long LastRevivalTime;
        public float HpDrain = 0;

        public bool UnlockCurse = false;
        public bool FastRun = false;
        public bool CanGainExp = true;
        private int _fireWallCastSeq = 0;
        public override bool Blocking
        {
            get
            {
                return !Dead && !Observer;
            }
        }
        public HumanObject() { }
        public HumanObject(CharacterInfo info, MirConnection connection)
        {
            Load(info, connection);
        }
        protected virtual void Load(CharacterInfo info, MirConnection connection) { }
        protected virtual void NewCharacter()
        {
            Level = 1;
            Hair = (byte)Envir.Random.Next(0, 9);

            for (int i = 0; i < Envir.StartItems.Count; i++)
            {
                ItemInfo info = Envir.StartItems[i];
                if (!CorrectStartItem(info)) continue;

                AddItem(Envir.CreateFreshItem(info));
            }
        }
        public long GetDelayTime(long original)
        {
            if (CurrentPoison.HasFlag(PoisonType.Slow))
            {
                return original * 2;
            }
            return original;
        }
        public override void Process()
        {
            if ((Race == ObjectType.Player && Connection == null) || Node == null || Info == null) return;

            if (CellTime + 700 < Envir.Time) _stepCounter = 0;

            if (Sneaking) CheckSneakRadius();

            if (FlamingSword && Envir.Time >= FlamingSwordTime)
            {
                FlamingSword = false;
                Enqueue(new S.SpellToggle { ObjectID = ObjectID, Spell = Spell.FlamingSword, CanUse = false });
            }

            if (CounterAttack && Envir.Time >= CounterAttackTime)
            {
                CounterAttack = false;
            }

            if (ReincarnationReady && Envir.Time >= ReincarnationExpireTime)
            {
                ReincarnationReady = false;
                ActiveReincarnation = false;
                ReincarnationTarget = null;
                ReceiveChat(GameLanguage.Get("Reincarnation failed."), ChatType.System);
            }
            if ((ReincarnationReady || ActiveReincarnation) && (ReincarnationTarget == null || !ReincarnationTarget.Dead))
            {
                ReincarnationReady = false;
                ActiveReincarnation = false;
                ReincarnationTarget = null;
            }

            if (Envir.Time > RunTime && _runCounter > 0)
            {
                RunTime = Envir.Time + 1500;
                _runCounter--;
            }

            if (Stacking && Envir.Time > StackingTime)
            {
                Stacking = false;

                for (int i = 0; i < 8; i++)
                {
                    if (Pushed(this, (MirDirection)i, 1) == 1) break;
                }
            }

            if (Mount.HasMount && Envir.Time > IncreaseLoyaltyTime)
            {
                IncreaseLoyaltyTime = Envir.Time + (LoyaltyDelay * 60);
                IncreaseMountLoyalty(1);
            }

            if (Envir.Time > ItemExpireTime)
            {
                ItemExpireTime = Envir.Time + ItemExpireDelay;

                ProcessItems();
            }

            for (int i = Pets.Count() - 1; i >= 0; i--)
            {
                MonsterObject pet = Pets[i];
                if (pet.Dead) Pets.Remove(pet);
            }

            ProcessBuffs();
            ProcessRegen();
            ProcessPoison();

            UserItem item;
            if (Envir.Time > TorchTime)
            {
                TorchTime = Envir.Time + 10000;
                item = Info.Equipment[(int)EquipmentSlot.Torch];
                if (item != null)
                {
                    DamageItem(item, 5);

                    if (item.CurrentDura == 0)
                    {
                        Info.Equipment[(int)EquipmentSlot.Torch] = null;
                        Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                        RefreshStats();
                    }
                }
            }

            if (Envir.Time > DuraTime)
            {
                DuraTime = Envir.Time + DuraDelay;

                for (int i = 0; i < Info.Equipment.Length; i++)
                {
                    item = Info.Equipment[i];
                    if (item == null || !item.DuraChanged) continue; // || item.Info.Type == ItemType.Mount
                    item.DuraChanged = false;
                    Enqueue(new S.DuraChanged { UniqueID = item.UniqueID, CurrentDura = item.CurrentDura });
                }
            }

            base.Process();

            RefreshNameColour();
        }

        public override void OnSafeZoneChanged()
        {
            base.OnSafeZoneChanged();

            bool needsUpdate = false;

            for (int i = 0; i < Buffs.Count; i++)
            {
                if (Buffs[i].ObjectID == 0) continue;
                if (!Buffs[i].Properties.HasFlag(BuffProperty.PauseInSafeZone)) continue;

                needsUpdate = true;

                if (InSafeZone)
                {
                    PauseBuff(Buffs[i]);
                }
                else
                {
                    UnpauseBuff(Buffs[i]);
                }
            }

            if (needsUpdate)
            {
                RefreshStats();
            }
        }

        public override void SetOperateTime()
        {
            OperateTime = Envir.Time;
        }
        public override void Die() { }
        // A carried token item grants its buff only while it stays in the bag.
        public static int GetCarriedExpBonus(UserItem[] inventory, DateTime now)
        {
            for (int i = 0; i < inventory.Length; i++)
            {
                UserItem item = inventory[i];

                if (item?.Info == null) continue;
                if (!item.Info.Unique.HasFlag(SpecialItemMode.CarryEffect)) continue;
                if (item.ExpireInfo != null && item.ExpireInfo.ExpiryDate <= now) continue;

                return item.Info.Stats[Stat.ExpRatePercent];
            }

            return 0;
        }

        protected void ProcessCarryBuffs()
        {
            int value = GetCarriedExpBonus(Info.Inventory, Envir.Now);

            bool has = HasBuff(BuffType.ExpStone, out Buff buff);

            if (value == 0)
            {
                if (has) RemoveBuff(BuffType.ExpStone);

                return;
            }

            if (has)
            {
                if (buff.Stats[Stat.ExpRatePercent] == value) return;

                buff.Stats[Stat.ExpRatePercent] = value;

                Enqueue(new S.AddBuff { Buff = buff.ToClientBuff() });
                RefreshStats();

                return;
            }

            AddBuff(BuffType.ExpStone, this, 0, new Stats { [Stat.ExpRatePercent] = value });
        }

        protected virtual void ProcessBuffs()
        {
            if (Envir.Time > CarryBuffTime)
            {
                CarryBuffTime = Envir.Time + 1000;

                ProcessCarryBuffs();
            }

            bool refresh = false;
            bool clearRing = false, skill = false, gm = false, mentor = false, lover = false;

            for (int i = Buffs.Count - 1; i >= 0; i--)
            {
                Buff buff = Buffs[i];

                switch (buff.Type)
                {
                    case BuffType.Concentration:
                        if (buff.Get<bool>("Interrupted") && buff.Get<long>("InterruptTime") <= Envir.Time)
                        {
                            buff.Set("Interrupted", false);
                            buff.Set("InterruptTime", (long)0);
                            UpdateConcentration(true, false);
                        }
                        break;
                    case BuffType.ClearRing:
                        clearRing = true;
                        if (!SpecialMode.HasFlag(SpecialItemMode.ClearRing)) buff.FlagForRemoval = true;
                        break;
                    case BuffType.Skill:
                        skill = true;
                        if (!SpecialMode.HasFlag(SpecialItemMode.Skill)) buff.FlagForRemoval = true;
                        break;
                    case BuffType.GameMaster:
                        gm = true;
                        if (!IsGM) buff.FlagForRemoval = true;
                        break;
                    case BuffType.Mentor:
                    case BuffType.Mentee:
                        mentor = true;
                        if (Info.Mentor == 0) buff.FlagForRemoval = true;
                        break;
                    case BuffType.Lover:
                        lover = true;
                        if (Info.Married == 0) buff.FlagForRemoval = true;
                        break;
                }

                if (buff.NextTime > Envir.Time) continue;

                if (!buff.Paused && buff.StackType != BuffStackType.Infinite)
                {
                    var change = Envir.Time - buff.LastTime;
                    buff.ExpireTime -= change;
                }

                buff.LastTime = Envir.Time;
                buff.NextTime = Envir.Time + 1000;

                if ((buff.ExpireTime > 0 || buff.StackType == BuffStackType.Infinite) && !buff.FlagForRemoval) continue;

                Buffs.RemoveAt(i);
                Enqueue(new S.RemoveBuff { Type = buff.Type, ObjectID = ObjectID });

                if (buff.Info.Visible)
                {
                    Broadcast(new S.RemoveBuff { Type = buff.Type, ObjectID = ObjectID });
                }

                switch (buff.Type)
                {
                    case BuffType.Hiding:
                    case BuffType.MoonLight:
                    case BuffType.DarkBody:
                    case BuffType.ClearRing:
                        if (!HasAnyBuffs(buff.Type, BuffType.ClearRing, BuffType.Hiding, BuffType.MoonLight, BuffType.DarkBody))
                        {
                            Hidden = false;
                        }
                        if (buff.Type == BuffType.MoonLight || buff.Type == BuffType.DarkBody)
                        {
                            if (!HasAnyBuffs(buff.Type, BuffType.MoonLight, BuffType.DarkBody))
                            {
                                Sneaking = false;
                            }
                            break;
                        }
                        break;
                    case BuffType.Concentration:
                        UpdateConcentration(false, false);
                        break;
                    case BuffType.SwiftFeet:
                        ActiveSwiftFeet = false;
                        break;
                    case BuffType.MagicShield:
                        CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.MagicShieldDown }, CurrentLocation);
                        break;
                    case BuffType.ElementalBarrier:
                        CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.ElementalBarrierDown }, CurrentLocation);
                        break;
                }

                refresh = true;
            }

            if (IsGM && !gm)
            {
                UpdateGMBuff();
            }

            if (SpecialMode.HasFlag(SpecialItemMode.ClearRing) && !clearRing)
            {
                AddBuff(BuffType.ClearRing, this, 0, new Stats());
            }

            if (SpecialMode.HasFlag(SpecialItemMode.Skill) && !skill)
            {
                AddBuff(BuffType.Skill, this, 0, new Stats { [Stat.SkillGainMultiplier] = 3 }, false);
            }

            if (Info.Mentor != 0 && !mentor)
            {
                CharacterInfo partnerC = Envir.GetCharacterInfo(Info.Mentor);
                PlayerObject partnerP = partnerC != null ? Envir.GetPlayer(partnerC.Name) : null;

                if (partnerP != null)
                {
                    if (Info.IsMentor)
                    {
                        AddBuff(BuffType.Mentor, partnerP, 0, new Stats { [Stat.MentorDamageRatePercent] = Settings.MentorDamageBoost });
                    }
                    else
                    {
                        AddBuff(BuffType.Mentee, partnerP, 0, new Stats { [Stat.MentorExpRatePercent] = Settings.MentorExpBoost });
                    }
                }
            }

            if (Info.Married != 0 && !lover)
            {
                CharacterInfo loverC = Envir.GetCharacterInfo(Info.Married);
                PlayerObject loverP = loverC != null ? Envir.GetPlayer(loverC.Name) : null;

                if (loverP != null)
                {
                    AddBuff(BuffType.Lover, loverP, 0, new Stats { [Stat.LoverExpRatePercent] = Settings.LoverEXPBonus });
                }
            }

            if (MyGuild != null && MyGuild.Name == Settings.NewbieGuild && Settings.NewbieGuildBuffEnabled == true)
            {
                AddBuff(BuffType.Newbie, this, 0, new Stats { [Stat.ExpRatePercent] = Settings.NewbieGuildExpBuff });
            }

            if (refresh)
            {
                RefreshStats();
            }
        }
        private void ProcessRegen()
        {
            if (Dead) return;

            int healthRegen = 0, manaRegen = 0;

            if (CanRegen)
            {
                RegenTime = Envir.Time + RegenDelay;

                if (HP < Stats[Stat.HP])
                {
                    healthRegen += (int)(Stats[Stat.HP] * 0.03F) + 1;
                    healthRegen += (int)(healthRegen * ((double)Stats[Stat.HealthRecovery] / Settings.HealthRegenWeight));
                }

                if (MP < Stats[Stat.MP])
                {
                    manaRegen += (int)(Stats[Stat.MP] * 0.03F) + 1;
                    manaRegen += (int)(manaRegen * ((double)Stats[Stat.SpellRecovery] / Settings.ManaRegenWeight));
                }
            }

            if (Envir.Time > PotTime)
            {
                //PotTime = Envir.Time + Math.Max(50,Math.Min(PotDelay, 600 - (Level * 10)));
                PotTime = Envir.Time + PotDelay;
                int PerTickRegen = 5 + (Level / 10);

                if (PotHealthAmount > PerTickRegen)
                {
                    healthRegen += PerTickRegen;
                    PotHealthAmount -= (ushort)PerTickRegen;
                }
                else
                {
                    healthRegen += PotHealthAmount;
                    PotHealthAmount = 0;
                }

                if (PotManaAmount > PerTickRegen)
                {
                    manaRegen += PerTickRegen;
                    PotManaAmount -= (ushort)PerTickRegen;
                }
                else
                {
                    manaRegen += PotManaAmount;
                    PotManaAmount = 0;
                }
            }

            if (Envir.Time > HealTime)
            {
                HealTime = Envir.Time + HealDelay;

                int incHeal = (Level / 10) + (HealAmount / 10);
                if (HealAmount > (5 + incHeal))
                {
                    healthRegen += (5 + incHeal);
                    HealAmount -= (ushort)Math.Min(HealAmount, 5 + incHeal);
                }
                else
                {
                    healthRegen += HealAmount;
                    HealAmount = 0;
                }
            }

            if (Envir.Time > VampTime)
            {
                VampTime = Envir.Time + VampDelay;

                if (VampAmount > 10)
                {
                    healthRegen += 10;
                    VampAmount -= 10;
                }
                else
                {
                    healthRegen += VampAmount;
                    VampAmount = 0;
                }
            }

            if (healthRegen > 0)
            {
                ChangeHP(healthRegen);
                BroadcastDamageIndicator(DamageType.Hit, healthRegen);
            }

            if (HP == Stats[Stat.HP])
            {
                PotHealthAmount = 0;
                HealAmount = 0;
            }

            if (manaRegen > 0) ChangeMP(manaRegen);
            if (MP == Stats[Stat.MP]) PotManaAmount = 0;
        }
        private void ProcessPoison()
        {
            PoisonType type = PoisonType.None;
            bool redPoisoned = false;
            DamageRate = 1F;

            for (int i = PoisonList.Count - 1; i >= 0; i--)
            {
                if (Dead) return;

                Poison poison = PoisonList[i];

                if (poison.Owner != null && poison.Owner.Node == null)
                {
                    PoisonList.RemoveAt(i);
                    continue;
                }

                if (Envir.Time > poison.TickTime)
                {
                    poison.Time++;
                    poison.TickTime = Envir.Time + poison.TickSpeed;

                    if (poison.Time >= poison.Duration)
                    {
                        PoisonList.RemoveAt(i);
                    }

                    if (poison.PType == PoisonType.Green || poison.PType == PoisonType.Bleeding)
                    {
                        LastHitter = poison.Owner;
                        LastHitTime = Envir.Time + 10000;

                        if (poison.PType == PoisonType.Bleeding)
                        {
                            Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Bleeding, EffectType = 0 });
                        }

                        PoisonDamage(-poison.Value, poison.Owner);
                        BroadcastDamageIndicator(DamageType.Hit, -poison.Value);

                        if (Dead) break;
                        RegenTime = Envir.Time + RegenDelay;
                    }

                    if (poison.PType == PoisonType.DelayedExplosion)
                    {
                        if (Envir.Time > ExplosionInflictedTime) ExplosionInflictedStage++;

                        if (!ProcessDelayedExplosion(poison))
                        {
                            if (Dead) break;

                            ExplosionInflictedStage = 0;
                            ExplosionInflictedTime = 0;

                            PoisonList.RemoveAt(i);
                            continue;
                        }
                    }
                }

                switch (poison.PType)
                {
                    case PoisonType.Red:
                        redPoisoned = true;
                        break;
                    case PoisonType.Stun:
                        DamageRate += 0.20F;
                        break;
                    case PoisonType.Blindness:
                        break;
                }

                type |= poison.PType;
            }

            SetRedArmourPoison(redPoisoned);

            if (type == CurrentPoison) return;

            Enqueue(new S.Poisoned { Poison = type });
            Broadcast(new S.ObjectPoisoned { ObjectID = ObjectID, Poison = type });

            CurrentPoison = type;
        }
        private bool ProcessDelayedExplosion(Poison poison)
        {
            if (Dead) return false;

            if (ExplosionInflictedStage == 0)
            {
                Enqueue(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 0 });
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 0 });
                return true;
            }
            if (ExplosionInflictedStage == 1)
            {
                if (Envir.Time > ExplosionInflictedTime)
                    ExplosionInflictedTime = poison.TickTime + 3000;
                Enqueue(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 1 });
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 1 });
                return true;
            }
            if (ExplosionInflictedStage == 2)
            {
                Enqueue(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 2 });
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 2 });
                if (poison.Owner != null)
                {
                    switch (poison.Owner.Race)
                    {
                        case ObjectType.Player:
                            PlayerObject caster = (PlayerObject)poison.Owner;
                            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time, poison.Owner, caster.GetMagic(Spell.DelayedExplosion), poison.Value, this.CurrentLocation);
                            CurrentMap.ActionList.Add(action);
                            break;
                        case ObjectType.Monster://this is in place so it could be used by mobs if one day someone chooses to
                            Attacked((MonsterObject)poison.Owner, poison.Value, DefenceType.MAC);
                            break;
                    }

                    LastHitter = poison.Owner;
                }
                return false;
            }
            return false;
        }
        private void ProcessItems()
        {
            for (var i = 0; i < Info.Inventory.Length; i++)
            {
                var item = Info.Inventory[i];

                if (item?.ExpireInfo?.ExpiryDate <= Envir.Now)
                {
                    ReceiveChat(GameLanguage.Get("{0} has just expired from your inventory.", item.Info.FriendlyName), ChatType.Hint);
                    Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                    Info.Inventory[i] = null;
                    continue;
                }

                if (item?.RentalInformation?.RentalLocked == true && item?.RentalInformation?.ExpiryDate <= Envir.Now)
                {
                    ReceiveChat(GameLanguage.Get("The rental lock has been removed from {0}.", item.Info.FriendlyName), ChatType.Hint);
                    item.RentalInformation = null;
                }
            }

            for (var i = 0; i < Info.Equipment.Length; i++)
            {
                var item = Info.Equipment[i];

                if (item?.ExpireInfo?.ExpiryDate <= Envir.Now)
                {
                    ReceiveChat(GameLanguage.Get("{0} has just expired from your equipment.", item.Info.FriendlyName), ChatType.Hint);
                    Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                    Info.Equipment[i] = null;
                    continue;
                }

                if (item?.RentalInformation?.RentalLocked == true && item?.RentalInformation?.ExpiryDate <= Envir.Now)
                {
                    ReceiveChat(GameLanguage.Get("The rental lock has been removed from {0}.", item.Info.FriendlyName), ChatType.Hint);
                    item.RentalInformation = null;
                }
            }

            if (Info.AccountInfo == null) return;

            for (int i = 0; i < Info.AccountInfo.Storage.Length; i++)
            {
                var item = Info.AccountInfo.Storage[i];
                if (item?.ExpireInfo?.ExpiryDate <= Envir.Now)
                {
                    ReceiveChat(GameLanguage.Get("{0} has just expired from your storage.", item.Info.FriendlyName), ChatType.Hint);
                    Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                    Info.AccountInfo.Storage[i] = null;
                    continue;
                }
            }
        }
        public override void Process(DelayedAction action)
        {
        }
        protected void UpdateGMBuff()
        {
            if (!IsGM) return;

            GMOptions options = GMOptions.None;

            if (GMGameMaster) options |= GMOptions.GameMaster;
            if (GMNeverDie) options |= GMOptions.Superman;
            if (Observer) options |= GMOptions.Observer;

            AddBuff(BuffType.GameMaster, this, 0, null, false, values: (byte)options);
        }
        public virtual void LevelUp()
        {
            RefreshStats();
            SetHP(Stats[Stat.HP]);
            SetMP(Stats[Stat.MP]);
            
            Broadcast(new S.ObjectLeveled { ObjectID = ObjectID });          
        }
        public virtual Color GetNameColour(HumanObject human)
        {
            return NameColour;
        }
        public virtual void RefreshNameColour() { }
        protected void SetHP(int amount)
        {
            if (HP == amount) return;

            HP = amount <= Stats[Stat.HP] ? amount : Stats[Stat.HP];
            HP = GMNeverDie ? Stats[Stat.HP] : HP;

            if (!Dead && HP == 0) Die();

            //HealthChanged = true;
            SendHealthChanged();
        }
        protected virtual void SendHealthChanged()
        {
            Enqueue(new S.HealthChanged { HP = HP, MP = MP });
            BroadcastHealthChange();
        }
        protected void SetMP(int amount)
        {
            if (MP == amount) return;
            //was info.MP
            MP = amount <= Stats[Stat.MP] ? amount : Stats[Stat.MP];
            MP = GMNeverDie ? Stats[Stat.MP] : MP;

            // HealthChanged = true;
            SendHealthChanged();
        }
        public void ChangeHP(int amount)
        {
            if (SpecialMode.HasFlag(SpecialItemMode.Protection) && MP > 0 && amount < 0)
            {
                ChangeMP(amount);
                return;
            }

            if (HP + amount > Stats[Stat.HP])
                amount = Stats[Stat.HP] - HP;

            if (amount == 0) return;

            HP += amount;
            HP = GMNeverDie ? Stats[Stat.HP] : HP;

            if (HP < 0) HP = 0;

            if (!Dead && HP == 0) Die();

            // HealthChanged = true;
            SendHealthChanged();
        }
        public void PoisonDamage(int amount, MapObject Attacker)
        {
            ChangeHP(amount);
        }
        public void ChangeMP(int amount)
        {
            if (MP + amount > Stats[Stat.MP])
                amount = Stats[Stat.MP] - MP;

            if (amount == 0) return;

            MP += amount;
            MP = GMNeverDie ? Stats[Stat.MP] : MP;

            if (MP < 0) MP = 0;

            // HealthChanged = true;
            SendHealthChanged();
        }
        public void GetMinePayout(MineSet Mine)
        {
            if ((Mine.Drops == null) || (Mine.Drops.Count == 0)) return;
            if (FreeSpace(Info.Inventory) == 0) return;
            byte Slot = (byte)Envir.Random.Next(Mine.TotalSlots);
            for (int i = 0; i < Mine.Drops.Count; i++)
            {
                MineDrop Drop = Mine.Drops[i];
                if ((Drop.MinSlot <= Slot) && (Drop.MaxSlot >= Slot) && (Drop.Item != null))
                {
                    var info = Envir.GetItemInfo(Drop.Item.Index);

                    UserItem item = Envir.CreateDropItem(info);
                    if (item.Info.Type == ItemType.Ore)
                    {
                        item.CurrentDura = (ushort)Math.Min(ushort.MaxValue, (Drop.MinDura + Envir.Random.Next(Math.Max(0, Drop.MaxDura - Drop.MinDura))) * 1000);
                        if ((Drop.BonusChance > 0) && (Envir.Random.Next(100) <= Drop.BonusChance))
                            item.CurrentDura = (ushort)Math.Min(ushort.MaxValue, item.CurrentDura + (Envir.Random.Next(Drop.MaxBonusDura) * 1000));
                    }

                    if (CheckGroupQuestItem(item)) continue;

                    if (CanGainItem(item))
                    {
                        GainItem(item);
                        Report.ItemChanged(item, item.Count, 2);
                    }
                    return;
                }
            }
        }
        public virtual bool CheckGroupQuestItem(UserItem item, bool gainItem = true)
        {
            return false;
        }
        protected bool TryLuckWeapon()
        {
            var item = Info.Equipment[(int)EquipmentSlot.Weapon];

            if (item == null || item.AddedStats[Stat.Luck] >= 7)
                return false;

            if (item.Info.Bind.HasFlag(BindMode.DontUpgrade))
                return false;

            if (item.RentalInformation != null && item.RentalInformation.BindingFlags.HasFlag(BindMode.DontUpgrade))
                return false;

            string message = String.Empty;
            ChatType chatType;

            if (item.AddedStats[Stat.Luck] > (Settings.MaxLuck * -1) && Envir.Random.Next(20) == 0)
            {
                BaseStats[Stat.Luck]--;
                item.AddedStats[Stat.Luck]--;
                RebuildEffectiveStats();
                Enqueue(new S.RefreshItem { Item = item });

                message = GameLanguage.Get("Curse dwells within your weapon.");
                chatType = ChatType.System;
                
            }
            else if (item.AddedStats[Stat.Luck] <= 0 || Envir.Random.Next(10 * item.GetTotal(Stat.Luck)) == 0)
            {
                BaseStats[Stat.Luck]++;
                item.AddedStats[Stat.Luck]++;
                RebuildEffectiveStats();
                Enqueue(new S.RefreshItem { Item = item });

                message = GameLanguage.Get("Luck dwells within your weapon.");
                chatType = ChatType.Hint;
            }
            else
            {
                message = GameLanguage.Get("No effect.");
                chatType = ChatType.Hint;
            }

            if (this is HeroObject hero)
            {
                if (message == GameLanguage.Get("Curse dwells within your weapon.") ||
                    message == GameLanguage.Get("Luck dwells within your weapon."))
                {
                    hero.Owner.Enqueue(new S.RefreshItem { Item = item });
                }

                hero.Owner.ReceiveChat(GameLanguage.Get("[Hero: {0}] {1}", hero.Name, message), chatType);
            }
            else
            {
                ReceiveChat(message, chatType);
            }

            return true;
        }
        protected bool CanUseItem(UserItem item)
        {
            if (item == null || item.Count < 1 ||
                (item.RentalInformation != null && item.RentalInformation.BindingFlags != BindMode.None)) return false;

            switch (Gender)
            {
                case MirGender.Male:
                    if (!item.Info.RequiredGender.HasFlag(RequiredGender.Male))
                    {
                        ReceiveChat(GameLanguage.Get("You are not Female."), ChatType.System);
                        return false;
                    }
                    break;
                case MirGender.Female:
                    if (!item.Info.RequiredGender.HasFlag(RequiredGender.Female))
                    {
                        ReceiveChat(GameLanguage.Get("You are not Male."), ChatType.System);
                        return false;
                    }
                    break;
            }

            switch (Class)
            {
                case MirClass.Warrior:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Warrior))
                    {
                        ReceiveChat(GameLanguage.Get("Warriors cannot use this item."), ChatType.System);
                        return false;
                    }
                    break;
                case MirClass.Wizard:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Wizard))
                    {
                        ReceiveChat(GameLanguage.Get("Wizards cannot use this item."), ChatType.System);
                        return false;
                    }
                    break;
                case MirClass.Taoist:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Taoist))
                    {
                        ReceiveChat(GameLanguage.Get("Taoists cannot use this item."), ChatType.System);
                        return false;
                    }
                    break;
                case MirClass.Assassin:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Assassin))
                    {
                        ReceiveChat(GameLanguage.Get("Assassins cannot use this item."), ChatType.System);
                        return false;
                    }
                    break;
                case MirClass.Archer:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Archer))
                    {
                        ReceiveChat(GameLanguage.Get("Archers cannot use this item."), ChatType.System);
                        return false;
                    } 
                    break;
            }

            switch (item.Info.RequiredType)
            {
                case RequiredType.Level:
                    if (Level < item.Info.RequiredAmount)
                    {
                        ReceiveChat(GameLanguage.Get("You are not a high enough level."), ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MaxAC:
                    if (BaseStats[Stat.MaxAC] < item.Info.RequiredAmount)
                    {
                        ReceiveChat(GameLanguage.Get("You do not have enough AC."), ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MaxMAC:
                    if (BaseStats[Stat.MaxMAC] < item.Info.RequiredAmount)
                    {
                        ReceiveChat(GameLanguage.Get("You do not have enough MAC."), ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MaxDC:
                    if (Stats[Stat.MaxDC] < item.Info.RequiredAmount)
                    {
                        ReceiveChat(GameLanguage.Get("You do not have enough DC."), ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MaxMC:
                    if (Stats[Stat.MaxMC] < item.Info.RequiredAmount)
                    {
                        ReceiveChat(GameLanguage.Get("You do not have enough MC."), ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MaxSC:
                    if (Stats[Stat.MaxSC] < item.Info.RequiredAmount)
                    {
                        ReceiveChat(GameLanguage.Get("You do not have enough SC."), ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MaxLevel:
                    if (Level > item.Info.RequiredAmount)
                    {
                        ReceiveChat(GameLanguage.Get("You have exceeded the maximum level."), ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MinAC:
                    if (BaseStats[Stat.MinAC] < item.Info.RequiredAmount)
                    {
                        ReceiveChat(GameLanguage.Get("You do not have enough Base AC."), ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MinMAC:
                    if (BaseStats[Stat.MinMAC] < item.Info.RequiredAmount)
                    {
                        ReceiveChat(GameLanguage.Get("You do not have enough Base MAC."), ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MinDC:
                    if (Stats[Stat.MinDC] < item.Info.RequiredAmount)
                    {
                        ReceiveChat(GameLanguage.Get("You do not have enough Base DC."), ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MinMC:
                    if (Stats[Stat.MinMC] < item.Info.RequiredAmount)
                    {
                        ReceiveChat(GameLanguage.Get("You do not have enough Base MC."), ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MinSC:
                    if (Stats[Stat.MinSC] < item.Info.RequiredAmount)
                    {
                        ReceiveChat(GameLanguage.Get("You do not have enough Base SC."), ChatType.System);
                        return false;
                    }
                    break;
            }

            switch (item.Info.Type)
            {
                case ItemType.Scroll:
                    switch (item.Info.Shape)
                    {
                        case 0:
                            if (CurrentMap.Info.NoEscape)
                            {
                                ReceiveChat(GameLanguage.Get("You cannot use Dungeon Escapes here"), ChatType.System);
                                return false;
                            }
                            break;
                        case 1:
                            if (CurrentMap.Info.NoTownTeleport)
                            {
                                ReceiveChat(GameLanguage.Get("You cannot use Town Teleports here"), ChatType.System);
                                return false;
                            }
                            break;
                        case 2:
                            if (CurrentMap.Info.NoRandom)
                            {
                                ReceiveChat(GameLanguage.Get("You cannot use Random Teleports here"), ChatType.System);
                                return false;
                            }
                            break;
                        case 6:
                            if (!Dead)
                            {
                                ReceiveChat(GameLanguage.Get("You cannot use Resurrection Scrolls whilst alive"), ChatType.Hint);
                                return false;
                            }
                            break;
                        case 10:
                            {
                                int skillId = item.Info.Effect;

                                if (MyGuild == null)
                                {
                                    ReceiveChat(GameLanguage.Get("You must be in a guild to use this skill"), ChatType.Hint);
                                    return false;
                                }
                                if (MyGuildRank != MyGuild.Ranks[0])
                                {
                                    ReceiveChat(GameLanguage.Get("You must be the guild leader to use this skill"), ChatType.Hint);
                                    return false;
                                }
                                GuildBuffInfo buffInfo = Envir.FindGuildBuffInfo(skillId);

                                if (buffInfo == null) return false;

                                if (MyGuild.BuffList.Any(e => e.Info.Id == skillId))
                                {
                                    ReceiveChat(GameLanguage.Get("Your guild already has this skill"), ChatType.Hint);
                                    return false;
                                }
                            }
                            break;
                    }
                    break;
                case ItemType.Potion:
                    if (CurrentMap.Info.NoDrug)
                    {
                        ReceiveChat(GameLanguage.Get("You cannot use Potions here"), ChatType.System);
                        return false;
                    }
                    break;

                case ItemType.Book:
                    if (Info.Magics.Any(t => t.Spell == (Spell)item.Info.Shape))
                    {
                        return false;
                    }
                    break;
                case ItemType.Saddle:
                case ItemType.Ribbon:
                case ItemType.Bells:
                case ItemType.Mask:
                case ItemType.Reins:
                    if (Info.Equipment[(int)EquipmentSlot.Mount] == null)
                    {
                        ReceiveChat(GameLanguage.Get("Can only be used with a mount"), ChatType.System);
                        return false;
                    }
                    break;
                case ItemType.Hook:
                case ItemType.Float:
                case ItemType.Bait:
                case ItemType.Finder:
                case ItemType.Reel:
                    if (Info.Equipment[(int)EquipmentSlot.Weapon] == null || !Info.Equipment[(int)EquipmentSlot.Weapon].Info.IsFishingRod)
                    {
                        ReceiveChat(GameLanguage.Get("Can only be used with a fishing rod"), ChatType.System);
                        return false;
                    }
                    break;
                case ItemType.Socket:
                    break;
                case ItemType.Pets:
                    switch (item.Info.Shape)
                    {
                        case 20://mirror rename creature
                            if (Info.IntelligentCreatures.Count == 0) return false;
                            break;
                        case 21://creature stone
                            break;
                        case 22://nuts maintain food levels
                            if (!CreatureSummoned)
                            {
                                ReceiveChat(GameLanguage.Get("Can only be used with a creature summoned"), ChatType.System);
                                return false;
                            }
                            break;
                        case 23://basic creature food
                            if (!CreatureSummoned)
                            {
                                ReceiveChat(GameLanguage.Get("Can only be used with a creature summoned"), ChatType.System);
                                return false;
                            }
                            else
                            {
                                for (int i = 0; i < Pets.Count; i++)
                                {
                                    if (Pets[i].Race != ObjectType.Creature) continue;

                                    var pet = (IntelligentCreatureObject)Pets[i];
                                    if (pet.PetType != SummonedCreatureType) continue;
                                    if (pet.Fullness > 9900)
                                    {
                                        ReceiveChat(GameLanguage.Get("{0} is not hungry", pet.Name), ChatType.System);
                                        return false;
                                    }
                                    return true;
                                }
                                return false;
                            }
                        case 24://wonderpill vitalize creature
                            if (!CreatureSummoned)
                            {
                                ReceiveChat(GameLanguage.Get("Can only be used with a creature summoned"), ChatType.System);
                                return false;
                            }
                            else
                            {
                                for (int i = 0; i < Pets.Count; i++)
                                {
                                    if (Pets[i].Race != ObjectType.Creature) continue;

                                    var pet = (IntelligentCreatureObject)Pets[i];
                                    if (pet.PetType != SummonedCreatureType) continue;
                                    if (pet.Fullness > 0)
                                    {
                                        ReceiveChat(GameLanguage.Get("{0} does not need to be vitalized", pet.Name), ChatType.System);
                                        return false;
                                    }
                                    return true;
                                }
                                return false;
                            }
                        case 25://Strongbox
                            break;
                        case 26://Wonderdrugs
                            break;
                        case 27://Fortunecookies
                            break;
                    }
                    break;
            }

            if (RidingMount && item.Info.Type != ItemType.Scroll && item.Info.Type != ItemType.Potion)
            {
                return false;
            }

            return true;
        }
        public virtual void UseItem(ulong id) { }
        protected void ConsumeItem(UserItem item, byte cost)
        {
            item.Count -= cost;
            Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = cost });

            if (item.Count != 0) return;

            for (int i = 0; i < Info.Equipment.Length; i++)
            {
                if (Info.Equipment[i] != null && Info.Equipment[i].Slots.Length > 0)
                {
                    for (int j = 0; j < Info.Equipment[i].Slots.Length; j++)
                    {
                        if (Info.Equipment[i].Slots[j] != item) continue;
                        Info.Equipment[i].Slots[j] = null;
                        return;
                    }
                }

                if (Info.Equipment[i] != item) continue;
                Info.Equipment[i] = null;

                return;
            }

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                if (Info.Inventory[i] != item) continue;
                Info.Inventory[i] = null;
                return;
            }

            //Item not found
        }
        protected bool DropItem(UserItem item, int range = 1, bool DeathDrop = false)
        {
            ItemObject ob = new ItemObject(this, item, DeathDrop);

            if (!ob.Drop(range)) return false;

            if (item.Info.Type == ItemType.Meat)
                item.CurrentDura = (ushort)Math.Max(0, item.CurrentDura - 2000);

            return true;
        }
        protected void DeathDrop(MapObject killer)
        {
            var pkbodydrop = true;

            if (CurrentMap.Info.NoDropPlayer && (Race == ObjectType.Player || Race == ObjectType.Hero))
                return;

            if ((killer == null) || ((pkbodydrop) || (killer.Race != ObjectType.Player) || killer.Race != ObjectType.Hero))
            {
                for (var i = 0; i < Info.Equipment.Length; i++)
                {
                    var item = Info.Equipment[i];

                    if (item == null)
                        continue;

                    if (item.Info.Bind.HasFlag(BindMode.DontDeathdrop))
                        continue;

                    // TODO: Check this.
                    if (item.WeddingRing != -1 && Info.Equipment[(int)EquipmentSlot.RingL].UniqueID == item.UniqueID)
                        continue;

                    if (item.SealedInfo != null && item.SealedInfo.ExpiryDate > Envir.Now)
                        continue;

                    if (((killer == null) || ((killer != null) && ((killer.Race != ObjectType.Player) || (killer.Race != ObjectType.Hero)))))
                    {
                        if (item.Info.Bind.HasFlag(BindMode.BreakOnDeath))
                        {
                            Info.Equipment[i] = null;
                            Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                            ReceiveChat(GameLanguage.Get("Your {0} shattered upon death.", item.FriendlyName), ChatType.System2);
                            Report?.ItemChanged(item, item.Count, 1);
                        }
                    }
                    if (ItemSets.Any(set => set.Set == ItemSet.Spirit && !set.SetComplete))
                    {
                        if (item.Info.Set == ItemSet.Spirit)
                        {
                            Info.Equipment[i] = null;
                            Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                            Report?.ItemChanged(item, item.Count, 1);
                        }
                    }

                    if (item.Count > 1)
                    {
                        var percent = Envir.RandomomRange(10, 8);

                        if (percent == 0)
                            continue;

                        var count = (ushort)Math.Ceiling(item.Count / 10F * percent);

                        if (count > item.Count)
                            throw new ArgumentOutOfRangeException();

                        var temp2 = Envir.CreateFreshItem(item.Info);
                        temp2.Count = count;

                        if (!DropItem(temp2, Settings.DropRange, true))
                            continue;

                        if (count == item.Count)
                            Info.Equipment[i] = null;

                        Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = count });
                        item.Count -= count;

                        Report?.ItemChanged(item, count, 1);
                    }
                    else if (Envir.Random.Next(30) == 0)
                    {
                        if (Envir.ReturnRentalItem(item, item.RentalInformation?.OwnerName, Info))
                        {
                            Info.Equipment[i] = null;
                            Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                            ReceiveChat(GameLanguage.Get("You died and {0} has been returned to it's owner.", item.Info.FriendlyName), ChatType.Hint);
                            Report?.ItemMailed(item, 1, 1);

                            continue;
                        }

                        if (!DropItem(item, Settings.DropRange, true))
                        {
                            continue;
                        }

                        if (item.Info.GlobalDropNotify)
                        {
                            foreach (var player in Envir.Players)
                            {
                                player.ReceiveChat(GameLanguage.Get("{0} has dropped {1}.", Name, item.FriendlyName), ChatType.System2);
                            }
                        }

                        Info.Equipment[i] = null;
                        Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                        Report?.ItemChanged(item, item.Count, 1);
                    }
                }

            }

            for (var i = 0; i < Info.Inventory.Length; i++)
            {
                var item = Info.Inventory[i];

                if (item == null)
                    continue;

                if (item.Info.Bind.HasFlag(BindMode.DontDeathdrop))
                    continue;

                if (item.WeddingRing != -1)
                    continue;

                if (item.SealedInfo != null && item.SealedInfo.ExpiryDate > Envir.Now)
                    continue;

                if (item.Count > 1)
                {
                    var percent = Envir.RandomomRange(10, 8);

                    if (percent == 0)
                        continue;

                    var count = (ushort)Math.Ceiling(item.Count / 10F * percent);

                    if (count > item.Count)
                        throw new ArgumentOutOfRangeException();

                    var temp2 = Envir.CreateFreshItem(item.Info);
                    temp2.Count = count;

                    if (!DropItem(temp2, Settings.DropRange, true))
                        continue;

                    if (count == item.Count)
                        Info.Inventory[i] = null;

                    Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = count });
                    item.Count -= count;

                    Report?.ItemChanged(item, count, 1);
                }
                else if (Envir.Random.Next(10) == 0)
                {
                    if (Envir.ReturnRentalItem(item, item.RentalInformation?.OwnerName, Info))
                    {
                        Info.Inventory[i] = null;
                        Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                        ReceiveChat(GameLanguage.Get("You died and {0} has been returned to has been returned to it's owner.", item.Info), ChatType.Hint);
                        Report?.ItemMailed(item, 1, 1);

                        continue;
                    }

                    if (!DropItem(item, Settings.DropRange, true))
                        continue;

                    if (item.Info.GlobalDropNotify)
                        foreach (var player in Envir.Players)
                        {
                            player.ReceiveChat(GameLanguage.Get("{0} has dropped {1}.", Name, item.FriendlyName), ChatType.System2);;
                        }

                    Info.Inventory[i] = null;
                    Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                    Report?.ItemChanged(item, item.Count, 1);
                }
            }

            RefreshStats();
        }
        protected static int FreeSpace(IList<UserItem> array)
        {
            int count = 0;

            for (int i = 0; i < array.Count; i++)
                if (array[i] == null) count++;

            return count;
        }
        protected int AddItem(UserItem item)
        {
            if (item.Info.StackSize > 1) //Stackable
            {
                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    UserItem temp = Info.Inventory[i];
                    if (!item.CanStackWith(temp) || temp.Count >= temp.Info.StackSize) continue;

                    if (item.Count + temp.Count <= temp.Info.StackSize)
                    {
                        temp.Count += item.Count;
                        return i;
                    }
                    item.Count -= (ushort)(temp.Info.StackSize - temp.Count);
                    temp.Count = temp.Info.StackSize;
                }
            }

            if (item.Info.Type == ItemType.Potion || item.Info.Type == ItemType.Scroll || (item.Info.Type == ItemType.Script && item.Info.Effect == 1))
            {
                for (int i = PotionBeltMinimum; i < PotionBeltMaximum; i++)
                {
                    if (Info.Inventory[i] != null) continue;
                    Info.Inventory[i] = item;
                    return i;
                }
            }
            else if (item.Info.Type == ItemType.Amulet)
            {
                for (int i = AmuletBeltMinimum; i < AmuletBeltMaximum; i++)
                {
                    if (Info.Inventory[i] != null) continue;
                    Info.Inventory[i] = item;
                    return i;
                }
            }
            else
            {
                for (int i = BeltSize; i < Info.Inventory.Length; i++)
                {
                    if (Info.Inventory[i] != null) continue;
                    Info.Inventory[i] = item;
                    return i;
                }
            }

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                if (Info.Inventory[i] != null) continue;
                Info.Inventory[i] = item;
                return i;
            }

            return -1;
        }
        protected bool CorrectStartItem(ItemInfo info)
        {
            switch (Class)
            {
                case MirClass.Warrior:
                    if (!info.RequiredClass.HasFlag(RequiredClass.Warrior)) return false;
                    break;
                case MirClass.Wizard:
                    if (!info.RequiredClass.HasFlag(RequiredClass.Wizard)) return false;
                    break;
                case MirClass.Taoist:
                    if (!info.RequiredClass.HasFlag(RequiredClass.Taoist)) return false;
                    break;
                case MirClass.Assassin:
                    if (!info.RequiredClass.HasFlag(RequiredClass.Assassin)) return false;
                    break;
                case MirClass.Archer:
                    if (!info.RequiredClass.HasFlag(RequiredClass.Archer)) return false;
                    break;
                default:
                    return false;
            }

            switch (Gender)
            {
                case MirGender.Male:
                    if (!info.RequiredGender.HasFlag(RequiredGender.Male)) return false;
                    break;
                case MirGender.Female:
                    if (!info.RequiredGender.HasFlag(RequiredGender.Female)) return false;
                    break;
                default:
                    return false;
            }

            return true;
        }
        public void CheckItemInfo(ItemInfo info, bool dontLoop = false)
        {
            Connection.CheckItemInfo(info, dontLoop);
        }
        public void CheckItem(UserItem item)
        {
            Connection.CheckItem(item);         
        }
        public void SetLevelEffects()
        {
            LevelEffects = LevelEffects.None;

            if (Info.Flags[990]) LevelEffects |= LevelEffects.Mist;
            if (Info.Flags[991]) LevelEffects |= LevelEffects.RedDragon;
            if (Info.Flags[992]) LevelEffects |= LevelEffects.BlueDragon;
            if (Info.Flags[993]) LevelEffects |= LevelEffects.Rebirth1;
            if (Info.Flags[994]) LevelEffects |= LevelEffects.Rebirth2;
            if (Info.Flags[995]) LevelEffects |= LevelEffects.Rebirth3;
            if (Info.Flags[996]) LevelEffects |= LevelEffects.NewBlue;
            if (Info.Flags[997]) LevelEffects |= LevelEffects.YellowDragon;
            if (Info.Flags[998]) LevelEffects |= LevelEffects.Phoenix;
        }
        public virtual void Revive(int hp, bool effect)
        {
            if (!Dead) return;

            Dead = false;
            SetHP(hp);

            CurrentMap.RemoveObject(this);
            Broadcast(new S.ObjectRemove { ObjectID = ObjectID });

            CurrentMap = CurrentMap;
            CurrentLocation = CurrentLocation;

            CurrentMap.AddObject(this);

            Enqueue(new S.MapChanged
            {
                MapIndex = CurrentMap.Info.Index,
                FileName = CurrentMap.Info.FileName,
                Title = CurrentMap.Info.Title,
                Weather = CurrentMap.Info.WeatherParticles,
                MiniMap = CurrentMap.Info.MiniMap,
                BigMap = CurrentMap.Info.BigMap,
                Lights = CurrentMap.Info.Light,
                Location = CurrentLocation,
                Direction = Direction,
                MapDarkLight = CurrentMap.Info.MapDarkLight,
                Music = CurrentMap.Info.Music
            });

            Enqueue(new S.Revived());
            Broadcast(new S.ObjectRevived { ObjectID = ObjectID, Effect = effect });
        }

        protected override void OnStatMultipliersChanged()
        {
            var stats = new List<Stat>();
            var factors = new List<float>();

            BuildStatMultipliers(stats, factors);

            Enqueue(new S.SelfStatMultipliers
            {
                Stats = stats.ToArray(),
                Factors = factors.ToArray(),
            });
        }

        protected virtual void SendBaseStats()
        {
            Enqueue(new S.BaseStatsInfo { Stats = Settings.ClassBaseStats[(byte)Class] });
        }

        #region Refresh Stats
        public void RefreshStats()
        {
            if (HasUpdatedBaseStats == false)
            {
                SendBaseStats();                
                HasUpdatedBaseStats = true;
                OnStatMultipliersChanged();
            }

            BaseStats.Clear();

            RefreshLevelStats();
            RefreshBagWeight();
            RefreshEquipmentStats();
            RefreshItemSetStats();
            RefreshMirSetStats();
            RefreshSkills();
            RefreshBuffs();
            RefreshGuildBuffs();

            //Add any rate percent changes

            BaseStats[Stat.HP] += (BaseStats[Stat.HP] * BaseStats[Stat.HPRatePercent]) / 100;
            BaseStats[Stat.MP] += (BaseStats[Stat.MP] * BaseStats[Stat.MPRatePercent]) / 100;
            BaseStats[Stat.MaxAC] += (BaseStats[Stat.MaxAC] * BaseStats[Stat.MaxACRatePercent]) / 100;
            BaseStats[Stat.MaxMAC] += (BaseStats[Stat.MaxMAC] * BaseStats[Stat.MaxMACRatePercent]) / 100;

            BaseStats[Stat.MaxDC] += (BaseStats[Stat.MaxDC] * BaseStats[Stat.MaxDCRatePercent]) / 100;
            BaseStats[Stat.MaxMC] += (BaseStats[Stat.MaxMC] * BaseStats[Stat.MaxMCRatePercent]) / 100;
            BaseStats[Stat.MaxSC] += (BaseStats[Stat.MaxSC] * BaseStats[Stat.MaxSCRatePercent]) / 100;
            BaseStats[Stat.AttackSpeed] += (BaseStats[Stat.AttackSpeed] * BaseStats[Stat.AttackSpeedRatePercent]) / 100;

            RefreshStatCaps();

            if (HP > BaseStats[Stat.HP]) SetHP(BaseStats[Stat.HP]);
            if (MP > BaseStats[Stat.MP]) SetMP(BaseStats[Stat.MP]);

            AttackSpeed = 1400 - ((BaseStats[Stat.AttackSpeed] * 60) + Math.Min(370, (Level * 14)));

            if (AttackSpeed < 550) AttackSpeed = 550;

            RefreshPetInheritance();

            RebuildEffectiveStats();
        }

        /// <summary>
        /// Rebuilds the inherited part of every living servant after the owner's own stats changed.
        /// </summary>
        public void RefreshPetInheritance()
        {
            for (int i = 0; i < Pets.Count; i++)
            {
                MonsterObject pet = Pets[i];

                if (pet == null || pet.Dead || pet.Node == null) continue;

                pet.RefreshAll();
            }
        }

        public virtual void RefreshGuildBuffs() { }

        public virtual void RefreshMaxExperience() { }
        protected virtual void RefreshLevelStats()
        {
            RefreshMaxExperience();

            foreach (var stat in Settings.ClassBaseStats[(byte)Class].Stats)
            {
                BaseStats[stat.Type] = stat.Calculate(Class, Level);
            }
        }
        public void RefreshBagWeight()
        {
            CurrentBagWeight = 0;

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                UserItem item = Info.Inventory[i];
                if (item != null)
                {
                    CurrentBagWeight += item.Weight;
                }
            }
        }
        private void RefreshEquipmentStats()
        {
            short OldLooks_Weapon = Looks_Weapon;
            short OldLooks_WeaponEffect = Looks_WeaponEffect;
            short OldLooks_Armour = Looks_Armour;
            short Old_MountType = Mount.MountType;
            byte OldLooks_Wings = Looks_Wings;
            byte OldLight = Light;

            Looks_Armour = 0;
            Looks_Weapon = -1;
            Looks_WeaponEffect = 0;
            Looks_Wings = 0;
            Light = 0;
            CurrentWearWeight = 0;
            CurrentHandWeight = 0;
            Mount.MountType = -1;

            SpecialMode = SpecialItemMode.None;

            FastRun = false;

            BaseStats[Stat.SkillGainMultiplier] = 1;

            var skillsToAdd = new List<string>();
            var skillsToRemove = new List<string> { Settings.HealRing, Settings.FireRing, Settings.BlinkSkill };

            ItemSets.Clear();
            MirSet.Clear();

            for (int i = 0; i < Info.Equipment.Length; i++)
            {
                UserItem temp = Info.Equipment[i];
                if (temp == null) continue;
                ItemInfo realItem = Functions.GetRealItem(temp.Info, Info.Level, Info.Class, Envir.ItemInfoList);

                if (realItem.Type == ItemType.Weapon || realItem.Type == ItemType.Torch)
                    CurrentHandWeight = (int)Math.Min(int.MaxValue, CurrentHandWeight + temp.Weight);
                else
                    CurrentWearWeight = (int)Math.Min(int.MaxValue, CurrentWearWeight + temp.Weight);

                if (temp.CurrentDura == 0 && temp.Info.Durability > 0) continue;

                if (realItem.Type == ItemType.Armour)
                {
                    Looks_Armour = realItem.Shape;
                    Looks_Wings = realItem.Effect;
                }

                if (realItem.Type == ItemType.Weapon)
                {
                    Looks_Weapon = realItem.Shape;
                    Looks_WeaponEffect = realItem.Effect;
                }

                if (realItem.Type == ItemType.Mount)
                {
                    Mount.MountType = realItem.Shape;
                    //RealItem.Effect;
                }

                if (temp.Info.IsFishingRod) continue;

                BaseStats.Add(realItem.Stats);
                BaseStats.Add(temp.AddedStats);

                BaseStats[Stat.MinAC] += temp.Awake.GetAC();
                BaseStats[Stat.MaxAC] += temp.Awake.GetAC();
                BaseStats[Stat.MinMAC] += temp.Awake.GetMAC();
                BaseStats[Stat.MaxMAC] += temp.Awake.GetMAC();

                BaseStats[Stat.MinDC] += temp.Awake.GetDC();
                BaseStats[Stat.MaxDC] += temp.Awake.GetDC();
                BaseStats[Stat.MinMC] += temp.Awake.GetMC();
                BaseStats[Stat.MaxMC] += temp.Awake.GetMC();
                BaseStats[Stat.MinSC] += temp.Awake.GetSC();
                BaseStats[Stat.MaxSC] += temp.Awake.GetSC();

                BaseStats[Stat.HP] += temp.Awake.GetHPMP();
                BaseStats[Stat.MP] += temp.Awake.GetHPMP();

                if (realItem.Light > Light) Light = realItem.Light;
                if (realItem.Unique != SpecialItemMode.None)
                {
                    SpecialMode |= realItem.Unique;

                    if (realItem.Unique.HasFlag(SpecialItemMode.Flame)) skillsToAdd.Add(Settings.FireRing);
                    if (realItem.Unique.HasFlag(SpecialItemMode.Healing)) skillsToAdd.Add(Settings.HealRing);
                    if (realItem.Unique.HasFlag(SpecialItemMode.Blink)) skillsToAdd.Add(Settings.BlinkSkill);
                }

                if (realItem.CanFastRun)
                {
                    FastRun = true;
                }

                RefreshSocketStats(temp, skillsToAdd);

                if (realItem.Set == ItemSet.None) continue;

                ItemSets itemSet = ItemSets.Where(set => set.Set == realItem.Set && !set.Type.Contains(realItem.Type) && !set.SetComplete).FirstOrDefault();

                if (itemSet != null)
                {
                    itemSet.Type.Add(realItem.Type);
                    itemSet.Count++;
                }
                else
                {
                    ItemSets.Add(new ItemSets { Count = 1, Set = realItem.Set, Type = new List<ItemType> { realItem.Type } });
                }

                //Mir Set
                if (realItem.Set == ItemSet.Mir)
                {
                    if (!MirSet.Contains((EquipmentSlot)i))
                    {
                        MirSet.Add((EquipmentSlot)i);
                    }
                }
            }

            AddTempSkills(skillsToAdd);
            RemoveTempSkills(skillsToRemove.Except(skillsToAdd));

            if (SpecialMode.HasFlag(SpecialItemMode.Muscle))
            {
                BaseStats[Stat.BagWeight] = BaseStats[Stat.BagWeight] * 2;
                BaseStats[Stat.WearWeight] = BaseStats[Stat.WearWeight] * 2;
                BaseStats[Stat.HandWeight] = BaseStats[Stat.HandWeight] * 2;
            }

            if ((OldLooks_Armour != Looks_Armour) || (OldLooks_Weapon != Looks_Weapon) || (OldLooks_WeaponEffect != Looks_WeaponEffect) || (OldLooks_Wings != Looks_Wings) || (OldLight != Light))
            {
                UpdateLooks(OldLooks_Weapon);                
            }

            if (Old_MountType != Mount.MountType)
            {
                RefreshMount(false);
            }
        }
        private void RefreshSocketStats(UserItem equipItem, List<string> skillsToAdd)
        {
            if (equipItem == null) return;

            if (equipItem.Info.Type == ItemType.Weapon && equipItem.Info.IsFishingRod)
            {
                return;
            }

            if (equipItem.Info.Type == ItemType.Mount && !RidingMount)
            {
                return;
            }

            for (int j = 0; j < equipItem.Slots.Length; j++)
            {
                UserItem temp = equipItem.Slots[j];
                if (temp == null) continue;

                ItemInfo RealItem = Functions.GetRealItem(temp.Info, Info.Level, Info.Class, Envir.ItemInfoList);

                if (RealItem.Type == ItemType.Weapon || RealItem.Type == ItemType.Torch)
                    CurrentHandWeight = (int)Math.Min(int.MaxValue, CurrentHandWeight + temp.Weight);
                else
                    CurrentWearWeight = (int)Math.Min(int.MaxValue, CurrentWearWeight + temp.Weight);

                if (temp.CurrentDura == 0 && temp.Info.Durability > 0) continue;

                BaseStats.Add(RealItem.Stats);
                BaseStats.Add(temp.AddedStats);

                if (RealItem.Light > Light) Light = RealItem.Light;
                if (RealItem.Unique != SpecialItemMode.None)
                {
                    SpecialMode |= RealItem.Unique;

                    if (RealItem.Unique.HasFlag(SpecialItemMode.Skill)) BaseStats[Stat.SkillGainMultiplier] = 3;

                    if (RealItem.Unique.HasFlag(SpecialItemMode.Flame)) skillsToAdd.Add(Settings.FireRing);
                    if (RealItem.Unique.HasFlag(SpecialItemMode.Healing)) skillsToAdd.Add(Settings.HealRing);
                    if (RealItem.Unique.HasFlag(SpecialItemMode.Blink)) skillsToAdd.Add(Settings.BlinkSkill);
                }
            }

            //TODO - Add Socket bonuses
        }
        private void RefreshItemSetStats()
        {
            bool hasSmashSetBonus = false;     // Flag for Smash set AttackSpeed bonus
            bool hasPuritySetBonus = false;    // Flag for Purity set Holy bonus
            bool hasHwanDevilSetBonus = false; // Flag for HwanDevil set Weight bonuses

            foreach (var s in ItemSets)
            {
                if ((s.Set == ItemSet.Smash) && (s.Type.Contains(ItemType.Ring)) && (s.Type.Contains(ItemType.Bracelet)))
                {
                    if (!hasSmashSetBonus)
                    {
                        BaseStats[Stat.AttackSpeed] += 2;
                        hasSmashSetBonus = true;
                    }
                }

                if ((s.Set == ItemSet.Purity) && (s.Type.Contains(ItemType.Ring)) && (s.Type.Contains(ItemType.Bracelet)))
                {
                    if (!hasPuritySetBonus)
                    {
                        BaseStats[Stat.Holy] += 3;
                        hasPuritySetBonus = true;
                    }
                }

                if ((s.Set == ItemSet.HwanDevil) && (s.Type.Contains(ItemType.Ring)) && (s.Type.Contains(ItemType.Bracelet)))
                {
                    if (!hasHwanDevilSetBonus)
                    {
                        BaseStats[Stat.WearWeight] += 5;
                        BaseStats[Stat.BagWeight] += 20;
                        hasHwanDevilSetBonus = true;
                    }
                }
                if ((s.Set == ItemSet.DarkGhost) && (s.Type.Contains(ItemType.Necklace)) && (s.Type.Contains(ItemType.Bracelet)))
                {
                    BaseStats[Stat.HP] += 25;
                }

                if (!s.SetComplete) continue;

                switch (s.Set)
                {
                    case ItemSet.Mundane:
                        BaseStats[Stat.HP] += 50;
                        break;
                    case ItemSet.NokChi:
                        BaseStats[Stat.MP] += 50;
                        break;
                    case ItemSet.TaoProtect:
                        BaseStats[Stat.HP] += 30;
                        BaseStats[Stat.MP] += 30;
                        break;
                    case ItemSet.RedOrchid:
                        BaseStats[Stat.Accuracy] += 2;
                        break;
                    case ItemSet.RedFlower:
                        BaseStats[Stat.HP] += 50;
                        BaseStats[Stat.MP] -= 50;
                        break;
                    case ItemSet.Smash:
                        BaseStats[Stat.MinDC] += 1;
                        BaseStats[Stat.MaxDC] += 3;
                        break;
                    case ItemSet.HwanDevil:
                        BaseStats[Stat.MinMC] += 1;
                        BaseStats[Stat.MaxMC] += 2;
                        break;
                    case ItemSet.Purity:
                        BaseStats[Stat.MinSC] += 1;
                        BaseStats[Stat.MaxSC] += 2;
                        break;
                    case ItemSet.FiveString:
                        BaseStats[Stat.HP] += (int)(((double)BaseStats[Stat.HP] / 100) * 30);
                        BaseStats[Stat.MinAC] += 2;
                        BaseStats[Stat.MaxAC] += 2;
                        break;
                    case ItemSet.Spirit:
                        BaseStats[Stat.MinDC] += 2;
                        BaseStats[Stat.MaxDC] += 5;
                        BaseStats[Stat.AttackSpeed] += 2;
                        break;
                    case ItemSet.Bone:
                        BaseStats[Stat.MaxAC] += 2;
                        BaseStats[Stat.MaxMC] += 1;
                        BaseStats[Stat.MaxSC] += 1;
                        break;
                    case ItemSet.Bug:
                        BaseStats[Stat.MaxDC] += 1;
                        BaseStats[Stat.MaxMC] += 1;
                        BaseStats[Stat.MaxSC] += 1;
                        BaseStats[Stat.MaxMAC] += 1;
                        BaseStats[Stat.PoisonResist] += 1;
                        break;
                    case ItemSet.WhiteGold:
                        BaseStats[Stat.MaxDC] += 2;
                        BaseStats[Stat.MaxAC] += 2;
                        break;
                    case ItemSet.WhiteGoldH:
                        BaseStats[Stat.MaxDC] += 3;
                        BaseStats[Stat.HP] += 30;
                        BaseStats[Stat.AttackSpeed] += 2;
                        break;
                    case ItemSet.RedJade:
                        BaseStats[Stat.MaxMC] += 2;
                        BaseStats[Stat.MaxMAC] += 2;
                        break;
                    case ItemSet.RedJadeH:
                        BaseStats[Stat.MaxMC] += 2;
                        BaseStats[Stat.MP] += 40;
                        BaseStats[Stat.Agility] += 2;
                        break;
                    case ItemSet.Nephrite:
                        BaseStats[Stat.MaxSC] += 2;
                        BaseStats[Stat.MaxAC] += 1;
                        BaseStats[Stat.MaxMAC] += 1;
                        break;
                    case ItemSet.NephriteH:
                        BaseStats[Stat.MaxSC] += 2;
                        BaseStats[Stat.HP] += 15;
                        BaseStats[Stat.MP] += 20;
                        BaseStats[Stat.Holy] += 1;
                        BaseStats[Stat.Accuracy] += 1;
                        break;
                    case ItemSet.Whisker1:
                        BaseStats[Stat.MaxDC] += 1;
                        BaseStats[Stat.BagWeight] += 25;
                        break;
                    case ItemSet.Whisker2:
                        BaseStats[Stat.MaxMC] += 1;
                        BaseStats[Stat.BagWeight] += 17;
                        break;
                    case ItemSet.Whisker3:
                        BaseStats[Stat.MaxSC] += 1;
                        BaseStats[Stat.BagWeight] += 17;
                        break;
                    case ItemSet.Whisker4:
                        BaseStats[Stat.MaxDC] += 1;
                        BaseStats[Stat.BagWeight] += 20;
                        break;
                    case ItemSet.Whisker5:
                        BaseStats[Stat.MaxDC] += 1;
                        BaseStats[Stat.BagWeight] += 17;
                        break;
                    case ItemSet.Hyeolryong:
                        BaseStats[Stat.MaxSC] += 2;
                        BaseStats[Stat.HP] += 15;
                        BaseStats[Stat.MP] += 20;
                        BaseStats[Stat.Holy] += 1;
                        BaseStats[Stat.Accuracy] += 1;
                        break;
                    case ItemSet.Monitor:
                        BaseStats[Stat.MagicResist] += 1;
                        BaseStats[Stat.PoisonResist] += 1;
                        break;
                    case ItemSet.Oppressive:
                        BaseStats[Stat.MaxAC] += 1;
                        BaseStats[Stat.Agility] += 1;
                        break;
                    case ItemSet.BlueFrost:
                        BaseStats[Stat.MinDC] += 1;
                        BaseStats[Stat.MaxDC] += 1;
                        BaseStats[Stat.MinMC] += 1;
                        BaseStats[Stat.MaxMC] += 1;
                        BaseStats[Stat.HandWeight] += 1;
                        BaseStats[Stat.WearWeight] += 2;
                        break;
                    case ItemSet.BlueFrostH:
                        BaseStats[Stat.MinDC] += 1;
                        BaseStats[Stat.MaxDC] += 2;
                        BaseStats[Stat.MaxMC] += 2;
                        BaseStats[Stat.Accuracy] += 1;
                        BaseStats[Stat.HP] += 50;
                        break;
                    case ItemSet.DarkGhost:
                        BaseStats[Stat.MP] += 25;
                        BaseStats[Stat.AttackSpeed] += 2;
                        break;
                }
            }
        }
        private void RefreshMirSetStats()
        {
            if (MirSet.Count() == 10)
            {
                BaseStats[Stat.MaxAC] += 1;
                BaseStats[Stat.MaxMAC] += 1;
                BaseStats[Stat.BagWeight] += 70;
                BaseStats[Stat.Luck] += 2;
                BaseStats[Stat.AttackSpeed] += 2;
                BaseStats[Stat.HP] += 70;
                BaseStats[Stat.MP] += 80;
                BaseStats[Stat.MagicResist] += 6;
                BaseStats[Stat.PoisonResist] += 6;
            }

            if (MirSet.Contains(EquipmentSlot.RingL) && MirSet.Contains(EquipmentSlot.RingR))
            {
                BaseStats[Stat.MaxMAC] += 1;
                BaseStats[Stat.MaxAC] += 1;
            }
            if (MirSet.Contains(EquipmentSlot.BraceletL) && MirSet.Contains(EquipmentSlot.BraceletR))
            {
                BaseStats[Stat.MinAC] += 1;
                BaseStats[Stat.MinMAC] += 1;
            }
            if ((MirSet.Contains(EquipmentSlot.RingL) | MirSet.Contains(EquipmentSlot.RingR)) && (MirSet.Contains(EquipmentSlot.BraceletL) | MirSet.Contains(EquipmentSlot.BraceletR)) && MirSet.Contains(EquipmentSlot.Necklace))
            {
                BaseStats[Stat.MaxMAC] += 1;
                BaseStats[Stat.MaxAC] += 1;
                BaseStats[Stat.BagWeight] += 30;
                BaseStats[Stat.WearWeight] += 17;
            }
            if (MirSet.Contains(EquipmentSlot.RingL) && MirSet.Contains(EquipmentSlot.RingR) && MirSet.Contains(EquipmentSlot.BraceletL) && MirSet.Contains(EquipmentSlot.BraceletR) && MirSet.Contains(EquipmentSlot.Necklace))
            {
                BaseStats[Stat.MaxMAC] += 1;
                BaseStats[Stat.MaxAC] += 1;
                BaseStats[Stat.BagWeight] += 20;
                BaseStats[Stat.WearWeight] += 10;
            }
            if (MirSet.Contains(EquipmentSlot.Armour) && MirSet.Contains(EquipmentSlot.Helmet) && MirSet.Contains(EquipmentSlot.Weapon))
            {
                BaseStats[Stat.MaxDC] += 2;
                BaseStats[Stat.MaxMC] += 1;
                BaseStats[Stat.MaxSC] += 1;
                BaseStats[Stat.Agility] += 1;
            }
            if (MirSet.Contains(EquipmentSlot.Armour) && MirSet.Contains(EquipmentSlot.Boots) && MirSet.Contains(EquipmentSlot.Belt))
            {
                BaseStats[Stat.MaxDC] += 1;
                BaseStats[Stat.MaxMC] += 1;
                BaseStats[Stat.MaxSC] += 1;
                BaseStats[Stat.HandWeight] += 17;
            }
            if (MirSet.Contains(EquipmentSlot.Armour) && MirSet.Contains(EquipmentSlot.Boots) && MirSet.Contains(EquipmentSlot.Belt) && MirSet.Contains(EquipmentSlot.Helmet) && MirSet.Contains(EquipmentSlot.Weapon))
            {
                BaseStats[Stat.MinDC] += 1;
                BaseStats[Stat.MaxDC] += 1;
                BaseStats[Stat.MinMC] += 1;
                BaseStats[Stat.MaxMC] += 1;
                BaseStats[Stat.MinSC] += 1;
                BaseStats[Stat.MaxSC] += 1;
                BaseStats[Stat.HandWeight] += 17;
            }
        }
        public void RefreshStatCaps()
        {
            foreach (var cap in Settings.ClassBaseStats[(byte)Class].Caps.Values)
            {
                BaseStats[cap.Key] = Math.Min(cap.Value, BaseStats[cap.Key]);
            }

            BaseStats[Stat.HP] = Math.Max(0, BaseStats[Stat.HP]);
            BaseStats[Stat.MP] = Math.Max(0, BaseStats[Stat.MP]);

            BaseStats[Stat.MinAC] = Math.Max(0, BaseStats[Stat.MinAC]);
            BaseStats[Stat.MaxAC] = Math.Max(0, BaseStats[Stat.MaxAC]);
            BaseStats[Stat.MinMAC] = Math.Max(0, BaseStats[Stat.MinMAC]);
            BaseStats[Stat.MaxMAC] = Math.Max(0, BaseStats[Stat.MaxMAC]);
            BaseStats[Stat.MinDC] = Math.Max(0, BaseStats[Stat.MinDC]);
            BaseStats[Stat.MaxDC] = Math.Max(0, BaseStats[Stat.MaxDC]);
            BaseStats[Stat.MinMC] = Math.Max(0, BaseStats[Stat.MinMC]);
            BaseStats[Stat.MaxMC] = Math.Max(0, BaseStats[Stat.MaxMC]);
            BaseStats[Stat.MinSC] = Math.Max(0, BaseStats[Stat.MinSC]);
            BaseStats[Stat.MaxSC] = Math.Max(0, BaseStats[Stat.MaxSC]);

            BaseStats[Stat.MinDC] = Math.Min(BaseStats[Stat.MinDC], BaseStats[Stat.MaxDC]);
            BaseStats[Stat.MinMC] = Math.Min(BaseStats[Stat.MinMC], BaseStats[Stat.MaxMC]);
            BaseStats[Stat.MinSC] = Math.Min(BaseStats[Stat.MinSC], BaseStats[Stat.MaxSC]);
        }
        #endregion

        private void AddTempSkills(IEnumerable<string> skillsToAdd)
        {
            foreach (var skill in skillsToAdd)
            {
                Spell spelltype;
                bool hasSkill = false;

                if (!Enum.TryParse(skill, out spelltype)) return;

                for (var i = Info.Magics.Count - 1; i >= 0; i--)
                    if (Info.Magics[i].Spell == spelltype) hasSkill = true;

                if (hasSkill) continue;

                var magic = new UserMagic(spelltype) { IsTempSpell = true };
                Info.Magics.Add(magic);
                SendMagicInfo(magic);                
            }
        }
        public virtual void SendMagicInfo(UserMagic magic)
        {
            Enqueue(magic.GetInfo(false));
        }
        public virtual void SendRemoveMagic(int placeId)
        {
            Enqueue(new S.RemoveMagic { PlaceId = placeId });
        }
        private void RemoveTempSkills(IEnumerable<string> skillsToRemove)
        {
            foreach (var skill in skillsToRemove)
            {
                if (!Enum.TryParse(skill, out Spell spelltype)) return;

                for (var i = Info.Magics.Count - 1; i >= 0; i--)
                {
                    if (!Info.Magics[i].IsTempSpell || Info.Magics[i].Spell != spelltype) continue;

                    Info.Magics.RemoveAt(i);
                    SendRemoveMagic(i);
                }
            }
        }
        private void RefreshSkills()
        {
            int[] spiritSwordLvPlus = { 0, 3, 5, 8 };
            int[] slayingLvPlus = { 5, 6, 7, 8 };
            for (int i = 0; i < Info.Magics.Count; i++)
            {
                UserMagic magic = Info.Magics[i];
                switch (magic.Spell)
                {
                    case Spell.Fencing:
                        BaseStats[Stat.Accuracy] += magic.Level * 3;
                        // BaseStats[Stat.MaxAC] += (magic.Level + 1) * 3;
                        break;
                    // case Spell.FatalSword:
                    case Spell.Slaying:
                        BaseStats[Stat.Accuracy] += magic.Level;
                        BaseStats[Stat.MaxDC] += slayingLvPlus[magic.Level];
                        break;
                    case Spell.SpiritSword:
                        BaseStats[Stat.Accuracy] += spiritSwordLvPlus[magic.Level];
                        // BaseStats[Stat.Accuracy] += magic.Level;
                        // BaseStats[Stat.MaxDC] += (int)(BaseStats[Stat.MaxSC] * (magic.Level + 1) * 0.1F);
                        break;
                }
            }
        }
        private void RefreshBuffs()
        {
            short Old_TransformType = TransformType;

            TransformType = -1;

            for (int i = 0; i < Buffs.Count; i++)
            {
                Buff buff = Buffs[i];

                if (buff.Paused) continue;

                BaseStats.Add(buff.Stats);

                if (buff.Values != null && buff.Values.Length > 0)
                {
                    switch (buff.Type)
                    {
                        case BuffType.Transform:
                            TransformType = (short)buff.Values[0];
                            FastRun = true;
                            break;
                    }
                }
            }

            if (Old_TransformType != TransformType)
            {
                Broadcast(new S.TransformUpdate { ObjectID = ObjectID, TransformType = TransformType });
            }
        }
        public void BroadcastColourChange()
        {
            if (CurrentMap == null) return;

            for (int i = CurrentMap.Players.Count - 1; i >= 0; i--)
            {
                PlayerObject player = CurrentMap.Players[i];
                if (player == this) continue;

                if (Functions.InRange(CurrentLocation, player.CurrentLocation, Globals.DataRange))
                    player.Enqueue(new S.ObjectColourChanged { ObjectID = ObjectID, NameColour = player.GetNameColour(this) });
            }
        }
        public virtual void GainExp(uint amount) { }
        public int ReduceExp(uint amount, uint targetLevel)
        {
            int expPoint;

            if (Level < targetLevel + 10 || !Settings.ExpMobLevelDifference)
            {
                expPoint = (int)amount;
            }
            else
            {
                expPoint = (int)amount - (int)Math.Round(Math.Max(amount / 15, 1) * ((double)Level - (targetLevel + 10)));
            }

            if (expPoint <= 0) expPoint = 1;

            return expPoint;
        }
        public override void BroadcastInfo()
        {
            Packet p;
            if (CurrentMap == null) return;

            for (int i = CurrentMap.Players.Count - 1; i >= 0; i--)
            {
                PlayerObject player = CurrentMap.Players[i];
                if (player == this) continue;

                if (Functions.InRange(CurrentLocation, player.CurrentLocation, Globals.DataRange))
                {
                    p = GetInfoEx(player);
                    if (p != null)
                        player.Enqueue(p);
                }
            }
        }
        public virtual bool CheckMovement(Point location)
        {
            return false;
        }
        public bool Walk(MirDirection dir)
        {
            if (!CanMove || !CanWalk)
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return false;
            }

            Point location = Functions.PointMove(CurrentLocation, dir, 1);

            if (!CurrentMap.ValidPoint(location))
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return false;
            }

            if (!CurrentMap.CheckDoorOpen(location))
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return false;
            }


            Cell cell = CurrentMap.GetCell(location);
            if (cell.Objects != null)
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    MapObject ob = cell.Objects[i];

                    if (ob.Race == ObjectType.Merchant && Race == ObjectType.Player)
                    {
                        NPCObject NPC = (NPCObject)ob;
                        if (!NPC.Visible || !NPC.VisibleLog[Info.Index]) continue;
                    }
                    else
                    {
                        if (!ob.Blocking || (CheckCellTime && ob.CellTime >= Envir.Time)) continue;
                    }

                    Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                    return false;
                }
            }

            if (HasBuff(BuffType.Concentration, out Buff concentration))
            {
                concentration.Set("InterruptTime", Envir.Time + (Settings.Second * 3));

                if (!concentration.Get<bool>("Interrupted"))
                {
                    concentration.Set("Interrupted", true);
                    UpdateConcentration(true, true);
                }
            }

            if (Hidden)
            {
                RemoveBuff(BuffType.Hiding);
            }

            Direction = dir;
            if (CheckMovement(location)) return false;

            CurrentMap.GetCell(CurrentLocation).Remove(this);

            RemoveObjects(dir, 1);

            CurrentLocation = location;
            var dstCell = CurrentMap.GetCell(CurrentLocation);
            dstCell.Add(this);

            AddObjects(dir, 1);

            _stepCounter++;

            SafeZoneInfo szi = CurrentMap.GetSafeZone(CurrentLocation);

            if (szi != null)
            {
                SetBindSafeZone(szi);
                InSafeZone = true;
            }
            else
                InSafeZone = false;

            if (RidingMount) DecreaseMountLoyalty(1);
            Moved();

            CellTime = Envir.Time + 500;
            ActionTime = Envir.Time + GetDelayTime(MoveDelay);          
            
            Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
            Broadcast(new S.ObjectWalk { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
            GetPlayerLocation();

            cell = CurrentMap.GetCell(CurrentLocation);

            if (cell.Objects != null)
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    if (cell.Objects[i].Race != ObjectType.Spell) continue;
                    SpellObject ob = (SpellObject)cell.Objects[i];
                    ob.ProcessSpell(this);
                }
            }

            return true;
        }
        public bool Run(MirDirection dir)
        {
            if (CurrentBagWeight > Stats[Stat.BagWeight])
            {
                Walk(dir);
            }

            var steps = RidingMount || ActiveSwiftFeet && !Sneaking ? 3 : 2;

            if (!CanMove || !CanWalk || !CanRun)
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return false;
            }

            if (HasBuff(BuffType.Concentration, out Buff concentration))
            {
                concentration.Set("InterruptTime", Envir.Time + (Settings.Second * 3));

                if (!concentration.Get<bool>("Interrupted"))
                {
                    concentration.Set("Interrupted", true);
                    UpdateConcentration(true, true);
                }
            }            

            if (Hidden && !Sneaking)
            {
                RemoveBuff(BuffType.Hiding);
                RemoveBuff(BuffType.MoonLight);
                RemoveBuff(BuffType.DarkBody);
            }

            Moved();

            Direction = dir;
            Point location = Functions.PointMove(CurrentLocation, dir, 1);
            for (int j = 1; j <= steps; j++)
            {
                location = Functions.PointMove(CurrentLocation, dir, j);
                if (!CurrentMap.ValidPoint(location))
                {
                    Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                    return false;
                }
                if (!CurrentMap.CheckDoorOpen(location))
                {
                    Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                    return false;
                }
                Cell cell = CurrentMap.GetCell(location);

                if (cell.Objects != null)
                {
                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        MapObject ob = cell.Objects[i];

                        if (ob.Race == ObjectType.Merchant && Race == ObjectType.Player)
                        {
                            NPCObject NPC = (NPCObject)ob;
                            if (!NPC.Visible || !NPC.VisibleLog[Info.Index]) continue;
                        }
                        else
                            if (!ob.Blocking || (CheckCellTime && ob.CellTime >= Envir.Time)) continue;

                        Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                        return false;
                    }
                }
                if (CheckMovement(location)) return false;
            }
            Enqueue(new S.UserLocation { Direction = dir, Location = location });
            Broadcast(new S.ObjectRun { ObjectID = ObjectID, Direction = dir, Location = location });

            if (RidingMount && !Sneaking)
            {
                DecreaseMountLoyalty(2);
            }

            Direction = dir;

            CurrentMap.GetCell(CurrentLocation).Remove(this);
            RemoveObjects(dir, steps);

            Point OldLocation = CurrentLocation;
            CurrentLocation = location;
            CurrentMap.GetCell(CurrentLocation).Add(this);
            AddObjects(dir, steps);


            SafeZoneInfo szi = CurrentMap.GetSafeZone(CurrentLocation);

            if (szi != null)
            {
                SetBindSafeZone(szi);
                InSafeZone = true;
            }
            else
                InSafeZone = false;            

            CellTime = Envir.Time + 500;
            ActionTime = Envir.Time + GetDelayTime(MoveDelay);

            if (!RidingMount)
                _runCounter++;

            if (_runCounter > 10)
            {
                _runCounter -= 8;
                ChangeHP(-1);
            }

            GetPlayerLocation();

            for (int j = 1; j <= steps; j++)
            {
                location = Functions.PointMove(OldLocation, dir, j);
                Cell cell = CurrentMap.GetCell(location);
                if (cell.Objects == null) continue;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    if (cell.Objects[i].Race != ObjectType.Spell) continue;
                    SpellObject ob = (SpellObject)cell.Objects[i];

                    ob.ProcessSpell(this);
                    //break;
                }
            }

            return true;
        }
        protected virtual void Moved()
        {
        }
        public override int Pushed(MapObject pusher, MirDirection dir, int distance)
        {
            int result = 0;
            MirDirection reverse = Functions.ReverseDirection(dir);
            Cell cell;
            for (int i = 0; i < distance; i++)
            {
                Point location = Functions.PointMove(CurrentLocation, dir, 1);

                if (!CurrentMap.ValidPoint(location)) return result;

                cell = CurrentMap.GetCell(location);

                bool stop = false;
                if (cell.Objects != null)
                    for (int c = 0; c < cell.Objects.Count; c++)
                    {
                        MapObject ob = cell.Objects[c];
                        if (!ob.Blocking) continue;
                        stop = true;
                    }
                if (stop) break;

                CurrentMap.GetCell(CurrentLocation).Remove(this);

                Direction = reverse;
                RemoveObjects(dir, 1);
                CurrentLocation = location;
                CurrentMap.GetCell(CurrentLocation).Add(this);
                AddObjects(dir, 1);

                Moved();

                Enqueue(new S.Pushed { Direction = Direction, Location = CurrentLocation });
                Broadcast(new S.ObjectPushed { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
                GetPlayerLocation();
                result++;
            }

            if (result > 0)
            {
                if (HasBuff(BuffType.Concentration, out Buff concentration))
                {
                    concentration.Set("InterruptTime", Envir.Time + (Settings.Second * 3));

                    if (!concentration.Get<bool>("Interrupted"))
                    {
                        concentration.Set("Interrupted", true);
                        UpdateConcentration(true, true);
                    }
                }

                cell = CurrentMap.GetCell(CurrentLocation);

                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    if (cell.Objects[i].Race != ObjectType.Spell) continue;
                    SpellObject ob = (SpellObject)cell.Objects[i];

                    ob.ProcessSpell(this);
                    //break;
                }

                SafeZoneInfo szi = CurrentMap.GetSafeZone(CurrentLocation);

                if (szi != null)
                {
                    SetBindSafeZone(szi);
                    InSafeZone = true;
                }
                else
                    InSafeZone = false;
            }

            ActionTime = Envir.Time + 500;
            return result;
        }

        public void GetPlayerLocation()
        {
            if (GroupMembers == null) return;

            for (int i = 0; i < GroupMembers.Count; i++)
            {
                PlayerObject member = GroupMembers[i];
                
                if (member.CurrentMap.Info.BigMap <= 0) continue;
                  
                member.Enqueue(new S.SendMemberLocation { MemberName = Name, MemberLocation = CurrentLocation });
                Enqueue(new S.SendMemberLocation { MemberName = member.Name, MemberLocation = member.CurrentLocation });
            }
            Enqueue(new S.SendMemberLocation { MemberName = Name, MemberLocation = CurrentLocation });
        }



        /// <summary>
        /// Hands the object this character is fighting to the rest of its side. The player and the
        /// hero are one combat group: the hero assists an owner's attack, and in the focus stance
        /// the owner's pets and hero follow the target. A hero reports to its owner's group, so the
        /// targets it picks reach the pets as well; before, a summon kept standing next to its
        /// owner while the hero fought on.
        /// </summary>
        private void ShareCombatTarget(MapObject target)
        {
            PlayerObject player;
            HeroObject hero;

            if (this is PlayerObject self)
            {
                player = self;
                hero = self.HeroSpawned ? self.Hero : null;

                //The hero helps with an attack its owner started
                if (hero != null && !hero.Dead)
                    hero.AssistOwnerAttack(target);
            }
            else if (this is HeroObject attacker)
            {
                player = attacker.Owner;
                hero = attacker;
            }
            else
            {
                return;
            }

            if (player == null || player.PMode != PetMode.FocusMasterTarget) return;

            for (int i = 0; i < player.Pets.Count; i++)
            {
                MonsterObject pet = player.Pets[i];
                if (pet.Race == ObjectType.Creature) continue;

                pet.Target = target;
            }

            //The hero already focuses this target, and a counter attacking hero reacts on its own
            if (hero == null || hero == this || hero.Dead) return;
            if (player.Info.HeroBehaviour == HeroBehaviour.CounterAttack) return;

            hero.Target = target;
        }

        public void RangeAttack(MirDirection dir, Point location, uint targetID)
        {
            LogTime = Envir.Time + Globals.LogDelay;

            if (Info.Equipment[(int)EquipmentSlot.Weapon] == null) return;
            ItemInfo RealItem = Functions.GetRealItem(Info.Equipment[(int)EquipmentSlot.Weapon].Info, Info.Level, Info.Class, Envir.ItemInfoList);

            if ((RealItem.Shape / Globals.ClassWeaponCount) != 2) return;
            if (Functions.InRange(CurrentLocation, location, Globals.MaxAttackRange) == false) return;

            MapObject target = null;

            if (targetID == ObjectID)
                target = this;
            else if (targetID > 0)
                target = FindObject(targetID, 10);

            if (target != null && target.Dead) return;

            if (target != null && target.Race != ObjectType.Monster && target.Race != ObjectType.Player && target.Race != ObjectType.Hero) return;

            if (target != null && !target.Dead && target.IsAttackTarget(this) && !target.IsFriendlyTarget(this))
            {
                ShareCombatTarget(target);
            }

            Direction = dir;

            Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });

            UserMagic magic;
            Spell spell = Spell.None;
            bool focus = false;

            if (target != null && !CanFly(target.CurrentLocation) && (Info.MentalState != 1))
            {
                target = null;
                targetID = 0;
            }

            if (target != null)
            {
                magic = GetMagic(Spell.Focus);

                if (magic != null && Envir.Random.Next(5) <= magic.Level)
                {
                    focus = true;
                    LevelMagic(magic);
                    spell = Spell.Focus;
                }

                int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);

                int damage = GetRangeAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC], distance);

                damage = ApplyArcherState(damage);

                int chanceToHit = (100 + Settings.RangeAccuracyBonus - ((100 / Globals.MaxAttackRange) * distance)) * (focus ? 2 : 1);

                if (chanceToHit < 0) chanceToHit = 0;

                int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500 + 50; //50 MS per Step

                if (Envir.Random.Next(100) < chanceToHit)
                {
                    if (target.CurrentLocation != location)
                        location = target.CurrentLocation;

                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, target, damage, DefenceType.ACAgility, true);
                    ActionList.Add(action);
                }
                else
                {
                    DelayedAction action = new DelayedAction(DelayedType.DamageIndicator, Envir.Time + delay, target, DamageType.Miss);
                    ActionList.Add(action);
                }
            }
            else
                targetID = 0;

            Enqueue(new S.RangeAttack { TargetID = targetID, Target = location, Spell = spell });
            Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = targetID, Target = location, Spell = spell });

            AttackTime = Envir.Time + AttackSpeed;
            ActionTime = Envir.Time + 550;
            RegenTime = Envir.Time + RegenDelay;
        }
        public void Attack(MirDirection dir, Spell spell)
        {
            LogTime = Envir.Time + Globals.LogDelay;

            bool Mined = false;
            bool MoonLightAttack = false;
            bool DarkBodyAttack = false;

            if (!CanAttack)
            {
                switch (spell)
                {
                    case Spell.Slaying:
                        Slaying = false;
                        break;
                }

                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            if (Hidden)
            {
                for (int i = 0; i < Buffs.Count; i++)
                {
                    switch (Buffs[i].Type)
                    {
                        case BuffType.MoonLight:
                            MoonLightAttack = true;
                            break;
                        case BuffType.DarkBody:
                            DarkBodyAttack = true;
                            break;
                    }
                }

                RemoveBuff(BuffType.MoonLight);
                RemoveBuff(BuffType.DarkBody);
            }

            byte level = 0;
            UserMagic magic;

            if (RidingMount)
            {
                spell = Spell.None;
            }

            switch (spell)
            {
                case Spell.Slaying:
                    if (!Slaying)
                        spell = Spell.None;
                    else
                    {
                        magic = GetMagic(Spell.Slaying);
                        level = magic.Level;
                    }

                    Slaying = false;
                    break;
                case Spell.DoubleSlash:
                    magic = GetMagic(spell);
                    if (magic == null || magic.Info.BaseCost + (magic.Level * magic.Info.LevelCost) > MP)
                    {
                        spell = Spell.None;
                        break;
                    }
                    level = magic.Level;
                    ChangeMP(-(magic.Info.BaseCost + magic.Level * magic.Info.LevelCost));
                    break;
                case Spell.Thrusting:
                case Spell.FlamingSword:
                    magic = GetMagic(spell);
                    if ((magic == null) || (!FlamingSword && (spell == Spell.FlamingSword)))
                    {
                        spell = Spell.None;
                        break;
                    }
                    level = magic.Level;
                    break;
                case Spell.HalfMoon:
                case Spell.CrossHalfMoon:
                    magic = GetMagic(spell);
                    if (magic == null || magic.Info.BaseCost + (magic.Level * magic.Info.LevelCost) > MP)
                    {
                        spell = Spell.None;
                        break;
                    }
                    level = magic.Level;
                    ChangeMP(-(magic.Info.BaseCost + magic.Level * magic.Info.LevelCost));
                    break;
                case Spell.TwinDrakeBlade:
                    magic = GetMagic(spell);
                    if (!TwinDrakeBlade || magic == null || magic.Info.BaseCost + magic.Level * magic.Info.LevelCost > MP)
                    {
                        spell = Spell.None;
                        break;
                    }
                    level = magic.Level;
                    ChangeMP(-(magic.Info.BaseCost + magic.Level * magic.Info.LevelCost));
                    break;
                default:
                    spell = Spell.None;
                    break;
            }


            if (!Slaying)
            {
                magic = GetMagic(Spell.Slaying);

                if (magic != null && Envir.Random.Next(12) <= magic.Level)
                {
                    Slaying = true;
                    Enqueue(new S.SpellToggle { ObjectID = ObjectID, Spell = Spell.Slaying, CanUse = Slaying });
                }
            }

            Direction = dir;

            if (RidingMount) DecreaseMountLoyalty(3);

            Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Spell = spell, Level = level });

            AttackTime = Envir.Time + AttackSpeed;
            ActionTime = Envir.Time + 550;
            RegenTime = Envir.Time + RegenDelay;

            Point target = Functions.PointMove(CurrentLocation, dir, 1);

            //damabeBase = the original damage from your gear (+ bonus from moonlight and darkbody)
            int damageBase = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
            //damageFinal = the damage you're gonna do with skills added
            int damageFinal;

            if (MoonLightAttack || DarkBodyAttack)
            {
                magic = MoonLightAttack ? GetMagic(Spell.MoonLight) : GetMagic(Spell.DarkBody);

                if (magic != null)
                {
                    damageBase += magic.GetPower();
                }
            }

            if (!CurrentMap.ValidPoint(target))
            {
                switch (spell)
                {
                    case Spell.Thrusting:
                        goto Thrusting;
                    case Spell.HalfMoon:
                        goto HalfMoon;
                    case Spell.CrossHalfMoon:
                        goto CrossHalfMoon;
                    case Spell.None:
                        Mined = true;
                        goto Mining;
                }
                return;
            }

            Cell cell = CurrentMap.GetCell(target);

            if (cell.Objects == null)
            {
                switch (spell)
                {
                    case Spell.Thrusting:
                        goto Thrusting;
                    case Spell.HalfMoon:
                        goto HalfMoon;
                    case Spell.CrossHalfMoon:
                        goto CrossHalfMoon;
                }
                return;
            }

            damageFinal = damageBase;//incase we're not using skills
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                MapObject ob = cell.Objects[i];
                if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster && ob.Race != ObjectType.Hero) continue;
                if (!ob.IsAttackTarget(this)) continue;

                if (ob != null && !ob.Dead && ob.IsAttackTarget(this) && !ob.IsFriendlyTarget(this))
                {
                    ShareCombatTarget(ob);
                }

                //Only undead targets
                if (ob.Undead)
                {
                    damageBase = Math.Min(int.MaxValue, damageBase + Stats[Stat.Holy]);
                    damageFinal = damageBase;//incase we're not using skills
                }

                #region FatalSword
                magic = GetMagic(Spell.FatalSword);

                DefenceType defence = DefenceType.ACAgility;

                if (magic != null)
                {
                    if (!FatalSword && Envir.Random.Next(10) == 0)
                        FatalSword = true;

                    if (FatalSword)
                        damageBase = magic.GetDamage(damageBase);
                }
                #endregion

                #region MPEater
                magic = GetMagic(Spell.MPEater);

                if (magic != null)
                {
                    int baseCount = 1 + Stats[Stat.Accuracy] / 2;
                    int maxCount = baseCount + magic.Level * 5;
                    MPEaterCount += Envir.Random.Next(baseCount, maxCount);
                    if (MPEater)
                    {
                        LevelMagic(magic);
                        damageFinal = magic.GetDamage(damageBase);
                        defence = DefenceType.ACAgility;

                        S.ObjectEffect p = new S.ObjectEffect { ObjectID = ob.ObjectID, Effect = SpellEffect.MPEater, EffectType = ObjectID };
                        CurrentMap.Broadcast(p, ob.CurrentLocation);

                        int addMp = 5 * (magic.Level + Stats[Stat.Accuracy] / 4);

                        if (ob.Race == ObjectType.Player)
                        {
                            ((PlayerObject)ob).ChangeMP(-addMp);
                        }

                        ChangeMP(addMp);
                        MPEaterCount = 0;
                        MPEater = false;
                    }
                    else if (!MPEater && 100 <= MPEaterCount) MPEater = true;
                }
                #endregion

                #region Hemorrhage
                magic = GetMagic(Spell.Hemorrhage);

                if (magic != null)
                {
                    HemorrhageAttackCount += Envir.Random.Next(1, 1 + magic.Level * 2);
                    if (Hemorrhage)
                    {
                        damageFinal = magic.GetDamage(damageBase);
                        LevelMagic(magic);
                        S.ObjectEffect ef = new S.ObjectEffect { ObjectID = ob.ObjectID, Effect = SpellEffect.Hemorrhage };

                        CurrentMap.Broadcast(ef, ob.CurrentLocation);

                        if (ob == null || ob.Node == null) continue;

                        long calcDuration = magic.Level * 2 + Stats[Stat.Luck] / 6;

                        ob.ApplyPoison(new Poison
                        {
                            Duration = (calcDuration <= 0) ? 1 : calcDuration,
                            Owner = this,
                            PType = PoisonType.Bleeding,
                            TickSpeed = 1000,
                            Value = Stats[Stat.MaxDC] + 1
                        }, this);

                        ob.OperateTime = 0;
                        HemorrhageAttackCount = 0;
                        Hemorrhage = false;
                    }
                    else if (!Hemorrhage && 55 <= HemorrhageAttackCount) Hemorrhage = true;
                }
                #endregion

                DelayedAction action;
                switch (spell)
                {
                    case Spell.Slaying:
                        magic = GetMagic(Spell.Slaying);
                        damageFinal = magic.GetDamage(damageBase);
                        LevelMagic(magic);
                        break;
                    case Spell.DoubleSlash:
                        magic = GetMagic(Spell.DoubleSlash);
                        damageFinal = magic.GetDamage(damageBase);

                        if (defence == DefenceType.ACAgility) defence = DefenceType.MACAgility;

                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 400, ob, damageFinal, DefenceType.Agility, false);
                        ActionList.Add(action);
                        LevelMagic(magic);
                        break;
                    case Spell.Thrusting:
                        magic = GetMagic(Spell.Thrusting);
                        LevelMagic(magic);
                        break;
                    case Spell.HalfMoon:
                        magic = GetMagic(Spell.HalfMoon);
                        LevelMagic(magic);
                        break;
                    case Spell.CrossHalfMoon:
                        magic = GetMagic(Spell.CrossHalfMoon);
                        LevelMagic(magic);
                        break;
                    case Spell.TwinDrakeBlade:
                        magic = GetMagic(Spell.TwinDrakeBlade);
                        damageFinal = magic.GetDamage(damageBase);
                        TwinDrakeBlade = false;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 400,
                            ob,                     //Object (Target)
                            damageFinal,            //Damage
                            DefenceType.Agility,    //Defence to target
                            false,                  //Damage Weapon
                            magic,                  //Magic
                            true);                  //Final hit
                        ActionList.Add(action);
                        LevelMagic(magic);
                        break;
                    case Spell.FlamingSword:
                        magic = GetMagic(Spell.FlamingSword);
                        damageFinal = magic.GetDamage(damageBase);
                        FlamingSword = false;
                        defence = DefenceType.AC;
                        //action = new DelayedAction(DelayedType.Damage, Envir.Time + 400, ob, damage, DefenceType.Agility, true);
                        //ActionList.Add(action);
                        LevelMagic(magic);
                        break;
                }

                //if (ob.Attacked(this, damage, defence) <= 0) break;
                action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, ob, damageFinal, defence, true);
                ActionList.Add(action);
                break;
            }

        Thrusting:
            if (spell == Spell.Thrusting)
            {
                target = Functions.PointMove(target, dir, 1);

                if (!CurrentMap.ValidPoint(target)) return;

                cell = CurrentMap.GetCell(target);

                if (cell.Objects == null) return;

                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    MapObject ob = cell.Objects[i];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster && ob.Race != ObjectType.Hero) continue;
                    if (!ob.IsAttackTarget(this)) continue;

                    magic = GetMagic(spell);
                    damageFinal = magic.GetDamage(damageBase);
                    ob.Attacked(this, damageFinal,
                        ob is MonsterObject monster && (monster.Info.AI == 49) ? DefenceType.Repulsion : DefenceType.Agility, 
                        false);
                    break;
                }


            }
        HalfMoon:
            if (spell == Spell.HalfMoon)
            {
                dir = Functions.PreviousDir(dir);

                magic = GetMagic(spell);
                damageFinal = magic.GetDamage(damageBase);
                for (int i = 0; i < 4; i++)
                {
                    target = Functions.PointMove(CurrentLocation, dir, 1);
                    dir = Functions.NextDir(dir);
                    if (target == Front) continue;

                    if (!CurrentMap.ValidPoint(target)) continue;

                    cell = CurrentMap.GetCell(target);

                    if (cell.Objects == null) continue;

                    for (int o = 0; o < cell.Objects.Count; o++)
                    {
                        MapObject ob = cell.Objects[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;

                        ob.Attacked(this, damageFinal, DefenceType.Agility, false);
                        break;
                    }
                }
            }

        CrossHalfMoon:
            if (spell == Spell.CrossHalfMoon)
            {
                magic = GetMagic(spell);
                damageFinal = magic.GetDamage(damageBase);
                for (int i = 0; i < 8; i++)
                {
                    target = Functions.PointMove(CurrentLocation, dir, 1);
                    dir = Functions.NextDir(dir);
                    if (target == Front) continue;

                    if (!CurrentMap.ValidPoint(target)) continue;

                    cell = CurrentMap.GetCell(target);

                    if (cell.Objects == null) continue;

                    for (int o = 0; o < cell.Objects.Count; o++)
                    {
                        MapObject ob = cell.Objects[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;

                        ob.Attacked(this, damageFinal, DefenceType.Agility, false);
                        break;
                    }
                }
            }

        Mining:
            if (Mined)
            {
                if (Info.Equipment[(int)EquipmentSlot.Weapon] == null) return;
                if (!Info.Equipment[(int)EquipmentSlot.Weapon].Info.CanMine) return;
                if (Info.Equipment[(int)EquipmentSlot.Weapon].CurrentDura <= 0)//Stop dura 0 working. use below if you wish to break the item.
                    /*{
                        Enqueue(new S.DeleteItem { UniqueID = Info.Equipment[(int)EquipmentSlot.Weapon].UniqueID, Count = Info.Equipment[(int)EquipmentSlot.Weapon].Count });
                        Info.Equipment[(int)EquipmentSlot.Weapon] = null;
                        RefreshStats();*/
                    return;
                /*}*/
                if (CurrentMap.Mine == null) return;
                MineSpot Mine = CurrentMap.Mine[target.X, target.Y];
                if ((Mine == null) || (Mine.Mine == null)) return;
                if (Mine.StonesLeft > 0)
                {
                    Mine.StonesLeft--;
                    if (Envir.Random.Next(100) < (Mine.Mine.HitRate + (Info.Equipment[(int)EquipmentSlot.Weapon].GetTotal(Stat.Accuracy)) * 10))
                    {
                        //create some rubble on the floor (or increase whats there)
                        SpellObject Rubble = null;
                        Cell minecell = CurrentMap.GetCell(CurrentLocation);
                        for (int i = 0; i < minecell.Objects.Count; i++)
                        {
                            if (minecell.Objects[i].Race != ObjectType.Spell) continue;
                            SpellObject ob = (SpellObject)minecell.Objects[i];

                            if (ob.Spell != Spell.Rubble) continue;
                            Rubble = ob;
                            Rubble.ExpireTime = Envir.Time + (5 * 60 * 1000);
                            break;
                        }
                        if (Rubble == null)
                        {
                            Rubble = new SpellObject
                            {
                                Spell = Spell.Rubble,
                                Value = 1,
                                ExpireTime = Envir.Time + (5 * 60 * 1000),
                                TickSpeed = 2000,
                                Caster = null,
                                CurrentLocation = CurrentLocation,
                                CurrentMap = this.CurrentMap,
                                Direction = MirDirection.Up
                            };
                            CurrentMap.AddObject(Rubble);
                            Rubble.Spawned();
                        }
                        if (Rubble != null)
                        {
                            ActionList.Add(new DelayedAction(DelayedType.Mine, Envir.Time + 400, Rubble));
                        }

                        //check if we get a payout
                        if (Envir.Random.Next(100) < (Mine.Mine.DropRate + Stats[Stat.MineRatePercent]))
                        {
                            GetMinePayout(Mine.Mine);
                        }

                        DamageItem(Info.Equipment[(int)EquipmentSlot.Weapon], 5 + Envir.Random.Next(15));
                    }
                }
                else
                {
                    if (Envir.Time > Mine.LastRegenTick)
                    {
                        Mine.LastRegenTick = Envir.Time + Mine.Mine.SpotRegenRate * 60 * 1000;
                        Mine.StonesLeft = (byte)Envir.Random.Next(Mine.Mine.MaxStones);
                    }
                }
            }
        }
        public virtual bool TryMagic()
        {
            return !Dead && Envir.Time >= ActionTime || Envir.Time >= SpellTime;
        }
        public virtual void BeginMagic(Spell spell, MirDirection dir, uint targetID, Point location, Boolean spellTargetLock = false)
        {
            Magic(spell, dir, targetID, location, spellTargetLock);
        }

        public int MagicCost(UserMagic magic)
        {
            int cost = magic.Info.BaseCost + magic.Info.LevelCost * magic.Level;
            Spell spell = magic.Spell;
            if (spell == Spell.Teleport || spell == Spell.Blink || spell == Spell.StormEscape)
            {
                if (Stats[Stat.TeleportManaPenaltyPercent] > 0)
                {
                    cost += (cost * Stats[Stat.TeleportManaPenaltyPercent]) / 100;
                }
            }

            if (Stats[Stat.ManaPenaltyPercent] > 0)
            {
                cost += (cost * Stats[Stat.ManaPenaltyPercent]) / 100;
            }

            if (spell == Spell.Plague)
            {
                cost = Stats[Stat.MaxSC] + Stats[Stat.MinSC];
            }

            return cost;
        }
        public virtual MapObject DefaultMagicTarget => this;
        public void Magic(Spell spell, MirDirection dir, uint targetID, Point location, bool spellTargetLock = false)
        {
            if (!CanCast)
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            UserMagic magic = GetMagic(spell);

            if (magic == null)
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            if (spell.CastsOnSelf())
            {
                //Self spells are anchored on the caster; a cursor position sent by the client carries no meaning.
                location = CurrentLocation;
            }
            else if ((location.X != 0) && (location.Y != 0) && magic.Info.Range != 0 && Functions.InRange(CurrentLocation, location, magic.Info.Range) == false) return;

            if (Hidden)
            {
                RemoveBuff(BuffType.MoonLight);
                RemoveBuff(BuffType.DarkBody);
            }

            AttackTime = Envir.Time + MoveDelay;
            SpellTime = Envir.Time + 1800; //Spell Delay

            if (spell != Spell.ShoulderDash)
            {
                ActionTime = Envir.Time + MoveDelay;
            }

            LogTime = Envir.Time + Globals.LogDelay;

            long delay = magic.GetDelay();

            if (magic != null && Envir.Time < (magic.CastTime + delay))
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            int cost = MagicCost(magic);

            if (cost > MP)
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            RegenTime = Envir.Time + RegenDelay;
            ChangeMP(-cost);

            if (spell == Spell.ShoulderDash && Race == ObjectType.Hero)
                dir = Direction;

            Direction = dir;
            if (spell != Spell.ShoulderDash && spell != Spell.BackStep && spell != Spell.FlashDash)
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });

            MapObject target = null;

            if (targetID == ObjectID)
            {
                target = this;
            }
            else if (targetID > 0)
            {
                target = FindObject(targetID, 10);
            }

            if (target != null && target.Race != ObjectType.Monster && target.Race != ObjectType.Player && target.Race != ObjectType.Hero)
            {
                target = null;
            }

            if (target != null && !target.Dead && target.IsAttackTarget(this) && !target.IsFriendlyTarget(this))
            {
                ShareCombatTarget(target);
            }

            bool cast = true;
            byte level = magic.Level;
            switch (spell)
            {
                case Spell.FireBall:
                case Spell.GreatFireBall:
                case Spell.FrostCrunch:
                    if (!Fireball(target, magic)) targetID = 0;
                    break;
                case Spell.Healing:
                    if (target == null)
                    {
                        target = DefaultMagicTarget;
                        targetID = ObjectID;
                    }
                    Healing(target, magic);
                    break;
                case Spell.Repulsion:
                case Spell.EnergyRepulsor:
                case Spell.FireBurst:
                    Repulsion(magic);
                    break;
                case Spell.ElectricShock:
                    ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, target as MonsterObject));
                    break;
                case Spell.Poisoning:
                    if (!Poisoning(target, magic)) cast = false;
                    break;
                case Spell.HellFire:
                    HellFire(magic);
                    break;
                case Spell.ThunderBolt:
                    ThunderBolt(target, magic);
                    break;
                case Spell.SoulFireBall:
                    if (!SoulFireball(target, magic, out cast)) targetID = 0;
                    break;
                case Spell.SummonSkeleton:
                    SummonSkeleton(magic);
                    break;
                case Spell.Teleport:
                case Spell.Blink:
                    ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 200, magic, location));
                    break;
                case Spell.Hiding:
                    Hiding(magic);
                    break;
                case Spell.Haste:
                case Spell.LightBody:
                    ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic));
                    break;
                case Spell.Fury:
                    FurySpell(magic, out cast);
                    break;
                case Spell.ImmortalSkin:
                    ImmortalSkin(magic, out cast);
                    break;
                case Spell.FireBang:
                case Spell.IceStorm:
                    FireBang(magic, spellTargetLock ? (target != null ? target.CurrentLocation : location) : location);
                    break;
                case Spell.MassHiding:
                    MassHiding(magic, spellTargetLock ? (target != null ? target.CurrentLocation : location) : location, out cast);
                    break;
                case Spell.SoulShield:
                case Spell.BlessedArmour:
                    SoulShield(magic, spellTargetLock ? (target != null ? target.CurrentLocation : location) : location, out cast);
                    break;
                case Spell.FireWall:
                    FireWall(magic, spellTargetLock ? (target != null ? target.CurrentLocation : location) : location);
                    break;
                case Spell.Lightning:
                    Lightning(magic);
                    break;
                case Spell.HeavenlySword:
                    HeavenlySword(magic);
                    break;
                case Spell.MassHealing:
                    MassHealing(magic, spellTargetLock ? (target != null ? target.CurrentLocation : location) : location);
                    break;
                case Spell.ShoulderDash:
                    ShoulderDash(magic);
                    return;
                case Spell.ThunderStorm:
                case Spell.FlameField:
                case Spell.StormEscape:
                    ThunderStorm(magic);
                    if (spell == Spell.FlameField)
                        SpellTime = Envir.Time + 2500; //Spell Delay
                    if (spell == Spell.StormEscape)
                        //Start teleport.
                        ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 750, magic, location));
                    break;
                case Spell.MagicShield:
                    ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, magic.GetPower(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]) + 15)));
                    break;
                case Spell.FlameDisruptor:
                    FlameDisruptor(target, magic);
                    break;
                case Spell.TurnUndead:
                    TurnUndead(target, magic);
                    break;
                case Spell.MagicBooster:
                    MagicBooster(magic);
                    break;
                case Spell.Vampirism:
                    Vampirism(target, magic);
                    break;
                case Spell.SummonShinsu:
                    SummonShinsu(magic);
                    break;
                case Spell.Purification:
                    if (target == null)
                    {
                        target = DefaultMagicTarget;
                        targetID = ObjectID;
                    }
                    Purification(target, magic);
                    break;
                case Spell.LionRoar:
                case Spell.BattleCry:
                    CurrentMap.ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, CurrentLocation));
                    break;
                case Spell.Revelation:
                    Revelation(target, magic);
                    break;
                case Spell.PoisonCloud:
                    PoisonCloud(magic, location, out cast);
                    break;
                case Spell.Entrapment:
                    Entrapment(target, magic);
                    break;
                case Spell.BladeAvalanche:
                    BladeAvalanche(magic);
                    break;
                case Spell.SlashingBurst:
                    ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 300, magic));
                    break;
                case Spell.Rage:
                    Rage(magic);
                    break;
                case Spell.Mirroring:
                    Mirroring(magic);
                    break;
                case Spell.Blizzard:
                    Blizzard(magic, spellTargetLock ? (target != null ? target.CurrentLocation : location) : location, out cast);
                    break;
                case Spell.MeteorStrike:
                    MeteorStrike(magic, spellTargetLock ? (target != null ? target.CurrentLocation : location) : location, out cast);
                    break;
                case Spell.IceThrust:
                    IceThrust(magic);
                    break;
                case Spell.ProtectionField:
                    ProtectionField(magic);
                    break;
                case Spell.PetEnhancer:
                    PetEnhancer(target, magic, out cast);
                    break;
                case Spell.TrapHexagon:
                    TrapHexagon(magic, spellTargetLock ? (target != null ? target.CurrentLocation : location) : location, out cast);
                    break;
                case Spell.Reincarnation:
                    if (!CurrentMap.Info.NoReincarnation)
                        Reincarnation(magic, target == null ? null : target as PlayerObject, out cast);
                    break;
                case Spell.Curse:
                    Curse(magic, spellTargetLock ? (target != null ? target.CurrentLocation : location) : location, out cast);
                    break;
                case Spell.SummonHolyDeva:
                    SummonHolyDeva(magic);
                    break;
                case Spell.Hallucination:
                    Hallucination(target, magic);
                    break;
                case Spell.EnergyShield:
                    EnergyShield(target, magic, out cast);
                    break;
                case Spell.UltimateEnhancer:
                    UltimateEnhancer(target, magic, out cast);
                    break;
                case Spell.Plague:
                    Plague(magic, spellTargetLock ? (target != null ? target.CurrentLocation : location) : location, out cast);
                    break;
                case Spell.SwiftFeet:
                    SwiftFeet(magic, out cast);
                    break;
                case Spell.MoonLight:
                    MoonLight(magic);
                    break;
                case Spell.Trap:
                    Trap(magic, target, out cast);
                    break;
                case Spell.CatTongue:
                    CatTongue(target, magic);
                    break;
                case Spell.PoisonSword:
                    PoisonSword(magic);
                    break;
                case Spell.DarkBody:
                    DarkBody(target, magic);
                    break;
                case Spell.FlashDash:
                    FlashDash(magic);
                    return;
                case Spell.CrescentSlash:
                    CrescentSlash(magic);
                    break;
                case Spell.StraightShot:
                    if (!StraightShot(target, magic)) targetID = 0;
                    break;
                case Spell.DoubleShot:
                    if (!DoubleShot(target, magic)) targetID = 0;
                    break;
                case Spell.BackStep:
                    BackStep(magic);
                    return;
                case Spell.ExplosiveTrap:
                    ExplosiveTrap(magic, Front);
                    break;
                case Spell.DelayedExplosion:
                    if (!DelayedExplosion(target, magic)) targetID = 0;
                    break;
                case Spell.Concentration:
                    Concentration(magic);
                    break;
                case Spell.ElementalShot:
                    if (!ElementalShot(target, magic)) targetID = 0;
                    break;
                case Spell.ElementalBarrier:
                    ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, magic.GetPower(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]))));
                    break;
                case Spell.BindingShot:
                    BindingShot(magic, target, out cast);
                    break;
                case Spell.SummonVampire:
                case Spell.SummonToad:
                case Spell.SummonSnakes:
                    ArcherSummon(magic, target, location);
                    break;
                case Spell.Stonetrap:
                    ArcherSummonStone(magic, spellTargetLock ? (target != null ? target.CurrentLocation : location) : location, out cast);
                    break;
                case Spell.VampireShot:
                case Spell.PoisonShot:
                case Spell.CrippleShot:
                    SpecialArrowShot(target, magic);
                    break;
                case Spell.NapalmShot:
                    NapalmShot(target, magic);
                    break;
                case Spell.OneWithNature:
                    OneWithNature(target, magic);
                    break;
                case Spell.MoonMist:
                    MoonMist(magic);
                    break;
                case Spell.HealingCircle:
                    HealingCircle(magic, spellTargetLock ? (target != null ? target.CurrentLocation : location) : location);
                    break;

                //Custom Spells
                case Spell.Portal:
                    Portal(magic, location, out cast);
                    break;
                case Spell.MeteorShower:
                    if (!MeteorShower(target, magic)) targetID = 0;
                    return;
                case Spell.FireBounce:
                    if (!FireBounce(target, magic, this)) targetID = 0;
                    break;
                default:
                    cast = false;
                    break;
            }

            if (cast)
            {
                magic.CastTime = Envir.Time;
            }

            Enqueue(new S.Magic { Spell = spell, TargetID = targetID, Target = location, Cast = cast, Level = level });
            Broadcast(new S.ObjectMagic { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Spell = spell, TargetID = targetID, Target = location, Cast = cast, Level = level });
        }

        #region Elemental System
        private void Concentration(UserMagic magic)
        {
            int duration = 45 + (15 * magic.Level);

            var buff = AddBuff(BuffType.Concentration, this, Settings.Second * duration, new Stats());

            buff.Set("InterruptTime", (long)0);
            buff.Set("Interrupted", false);

            LevelMagic(magic);
            OperateTime = 0;

            UpdateConcentration(true, false);
        }
        public void UpdateConcentration(bool concentrating, bool interrupted)
        {
            Enqueue(new S.SetConcentration { ObjectID = ObjectID, Enabled = concentrating, Interrupted = interrupted });
            Broadcast(new S.SetConcentration { ObjectID = ObjectID, Enabled = concentrating, Interrupted = interrupted });
        }
        private bool ElementalShot(MapObject target, UserMagic magic)
        {
            if (HasElemental)
            {
                if (target == null || !target.IsAttackTarget(this)) return false;
                if ((Info.MentalState != 1) && !CanFly(target.CurrentLocation)) return false;

                int orbPower = GetElementalOrbPower(false);//base power + orbpower

                int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]) + orbPower);
                int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500; //50 MS per Step

                DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target, target.CurrentLocation);
                ActionList.Add(action);
            }
            else
            {
                ObtainElement(true);//gather orb through casting
                LevelMagic(magic);
                return false;
            }
            return true;
        }
        public void GatherElement()
        {
            UserMagic magic = GetMagic(Spell.Meditation);

            if (magic == null) return;

            int meditationLvl = magic.Level;
            int concentrateChance = 0;

            if (HasBuff(BuffType.Concentration, out Buff concentration) && !concentration.Get<bool>("Interrupted"))
            {
                magic = GetMagic(Spell.Concentration);

                if (magic != null)
                {
                    concentrateChance = magic.Level + 1;
                }
            }

            if (meditationLvl >= 0)
            {
                int rnd = Envir.Random.Next(10);
                if (rnd >= (8 - meditationLvl - concentrateChance))
                {
                    ObtainElement(false);
                    LevelMagic(GetMagic(Spell.Meditation));
                }
            }
        }
        public void ObtainElement(bool cast)
        {
            int orbType = 0;
            int meditateLevel = 0;

            UserMagic spell = GetMagic(Spell.Meditation);

            if (spell == null)
            {
                ReceiveChat(GameLanguage.Get("Skill requires meditation."), ChatType.System);
                return;
            }

            meditateLevel = spell.Level;

            int maxOrbs = (int)Settings.OrbsExpList[Settings.OrbsExpList.Count - 1];

            if (cast)
            {
                ElementsLevel = (int)Settings.OrbsExpList[0];
                orbType = 1;
                if (Settings.GatherOrbsPerLevel)//Meditation Orbs per level
                    if (meditateLevel == 3)
                    {
                        Enqueue(new S.SetElemental { ObjectID = ObjectID, Enabled = true, Value = (uint)Settings.OrbsExpList[0], ElementType = 1, ExpLast = (uint)maxOrbs });
                        Broadcast(new S.SetElemental { ObjectID = ObjectID, Enabled = true, Casted = true, Value = (uint)Settings.OrbsExpList[0], ElementType = 1, ExpLast = (uint)maxOrbs });
                        ElementsLevel = (int)Settings.OrbsExpList[1];
                        orbType = 2;
                    }

                HasElemental = true;
            }
            else
            {
                HasElemental = false;
                ElementsLevel++;

                if (Settings.GatherOrbsPerLevel)//Meditation Orbs per level
                    if (ElementsLevel > Settings.OrbsExpList[GetMagic(Spell.Meditation).Level])
                    {
                        HasElemental = true;
                        ElementsLevel = (int)Settings.OrbsExpList[GetMagic(Spell.Meditation).Level];
                        return;
                    }

                if (ElementsLevel >= Settings.OrbsExpList[0]) HasElemental = true;
                for (int i = 0; i <= Settings.OrbsExpList.Count - 1; i++)
                {
                    if (Settings.OrbsExpList[i] != ElementsLevel) continue;
                    orbType = i + 1;
                    break;
                }
            }

            Enqueue(new S.SetElemental { ObjectID = ObjectID, Enabled = HasElemental, Value = (uint)ElementsLevel, ElementType = (uint)orbType, ExpLast = (uint)maxOrbs });
            Broadcast(new S.SetElemental { ObjectID = ObjectID, Enabled = HasElemental, Casted = cast, Value = (uint)ElementsLevel, ElementType = (uint)orbType, ExpLast = (uint)maxOrbs });
        }
        public int GetElementalOrbCount()
        {
            int OrbCount = 0;
            for (int i = Settings.OrbsExpList.Count - 1; i >= 0; i--)
            {
                if (ElementsLevel >= Settings.OrbsExpList[i])
                {
                    OrbCount = i + 1;
                    break;
                }
            }
            return OrbCount;
        }
        public int GetElementalOrbPower(bool defensive)
        {
            if (!HasElemental) return 0;

            if (defensive)
                return (int)Settings.OrbsDefList[GetElementalOrbCount() - 1];

            if (!defensive)
                return (int)Settings.OrbsDmgList[GetElementalOrbCount() - 1];

            return 0;
        }
        #endregion

        #region Wizard Skills
        private bool Fireball(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this) || !CanFly(target.CurrentLocation)) return false;

            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));

            int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target, target.CurrentLocation);

            ActionList.Add(action);

            return true;
        }
        private void Repulsion(UserMagic magic)
        {
            bool result = false;
            for (int d = 0; d <= 1; d++)
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

                        for (int i = 0; cell.Objects != null && i < cell.Objects.Count; i++)
                        {
                            MapObject ob = cell.Objects[i];
                            if (ob.Race != ObjectType.Monster && ob.Race != ObjectType.Player && ob.Race != ObjectType.Hero) continue;

                            if (!ob.IsAttackTarget(this) || ob.Level >= Level) continue;

                            if (Envir.Random.Next(20) >= 6 + magic.Level * 3 + Level - ob.Level) continue;

                            int distance = 1 + Math.Max(0, magic.Level - 1) + Envir.Random.Next(2);
                            MirDirection dir = Functions.DirectionFromPoint(CurrentLocation, ob.CurrentLocation);

                            if (ob.Pushed(this, dir, distance) == 0) continue;

                            if (ob.Race == ObjectType.Player)
                            {
                                SafeZoneInfo szi = CurrentMap.GetSafeZone(ob.CurrentLocation);

                                if (szi != null)
                                {
                                    ((PlayerObject)ob).BindLocation = szi.Location;
                                    ((PlayerObject)ob).BindMapIndex = CurrentMapIndex;
                                    ob.InSafeZone = true;

                                }
                                else
                                    ob.InSafeZone = false;

                                ob.Attacked(this, magic.GetDamage(0), DefenceType.None, false);
                            }
                            result = true;
                        }
                    }
                }
            }

            if (result) LevelMagic(magic);
        }
        private void ElectricShock(MonsterObject target, UserMagic magic)
        {
            if (CurrentMap.Info.NoPets)
            {
                ReceiveChat("You cannot summon pets on this map.", ChatType.System);
                return;
            }
            
            if (target == null || !target.IsAttackTarget(this)) return;

            PetTameSettings tame = Settings.PetTame;

            if (Envir.Random.Next(tame.ShockChance(magic.Level)) > 0)
            {
                if (Envir.Random.Next(2) == 0) LevelMagic(magic);
                return;
            }

            LevelMagic(magic);

            if (target.Master == this)
            {
                target.ShockTime = Envir.Time + tame.ShockDuration(magic.Level);
                target.Target = null;
                return;
            }

            if (Envir.Random.Next(2) > 0)
            {
                target.ShockTime = Envir.Time + tame.ShockDuration(magic.Level);
                target.Target = null;
                return;
            }

            if (target.Level > Level + tame.MaxLevelDifference || !target.Info.CanTame) return;

            if (target.Info.IsBoss)
            {
                int limit = Settings.MaxBossTames;
                if (limit <= 0) return;

                int currentBossTames = Pets.Count(p => !p.Dead && p.Race != ObjectType.Creature && p.Info != null && p.Info.IsBoss);
                if (currentBossTames >= limit) return;
            }

            if (Envir.Random.Next(Level + tame.TameChanceBase + magic.Level * tame.TameChancePerMagicLevel) <= target.Level + tame.TameChanceTargetOffset)
            {
                if (Envir.Random.Next(tame.RageChance()) > 0 && target.Master == null)
                {
                    target.RageTime = Envir.Time + (Envir.Random.Next(tame.RageRandomSeconds) + tame.RageBaseSeconds) * 1000;
                    target.Target = null;
                }
                return;
            }

            if (Pets.Count(t => !t.Dead && t.Race != ObjectType.Creature) >= tame.PetLimit(magic.Level)) return;

            int rate = (int)(target.Stats[Stat.HP] / 100);
            if (rate <= 2) rate = 2;
            else rate *= 2;

            if (Envir.Random.Next(rate) != 0) return;
            //else if (Envir.Random.Next(20) == 0) target.Die();

            if (target.Master != null)
            {
                target.SetHP(target.Stats[Stat.HP] / 10);
                target.Master.Pets.Remove(target);
            }
            else if (target.Respawn != null)
            {
                target.Respawn.Count--;
                Envir.MonsterCount--;
                CurrentMap.MonsterCount--;
                target.Respawn = null;
            }

            target.Master = this;
            //target.HealthChanged = true;
            target.BroadcastHealthChange();
            Pets.Add(target);
            target.Target = null;
            target.RageTime = 0;
            target.ShockTime = 0;
            target.OperateTime = 0;
            target.MaxPetLevel = (byte)(1 + magic.Level * 2);

            if (!Settings.PetSave)
            {
                target.TameTime = Envir.Time + (Settings.Minute * 60);
            }

            target.Broadcast(new S.ObjectName { ObjectID = target.ObjectID, Name = target.Name });
        }
        private void HellFire(UserMagic magic)
        {
            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation, Direction, 4);
            CurrentMap.ActionList.Add(action);

            if (magic.Level != 3) return;

            MirDirection dir = (MirDirection)(((int)Direction + 1) % 8);
            action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation, dir, 4);
            CurrentMap.ActionList.Add(action);

            dir = (MirDirection)(((int)Direction - 1 + 8) % 8);
            action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation, dir, 4);
            CurrentMap.ActionList.Add(action);
        }
        private void ThunderBolt(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return;

            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));

            if (target.Undead) damage = (int)(damage * 1.5F);

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, damage, target, target.CurrentLocation);

            ActionList.Add(action);
        }
        private void Vampirism(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return;

            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, damage, target);

            ActionList.Add(action);
        }
        private void FireBang(UserMagic magic, Point location)
        {
            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, location);
            CurrentMap.ActionList.Add(action);
        }
        private void FireWall(UserMagic magic, Point location)
        {
            List<SpellObject> activeFireWalls = null;
            if (CurrentMap.Info.FireWallLimit && CurrentMap.Info.FireWallCount > 0)
            {
                activeFireWalls = Envir
                    .GetObjects(CurrentMapIndex, ObjectType.Spell)
                    .OfType<SpellObject>()
                    .Where(so => so.Spell == Spell.FireWall && so.Caster == this)
                    .ToList();

                var activeCasts = activeFireWalls
                    .Select(so => so.CastInstanceId)
                    .DefaultIfEmpty(0)
                    .Distinct()
                    .Count();

                if (activeCasts >= CurrentMap.Info.FireWallCount)
                {
                    if (!TryReplaceOldestFireWallCast(activeFireWalls))
                    {
                        ReceiveChat("Unable to recycle an existing FireWall. Please wait a moment.", ChatType.System);
                        return;
                    }

                }
            }

            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));
            int castId = ++_fireWallCastSeq;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, location, castId);
            CurrentMap.ActionList.Add(action);
        }
        private bool TryReplaceOldestFireWallCast(List<SpellObject> activeFireWalls)
        {
            if (activeFireWalls == null || activeFireWalls.Count == 0) return false;

            var targetGroup = activeFireWalls
                .GroupBy(so => so.CastInstanceId)
                .OrderBy(g => g.Key == 0 ? int.MinValue : g.Key)
                .FirstOrDefault();

            if (targetGroup == null) return false;

            foreach (var spell in targetGroup)
            {
                if (spell.CurrentMap != null)
                {
                    spell.CurrentMap.RemoveObject(spell);
                }

                if (spell.Node != null)
                {
                    spell.Despawn();
                }
            }

            return true;
        }
        private void Lightning(UserMagic magic)
        {
            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation, Direction);
            CurrentMap.ActionList.Add(action);
        }
        private void TurnUndead(MapObject target, UserMagic magic)
        {
            if(target != null &&
               target.Race == ObjectType.Monster &&
               target.Undead &&
               target.IsAttackTarget(this))
            {
                // undead pet logic
                if (target.Master is PlayerObject master)
                {
                    if (master.PKPoints < 200 &&
                        (master.BrownTime == 0 &&
                        !master.AtWar(this)))
                    {
                            BrownTime = Envir.Time + Settings.Minute;
                    }   
                }

                if (Envir.Random.Next(2) + Level - 1 <= target.Level)
                {
                    target.Target = this;
                    return;
                }

                int dif = Level - target.Level + 15;

                if (Envir.Random.Next(100) >= (magic.Level + 1 << 3) + dif)
                {
                    target.Target = this;
                    return;
                }

                DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, target);
                ActionList.Add(action);
            }
        }
        private void FlameDisruptor(MapObject target, UserMagic magic)
        {
            if (target == null || (target.Race != ObjectType.Player && target.Race != ObjectType.Monster) || !target.IsAttackTarget(this)) return;

            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));

            if (!target.Undead) damage = (int)(damage * 1.5F);

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, damage, target, target.CurrentLocation);

            ActionList.Add(action);
        }
        private void ThunderStorm(UserMagic magic)
        {
            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation);
            CurrentMap.ActionList.Add(action);
        }
        private void Mirroring(UserMagic magic)
        {
            if (CurrentMap.Info.NoPets)
            {
                ReceiveChat("You cannot summon pets on this map.", ChatType.System);
                return;
            }
            
            MonsterObject monster;
            DelayedAction action;
            for (int i = 0; i < Pets.Count; i++)
            {
                monster = Pets[i];
                if ((monster.Info.Name != Settings.CloneName) || monster.Dead) continue;
                if (monster.Node == null) continue;
                action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, monster, Front, true);
                CurrentMap.ActionList.Add(action);
                return;
            }

            MonsterInfo info = Envir.GetMonsterInfo(Settings.CloneName);
            if (info == null) return;

            LevelMagic(magic);

            monster = MonsterObject.GetMonster(info);
            monster.Master = this;
            monster.ActionTime = Envir.Time + 1000;
            monster.RefreshNameColour(false);

            Pets.Add(monster);

            action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, monster, Front, false);
            CurrentMap.ActionList.Add(action);
        }
        private void Blizzard(UserMagic magic, Point location, out bool cast)
        {
            cast = false;

            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, location);

            ActiveBlizzard = true;
            CurrentMap.ActionList.Add(action);
            cast = true;
        }
        private void MeteorStrike(UserMagic magic, Point location, out bool cast)
        {
            cast = false;

            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, location);

            ActiveBlizzard = true;
            CurrentMap.ActionList.Add(action);
            cast = true;
        }

        private void IceThrust(UserMagic magic)
        {
            int damageBase = GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]);
            if (Envir.Random.Next(100) < (1 + Stats[Stat.Luck]))
                damageBase += damageBase;
            int damageFinish = magic.GetDamage(damageBase);

            Point location = Functions.PointMove(CurrentLocation, Direction, 1);

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 1500, this, magic, location, Direction, damageFinish, (int)(damageFinish * 0.6));

            CurrentMap.ActionList.Add(action);
        }

        private void MagicBooster(UserMagic magic)
        {
            int bonus = 6 + magic.Level * 6;

            ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, bonus));
        }

        #endregion

        #region Taoist Skills
        private void Healing(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsFriendlyTarget(this)) return;

            int health = magic.GetDamage(GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]) * 2) + Level;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, health, target);

            ActionList.Add(action);
        }
        private bool Poisoning(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return false;

            UserItem item = GetPoison(1);
            if (item == null) return false;

            int power = magic.GetDamage(GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, power, target, item);
            ActionList.Add(action);
            ConsumeItem(item, 1);
            return true;
        }
        private bool SoulFireball(MapObject target, UserMagic magic, out bool cast)
        {
            cast = false;
            UserItem item = GetAmulet(1);
            if (item == null) return false;
            cast = true;

            if (target == null || !target.IsAttackTarget(this) || !CanFly(target.CurrentLocation)) return false;

            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]));

            int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target, target.CurrentLocation);

            ActionList.Add(action);
            ConsumeItem(item, 1);

            return true;
        }
        private void SummonSkeleton(UserMagic magic)
        {
            SummonTaoistPet(magic, Settings.PetSummonSkeleton);
        }

        /// <summary>
        /// Shared implementation of the three Taoist summons. Everything they used to hardcode,
        /// from the monster template to the pet level growth, is read from Configs/PetSummon.ini
        /// through <see cref="Settings.FindTaoistSummon"/>.
        /// </summary>
        private void SummonTaoistPet(UserMagic magic, PetSummonSettings summon)
        {
            if (CurrentMap.Info.NoPets)
            {
                ReceiveChat("You cannot summon pets on this map.", ChatType.System);
                return;
            }

            string monsterName = summon.MonsterName;
            MonsterObject monster;
            MonsterObject recall = null;
            int existing = 0;

            for (int i = 0; i < Pets.Count; i++)
            {
                monster = Pets[i];
                if ((monster.Info.Name != monsterName) || monster.Dead) continue;
                if (monster.Node == null) continue;

                if (recall == null) recall = monster;
                existing++;
            }

            // Below the skill limit a cast adds another servant, at the limit it recalls one. The
            // old code always recalled, so a limit of one keeps the original behaviour.
            PetSummonDecision decision = PetSummonSettings.DecideSummon(existing, summon,
                Pets.Count(x => x.Race == ObjectType.Monster), Settings.PetSummonShared.MaxActivePets);

            if (decision == PetSummonDecision.Reject) return;

            if (decision == PetSummonDecision.Recall)
            {
                recall.ActionList.Add(new DelayedAction(DelayedType.Recall, Envir.Time + 500));
                return;
            }

            UserItem item = null;

            if (summon.AmuletCost > 0)
            {
                item = GetAmulet(summon.AmuletCost);
                if (item == null) return;
            }

            MonsterInfo info = Envir.GetMonsterInfo(monsterName);
            if (info == null) return;

            LevelMagic(magic);

            if (item != null) ConsumeItem(item, (byte)summon.AmuletCost);

            monster = MonsterObject.GetMonster(info);
            monster.PetLevel = magic.Level;
            monster.Master = this;
            monster.MaxPetLevel = summon.GetMaxPetLevel(magic.Level);
            monster.Direction = Direction;
            monster.ActionTime = Envir.Time + 1000;
            monster.RefreshNameColour(false);

            //Pets.Add(monster);

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + summon.SummonDelay, this, magic, monster, Front);
            CurrentMap.ActionList.Add(action);
        }
        private void Purification(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsFriendlyTarget(this)) return;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, target);

            ActionList.Add(action);
        }
        private void SummonShinsu(UserMagic magic)
        {
            SummonTaoistPet(magic, Settings.PetSummonShinsu);
        }
        private void Hiding(UserMagic magic)
        {
            UserItem item = GetAmulet(1);
            if (item == null) return;

            ConsumeItem(item, 1);

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]) + (magic.Level + 1) * 5);
            ActionList.Add(action);

        }
        private void MassHiding(UserMagic magic, Point location, out bool cast)
        {
            cast = false;
            UserItem item = GetAmulet(1);
            if (item == null) return;
            cast = true;

            int delay = Functions.MaxDistance(CurrentLocation, location) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, this, magic, GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]) / 2 + (magic.Level + 1) * 2, location);
            CurrentMap.ActionList.Add(action);
        }
        private void SoulShield(UserMagic magic, Point location, out bool cast)
        {
            cast = false;
            UserItem item = GetAmulet(1);
            if (item == null) return;
            cast = true;

            int delay = Functions.MaxDistance(CurrentLocation, location) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, this, magic, GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]) * 4 + (magic.Level + 1) * 50, location);
            CurrentMap.ActionList.Add(action);

            ConsumeItem(item, 1);
        }
        private void MassHealing(UserMagic magic, Point location)
        {
            int value = magic.GetDamage(GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, value, location);
            CurrentMap.ActionList.Add(action);
        }
        private void Revelation(MapObject target, UserMagic magic)
        {
            if (target == null) return;

            int value = GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]) + magic.GetPower();

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, value, target);

            ActionList.Add(action);
        }
        private void PoisonCloud(UserMagic magic, Point location, out bool cast)
        {
            cast = false;

            UserItem amulet = GetAmulet(5);
            if (amulet == null) return;

            UserItem poison = GetPoison(5, 1);
            if (poison == null) return;

            int delay = Functions.MaxDistance(CurrentLocation, location) * 50 + 500; //50 MS per Step
            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, this, magic, damage, location, (byte)Envir.Random.Next(Stats[Stat.PoisonAttack]));

            ConsumeItem(amulet, 5);
            ConsumeItem(poison, 5);

            CurrentMap.ActionList.Add(action);
            cast = true;
        }

        private void MoonMist(UserMagic magic)
        {
            for (int i = 0; i < Buffs.Count; i++)
                if (Buffs[i].Type == BuffType.MoonLight) return;

            var time = GetAttackPower(BaseStats[Stat.MinAC], BaseStats[Stat.MaxAC]);

            AddBuff(BuffType.MoonLight, this, (time + (magic.Level + 1) * 5) * 500, new Stats());

            CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.MoonMist }, CurrentLocation);
            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]));
            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation, Direction);
            CurrentMap.ActionList.Add(action);
            LevelMagic(magic);

        }
        private bool CatTongue(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this) || !CanFly(target.CurrentLocation)) return false;

            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]));

            int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target);

            ActionList.Add(action);

            return true;
        }

        private void TrapHexagon(UserMagic magic, Point location, out bool cast)
        {
            cast = false;
            bool anyTargetsFound = false;
            for (int x = location.X - 1; x <= location.X + 1; x++)
            {
                if (x < 0 || x >= CurrentMap.Width) continue;
                for (int y = location.Y - 1; y < location.Y + 1; y++)
                {
                    if (y < 0 || y >= CurrentMap.Height) continue;
                    if (!CurrentMap.ValidPoint(x, y)) continue;
                    var cell = CurrentMap.GetCell(x, y);
                    if (cell == null ||
                        cell.Objects == null ||
                        cell.Objects.Count <= 0) continue;
                    foreach (var target in cell.Objects)
                    {
                        switch (target.Race)
                        {
                            case ObjectType.Monster:
                                if (!target.IsAttackTarget(this)) continue;
                                if (target.Level > Level + 2) continue;
                                anyTargetsFound = true;
                                break;
                        }
                    }
                }
            }
            if (!anyTargetsFound)
                return;

            UserItem item = GetAmulet(1);
            //Point location = target.CurrentLocation;

            if (item == null) return;

            LevelMagic(magic);
            uint duration = (uint)((magic.Level * 5 + 10) * 1000);
            int value = (int)duration;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, value, location);
            CurrentMap.ActionList.Add(action);

            ConsumeItem(item, 1);
            cast = true;
        }
        private void Reincarnation(UserMagic magic, PlayerObject target, out bool cast)
        {
            cast = true;

            if (target == null || !target.Dead) return;

            // checks for amulet of revival
            UserItem item = GetAmulet(1, 3);
            if (item == null) return;

            if (!ActiveReincarnation && !ReincarnationReady)
            {
                cast = false;
                int CastTime = Math.Abs(((magic.Level + 1) * 1000) - 9000);
                ExpireTime = Envir.Time + CastTime;
                ReincarnationReady = true;
                ActiveReincarnation = true;
                ReincarnationTarget = target;
                ReincarnationExpireTime = ExpireTime + 5000;

                target.ReincarnationHost = this;

                SpellObject ob = new SpellObject
                {
                    Spell = Spell.Reincarnation,
                    ExpireTime = ExpireTime,
                    TickSpeed = 1000,
                    Caster = this,
                    CurrentLocation = CurrentLocation,
                    CastLocation = CurrentLocation,
                    Show = true,
                    CurrentMap = CurrentMap,
                };
                Packet p = new S.Chat { Message = GameLanguage.Get("{0} is attempting to revive {1}", Name, target.Name), Type = ChatType.Shout };

                for (int i = 0; i < CurrentMap.Players.Count; i++)
                {
                    if (!Functions.InRange(CurrentLocation, CurrentMap.Players[i].CurrentLocation, Globals.DataRange * 2)) continue;
                    CurrentMap.Players[i].Enqueue(p);
                }

                CurrentMap.AddObject(ob);
                ob.Spawned();
                ConsumeItem(item, 1);
                // chance of failing Reincarnation when casting
                if (Envir.Random.Next(30) > (1 + magic.Level) * 10)
                {
                    return;
                }

                DelayedAction action = new DelayedAction(DelayedType.Magic, ExpireTime, magic);

                ActionList.Add(action);
                return;
            }
            return;
        }
        private void SummonHolyDeva(UserMagic magic)
        {
            SummonTaoistPet(magic, Settings.PetSummonHolyDeva);
        }
        private void Hallucination(MapObject target, UserMagic magic)
        {
            if (target == null || target.Race != ObjectType.Monster || !target.IsAttackTarget(this)) return;

            int damage = 0;
            int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, delay, magic, damage, target);

            ActionList.Add(action);
        }
        private void EnergyShield(MapObject target, UserMagic magic, out bool cast)
        {
            cast = false;

            if (target == null || target.Node == null || !target.IsFriendlyTarget(this)) target = this; //offical is only party target

            int duration = 30 + 50 * magic.Level;
            int power = magic.GetPower(GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]));

            int chance = (10 - (Stats[Stat.Luck] / 3 + magic.Level + 1));

            if (chance < 2) chance = 2;

            var stats = new Stats
            {
                [Stat.EnergyShieldPercent] = (int)Math.Round((1 / (decimal)chance) * 100),
                [Stat.EnergyShieldHPGain] = power
            };

            switch (target.Race)
            {
                case ObjectType.Player:
                    //Only targets
                    if (target.IsFriendlyTarget(this))
                    {
                        target.AddBuff(BuffType.EnergyShield, this, (Settings.Second * duration), stats);
                        target.OperateTime = 0;
                        LevelMagic(magic);
                        cast = true;
                    }
                    break;
            }
        }
        private void UltimateEnhancer(MapObject target, UserMagic magic, out bool cast)
        {
            cast = false;

            if (target == null || target.Node == null || !target.IsFriendlyTarget(this)) return;
            UserItem item = GetAmulet(1);
            if (item == null) return;

            int expiretime = GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]) * 4 + (magic.Level + 1) * 50;
            int value = Stats[Stat.MaxSC] >= 5 ? Math.Min(8, Stats[Stat.MaxSC] / 5) : 1;

            switch (target.Race)
            {
                case ObjectType.Monster:
                case ObjectType.Player:
                case ObjectType.Hero:
                    //Only targets
                    if (target.IsFriendlyTarget(this))
                    {
                        var stats = new Stats();

                        if (target.Race == ObjectType.Monster || ((HumanObject)target).Class == MirClass.Warrior || ((HumanObject)target).Class == MirClass.Assassin)
                        {
                            stats[Stat.MaxDC] = value;
                        }
                        else if (((HumanObject)target).Class == MirClass.Wizard || ((HumanObject)target).Class == MirClass.Archer)
                        {
                            stats[Stat.MaxMC] = value;
                        }
                        else if (((HumanObject)target).Class == MirClass.Taoist)
                        {
                            stats[Stat.MaxSC] = value;
                        }

                        target.AddBuff(BuffType.UltimateEnhancer, this, Settings.Second * expiretime, stats);
                        target.OperateTime = 0;
                        LevelMagic(magic);
                        ConsumeItem(item, 1);
                        cast = true;
                    }
                    break;
            }
        }
        private void Plague(UserMagic magic, Point location, out bool cast)
        {
            cast = false;
            UserItem item = GetAmulet(1);
            if (item == null) return;
            cast = true;

            int delay = Functions.MaxDistance(CurrentLocation, location) * 50 + 500; //50 MS per Step


            PoisonType pType = PoisonType.None;

            UserItem itemp = GetPoison(1, 1);

            if (itemp != null)
                pType = PoisonType.Green;
            else
            {
                itemp = GetPoison(1, 2);

                if (itemp != null)
                    pType = PoisonType.Red;
            }

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, this, magic, magic.GetDamage(GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC])), location, pType);
            CurrentMap.ActionList.Add(action);

            ConsumeItem(item, 1);
            if (itemp != null) ConsumeItem(itemp, 1);
        }
        private void Curse(UserMagic magic, Point location, out bool cast)
        {
            cast = false;
            UserItem item = GetAmulet(1);
            if (item == null) return;
            cast = true;

            ConsumeItem(item, 1);

            if (Envir.Random.Next(10 - ((magic.Level + 1) * 2)) > 2) return;

            int delay = Functions.MaxDistance(CurrentLocation, location) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, this, magic, magic.GetDamage(GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC])), location, 1 + ((magic.Level + 1) * 2));
            CurrentMap.ActionList.Add(action);

        }


        private void PetEnhancer(MapObject target, UserMagic magic, out bool cast)
        {
            cast = false;

            if (target == null || target.Race != ObjectType.Monster || !target.IsFriendlyTarget(this)) return;

            int duration = GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]) + magic.GetPower();

            cast = true;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, duration, target);

            ActionList.Add(action);
        }
        #endregion

        #region Warrior Skills
        private void Entrapment(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return;

            int damage = 0;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, damage, target);

            ActionList.Add(action);
        }
        private void BladeAvalanche(UserMagic magic)
        {
            int damageBase = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
            if (Envir.Random.Next(0, 100) <= (1 + Stats[Stat.Luck]))
                damageBase += damageBase;//crit should do something like double dmg, not double max dc dmg!
            int damageFinal = magic.GetDamage(damageBase);

            int col = 3;
            int row = 3;

            Point[] loc = new Point[col]; //0 = left 1 = center 2 = right
            loc[0] = Functions.PointMove(CurrentLocation, Functions.PreviousDir(Direction), 1);
            loc[1] = Functions.PointMove(CurrentLocation, Direction, 1);
            loc[2] = Functions.PointMove(CurrentLocation, Functions.NextDir(Direction), 1);
            bool train = false;
            for (int i = 0; i < col; i++)
            {
                Point startPoint = loc[i];
                for (int j = 0; j < row; j++)
                {
                    Point hitPoint = Functions.PointMove(startPoint, Direction, j);

                    if (!CurrentMap.ValidPoint(hitPoint)) continue;

                    Cell cell = CurrentMap.GetCell(hitPoint);

                    if (cell.Objects == null) continue;

                    for (int k = 0; k < cell.Objects.Count; k++)
                    {
                        MapObject target = cell.Objects[k];
                        switch (target.Race)
                        {
                            case ObjectType.Monster:
                            case ObjectType.Player:
                            case ObjectType.Hero:
                                //Only targets
                                if (target.IsAttackTarget(this))
                                {
                                        if (target.Attacked(this, j <= 1 ? damageFinal : (int)(damageFinal * 0.6),
                                            target is MonsterObject monster && (monster.Info.AI == 49) ? DefenceType.Repulsion : DefenceType.MAC,
                                            false) > 0)
                                            train = true;
                                }
                                break;
                        }
                    }
                }
            }
            if (train)
                LevelMagic(magic);
        }
        private void ProtectionField(UserMagic magic)
        {
            int duration = 45 + (15 * magic.Level);
            int addValue = (int)Math.Round(BaseStats[Stat.MaxAC] * (0.2 + (0.03 * magic.Level)));

            AddBuff(BuffType.ProtectionField, this, Settings.Second * duration, new Stats { [Stat.MaxAC] = addValue, [Stat.MinAC] = addValue });
            OperateTime = 0;
            LevelMagic(magic);
        }
        private void Rage(UserMagic magic)
        {

            int duration = 18 + (6 * magic.Level);
            int addValue = (int)Math.Round(Stats[Stat.MaxDC] * (0.12 + (0.03 * magic.Level)));

            AddBuff(BuffType.Rage, this, Settings.Second * duration, new Stats { [Stat.MaxDC] = addValue, [Stat.MinDC] = addValue });
            OperateTime = 0;
            LevelMagic(magic);
        }

        private void ShoulderDash(UserMagic magic)
        {
            if (InTrapRock || !CanWalk)
            {
                return;
            }

            Point _nextLocation;
            MapObject _target = null;

            bool _blocking = false;
            bool _canDash = false;

            int _cellsTravelled = 0;
            int dist = Envir.Random.Next(2) + magic.Level + 2;

            ActionTime = Envir.Time + MoveDelay;

            for (int i = 0; i < dist; i++)
            {
                if (_blocking)
                {
                    break;
                }

                _nextLocation = Functions.PointMove(CurrentLocation, Direction, 1);

                if (!CurrentMap.ValidPoint(_nextLocation) || CurrentMap.GetSafeZone(_nextLocation) != null)
                {
                    break;
                }


                // acquire target
                if (i == 0)
                {
                    Cell targetCell = CurrentMap.GetCell(_nextLocation);

                    if (targetCell.Objects != null)
                    {
                        int cellCnt = targetCell.Objects.Count;

                        for (int j = 0; j < cellCnt; j++)
                        {
                            MapObject ob = targetCell.Objects[j];

                            if ((ob.Race == ObjectType.Player ||
                                ob.Race == ObjectType.Monster ||
                                ob.Race == ObjectType.Hero) &&
                                ob.IsAttackTarget(this) &&
                                ob.Level < Level)
                            {
                                _target = ob;
                                break;
                            }

                            if(ob.Blocking)
                            {
                                _blocking = true;
                                break;
                            }
                        }
                    }
                    
                    if (_blocking)
                    {
                        break;
                    }
                }

                // try to dash
                Cell dashCell = CurrentMap.GetCell(_nextLocation);
                _canDash = false;

                if (_target == null)
                {
                    if (dashCell.Objects != null)
                    {
                        int cellCnt = dashCell.Objects.Count;

                        for (int k = 0; k < cellCnt; k++)
                        {
                            MapObject ob = dashCell.Objects[k];

                            if (ob.Blocking)
                            {
                                _blocking = true;
                                break;
                            }
                        }

                        if(!_blocking)
                        {
                            _canDash = true;
                        }
                    }
                    else
                    {
                        _canDash = true;
                    }
                }
                else
                {
                    // try to push
                    if (_target.Pushed(this, Direction, 1) == 0)
                    {
                        _blocking = true;
                    }
                    else
                    {
                        _canDash = true;
                    }
                }

                if (_canDash)
                {
                    CurrentMap.GetCell(CurrentLocation).Remove(this);
                    RemoveObjects(Direction, 1);

                    Enqueue(new S.UserDash { Direction = Direction, Location = _nextLocation });
                    Broadcast(new S.ObjectDash { ObjectID = ObjectID, Direction = Direction, Location = _nextLocation });

                    CurrentMap.GetCell(_nextLocation).Add(this);
                    AddObjects(Direction, 1);

                    // dash interrupt
                    Cell cell = CurrentMap.GetCell(_nextLocation);
                    for (int l = 0; l < cell.Objects.Count; l++)
                    {
                        if (cell.Objects[l].Race == ObjectType.Spell)
                        {
                            SpellObject ob = (SpellObject)cell.Objects[l];

                            if (IsAttackTarget(ob.Caster))
                            {
                                switch (ob.Spell)
                                {
                                    case Spell.FireWall:
                                        if (Attacked((PlayerObject)ob.Caster, ob.Value, DefenceType.MAC, false) > 0)
                                        {
                                        _blocking = true;
                                        }
                                        break;
                                }
                            }
                        }
                    }

                    CurrentLocation = _nextLocation;
                    _cellsTravelled++;
                }
            }

            if (_cellsTravelled == 0)
            {
                Enqueue(new S.UserDashFail { Direction = Direction, Location = CurrentLocation });
                Broadcast(new S.ObjectDashFail { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

                if (InSafeZone)
                {
                    ReceiveChat(GameLanguage.Get("No pushing in the safezone. tut tut."), ChatType.System);
                }
                else
                {
                    ReceiveChat(GameLanguage.Get("Not enough pushing Power."), ChatType.System);
                }
            }
            else
            {
                _target?.Attacked(this, magic.GetDamage(0), DefenceType.None, false);
                LevelMagic(magic);

                // Broadcast(new S.ObjectDash { ObjectID = ObjectID, Direction = Direction, Location = Front });
            }

            long now = Envir.Time;

            magic.CastTime = now;
            Enqueue(new S.MagicCast { Spell = magic.Spell });

            CellTime = now + 500;
            _stepCounter = 0;
        }

        private void SlashingBurst(UserMagic magic)
        {
            int damageBase = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
            int damageFinal = magic.GetDamage(damageBase);

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damageFinal, CurrentLocation, Direction, 1);
            CurrentMap.ActionList.Add(action);

            Point location = Functions.PointMove(CurrentLocation, Direction, 2);

            if (!CurrentMap.ValidPoint(location)) return;

            Cell cInfo = CurrentMap.GetCell(location);

            bool blocked = false;
            if (cInfo.Objects != null)
            {
                for (int c = 0; c < cInfo.Objects.Count; c++)
                {
                    MapObject ob = cInfo.Objects[c];
                    if (!ob.Blocking) continue;
                    blocked = true;
                    if ((cInfo.Objects == null) || blocked) break;
                }
            }

            if (blocked) return;

            CurrentMap.GetCell(CurrentLocation).Remove(this);
            Broadcast(new S.ObjectRemove { ObjectID = ObjectID });
            RemoveObjects(Direction, 2);

            CurrentLocation = location;
            InTrapRock = false;

            CurrentMap.GetCell(CurrentLocation).Add(this);
            BroadcastInfo();
            AddObjects(Direction, 2);
            Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
        }
        private void FurySpell(UserMagic magic, out bool cast)
        {
            cast = true;

            ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic));
        }
        private void ImmortalSkin(UserMagic magic, out bool cast)
        {
            cast = true;

            ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic));

        }
        private void CounterAttackCast(UserMagic magic, MapObject target)
        {
            if (target == null || magic == null) return;

            if (CounterAttack == false) return;

            int damageBase = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
            if (Envir.Random.Next(0, 100) <= Stats[Stat.Accuracy])
                damageBase += damageBase;//crit should do something like double dmg, not double max dc dmg!
            int damageFinal = magic.GetDamage(damageBase);


            MirDirection dir = Functions.ReverseDirection(target.Direction);
            Direction = dir;

            if (Functions.InRange(CurrentLocation, target.CurrentLocation, 1) == false) return;
            if (Envir.Random.Next(10) > magic.Level + 6) return;
            Enqueue(new S.ObjectMagic { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Spell = Spell.CounterAttack, TargetID = target.ObjectID, Target = target.CurrentLocation, Cast = true, Level = GetMagic(Spell.CounterAttack).Level, SelfBroadcast = true });
            DelayedAction action = new DelayedAction(DelayedType.Damage, AttackTime, target, damageFinal, DefenceType.AC, true);
            ActionList.Add(action);
            LevelMagic(magic);
            CounterAttack = false;
        }
        #endregion

        #region Assassin Skills

        private void HeavenlySword(UserMagic magic)
        {
            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation, Direction);
            CurrentMap.ActionList.Add(action);
        }
        private void SwiftFeet(UserMagic magic, out bool cast)
        {
            cast = true;

            ActiveSwiftFeet = true;

            AddBuff(BuffType.SwiftFeet, this, (Settings.Second * 25) + (magic.Level * 5000), new Stats(), true);

            LevelMagic(magic);
        }
        private void MoonLight(UserMagic magic)
        {
            var time = GetAttackPower(BaseStats[Stat.MinAC], BaseStats[Stat.MaxAC]);

            AddBuff(BuffType.MoonLight, this, (time + (magic.Level + 1) * 5) * 500, new Stats());

            LevelMagic(magic);
        }
        private void Trap(UserMagic magic, MapObject target, out bool cast)
        {
            cast = false;

            if (target == null || !target.IsAttackTarget(this) || !(target is MonsterObject)) return;
            if (target.Level >= Level + 2) return;

            LevelMagic(magic);
            uint duration = 60000;
            int value = (int)duration;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, value, target);
            CurrentMap.ActionList.Add(action);
            cast = true;
        }
        private bool PoisonSword(UserMagic magic)
        {
            UserItem item = GetPoison(1);
            if (item == null) return false;

            Point hitPoint;
            Cell cell;
            MirDirection dir = Functions.PreviousDir(Direction);
            int power = magic.GetDamage(GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]));

            for (int i = 0; i < 5; i++)
            {
                hitPoint = Functions.PointMove(CurrentLocation, dir, 1);
                dir = Functions.NextDir(dir);

                if (!CurrentMap.ValidPoint(hitPoint)) continue;
                cell = CurrentMap.GetCell(hitPoint);

                if (cell.Objects == null) continue;

                for (int o = 0; o < cell.Objects.Count; o++)
                {
                    MapObject target = cell.Objects[o];
                    if (target.Race != ObjectType.Player && target.Race != ObjectType.Monster) continue;
                    if (target == null || !target.IsAttackTarget(this) || target.Node == null) continue;

                    target.ApplyPoison(new Poison
                    {
                        Duration = 3 + power / 10 + magic.Level * 3,
                        Owner = this,
                        PType = PoisonType.Green,
                        TickSpeed = 1000,
                        Value = power / 10 + magic.Level + 1 + Envir.Random.Next(Stats[Stat.PoisonAttack])
                    }, this);

                    target.OperateTime = 0;
                    break;
                }
            }

            LevelMagic(magic);
            ConsumeItem(item, 1);
            return true;
        }
        private void DarkBody(MapObject target, UserMagic magic)
        {
            if (CurrentMap.Info.NoPets)
            {
                ReceiveChat("You cannot summon pets on this map.", ChatType.System);
                return;
            }
            
            if (target == null) return;

            MonsterObject monster;
            for (int i = 0; i < Pets.Count; i++)
            {
                monster = Pets[i];
                if ((monster.Info.Name != Settings.AssassinCloneName) || monster.Dead) continue;
                if (monster.Node == null) continue;
                monster.Die();
                return;
            }

            MonsterInfo info = Envir.GetMonsterInfo(Settings.AssassinCloneName);
            if (info == null) return;

            monster = MonsterObject.GetMonster(info);
            monster.Master = this;
            monster.Direction = Direction;
            monster.ActionTime = Envir.Time + 500;
            monster.RefreshNameColour(false);
            monster.Target = target;
            Pets.Add(monster);

            monster.Spawn(CurrentMap, CurrentLocation);

            if (!HasBuff(BuffType.DarkBody, out _))
            {
                LevelMagic(magic);
            }

            var duration = (GetAttackPower(BaseStats[Stat.MinAC], BaseStats[Stat.MaxAC]) + (magic.Level + 1) * 5) * 500;

            AddBuff(BuffType.DarkBody, this, duration, new Stats());
        }
        private void CrescentSlash(UserMagic magic)
        {
            int damageBase = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
            if (Envir.Random.Next(0, 100) <= Stats[Stat.Accuracy])
                damageBase += damageBase;//crit should do something like double dmg, not double max dc dmg!
            int damageFinal = magic.GetDamage(damageBase);

            MirDirection backDir = Functions.ReverseDirection(Direction);
            MirDirection preBackDir = Functions.PreviousDir(backDir);
            MirDirection nextBackDir = Functions.NextDir(backDir);

            for (int i = 0; i < 8; i++)
            {
                MirDirection dir = (MirDirection)i;
                Point hitPoint = Functions.PointMove(CurrentLocation, dir, 1);

                if (dir != backDir && dir != preBackDir && dir != nextBackDir)
                {

                    if (!CurrentMap.ValidPoint(hitPoint)) continue;

                    Cell cell = CurrentMap.GetCell(hitPoint);

                    if (cell.Objects == null) continue;


                    for (int j = 0; j < cell.Objects.Count; j++)
                    {
                        MapObject target = cell.Objects[j];
                        switch (target.Race)
                        {
                            case ObjectType.Monster:
                            case ObjectType.Player:
                                //Only targets
                                if (target.IsAttackTarget(this))
                                {
                                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + AttackSpeed, target, damageFinal, DefenceType.AC, true);
                                    ActionList.Add(action);
                                }
                                break;
                        }
                    }
                    LevelMagic(magic);
                }
            }
        }

        private void FlashDash(UserMagic magic)
        {
            bool success = false;
            ActionTime = Envir.Time;

            int travel = 0;
            bool blocked = false;
            int jumpDistance = (magic.Level <= 1) ? 0 : 1;//3 max
            Point location = CurrentLocation;
            for (int i = 0; i < jumpDistance; i++)
            {
                location = Functions.PointMove(location, Direction, 1);
                if (!CurrentMap.ValidPoint(location)) break;

                Cell cInfo = CurrentMap.GetCell(location);
                if (cInfo.Objects != null)
                {
                    for (int c = 0; c < cInfo.Objects.Count; c++)
                    {
                        MapObject ob = cInfo.Objects[c];
                        if (!ob.Blocking) continue;
                        blocked = true;
                        if ((cInfo.Objects == null) || blocked) break;
                    }
                }
                if (blocked) break;
                travel++;
            }

            jumpDistance = travel;

            if (jumpDistance > 0)
            {
                location = Functions.PointMove(CurrentLocation, Direction, jumpDistance);
                CurrentMap.GetCell(CurrentLocation).Remove(this);
                RemoveObjects(Direction, 1);
                CurrentLocation = location;
                CurrentMap.GetCell(CurrentLocation).Add(this);
                AddObjects(Direction, 1);
                Enqueue(new S.UserDashAttack { Direction = Direction, Location = location });
                Broadcast(new S.ObjectDashAttack { ObjectID = ObjectID, Direction = Direction, Location = location, Distance = jumpDistance });
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
            }

            if (travel == 0) location = CurrentLocation;

            int attackDelay = (AttackSpeed - 120) <= 300 ? 300 : (AttackSpeed - 120);
            AttackTime = Envir.Time + attackDelay;
            SpellTime = Envir.Time + 300;

            location = Functions.PointMove(location, Direction, 1);
            if (CurrentMap.ValidPoint(location))
            {
                Cell cInfo = CurrentMap.GetCell(location);
                if (cInfo.Objects != null)
                {
                    for (int c = 0; c < cInfo.Objects.Count; c++)
                    {
                        MapObject ob = cInfo.Objects[c];
                        switch (ob.Race)
                        {
                            case ObjectType.Monster:
                            case ObjectType.Player:
                                //Only targets
                                if (ob.IsAttackTarget(this))
                                {
                                    DelayedAction action = new DelayedAction(DelayedType.Damage, AttackTime, ob, magic.GetDamage(GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC])), DefenceType.AC, true);
                                    ActionList.Add(action);
                                    success = true;
                                    if ((((ob.Race != ObjectType.Player) || Settings.PvpCanResistPoison) && (Envir.Random.Next(Settings.PoisonAttackWeight) >= ob.Stats[Stat.PoisonResist])) && (Envir.Random.Next(15) <= magic.Level + 1))
                                    {
                                        DelayedAction pa = new DelayedAction(DelayedType.Poison, AttackTime, ob, PoisonType.Stun, SpellEffect.TwinDrakeBlade, magic.Level + 1, 1000);
                                        ActionList.Add(pa);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            if (success) //technicaly this makes flashdash lvl when it casts rather then when it hits (it wont lvl if it's not hitting!)
                LevelMagic(magic);

            magic.CastTime = Envir.Time;
            Enqueue(new S.MagicCast { Spell = magic.Spell });
        }
        #endregion

        #region Archer Skills
        private int ApplyArcherState(int damage)
        {
            UserMagic magic = GetMagic(Spell.MentalState);

            if (magic != null)
            {
                LevelMagic(magic);
            }

            int dmgpenalty = 100;
            switch (Info.MentalState)
            {
                case 1: //trickshot
                    dmgpenalty = 55 + (Info.MentalStateLvl * 5);
                    break;
                case 2: //group attack
                    dmgpenalty = 80;
                    break;
            }
            return (damage * dmgpenalty) / 100;
        }

        private bool StraightShot(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return false;
            if ((Info.MentalState != 1) && !CanFly(target.CurrentLocation)) return false;

            int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);
            int damage = magic.GetDamage(GetRangeAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC], distance));
            damage = ApplyArcherState(damage);

            int delay = distance * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target, target.CurrentLocation);

            ActionList.Add(action);

            return true;
        }
        private bool DoubleShot(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return false;
            if ((Info.MentalState != 1) && !CanFly(target.CurrentLocation)) return false;

            int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);
            int damage = magic.GetDamage(GetRangeAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC], distance));
            damage = ApplyArcherState(damage);

            int delay = distance * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target, target.CurrentLocation);

            ActionList.Add(action);

            action = new DelayedAction(DelayedType.Magic, Envir.Time + delay + 50, magic, damage, target, target.CurrentLocation);

            ActionList.Add(action);

            return true;
        }
        private void BackStep(UserMagic magic)
        {
            ActionTime = Envir.Time;
            if (!CanWalk) return;

            int travel = 0;
            bool blocked = false;
            int jumpDistance = (magic.Level == 0) ? 1 : magic.Level;//3 max
            MirDirection jumpDir = Functions.ReverseDirection(Direction);
            Point location = CurrentLocation;
            for (int i = 0; i < jumpDistance; i++)
            {
                location = Functions.PointMove(location, jumpDir, 1);
                if (!CurrentMap.ValidPoint(location)) break;

                Cell cInfo = CurrentMap.GetCell(location);
                if (cInfo.Objects != null)
                    for (int c = 0; c < cInfo.Objects.Count; c++)
                    {
                        MapObject ob = cInfo.Objects[c];
                        if (!ob.Blocking) continue;
                        blocked = true;
                        if ((cInfo.Objects == null) || blocked) break;
                    }
                if (blocked) break;
                travel++;
            }

            jumpDistance = travel;
            if (jumpDistance > 0)
            {
                for (int i = 0; i < jumpDistance; i++)
                {
                    location = Functions.PointMove(CurrentLocation, jumpDir, 1);
                    CurrentMap.GetCell(CurrentLocation).Remove(this);
                    RemoveObjects(jumpDir, 1);
                    CurrentLocation = location;
                    CurrentMap.GetCell(CurrentLocation).Add(this);
                    AddObjects(jumpDir, 1);
                }
                Enqueue(new S.UserBackStep { Direction = Direction, Location = location });
                Broadcast(new S.ObjectBackStep { ObjectID = ObjectID, Direction = Direction, Location = location, Distance = jumpDistance });
                LevelMagic(magic);
            }
            else
            {
                Broadcast(new S.ObjectBackStep { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Distance = jumpDistance });
                ReceiveChat(GameLanguage.Get("Not enough jumping power."), ChatType.System);
            }

            magic.CastTime = Envir.Time;
            Enqueue(new S.MagicCast { Spell = magic.Spell });

            CellTime = Envir.Time + 500;
        }
        private bool DelayedExplosion(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this) || !CanFly(target.CurrentLocation)) return false;

            int power = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));
            int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, power, target, target.CurrentLocation);
            ActionList.Add(action);
            return true;
        }
        private void ExplosiveTrap(UserMagic magic, Point location)
        {
            int freeTrapSpot = -1;

            var trapIDs = CurrentMap.GetSpellObjects(Spell.ExplosiveTrap, this).Select(x => x.ExplosiveTrapID).Distinct();

            var max = magic.Level + 1;

            if (trapIDs.Count() >= max) return;

            for (int i = 0; i < max; i++)
            {
                if (!trapIDs.Contains(i))
                {
                    freeTrapSpot = i;
                    break;
                }
            }

            if (freeTrapSpot == -1) return;

            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));
            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, location, freeTrapSpot);
            CurrentMap.ActionList.Add(action);
        }

        public void DoKnockback(MapObject target, UserMagic magic)//ElementalShot - knockback
        {
            Cell cell = CurrentMap.GetCell(target.CurrentLocation);
            if (!cell.Valid || cell.Objects == null) return;

            if (target.CurrentLocation.Y < 0 || target.CurrentLocation.Y >= CurrentMap.Height || target.CurrentLocation.X < 0 || target.CurrentLocation.X >= CurrentMap.Height) return;

            if (target.Race != ObjectType.Monster && target.Race != ObjectType.Player && target.Race != ObjectType.Hero) return;
            if (!target.IsAttackTarget(this) || target.Level >= Level) return;

            if (Envir.Random.Next(20) >= 6 + magic.Level * 3 + ElementsLevel + Level - target.Level) return;
            int distance = 1 + Math.Max(0, magic.Level - 1) + Envir.Random.Next(2);
            MirDirection dir = Functions.DirectionFromPoint(CurrentLocation, target.CurrentLocation);

            target.Pushed(this, dir, distance);
        }
        public void BindingShot(UserMagic magic, MapObject target, out bool cast)
        {
            cast = false;

            if (target == null || !target.IsAttackTarget(this) || !(target is MonsterObject)) return;
            if ((Info.MentalState != 1) && !CanFly(target.CurrentLocation)) return;
            if (target.Level > Level + 2) return;
            if (((MonsterObject)target).ShockTime >= Envir.Time) return;//Already shocked


            uint duration = (uint)((magic.Level * 5 + 10) * 1000);
            int value = (int)duration;
            int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, value, target, target.CurrentLocation);
            ActionList.Add(action);

            cast = true;
        }
        public void SpecialArrowShot(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return;
            if ((Info.MentalState != 1) && !CanFly(target.CurrentLocation)) return;

            int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);
            int damage = magic.GetDamage(GetRangeAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC], distance));
            damage = ApplyArcherState(damage);

            int delay = distance * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target, target.CurrentLocation);
            ActionList.Add(action);
        }
        public void NapalmShot(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return;
            if ((Info.MentalState != 1) && !CanFly(target.CurrentLocation)) return;

            int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);
            int damage = magic.GetDamage(GetRangeAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC], distance));
            damage = ApplyArcherState(damage);

            int delay = distance * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, this, magic, damage, target.CurrentLocation);
            CurrentMap.ActionList.Add(action);
        }
        public void ArcherSummon(UserMagic magic, MapObject target, Point location)
        {
            if (CurrentMap.Info.NoPets)
            {
                ReceiveChat("You cannot summon pets on this map.", ChatType.System);
                return;
            }
            
            if (target != null && target.IsAttackTarget(this))
                location = target.CurrentLocation;
            if (!CanFly(location)) return;

            uint duration = (uint)((magic.Level * 5 + 10) * 1000);
            int value = (int)duration;
            int delay = Functions.MaxDistance(CurrentLocation, location) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, value, location, target);
            ActionList.Add(action);
        }

        public void ArcherSummonStone(UserMagic magic, Point location, out bool cast)
        {
            cast = false;

            if (CurrentMap.Info.NoPets)
            {
                ReceiveChat("You cannot summon pets on this map.", ChatType.System);
                return;
            }
            
            if (!CurrentMap.ValidPoint(location) ||
                !CanFly(location))
            {
                return;
            }

            if (Pets.Exists(x => x.Info.GameName == Settings.StoneName))
            {
                MonsterObject st = Pets.First(x => x.Info.GameName == Settings.StoneName);
                if (!st.Dead)
                {
                    ReceiveChat(GameLanguage.Get("You can only have 1 active {0} alive.", Settings.StoneName), ChatType.Hint);
                    return;
                }
            }

            int duration = (((magic.Level * 5) + 10) * 1000);
            int delay = Functions.MaxDistance(CurrentLocation, location) * 50 + 500; //50 MS per Step
                                                                                     //
            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, duration, location);
            ActionList.Add(action);

            cast = true;
        }

        public void OneWithNature(MapObject target, UserMagic magic)
        {
            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation);
            CurrentMap.ActionList.Add(action);
        }
        public void HealingCircle(UserMagic magic, Point location)
        {
            var damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]));
            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500 + 1200, this, magic, damage, location);
            CurrentMap.ActionList.Add(action);
        }
        #endregion

        #region Custom
        private void Portal(UserMagic magic, Point location, out bool cast)
        {
            cast = false;

            if (!CurrentMap.ValidPoint(location)) return;

            var portalCount = Envir.Spells.Count(x => x.Spell == Spell.Portal && x.Caster == this);

            if (portalCount == 2) return;

            if (!CanFly(location)) return;

            int duration = 30 + (magic.Level * 30);
            int value = duration;
            int passthroughCount = (magic.Level * 2) - 1;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, value, location, passthroughCount);
            CurrentMap.ActionList.Add(action);
            cast = true;
        }

        private bool FireBounce(MapObject target, UserMagic magic, MapObject source, int bounce = -1)
        {
            if (target == null || !target.IsAttackTarget(this) || !source.CanFly(target.CurrentLocation) || bounce == 0) return false;

            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));

            int delay = Functions.MaxDistance(source.CurrentLocation, target.CurrentLocation) * 50; //50 MS per Step

            if (bounce == -1)
            {
                bounce = magic.Level + 2;
                delay += 500;
            }
            else
            {
                CurrentMap.Broadcast(new S.ObjectProjectile { Spell = magic.Info.Spell, Source = source.ObjectID, Destination = target.ObjectID }, source.CurrentLocation);
            }

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target, target.CurrentLocation, bounce);
            ActionList.Add(action);

            return true;
        }

        private bool MeteorShower(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this) || !CanFly(target.CurrentLocation)) return false;

            int damage = magic.GetDamage(GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]));

            int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500; //50 MS per Step

            var targetIDs = new List<uint>();

            if (target.Race == ObjectType.Monster)
            {
                List<MapObject> targets = ((MonsterObject)target).FindAllNearby(4, target.CurrentLocation);

                int secondaryTargetCount = targets.Count > 3 ? 3 : targets.Count;

                for (int i = 0; i < secondaryTargetCount; i++)
                {
                    if (targets[i] == target || !targets[i].IsAttackTarget(this)) continue;

                    DelayedAction action2 = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage / 2, targets[i], targets[i].CurrentLocation);
                    ActionList.Add(action2);

                    targetIDs.Add(targets[i].ObjectID);
                }
            }

            Broadcast(new S.ObjectMagic { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Spell = magic.Info.Spell, TargetID = target.ObjectID, Target = target.CurrentLocation, Cast = true, Level = magic.Level, SecondaryTargetIDs = targetIDs });
            Enqueue(new S.Magic { Spell = Spell.MeteorShower, TargetID = target.ObjectID, Target = target.CurrentLocation, Cast = true, Level = magic.Level, SecondaryTargetIDs = targetIDs });

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target, target.CurrentLocation);
            ActionList.Add(action);

            return true;
        }

        #endregion

        private void CheckSneakRadius()
        {
            if (!Sneaking) return;

            for (int y = CurrentLocation.Y - 3; y <= CurrentLocation.Y + 3; y++)
            {
                if (y < 0) continue;
                if (y >= CurrentMap.Height) break;

                for (int x = CurrentLocation.X - 3; x <= CurrentLocation.X + 3; x++)
                {
                    if (x < 0) continue;
                    if (x >= CurrentMap.Width) break;

                    Cell cell = CurrentMap.GetCell(x, y);
                    if (!cell.Valid || cell.Objects == null) continue;

                    for (int i = 0; cell.Objects != null && i < cell.Objects.Count; i++)
                    {
                        MapObject ob = cell.Objects[i];
                        if ((ob.Race != ObjectType.Player) || ob == this) continue;

                        SneakingActive = false;
                        return;
                    }
                }
            }

            SneakingActive = true;
        }
        protected void CompleteMagic(IList<object> data)
        {
            UserMagic magic = (UserMagic)data[0];
            int value;
            MapObject target;
            Point targetLocation;
            Point location;
            MonsterObject monster;

            switch (magic.Spell)
            {
                #region FireBall, GreatFireBall, ThunderBolt, SoulFireBall, FlameDisruptor

                case Spell.FireBall:
                case Spell.GreatFireBall:
                case Spell.ThunderBolt:
                case Spell.SoulFireBall:
                case Spell.FlameDisruptor:
                case Spell.StraightShot:
                case Spell.DoubleShot:
                case Spell.MeteorShower:
                    value = (int)data[1];
                    target = (MapObject)data[2];
                    targetLocation = (Point)data[3];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null || !Functions.InRange(target.CurrentLocation, targetLocation, 2)) return;
                    if (target.Attacked(this, value, DefenceType.MAC, false) > 0) LevelMagic(magic);
                    break;

                #endregion

                #region FireBounce

                case Spell.FireBounce:
                    value = (int)data[1];
                    target = (MapObject)data[2];
                    targetLocation = (Point)data[3];
                    int bounce = (int)data[4];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null || !Functions.InRange(target.CurrentLocation, targetLocation, 2)) return;
                    if (target.Attacked(this, value, DefenceType.MAC, false) > 0) LevelMagic(magic);

                    if (target.Race == ObjectType.Monster)
                    {
                        var targets = ((MonsterObject)target).FindAllNearby(3, target.CurrentLocation).Where(x => x != target && x.IsAttackTarget(this) && target.CanFly(x.CurrentLocation)).ToList();

                        if (targets.Count > 0)
                        {
                            var nextTarget = targets[Envir.Random.Next(targets.Count)];

                            this.FireBounce(nextTarget, magic, target, --bounce);
                        }
                    }

                    break;

                #endregion

                #region FrostCrunch
                case Spell.FrostCrunch:
                    value = (int)data[1];
                    target = (MapObject)data[2];
                    targetLocation = (Point)data[3];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null || !Functions.InRange(target.CurrentLocation, targetLocation, 2)) return;
                    if (target.Attacked(this, value, DefenceType.MAC, false) > 0)
                    {
                        if (Level + (target.Race == ObjectType.Player ? 2 : 10) >= target.Level && Envir.Random.Next(target.Race == ObjectType.Player ? 100 : 20) <= magic.Level)
                        {
                            target.ApplyPoison(new Poison
                            {
                                Owner = this,
                                Duration = target.Race == ObjectType.Player ? 4 : 5 + Envir.Random.Next(5),
                                PType = PoisonType.Slow,
                                TickSpeed = 1000,
                            }, this);
                            target.OperateTime = 0;
                        }

                        if (Level + (target.Race == ObjectType.Player ? 2 : 10) >= target.Level && Envir.Random.Next(target.Race == ObjectType.Player ? 100 : 40) <= magic.Level)
                        {
                            target.ApplyPoison(new Poison
                            {
                                Owner = this,
                                Duration = target.Race == ObjectType.Player ? 2 : 5 + Envir.Random.Next(Stats[Stat.Freezing]),
                                PType = PoisonType.Frozen,
                                TickSpeed = 1000,
                            }, this);
                            target.OperateTime = 0;
                        }

                        LevelMagic(magic);
                    }
                    break;

                #endregion

                #region Vampirism

                case Spell.Vampirism:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
                    value = target.Attacked(this, value, DefenceType.MAC, false);
                    if (value == 0) return;
                    LevelMagic(magic);
                    if (VampAmount == 0) VampTime = Envir.Time + 1000;
                    VampAmount += (ushort)(value * (magic.Level + 1) * 0.25F);
                    break;

                #endregion

                #region Healing

                case Spell.Healing:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsFriendlyTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
                    if (target.Health >= target.MaxHealth) return;
                    target.HealAmount = (ushort)Math.Min(ushort.MaxValue, target.HealAmount + value);
                    target.OperateTime = 0;
                    LevelMagic(magic);
                    break;

                #endregion

                #region ElectricShock

                case Spell.ElectricShock:
                    monster = (MonsterObject)data[1];
                    if (monster == null || !monster.IsAttackTarget(this) || monster.CurrentMap != CurrentMap || monster.Node == null) return;
                    ElectricShock(monster, magic);
                    break;

                #endregion

                #region Poisoning

                case Spell.Poisoning:
                    value = (int)data[1];
                    target = (MapObject)data[2];
                    UserItem item = (UserItem)data[3];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

                    switch (item.Info.Shape)
                    {
                        case 1:
                            target.ApplyPoison(new Poison
                            {
                                Duration = (value * 2) + ((magic.Level + 1) * 7),
                                Owner = this,
                                PType = PoisonType.Green,
                                TickSpeed = 2000,
                                Value = value / 15 + magic.Level + 1 + Envir.Random.Next(Stats[Stat.PoisonAttack])
                            }, this);
                            break;
                        case 2:
                            target.ApplyPoison(new Poison
                            {
                                Duration = (value * 2) + (magic.Level + 1) * 7,
                                Owner = this,
                                PType = PoisonType.Red,
                                TickSpeed = 2000,
                            }, this);
                            break;
                    }
                    target.OperateTime = 0;

                    LevelMagic(magic);
                    break;

                #endregion

                #region StormEscape
                case Spell.StormEscape:
                    location = (Point)data[1];
                    if (CurrentMap.Info.NoTeleport)
                    {
                        ReceiveChat(GameLanguage.Get("You cannot teleport on this map"), ChatType.System);
                        return;
                    }
                    if (!CurrentMap.ValidPoint(location) || Envir.Random.Next(4) >= magic.Level + 1 || !Teleport(CurrentMap, location, false)) return;
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.StormEscape }, CurrentLocation);

                    AddBuff(BuffType.TemporalFlux, this, Settings.Second * 30, new Stats { [Stat.TeleportManaPenaltyPercent] = 30 });
                    LevelMagic(magic);
                    break;
                #endregion

                #region Teleport
                case Spell.Teleport:                                 
                    if (CurrentMap.Info.NoTeleport)
                    {
                        ReceiveChat(GameLanguage.Get("You cannot teleport on this map"), ChatType.System);
                        return;
                    }

                    if (!MagicTeleport(magic))
                        return;                    

                    AddBuff(BuffType.TemporalFlux, this, Settings.Second * 30, new Stats { [Stat.TeleportManaPenaltyPercent] = 30 });
                    LevelMagic(magic);

                    break;
                #endregion

                #region Blink

                case Spell.Blink:
                    {
                        location = (Point)data[1];
                        if (CurrentMap.Info.NoTeleport)
                        {
                            ReceiveChat(GameLanguage.Get("You cannot teleport on this map"), ChatType.System);
                            return;
                        }
                        if (Functions.InRange(CurrentLocation, location, magic.Info.Range) == false) return;
                        if (!CurrentMap.ValidPoint(location) || Envir.Random.Next(4) >= magic.Level + 1 || !Teleport(CurrentMap, location, false)) return;
                        CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Teleport }, CurrentLocation);
                        LevelMagic(magic);

                        AddBuff(BuffType.TemporalFlux, this, Settings.Second * 30, new Stats { [Stat.TeleportManaPenaltyPercent] = 30 });
                    }
                    break;

                #endregion

                #region Hiding

                case Spell.Hiding:
                    {
                        value = (int)data[1];

                        AddBuff(BuffType.Hiding, this, Settings.Second * value, new Stats());

                        LevelMagic(magic);
                    }
                    break;

                #endregion

                #region Haste

                case Spell.Haste:
                    {
                        AddBuff(BuffType.Haste, this, (Settings.Second * 25) + (Settings.Second * magic.Level * 15), new Stats { [Stat.AttackSpeed] = (magic.Level * 2) + 2 });
                        LevelMagic(magic);
                    }
                    break;

                #endregion

                #region Fury

                case Spell.Fury:
                    {
                        AddBuff(BuffType.Fury, this, (Settings.Second * 60) + (magic.Level * 10000), new Stats { [Stat.AttackSpeed] = 4 });
                        LevelMagic(magic);
                    }
                    break;

                #endregion

                #region ImmortalSkin

                case Spell.ImmortalSkin:
                    {
                        var stats = new Stats
                        {
                            [Stat.MaxDC] = (int)Math.Round(Stats[Stat.MaxDC] * (0.05 + (0.01 * magic.Level))) * -1,
                            [Stat.MaxAC] = (int)Math.Round(BaseStats[Stat.MaxAC] * (0.10 + (0.07 * magic.Level)))
                        };

                        AddBuff(BuffType.ImmortalSkin, this, (Settings.Second * 60) + (magic.Level * 1000), stats);
                        LevelMagic(magic);
                    }
                    break;
                #endregion

                #region LightBody

                case Spell.LightBody:
                    {
                        AddBuff(BuffType.LightBody, this, (magic.Level + 1) * (Settings.Second * 30), new Stats { [Stat.Agility] = (magic.Level + 1) * 2 });
                        LevelMagic(magic);
                    }
                    break;

                #endregion

                #region MagicShield

                case Spell.MagicShield:
                    {
                        if (HasBuff(BuffType.MagicShield, out _)) return;

                        LevelMagic(magic);
                        AddBuff(BuffType.MagicShield, this, Settings.Second * (int)data[1], new Stats { [Stat.DamageReductionPercent] = (magic.Level + 2) * 10 });
                    }
                    break;

                #endregion

                #region TurnUndead

                case Spell.TurnUndead:
                    {
                        monster = (MonsterObject)data[1];
                        if (monster == null || !monster.IsAttackTarget(this) || monster.CurrentMap != CurrentMap || monster.Node == null) return;
                        monster.LastHitter = this;
                        monster.LastHitTime = Envir.Time + 5000;
                        monster.EXPOwner = this;
                        monster.EXPOwnerTime = Envir.Time + 5000;
                        monster.Die();
                        LevelMagic(magic);
                    }
                    break;

                #endregion

                #region MagicBooster

                case Spell.MagicBooster:
                    {
                        var stats = new Stats
                        {
                            [Stat.MinMC] = (int)data[1],
                            [Stat.MaxMC] = (int)data[1],
                            [Stat.ManaPenaltyPercent] = 6 + magic.Level
                        };

                        AddBuff(BuffType.MagicBooster, this, Settings.Second * 60, stats, true);
                        LevelMagic(magic);
                    }
                    break;

                #endregion

                #region Purification

                case Spell.Purification:
                    target = (MapObject)data[1];

                    if (target == null || !target.IsFriendlyTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
                    if (Envir.Random.Next(4) > magic.Level) return;

                    for (int i = 0; i < target.Buffs.Count; i++)
                    {
                        var buff = target.Buffs[i];

                        if (!buff.Properties.HasFlag(BuffProperty.Debuff)) continue;

                        target.RemoveBuff(buff.Type);
                    }

                    if (target.PoisonList.Any(x => x.PType == PoisonType.DelayedExplosion))
                    {
                        target.ExplosionInflictedTime = 0;
                        target.ExplosionInflictedStage = 0;

                        if (target.ObjectID == ObjectID)
                        {
                            Enqueue(new S.RemoveDelayedExplosion { ObjectID = target.ObjectID });
                        }

                        target.Broadcast(new S.RemoveDelayedExplosion { ObjectID = target.ObjectID });
                    }

                    target.PoisonList.Clear();
                    target.OperateTime = 0;

                    LevelMagic(magic);
                    break;

                #endregion

                #region Revelation

                case Spell.Revelation:
                    value = (int)data[1];
                    target = (MapObject)data[2];
                    if (target == null || target.CurrentMap != CurrentMap || target.Node == null) return;
                    if (target.Race != ObjectType.Player && target.Race != ObjectType.Monster) return;
                    if (Envir.Random.Next(4) > magic.Level || Envir.Time < target.RevTime) return;

                    target.RevTime = Envir.Time + value * 1000;
                    target.OperateTime = 0;
                    target.BroadcastHealthChange();

                    LevelMagic(magic);
                    break;

                #endregion

                #region Reincarnation

                case Spell.Reincarnation:

                    if (ReincarnationReady)
                    {
                        ReincarnationTarget.Enqueue(new S.RequestReincarnation { });
                        LevelMagic(magic);
                    }
                    break;

                #endregion

                #region Entrapment

                case Spell.Entrapment:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null || target.Race != ObjectType.Monster ||
                        Functions.MaxDistance(CurrentLocation, target.CurrentLocation) > 7 || target.Level >= Level + 5 + Envir.Random.Next(8)) return;

                    MirDirection pulldirection = (MirDirection)((byte)(Direction - 4) % 8);
                    int pulldistance = 0;
                    if ((byte)pulldirection % 2 > 0)
                        pulldistance = Math.Max(0, Math.Min(Math.Abs(CurrentLocation.X - target.CurrentLocation.X), Math.Abs(CurrentLocation.Y - target.CurrentLocation.Y)));
                    else
                        pulldistance = pulldirection == MirDirection.Up || pulldirection == MirDirection.Down ? Math.Abs(CurrentLocation.Y - target.CurrentLocation.Y) - 2 : Math.Abs(CurrentLocation.X - target.CurrentLocation.X) - 2;

                    int levelgap = target.Race == ObjectType.Player ? Level - target.Level + 4 : Level - target.Level + 9;
                    if (Envir.Random.Next(30) >= ((magic.Level + 1) * 3) + levelgap) return;

                    int duration = target.Race == ObjectType.Player ? (int)Math.Round((magic.Level + 1) * 1.6) : (int)Math.Round((magic.Level + 1) * 0.8);
                    if (duration > 0) target.ApplyPoison(new Poison { PType = PoisonType.Paralysis, Duration = duration, TickSpeed = 1000 }, this);
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = target.ObjectID, Effect = SpellEffect.Entrapment }, target.CurrentLocation);
                    if (target.Pushed(this, pulldirection, pulldistance) > 0) LevelMagic(magic);
                    break;

                #endregion

                #region Hallucination

                case Spell.Hallucination:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null ||
                        Functions.MaxDistance(CurrentLocation, target.CurrentLocation) > 7 || Envir.Random.Next(Level + 20 + magic.Level * 5) <= target.Level + 10) return;
                    item = GetAmulet(1);
                    if (item == null) return;

                    ((MonsterObject)target).HallucinationTime = Envir.Time + (Envir.Random.Next(20) + 10) * 1000;
                    target.Target = null;

                    ConsumeItem(item, 1);

                    LevelMagic(magic);
                    break;

                #endregion

                #region PetEnhancer

                case Spell.PetEnhancer:
                    {
                        value = (int)data[1];
                        target = (MonsterObject)data[2];

                        if (target.Node == null) return;

                        int dcInc = 2 + target.Level * 2;
                        int acInc = 4 + target.Level;

                        var stats = new Stats
                        {
                            [Stat.MinDC] = dcInc,
                            [Stat.MaxDC] = dcInc,
                            [Stat.MinAC] = acInc,
                            [Stat.MaxAC] = acInc
                        };

                        target.AddBuff(BuffType.PetEnhancer, this, (Settings.Second * value), stats);
                        LevelMagic(magic);
                    }
                    break;

                #endregion

                #region CatTongue 
                case Spell.CatTongue:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
                    if (target.Attacked(this, value, DefenceType.AC, false) > 0)
                    {
                        int rnd = Envir.Random.Next(10);
                        if (rnd >= 8)
                        {
                            if (target.Race == ObjectType.Player && Settings.PvpCanFreeze)
                                target.ApplyPoison(new Poison { PType = PoisonType.Frozen, Duration = (magic.Level + 1) * 3, TickSpeed = 1000 }, this);
                            else
                                rnd -= 4;
                        }

                        if (rnd <= 4)
                            target.ApplyPoison(new Poison { PType = PoisonType.Stun, Duration = (magic.Level + 1) * 3, TickSpeed = 1000 }, this);
                        else if (rnd <= 7)
                            target.ApplyPoison(new Poison { PType = PoisonType.Slow, Duration = (magic.Level + 1) * 3, TickSpeed = 1000 }, this);

                        LevelMagic(magic);
                    }
                    break;
                #endregion

                #region ElementalBarrier, ElementalShot

                case Spell.ElementalBarrier:
                    {
                        if (HasBuff(BuffType.ElementalBarrier, out _)) return;

                        if (!HasElemental)
                        {
                            ObtainElement(true);
                            LevelMagic(magic);
                            return;
                        }

                        int barrierPower = GetElementalOrbPower(true);//defensive orbpower
                                                                      //destroy orbs
                        ElementsLevel = 0;
                        ObtainElement(false);
                        LevelMagic(magic);

                        AddBuff(BuffType.ElementalBarrier, this, Settings.Second * ((int)data[1] + barrierPower), new Stats { [Stat.DamageReductionPercent] = (magic.Level + 1) * 10 });
                        CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.ElementalBarrierUp }, CurrentLocation);
                    }
                    break;

                case Spell.ElementalShot:
                    value = (int)data[1];
                    target = (MapObject)data[2];
                    targetLocation = (Point)data[3];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null || !Functions.InRange(target.CurrentLocation, targetLocation, 2))
                    {
                        //destroy orbs
                        ElementsLevel = 0;
                        ObtainElement(false);//update and send to client
                        return;
                    }
                    if (target.Attacked(this, value, DefenceType.MAC, false) > 0)
                        LevelMagic(magic);
                    DoKnockback(target, magic);//ElementalShot - Knockback

                    //destroy orbs
                    ElementsLevel = 0;
                    ObtainElement(false);//update and send to client
                    break;

                #endregion

                #region DelayedExplosion

                case Spell.DelayedExplosion:
                    value = (int)data[1];
                    target = (MapObject)data[2];
                    targetLocation = (Point)data[3];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null || !Functions.InRange(target.CurrentLocation, targetLocation, 2)) return;
                    if (target.Attacked(this, value, DefenceType.MAC, false) > 0) LevelMagic(magic);

                    target.ApplyPoison(new Poison
                    {
                        Duration = (value * 2) + (magic.Level + 1) * 7,
                        Owner = this,
                        PType = PoisonType.DelayedExplosion,
                        TickSpeed = 2000,
                        Value = value
                    }, this);

                    target.OperateTime = 0;
                    LevelMagic(magic);
                    break;

                #endregion

                #region BindingShot

                case Spell.BindingShot:
                    value = (int)data[1];
                    target = (MapObject)data[2];
                    targetLocation = (Point)data[3];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null || !Functions.InRange(target.CurrentLocation, targetLocation, 2)) return;
                    if (((MonsterObject)target).ShockTime >= Envir.Time) return;//Already shocked

                    Point place = target.CurrentLocation;
                    MonsterObject centerTarget = null;

                    for (int y = place.Y - 1; y <= place.Y + 1; y++)
                    {
                        if (y < 0) continue;
                        if (y >= CurrentMap.Height) break;

                        for (int x = place.X - 1; x <= place.X + 1; x++)
                        {
                            if (x < 0) continue;
                            if (x >= CurrentMap.Width) break;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject targetob = cell.Objects[i];

                                if (y == place.Y && x == place.X && targetob.Race == ObjectType.Monster)
                                {
                                    centerTarget = (MonsterObject)targetob;
                                }

                                switch (targetob.Race)
                                {
                                    case ObjectType.Monster:
                                        if (targetob == null || !targetob.IsAttackTarget(this) || targetob.Node == null || targetob.Level > this.Level + 2) continue;

                                        MonsterObject mobTarget = (MonsterObject)targetob;

                                        if (centerTarget == null) centerTarget = mobTarget;

                                        mobTarget.ShockTime = Envir.Time + value;
                                        mobTarget.Target = null;
                                        break;
                                }
                            }
                        }
                    }

                    if (centerTarget == null) return;

                    //only the centertarget holds the effect
                    centerTarget.BindingShotCenter = true;
                    centerTarget.Broadcast(new S.SetBindingShot { ObjectID = centerTarget.ObjectID, Enabled = true, Value = value });

                    LevelMagic(magic);
                    break;

                #endregion

                #region VampireShot, PoisonShot, CrippleShot
                case Spell.VampireShot:
                case Spell.PoisonShot:
                case Spell.CrippleShot:
                    value = (int)data[1];
                    target = (MapObject)data[2];
                    targetLocation = (Point)data[3];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null || !Functions.InRange(target.CurrentLocation, targetLocation, 2)) return;
                    if (target.Attacked(this, value, DefenceType.MAC, false) == 0) return;

                    int buffTime = 5 + (5 * magic.Level);

                    bool hasVampBuff = HasBuff(BuffType.VampireShot, out _);
                    bool hasPoisonBuff = HasBuff(BuffType.PoisonShot, out _);

                    bool doVamp = false, doPoison = false;
                    if (magic.Spell == Spell.VampireShot)
                    {
                        doVamp = true;
                        if (!hasVampBuff && !hasPoisonBuff && (Envir.Random.Next(20) >= 8))//40% chance
                        {
                            AddBuff(BuffType.VampireShot, this, Settings.Second * buffTime, new Stats());
                            BroadcastInfo();
                        }
                    }
                    if (magic.Spell == Spell.PoisonShot)
                    {
                        doPoison = true;
                        if (!hasPoisonBuff && !hasVampBuff && (Envir.Random.Next(20) >= 8))//40% chance
                        {
                            AddBuff(BuffType.PoisonShot, this, Settings.Second * buffTime, new Stats());
                            BroadcastInfo();
                        }
                    }
                    if (magic.Spell == Spell.CrippleShot)
                    {
                        if (hasVampBuff || hasPoisonBuff)
                        {
                            place = target.CurrentLocation;
                            for (int y = place.Y - 1; y <= place.Y + 1; y++)
                            {
                                if (y < 0) continue;
                                if (y >= CurrentMap.Height) break;
                                for (int x = place.X - 1; x <= place.X + 1; x++)
                                {
                                    if (x < 0) continue;
                                    if (x >= CurrentMap.Width) break;
                                    Cell cell = CurrentMap.GetCell(x, y);
                                    if (!cell.Valid || cell.Objects == null) continue;
                                    for (int i = 0; i < cell.Objects.Count; i++)
                                    {
                                        MapObject targetob = cell.Objects[i];
                                        if (targetob.Race != ObjectType.Monster && targetob.Race != ObjectType.Player) continue;
                                        if (targetob == null || !targetob.IsAttackTarget(this) || targetob.Node == null) continue;
                                        if (targetob.Dead) continue;

                                        if (hasVampBuff)//Vampire Effect
                                        {
                                            //cancel out buff
                                            AddBuff(BuffType.VampireShot, this, 0, new Stats());

                                            target.Attacked(this, value, DefenceType.MAC, false);
                                            if (VampAmount == 0) VampTime = Envir.Time + Settings.Second;
                                            VampAmount += (ushort)(value * (magic.Level + 1) * 0.25F);
                                        }
                                        if (hasPoisonBuff)//Poison Effect
                                        {
                                            //cancel out buff
                                            AddBuff(BuffType.PoisonShot, this, 0, new Stats());

                                            targetob.ApplyPoison(new Poison
                                            {
                                                Duration = (value * 2) + (magic.Level + 1) * 7,
                                                Owner = this,
                                                PType = PoisonType.Green,
                                                TickSpeed = 2000,
                                                Value = value / 25 + magic.Level + 1 + Envir.Random.Next(Stats[Stat.PoisonAttack])
                                            }, this);
                                            targetob.OperateTime = 0;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (doVamp)//Vampire Effect
                        {
                            if (VampAmount == 0) VampTime = Envir.Time + 1000;
                            VampAmount += (ushort)(value * (magic.Level + 1) * 0.25F);
                        }
                        if (doPoison)//Poison Effect
                        {
                            target.ApplyPoison(new Poison
                            {
                                Duration = (value * 2) + (magic.Level + 1) * 7,
                                Owner = this,
                                PType = PoisonType.Green,
                                TickSpeed = 2000,
                                Value = value / 25 + magic.Level + 1 + Envir.Random.Next(Stats[Stat.PoisonAttack])
                            }, this);
                            target.OperateTime = 0;
                        }
                    }

                    LevelMagic(magic);
                    break;
                #endregion

                #region ArcherSummons
                case Spell.SummonVampire:
                case Spell.SummonToad:
                case Spell.SummonSnakes:
                    value = (int)data[1];
                    location = (Point)data[2];
                    target = (MapObject)data[3];

                    int SummonType = 0;
                    switch (magic.Spell)
                    {
                        case Spell.SummonVampire:
                            SummonType = 1;
                            break;
                        case Spell.SummonToad:
                            SummonType = 2;
                            break;
                        case Spell.SummonSnakes:
                            SummonType = 3;
                            break;
                    }
                    if (SummonType == 0) return;

                    for (int i = 0; i < Pets.Count; i++)
                    {
                        monster = Pets[i];
                        if ((monster.Info.Name != (SummonType == 1 ? Settings.VampireName : (SummonType == 2 ? Settings.ToadName : Settings.SnakeTotemName))) || monster.Dead) continue;
                        if (monster.Node == null) continue;
                        monster.ActionList.Add(new DelayedAction(DelayedType.Recall, Envir.Time + 500, target));
                        monster.Target = target;
                        return;
                    }

                    if (Pets.Count(x => x.Race == ObjectType.Monster) >= 2) return;

                    //left it in for future summon amulets
                    //UserItem item = GetAmulet(5);
                    //if (item == null) return;

                    MonsterInfo info = Envir.GetMonsterInfo((SummonType == 1 ? Settings.VampireName : (SummonType == 2 ? Settings.ToadName : Settings.SnakeTotemName)));
                    if (info == null) return;

                    LevelMagic(magic);
                    //ConsumeItem(item, 5);

                    monster = MonsterObject.GetMonster(info);
                    monster.PetLevel = magic.Level;
                    monster.Master = this;
                    monster.MaxPetLevel = (byte)(1 + magic.Level * 2);
                    monster.Direction = Direction;
                    monster.ActionTime = Envir.Time + 1000;
                    monster.Target = target;

                    if (SummonType == 1)
                        ((Monsters.VampireSpider)monster).AliveTime = Envir.Time + ((magic.Level * 1500) + 15000);
                    if (SummonType == 2)
                        ((Monsters.SpittingToad)monster).AliveTime = Envir.Time + ((magic.Level * 2000) + 25000);
                    if (SummonType == 3)
                        ((Monsters.SnakeTotem)monster).AliveTime = Envir.Time + ((magic.Level * 1500) + 20000);

                    //Pets.Add(monster);

                    DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, monster, location);
                    CurrentMap.ActionList.Add(action);
                    break;
                case Spell.Stonetrap:
                    {
                        duration = (int)data[1];
                        location = (Point)data[2];

                        if (Pets.Where(x => x.Race == ObjectType.Monster).Count() >= magic.Level + 1) return;

                        MonsterInfo mInfo = Envir.GetMonsterInfo(Settings.StoneName);
                        if (mInfo == null) return;

                        LevelMagic(magic);

                        monster = MonsterObject.GetMonster(mInfo);

                        monster.Master = this;
                        monster.MaxPetLevel = (byte)(1 + magic.Level * 2);
                        monster.Direction = Direction;
                        monster.ActionTime = Envir.Time + 1000;

                        StoneTrap st = monster as StoneTrap;
                        st.DieTime = Envir.Time + duration;

                        DelayedAction act = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, monster, location);
                        CurrentMap.ActionList.Add(act);
                        break;
                    }
                #endregion

                #region SlashingBurst

                case Spell.SlashingBurst:
                    SlashingBurst(magic);
                    break;

                    #endregion

            }
        }
        protected void CompleteMine(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            if (target == null) return;
            target.Broadcast(new S.MapEffect { Effect = SpellEffect.Mine, Location = target.CurrentLocation, Value = (byte)Direction });
            //target.Broadcast(new S.ObjectEffect { ObjectID = target.ObjectID, Effect = SpellEffect.Mine });
            if ((byte)target.Direction < 6)
                target.Direction++;
            target.Broadcast(target.GetInfo());
        }
        protected void CompleteAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];
            bool damageWeapon = (bool)data[3];
            UserMagic userMagic = null;
            bool finalHit = false;
            if (data.Count >= 5)
                userMagic = (UserMagic)data[4];
            if (data.Count >= 6)
                finalHit = (bool)data[5];
            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            if (FatalSword)
                defence = DefenceType.Agility;

            if (target.Attacked(this, damage, defence, damageWeapon) <= 0) return;
            if (FatalSword)
            {
                S.ObjectEffect p = new S.ObjectEffect { ObjectID = target.ObjectID, Effect = SpellEffect.FatalSword };
                CurrentMap.Broadcast(p, target.CurrentLocation);
                FatalSword = false;
                var magic = GetMagic(Spell.FatalSword);
                if (magic != null) LevelMagic(magic);
            }
            if (userMagic != null && finalHit)
            {
                if (userMagic.Spell == Spell.TwinDrakeBlade)
                {
                    if ((((target.Race != ObjectType.Player) || Settings.PvpCanResistPoison) &&
                        (Envir.Random.Next(Settings.PoisonAttackWeight) >= target.Stats[Stat.PoisonResist])) &&
                        (target.Level < Level + 10 && Envir.Random.Next(target.Race == ObjectType.Player ? 40 : 20) <= userMagic.Level + 1))
                    {
                        target.ApplyPoison(new Poison { PType = PoisonType.Stun, Duration = target.Race == ObjectType.Player ? 2 : 2 + userMagic.Level, TickSpeed = 1000 }, this);
                        target.Broadcast(new S.ObjectEffect { ObjectID = target.ObjectID, Effect = SpellEffect.TwinDrakeBlade });
                    }
                }
            }

            //Level Fencing / SpiritSword
            foreach (UserMagic magic in Info.Magics)
            {
                switch (magic.Spell)
                {
                    case Spell.Fencing:
                    case Spell.SpiritSword:
                        LevelMagic(magic);
                        break;
                }
            }
        }
        protected void CompleteDamageIndicator(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            DamageType type = (DamageType)data[1];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            target.BroadcastDamageIndicator(type);
        }
        protected void CompleteSpellEffect(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            SpellEffect effect = (SpellEffect)data[1];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            S.ObjectEffect p = new S.ObjectEffect { ObjectID = target.ObjectID, Effect = effect };
            CurrentMap.Broadcast(p, target.CurrentLocation);
        }
        protected void CompletePoison(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            PoisonType pt = (PoisonType)data[1];
            SpellEffect sp = (SpellEffect)data[2];
            int duration = (int)data[3];
            int tickSpeed = (int)data[4];

            if (target == null) return;

            target.ApplyPoison(new Poison { PType = pt, Duration = duration, TickSpeed = tickSpeed }, this);
            target.Broadcast(new S.ObjectEffect { ObjectID = target.ObjectID, Effect = sp });
        }
        protected UserItem GetAmulet(int count, int shape = 0)
        {
            for (int i = 0; i < Info.Equipment.Length; i++)
            {
                UserItem item = Info.Equipment[i];
                if (item != null && item.Info.Type == ItemType.Amulet && item.Info.Shape == shape && item.Count >= count)
                    return item;
            }

            return null;
        }
        protected UserItem GetPoison(int count, byte shape = 0)
        {
            for (int i = 0; i < Info.Equipment.Length; i++)
            {
                UserItem item = Info.Equipment[i];
                if (item != null && item.Info.Type == ItemType.Amulet && item.Count >= count)
                {
                    if (shape == 0)
                    {
                        if (item.Info.Shape == 1 || item.Info.Shape == 2)
                            return item;
                    }
                    else
                    {
                        if (item.Info.Shape == shape)
                            return item;
                    }
                }
            }

            return null;
        }
        public UserMagic GetMagic(Spell spell)
        {
            for (int i = 0; i < Info.Magics.Count; i++)
            {
                UserMagic magic = Info.Magics[i];
                if (magic.Spell != spell) continue;
                return magic;
            }

            return null;
        }
        public void LevelMagic(UserMagic magic)
        {
            byte exp = (byte)(Envir.Random.Next(3) + 1);

            if (Settings.MentorSkillBoost && Info.Mentor != 0 && Info.IsMentor)
            {
                if (HasBuff(BuffType.Mentee, out _))
                {
                    CharacterInfo mentor = Envir.GetCharacterInfo(Info.Mentor);
                    PlayerObject player = Envir.GetPlayer(mentor.Name);
                    if (player.CurrentMap == CurrentMap && Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange) && !player.Dead)
                    {
                        if (Stats[Stat.SkillGainMultiplier] == 1)
                        {
                            if (GroupMembers != null && GroupMembers.Contains(player))
                                exp *= 2;
                        }
                    }
                }
            }

            exp *= (byte)Math.Min(byte.MaxValue, Stats[Stat.SkillGainMultiplier]);

            if (Level == ushort.MaxValue) exp = byte.MaxValue;

            int oldLevel = magic.Level;

            switch (magic.Level)
            {
                case 0:
                    if (Level < magic.Info.Level1)
                        return;

                    magic.Experience += exp;
                    if (magic.Experience >= magic.Info.Need1)
                    {
                        magic.Level++;
                        magic.Experience = (ushort)(magic.Experience - magic.Info.Need1);
                        RefreshStats();
                    }
                    break;
                case 1:
                    if (Level < magic.Info.Level2)
                        return;

                    magic.Experience += exp;
                    if (magic.Experience >= magic.Info.Need2)
                    {
                        magic.Level++;
                        magic.Experience = (ushort)(magic.Experience - magic.Info.Need2);
                        RefreshStats();
                    }
                    break;
                case 2:
                    if (Level < magic.Info.Level3)
                        return;

                    magic.Experience += exp;
                    if (magic.Experience >= magic.Info.Need3)
                    {
                        magic.Level++;
                        magic.Experience = 0;
                        RefreshStats();
                    }
                    break;
                default:
                    return;
            }

            if (oldLevel != magic.Level)
            {
                long delay = magic.GetDelay();
                Enqueue(new S.MagicDelay { ObjectID = ObjectID, Spell = magic.Spell, Delay = delay });
            }

            Enqueue(new S.MagicLeveled { ObjectID = ObjectID, Spell = magic.Spell, Level = magic.Level, Experience = magic.Experience });

        }
        public virtual bool MagicTeleport(UserMagic magic)
        {
            return false;
        }
        public override bool Teleport(Map temp, Point location, bool effects = true, byte effectnumber = 0)
        {
            Map oldMap = CurrentMap;
            Point oldLocation = CurrentLocation;

            bool mapChanged = temp != oldMap;

            if (!base.Teleport(temp, location, effects)) return false;

            Enqueue(new S.MapChanged
            {
                MapIndex = CurrentMap.Info.Index,
                FileName = CurrentMap.Info.FileName,
                Title = CurrentMap.Info.Title,
                Weather = CurrentMap.Info.WeatherParticles,
                MiniMap = CurrentMap.Info.MiniMap,
                BigMap = CurrentMap.Info.BigMap,
                Lights = CurrentMap.Info.Light,
                Location = CurrentLocation,
                Direction = Direction,
                MapDarkLight = CurrentMap.Info.MapDarkLight,
                Music = CurrentMap.Info.Music
            });

            if (effects) Enqueue(new S.ObjectTeleportIn { ObjectID = ObjectID, Type = effectnumber });

            if (RidingMount) RefreshMount();
            if (ActiveBlizzard) ActiveBlizzard = false;

            if (CheckStacked())
            {
                StackingTime = Envir.Time + 1000;
                Stacking = true;
            }

            SafeZoneInfo szi = CurrentMap.GetSafeZone(CurrentLocation);

            if (szi != null)
            {
                SetBindSafeZone(szi);
                InSafeZone = true;
            }
            else
                InSafeZone = false;            

            return true;
        }
        protected virtual void SetBindSafeZone(SafeZoneInfo szi) { }
        public virtual bool AtWar(HumanObject attacker)
        {
            return false;
        }
        private Packet GetMountInfo()
        {
            return new S.MountUpdate
            {
                ObjectID = ObjectID,
                RidingMount = RidingMount,
                MountType = Mount.MountType
            };
        }
        protected Packet GetUpdateInfo()
        {
            return new S.PlayerUpdate
            {
                ObjectID = ObjectID,
                Weapon = Looks_Weapon,
                WeaponEffect = Looks_WeaponEffect,
                Armour = Looks_Armour,
                Light = Light,
                WingEffect = Looks_Wings
            };
        }
        protected virtual void UpdateLooks(short OldLooks_Weapon)
        {
            Broadcast(GetUpdateInfo());
        }
        public Packet GetInfoEx(HumanObject player)
        {
            var p = (S.ObjectPlayer)GetInfo();

            if (p != null)
            {
                p.NameColour = player.GetNameColour(this);
            }

            return p;
        }
        public override bool IsAttackTarget(HumanObject attacker)
        {
            return true;
        }
        public override bool IsAttackTarget(MonsterObject attacker)
        {            
            return true;
        }

        public override bool IsFriendlyTarget(HumanObject ally)
        {
            return true;
        }
        public override bool IsFriendlyTarget(MonsterObject ally)
        {            
            return true;
        }
        public override void ReceiveChat(string text, ChatType type) { }
        public override Packet GetInfo() { return null; }
        /// <summary>
        /// Energy shield: a chance for incoming damage to restore health instead of
        /// removing it. Rolled only when the stat is present, matching the original order.
        /// </summary>
        protected void TryEnergyShield()
        {
            if (Stats[Stat.EnergyShieldPercent] <= 0) return;
            if (Envir.Random.Next(100) >= Stats[Stat.EnergyShieldPercent]) return;

            if (HP + Stats[Stat.EnergyShieldHPGain] >= Stats[Stat.HP])
                SetHP(Stats[Stat.HP]);
            else
                ChangeHP(Stats[Stat.EnergyShieldHPGain]);
        }

        /// <summary>
        /// Damage shields lose duration in proportion to the damage that got through.
        /// </summary>
        protected void DrainDamageShields(int damage, int armour)
        {
            if (HasBuff(BuffType.MagicShield, out Buff magicShield))
            {
                var duration = (int)Math.Min(int.MaxValue, magicShield.ExpireTime - ((damage - armour) * 60));
                AddBuff(BuffType.MagicShield, this, duration, null);
            }

            if (HasBuff(BuffType.ElementalBarrier, out Buff elementalBarrier))
            {
                var duration = (int)Math.Min(int.MaxValue, elementalBarrier.ExpireTime - ((damage - armour) * 60));
                AddBuff(BuffType.ElementalBarrier, this, duration, null);
            }
        }

        public override int Attacked(HumanObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            if (attacker.Race == ObjectType.Hero)
            {
                HeroObject heroAttacker = (HeroObject)attacker;
                attacker = heroAttacker.Owner;
                heroAttacker.Target = this;
            }

            (this as HeroObject ?? (this as PlayerObject)?.Hero)?.ReactToAttack(attacker);

            var armour = GetArmour(type, attacker, out bool hit);

            if (!hit)
            {
                return 0;
            }

            damage = ScaleIncomingDamage(damage);

            if (damageWeapon)
                attacker.DamageWeapon();

            damage += attacker.Stats[Stat.AttackBonus];

            if (Stats[Stat.Reflect] > 0 && Envir.Random.Next(100) < Stats[Stat.Reflect])
            {
                if (attacker.IsAttackTarget(this))
                {
                    attacker.Attacked(this, damage, type, false);
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Reflect }, CurrentLocation);
                }
                return 0;
            }

            //MagicShield, ElementalBarrier
            if (Stats[Stat.DamageReductionPercent] > 0)
            {
                damage -= (damage * Stats[Stat.DamageReductionPercent]) / 100;
            }

            if (armour >= damage)
            {
                BroadcastDamageIndicator(DamageType.Miss);
                return 0;
            }

            if (Hidden)
            {
                RemoveBuff(BuffType.MoonLight);
                RemoveBuff(BuffType.DarkBody);
            }

            //EnergyShield
            TryEnergyShield();

            if (attacker.Stats[Stat.CriticalRate] > 0 && Envir.Random.Next(100) < (attacker.Stats[Stat.CriticalRate] * Settings.CriticalRateWeight))
            {
                CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Critical }, CurrentLocation);
                damage = Math.Min(int.MaxValue, damage + (int)Math.Floor(damage * (((double)attacker.Stats[Stat.CriticalDamage] / (double)Settings.CriticalDamageWeight) * 10)));
                BroadcastDamageIndicator(DamageType.Critical);
            }

            DrainDamageShields(damage, armour);

            if (attacker.Stats[Stat.HPDrainRatePercent] > 0 && damageWeapon)
            {
                attacker.HpDrain += Math.Max(0, ((float)(damage - armour) / 100) * attacker.Stats[Stat.HPDrainRatePercent]);
                if (attacker.HpDrain > 2)
                {
                    int HpGain = (int)Math.Floor(attacker.HpDrain);
                    attacker.ChangeHP(HpGain);
                    attacker.HpDrain -= HpGain;
                }
            }

            ClearParalysisOnHit();

            LastHitter = attacker;
            LastHitTime = Envir.Time + 10000;
            RegenTime = Envir.Time + RegenDelay;
            LogTime = Envir.Time + Globals.LogDelay;

            if (Envir.Time > BrownTime && PKPoints < 200 && !AtWar(attacker))
                attacker.BrownTime = Envir.Time + Settings.Minute;

            ushort LevelOffset = (byte)(Level > attacker.Level ? 0 : Math.Min(10, attacker.Level - Level));

            ApplyNegativeEffects(attacker, type, LevelOffset);

            attacker.GatherElement();

            DamageDura();
            ActiveBlizzard = false;
            ActiveReincarnation = false;

            CounterAttackCast(GetMagic(Spell.CounterAttack), LastHitter);

            Enqueue(new S.Struck { AttackerID = attacker.ObjectID });
            Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = attacker.ObjectID, Direction = Direction, Location = CurrentLocation });

            BroadcastDamageIndicator(DamageType.Hit, armour - damage);

            ChangeHP(armour - damage);
            return damage - armour;
        }
        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            (this as HeroObject ?? (this as PlayerObject)?.Hero)?.ReactToAttack(attacker.Master ?? attacker);

            var armour = GetArmour(type, attacker, out bool hit);

            if (!hit)
            {
                return 0;
            }

            if (Stats[Stat.Reflect] > 0 && Envir.Random.Next(100) < Stats[Stat.Reflect])
            {
                if (attacker.IsAttackTarget(this))
                {
                    attacker.Attacked(this, damage, type, false);
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Reflect }, CurrentLocation);
                }
                return 0;
            }

            damage = ScaleIncomingDamage(damage);

            //MagicShield, ElementalBarrier
            if (Stats[Stat.DamageReductionPercent] != 0)
            {
                damage -= (damage * Stats[Stat.DamageReductionPercent]) / 100;
            }

            if (armour >= damage)
            {
                BroadcastDamageIndicator(DamageType.Miss);
                return 0;
            }

            if (Hidden)
            {
                RemoveBuff(BuffType.MoonLight);
                RemoveBuff(BuffType.DarkBody);
            }

            TryEnergyShield();

            if (attacker.Stats[Stat.CriticalRate] > 0 && Envir.Random.Next(100) < (attacker.Stats[Stat.CriticalRate] * Settings.CriticalRateWeight))
            {
                CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Critical }, CurrentLocation);
                damage = Math.Min(int.MaxValue, damage + (int)Math.Floor(damage * (((double)attacker.Stats[Stat.CriticalDamage] / (double)Settings.CriticalDamageWeight) * 10)));
                BroadcastDamageIndicator(DamageType.Critical);
            }

            DrainDamageShields(damage, armour);

            ClearParalysisOnHit();

            LastHitter = attacker.Master ?? attacker;
            LastHitTime = Envir.Time + 10000;
            RegenTime = Envir.Time + RegenDelay;
            LogTime = Envir.Time + Globals.LogDelay;

            DamageDura();
            ActiveBlizzard = false;
            ActiveReincarnation = false;

            CounterAttackCast(GetMagic(Spell.CounterAttack), LastHitter);

            if (StruckTime < Envir.Time)
            {
                Enqueue(new S.Struck { AttackerID = attacker.ObjectID });
                Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = attacker.ObjectID, Direction = Direction, Location = CurrentLocation });
                StruckTime = Envir.Time + 500;
            }

            BroadcastDamageIndicator(DamageType.Hit, armour - damage);

            ChangeHP(armour - damage);
            return damage - armour;
        }
        public override int Struck(int damage, DefenceType type = DefenceType.ACAgility)
        {
            int armour = 0;

            if (Hidden)
            {
                RemoveBuff(BuffType.MoonLight);
                RemoveBuff(BuffType.DarkBody);
            }

            armour = RollTypeArmour(type);

            damage = ScaleIncomingDamage(damage);

            //MagicShield, ElementalBarrier
            if (Stats[Stat.DamageReductionPercent] != 0)
            {
                damage -= (damage * Stats[Stat.DamageReductionPercent]) / 100;
            }

            if (armour >= damage) return 0;

            DrainDamageShields(damage, armour);

            RegenTime = Envir.Time + RegenDelay;
            LogTime = Envir.Time + Globals.LogDelay;

            DamageDura();
            ActiveBlizzard = false;
            ActiveReincarnation = false;
            Enqueue(new S.Struck { AttackerID = 0 });
            Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = 0, Direction = Direction, Location = CurrentLocation });

            ChangeHP(armour - damage);
            return damage - armour;
        }

        public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true)
        {
            if (Caster != null && !NoResist)
            {
                if (((Caster.Race != ObjectType.Player) || Settings.PvpCanResistPoison) && (Envir.Random.Next(Settings.PoisonResistWeight) < Stats[Stat.PoisonResist]))
                {
                    return;
                }
            }

            if (!ignoreDefence && (p.PType == PoisonType.Green))
            {
                int armour = GetAttackPower(BaseStats[Stat.MinMAC], BaseStats[Stat.MaxMAC]);

                if (p.Value < armour)
                    p.PType = PoisonType.None;
                else
                    p.Value -= armour;
            }

            if (p.Owner != null && p.Owner is PlayerObject player && Envir.Time > BrownTime && PKPoints < 200)
            {
                bool ownerBrowns = true;
                if (player.MyGuild != null && MyGuild != null && MyGuild.IsAtWar() && MyGuild.IsEnemy(player.MyGuild))
                    ownerBrowns = false;

                if (ownerBrowns && !player.WarZone)
                        p.Owner.BrownTime = Envir.Time + Settings.Minute;
            }

            if ((p.PType == PoisonType.Green) || (p.PType == PoisonType.Red)) p.Duration = Math.Max(0, p.Duration - Stats[Stat.PoisonRecovery]);
            if (p.Duration == 0) return;
            if (p.PType == PoisonType.None) return;

            for (int i = 0; i < PoisonList.Count; i++)
            {
                if (PoisonList[i].PType != p.PType) continue;
                if ((PoisonList[i].PType == PoisonType.Green) && (PoisonList[i].Value > p.Value)) return;//cant cast weak poison to cancel out strong poison
                if ((PoisonList[i].PType != PoisonType.Green) && ((PoisonList[i].Duration - PoisonList[i].Time) > p.Duration)) return;//cant cast 1 second poison to make a 1minute poison go away!
                if ((PoisonList[i].PType == PoisonType.Frozen) || (PoisonList[i].PType == PoisonType.Slow) || (PoisonList[i].PType == PoisonType.Paralysis) || (PoisonList[i].PType == PoisonType.LRParalysis)) return;//prevents mobs from being perma frozen/slowed
                if (p.PType == PoisonType.DelayedExplosion) return;

                ReceiveChat(GameLanguage.Get("You have been poisoned"), ChatType.System2);
                PoisonList[i] = p;
                return;
            }

            switch (p.PType)
            {
                case PoisonType.DelayedExplosion:
                    {
                        ExplosionInflictedTime = Envir.Time + 4000;
                        Enqueue(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion });
                        Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion });
                        ReceiveChat(GameLanguage.Get("You are a walking explosive."), ChatType.System);
                    }
                    break;
                case PoisonType.Dazed:
                    {
                        Enqueue(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Stunned, Time = (uint)(p.Duration * p.TickSpeed) });
                        Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Stunned, Time = (uint)(p.Duration * p.TickSpeed) });
                        ReceiveChat(GameLanguage.Get("You have been poisoned"), ChatType.System2);
                    }
                    break;
                case PoisonType.Blindness:
                    {
                        AddBuff(BuffType.Blindness, Caster, (int)(p.Duration * p.TickSpeed), new Stats { [Stat.Accuracy] = p.Value * -1 });
                        ReceiveChat(GameLanguage.Get("You have been poisoned"), ChatType.System2);
                    }
                    break;
                default:
                    {
                        ReceiveChat(GameLanguage.Get("You have been poisoned"), ChatType.System2);
                    }
                    break;
            }

            PoisonList.Add(p);
        }

        public override Buff AddBuff(BuffType type, MapObject owner, int duration, Stats stats, bool refreshStats = true, bool updateOnly = false, params int[] values)
        {
            Buff b = base.AddBuff(type, owner, duration, stats, refreshStats, updateOnly, values);

            switch (b.Type)
            {
                case BuffType.MagicShield:
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.MagicShieldUp }, CurrentLocation);
                    break;
                case BuffType.ElementalBarrier:
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.ElementalBarrierUp }, CurrentLocation);
                    break;
            }

            var packet = new S.AddBuff { Buff = b.ToClientBuff() };

            Enqueue(packet);

            if (b.Info.Visible)
            {
                Broadcast(packet);
            }

            if (refreshStats)
            {
                RefreshStats();
            }

            return b;
        }

        public override void PauseBuff(Buff b)
        {
            if (b.Paused) return;

            base.PauseBuff(b);

            Enqueue(new S.PauseBuff { Type = b.Type, ObjectID = ObjectID, Paused = true });
        }

        public override void UnpauseBuff(Buff b)
        {
            if (!b.Paused) return;

            base.UnpauseBuff(b);

            Enqueue(new S.PauseBuff { Type = b.Type, ObjectID = ObjectID, Paused = false });
        }
        protected int GetCurrentStatCount(UserItem gem, UserItem item)
        {
            if (gem.GetTotal(Stat.MaxDC) > 0)
                return item.AddedStats[Stat.MaxDC];

            else if (gem.GetTotal(Stat.MaxMC) > 0)
                return item.AddedStats[Stat.MaxMC];

            else if (gem.GetTotal(Stat.MaxSC) > 0)
                return item.AddedStats[Stat.MaxSC];

            else if (gem.GetTotal(Stat.MaxAC) > 0)
                return item.AddedStats[Stat.MaxAC];

            else if (gem.GetTotal(Stat.MaxMAC) > 0)
                return item.AddedStats[Stat.MaxMAC];

            else if ((gem.Info.Durability) > 0)
                return item.Info.Durability > item.MaxDura ? 0 : ((item.MaxDura - item.Info.Durability) / 1000);

            else if (gem.GetTotal(Stat.AttackSpeed) > 0)
                return item.AddedStats[Stat.AttackSpeed];

            else if (gem.GetTotal(Stat.Agility) > 0)
                return item.AddedStats[Stat.Agility];

            else if (gem.GetTotal(Stat.Accuracy) > 0)
                return item.AddedStats[Stat.Accuracy];

            else if (gem.GetTotal(Stat.PoisonAttack) > 0)
                return item.AddedStats[Stat.PoisonAttack];

            else if (gem.GetTotal(Stat.Freezing) > 0)
                return item.AddedStats[Stat.Freezing];

            else if (gem.GetTotal(Stat.MagicResist) > 0)
                return item.AddedStats[Stat.MagicResist];

            else if (gem.GetTotal(Stat.PoisonResist) > 0)
                return item.AddedStats[Stat.PoisonResist];

            else if (gem.GetTotal(Stat.Luck) > 0)
                return item.AddedStats[Stat.Luck];

            else if (gem.GetTotal(Stat.PoisonRecovery) > 0)
                return item.AddedStats[Stat.PoisonRecovery];

            else if (gem.GetTotal(Stat.HP) > 0)
                return item.AddedStats[Stat.HP];

            else if (gem.GetTotal(Stat.MP) > 0)
                return item.AddedStats[Stat.MP];

            else if (gem.GetTotal(Stat.HealthRecovery) > 0)
                return item.AddedStats[Stat.HealthRecovery];

            // Definitions are missing for these.
            /*
            else if ((gem.Info.HPrate) > 0)
                return item.h

            else if ((gem.Info.MPrate) > 0)
                return 

            else if ((gem.Info.SpellRecovery) > 0)
                return 

            else if ((gem.Info.Holy) > 0)
                return 

            else if ((gem.Info.Strong + gem.Strong) > 0)
                return 

            else if (gem.Info.HPrate > 0)
                return
            */
            return 0;
        }
        public bool CanGainItem(UserItem item)
        {
            if (FreeSpace(Info.Inventory) > 0)
            {
                return true;
            }

            if (item.Info.Type == ItemType.Amulet)
            {
                ushort count = item.Count;

                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    UserItem bagItem = Info.Inventory[i];

                    if (!item.CanStackWith(bagItem) || bagItem.Count >= bagItem.Info.StackSize) continue;

                    if (bagItem.Count + count <= bagItem.Info.StackSize) return true;

                    count -= (ushort)(bagItem.Info.StackSize - bagItem.Count);
                }

                return false;
            }

            if (item.Info.StackSize > 1)
            {
                ushort count = item.Count;

                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    UserItem bagItem = Info.Inventory[i];

                    if (!item.CanStackWith(bagItem) || bagItem.Count >= bagItem.Info.StackSize) continue;

                    if (bagItem.Count + count <= bagItem.Info.StackSize) return true;

                    count -= (ushort)(bagItem.Info.StackSize - bagItem.Count);
                }
            }

            return false;
        }
        public bool CanGainItems(UserItem[] items)
        {
            int itemCount = items.Count(e => e != null);
            ushort stackOffset = 0;

            if (itemCount < 1) return true;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) continue;

                if (items[i].Info.StackSize > 1)
                {
                    ushort count = items[i].Count;

                    for (int u = 0; u < Info.Inventory.Length; u++)
                    {
                        UserItem bagItem = Info.Inventory[u];

                        if (!items[i].CanStackWith(bagItem)) continue;

                        if (bagItem.Count + count > bagItem.Info.StackSize) stackOffset++;

                        break;
                    }
                }
            }

            if (FreeSpace(Info.Inventory) < itemCount + stackOffset) return false;

            return true;
        }
        public bool CanEquipItem(UserItem item, int slot)
        {
            switch ((EquipmentSlot)slot)
            {
                case EquipmentSlot.Weapon:
                    if (item.Info.Type != ItemType.Weapon)
                        return false;
                    break;
                case EquipmentSlot.Armour:
                    if (item.Info.Type != ItemType.Armour)
                        return false;
                    break;
                case EquipmentSlot.Helmet:
                    if (item.Info.Type != ItemType.Helmet)
                        return false;
                    break;
                case EquipmentSlot.Torch:
                    if (item.Info.Type != ItemType.Torch)
                        return false;
                    break;
                case EquipmentSlot.Necklace:
                    if (item.Info.Type != ItemType.Necklace)
                        return false;
                    break;
                case EquipmentSlot.BraceletL:
                    if (item.Info.Type != ItemType.Bracelet)
                        return false;
                    break;
                case EquipmentSlot.BraceletR:
                    if (item.Info.Type != ItemType.Bracelet && item.Info.Type != ItemType.Amulet)
                        return false;
                    break;
                case EquipmentSlot.RingL:
                case EquipmentSlot.RingR:
                    if (item.Info.Type != ItemType.Ring)
                        return false;
                    break;
                case EquipmentSlot.Amulet:
                    if (item.Info.Type != ItemType.Amulet)// || item.Info.Shape == 0
                        return false;
                    break;
                case EquipmentSlot.Boots:
                    if (item.Info.Type != ItemType.Boots)
                        return false;
                    break;
                case EquipmentSlot.Belt:
                    if (item.Info.Type != ItemType.Belt)
                        return false;
                    break;
                case EquipmentSlot.Stone:
                    if (item.Info.Type != ItemType.Stone)
                        return false;
                    break;
                case EquipmentSlot.Mount:
                    if (item.Info.Type != ItemType.Mount)
                        return false;
                    break;
                default:
                    return false;
            }


            switch (Gender)
            {
                case MirGender.Male:
                    if (!item.Info.RequiredGender.HasFlag(RequiredGender.Male))
                        return false;
                    break;
                case MirGender.Female:
                    if (!item.Info.RequiredGender.HasFlag(RequiredGender.Female))
                        return false;
                    break;
            }


            switch (Class)
            {
                case MirClass.Warrior:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Warrior))
                        return false;
                    break;
                case MirClass.Wizard:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Wizard))
                        return false;
                    break;
                case MirClass.Taoist:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Taoist))
                        return false;
                    break;
                case MirClass.Assassin:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Assassin))
                        return false;
                    break;
            }

            switch (item.Info.RequiredType)
            {
                case RequiredType.Level:
                    if (Level < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MaxAC:
                    if (BaseStats[Stat.MaxAC] < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MaxMAC:
                    if (BaseStats[Stat.MaxMAC] < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MaxDC:
                    if (Stats[Stat.MaxDC] < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MaxMC:
                    if (Stats[Stat.MaxMC] < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MaxSC:
                    if (Stats[Stat.MaxSC] < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MaxLevel:
                    if (Level > item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MinAC:
                    if (BaseStats[Stat.MinAC] < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MinMAC:
                    if (BaseStats[Stat.MinMAC] < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MinDC:
                    if (Stats[Stat.MinDC] < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MinMC:
                    if (Stats[Stat.MinMC] < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MinSC:
                    if (Stats[Stat.MinSC] < item.Info.RequiredAmount)
                        return false;
                    break;
            }

            if (item.Info.Type == ItemType.Weapon || item.Info.Type == ItemType.Torch)
            {
                if (item.Weight - (Info.Equipment[slot] != null ? Info.Equipment[slot].Weight : 0) + CurrentHandWeight > Stats[Stat.HandWeight])
                    return false;
            }
            else
                if (item.Weight - (Info.Equipment[slot] != null ? Info.Equipment[slot].Weight : 0) + CurrentWearWeight > Stats[Stat.WearWeight])
                return false;

            if (RidingMount && item.Info.Type != ItemType.Torch)
            {
                return false;
            }

            return true;
        }
        public int GainItem(UserItem item)
        {
            //CheckItemInfo(item.Info);
            CheckItem(item);

            UserItem clonedItem = item.Clone();

            Enqueue(new S.GainedItem { Item = clonedItem }); //Cloned because we are probably going to change the amount.

            int slot = AddItem(item);

            RefreshBagWeight();

            return slot;
        }

        private void DamageDura()
        {
            if (!SpecialMode.HasFlag(SpecialItemMode.NoDuraLoss))
                for (int i = 0; i < Info.Equipment.Length; i++)
                    if (i != (int)EquipmentSlot.Weapon)
                        DamageItem(Info.Equipment[i], Envir.Random.Next(1) + 1);
        }
        public void DamageWeapon()
        {
            if (!SpecialMode.HasFlag(SpecialItemMode.NoDuraLoss))
                DamageItem(Info.Equipment[(int)EquipmentSlot.Weapon], Envir.Random.Next(4) + 1);
        }
        public void DamageItem(UserItem item, int amount, bool isChanged = false)
        {
            if (item == null || item.CurrentDura == 0 || item.Info.Type == ItemType.Amulet) return;
            if ((item.WeddingRing == Info.Married) && (Info.Equipment[(int)EquipmentSlot.RingL].UniqueID == item.UniqueID)) return;
            if (item.GetTotal(Stat.Strong) > 0) amount = Math.Max(1, amount - item.GetTotal(Stat.Strong));
            item.CurrentDura = (ushort)Math.Max(ushort.MinValue, item.CurrentDura - amount);
            item.DuraChanged = true;

            if (item.CurrentDura > 0 && isChanged != true) return;
            Enqueue(new S.DuraChanged { UniqueID = item.UniqueID, CurrentDura = item.CurrentDura });

            item.DuraChanged = false;
            RefreshStats();
        }

        public void RemoveObjects(MirDirection dir, int count)
        {
            switch (dir)
            {
                case MirDirection.Up:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.UpRight:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Remove(this);
                            }
                        }
                    }

                    //Left Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange - count; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Right:
                    //Left Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.DownRight:
                    //Top Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Remove(this);
                            }
                        }
                    }

                    //Left Block
                    for (int a = -Globals.DataRange + count; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Down:
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.DownLeft:
                    //Top Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Remove(this);
                            }
                        }
                    }

                    //Right Block
                    for (int a = -Globals.DataRange + count; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Left:
                    for (int a = -Globals.DataRange; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.UpLeft:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Remove(this);
                            }
                        }
                    }

                    //Right Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange - count; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
            }
        }
        public void AddObjects(MirDirection dir, int count)
        {
            switch (dir)
            {
                case MirDirection.Up:
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.UpRight:
                    //Top Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Add(this);
                            }
                        }
                    }

                    //Right Block
                    for (int a = -Globals.DataRange + count; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Right:
                    for (int a = -Globals.DataRange; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.DownRight:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Add(this);
                            }
                        }
                    }

                    //Right Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange - count; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Down:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.DownLeft:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Add(this);
                            }
                        }
                    }

                    //Left Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange - count; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Left:
                    //Left Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.UpLeft:
                    //Top Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Add(this);
                            }
                        }
                    }

                    //Left Block
                    for (int a = -Globals.DataRange + count; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            Cell cell = CurrentMap.GetCell(x, y);

                            if (!cell.Valid || cell.Objects == null) continue;

                            for (int i = 0; i < cell.Objects.Count; i++)
                            {
                                MapObject ob = cell.Objects[i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
            }
        }
        public override void Remove(HumanObject player)
        {
            if (player == this) return;

            base.Remove(player);
            Enqueue(new S.ObjectRemove { ObjectID = player.ObjectID });
        }
        public override void Add(HumanObject player)
        {
            if (player == this) return;

            //base.Add(player);
            Enqueue(player.GetInfoEx(this));
            player.Enqueue(GetInfoEx(player));

            player.SendHealth(this);
            SendHealth(player);
        }
        public override void Remove(MonsterObject monster)
        {
            Enqueue(new S.ObjectRemove { ObjectID = monster.ObjectID });
        }
        public override void Add(MonsterObject monster)
        {
            Enqueue(monster.GetInfo());

            monster.SendHealth(this);
        }
        public override void SendHealth(HumanObject player)
        {
            if (!player.IsMember(this) && Envir.Time > RevTime) return;
            byte time = Math.Min(byte.MaxValue, (byte)Math.Max(5, (RevTime - Envir.Time) / 1000));
            player.Enqueue(new S.ObjectHealth { ObjectID = ObjectID, Percent = PercentHealth, Expire = time });
        }
        protected virtual void CleanUp()
        {
            Connection.Player = null;
            Info.Player = null;
            Info.Mount = null;
            Connection.CleanObservers();
            Connection = null;
            Info = null;
        }
        public virtual void Enqueue(Packet p)
        {
            if (Connection == null) return;
            Connection.Enqueue(p);

			//if (p != null) MessageQueue.EnqueueDebugging(((ServerPacketIds)p.Index).ToString());
		}
		public virtual void Enqueue(Packet p, MirConnection c)
        {            
            if (c == null)
            {
                Enqueue(p);
                return;
            }

            c.Enqueue(p);
        }

        public void SpellToggle(Spell spell, SpellToggleState state)
        {
            if (Dead) return;

            UserMagic magic;
            bool use = Convert.ToBoolean(state);

            magic = GetMagic(spell);
            if (magic == null) return;

            int cost;
            switch (spell)
            {
                case Spell.Thrusting:
                    Info.Thrusting = state == SpellToggleState.None ? !Info.Thrusting : use;
                    break;
                case Spell.HalfMoon:
                    Info.HalfMoon = state == SpellToggleState.None ? !Info.HalfMoon : use;
                    break;
                case Spell.CrossHalfMoon:
                    Info.CrossHalfMoon = state == SpellToggleState.None ? !Info.CrossHalfMoon : use;
                    break;
                case Spell.DoubleSlash:
                    Info.DoubleSlash = state == SpellToggleState.None ? !Info.DoubleSlash : use;
                    break;
                case Spell.TwinDrakeBlade:
                    if (TwinDrakeBlade) return;
                    magic = GetMagic(spell);
                    if (magic == null) return;
                    cost = magic.Info.BaseCost + magic.Level * magic.Info.LevelCost;
                    if (cost >= MP) return;

                    TwinDrakeBlade = true;
                    ChangeMP(-cost);

                    Enqueue(new S.ObjectMagic { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Spell = spell });
                    break;
                case Spell.FlamingSword:
                    if (FlamingSword || Envir.Time < FlamingSwordTime) return;
                    magic = GetMagic(spell);
                    if (magic == null) return;
                    cost = magic.Info.BaseCost + magic.Level * magic.Info.LevelCost;
                    if (cost >= MP) return;

                    FlamingSword = true;
                    FlamingSwordTime = Envir.Time + 10000;
                    Enqueue(new S.SpellToggle { ObjectID = ObjectID, Spell = Spell.FlamingSword, CanUse = true });
                    ChangeMP(-cost);
                    break;
                case Spell.CounterAttack:
                    if (CounterAttack || Envir.Time < CounterAttackTime) return;
                    magic = GetMagic(spell);
                    if (magic == null) return;
                    cost = magic.Info.BaseCost + magic.Level * magic.Info.LevelCost;
                    if (cost >= MP) return;

                    CounterAttack = true;
                    CounterAttackTime = Envir.Time + (Settings.Second * 7);

                    var stats = new Stats
                    {
                        [Stat.MinAC] = 11 + magic.Level * 3,
                        [Stat.MinMAC] = 11 + magic.Level * 3,
                        [Stat.MaxAC] = 11 + magic.Level * 3,
                        [Stat.MaxMAC] = 11 + magic.Level * 3,
                    };

                    AddBuff(BuffType.CounterAttack, this, Settings.Second * 7, stats);
                    ChangeMP(-cost);
                    break;
                case Spell.MentalState:
                    Info.MentalState = (byte)((Info.MentalState + 1) % 3);

                    ShowMentalState();
                    break;
            }
        }

        private void ShowMentalState()
        {
            switch (Info.MentalState)
            {
                case 0:
                    ReceiveChat(GameLanguage.Get("Mentalstate: Agressive."), ChatType.Hint);
                    break;
                case 1:
                    ReceiveChat(GameLanguage.Get("Mentalstate: Trick shot."), ChatType.Hint);
                    break;
                case 2:
                    ReceiveChat(GameLanguage.Get("Mentalstate: Group mode."), ChatType.Hint);
                    break;
            }

            AddBuff(BuffType.MentalState, this, 0, new Stats(), false, values: Info.MentalState);
        }

        #region Mounts

        public void RefreshMount(bool refreshStats = true)
        {
            if (RidingMount)
            {
                if (Mount.MountType < 0)
                {
                    RidingMount = false;
                }
                else if (!Mount.CanRide)
                {
                    RidingMount = false;
                    ReceiveChat(GameLanguage.Get("You must have a saddle to ride your mount"), ChatType.System);
                }
                else if (!Mount.CanMapRide)
                {
                    RidingMount = false;
                    ReceiveChat(GameLanguage.Get("You cannot ride on this map"), ChatType.System);
                }
                else if (!Mount.CanDungeonRide)
                {
                    RidingMount = false;
                    ReceiveChat(GameLanguage.Get("You cannot ride here without a bridle"), ChatType.System);
                }
            }
            else
            {
                RidingMount = false;
            }

            if (refreshStats)
                RefreshStats();

            Broadcast(GetMountInfo());
            Enqueue(GetMountInfo());
        }
        public void IncreaseMountLoyalty(int amount)
        {
            UserItem item = Info.Equipment[(int)EquipmentSlot.Mount];
            if (item != null && item.CurrentDura < item.MaxDura)
            {
                item.CurrentDura = (ushort)Math.Min(item.MaxDura, item.CurrentDura + amount);
                item.DuraChanged = false;
                Enqueue(new S.ItemRepaired { UniqueID = item.UniqueID, MaxDura = item.MaxDura, CurrentDura = item.CurrentDura });
            }
        }
        public void DecreaseMountLoyalty(int amount)
        {
            if (Envir.Time > DecreaseLoyaltyTime)
            {
                DecreaseLoyaltyTime = Envir.Time + (Mount.SlowLoyalty ? (LoyaltyDelay * 2) : LoyaltyDelay);
                UserItem item = Info.Equipment[(int)EquipmentSlot.Mount];
                if (item != null && item.CurrentDura > 0)
                {
                    DamageItem(item, amount);

                    if (item.CurrentDura == 0)
                    {
                        RefreshMount();
                    }
                }
            }
        }

        public void ToggleRide()
        {
            if (Mount.MountType > -1)
            {
                RidingMount = !RidingMount;
                RefreshMount();
            }
            else
                ReceiveChat(GameLanguage.Get("You do not have a mount equiped."), ChatType.System);
        }

        #endregion
    }
}
