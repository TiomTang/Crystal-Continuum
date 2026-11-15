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
    /// The actions that act on the world instead of the player: flow, recall, timers, dice, text,
    /// values, monsters, gates, conquest and the game shop.
    public partial class NPCSegment
    {
        // ---------------------------------------------------------------- flow


        internal static ScriptOutcome Exec_Goto(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                DelayedAction action = new DelayedAction(DelayedType.NPC, -1, player.NPCObjectID, player.NPCScriptID, "[" + param[0] + "]");
                player.ActionList.Add(action);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Call(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int scriptID)) return ScriptOutcome.InvalidArguments;

                if (!Envir.Scripts.ContainsKey(scriptID))
                {
                    ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.MissingScript,
                        seg.Page == null ? string.Empty : seg.Page.Key, "CALL", "Unknown script: " + scriptID, seg.Page?.FileName);

                    return ScriptOutcome.Abort;
                }

                var action = new DelayedAction(DelayedType.NPC, -1, player.NPCObjectID, scriptID,
                    ScriptSpecialPageCatalog.Key(ScriptSpecialPageKind.Main));
                player.ActionList.Add(action);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Break(ScriptExecuteContext ctx)
        {
            if (ctx.Call != null) ctx.Call.BreakRequested = true;

            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Close(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                player.NPCDialogClose = true;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GroupGoto(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.GroupMembers == null) return ScriptOutcome.Abort;

                for (int j = 0; j < player.GroupMembers.Count(); j++)
                {
                    var action = new DelayedAction(DelayedType.NPC, Envir.Time, player.NPCObjectID, player.NPCScriptID, "[" + param[0] + "]");
                    player.GroupMembers[j].ActionList.Add(action);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_DelayGoto(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!long.TryParse(param[0], out long tempLong)) return ScriptOutcome.InvalidArguments;

                var action = new DelayedAction(DelayedType.NPC, Envir.Time + (tempLong * 1000), player.NPCObjectID, player.NPCScriptID, "[" + param[1] + "]");
                player.ActionList.Add(action);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_EnterMap(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!player.NPCData.TryGetValue("NPCMoveMap", out object _npcMoveMap) || !player.NPCData.TryGetValue("NPCMoveCoord", out object _npcMoveCoord)) return ScriptOutcome.Abort;

                player.Teleport((Map)_npcMoveMap, (Point)_npcMoveCoord, false);

                player.NPCData.Remove("NPCMoveMap");
                player.NPCData.Remove("NPCMoveCoord");
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- recall


        internal static ScriptOutcome Exec_TimeRecall(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var tempString = "";
                if (!long.TryParse(param[0], out long tempLong)) return ScriptOutcome.InvalidArguments;

                if (param[1].Length > 0) tempString = "[" + param[1] + "]";

                Map tempMap = player.CurrentMap;
                Point tempPoint = player.CurrentLocation;

                var action = new DelayedAction(DelayedType.NPC, Envir.Time + (tempLong * 1000), player.NPCObjectID, player.NPCScriptID, tempString, tempMap, tempPoint);
                player.ActionList.Add(action);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TimeRecallGroup(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var tempString = "";
                if (player.GroupMembers == null) return ScriptOutcome.Abort;
                if (!long.TryParse(param[0], out long tempLong)) return ScriptOutcome.InvalidArguments;
                if (param[1].Length > 0) tempString = "[" + param[1] + "]";

                for (int j = 0; j < player.GroupMembers.Count(); j++)
                {
                    var groupMember = player.GroupMembers[j];

                    var action = new DelayedAction(DelayedType.NPC, Envir.Time + (tempLong * 1000), player.NPCObjectID, player.NPCScriptID, tempString, player.CurrentMap, player.CurrentLocation);
                    groupMember.ActionList.Add(action);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_BreakTimeRecall(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                foreach (DelayedAction ac in player.ActionList.Where(u => u.Type == DelayedType.NPC))
                {
                    ac.FlaggedToRemove = true;
                }
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- timers


        internal static ScriptOutcome Exec_SetTimer(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[1], out int seconds) || !byte.TryParse(param[2], out byte type)) return ScriptOutcome.InvalidArguments;

                bool.TryParse(param[3], out bool global);

                if (seconds < 0) seconds = 0;

                if (global)
                {
                    var timerKey = "_-" + param[0];

                    Envir.Timers[timerKey] = new Timer(timerKey, seconds, type);
                }
                else
                {
                    player.SetTimer(param[0], seconds, type);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ExpireTimer(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var globalTimerKey = "_-" + param[0];

                if (Envir.Timers.ContainsKey(globalTimerKey))
                {
                    Envir.Timers.Remove(globalTimerKey);
                }

                player.ExpireTimer(param[0]);
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- dice


        internal static ScriptOutcome Exec_RollDie(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                bool.TryParse(param[1], out bool autoRoll);

                var result = Envir.Random.Next(1, 7);

                S.Roll p = new S.Roll { Type = 0, Page = param[0], AutoRoll = autoRoll, Result = result };

                player.NPCData["NPCRollResult"] = result;
                player.Enqueue(p);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_RollYut(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                bool.TryParse(param[1], out bool autoRoll);

                var result = Envir.Random.Next(1, 7);

                S.Roll p = new S.Roll { Type = 1, Page = param[0], AutoRoll = autoRoll, Result = result };

                player.NPCData["NPCRollResult"] = result;
                player.Enqueue(p);
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- text


        internal static ScriptOutcome Exec_GetRandomText(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                string randomTextPath = Path.Combine(Settings.NPCPath, param[0]);
                if (!File.Exists(randomTextPath))
                {
                    MessageQueue.Enqueue(GameLanguage.Get("the randomTextFile:{0} does not exist.", randomTextPath));
                }
                else
                {
                    var lines = File.ReadAllLines(randomTextPath);
                    int index = Envir.Random.Next(0, lines.Length);
                    string randomText = lines[index];
                    seg.AddVariable(player, param[1], randomText);
                }
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- values


        internal static ScriptOutcome Exec_LoadValue(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                string val = param[0];
                string filePath = param[1];
                string header = param[2];
                string key = param[3];

                InIReader reader = new InIReader(filePath);
                string loadedString = reader.ReadString(header, key, "");

                if (loadedString == "") return ScriptOutcome.Pass;
                seg.AddVariable(player, val, loadedString);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_SaveValue(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                string filePath = param[0];
                string header = param[1];
                string key = param[2];
                string val = param[3];

                InIReader reader = new InIReader(filePath);
                reader.Write(header, key, val);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome ExecMonster_LoadValue(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MonsterObject monster = ctx.Monster;

            if (monster == null) return ScriptOutcome.Pass;

            {
                string val = param[0];
                string filePath = param[1];
                string header = param[2];
                string key = param[3];

                var reader = new InIReader(filePath);
                string loadedString = reader.ReadString(header, key, "");

                if (loadedString == "") return ScriptOutcome.Pass;
                seg.AddVariable(monster, val, loadedString);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome ExecMonster_SaveValue(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MonsterObject monster = ctx.Monster;

            if (monster == null) return ScriptOutcome.Pass;

            {
                string filePath = param[0];
                string header = param[1];
                string key = param[2];
                string val = param[3];

                var reader = new InIReader(filePath);
                reader.Write(header, key, val);
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- monsters


        internal static ScriptOutcome Exec_Mongen(ScriptExecuteContext ctx)
        {
            ScriptPageState state = ctx.Call?.PageState;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (state == null || state.Param1 == null || state.Param2 == 0 || state.Param3 == 0) return ScriptOutcome.Abort;
                if (!byte.TryParse(param[1], out byte tempByte)) return ScriptOutcome.InvalidArguments;

                Map targetmap = Envir.GetMapByNameAndInstance(state.Param1, state.Param1Instance);
                if (targetmap == null) return ScriptOutcome.Abort;

                var monInfo = Envir.GetMonsterInfo(param[0]);
                if (monInfo == null) return ScriptOutcome.Abort;

                for (int j = 0; j < tempByte; j++)
                {
                    MonsterObject monster = MonsterObject.GetMonster(monInfo);
                    if (monster == null) return ScriptOutcome.Abort;
                    monster.Direction = 0;
                    monster.ActionTime = Envir.Time + 1000;
                    monster.Spawn(targetmap, new Point(state.Param2, state.Param3));
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_MonClear(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[1], out int tempInt)) return ScriptOutcome.InvalidArguments;

                var targetmap = Envir.GetMapByNameAndInstance(param[0], tempInt);
                if (targetmap == null) return ScriptOutcome.Abort;

                foreach (var cell in targetmap.Cells)
                {
                    if (cell == null || cell.Objects == null) continue;

                    for (int j = 0; j < cell.Objects.Count(); j++)
                    {
                        MapObject ob = cell.Objects[j];

                        if (ob.Race != ObjectType.Monster) continue;
                        if (ob.Dead) continue;

                        if (!string.IsNullOrEmpty(param[2]) && string.Compare(param[2], ((MonsterObject)ob).Info.Name, true) != 0)
                            continue;

                        ob.Die();
                    }
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome ExecMonster_Mongen(ScriptExecuteContext ctx)
        {
            ScriptPageState state = ctx.Call?.PageState;
            List<string> param = ctx.Params;
            MonsterObject monster = ctx.Monster;

            if (monster == null) return ScriptOutcome.Pass;

            {
                if (state == null || state.Param1 == null || state.Param2 == 0 || state.Param3 == 0) return ScriptOutcome.Abort;
                if (!byte.TryParse(param[1], out byte tempByte)) return ScriptOutcome.InvalidArguments;

                var map = Envir.GetMapByNameAndInstance(state.Param1, state.Param1Instance);
                if (map == null) return ScriptOutcome.Abort;

                var monInfo = Envir.GetMonsterInfo(param[0]);
                if (monInfo == null) return ScriptOutcome.Abort;

                for (int j = 0; j < tempByte; j++)
                {
                    MonsterObject mob = MonsterObject.GetMonster(monInfo);
                    if (mob == null) return ScriptOutcome.Abort;
                    mob.Direction = 0;
                    mob.ActionTime = Envir.Time + 1000;
                    mob.Spawn(map, new Point(state.Param2, state.Param3));
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome ExecMonster_MonClear(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MonsterObject monster = ctx.Monster;

            if (monster == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[1], out int tempInt)) return ScriptOutcome.InvalidArguments;

                var map = Envir.GetMapByNameAndInstance(param[0], tempInt);
                if (map == null) return ScriptOutcome.Abort;

                foreach (var cell in map.Cells)
                {
                    if (cell == null || cell.Objects == null) continue;

                    for (int j = 0; j < cell.Objects.Count(); j++)
                    {
                        MapObject ob = cell.Objects[j];

                        if (ob.Race != ObjectType.Monster) continue;
                        if (ob.Dead) continue;
                        ob.Die();
                    }
                }
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- gates


        internal static ScriptOutcome Exec_OpenGate(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
                var Conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (Conquest == null) return ScriptOutcome.Abort;

                if (!int.TryParse(param[1], out tempInt)) return ScriptOutcome.InvalidArguments;
                ConquestGuildGateInfo OpenGate = Conquest.GateList.FirstOrDefault(z => z.Index == tempInt);
                if (OpenGate == null) return ScriptOutcome.Abort;
                if (OpenGate.Gate == null) return ScriptOutcome.Abort;
                OpenGate.Gate.OpenDoor();
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CloseGate(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (!int.TryParse(param[1], out tempInt)) return ScriptOutcome.InvalidArguments;
                ConquestGuildGateInfo CloseGate = conquest.GateList.FirstOrDefault(z => z.Index == tempInt);
                if (CloseGate == null) return ScriptOutcome.Abort;
                if (CloseGate.Gate == null) return ScriptOutcome.Abort;
                CloseGate.Gate.CloseDoor();
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- conquest


        internal static ScriptOutcome Exec_ConquestGuard(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (!int.TryParse(param[1], out tempInt)) return ScriptOutcome.InvalidArguments;
                ConquestGuildArcherInfo conquestArcher = conquest.ArcherList.FirstOrDefault(z => z.Index == tempInt);
                if (conquestArcher == null) return ScriptOutcome.Abort;

                if (conquestArcher.ArcherMonster != null)
                    if (!conquestArcher.ArcherMonster.Dead) return ScriptOutcome.Abort;

                if (player.IsGM)
                {
                    conquestArcher.Spawn(true);
                }
                else
                {
                    if (player.MyGuild == null || player.MyGuild.Gold < conquestArcher.GetRepairCost()) return ScriptOutcome.Abort;

                    player.MyGuild.Gold -= conquestArcher.GetRepairCost();
                    player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Amount = conquestArcher.GetRepairCost() });

                    conquestArcher.Spawn(true);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ConquestGate(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (!int.TryParse(param[1], out tempInt)) return ScriptOutcome.InvalidArguments;
                ConquestGuildGateInfo conquestGate = conquest.GateList.FirstOrDefault(z => z.Index == tempInt);
                if (conquestGate == null) return ScriptOutcome.Abort;

                if (player.IsGM)
                {
                    conquestGate.Repair();
                }
                else
                {
                    if (player.MyGuild == null || player.MyGuild.Gold < conquestGate.GetRepairCost()) return ScriptOutcome.Abort;

                    player.MyGuild.Gold -= (uint)conquestGate.GetRepairCost();
                    player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Amount = (uint)conquestGate.GetRepairCost() });

                    conquestGate.Repair();
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ConquestWall(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (!int.TryParse(param[1], out tempInt)) return ScriptOutcome.InvalidArguments;
                ConquestGuildWallInfo conquestWall = conquest.WallList.FirstOrDefault(z => z.Index == tempInt);

                if (conquestWall == null) return ScriptOutcome.Abort;

                if (player.IsGM)
                {
                    conquestWall.Repair();
                }
                else
                {
                    if (player.MyGuild == null || player.MyGuild.Gold < conquestWall.GetRepairCost()) return ScriptOutcome.Abort;

                    player.MyGuild.Gold -= (uint)conquestWall.GetRepairCost();
                    player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Amount = (uint)conquestWall.GetRepairCost() });

                    conquestWall.Repair();
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_SetConquestRate(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (!byte.TryParse(param[1], out byte tempByte)) return ScriptOutcome.InvalidArguments;
                if (player.MyGuild != null && player.MyGuild.Guildindex == conquest.GuildInfo.Owner)
                {
                    conquest.GuildInfo.NPCRate = tempByte;
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_StartConquest(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (!conquest.WarIsOn)
                {
                    conquest.StartType = ConquestType.Forced;
                    conquest.StartWar(conquest.GameType);

                    MessageQueue.Enqueue(GameLanguage.Get("{0} War Started.", conquest.Info.Name));

                }
                else
                {
                    conquest.WarIsOn = false;

                    MessageQueue.Enqueue(GameLanguage.Get("{0} War Stopped.", conquest.Info.Name));
                }

                foreach (var pl in Envir.Players)
                {
                    if (conquest.WarIsOn)
                    {
                        pl.ReceiveChat(GameLanguage.Get("{0} War Started.", conquest.Info.Name), ChatType.System);
                    }
                    else
                    {
                        pl.ReceiveChat(GameLanguage.Get("{0} War Stopped.", conquest.Info.Name), ChatType.System);
                    }

                    pl.BroadcastInfo();
                }

            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ScheduleConquest(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (player.MyGuild != null && player.MyGuild.Guildindex != conquest.GuildInfo.Owner && !conquest.WarIsOn)
                {
                    conquest.GuildInfo.AttackerID = player.MyGuild.Guildindex;
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ConquestRepairAll(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            if (!player.IsGM)
            {
                player.ReceiveChat(GameLanguage.Get("You are not a GM and this command is not enabled for you."), ChatType.System);
                MessageQueue.Enqueue(GameLanguage.Get("GM Command @CONQUESTREPAIRALL invoked by non-GM player: {0}", player.Name));
                return ScriptOutcome.Abort;
            }

            if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
            var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
            if (conquest == null) return ScriptOutcome.Abort;

            MessageQueue.Enqueue(GameLanguage.Get("@CONQUESTREPAIRALL invoked by GM: {0} on account index: {1}", player.Name, player.Info.AccountInfo.Index));
            MessageQueue.Enqueue(GameLanguage.Get("Conquest: {0}", conquest.Info.Name));

            if (conquest.Guild != null)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Owner: {0}", conquest.Guild.Name));
            }
            else
            {
                MessageQueue.Enqueue(GameLanguage.Get("No current owner."));
            }

            int _fixed = 0;
            foreach (ConquestGuildArcherInfo archer in conquest.ArcherList)
            {
                if (archer.ArcherMonster != null &&
                archer.ArcherMonster.Dead)
                {
                    archer.Spawn(true);
                    _fixed++;
                }
            }
            player.ReceiveChat(GameLanguage.Get("Archers repaired: {0}/{1}", _fixed, conquest.ArcherList.Count), ChatType.System);
            MessageQueue.Enqueue(GameLanguage.Get("Archers repaired: {0}/{1}", _fixed, conquest.ArcherList.Count));

            _fixed = 0;
            foreach (ConquestGuildGateInfo conquestGate in conquest.GateList)
            {
                if (conquestGate != null)
                {
                    conquestGate.Repair();
                    _fixed++;
                }
            }
            player.ReceiveChat(GameLanguage.Get("Gates repaired: {0}/{1}", _fixed, conquest.GateList.Count), ChatType.System);
            MessageQueue.Enqueue(GameLanguage.Get("Gates repaired: {0}/{1}", _fixed, conquest.GateList.Count));

            _fixed = 0;
            foreach (ConquestGuildWallInfo conquestWall in conquest.WallList)
            {
                if (conquestWall != null)
                {
                    conquestWall.Repair();
                    _fixed++;
                }
            }
            player.ReceiveChat(GameLanguage.Get("Walls repaired: {0}/{1}", _fixed, conquest.WallList.Count), ChatType.System);
            MessageQueue.Enqueue(GameLanguage.Get("Walls repaired: {0}/{1}", _fixed, conquest.WallList.Count));

            _fixed = 0;
            foreach (ConquestGuildSiegeInfo conquestSiege in conquest.SiegeList)
            {
                if (conquestSiege != null)
                {
                    conquestSiege.Repair();
                    _fixed++;
                }
            }
            player.ReceiveChat(GameLanguage.Get("Sieges repaired: {0}/{1}", _fixed, conquest.SiegeList.Count), ChatType.System);
            MessageQueue.Enqueue(GameLanguage.Get("Sieges repaired: {0}/{1}", _fixed, conquest.SiegeList.Count));

            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- game shop


        internal static ScriptOutcome Exec_BuyGT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            if (player.MyGuild == null || player.MyGuildRank == null) return ScriptOutcome.Abort;
            if (player.MyGuildRank != player.MyGuild.Ranks[0])
            {
                player.ReceiveChat(GameLanguage.Get("You need to be Guild Leader!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (player.MyGuild.Gold < Settings.BuyGTGold)
            {
                player.ReceiveChat(GameLanguage.Get("Insufficient Guild funds!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (player.MyGuild.HasGT)
            {
                player.ReceiveChat(GameLanguage.Get("You already own a Guild Territory!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            GTMap GTmap = null;
            foreach (var gt in Envir.GTMapList)
            {
                if (gt.Owner == "None")
                {
                    GTmap = gt;
                    break;
                }
            }

            if (GTmap == null)
            {
                player.ReceiveChat(GameLanguage.Get("No Guild Territory's available!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            player.MyGuild.Gold -= (uint)Settings.BuyGTGold;
            player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Amount = (uint)Settings.BuyGTGold });
            player.ReceiveChat(GameLanguage.Get("You rented a Guild Territory!"), ChatType.System);
            player.MyGuild.GTIndex = GTmap.Index;
            player.MyGuild.GTRent = DateTime.Now.AddDays(Settings.GTDays);
            player.MyGuild.GTKey = Envir.Random.Next(100, int.MaxValue - 100);
            player.MyGuild.GTPrice = 0;
            GTmap.Owner = player.MyGuild.Name;
            GTmap.Leader = player.MyGuild.Ranks[0].Members[0].Name;
            if (player.MyGuild.Ranks[0].Members.Count > 1)
                GTmap.Leader = player.MyGuild.Ranks[0].Members[1].Name;
            GTmap.Price = 0;
            GTmap.Key = player.MyGuild.GTKey;

            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ExtendGT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            GTMap GTmap = null;

            if (player == null) return ScriptOutcome.Pass;

            if (player.MyGuild == null || player.MyGuildRank == null) return ScriptOutcome.Abort;
            if (player.MyGuildRank != player.MyGuild.Ranks[0])
            {
                player.ReceiveChat(GameLanguage.Get("You need to be Guild Leader!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (player.MyGuild.Gold < Settings.ExtendGT)
            {
                player.ReceiveChat(GameLanguage.Get("Insufficient funds!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            GTmap = null;
            foreach (var gt in Envir.GTMapList)
            {
                if (gt.Owner == player.MyGuild.Name)
                {
                    GTmap = gt;
                    break;
                }
            }

            if (GTmap == null)
            {
                player.ReceiveChat(GameLanguage.Get("You dont own any GT!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            player.MyGuild.Gold -= (uint)Settings.ExtendGT;
            player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Amount = (uint)Settings.ExtendGT });

            player.MyGuild.GTRent = player.MyGuild.GTRent.AddDays(Settings.GTDays);
            GTmap.Price = 10000000;
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GTRecall(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            if (player.MyGuild == null)
            {
                player.ReceiveChat(GameLanguage.Get("To summon a guild member, one first must be in a guild."), ChatType.System);
                return ScriptOutcome.Abort;
            }
            if (player.MyGuildRank.Index != 0)
            {
                player.ReceiveChat(GameLanguage.Get("Only the leader of {0} may summon members.", player.MyGuild.Name), ChatType.System);
                return ScriptOutcome.Abort;
            }
            PlayerObject guildmember = Envir.GetPlayer(param[0]);
            if (guildmember == null)
            {
                player.ReceiveChat(GameLanguage.Get("I can not summon {0} as they do not appear to exist", param[0]), ChatType.System);
                return ScriptOutcome.Abort;
            }
            if (guildmember.Connection == null)
            {
                player.ReceiveChat(GameLanguage.Get("I can not summon {0} at this time", param[0]), ChatType.System);
                return ScriptOutcome.Pass; //really shouldnt be needed but better safe than sorry
            }
            if (guildmember.Connection.Stage != GameStage.Game)
            {
                player.ReceiveChat(GameLanguage.Get("{0} is not currently in-game", param[0]), ChatType.System);
                return ScriptOutcome.Abort;
            }
            if (guildmember.MyGuild == null)
            {
                player.ReceiveChat(GameLanguage.Get("{0} is not in a guild", param[0]), ChatType.System);
                return ScriptOutcome.Abort;
            }
            if (guildmember.MyGuild.Name != player.MyGuild.Name)
            {
                player.ReceiveChat(GameLanguage.Get("{0} is not in {1}. They are in {2}", param[0], player.MyGuild.Name, guildmember.MyGuild.Name), ChatType.System);
                return ScriptOutcome.Abort;
            }
            guildmember.Teleport(player.CurrentMap, new Point(player.CurrentLocation.X + Envir.Random.Next(4), player.CurrentLocation.Y + Envir.Random.Next(4)));
            guildmember.ReceiveChat(GameLanguage.Get("You have been summoned by {0}", player.Name), ChatType.System);
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GTAllRecall(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            if (player.MyGuild == null)
            {
                player.ReceiveChat(GameLanguage.Get("To summon guild members, one first must be in a guild."), ChatType.System);
                return ScriptOutcome.Abort;
            }
            if (player.MyGuildRank.Index != 0)
            {
                player.ReceiveChat(GameLanguage.Get("Only the leader of {0} may summon all members.", player.MyGuild.Name), ChatType.System);
                return ScriptOutcome.Abort;
            }
            //ok were confident the player has a guild and is rank 0 (which we assume is leader)
            foreach (PlayerObject ob in Envir.Players) //thers no accessable member list from the player.myguild....hmmm ok we will look at all players curently online
            {
                if (ob == null)
                {
                    continue;//ignore people that dont exist, shouldnt happen but lets check for it
                }
                if (ob.Connection == null)
                {
                    continue; //really shouldnt be needed but better safe than sorry
                }
                if (ob.Connection.Stage != GameStage.Game)
                {
                    continue; //shouldnt be needed but lets make sure the player is in game
                }
                if (ob.MyGuild == null)
                {
                    continue; //they aint in a guild? lets not check them further (oops)
                }
                if (ob.MyGuild.Name != player.MyGuild.Name)
                {
                    continue; //not in my guild?who wants you!?
                }
                if (ob.Name == player.Name)
                {
                    continue;//dont try to recall yourself
                }
                ob.Teleport(player.CurrentMap, new Point(player.CurrentLocation.X + Envir.Random.Next(4), player.CurrentLocation.Y + Envir.Random.Next(4)));
                ob.ReceiveChat(GameLanguage.Get("You have been summoned by ") + player.Name, ChatType.System);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GTSale(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            if (player.MyGuild == null)
                return ScriptOutcome.Abort;

            if (player.MyGuildRank.Index != 0 || player.CurrentMap.Info.GTIndex != player.MyGuild.GTIndex)
            {
                player.ReceiveChat(GameLanguage.Get("Only the leader of {0} may initiate sale.", player.MyGuild.Name), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (!int.TryParse(param[0], out int saleprice))
                return ScriptOutcome.Abort;


            if (saleprice < 2000000)
            {
                player.ReceiveChat(GameLanguage.Get("Minimum sale price must be 2,000,000"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (!player.MyGuild.GTForSale(player, saleprice))
                return ScriptOutcome.Abort;

            player.ReceiveChat(GameLanguage.Get("This territory has been listed for sale."), ChatType.System);
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GTCancelSale(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            if (player.MyGuild == null)
                return ScriptOutcome.Abort;

            if (player.MyGuildRank.Index != 0 || player.CurrentMap.Info.GTIndex != player.MyGuild.GTIndex)
            {
                player.ReceiveChat(GameLanguage.Get("Only the leader of {0} may cancel sale.", player.MyGuild.Name), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (!player.MyGuild.EndGTSale(player))
                return ScriptOutcome.Abort;

            player.ReceiveChat(GameLanguage.Get("This territory sale has been cancelled."), ChatType.System);
            return ScriptOutcome.Pass;
        }
    }
}
