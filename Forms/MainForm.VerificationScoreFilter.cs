namespace CheckTranslation;

public partial class MainForm
{
    private const string VerificationScoreFilterPrefix = "score<=";
    private readonly Dictionary<string, ComboBox> _filterComboBoxes = new();

    private bool TryCreateSpecialFilterControl(DataGridViewColumn col)
    {
        if (col.DataPropertyName != "Comment")
            return false;

        var comboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Font = new Font(dataGridView.Font.FontFamily, 8.5f),
            Tag = col.DataPropertyName,
        };

        comboBox.Items.AddRange(
        [
            string.Empty,
            "Non vérifiés",
            "≤  50",
            "≤  60",
            "≤  70",
            "≤  80",
            "≤  90",
            "≤ 100",
            "≥  90",
        ]);
        comboBox.SelectedIndex = 0;
        comboBox.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;
        comboBox.GotFocus += (s, _) =>
        {
            if (s is ComboBox cb)
                cb.BackColor = Color.FromArgb(255, 255, 230);
        };
        comboBox.LostFocus += (s, _) =>
        {
            if (s is ComboBox cb)
                UpdateComboBoxBackColor(cb);
        };
        comboBox.KeyDown += (s, args) =>
        {
            if (args.KeyCode == Keys.Escape && s is ComboBox cb)
            {
                cb.SelectedIndex = 0;
                dataGridView.Focus();
                args.SuppressKeyPress = true;
            }
        };

        _filterComboBoxes[col.DataPropertyName] = comboBox;
        dataGridView.Controls.Add(comboBox);
        UpdateComboBoxBackColor(comboBox);
        return true;
    }

    private void UpdateComboBoxBackColor(ComboBox comboBox)
    {
        if (dataGridView.EnableHeadersVisualStyles)
        {
            comboBox.BackColor = SystemColors.Control;
        }
        else
        {
            var backColor = dataGridView.ColumnHeadersDefaultCellStyle.BackColor;
            comboBox.BackColor = backColor == Color.Empty ? SystemColors.Control : backColor;
        }
    }

    private void FilterComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _filterDebounceTimer?.Stop();
        _filterDebounceTimer?.Start();

        if (sender is ComboBox comboBox && comboBox.Tag is string propertyName)
        {
            var column = dataGridView.Columns
                .Cast<DataGridViewColumn>()
                .FirstOrDefault(c => c.DataPropertyName == propertyName);
            if (column is not null)
                dataGridView.InvalidateColumn(column.Index);
        }
    }

    private bool TryLayoutSpecialFilterControl(DataGridViewColumn col)
    {
        if (!_filterComboBoxes.TryGetValue(col.DataPropertyName, out var comboBox))
            return false;

        if (!col.Visible)
        {
            comboBox.Visible = false;
            return true;
        }

        var rect = dataGridView.GetColumnDisplayRectangle(col.Index, false);
        if (rect.Width == 0)
        {
            comboBox.Visible = false;
            return true;
        }

        const int filterHeight = 18;
        const int bottomMargin = 4;
        const int iconWidth = 18;

        comboBox.Visible = true;
        comboBox.SetBounds(
            rect.Left + iconWidth,
            dataGridView.ColumnHeadersHeight - filterHeight - bottomMargin,
            rect.Width - iconWidth - 2,
            filterHeight);
        return true;
    }

    private void ResetSpecialFilters()
    {
        foreach (var comboBox in _filterComboBoxes.Values)
            comboBox.SelectedIndex = 0;
    }

    private void CollectSpecialFilters(IDictionary<string, string> filters)
    {
        foreach (var (prop, comboBox) in _filterComboBoxes)
        {
            if (comboBox.SelectedItem is not string selectedValue || string.IsNullOrWhiteSpace(selectedValue))
                continue;

            filters[prop] = selectedValue switch
            {
                "Non vérifiés" => "score:none",
                "≥  90" => "score>=90",
                _ => $"{VerificationScoreFilterPrefix}{selectedValue.Replace("≤", string.Empty, StringComparison.Ordinal).Trim()}"
            };
        }
    }

    private bool HasFilter(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return false;

        return (_filterTextBoxes.TryGetValue(propertyName, out var textBox) && !string.IsNullOrEmpty(textBox.Text))
            || (_filterComboBoxes.TryGetValue(propertyName, out var comboBox) && comboBox.SelectedIndex > 0);
    }
}
