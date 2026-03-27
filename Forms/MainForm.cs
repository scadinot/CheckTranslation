using System.ComponentModel;

namespace CheckTranslation;

public partial class MainForm : Form
{
    private readonly IExcelService _excelService;
    private readonly ITranslationService _translationService;
    private readonly Func<ConfigForm> _configFormFactory;

    private static readonly LanguageInfo[] Languages =
    [
        new("en-US", "Anglais",     9),
        new("de-DE", "Allemand",    7),
        new("es-ES", "Espagnol",   11),
        new("it-IT", "Italien",    13),
        new("nl-NL", "Néerlandais",15),
        new("pl-PL", "Polonais",   17),
        new("zh-CN", "Chinois",    19),
    ];

    private string? _currentFilePath;
    private LanguageInfo _currentLanguage = Languages[0];
    private List<TranslationRow>? _allRows;
    private readonly Dictionary<string, string> _filters = new();
    private readonly Dictionary<string, TextBox> _filterTextBoxes = new();
    private System.Windows.Forms.Timer? _filterDebounceTimer;
    private int _sortColumnIndex = -1;
    private ListSortDirection _sortDirection;
    private int _contextMenuRowIndex = -1;
    private ToolStripButton? btnDetails;
    private DataGridViewTextBoxColumn? colProject;
    private DataGridViewTextBoxColumn? colFile;
    private DataGridViewTextBoxColumn? colKey;
    private DataGridViewTextBoxColumn colComment = null!;

    public MainForm() : this(new ExcelService(), new TranslationService(), () => new ConfigForm())
    {
    }

