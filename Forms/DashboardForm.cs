namespace CheckTranslation;

/// <summary>
/// Tableau de bord de l'état des traductions.
///
/// Il ne se contente pas d'afficher des chiffres : chaque nombre souligné est un point d'entrée
/// dans la grille. Cliquer « 342 non traduites en polonais » bascule sur le polonais et filtre la
/// grille sur ces lignes. Sans ce lien, un tableau de bord n'est qu'une image dont on ne peut rien
/// faire — c'est ce qui a décidé de la forme retenue.
///
/// Le calcul vit dans <see cref="TranslationStatistics"/> : cette classe ne fait que mettre en
/// forme et rendre cliquable.
/// </summary>
internal sealed partial class DashboardForm : Form
{
    private const string ClickableTag = "clickable";

    private readonly IReadOnlyList<TranslationRow> _rows;
    private readonly IReadOnlyList<LanguageInfo> _languages;
    private readonly string _activeLanguageCode;
    private readonly TranslationOverview _overview;

    /// <summary>
    /// Filtre demandé par un clic. Nul si l'utilisateur a simplement fermé la fenêtre.
    /// <see cref="MainForm"/> l'applique au retour de <see cref="Form.ShowDialog()"/>.
    /// </summary>
    public DashboardDrillDown? DrillDown { get; private set; }

    // Colonne -> ce qu'un clic sur cette colonne doit filtrer. Le code de langue vient de la ligne
    // cliquée, ce qui permet au même tableau de servir les sept langues.
    private readonly Dictionary<int, (string Column, string Value)> _languageGridActions = new();
    private readonly Dictionary<int, (string Column, string Value)> _layoutGridActions = new();

    public DashboardForm(
        IReadOnlyList<TranslationRow> rows,
        IReadOnlyList<LanguageInfo> languages,
        string activeLanguageCode)
    {
        _rows = rows;
        _languages = languages;
        _activeLanguageCode = activeLanguageCode;
        _overview = TranslationStatistics.Compute(rows, languages, activeLanguageCode);

        InitializeComponent();

        BuildSummary();
        BuildLanguageGrid();
        BuildLayoutGrid();
        InitGroupLanguageSelector();

        gridLanguages.CellContentClick += GridLanguages_CellContentClick;
        gridLanguages.CellPainting += Grid_CellPainting;
        gridProjects.CellPainting += Grid_CellPainting;
        gridFiles.CellPainting += Grid_CellPainting;
        gridLayout.CellContentClick += GridLayout_CellContentClick;
        gridProjects.CellDoubleClick += (_, e) => DrillIntoGroup(gridProjects, e.RowIndex, "Project");
        gridFiles.CellDoubleClick += (_, e) => DrillIntoGroup(gridFiles, e.RowIndex, "File");
        cmbGroupLanguage.SelectedIndexChanged += (_, _) => RefreshGroupGrids();
        btnCopy.Click += BtnCopy_Click;
    }

    // --- Bandeau de synthèse ---

    private void BuildSummary()
    {
        AddCard("Lignes", _overview.Rows.ToString("N0"), $"{_overview.Projects} projet(s), {_overview.Files} fichier(s)");
        AddCard("Traduit", Percent(_overview.OverallTranslatedRatio), $"toutes langues — {_languages.Count} langues");
        AddCard("Vérifié", Percent(_overview.OverallVerifiedRatio), "des lignes traduites");

        if (_overview.LeastAdvanced is { } least)
            AddCard("Langue la moins avancée", least.Name, $"{Percent(least.TranslatedRatio)} traduit");

        if (_overview.Layout is { } layout)
        {
            var name = _languages.FirstOrDefault(language => language.Code == layout.LanguageCode)?.Name ?? layout.LanguageCode;
            AddCard("Défauts de mise en page", layout.Issues.ToString("N0"), $"{name} — sur {layout.Analyzed:N0} analysées");
        }
    }

