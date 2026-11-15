using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Server.MirDatabase;
using Server.MirEnvir;
using Server.MirNetwork;
using Server.Library.MirDatabase;
using S = ServerPackets;
using Timer = Server.MirEnvir.Timer;

namespace Server.MirObjects
{
    /// The actions that change relations or send something out: name lists, guild, marriage,
    /// messages, mail, buffs, the browser and sound.
    public partial class NPCSegment
    {
        // ---------------------------------------------------------------- name lists


        internal static ScriptOutcome Exec_AddNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var tempString = param[0];
                if (File.ReadAllLines(tempString).All(t => player.Name != t))
                {
                    using (var line = File.AppendText(tempString))
                    {
                        line.WriteLine(player.Name);
                    }
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_AddGuildNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var tempString = param[0];
                if (player.MyGuild == null) return ScriptOutcome.Pass;
                if (File.ReadAllLines(tempString).All(t => player.MyGuild.Name != t))
                {
                    using (var line = File.AppendText(tempString))
                    {
                        line.WriteLine(player.MyGuild.Name);
                    }
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_DelNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var tempString = param[0];
                if (!File.Exists(tempString)) return ScriptOutcome.Pass;
                File.WriteAllLines(tempString, File.ReadLines(tempString).Where(l => l != player.Name).ToList());
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_DelGuildNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.MyGuild == null) return ScriptOutcome.Pass;
                var tempString = param[0];
                if (!File.Exists(tempString)) return ScriptOutcome.Pass;
                File.WriteAllLines(tempString, File.ReadLines(tempString).Where(l => l != player.MyGuild.Name).ToList());
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ClearNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var tempString = param[0];
                File.WriteAllLines(tempString, new string[] { });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ClearGuildNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.MyGuild == null) return ScriptOutcome.Pass;
                var tempString = param[0];
                File.WriteAllLines(tempString, new string[] { });
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- guild


        internal static ScriptOutcome Exec_AddToGuild(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.MyGuild != null) return ScriptOutcome.Abort;

                GuildObject guild = Envir.GetGuild(param[0]);

                if (guild == null)
                {
                    MessageQueue.Enqueue($"AddToGuild: guild '{param[0]}' not found. Player: {player.Name}, Page: {seg.Page?.Key}");
                    player.ReceiveChat(GameLanguage.Get("Guild {0} is not available. Please contact an administrator.", param[0]), ChatType.System);
                    return ScriptOutcome.Abort;
                }

                player.PendingGuildInvite = guild;
                player.GuildInvite(true);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_RemoveFromGuild(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.MyGuild == null) return ScriptOutcome.Abort;

                if (player.MyGuildRank == null) return ScriptOutcome.Abort;

                if (player.MyGuild.Name == Settings.NewbieGuild) player.RemoveBuff(BuffType.Newbie);
                if (player.HasBuff(BuffType.Guild)) player.RemoveBuff(BuffType.Guild);

                player.MyGuild.DeleteMember(player, player.Name);
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- marriage