    internal MainForm(IExcelService excelService, ITranslationService translationService, Func<ConfigForm> configFormFactory)
    {
        _excelService = excelService;
        _translationService = translationService;
        _configFormFactory = configFormFactory;

        InitializeComponent();
        var icoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "CheckTranslation.ico");
        if (File.Exists(icoPath))
            Icon = new Icon(icoPath);
        btnOpen.Image = LoadIcon("open.png", 24);
        btnSave.Image = LoadIcon("save.png", 24);
        btnConfig.Image = LoadIcon("config.png", 24);
        btnOpen.Click += BtnOpen_Click;
        btnSave.Click += BtnSave_Click;
        btnConfig.Click += BtnConfig_Click;
        InitDetailsColumns();
        InitCommentColumn();
        InitDetailsButton();
        InitRefreshButton();
        InitLanguageButtons();
        colFrench.SortMode = DataGridViewColumnSortMode.Programmatic;
        colTranslation.SortMode = DataGridViewColumnSortMode.Programmatic;
        dataGridView.CellPainting += DataGridView_CellPainting;
        dataGridView.CellFormatting += DataGridView_CellFormatting;
        dataGridView.CellEndEdit += DataGridView_CellEndEdit;
        dataGridView.ColumnHeaderMouseClick += DataGridView_ColumnHeaderMouseClick;
        dataGridView.ColumnWidthChanged += (_, _) => UpdateFilterPanelLayout();
        dataGridView.Scroll += (_, _) => UpdateFilterPanelLayout();
        dataGridView.ColumnDisplayIndexChanged += (_, _) => UpdateFilterPanelLayout();
        dataGridView.SelectionChanged += (_, _) => UpdateSelectionStatus();
        InitFilterPanel();
        InitContextMenu();
        ApplyShowDetails(AppConfig.Current.ShowDetails);
        UpdateSelectionStatus();
        UpdateTranslationCacheCountStatus();
        UpdateVerificationCacheCountStatus();
        UpdateProviderStatus();
    }

    // --- Indicateur de qualité (couleur) ---
    // La vérification IA écrit dans `Comment` un score au format "XXX - ...".
    // On s'appuie sur ce score pour colorer les cellules, afin de visualiser rapidement
    // la qualité de la traduction (dégradé rouge -> vert).

    private void DataGridView_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var colName = dataGridView.Columns[e.ColumnIndex].Name;
        if (colName is not "colTranslation" and not "colComment")
            return;

        if (dataGridView.Rows[e.RowIndex].DataBoundItem is not TranslationRow row)
            return;

        if (!QualityScore.TryParse(row.Comment, out var score))
            return;

        var backColor = QualityScore.GetBackColor(score);
        e.CellStyle.BackColor = backColor;
        e.CellStyle.SelectionBackColor = ControlPaint.Dark(backColor);
        e.CellStyle.ForeColor = SystemColors.ControlText;
        e.CellStyle.SelectionForeColor = SystemColors.ControlText;
    }

    private void InitDetailsColumns()
    {
        colProject = new DataGridViewTextBoxColumn
        {
            Name = "colProject",
            DataPropertyName = "Project",
            HeaderText = "Projet",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 8,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Programmatic,
        };
        colFile = new DataGridViewTextBoxColumn
        {
            Name = "colFile",
            DataPropertyName = "File",
            HeaderText = "Fichier",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 15,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Programmatic,
        };
        colKey = new DataGridViewTextBoxColumn
        {
            Name = "colKey",
            DataPropertyName = "Key",
            HeaderText = "Clé",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 15,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Programmatic,
        };
        dataGridView.Columns.Insert(0, colProject);
        dataGridView.Columns.Insert(1, colFile);
        dataGridView.Columns.Insert(2, colKey);
    }

    private void InitCommentColumn()
    {
        colComment = new DataGridViewTextBoxColumn
        {
            Name = "colComment",
            DataPropertyName = "Comment",
            HeaderText = "Commentaire",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 25,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Programmatic,
        };
        dataGridView.Columns.Add(colComment);
    }

    private void InitDetailsButton()
    {
        btnDetails = new ToolStripButton
        {
            Image = LoadIcon("columns.png", 24),
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            CheckOnClick = true,
            ToolTipText = "Afficher/Masquer Projet, Fichier, Clé",
        };
        btnDetails.Click += BtnDetails_Click;
        toolStrip.Items.Insert(2, btnDetails);
    }

    private void BtnDetails_Click(object? sender, EventArgs e)
    {
        bool show = btnDetails!.Checked;
        ApplyShowDetails(show);
        var config = AppConfig.Current;
        config.ShowDetails = show;
        config.Save();
    }

    private void ApplyShowDetails(bool show)
    {
        if (colProject is null) return;
        colProject.Visible = show;
        colFile!.Visible = show;
        colKey!.Visible = show;
        if (btnDetails is not null)
            btnDetails.Checked = show;
        UpdateFilterPanelLayout();
    }

    private void InitLanguageButtons()
    {
        int insertIndex = toolStrip.Items.IndexOf(btnConfig);

        foreach (var lang in Languages)
        {
            var btn = new ToolStripButton
            {
                Image = LoadIcon($"{lang.Code}.png", 24),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Tag = lang,
                ToolTipText = lang.Name,
            };
            btn.Click += LanguageButton_Click;
            _languageButtons.Add(btn);
            toolStrip.Items.Insert(insertIndex++, btn);
        }

        toolStrip.Items.Insert(insertIndex, new ToolStripSeparator());

        var selectedLanguage = Languages.FirstOrDefault(l => string.Equals(l.Code, AppConfig.Current.SelectedLanguageCode, StringComparison.OrdinalIgnoreCase))
            ?? Languages[0];
        SelectLanguage(selectedLanguage);
    }

    private void SelectLanguage(LanguageInfo lang)
    {
        _currentLanguage = lang;
        colTranslation.HeaderText = lang.Name;
        colComment.HeaderText = $"Commentaire {lang.Name}";
        statusLanguage.Image = LoadIcon($"{lang.Code}.png");
        statusLanguage.Text = $"Langue : {lang.Name}";

        foreach (var btn in _languageButtons)
            btn.Checked = btn.Tag is LanguageInfo l && l == lang;
    }

    private void LanguageButton_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripButton btn || btn.Tag is not LanguageInfo lang)
            return;

        if (lang == _currentLanguage)
            return;

        if (_allRows is not null)
        {
            int oldCol = _currentLanguage.Column;
            int newCol = lang.Column;
            foreach (var row in _allRows)
                row.SwitchLanguage(oldCol, newCol);

            // Effacer le filtre Translation (les données ont changé)
            if (_filterTextBoxes.TryGetValue("Translation", out var tb))
                tb.Text = string.Empty;
            ResetSpecialFilters();
            ApplyFilters();
        }

        SelectLanguage(lang);
        AppConfig.Current.SelectedLanguageCode = lang.Code;
        AppConfig.Current.Save();
        UpdateTranslationCacheCountStatus();
        UpdateVerificationCacheCountStatus();
        UpdateFilterPanelLayout(); // Mettre à jour les placeholders
    }

    private async void BtnOpen_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Sélectionner un fichier Excel de traductions",
            Filter = "Fichiers Excel (*.xlsx)|*.xlsx",
            RestoreDirectory = true,
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        _currentFilePath = dialog.FileName;
        statusFileName.Text = $"Fichier : {Path.GetFileName(_currentFilePath)}";
        await LoadFileAsync(_currentFilePath);
    }

    private async Task LoadFileAsync(string filePath)
    {
        statusProgressBar.Visible = true;
        statusProgressBar.Style = ProgressBarStyle.Blocks;
        statusProgressBar.Maximum = 1;
        statusProgressBar.Value = 0;
        statusRowCount.Text = "Chargement...";
        dataGridView.AutoGenerateColumns = false;
        btnOpen.Enabled = false;

        btnSave.Enabled = false;

        try
        {
            var allColumns = Languages.Select(l => l.Column).ToArray();
            var activeColumn = _currentLanguage.Column;

            var progress = new Progress<ExcelLoadProgress>(p =>
            {
                if (p.Total > 0)
                    statusProgressBar.Maximum = p.Total;

                statusProgressBar.Value = Math.Clamp(p.Done, 0, statusProgressBar.Maximum);

                statusRowCount.Text = p.Total > 0
                    ? $"Chargement : {p.Done} / {p.Total}"
                    : $"Chargement : {p.Done}";
            });

            var rows = await Task.Run(() => _excelService.LoadWithRowProgress(filePath, allColumns, activeColumn, progress));

            _allRows = rows;
            foreach (var textBox in _filterTextBoxes.Values)
                textBox.Text = string.Empty;
            ResetSpecialFilters();

            _filters.Clear();
            dataGridView.DataSource = new SortableBindingList<TranslationRow>(rows);
            SetViewRefreshPending(false);
            statusRowCount.Text = $"Lignes : {rows.Count}";
            btnSave.Enabled = true;
        }
        catch (Exception ex)
        {
            statusRowCount.Text = "Erreur de chargement";
            MessageBox.Show(
                $"Impossible de charger le fichier Excel :\n\n{ex.Message}",
                "Erreur",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            statusProgressBar.Visible = false;
            btnOpen.Enabled = true;
        }
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (_currentFilePath is null || _allRows is null)
            return;

        dataGridView.EndEdit();

        btnSave.Enabled = false;
        btnOpen.Enabled = false;

        statusProgressBar.Visible = true;
        statusProgressBar.Style = ProgressBarStyle.Marquee;

        try
        {
            var filePath = _currentFilePath;
            var column = _currentLanguage.Column;
            await Task.Run(() => _excelService.Save(filePath, column, _allRows));
            statusRowCount.Text = $"Lignes : {_allRows.Count} (sauvegardé)";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossible de sauvegarder le fichier Excel :\n\n{ex.Message}",
                "Erreur",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            statusProgressBar.Style = ProgressBarStyle.Blocks;
            statusProgressBar.Visible = false;
            btnSave.Enabled = true;
            btnOpen.Enabled = true;
        }
    }

    // --- Filtres (style ResX Resource Manager) ---

    private void InitFilterPanel()
    {
        // Désactiver le style visuel des en-têtes pour contrôler les couleurs
        dataGridView.EnableHeadersVisualStyles = false;
        dataGridView.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
        dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = SystemColors.ControlText;
        dataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = SystemColors.Control;
        dataGridView.ColumnHeadersDefaultCellStyle.SelectionForeColor = SystemColors.ControlText;

        // Augmenter la hauteur des en-têtes pour contenir titre + filtre
        dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dataGridView.ColumnHeadersHeight = 50;

        // Timer pour le debounce du filtrage
        _filterDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
        _filterDebounceTimer.Tick += (_, _) =>
        {
            _filterDebounceTimer.Stop();
            ApplyFilters();
        };

        // Créer les TextBox après que le formulaire soit affiché
        Load += (_, _) => CreateFilterTextBoxes();
    }

    private void CreateFilterTextBoxes()
    {
        _filterTextBoxes.Clear();
        _filterComboBoxes.Clear();

        foreach (DataGridViewColumn col in dataGridView.Columns)
        {
            if (TryCreateSpecialFilterControl(col))
                continue;

            var textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font(dataGridView.Font.FontFamily, 8.5f),
                Tag = col.DataPropertyName,
            };

            textBox.TextChanged += FilterTextBox_TextChanged;
            textBox.TextChanged += (_, _) => dataGridView.InvalidateColumn(col.Index); // Redessiner pour l'icône
            textBox.GotFocus += (s, _) =>
            {
                if (s is TextBox tb)
                    tb.BackColor = Color.FromArgb(255, 255, 230); // Jaune pâle quand actif
            };
            textBox.LostFocus += (s, _) =>
            {
                if (s is TextBox tb)
                    UpdateTextBoxBackColor(tb);
            };
            textBox.KeyDown += (s, args) =>
            {
                if (args.KeyCode == Keys.Escape && s is TextBox tb)
                {
                    tb.Text = string.Empty;
                    dataGridView.Focus();
                    args.SuppressKeyPress = true;
                }
            };

            _filterTextBoxes[col.DataPropertyName] = textBox;
            dataGridView.Controls.Add(textBox);
            UpdateTextBoxBackColor(textBox);
        }

        UpdateFilterPanelLayout();
    }

    private void UpdateTextBoxBackColor(TextBox textBox)
    {
        // Utiliser la couleur de fond réelle de l'en-tête
        // EnableHeadersVisualStyles = true utilise le thème Windows, sinon c'est la couleur définie
        if (dataGridView.EnableHeadersVisualStyles)
        {
            // Couleur typique du thème Windows pour les en-têtes
            textBox.BackColor = SystemColors.Control;
        }
        else
        {
            var backColor = dataGridView.ColumnHeadersDefaultCellStyle.BackColor;
            textBox.BackColor = backColor == Color.Empty ? SystemColors.Control : backColor;
        }
    }

    private void FilterTextBox_TextChanged(object? sender, EventArgs e)
    {
        _filterDebounceTimer?.Stop();
        _filterDebounceTimer?.Start();
    }

    private void UpdateFilterPanelLayout()
    {
        foreach (DataGridViewColumn col in dataGridView.Columns)
        {
            if (TryLayoutSpecialFilterControl(col))
                continue;

            if (!_filterTextBoxes.TryGetValue(col.DataPropertyName, out var textBox))
                continue;

            if (!col.Visible)
            {
                textBox.Visible = false;
                continue;
            }

            var rect = dataGridView.GetColumnDisplayRectangle(col.Index, false);

            // Si la colonne est hors de la vue, la cacher
            if (rect.Width == 0)
            {
                textBox.Visible = false;
                continue;
            }

            // Positionner le TextBox avec un espace à gauche pour l'icône de filtre
            const int filterHeight = 16;
            const int bottomMargin = 5;
            const int iconWidth = 18; // Espace pour l'icône 🔍

            textBox.Visible = true;
            textBox.SetBounds(
                rect.Left + iconWidth,
                dataGridView.ColumnHeadersHeight - filterHeight - bottomMargin,
                rect.Width - iconWidth - 2,
                filterHeight);
        }
    }

    private void DataGridView_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex != -1 || e.ColumnIndex < 0 || e.Graphics is null)
            return;

        // Dessiner le fond de l'en-tête
        e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

        var column = dataGridView.Columns[e.ColumnIndex];

        // Constantes pour le layout (synchronisées avec UpdateFilterPanelLayout)
        const int filterHeight = 16;
        const int bottomMargin = 5;
        const int iconWidth = 18;
        int titleAreaHeight = dataGridView.ColumnHeadersHeight - filterHeight - bottomMargin - 4;

        // Zone pour le titre (partie haute, au-dessus du TextBox)
        var titleRect = new Rectangle(
            e.CellBounds.Left + 4,
            e.CellBounds.Top + 2,
            e.CellBounds.Width - 20, // Espace pour l'icône de tri
            titleAreaHeight);

        // Dessiner le titre de la colonne
        using var titleBrush = new SolidBrush(dataGridView.ColumnHeadersDefaultCellStyle.ForeColor);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
        };
        e.Graphics.DrawString(column.HeaderText, dataGridView.ColumnHeadersDefaultCellStyle.Font ?? dataGridView.Font, titleBrush, titleRect, sf);

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Indicateur de tri (triangle) à droite du titre
        if (_sortColumnIndex == e.ColumnIndex)
        {
            int sSize = 8;
            int sx = e.CellBounds.Right - sSize - 6;
            int sy = e.CellBounds.Top + (titleAreaHeight / 2) - 2;

            using var sortBrush = new SolidBrush(Color.DimGray);
            if (_sortDirection == ListSortDirection.Ascending)
            {
                e.Graphics.FillPolygon(sortBrush, new PointF[]
                {
                    new(sx + sSize / 2f, sy),
                    new(sx + sSize, sy + sSize / 2f),
                    new(sx, sy + sSize / 2f),
                });
            }
            else
            {
                e.Graphics.FillPolygon(sortBrush, new PointF[]
                {
                    new(sx, sy),
                    new(sx + sSize, sy),
                    new(sx + sSize / 2f, sy + sSize / 2f),
                });
            }
        }

        // Dessiner l'icône de filtre (🔍) à gauche de la zone de filtre
        int filterY = dataGridView.ColumnHeadersHeight - filterHeight - bottomMargin;
        var iconRect = new RectangleF(
            e.CellBounds.Left + 2,
            filterY - 1,
            iconWidth - 2,
            filterHeight + 2);

        // Couleur de l'icône : bleue si filtre actif, grise sinon
        bool hasFilter = HasFilter(column.DataPropertyName);
        using var iconBrush = new SolidBrush(hasFilter ? Color.DodgerBlue : Color.Gray);
        using var iconFont = new Font("Segoe UI Emoji", 9f);
        using var iconSf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        e.Graphics.DrawString("🔍", iconFont, iconBrush, iconRect, iconSf);

        e.Handled = true;
    }

    private void DataGridView_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        var column = dataGridView.Columns[e.ColumnIndex];
        var direction = (_sortColumnIndex == e.ColumnIndex && _sortDirection == ListSortDirection.Ascending)
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;
        dataGridView.Sort(column, direction);
        ClearSortGlyphs();
        _sortColumnIndex = e.ColumnIndex;
        _sortDirection = direction;
    }

    private void DataGridView_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        if (dataGridView.Columns[e.ColumnIndex].Name != "colTranslation")
            return;

        if (dataGridView.Rows[e.RowIndex].DataBoundItem is not TranslationRow row)
            return;

        _translationService.UpdateTranslationCache(row.French, row.Translation, AppConfig.Current, _currentLanguage.Name);
        UpdateTranslationCacheCountStatus();
    }

    private void ApplyFilters()
    {
        if (_allRows is null) return;

        _filters.Clear();
        foreach (var (prop, textBox) in _filterTextBoxes)
        {
            var text = textBox.Text.Trim();
            if (!string.IsNullOrEmpty(text))
                _filters[prop] = text;
        }

        CollectSpecialFilters(_filters);

        var list = TranslationRowFiltering.Filter(_allRows, _filters);
        dataGridView.DataSource = new SortableBindingList<TranslationRow>(list);

        if (_sortColumnIndex >= 0 && _sortColumnIndex < dataGridView.Columns.Count)
            dataGridView.Sort(dataGridView.Columns[_sortColumnIndex], _sortDirection);

        SetViewRefreshPending(false);
        ClearSortGlyphs();
        statusRowCount.Text = _filters.Count > 0
            ? $"Lignes : {list.Count} / {_allRows.Count}"
            : $"Lignes : {list.Count}";
        UpdateSelectionStatus();
    }

    private void ApplyFiltersPreservingSelection()
    {
        int firstDisplayedRowIndex = -1;
        int firstDisplayedColumnIndex = -1;
        int horizontalScrollingOffset = 0;

        try
        {
            firstDisplayedRowIndex = dataGridView.FirstDisplayedScrollingRowIndex;
            firstDisplayedColumnIndex = dataGridView.FirstDisplayedScrollingColumnIndex;
            horizontalScrollingOffset = dataGridView.HorizontalScrollingOffset;
        }
        catch
        {
            // best effort
        }

        var selectedItems = dataGridView.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => r.DataBoundItem as TranslationRow)
            .Where(r => r is not null)
            .Cast<TranslationRow>()
            .ToHashSet();

        var currentItem = dataGridView.CurrentRow?.DataBoundItem as TranslationRow;
        var currentColumnIndex = dataGridView.CurrentCell?.ColumnIndex ?? 0;

        ApplyFilters();

        if (selectedItems.Count == 0 && currentItem is null)
            return;

        dataGridView.ClearSelection();

        DataGridViewRow? currentRow = null;
        DataGridViewRow? firstSelectedRow = null;

        foreach (DataGridViewRow row in dataGridView.Rows)
        {
            if (row.DataBoundItem is not TranslationRow item)
                continue;

            if (currentItem is not null && ReferenceEquals(item, currentItem))
                currentRow = row;

            if (selectedItems.Contains(item))
                firstSelectedRow ??= row;
        }

        // Important : définir la cellule courante AVANT de restaurer les sélections.
        // Sur WinForms, assigner `CurrentCell` peut modifier la sélection courante.
        var anchorRow = currentRow ?? firstSelectedRow;
        if (anchorRow is not null)
        {
            var colIndex = Math.Clamp(currentColumnIndex, 0, dataGridView.ColumnCount - 1);
            dataGridView.CurrentCell = anchorRow.Cells[colIndex];
        }

        foreach (DataGridViewRow row in dataGridView.Rows)
        {
            if (row.DataBoundItem is not TranslationRow item)
                continue;

            if (selectedItems.Contains(item) || (currentItem is not null && ReferenceEquals(item, currentItem)))
                row.Selected = true;
        }

        // Restaurer la position de scroll (best effort). Le rebind du DataSource remet la vue en haut.
        try
        {
            if (dataGridView.RowCount > 0 && firstDisplayedRowIndex >= 0)
                dataGridView.FirstDisplayedScrollingRowIndex = Math.Clamp(firstDisplayedRowIndex, 0, dataGridView.RowCount - 1);

            if (dataGridView.ColumnCount > 0 && firstDisplayedColumnIndex >= 0)
                dataGridView.FirstDisplayedScrollingColumnIndex = Math.Clamp(firstDisplayedColumnIndex, 0, dataGridView.ColumnCount - 1);

            if (horizontalScrollingOffset >= 0)
                dataGridView.HorizontalScrollingOffset = horizontalScrollingOffset;
        }
        catch
        {
            // best effort
        }

        UpdateSelectionStatus();
    }

    private void ClearSortGlyphs()
    {
        foreach (DataGridViewColumn col in dataGridView.Columns)
            col.HeaderCell.SortGlyphDirection = SortOrder.None;
    }

    private void BtnConfig_Click(object? sender, EventArgs e)
    {
        using var form = _configFormFactory();
        form.ShowDialog(this);
        UpdateProviderStatus(); // Mettre à jour après modification de la config
        UpdateTranslationCacheCountStatus();
        UpdateVerificationCacheCountStatus();
    }

    private void UpdateSelectionStatus()
    {
        int count = dataGridView.SelectedRows.Count;
        statusSelection.Text = count > 0 ? $"Sélection : {count}" : string.Empty;
    }

    private void UpdateTranslationCacheCountStatus()
    {
        statusTranslationCacheCount.Text = $"Cache trad. : {_translationService.GetTranslationCacheCount(AppConfig.Current, _currentLanguage.Name)}";
    }

    private void UpdateVerificationCacheCountStatus()
    {
        statusVerificationCacheCount.Text = $"Cache Vérif. : {_translationService.GetVerificationCacheCount(AppConfig.Current, _currentLanguage.Name)}";
    }

    private void UpdateProviderStatus()
    {
        var config = AppConfig.Current;
        var providerName = config.Provider switch
        {
            AiProvider.Anthropic => "Anthropic",
            _ => "OpenAI",
        };
        var modelName = config.ModelName;
        statusProvider.Text = string.IsNullOrWhiteSpace(modelName)
            ? $"IA : {providerName}"
            : $"IA : {providerName} ({modelName})";
    }

    private void RestoreStatusBar()
    {
        UpdateRowCountStatus();
        UpdateSelectionStatus();
        UpdateTranslationCacheCountStatus();
        UpdateVerificationCacheCountStatus();
        UpdateProviderStatus();
    }

    // --- Menu contextuel ---

    private void InitContextMenu()
    {
        var menuAutoTranslate = new ToolStripMenuItem("Auto-traduire (copie existante)");
        menuAutoTranslate.Click += MenuAutoTranslate_Click;

        var menuTranslate = new ToolStripMenuItem("Traduire");
        menuTranslate.Click += MenuTranslate_Click;

        var menuVerify = new ToolStripMenuItem("Vérifier la traduction");
        menuVerify.Click += MenuVerify_Click;

        var menuCopyFrench = new ToolStripMenuItem("Copier le français");
        menuCopyFrench.Click += MenuCopyFrench_Click;

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(menuAutoTranslate);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(menuTranslate);
        contextMenu.Items.Add(menuVerify);

        var frenchContextMenu = new ContextMenuStrip();
        frenchContextMenu.Items.Add(menuCopyFrench);

        contextMenu.Opening += (_, _) =>
        {
            int count = dataGridView.SelectedRows.Count;
            if (count > 1)
            {
                menuAutoTranslate.Text = $"Auto-traduire la sélection ({count} lignes)";
                menuTranslate.Text = $"Traduire la sélection ({count} lignes)";
                menuVerify.Text = $"Vérifier la sélection ({count} lignes)";
            }
            else
            {
                menuAutoTranslate.Text = "Auto-traduire (copie existante)";
                menuTranslate.Text = "Traduire";
                menuVerify.Text = "Vérifier la traduction";
            }
        };

        frenchContextMenu.Opening += (_, _) =>
        {
            int count = dataGridView.SelectedRows.Count;
            menuCopyFrench.Text = count > 1
                ? $"Copier les textes français sélectionnés ({count} lignes)"
                : "Copier le texte français";
        };

        dataGridView.CellMouseClick += (_, e) =>
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0)
                return;

            var columnName = dataGridView.Columns[e.ColumnIndex].Name;
            if (columnName is not "colTranslation" and not "colFrench")
                return;

            _contextMenuRowIndex = e.RowIndex;
            var cellRect = dataGridView.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
            var location = new Point(cellRect.Left + e.X, cellRect.Top + e.Y);

            if (columnName == "colFrench")
                frenchContextMenu.Show(dataGridView, location);
            else
                contextMenu.Show(dataGridView, location);
        };
    }

    private void MenuCopyFrench_Click(object? sender, EventArgs e)
    {
        IReadOnlyList<TranslationRow> rows;
        if (dataGridView.SelectedRows.Count > 1)
        {
            rows = dataGridView.SelectedRows
                .Cast<DataGridViewRow>()
                .OrderBy(r => r.Index)
                .Select(r => r.DataBoundItem as TranslationRow)
                .Where(r => r is not null && !string.IsNullOrWhiteSpace(r.French))
                .Cast<TranslationRow>()
                .ToList();
        }
        else
        {
            if (_contextMenuRowIndex < 0)
                return;

            var row = dataGridView.Rows[_contextMenuRowIndex].DataBoundItem as TranslationRow;
            if (row is null || string.IsNullOrWhiteSpace(row.French))
                return;

            rows = [row];
        }

        var text = string.Join(Environment.NewLine, rows.Select(r => r.French));
        if (string.IsNullOrWhiteSpace(text))
            return;

        Clipboard.SetText(text);
        statusRowCount.Text = rows.Count > 1
            ? $"Copie : {rows.Count} textes français"
            : "Copie : texte français";
    }

    private void MenuAutoTranslate_Click(object? sender, EventArgs e)
    {
        if (_allRows is null)
            return;

        IReadOnlyList<TranslationRow> rows;
        if (dataGridView.SelectedRows.Count > 1)
        {
            rows = dataGridView.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as TranslationRow)
                .Where(r => r is not null && !string.IsNullOrWhiteSpace(r.French))
                .Cast<TranslationRow>()
                .ToList();
        }
        else
        {
            if (_contextMenuRowIndex < 0)
                return;

            var row = dataGridView.Rows[_contextMenuRowIndex].DataBoundItem as TranslationRow;
            if (row is null || string.IsNullOrWhiteSpace(row.French))
                return;

            rows = [row];
        }

        int filled = AutoTranslateFromExistingRows(rows);
        dataGridView.Refresh();
        MarkViewRefreshPendingIfNeeded();
        UpdateTranslationCacheCountStatus();

        statusRowCount.Text = filled > 0
            ? $"Auto-traduction : {filled} ligne(s) mise(s) à jour"
            : "Auto-traduction : aucune correspondance";
    }

    private int AutoTranslateFromExistingRows(IReadOnlyList<TranslationRow> targetRows)
    {
        if (_allRows is null || targetRows.Count == 0)
            return 0;

        static string Normalize(string s)
            => s.Trim().Replace("\r\n", "\n", StringComparison.Ordinal);

        var knownTranslations = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var row in _allRows)
        {
            if (string.IsNullOrWhiteSpace(row.French) || string.IsNullOrWhiteSpace(row.Translation))
                continue;

            if (row.Translation is "Traduction en cours..." or "Vérification...")
                continue;

            var key = Normalize(row.French);
            if (!knownTranslations.ContainsKey(key))
                knownTranslations.Add(key, row.Translation.Trim());
        }

        int filled = 0;

        foreach (var row in targetRows)
        {
            if (string.IsNullOrWhiteSpace(row.French))
                continue;

            if (!string.IsNullOrWhiteSpace(row.Translation) && row.Translation is not "Traduction en cours...")
                continue;

            var key = Normalize(row.French);
            if (!knownTranslations.TryGetValue(key, out var translation))
                continue;

            row.Comment = string.Empty;
            row.Translation = translation;
            filled++;
        }

        return filled;
    }

    private async void MenuTranslate_Click(object? sender, EventArgs e)
    {
        IReadOnlyList<TranslationRow> rows;
        if (dataGridView.SelectedRows.Count > 1)
        {
            rows = dataGridView.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as TranslationRow)
                .Where(r => r is not null && !string.IsNullOrWhiteSpace(r.French))
                .Cast<TranslationRow>()
                .ToList();
        }
        else
        {
            if (_contextMenuRowIndex < 0) return;

            var row = dataGridView.Rows[_contextMenuRowIndex].DataBoundItem as TranslationRow;
            if (row is null || string.IsNullOrWhiteSpace(row.French)) return;
            rows = [row];
        }

        await TranslateRowsAsync(rows);
    }

    private async void MenuVerify_Click(object? sender, EventArgs e)
    {
        IReadOnlyList<TranslationRow> rows;
        if (dataGridView.SelectedRows.Count > 1)
        {
            rows = dataGridView.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as TranslationRow)
                .Where(r => r is not null && !string.IsNullOrWhiteSpace(r.French) && !string.IsNullOrWhiteSpace(r.Translation))
                .Cast<TranslationRow>()
                .ToList();
        }
        else
        {
            if (_contextMenuRowIndex < 0) return;

            var row = dataGridView.Rows[_contextMenuRowIndex].DataBoundItem as TranslationRow;
            if (row is null || string.IsNullOrWhiteSpace(row.French)) return;
            rows = [row];
        }

        await VerifyRowsAsync(rows);
    }

    private static bool HasApiConfig(AppConfig config)
        => !string.IsNullOrWhiteSpace(config.Key) && !string.IsNullOrWhiteSpace(config.Url);

    private void UpdateRowCountStatus()
    {
        var total = _allRows?.Count ?? 0;
        var visible = dataGridView.RowCount;
        statusRowCount.Text = _filters.Count > 0
            ? $"Lignes : {visible} / {total}"
            : $"Lignes : {total}";
    }


    private async Task TranslateRowsAsync(IReadOnlyList<TranslationRow> rows)
    {
        if (rows.Count == 0)
            return;

        var config = AppConfig.Current;
        if (!HasApiConfig(config))
        {
            MessageBox.Show("Veuillez configurer l'URL et la clé API dans la configuration.",
                "Configuration manquante", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnOpen.Enabled = false;
        btnSave.Enabled = false;
        statusProgressBar.Visible = true;
        statusProgressBar.Maximum = rows.Count;
        statusProgressBar.Value = 0;

        UseWaitCursor = true;
        Application.UseWaitCursor = true;

        var previousTranslations = rows.Select(r => r.Translation).ToList();
        var previousComments = rows.Select(r => r.Comment).ToList();

        foreach (var row in rows)
        {
            row.Comment = string.Empty;
            row.Translation = "Traduction en cours...";
        }
        dataGridView.Refresh();

        int errors = 0;
        var texts = rows.Select(r => r.French).ToList();
        var progress = new Progress<int>(done =>
        {
            statusProgressBar.Value = done;
            statusRowCount.Text = $"Traduction : {done} / {rows.Count}";
        });

        try
        {
            var batches = await _translationService.TranslateInBatchesAsync(texts, config, _currentLanguage.Name, progress);

            int rowIndex = 0;
            foreach (var batch in batches)
            {
                for (int i = 0; i < batch.Length && rowIndex < rows.Count; i++, rowIndex++)
                {
                    if (!string.IsNullOrEmpty(batch[i]))
                        rows[rowIndex].Translation = batch[i];
                    else
                        errors++;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de la traduction par lot :\n\n{ex.Message}",
                "Erreur de traduction", MessageBoxButtons.OK, MessageBoxIcon.Error);
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Translation == "Traduction en cours...")
                {
                    rows[i].Translation = previousTranslations[i];
                    rows[i].Comment = previousComments[i];
                }
        }
        finally
        {
            dataGridView.Refresh();
            statusProgressBar.Visible = false;
            btnOpen.Enabled = true;
            btnSave.Enabled = _allRows is not null;

            MarkViewRefreshPendingIfNeeded();
            RestoreStatusBar();

            UseWaitCursor = false;
            Application.UseWaitCursor = false;

            if (errors > 0)
                MessageBox.Show($"{errors} traduction(s) n'ont pas pu être extraites de la réponse.\n\nLe format de réponse de l'IA n'a pas été reconnu.",
                    "Erreur de traduction partielle", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task VerifyRowsAsync(IReadOnlyList<TranslationRow> rows)
    {
        if (rows.Count == 0)
            return;

        if (rows.Count == 1 && string.IsNullOrWhiteSpace(rows[0].Translation))
        {
            MessageBox.Show("Aucune traduction à vérifier pour cette ligne.",
                "Vérification", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var config = AppConfig.Current;
        if (!HasApiConfig(config))
        {
            MessageBox.Show("Veuillez configurer l'URL et la clé API dans la configuration.",
                "Configuration manquante", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnOpen.Enabled = false;
        btnSave.Enabled = false;
        statusProgressBar.Visible = true;
        statusProgressBar.Maximum = rows.Count;
        statusProgressBar.Value = 0;

        var previousValues = rows.Select(r => r.Comment).ToList();
        foreach (var row in rows)
            row.Comment = "Vérification...";
        dataGridView.Refresh();

        statusRowCount.Text = "Vérification en cours...";
        UseWaitCursor = true;
        Application.UseWaitCursor = true;

        int errors = 0;
        var pairs = rows.Select(r => (r.French, r.Translation)).ToList();
        var progress = new Progress<int>(done =>
        {
            statusProgressBar.Value = done;
            statusRowCount.Text = $"Vérification : {done} / {rows.Count}";
        });

        try
        {
            var batches = await _translationService.VerifyInBatchesAsync(pairs, config, _currentLanguage.Name, progress);

            int rowIndex = 0;
            foreach (var batch in batches)
            {
                for (int i = 0; i < batch.Length && rowIndex < rows.Count; i++, rowIndex++)
                {
                    if (!string.IsNullOrEmpty(batch[i]))
                    {
                        rows[rowIndex].Comment = batch[i];
                        _translationService.UpdateVerificationCache(rows[rowIndex].French, rows[rowIndex].Translation, batch[i], config, _currentLanguage.Name);
                    }
                    else
                        errors++;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de la vérification par lot :\n\n{ex.Message}",
                "Erreur de vérification par lot", MessageBoxButtons.OK, MessageBoxIcon.Error);
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Comment == "Vérification...")
                    rows[i].Comment = previousValues[i];
        }
        finally
        {
            dataGridView.Refresh();
            statusProgressBar.Visible = false;
            btnOpen.Enabled = true;
            btnSave.Enabled = _allRows is not null;

            MarkViewRefreshPendingIfNeeded();
            RestoreStatusBar();
            UseWaitCursor = false;
            Application.UseWaitCursor = false;

            if (errors > 0)
                MessageBox.Show($"{errors} vérification(s) n'ont pas pu être extraites de la réponse.\n\nLe format de réponse de l'IA n'a pas été reconnu.",
                    "Erreur de vérification partielle", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // --- Icônes ---

private static readonly string ResourceDir = Path.Combine(AppContext.BaseDirectory, "Resources");

    private static Bitmap LoadIcon(string name, int size = 16)
    {
        using var original = new Bitmap(Path.Combine(ResourceDir, name));
        var resized = new Bitmap(size, size);
        using var g = Graphics.FromImage(resized);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(original, 0, 0, size, size);
        return resized;
    }
}

internal sealed record LanguageInfo(string Code, string Name, int Column);
