using Server.MirObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;
using Timer = Server.MirEnvir.Timer;

namespace Server.Scripting
{
    /// <summary>
    /// The checks that read the acting player: money, identity, items, pets, page values, quests,
    /// heroes and effects.
    /// </summary>
    public partial class ScriptSegment
    {
        // ---------------------------------------------------------------- money


        internal static ScriptOutcome Exec_CheckGold(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            uint tempUint = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!uint.TryParse(param[1], out tempUint))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                failed = !Compare(param[0], player.Account.Gold, tempUint);
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckGuildGold(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            uint tempUint = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!uint.TryParse(param[1], out tempUint))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                failed = !Compare(param[0], player.MyGuild.Gold, tempUint);
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckCredit(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            uint tempUint = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!uint.TryParse(param[1], out tempUint))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                failed = !Compare(param[0], player.Account.Credit, tempUint);
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- identity


        internal static ScriptOutcome Exec_Level(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;
            bool failed = false;

            if (obj == null) return ScriptOutcome.Pass;

            {
                if (!ushort.TryParse(param[1], out ushort level))
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }

                try
                {
                    failed = !Compare(param[0], ObjectLevel(obj), level);
                }
                catch (ArgumentException)
                {
                    MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                    return ScriptOutcome.Abort;
                }
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        private static int ObjectLevel(MapObject obj)
        {
            var player = obj as PlayerObject;

            return player != null ? player.Level : ((MonsterObject)obj).Level;
        }

        internal static ScriptOutcome Exec_CheckGender(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            MirGender gender;

            if (!MirGender.TryParse(param[0], false, out gender))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = player.Gender != gender;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckClass(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            MirClass mirClass;

            if (!MirClass.TryParse(param[0], true, out mirClass))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = player.Class != mirClass;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_IsAdmin(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            failed = !player.IsGM;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckPkPoint(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[1], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                failed = !Compare(param[0], player.PKPoints, tempInt);
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckPermission(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            GuildRankOptions guildPermissions;
            if (!Enum.TryParse(param[0], true, out guildPermissions))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            if (player.MyGuild == null)
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = !(player.MyGuildRank.Options.HasFlag(guildPermissions));

            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_IsNewHuman(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            failed = player.Info.AccountInfo.Characters.Count > 1;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- items


        internal static ScriptOutcome Exec_CheckItem(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            ushort count;
            ushort dura;

            if (!ushort.TryParse(param[1], out count))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            bool checkDura = ushort.TryParse(param[2], out dura);

            var info = Envir.GetItemInfo(param[0]);

            foreach (var item in player.Info.Inventory.Where(item => item != null && item.Info == info))
            {
                if (checkDura)
                    if (item.CurrentDura < (dura * 1000)) continue;

                if (count > item.Count)
                {
                    count -= item.Count;
                    continue;
                }

                if (count > item.Count) continue;
                count = 0;
                break;
            }
            if (count > 0)
                failed = true;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_HasBagSpace(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[1], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            int slotCount = 0;

            for (int k = 0; k < player.Info.Inventory.Length; k++)
                if (player.Info.Inventory[k] == null) slotCount++;

            failed = !Compare(param[0], slotCount, tempInt);
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- pets


        internal static ScriptOutcome Exec_PetCount(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[1], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = !Compare(param[0], player.Pets.Count(), tempInt);
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_PetLevel(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[1], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            for (int p = 0; p < player.Pets.Count(); p++)
            {
                failed = !Compare(param[0], player.Pets[p].PetLevel, tempInt);
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckPet(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            bool petMatch = false;
            for (int c = player.Pets.Count - 1; c >= 0; c--)
            {
                if (string.Compare(player.Pets[c].Info.Name, param[0], true) != 0) continue;

                petMatch = true;
            }

            failed = !petMatch;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- page values


        internal static ScriptOutcome Exec_CheckCalc(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            bool failed = false;

            int left;
            int right;

            try
            {
                if (!int.TryParse(param[0], out left) || !int.TryParse(param[2], out right))
                {
                    failed = !Compare(param[1], param[0], param[2]);
                }
                else
                {
                    failed = !Compare(param[1], left, right);
                }
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[1], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- quests


        internal static ScriptOutcome Exec_CheckQuest(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[0], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            string tempString = param[1].ToUpper();

            if (tempString == "ACTIVE")
            {
                failed = !player.CurrentQuests.Any(e => e.Index == tempInt);
            }
            else //COMPLETE
            {
                failed = !player.CompletedQuests.Contains(tempInt);
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- heroes


        internal static ScriptOutcome Exec_HeroLevel(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[1], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }
            if (player.CurrentHero == null)
            {
                failed = true;
            }
            else
            {
                failed = !Compare(param[0], player.CurrentHero.Level, tempInt);
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckHeroClass(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            MirClass heroClass;
            if (!MirClass.TryParse(param[0], true, out heroClass))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }
            if (player.CurrentHero == null)
            {
                failed = true;
            }
            else
            {
                failed = player.CurrentHero.Class != heroClass;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckHeroGender(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            MirGender heroGender;
            if (!MirGender.TryParse(param[0], false, out heroGender))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }
            if (player.CurrentHero == null)
            {
                failed = true;
            }
            else
            {
                failed = player.CurrentHero.Gender != heroGender;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckHeroItem(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            ushort heroItemCount;
            ushort heroItemDura;

            if (!ushort.TryParse(param[1], out heroItemCount))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            bool heroCheckDura = ushort.TryParse(param[2], out heroItemDura);

            var heroItemInfo = Envir.GetItemInfo(param[0]);

            if (player.CurrentHero == null || player.CurrentHero.Inventory == null)
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            foreach (var item in player.CurrentHero.Inventory.Where(item => item != null && item.Info == heroItemInfo))
            {
                if (heroCheckDura)
                    if (item.CurrentDura < (heroItemDura * 1000)) continue;

                if (heroItemCount > item.Count)
                {
                    heroItemCount -= item.Count;
                    continue;
                }
                heroItemCount = 0;
                break;
            }
            if (heroItemCount > 0)
                failed = true;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- effects


        internal static ScriptOutcome Exec_CheckBuff(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!Enum.TryParse(param[0], true, out BuffType buffType))
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }
                failed = !player.HasBuff(buffType);
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }
    }
}