        internal static ScriptOutcome Exec_MakeWeddingRing(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                player.MakeWeddingRing();
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ForceDivorce(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                player.NPCDivorce();
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- messages


        internal static ScriptOutcome Exec_LocalMessage(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                ChatType chatType;
                if (!Enum.TryParse(param[1], true, out chatType)) return ScriptOutcome.InvalidArguments;
                player.ReceiveChat(param[0], chatType);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GlobalMessage(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!Enum.TryParse(param[1], true, out ChatType chatType)) return ScriptOutcome.InvalidArguments;

                var p = new S.Chat { Message = param[0], Type = chatType };
                Envir.Broadcast(p);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome ExecMonster_GlobalMessage(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MonsterObject monster = ctx.Monster;

            if (monster == null) return ScriptOutcome.Pass;

            {
                if (!Enum.TryParse(param[1], true, out ChatType chatType)) return ScriptOutcome.InvalidArguments;

                var p = new S.Chat { Message = param[0], Type = chatType };
                Envir.Broadcast(p);
            }

            /* //mobs have no real "delayed" npc code so not added this yet
            case ScriptActionKind.Goto:
            DelayedAction action = new DelayedAction(DelayedType.NPC, -1, player.NPCID, "[" + param[0] + "]");
            player.ActionList.Add(action);
                break;
            */

            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- mail


        internal static ScriptOutcome Exec_ComposeMail(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                ctx.State.Mail = new MailInfo(player.Info.Index, false)
                {
                    Sender = param[1],
                    Message = param[0]
                };
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_AddMailGold(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (ctx.State.Mail == null) return ScriptOutcome.Abort;

                uint.TryParse(param[0], out uint tempUint);

                ctx.State.Mail.Gold += tempUint;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_AddMailItem(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (ctx.State.Mail == null) return ScriptOutcome.Abort;
                if (ctx.State.Mail.Items.Count > 5) return ScriptOutcome.Abort;

                if (param.Count < 2 || !ushort.TryParse(param[1], out ushort count)) count = 1;

                var info = Envir.GetItemInfo(param[0]);

                if (info == null)
                {
                    MessageQueue.Enqueue(GameLanguage.Get("Failed to get ItemInfo: {0}, Page: {1}", param[0], seg.Key));
                    return ScriptOutcome.Pass;
                }

                while (count > 0 && ctx.State.Mail.Items.Count < 5)
                {
                    UserItem item = Envir.CreateFreshItem(info);

                    if (item == null)
                    {
                        MessageQueue.Enqueue(GameLanguage.Get("Failed to create UserItem: {0}, Page: {1}", param[0], seg.Key));
                        return ScriptOutcome.Abort;
                    }

                    if (item.Info.StackSize > count)
                    {
                        item.Count = count;
                        count = 0;
                    }
                    else
                    {
                        count -= item.Info.StackSize;
                        item.Count = item.Info.StackSize;
                    }

                    ctx.State.Mail.Items.Add(item);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_SendMail(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (ctx.State.Mail == null) return ScriptOutcome.Abort;

                ctx.State.Mail.Send();
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- effects


        internal static ScriptOutcome Exec_GiveBuff(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!Enum.TryParse(param[0], true, out BuffType buffType) || !Enum.IsDefined(typeof(BuffType), buffType) ||
                !int.TryParse(param[1], out int duration) || duration < 0 || duration > int.MaxValue / Settings.Second)
                {
                    MessageQueue.Enqueue($"GiveBuff: invalid type or duration, Page: {seg.Key}");
                    return ScriptOutcome.Pass;
                }

                var stats = new Stats();
                bool valid = true;
                // Legacy positional flags are retained for script compatibility. BuffInfo owns buff properties and stacking.
                foreach (string value in param.Skip(2).Where(value => value.Contains('=')))
                {
                    string[] pair = value.Split('=');
                    if (pair.Length != 2 || !Enum.TryParse(pair[0], true, out Stat stat) ||
                    !Enum.IsDefined(typeof(Stat), stat) || !int.TryParse(pair[1], out int amount))
                    {
                        MessageQueue.Enqueue($"GiveBuff: invalid stat '{value}', Page: {seg.Key}");
                        valid = false;
                        break;
                    }
                    stats[stat] = amount;
                }

                if (valid) player.AddBuff(buffType, player, Settings.Second * duration, stats);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_RemoveBuff(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;

            if (obj == null) return ScriptOutcome.Pass;

            {
                if (!Enum.IsDefined(typeof(BuffType), param[0])) return ScriptOutcome.Abort;

                BuffType bType = (BuffType)(byte)Enum.Parse(typeof(BuffType), param[0]);

                obj.RemoveBuff(bType);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome ExecMonster_GiveBuff(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MonsterObject monster = ctx.Monster;

            if (monster == null) return ScriptOutcome.Pass;

            {
                if (!Enum.IsDefined(typeof(BuffType), param[0])) return ScriptOutcome.Abort;

                int.TryParse(param[1], out int tempInt);
                bool.TryParse(param[2], out bool infinite);
                bool.TryParse(param[3], out bool visible);
                bool.TryParse(param[4], out bool stackable);

                monster.AddBuff((BuffType)(byte)Enum.Parse(typeof(BuffType), param[0], true), monster, Settings.Second * tempInt, new Stats(), visible);
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- browser and sound


        internal static ScriptOutcome Exec_OpenBrowser(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                player.Enqueue(new S.OpenBrowser { Url = param[0] });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_PlaySound(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int soundID)) return ScriptOutcome.InvalidArguments;
                player.Enqueue(new S.PlaySound { Sound = soundID });
            }
            return ScriptOutcome.Pass;
        }
    }
}
