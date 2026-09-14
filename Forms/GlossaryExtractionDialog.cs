namespace CheckTranslation;

/// <summary>
/// Validation des candidats d'extraction : une ligne par terme, une colonne par langue extraite
/// (une seule en mode « langue courante », toutes celles avec du contenu en mode multi-langues),
/// créées par code à l'appel de <see cref="SetCandidates"/> — comme dans <c>GlossaryForm</c>.
/// Grille non liée : les dictionnaires de <see cref="GlossaryTerm"/> ne se prêtent pas au
/// binding. L'utilisateur coche, corrige éventuellement, puis valide ; <see cref="AcceptedTerms"/>
/// ne rend que les termes cochés qui ont une source et au moins une cellule non vide.
/// </summary>
internal sealed partial class GlossaryExtractionDialog : Form
{
    private readonly List<DataGridViewTextBoxColumn> _languageColumns = new();

    public IReadOnlyList<GlossaryTerm> AcceptedTerms { get; private set; } = Array.Empty<GlossaryTerm>();

    public GlossaryExtractionDialog()
    {
        InitializeComponent();

        grid.CellContentClick += Grid_CellContentClick;
        btnAll.Click += (_, _) => SetAll(true);
        btnNone.Click += (_, _) => SetAll(false);
        btnOk.Click += BtnOk_Click;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ScreenFit.Apply(this);
    }

    public void SetCandidates(IReadOnlyList<GlossaryTerm> candidates, IReadOnlyList<LanguageInfo> languages)
    {
        // Colonnes de langue insérées avant Contexte, une par langue extraite. Poids réparti : sept
        // langues ne doivent pas écraser Source et Contexte, une seule ne doit pas les noyer.
        foreach (var column in _languageColumns)
            grid.Columns.Remove(column);
        _languageColumns.Clear();

        float weight = Math.Max(10F, 50F / Math.Max(1, languages.Count));
        int insertAt = colContext.Index;
        foreach (var language in languages)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = "colLang_" + language.Code,
                HeaderText = language.Name,
                FillWeight = weight,
                Tag = language.Code,
            };
            grid.Columns.Insert(insertAt++, column);
            _languageColumns.Add(column);
        }

        grid.Rows.Clear();
        foreach (var candidate in candidates)
        {
            int index = grid.Rows.Add();
            var row = grid.Rows[index];
            row.Tag = candidate;
            row.Cells[colSelected.Index].Value = true;
            row.Cells[colSource.Index].Value = candidate.Source;
            row.Cells[colContext.Index].Value = candidate.Context;
            foreach (var column in _languageColumns)
                row.Cells[column.Index].Value = candidate.Translations.GetValueOrDefault((string)column.Tag!, string.Empty);
        }

        lblHeader.Text = languages.Count == 1
            ? $"{candidates.Count} terme(s) candidat(s) pour {languages[0].Name}. Cochez ceux à ajouter (édition possible)."
            : $"{candidates.Count} terme(s) candidat(s) pour {languages.Count} langues ({string.Join(", ", languages.Select(l => l.Code))}). Cochez ceux à ajouter (édition possible) ; une cellule laissée vide reste non tranchée.";
    }

    private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        // Un clic sur la case doit se committer tout de suite : sinon l'état visible et la valeur
        // lue à la validation divergent pour la dernière case cliquée.
        if (e.RowIndex >= 0 && e.ColumnIndex == colSelected.Index)
            grid.EndEdit();
    }

    private void SetAll(bool value)
    {
        foreach (DataGridViewRow row in grid.Rows)
            row.Cells[colSelected.Index].Value = value;
        grid.Refresh();
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        grid.EndEdit();

        var accepted = new List<GlossaryTerm>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.Cells[colSelected.Index].Value is not true)
                continue;

            var source = (row.Cells[colSource.Index].Value as string)?.Trim() ?? string.Empty;
            if (source.Length == 0)
                continue;

            var term = new GlossaryTerm
            {
                Source = source,
                Context = (row.Cells[colContext.Index].Value as string)?.Trim() ?? string.Empty,
                Status = GlossaryTermStatus.Proposed,
            };

            foreach (var column in _languageColumns)
            {
                var value = (row.Cells[column.Index].Value as string)?.Trim();
                if (!string.IsNullOrEmpty(value))
                    term.Translations[(string)column.Tag!] = value;
            }

            // Un terme sans aucune cellule n'apporte rien au glossaire : l'utilisateur a tout effacé.
            if (term.Translations.Count > 0)
                accepted.Add(term);
        }

        AcceptedTerms = accepted;
        DialogResult = DialogResult.OK;
        Close();
    }
}
