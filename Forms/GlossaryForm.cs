using System.ComponentModel;

namespace CheckTranslation;

internal sealed class GlossaryForm : Form
{
    private readonly IGlossaryService _glossaryService;

    private readonly ComboBox _languageCombo;
    private readonly DataGridView _grid;
    private readonly Button _btnAdd;
    private readonly Button _btnRemove;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;
    private readonly Label _lblCount;

    private readonly Dictionary<string, SortableBindingList<GlossaryEntry>> _bindingsByLanguage = new(StringComparer.OrdinalIgnoreCase);
    private string _currentLanguageCode = string.Empty;
    private bool _dirty;

    public GlossaryForm() : this(new GlossaryService())
    {
    }

    public GlossaryForm(IGlossaryService glossaryService)
    {
        _glossaryService = glossaryService;

        Text = "Glossaire métier";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 500);
        ClientSize = new Size(1100, 600);
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = true;
        FormBorderStyle = FormBorderStyle.Sizable;

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(10, 8, 10, 4),
        };

        var lblLanguage = new Label
        {
            Text = "Langue :",
            AutoSize = true,
            Location = new Point(10, 12),
        };

        _languageCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(80, 8),
            Width = 220,
        };
        _languageCombo.SelectedIndexChanged += LanguageCombo_SelectedIndexChanged;

        _lblCount = new Label
        {
            AutoSize = true,
            Location = new Point(320, 12),
            ForeColor = SystemColors.GrayText,
        };

        topPanel.Controls.Add(lblLanguage);
        topPanel.Controls.Add(_languageCombo);
        topPanel.Controls.Add(_lblCount);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colSource",
            HeaderText = "Source",
            DataPropertyName = nameof(GlossaryEntry.Source),
            FillWeight = 30,
            SortMode = DataGridViewColumnSortMode.Automatic,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colDestination",
            HeaderText = "Destination",
            DataPropertyName = nameof(GlossaryEntry.Destination),
            FillWeight = 30,
            SortMode = DataGridViewColumnSortMode.Automatic,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colContext",
            HeaderText = "Contexte",
            DataPropertyName = nameof(GlossaryEntry.Context),
            FillWeight = 40,
            SortMode = DataGridViewColumnSortMode.Automatic,
        });
        _grid.CellValueChanged += (_, _) => MarkDirty();
        _grid.UserAddedRow += (_, _) => MarkDirty();
        _grid.UserDeletedRow += (_, _) => MarkDirty();

        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(10, 8, 10, 8),
        };

        _btnAdd = new Button
        {
            Text = "Ajouter",
            Location = new Point(10, 10),
            Width = 100,
        };
        _btnAdd.Click += (_, _) => AddEntry();

        _btnRemove = new Button
        {
            Text = "Supprimer",
            Location = new Point(120, 10),
            Width = 100,
        };
        _btnRemove.Click += (_, _) => RemoveSelectedEntries();

        _btnCancel = new Button
        {
            Text = "Annuler",
            DialogResult = DialogResult.Cancel,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(bottomPanel.Width - 230, 10),
            Width = 100,
        };

        _btnOk = new Button
        {
            Text = "Enregistrer",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(bottomPanel.Width - 120, 10),
            Width = 100,
        };
        _btnOk.Click += BtnOk_Click;

        bottomPanel.Controls.Add(_btnAdd);
        bottomPanel.Controls.Add(_btnRemove);
        bottomPanel.Controls.Add(_btnCancel);
        bottomPanel.Controls.Add(_btnOk);
        bottomPanel.Resize += (_, _) =>
        {
            _btnCancel.Location = new Point(bottomPanel.Width - 230, 10);
            _btnOk.Location = new Point(bottomPanel.Width - 120, 10);
        };

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        Controls.Add(_grid);
        Controls.Add(topPanel);
        Controls.Add(bottomPanel);

        InitLanguages();
        FormClosing += GlossaryForm_FormClosing;
    }

    /// <summary>
    /// Ouvre l'éditeur en pré-sélectionnant une langue particulière.
    /// </summary>
    public void SelectLanguage(string languageCode)
    {
        for (int i = 0; i < _languageCombo.Items.Count; i++)
        {
            if (_languageCombo.Items[i] is LanguageInfo lang
                && string.Equals(lang.Code, languageCode, StringComparison.OrdinalIgnoreCase))
            {
                _languageCombo.SelectedIndex = i;
                return;
            }
        }
    }

    private void InitLanguages()
    {
        foreach (var lang in MainForm.Languages)
            _languageCombo.Items.Add(lang);
        _languageCombo.DisplayMember = nameof(LanguageInfo.Name);
        if (_languageCombo.Items.Count > 0)
            _languageCombo.SelectedIndex = 0;
    }

    private void LanguageCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_languageCombo.SelectedItem is not LanguageInfo lang)
            return;

        _currentLanguageCode = lang.Code;

        if (!_bindingsByLanguage.TryGetValue(lang.Code, out var binding))
        {
            var entries = _glossaryService.GetEntries(lang.Code).Select(Clone).ToList();
            binding = new SortableBindingList<GlossaryEntry>(entries);
            binding.ListChanged += (_, _) => UpdateCountLabel();
            _bindingsByLanguage[lang.Code] = binding;
        }

        _grid.DataSource = binding;
        UpdateCountLabel();
    }

    private void AddEntry()
    {
        if (_grid.DataSource is not SortableBindingList<GlossaryEntry> binding)
            return;
        binding.Add(new GlossaryEntry());
        MarkDirty();
    }

    private void RemoveSelectedEntries()
    {
        if (_grid.DataSource is not SortableBindingList<GlossaryEntry> binding)
            return;

        var toRemove = _grid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => r.DataBoundItem as GlossaryEntry)
            .Where(e => e is not null)
            .Cast<GlossaryEntry>()
            .ToList();

        foreach (var entry in toRemove)
            binding.Remove(entry);

        if (toRemove.Count > 0)
            MarkDirty();
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        try
        {
            CommitCurrentEdit();
            foreach (var (langCode, binding) in _bindingsByLanguage)
            {
                var cleaned = binding
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Source) && !string.IsNullOrWhiteSpace(entry.Destination))
                    .ToList();
                _glossaryService.ReplaceEntries(langCode, cleaned);
            }
            _glossaryService.Save();
            _dirty = false;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Impossible d'enregistrer le glossaire :\n\n{ex.Message}",
                "Glossaire",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void CommitCurrentEdit()
    {
        _grid.EndEdit();
        if (_grid.DataSource is SortableBindingList<GlossaryEntry> binding)
        {
            if (_grid.BindingContext?[binding] is CurrencyManager cm)
                cm.EndCurrentEdit();
        }
    }

    private void GlossaryForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK || !_dirty)
            return;

        var result = MessageBox.Show(this,
            "Des modifications du glossaire n'ont pas été enregistrées. Les perdre ?",
            "Glossaire",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            e.Cancel = true;
    }

    private void MarkDirty() => _dirty = true;

    private void UpdateCountLabel()
    {
        if (_grid.DataSource is SortableBindingList<GlossaryEntry> binding)
            _lblCount.Text = $"{binding.Count} entrée(s)";
        else
            _lblCount.Text = string.Empty;
    }

    private static GlossaryEntry Clone(GlossaryEntry source) => new()
    {
        Source = source.Source,
        Destination = source.Destination,
        Context = source.Context,
    };
}
