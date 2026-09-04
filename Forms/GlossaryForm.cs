namespace CheckTranslation;

/// <summary>
/// Éditeur du glossaire transversal : une ligne par terme, une colonne par langue, plus le statut
/// de gouvernance et le commentaire des réviseurs externes (lecture seule, rempli par l'import —
/// voir GLOSSAIRE.md). La grille est non liée : les colonnes de langue sont dynamiques et les
/// dictionnaires d'un <see cref="GlossaryTerm"/> ne se prêtent pas au binding WinForms.
/// </summary>
internal sealed partial class GlossaryForm : Form
{
    private const string StatusProposed = "Proposé";
    private const string StatusInReview = "En contrôle";
    private const string StatusValidated = "Validé";

    private readonly IGlossaryService _glossaryService;
    private readonly List<DataGridViewTextBoxColumn> _languageColumns = new();
    private DataGridViewComboBoxColumn colStatus = null!;
    private DataGridViewTextBoxColumn colReviewer = null!;
    private bool _dirty;

    public GlossaryForm() : this(new GlossaryService())
    {
    }

    public GlossaryForm(IGlossaryService glossaryService)
    {
        _glossaryService = glossaryService;
        InitializeComponent();
        InitDynamicColumns();

        grid.CellValueChanged += (_, _) => MarkDirty();
        grid.UserAddedRow += (_, _) => MarkDirty();
        grid.UserDeletedRow += (_, _) => { MarkDirty(); UpdateCountLabel(); };
        // Une valeur de ComboBox invalide (statut inconnu) lèverait un DataError modal par
        // défaut : on neutralise, la cellule garde sa valeur et la sauvegarde retombera sur
        // Validé par défaut.
        grid.DataError += (_, e) => e.ThrowException = false;
        btnAdd.Click += (_, _) => AddTermRow();
        btnRemove.Click += (_, _) => RemoveSelectedRows();
        btnOk.Click += BtnOk_Click;
        FormClosing += GlossaryForm_FormClosing;

        LoadTerms();
    }

    /// <summary>
    /// Amène la colonne d'une langue à l'écran (appelé par MainForm avec la langue active de la
    /// grille principale). Le glossaire étant transversal, il n'y a plus rien à « sélectionner » :
    /// c'est un simple confort de positionnement.
    /// </summary>
    public void SelectLanguage(string languageCode)
    {
        var column = _languageColumns.Find(c => string.Equals((string)c.Tag!, languageCode, StringComparison.OrdinalIgnoreCase));
        if (column is null)
            return;

        if (grid.Rows.Count > 0)
            grid.CurrentCell = grid.Rows[0].Cells[column.Index];
    }

    // Colonnes dynamiques : une par langue (l'ordre est celui de la toolbar principale), puis le
    // statut et le commentaire réviseur. Créées par code, comme les colonnes de MainForm : le
    // Designer ne connaît que Source et Contexte.
    private void InitDynamicColumns()
    {
        foreach (var language in MainForm.Languages)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = "colLang_" + language.Code,
                HeaderText = language.Name,
                FillWeight = 9F,
                SortMode = DataGridViewColumnSortMode.Automatic,
                Tag = language.Code,
            };
            _languageColumns.Add(column);
            grid.Columns.Add(column);
        }

        colStatus = new DataGridViewComboBoxColumn
        {
            Name = "colStatus",
            HeaderText = "Statut",
            FillWeight = 8F,
            FlatStyle = FlatStyle.Flat,
            SortMode = DataGridViewColumnSortMode.Automatic,
        };
        colStatus.Items.AddRange(StatusProposed, StatusInReview, StatusValidated);
        grid.Columns.Add(colStatus);

        colReviewer = new DataGridViewTextBoxColumn
        {
            Name = "colReviewer",
            HeaderText = "Commentaire réviseur",
            FillWeight = 12F,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Automatic,
        };
        grid.Columns.Add(colReviewer);
    }

    private void LoadTerms()
    {
        foreach (var term in _glossaryService.GetTerms())
        {
            int index = grid.Rows.Add();
            var row = grid.Rows[index];
            row.Tag = term;
            row.Cells[colSource.Index].Value = term.Source;
            row.Cells[colContext.Index].Value = term.Context;

            foreach (var column in _languageColumns)
                row.Cells[column.Index].Value = term.Translations.GetValueOrDefault((string)column.Tag!, string.Empty);

            row.Cells[colStatus.Index].Value = StatusLabel(term.Status);
            row.Cells[colReviewer.Index].Value = term.ReviewerComment;
        }

        UpdateCountLabel();
    }

    private void AddTermRow()
    {
        // Un terme saisi à la main dans l'éditeur est une décision humaine : Validé par défaut,
        // la colonne Statut reste modifiable pour qui veut le soumettre au contrôle d'abord.
        int index = grid.Rows.Add();
        grid.Rows[index].Cells[colStatus.Index].Value = StatusValidated;
        grid.CurrentCell = grid.Rows[index].Cells[colSource.Index];
        grid.BeginEdit(true);
        MarkDirty();
        UpdateCountLabel();
    }

    private void RemoveSelectedRows()
    {
        var toRemove = grid.SelectedRows
            .Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .ToList();

        foreach (var row in toRemove)
            grid.Rows.Remove(row);

        if (toRemove.Count > 0)
        {
            MarkDirty();
            UpdateCountLabel();
        }
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        grid.EndEdit();

        var terms = new List<GlossaryTerm>();
        var seenSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
                continue;

            var source = (row.Cells[colSource.Index].Value as string)?.Trim() ?? string.Empty;
            if (source.Length == 0)
                continue;

            if (!seenSources.Add(source))
            {
                MessageBox.Show(this,
                    $"Le terme « {source} » apparaît plusieurs fois. Fusionnez les lignes avant d'enregistrer.",
                    "Glossaire",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var term = new GlossaryTerm
            {
                Source = source,
                Context = row.Cells[colContext.Index].Value as string ?? string.Empty,
                Status = ParseStatus(row.Cells[colStatus.Index].Value as string),
                // Le commentaire réviseur appartient au cycle d'import : il se transporte, il ne
                // s'édite pas ici.
                ReviewerComment = (row.Tag as GlossaryTerm)?.ReviewerComment ?? string.Empty,
            };

            foreach (var column in _languageColumns)
            {
                if (row.Cells[column.Index].Value is string destination && !string.IsNullOrWhiteSpace(destination))
                    term.Translations[(string)column.Tag!] = destination;
            }

            terms.Add(term);
        }

        try
        {
            _glossaryService.ReplaceTerms(terms);
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
        int count = grid.Rows.Cast<DataGridViewRow>().Count(row => !row.IsNewRow);
        lblCount.Text = $"{count} terme(s) — seuls les termes Validé sont injectés dans les prompts";
    }

    private static string StatusLabel(GlossaryTermStatus status) => status switch
    {
        GlossaryTermStatus.Proposed => StatusProposed,
        GlossaryTermStatus.InReview => StatusInReview,
        _ => StatusValidated,
    };

    private static GlossaryTermStatus ParseStatus(string? label) => label switch
    {
        StatusProposed => GlossaryTermStatus.Proposed,
        StatusInReview => GlossaryTermStatus.InReview,
        _ => GlossaryTermStatus.Validated,
    };
}
