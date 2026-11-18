using System.Data;
using System.Globalization;

namespace Server.Database;

internal static class MonsterCsvImport
{
    // Work on an unbound copy: even a conversion error in the last cell cannot dirty the editor.
    internal static DataTable Prepare(DataTable source, string[][] records, out int count)
    {
        count = 0;
        if (records.Length < 2) throw new FormatException(GameLanguage.Get("No rows to import."));
        var header = records[0];
        if (header.Length < 2 || header[0] != "MonsterIndex")
            throw new FormatException(GameLanguage.Get("The first column must be MonsterIndex, followed by monster fields."));

        // The editor's exporter writes one empty trailing field.
        int width = header.Length - (header[^1] == "" ? 1 : 0);
        if (width < 2) throw new FormatException(GameLanguage.Get("No columns to import."));
        var table = source.Copy();
        var columns = new DataColumn[width];
        var seen = new HashSet<DataColumn>();
        for (int j = 0; j < width; j++)
        {
            var col = table.Columns[header[j]];
            if (col == null || col.ColumnName == "Modified" || !seen.Add(col))
                throw new FormatException(GameLanguage.Get($"Unknown, repeated or read-only CSV column: {header[j]}"));
            columns[j] = col;
        }

        var byIndex = table.Rows.Cast<DataRow>().Where(r => !r.IsNull("MonsterIndex"))
            .ToDictionary(r => (int)r["MonsterIndex"]);
        var importedIds = new HashSet<int>();
        for (int i = 1; i < records.Length; i++)
        {
            var cells = records[i];
            if (cells.Length != header.Length || (width < header.Length && cells[^1] != ""))
                throw new FormatException(GameLanguage.Get($"CSV row {i + 1}: invalid column count or trailing field."));
            DataRow row = null;
            if (cells[0] != "")
            {
                if (!int.TryParse(cells[0], NumberStyles.None, CultureInfo.InvariantCulture, out int id) || !importedIds.Add(id))
                    throw new FormatException(GameLanguage.Get($"CSV row {i + 1}: invalid or repeated MonsterIndex '{cells[0]}'."));
                byIndex.TryGetValue(id, out row);
            }
            bool isNew = row == null;
            if (isNew) row = NewMonster(table);
            string oldName = (string)row["MonsterName"];
            for (int j = 1; j < width; j++)
            {
                var col = columns[j];
                try
                {
                    object value = col.DataType.IsEnum
                        ? Enum.Parse(col.DataType, cells[j])
                        : Convert.ChangeType(cells[j], col.DataType, CultureInfo.InvariantCulture);
                    if ((col.DataType.IsEnum && (!Enum.IsDefined(col.DataType, value) || cells[j].Contains(','))) || value is int n && n < 0)
                        throw new FormatException();
                    row[col] = value;
                }
                catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
                {
                    throw new FormatException(GameLanguage.Get($"CSV row {i + 1}, {col.ColumnName}: invalid value '{cells[j]}'."), ex);
                }
            }
            var name = (string)row["MonsterName"];
            if (string.IsNullOrWhiteSpace(name) || ((isNew || name != oldName) && !MonsterNameSynchronization.IsValidName(name)))
                throw new FormatException(GameLanguage.Get($"CSV row {i + 1}: invalid monster name '{name}'."));
            row["Modified"] = true;
            if (isNew) table.Rows.Add(row);
            count++;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataRow row in table.Rows)
        {
            var name = row["MonsterName"].ToString();
            if (!names.Add(name.Replace(" ", ""))) throw new FormatException(GameLanguage.Get($"Duplicate final monster name: {name}"));
        }
        table.DefaultView.RowFilter = source.DefaultView.RowFilter;
        table.DefaultView.Sort = source.DefaultView.Sort;
        return table;
    }

    private static DataRow NewMonster(DataTable table)
    {
        var row = table.NewRow();
        foreach (DataColumn col in table.Columns)
        {
            // Leave the persistent ID empty: SaveForm assigns it when the editor is saved.
            if (col.ColumnName == "MonsterIndex") continue;
            row[col] = col.ColumnName switch
            {
                "MonsterAttackSpeed" => (ushort)2500,
                "MonsterMoveSpeed" => (ushort)1800,
                "MonsterViewRange" => (byte)7,
                "MonsterCanTame" or "MonsterCanPush" or "MonsterAutoRev" => true,
                _ => col.DataType == typeof(string) ? "" : Activator.CreateInstance(col.DataType)
            };
        }
        return row;
    }
}
