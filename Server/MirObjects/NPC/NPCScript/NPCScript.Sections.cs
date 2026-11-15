using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using System.Text.RegularExpressions;
using S = ServerPackets;

namespace Server.MirObjects
{
    public partial class NPCScript
    {
        internal void ParseTypeSection(ScriptCompilation compilation, int headerIndex)
        {
            foreach (ScriptSectionLine entry in ScriptSectionCatalog.Body(compilation.Lines, headerIndex, ScriptSectionKind.Types))
            {
                int index;

                if (int.TryParse(entry.Text, out index)) Types.Add((ItemType)index);
            }
        }

        internal void ParseUsedTypeSection(ScriptCompilation compilation, int headerIndex)
        {
            foreach (ScriptSectionLine entry in ScriptSectionCatalog.Body(compilation.Lines, headerIndex, ScriptSectionKind.UsedTypes))
            {
                int index;

                if (int.TryParse(entry.Text, out index)) UsedTypes.Add((ItemType)index);
            }
        }

        internal void ParseGoodsSection(ScriptCompilation compilation, int headerIndex)
        {
            foreach (ScriptSectionLine entry in ScriptSectionCatalog.Body(compilation.Lines, headerIndex, ScriptSectionKind.Trade))
            {
                string line = entry.Text;
                string[] data = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (data.Length == 0) continue;

                ItemInfo info = Envir.GetItemInfo(data[0]);

                if (info == null) continue;

                UserItem goods = Envir.CreateShopItem(info, (uint)entry.Index);

                if (goods == null || Goods.Contains(goods))
                {
                    MessageQueue.Enqueue(GameLanguage.Get("Could not find Item: {0}, File: {1}", line, FileName));
                    continue;
                }

                ushort count = 1;

                if (data.Length == 2) ushort.TryParse(data[1], out count);

                goods.Count = count;

                Goods.Add(goods);
            }
        }

        internal void ParseQuestsSection(ScriptCompilation compilation, int headerIndex)
        {
            NPCObject loadedNPC = NPCObject.Get(LoadedObjectID);

            if (loadedNPC == null) return;

            foreach (ScriptSectionLine entry in ScriptSectionCatalog.Body(compilation.Lines, headerIndex, ScriptSectionKind.Quests))
            {
                int index;

                int.TryParse(entry.Text, out index);

                if (index == 0) continue;

                QuestInfo info = Envir.GetQuestInfo(Math.Abs(index));

                if (info == null)
                {
                    ScriptDiagnostics.Report(ScriptDiagnosticKind.MalformedSection, new ScriptParseContext
                    {
                        Line = entry.Text,
                        LineNumber = compilation.SourceAt(entry.Index).Number,
                        FileName = compilation.SourceAt(entry.Index).FileName,
                    }, "Quests", "Unknown quest: " + index);

                    continue;
                }

                if (index > 0) info.NpcIndex = LoadedObjectID;
                else info.FinishNpcIndex = LoadedObjectID;

                if (loadedNPC.Quests.All(x => x != info)) loadedNPC.Quests.Add(info);
            }
        }

        internal void ParseSpeechSection(ScriptCompilation compilation, int headerIndex)
        {
            NPCObject loadedNPC = NPCObject.Get(LoadedObjectID);

            if (loadedNPC == null) return;

            foreach (ScriptSectionLine entry in ScriptSectionCatalog.Body(compilation.Lines, headerIndex, ScriptSectionKind.Speech))
            {
                string line = entry.Text;
                string[] parts = line.Split(' ');
                int weight = int.Parse(parts[0]);

                loadedNPC.Speech.Add(new NPCSpeech { Weight = weight, Message = line.Substring(parts[0].Length + 1) });
            }
        }

        internal void ParseCraftingSection(ScriptCompilation compilation, int headerIndex)
        {
            foreach (ScriptSectionLine entry in ScriptSectionCatalog.Body(compilation.Lines, headerIndex, ScriptSectionKind.Recipe))
            {
                string line = entry.Text;
                string[] data = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (data.Length == 0) continue;

                ItemInfo info = Envir.GetItemInfo(data[0]);

                if (info == null) continue;

                RecipeInfo recipe = Envir.RecipeInfoList.SingleOrDefault(x => x.MatchItem(info.Index));

                if (recipe == null)
                {
                    MessageQueue.Enqueue(GameLanguage.Get("Could not find recipe: {0}, File: {1}", line, FileName));
                    continue;
                }

                if (recipe.Ingredients.Count == 0)
                {
                    MessageQueue.Enqueue(GameLanguage.Get("Could not find ingredients: {0}, File: {1}", line, FileName));
                    continue;
                }

                CraftGoods.Add(recipe);
            }
        }
    }
}
