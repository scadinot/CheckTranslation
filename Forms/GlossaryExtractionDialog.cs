using System.ComponentModel;

namespace CheckTranslation;

internal sealed class GlossaryExtractionDialog : Form
{
    private readonly DataGridView _grid;
    private readonly Button _btnAll;
    private readonly Button _btnNone;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;
    private readonly Label _lblHeader;
    private readonly BindingList<CandidateRow> _candidates = new();

    public IReadOnlyList<GlossaryEntry> AcceptedEntries { get; private set; } = Array.Empty<GlossaryEntry>();

    public GlossaryExtractionDialog()
    {
        Text = "Termes candidats à ajouter au glossaire";
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
            Height = 44,
            Padding = new Padding(10, 10, 10, 4),
        };
        _lblHeader = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Cochez les termes à ajouter au glossaire. Vous pouvez éditer les valeurs avant validation.",
        };
        topPanel.Controls.Add(_lblHeader);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
        };

        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "colSelected",
            HeaderText = "Ajouter",
            DataPropertyName = nameof(CandidateRow.Selected),
            FillWeight = 6,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colFrenchTerm",
            HeaderText = "Terme français",
            DataPropertyName = nameof(CandidateRow.FrenchTerm),
            FillWeight = 14,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colTranslation",
            HeaderText = "Traduction",
            DataPropertyName = nameof(CandidateRow.Translation),
            FillWeight = 14,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colCategory",
            HeaderText = "Catégorie",
            DataPropertyName = nameof(CandidateRow.Category),
            FillWeight = 10,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colComment",
            HeaderText = "Commentaire",
            DataPropertyName = nameof(CandidateRow.Comment),
            FillWeight = 12,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colFrenchExample",
            HeaderText = "Exemple FR",
            DataPropertyName = nameof(CandidateRow.FrenchExample),
            FillWeight = 22,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colTranslationExample",
            HeaderText = "Exemple cible",
            DataPropertyName = nameof(CandidateRow.TranslationExample),
            FillWeight = 22,
        });

        _grid.DataSource = _candidates;
        _grid.CellContentClick += (_, e) =>
        {
            if (e.RowIndex < 0)
                return;
            var columnName = _grid.Columns[e.ColumnIndex].Name;
            if (columnName == "colSelected")
                _grid.EndEdit();
        };

        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(10, 8, 10, 8),
        };

        _btnAll = new Button
        {
            Text = "Tout cocher",
            Location = new Point(10, 10),
            Width = 110,
        };
        _btnAll.Click += (_, _) => SetAll(true);

        _btnNone = new Button
        {
            Text = "Tout décocher",
            Location = new Point(130, 10),
            Width = 110,
        };
        _btnNone.Click += (_, _) => SetAll(false);

        _btnCancel = new Button
        {
            Text = "Annuler",
            DialogResult = DialogResult.Cancel,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(bottomPanel.Width - 270, 10),
            Width = 120,
        };

        _btnOk = new Button
        {
            Text = "Ajouter au glossaire",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(bottomPanel.Width - 140, 10),
            Width = 130,
        };
        _btnOk.Click += BtnOk_Click;

        bottomPanel.Controls.Add(_btnAll);
        bottomPanel.Controls.Add(_btnNone);
        bottomPanel.Controls.Add(_btnCancel);
        bottomPanel.Controls.Add(_btnOk);
        bottomPanel.Resize += (_, _) =>
        {
            _btnCancel.Location = new Point(bottomPanel.Width - 270, 10);
            _btnOk.Location = new Point(bottomPanel.Width - 140, 10);
        };

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        Controls.Add(_grid);
        Controls.Add(topPanel);
        Controls.Add(bottomPanel);
    }

    public void SetCandidates(IReadOnlyList<GlossaryEntry> entries, string languageName)
    {
        _candidates.Clear();
        foreach (var entry in entries)
        {
            _candidates.Add(new CandidateRow
            {
                Selected = true,
                FrenchTerm = entry.FrenchTerm,
                Translation = entry.Translation,
                Category = entry.Category,
                Comment = entry.Comment,
                FrenchExample = entry.FrenchExample,
                TranslationExample = entry.TranslationExample,
            });
        }
        _lblHeader.Text = $"{entries.Count} terme(s) candidat(s) pour {languageName}. Cochez ceux à ajouter (édition possible).";
    }

    private void SetAll(bool value)
    {
        foreach (var row in _candidates)
            row.Selected = value;
        _grid.Refresh();
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        _grid.EndEdit();

        AcceptedEntries = _candidates
            .Where(r => r.Selected
                && !string.IsNullOrWhiteSpace(r.FrenchTerm)
                && !string.IsNullOrWhiteSpace(r.Translation))
            .Select(r => new GlossaryEntry
            {
                FrenchTerm = r.FrenchTerm.Trim(),
                Translation = r.Translation.Trim(),
                Category = r.Category.Trim(),
                Comment = r.Comment.Trim(),
                FrenchExample = r.FrenchExample.Trim(),
                TranslationExample = r.TranslationExample.Trim(),
            })
            .ToList();

        DialogResult = DialogResult.OK;
        Close();
    }

    private sealed class CandidateRow
    {
        public bool Selected { get; set; } = true;
        public string FrenchTerm { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string FrenchExample { get; set; } = string.Empty;
        public string TranslationExample { get; set; } = string.Empty;
    }
}
