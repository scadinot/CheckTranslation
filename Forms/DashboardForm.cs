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

    /// <summary>Les moyennes sont calculées au dixième : elles doivent s'afficher au dixième.</summary>
    private const string ScoreFormat = "N1";

    /// <summary>Place réservée au pourcentage à droite de la barre, et marge de part et d'autre.</summary>
    private const int TrackTextWidth = 58;
    private const int TrackPadding = 6;

    /// <summary>
    /// Police soulignée des cellules cliquables, créée une seule fois. Une <see cref="Font"/>
    /// détient un handle GDI : en instancier une par cellule — onze colonnes cliquables par langue —
    /// en produisait des dizaines à chaque ouverture, jamais libérées explicitement. Même règle que
    /// pour <see cref="GdiTextWidthMeasurer"/>, où le cache de polices existe pour cette raison.
    /// </summary>
    private Font? _clickableFont;

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
    private readonly Dictionary<int, Dictionary<string, string>> _languageGridActions = new();
    private readonly Dictionary<int, Dictionary<string, string>> _layoutGridActions = new();

    public DashboardForm(
        IReadOnlyList<TranslationRow> rows,
        IReadOnlyList<LanguageInfo> languages,
        string activeLanguageCode)
    {
        _rows = rows;
        _languages = languages;
        _activeLanguageCode = activeLanguageCode;
        _overview = TranslationStatistics.Compute(rows, languages);

        InitializeComponent();

        _clickableFont = new Font(Font, FontStyle.Underline);

        BuildSummary();
        BuildLanguageGrid();
        BuildLayoutGrid();
        InitGroupLanguageSelector();

        gridLanguages.CellContentClick += GridLanguages_CellContentClick;
        gridLanguages.CellPainting += Grid_CellPainting;
        gridProjects.CellPainting += Grid_CellPainting;
        gridFiles.CellPainting += Grid_CellPainting;
        gridLayout.CellContentClick += GridLayout_CellContentClick;
        gridProjects.CellDoubleClick += (_, e) => DrillIntoGroup(gridProjects, e.RowIndex, GroupBy.Project);
        gridFiles.CellDoubleClick += (_, e) => DrillIntoGroup(gridFiles, e.RowIndex, GroupBy.File);
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

        // L'analyse couvre les sept langues en une passe : la carte totalise, et nomme celle qui
        // déborde le plus — c'est elle qui décide où regarder d'abord.
        if (_overview.TotalLayoutIssues is { } totalIssues)
        {
            var detail = _overview.WorstLayout is { Issues: > 0 } worst
                ? $"sur {_overview.TotalLayoutChecked:N0} vérifications — surtout {worst.LanguageName}"
                : $"sur {_overview.TotalLayoutChecked:N0} vérifications";

            AddCard("Défauts de mise en page", totalIssues.ToString("N0"), detail);
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
        _languageGridActions[AddNumberColumn(gridLanguages, "Traduites")] = Translated;
        AddRatioColumn(gridLanguages, "% traduit");
        _languageGridActions[AddNumberColumn(gridLanguages, "Non traduites")] = new() { ["Translation"] = "translation:none" };
        _languageGridActions[AddNumberColumn(gridLanguages, "Identiques FR")] = new() { ["Translation"] = "translation:same" };
        AddNumberColumn(gridLanguages, "Vérifiées");
        AddRatioColumn(gridLanguages, "% vérifié");

        // « Non vérifiées » et les tranches de score se comptent parmi les lignes traduites : le
        // filtre doit poser la même restriction, sinon il ramènerait aussi les lignes non traduites
        // sans score — bien plus nombreuses — et le chiffre affiché ne vaudrait plus rien.
        _languageGridActions[AddNumberColumn(gridLanguages, "Non vérifiées")] =
            new(Translated) { ["Comment"] = "Non vérifiés" };

        AddNumberColumn(gridLanguages, "Score moyen", ScoreFormat);

        // Le libellé sert à la fois d'en-tête de colonne et d'entrée à sélectionner dans le
        // filtre de la grille : un seul vocabulaire, donc aucun risque de divergence.
        foreach (var (label, _) in TranslationStatistics.ScoreBuckets())
            _languageGridActions[AddNumberColumn(gridLanguages, label)] = new(Translated) { ["Comment"] = label };

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

    /// <summary>Restriction « la ligne est traduite », commune à tous les compteurs qui s'y limitent.</summary>
    private static Dictionary<string, string> Translated
        => new(StringComparer.Ordinal) { ["Translation"] = "translation:done" };

    // --- Tableau de mise en page ---

    private void BuildLayoutGrid()
    {
        AddTextColumn(gridLayout, "Langue", 140);
        _layoutGridActions[AddNumberColumn(gridLayout, "Troncatures")] = LayoutFilter("Troncatures");
        _layoutGridActions[AddNumberColumn(gridLayout, "Collisions")] = LayoutFilter("Collisions");
        _layoutGridActions[AddNumberColumn(gridLayout, "Non vérifiable")] = LayoutFilter("Non vérifiable");
        _layoutGridActions[AddNumberColumn(gridLayout, "Conformes")] = LayoutFilter("Conformes");
        AddNumberColumn(gridLayout, "Analysées");

        // Aucune analyse : le dire, plutôt qu'aligner des zéros qui se liraient « rien à signaler ».
        // Le message reste volontairement ouvert : les causes sont multiples — source Excel, aucun
        // formulaire localisable, aucune traduction encore posée sur un libellé de contrôle — et en
        // nommer deux ferait passer les autres pour impossibles.
        if (_overview.Layouts.Count == 0)
        {
            gridLayout.Rows.Add("Aucune ligne n'a pu être analysée (source Excel, formulaires non localisables, ou aucun libellé de contrôle traduit).");
            return;
        }

        // Du plus défectueux au moins : ce qu'on cherche ici, c'est la langue à reprendre.
        foreach (var layout in _overview.Layouts.OrderByDescending(layout => layout.Issues))
        {
            int index = gridLayout.Rows.Add(
                layout.LanguageName,
                layout.Truncated,
                layout.Collision,
                layout.Unverifiable,
                layout.Ok,
                layout.Analyzed);

            gridLayout.Rows[index].Tag = layout.LanguageCode;
            StyleClickableCells(gridLayout.Rows[index], _layoutGridActions.Keys);
        }
    }

    /// <summary>
    /// Filtre de la colonne « Mise en page » de la grille principale. Le libellé est celui de la
    /// liste déroulante de cette colonne : un seul vocabulaire, donc aucun risque de divergence
    /// entre le chiffre affiché ici et les lignes ramenées là-bas.
    /// </summary>
    private static Dictionary<string, string> LayoutFilter(string label)
        => new(StringComparer.Ordinal) { ["LayoutIssue"] = label };

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
        AddNumberColumn(grid, "Score moyen", ScoreFormat);
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

    /// <param name="format">
    /// Format d'affichage. « N0 » pour un compteur, « N1 » pour une moyenne : afficher au dixième
    /// une valeur calculée au dixième. Avec « N0 », une moyenne de 72,5 s'affichait « 73 » alors
    /// que le presse-papiers en copiait 72,5 — l'écran et la copie ne disaient pas la même chose.
    /// </param>
    private static int AddNumberColumn(DataGridView grid, string header, string format = "N0")
        => grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            Width = 96,
            SortMode = DataGridViewColumnSortMode.Automatic,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Format = format,
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

    private void StyleClickableCells(DataGridViewRow row, IEnumerable<int> columns)
    {
        foreach (var column in columns)
        {
            var cell = row.Cells[column];
            cell.Tag = ClickableTag;
            cell.Style.ForeColor = Color.FromArgb(0, 102, 204);
            cell.Style.Font = _clickableFont;
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

        // Les largeurs sont bornées : les colonnes d'un DataGridView sont redimensionnables, et une
        // colonne rétrécie donnait un rectangle de largeur négative — barre incohérente, et zone de
        // texte débordant sur la cellule voisine.
        var bounds = e.CellBounds;
        int trackWidth = Math.Max(0, bounds.Width - TrackTextWidth - TrackPadding * 2);
        var track = new Rectangle(bounds.X + TrackPadding, bounds.Y + (bounds.Height - 14) / 2, trackWidth, 14);

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

        int textLeft = Math.Clamp(track.Right + 4, bounds.X, bounds.Right);
        var textArea = Rectangle.FromLTRB(textLeft, bounds.Y, Math.Max(textLeft, bounds.Right - 4), bounds.Bottom);
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
        if (e.RowIndex < 0 || !_languageGridActions.TryGetValue(e.ColumnIndex, out var filters))
            return;

        // Un zéro ne mène nulle part : filtrer sur une grille vide n'apprend rien et fait perdre
        // le contexte de lecture.
        if (gridLanguages.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is int count && count == 0)
            return;

        if (gridLanguages.Rows[e.RowIndex].Tag is not string languageCode)
            return;

        CloseWith(new DashboardDrillDown(languageCode, filters));
    }

    private void GridLayout_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0
            || !_layoutGridActions.TryGetValue(e.ColumnIndex, out var filters)
            || gridLayout.Rows[e.RowIndex].Tag is not string languageCode)
            return;

        // Même règle que pour le tableau des langues : un zéro ne mène nulle part. Sans cette
        // garde, la cellule n'est certes pas soulignée, mais elle reste cliquable — et referme le
        // tableau de bord sur une grille vide.
        if (gridLayout.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is not int count || count == 0)
            return;

        CloseWith(new DashboardDrillDown(languageCode, filters));
    }

    /// <summary>
    /// Double-clic sur une ligne de projet ou de fichier : filtre la grille sur ce groupe, dans la
    /// langue choisie.
    ///
    /// Un fichier est identifié par <b>son projet et son chemin</b>, pas par son chemin seul : le
    /// même <c>Properties\Msg</c> existe dans plusieurs projets, et le compteur du tableau n'en
    /// compte qu'un. Le filtre porte donc sur les deux colonnes, en égalité exacte.
    /// </summary>
    private void DrillIntoGroup(DataGridView grid, int rowIndex, GroupBy groupBy)
    {
        if (rowIndex < 0 || grid.Rows[rowIndex].Cells[0].Value is not string name)
            return;

        var filters = new Dictionary<string, string>(StringComparer.Ordinal);

        if (groupBy == GroupBy.Project)
        {
            filters["Project"] = "=" + name;
        }
        else
        {
            int separator = name.IndexOf('›');
            if (separator < 0)
                return;

            filters["Project"] = "=" + name[..separator].Trim();
            filters["File"] = "=" + name[(separator + 1)..].Trim();
        }

        CloseWith(new DashboardDrillDown(SelectedGroupLanguageCode(), filters));
    }

    /// <summary>
    /// Nommée <c>CloseWith</c> et non <c>Close</c> : une surcharge de <see cref="Form.Close"/> se
    /// lit mal et invite à la confusion avec l'appel hérité qu'elle contient.
    /// </summary>
    private void CloseWith(DashboardDrillDown drillDown)
    {
        DrillDown = drillDown;
        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>
    /// La police soulignée est libérée à la fermeture plutôt que dans <c>Dispose(bool)</c> : ce
    /// dernier appartient au fichier du concepteur, qu'on ne modifie pas à la main.
    /// </summary>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);

        _clickableFont?.Dispose();
        _clickableFont = null;
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
/// Filtre demandé depuis le tableau de bord. Chaque valeur est ce qu'attend le contrôle de filtre
/// de sa colonne : le texte d'une zone de saisie, ou l'entrée d'une liste déroulante.
///
/// Plusieurs colonnes peuvent être nécessaires — un fichier n'est identifié que par son projet
/// <i>et</i> son chemin — d'où un dictionnaire plutôt qu'un couple unique.
/// </summary>
internal sealed record DashboardDrillDown(string LanguageCode, IReadOnlyDictionary<string, string> Filters);
