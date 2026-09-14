namespace CheckTranslation;

/// <summary>
/// Validation des candidats d'extraction : une ligne par terme, une colonne par langue extraite
/// (une seule en mode « langue courante », toutes celles avec du contenu en mode multi-langues),
/// créées par code à l'appel de <see cref="SetCandidates"/> — comme dans <c>GlossaryForm</c>.
/// Grille non liée : les dictionnaires de <see cref="GlossaryTerm"/> ne se prêtent pas au
/// binding.
///
/// Chaque cellule est confrontée au glossaire (<see cref="GlossaryCandidates.Classify"/>, la même
/// définition que le versement) et colorée : <b>vert</b> sera écrit, <b>noir</b> est déjà au
/// glossaire (affiché en lecture seule, pour que personne ne ressaisisse une valeur qui serait
/// abandonnée), <b>rouge</b> diffère d'une valeur déjà tranchée et ne sera pas appliqué (la
/// valeur tranchée est en infobulle). Une ligne qui n'ajouterait rien arrive décochée. Les
/// couleurs sont un aperçu ; l'autorité reste le service, qui reclasse à l'écriture.
/// </summary>
internal sealed partial class GlossaryExtractionDialog : Form
{
    private static readonly Color AddedColor = Color.FromArgb(0, 128, 0);
    private static readonly Color ConflictColor = Color.FromArgb(192, 0, 0);

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

    public void SetCandidates(
        IReadOnlyList<GlossaryTerm> candidates,
        IReadOnlyList<LanguageInfo> languages,
        IReadOnlyList<GlossaryTerm> existingTerms)
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
        int newTerms = 0, completedTerms = 0, unchangedTerms = 0;
        foreach (var candidate in candidates)
        {
            var existing = GlossaryCandidates.FindExisting(existingTerms, candidate.Source);
            var diff = GlossaryCandidates.Classify(candidate, existing);

            int index = grid.Rows.Add();
            var row = grid.Rows[index];
            row.Tag = candidate;
            row.Cells[colSelected.Index].Value = diff.AddsAnything;

            var sourceCell = row.Cells[colSource.Index];
            sourceCell.Value = candidate.Source;
            if (diff.IsNewTerm)
                sourceCell.Style.ForeColor = AddedColor;
            else
                sourceCell.ToolTipText = "Terme déjà au glossaire : ses cases vides seront complétées, les autres restent.";

            PaintCell(row.Cells[colContext.Index], diff.Context, candidate.Context, existing?.Context);
            foreach (var column in _languageColumns)
            {
                var code = (string)column.Tag!;
                PaintCell(row.Cells[column.Index],
                    diff.Cells.GetValueOrDefault(code, CandidateCellStatus.Empty),
                    candidate.Translations.GetValueOrDefault(code),
                    existing?.Translations.GetValueOrDefault(code));
            }

            if (diff.IsNewTerm) newTerms++;
            else if (diff.AddsAnything) completedTerms++;
            else unchangedTerms++;
        }

        var scope = languages.Count == 1
            ? $"pour {languages[0].Name}"
            : $"pour {languages.Count} langues ({string.Join(", ", languages.Select(l => l.Code))})";
        lblHeader.Text =
            $"{candidates.Count} terme(s) candidat(s) {scope} : {newTerms} nouveau(x), {completedTerms} existant(s) à compléter, {unchangedTerms} sans rien à ajouter (décoché(s)). Cochez ceux à ajouter (édition possible ; une cellule laissée vide reste non tranchée)."
            + "\nVert : sera ajouté au glossaire · Noir : déjà au glossaire, non modifiable ici · Rouge : diffère d'une valeur déjà tranchée, ne sera pas appliqué (valeur tranchée en infobulle).";
    }

    /// <summary>
    /// Une cellule existante s'affiche telle que le glossaire la porte, en lecture seule : la
    /// modifier ici ne servirait à rien, le versement n'écrase jamais. Une cellule en conflit
    /// montre la proposition — c'est elle que l'utilisateur relit — et la valeur tranchée en
    /// infobulle, pour qu'il sache ce qui restera.
    /// </summary>
    private static void PaintCell(DataGridViewCell cell, CandidateCellStatus status, string? proposed, string? current)
    {
        switch (status)
        {
            case CandidateCellStatus.Added:
                cell.Value = proposed;
                cell.Style.ForeColor = AddedColor;
                break;
            case CandidateCellStatus.Existing:
                cell.Value = current;
                cell.ReadOnly = true;
                cell.ToolTipText = "Déjà au glossaire.";
                break;
            case CandidateCellStatus.Conflict:
                cell.Value = proposed;
                cell.Style.ForeColor = ConflictColor;
                cell.ToolTipText = $"Déjà tranché au glossaire : « {current} ». La proposition ne remplacera pas cette valeur.";
                break;
            default:
                cell.Value = string.Empty;
                break;
        }
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

            // Les cellules existantes (lecture seule) repartent telles quelles : le service les
            // reclassera « déjà là » et ne les écrira pas ; les rouges seront ignorées de même.
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