    private void AddCard(string title, string value, string detail)
    {
        var card = new Panel
        {
            Width = 210,
            Height = 82,
            Margin = new Padding(0, 0, 10, 0),
            BackColor = Color.FromArgb(246, 248, 250),
            BorderStyle = BorderStyle.FixedSingle,
        };

        card.Controls.Add(new Label
        {
            Text = detail,
            Dock = DockStyle.Bottom,
            Height = 20,
            ForeColor = SystemColors.GrayText,
            Font = new Font(Font.FontFamily, 7.5f),
            Padding = new Padding(8, 0, 4, 4),
            AutoEllipsis = true,
        });

        card.Controls.Add(new Label
        {
            Text = value,
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 15f, FontStyle.Bold),
            Padding = new Padding(6, 0, 4, 0),
            AutoEllipsis = true,
        });

        card.Controls.Add(new Label
        {
            Text = title.ToUpperInvariant(),
            Dock = DockStyle.Top,
            Height = 18,
            ForeColor = SystemColors.GrayText,
            Font = new Font(Font.FontFamily, 7.5f, FontStyle.Bold),
            Padding = new Padding(8, 4, 4, 0),
            AutoEllipsis = true,
        });

        summaryPanel.Controls.Add(card);
    }

    // --- Tableau par langue ---

    private void BuildLanguageGrid()
    {
        AddTextColumn(gridLanguages, "Langue", 120);
        _languageGridActions[AddNumberColumn(gridLanguages, "Traduites")] = ("Translation", "translation:done");
        AddRatioColumn(gridLanguages, "% traduit");
        _languageGridActions[AddNumberColumn(gridLanguages, "Non traduites")] = ("Translation", "translation:none");
        _languageGridActions[AddNumberColumn(gridLanguages, "Identiques FR")] = ("Translation", "translation:same");
        AddNumberColumn(gridLanguages, "Vérifiées");
        AddRatioColumn(gridLanguages, "% vérifié");
        _languageGridActions[AddNumberColumn(gridLanguages, "Non vérifiées")] = ("Comment", "Non vérifiés");
        AddNumberColumn(gridLanguages, "Score moyen");

        // Le libellé sert à la fois d'en-tête de colonne et d'entrée à sélectionner dans le
        // filtre de la grille : un seul vocabulaire, donc aucun risque de divergence.
        foreach (var (label, _) in TranslationStatistics.ScoreBuckets())
            _languageGridActions[AddNumberColumn(gridLanguages, label)] = ("Comment", label);

        foreach (var language in _overview.Languages)
        {
            var values = new List<object?>
            {
                language.Name,
                language.Translated,
                language.TranslatedRatio,
                language.Untranslated,
                language.SameAsSource,
                language.Verified,
                language.VerifiedRatio,
                language.Translated - language.Verified,
                language.AverageScore is { } average ? Math.Round(average, 1) : null,
            };

            values.AddRange(language.ScoreBuckets.Select(count => (object?)count));

            int index = gridLanguages.Rows.Add(values.ToArray());
            gridLanguages.Rows[index].Tag = language.Code;
            StyleClickableCells(gridLanguages.Rows[index], _languageGridActions.Keys);
        }
    }

    // --- Tableau de mise en page ---

    private void BuildLayoutGrid()
    {
        AddTextColumn(gridLayout, "Verdict", 260);
        _layoutGridActions[AddNumberColumn(gridLayout, "Lignes")] = ("LayoutIssue", string.Empty);

        if (_overview.Layout is not { } layout)
        {
            gridLayout.Rows.Add("Aucune ligne analysée — source Excel, ou analyse pas encore passée.", null);
            return;
        }

        AddLayoutRow("Troncatures", layout.Truncated, "Troncatures");
        AddLayoutRow("Collisions", layout.Collision, "Collisions");
        AddLayoutRow("Conformes", layout.Ok, "Conformes");
        AddLayoutRow("Non vérifiable", layout.Unverifiable, "Non vérifiable");
    }

    private void AddLayoutRow(string verdict, int count, string filterLabel)
    {
        int index = gridLayout.Rows.Add(verdict, count);
        gridLayout.Rows[index].Tag = filterLabel;

        if (count > 0)
            StyleClickableCells(gridLayout.Rows[index], _layoutGridActions.Keys);
    }

    // --- Tableaux par projet et par fichier ---

    private void InitGroupLanguageSelector()
    {
        foreach (var language in _languages)
            cmbGroupLanguage.Items.Add(language.Name);

        int active = _languages.ToList().FindIndex(language => language.Code == _activeLanguageCode);
        cmbGroupLanguage.SelectedIndex = active >= 0 ? active : 0;

        BuildGroupGrid(gridProjects);
        BuildGroupGrid(gridFiles);
        RefreshGroupGrids();
    }

    private static void BuildGroupGrid(DataGridView grid)
    {
        AddTextColumn(grid, grid.Name == "gridProjects" ? "Projet" : "Fichier", 380);
        AddNumberColumn(grid, "Lignes");
        AddNumberColumn(grid, "Traduites");
        AddRatioColumn(grid, "% traduit");
        AddNumberColumn(grid, "Non traduites");
        AddNumberColumn(grid, "Vérifiées");
        AddRatioColumn(grid, "% vérifié");
        AddNumberColumn(grid, "Score moyen");
    }

    private void RefreshGroupGrids()
    {
        var code = SelectedGroupLanguageCode();
        Fill(gridProjects, TranslationStatistics.ComputeGroups(_rows, code, GroupBy.Project));
        Fill(gridFiles, TranslationStatistics.ComputeGroups(_rows, code, GroupBy.File));

        static void Fill(DataGridView grid, List<GroupStatistics> groups)
        {
            grid.Rows.Clear();

            foreach (var group in groups)
            {
                grid.Rows.Add(
                    group.Name,
                    group.Total,
                    group.Translated,
                    group.TranslatedRatio,
                    group.Untranslated,
                    group.Verified,
                    group.VerifiedRatio,
                    group.AverageScore is { } average ? Math.Round(average, 1) : null);
            }
        }
    }

    private string SelectedGroupLanguageCode()
        => _languages[Math.Clamp(cmbGroupLanguage.SelectedIndex, 0, _languages.Count - 1)].Code;

    // --- Colonnes ---

    private static void AddTextColumn(DataGridView grid, string header, int width)
        => grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            Width = width,
            SortMode = DataGridViewColumnSortMode.Automatic,
        });

    private static int AddNumberColumn(DataGridView grid, string header)
        => grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            Width = 96,
            SortMode = DataGridViewColumnSortMode.Automatic,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Format = "N0",
            },
        });

    /// <summary>
    /// Colonne de taux. La valeur reste un <c>double</c> entre 0 et 1 — c'est elle qui sert au tri
    /// et à la barre ; le rendu est fait par <see cref="Grid_CellPainting"/>.
    /// </summary>
    private static void AddRatioColumn(DataGridView grid, string header)
    {
        int index = grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            Width = 130,
            SortMode = DataGridViewColumnSortMode.Automatic,
        });

        grid.Columns[index].Tag = "ratio";
    }

    private static void StyleClickableCells(DataGridViewRow row, IEnumerable<int> columns)
    {
        foreach (var column in columns)
        {
            var cell = row.Cells[column];
            cell.Tag = ClickableTag;
            cell.Style.ForeColor = Color.FromArgb(0, 102, 204);
            cell.Style.Font = new Font(row.DataGridView!.Font, FontStyle.Underline);
        }
    }

    // --- Rendu des barres ---

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (sender is not DataGridView grid || e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        if (grid.Columns[e.ColumnIndex].Tag as string != "ratio" || e.Value is not double ratio)
            return;

        e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

        var bounds = e.CellBounds;
        var track = new Rectangle(bounds.X + 6, bounds.Y + (bounds.Height - 14) / 2, bounds.Width - 70, 14);

        if (track.Width > 0)
        {
            using var background = new SolidBrush(Color.FromArgb(232, 234, 237));
            e.Graphics!.FillRectangle(background, track);

            int filled = (int)Math.Round(track.Width * Math.Clamp(ratio, 0, 1));
            if (filled > 0)
            {
                using var brush = new SolidBrush(RatioColor(ratio));
                e.Graphics.FillRectangle(brush, track.X, track.Y, filled, track.Height);
            }
        }

        var textArea = new Rectangle(track.Right + 4, bounds.Y, bounds.Right - track.Right - 8, bounds.Height);
        TextRenderer.DrawText(e.Graphics!, Percent(ratio), grid.Font, textArea, grid.ForeColor,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        e.Handled = true;
    }

    /// <summary>
    /// Couleur de la barre. Reprend l'esprit de <see cref="QualityScore.GetBackColor"/> — rouge en
    /// bas, vert en haut — pour qu'un même niveau d'avancement se lise pareil partout dans
    /// l'application, mais en teintes soutenues : ici la barre est le sujet, pas un fond de cellule.
    /// </summary>
    private static Color RatioColor(double ratio) => ratio switch
    {
        >= 0.95 => Color.FromArgb(56, 142, 60),
        >= 0.80 => Color.FromArgb(124, 179, 66),
        >= 0.50 => Color.FromArgb(251, 192, 45),
        >= 0.20 => Color.FromArgb(245, 124, 0),
        _ => Color.FromArgb(211, 47, 47),
    };

    private static string Percent(double ratio) => (ratio * 100).ToString("0.#") + " %";

    // --- Clics ---

    private void GridLanguages_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || !_languageGridActions.TryGetValue(e.ColumnIndex, out var action))
            return;

        // Un zéro ne mène nulle part : filtrer sur une grille vide n'apprend rien et fait perdre
        // le contexte de lecture.
        if (gridLanguages.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is int count && count == 0)
            return;

        if (gridLanguages.Rows[e.RowIndex].Tag is not string languageCode)
            return;

        Close(new DashboardDrillDown(languageCode, action.Column, action.Value));
    }

    private void GridLayout_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0
            || !_layoutGridActions.ContainsKey(e.ColumnIndex)
            || gridLayout.Rows[e.RowIndex].Tag is not string filterLabel
            || _overview.Layout is not { } layout)
            return;

        // Même règle que pour le tableau des langues : un zéro ne mène nulle part. Sans cette
        // garde, la cellule n'est certes pas soulignée, mais elle reste cliquable — et referme le
        // tableau de bord sur une grille vide.
        if (RowCount(gridLayout, e.RowIndex) == 0)
            return;

        Close(new DashboardDrillDown(layout.LanguageCode, "LayoutIssue", filterLabel));
    }

    /// <summary>
    /// Double-clic sur une ligne de projet ou de fichier : filtre la grille sur ce groupe, dans la
    /// langue choisie. Le nom d'un fichier est préfixé de son projet dans l'affichage ; seule la
    /// partie utile part dans le filtre.
    /// </summary>
    private void DrillIntoGroup(DataGridView grid, int rowIndex, string column)
    {
        if (rowIndex < 0 || grid.Rows[rowIndex].Cells[0].Value is not string name)
            return;

        var value = column == "File" && name.Contains('›')
            ? name[(name.IndexOf('›') + 1)..].Trim()
            : name;

        Close(new DashboardDrillDown(SelectedGroupLanguageCode(), column, value));
    }

    /// <summary>Compteur de la colonne « Lignes » d'une ligne de verdict, ou 0 si elle n'en porte pas.</summary>
    private static int RowCount(DataGridView grid, int rowIndex)
        => grid.Rows[rowIndex].Cells[1].Value is int count ? count : 0;

    private void Close(DashboardDrillDown drillDown)
    {
        DrillDown = drillDown;
        DialogResult = DialogResult.OK;
        Close();
    }

    // --- Copie ---

    private void BtnCopy_Click(object? sender, EventArgs e)
    {
        var grid = tabs.SelectedTab?.Controls.OfType<DataGridView>().FirstOrDefault();
        if (grid is null || grid.Rows.Count == 0)
            return;

        var lines = new List<string>
        {
            string.Join('\t', grid.Columns.Cast<DataGridViewColumn>().Select(column => column.HeaderText)),
        };

        foreach (DataGridViewRow row in grid.Rows)
        {
            lines.Add(string.Join('\t', row.Cells.Cast<DataGridViewCell>().Select(cell => cell.Value switch
            {
                // Un taux est stocké en fraction : le copier tel quel donnerait « 0,83 » là où
                // l'écran affiche « 83 % ».
                double ratio => Percent(ratio),
                null => string.Empty,
                var value => value.ToString() ?? string.Empty,
            })));
        }

        Clipboard.SetText(string.Join(Environment.NewLine, lines));
    }
}

/// <summary>
/// Filtre demandé depuis le tableau de bord. <paramref name="Value"/> est ce qu'attend le contrôle
/// de filtre de la colonne : le texte d'une zone de saisie, ou l'entrée d'une liste déroulante.
/// </summary>
internal sealed record DashboardDrillDown(string LanguageCode, string Column, string Value);
