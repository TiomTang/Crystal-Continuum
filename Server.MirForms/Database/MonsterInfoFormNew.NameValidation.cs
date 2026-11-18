using System.Data;

namespace Server.Database;

public partial class MonsterInfoFormNew
{
    private readonly Label nameFeedback = new() { Dock = DockStyle.Bottom, AutoSize = true, Visible = false };
    private TextBox nameEditor;

    private void InitializeNameValidation()
    {
        panel2.Controls.Add(nameFeedback);
        nameFeedback.SendToBack();
        monsterInfoGridView.EditingControlShowing += (_, e) =>
        {
            DetachNameEditor();
            if (monsterInfoGridView.CurrentCell?.OwningColumn.Name != nameof(MonsterName) || e.Control is not TextBox editor) return;
            nameEditor = editor;
            editor.TextChanged += NameTextChanged;
        };
        // Binding can recreate the column. Install the parser before its row cells are cloned.
        MonsterName.CellTemplate = new MonsterNameCell();
        monsterInfoGridView.ColumnAdded += (_, e) =>
        {
            if (e.Column.DataPropertyName == nameof(MonsterName))
                e.Column.CellTemplate = new MonsterNameCell();
        };
        monsterInfoGridView.CellEndEdit += (_, _) => DetachNameEditor();
    }

    private sealed class MonsterNameCell : DataGridViewTextBoxCell
    {
        public override object ParseFormattedValue(object formattedValue, DataGridViewCellStyle cellStyle,
            System.ComponentModel.TypeConverter formattedValueTypeConverter, System.ComponentModel.TypeConverter valueTypeConverter)
        {
            if (DataGridView?.FindForm() is MonsterInfoFormNew form && OwningRow is { } row &&
                form.GetNameError(row, formattedValue?.ToString() ?? string.Empty) is { } error)
            {
                form.ShowNameError(error);
                throw new FormatException(error);
            }
            return base.ParseFormattedValue(formattedValue, cellStyle, formattedValueTypeConverter, valueTypeConverter);
        }
    }

    private void DetachNameEditor()
    {
        if (nameEditor != null) nameEditor.TextChanged -= NameTextChanged;
        nameEditor = null;
        ShowNameError(null);
    }

    private void NameTextChanged(object sender, EventArgs e)
    {
        if (monsterInfoGridView.CurrentRow is { } row) ShowNameError(GetNameError(row, nameEditor.Text));
    }

    private void ShowNameError(string error)
    {
        nameFeedback.Text = error ?? string.Empty;
        nameFeedback.Visible = error != null;
    }

    private string GetNameError(DataGridViewRow row, string value)
    {
        if (row.Cells["MonsterName"].Value?.ToString() == value) return null;
        if (!MonsterNameSynchronization.IsValidName(value))
            return GameLanguage.Get("Invalid monster name: check for empty text, whitespace or script delimiters. Press Esc to undo.");
        var current = (row.DataBoundItem as DataRowView)?.Row;
        foreach (DataRow other in Table.Rows)
        {
            if (other == current || !string.Equals(other["MonsterName"].ToString().Replace(" ", ""), value, StringComparison.OrdinalIgnoreCase)) continue;
            return string.Format(GameLanguage.Get("Monster name '{0}' is already used (ID: {1}). Change it or press Esc to undo."), value,
                other.IsNull("MonsterIndex") ? GameLanguage.Get("New") : other["MonsterIndex"]);
        }
        if (row.Cells["MonsterIndex"].Value is int id && Envir.MonsterInfoList.FirstOrDefault(x => x.Index == id) is { } original && original.Name != value)
        {
            if (Envir.MonsterInfoList.Any(x => x.Index != id && string.Equals(x.Name.Replace(" ", ""), original.Name.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)))
                return string.Format(GameLanguage.Get("Original monster name '{0}' is ambiguous. References cannot be renamed. Press Esc to undo."), original.Name);
        }
        if (string.IsNullOrEmpty(row.Cells["MonsterDropPath"].Value?.ToString()) && value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return GameLanguage.Get("Monster name cannot be used as a drop filename. Change it or press Esc to undo.");
        return null;
    }
}
