namespace Server.MirForms.DropBuilder
{
    /// <summary>
    /// Item type tabs of the drop editor and the ";Header" lines they map to.
    /// Shared by the standalone drop builder and the monster form so both read and write
    /// the same sections.
    /// </summary>
    internal static class DropFileSchema
    {
        public static readonly string[] Tags =
        {
            "Weapon", "Armour", "Helmet", "Necklace", "Bracelet", "Ring", "Amulet", "Belt", "Boots",
            "Stone", "Torch", "Potion", "Ore", "Meat", "CraftingMaterial", "Scroll", "Gem", "Mount",
            "Book", "Nothing", "Script", "Reins", "Bells", "Saddle", "Ribbon", "Mask", "Food", "Hook",
            "Float", "Bait", "Finder", "Reel", "Fish", "Quest", "Awakening", "Pets", "Transform"
        };

        public static readonly string[] Headers =
        {
            ";Weapons", ";Armours", ";Helmets", ";Necklaces", ";Bracelets", ";Rings", ";Amulets", ";Belts", ";Boots",
            ";Stones", ";Torches", ";Potions", ";Ores", ";Meat", ";Crafting Materials", ";Scrolls", ";Gems", ";Mount",
            ";Books", ";Nothing", ";Script", ";Reins", ";Bells", ";Saddle", ";Ribbon", ";Mask", ";Food", ";Hook",
            ";Float", ";Bait", ";Finder", ";Reel", ";Fish", ";Quest", ";Awakening", ";Pets", ";Transform"
        };

        public static readonly string[][] Aliases =
        {
            new[] { "weapons", "weapon" },
            new[] { "armours", "armour", "armors", "armor" },
            new[] { "helmets", "helmet" },
            new[] { "necklaces", "necklace" },
            new[] { "bracelets", "bracelet" },
            new[] { "rings", "ring" },
            new[] { "amulets", "amulet" },
            new[] { "belts", "belt" },
            new[] { "boots", "boot", "shoes", "shoe" },
            new[] { "stones", "stone" },
            new[] { "torches", "torch" },
            new[] { "potions", "potion", "pots + other", "pots", "pot" },
            new[] { "ores", "ore" },
            new[] { "meat", "meats" },
            new[] { "crafting materials", "crafting material", "craftingmaterials", "craftingmaterial", "crafting materals", "craftingmaterals" },
            new[] { "scrolls", "scroll" },
            new[] { "gems", "gem" },
            new[] { "mount", "mounts" },
            new[] { "books", "book" },
            new[] { "nothing" },
            new[] { "script", "scripts" },
            new[] { "reins" },
            new[] { "bells" },
            new[] { "saddle" },
            new[] { "ribbon" },
            new[] { "mask" },
            new[] { "food" },
            new[] { "hook" },
            new[] { "float" },
            new[] { "bait" },
            new[] { "finder" },
            new[] { "reel" },
            new[] { "fish" },
            new[] { "quest", "quests" },
            new[] { "awakening" },
            new[] { "pets", "pet" },
            new[] { "transform" }
        };

        public static int IndexForTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return -1;

            for (int i = 0; i < Tags.Length; i++)
                if (string.Equals(Tags[i], tag, StringComparison.OrdinalIgnoreCase)) return i;

            return -1;
        }

        public static int IndexForHeader(string header)
        {
            if (string.IsNullOrEmpty(header)) return -1;

            for (int i = 0; i < Aliases.Length; i++)
                if (Aliases[i].Contains(header)) return i;

            return -1;
        }

        public static bool TryParseLine(string text, out string odds, out string name, out string quest)
        {
            odds = null;
            name = null;
            quest = string.Empty;

            var parts = (text ?? string.Empty).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;
            if (!parts[0].StartsWith("1/", StringComparison.Ordinal)) return false;
            if (!int.TryParse(parts[0].Substring(2), out int chance) || chance < 1) return false;

            odds = parts[0];
            name = parts[1];

            if (parts.Length > 2 && string.Equals(parts[2], "Q", StringComparison.OrdinalIgnoreCase)) quest = "Q";

            return true;
        }
    }
}
