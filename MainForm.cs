using System.ComponentModel;

namespace CheckTranslation;

public partial class MainForm : Form
{
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
    private bool _filterIconClicked;
    private int _sortColumnIndex = -1;
    private ListSortDirection _sortDirection;
    private int _contextMenuRowIndex = -1;

    public MainForm()
    {
        InitializeComponent();
        btnOpen.Image = LoadIcon("open.png", 24);
        btnSave.Image = LoadIcon("save.png", 24);
        btnConfig.Image = LoadIcon("config.png", 24);
        btnOpen.Click += BtnOpen_Click;
        btnSave.Click += BtnSave_Click;
        btnConfig.Click += BtnConfig_Click;
        InitLanguageButtons();
        colFrench.SortMode = DataGridViewColumnSortMode.Programmatic;
        colTranslation.SortMode = DataGridViewColumnSortMode.Programmatic;
        dataGridView.CellPainting += DataGridView_CellPainting;
        dataGridView.CellMouseDown += DataGridView_CellMouseDown;
        dataGridView.ColumnHeaderMouseClick += DataGridView_ColumnHeaderMouseClick;
        InitContextMenu();
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
        SelectLanguage(Languages[0]);
    }

    private void SelectLanguage(LanguageInfo lang)
    {
        _currentLanguage = lang;
        colTranslation.HeaderText = lang.Name;
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

            _filters.Remove("Translation");
            ApplyFilters();
        }

        SelectLanguage(lang);
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
        statusProgressBar.Value = 0;
        statusRowCount.Text = "Chargement...";
        dataGridView.AutoGenerateColumns = false;
        btnOpen.Enabled = false;

        btnSave.Enabled = false;

        try
        {
            var allColumns = Languages.Select(l => l.Column).ToArray();
            var activeColumn = _currentLanguage.Column;
            var progress = new Progress<int>(percent => statusProgressBar.Value = percent);
            var rows = await Task.Run(() => ExcelReader.Load(filePath, allColumns, activeColumn, progress));

            _allRows = rows;
            _filters.Clear();
            dataGridView.DataSource = new SortableBindingList<TranslationRow>(rows);
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

        btnSave.Enabled = false;
        btnOpen.Enabled = false;

        statusProgressBar.Visible = true;
        statusProgressBar.Style = ProgressBarStyle.Marquee;

        try
        {
            var filePath = _currentFilePath;
            var column = _currentLanguage.Column;
            await Task.Run(() => ExcelReader.Save(filePath, column, _allRows));
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

    // --- Filtres ---

    private void DataGridView_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex != -1 || e.ColumnIndex < 0 || e.Graphics is null)
            return;

        e.Paint(e.CellBounds, DataGridViewPaintParts.All);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Icône de filtre (entonnoir) à droite
        var column = dataGridView.Columns[e.ColumnIndex];
        bool hasFilter = _filters.ContainsKey(column.DataPropertyName);

        int fSize = 10;
        int fx = e.CellBounds.Right - fSize - 8;
        int fy = e.CellBounds.Top + (e.CellBounds.Height - fSize) / 2;

        using var filterBrush = new SolidBrush(hasFilter ? Color.DodgerBlue : Color.Silver);
        e.Graphics.FillPolygon(filterBrush, new PointF[]
        {
            new(fx, fy),
            new(fx + fSize, fy),
            new(fx + fSize * 0.6f, fy + fSize * 0.5f),
            new(fx + fSize * 0.6f, fy + fSize),
            new(fx + fSize * 0.4f, fy + fSize),
            new(fx + fSize * 0.4f, fy + fSize * 0.5f),
        });

        // Indicateur de tri (triangle) à gauche du filtre
        if (_sortColumnIndex == e.ColumnIndex)
        {
            int sSize = 8;
            int sx = fx - sSize - 4;
            int sy = e.CellBounds.Top + (e.CellBounds.Height - sSize / 2) / 2;

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

        e.Handled = true;
    }

    private void DataGridView_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex != -1 || e.ColumnIndex < 0) return;
        var cellRect = dataGridView.GetCellDisplayRectangle(e.ColumnIndex, -1, true);
        _filterIconClicked = e.X > cellRect.Width - 22;
    }

    private void DataGridView_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (_filterIconClicked)
        {
            _filterIconClicked = false;
            ShowFilterPopup(e.ColumnIndex);
        }
        else
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
    }

    private void ShowFilterPopup(int columnIndex)
    {
        if (_allRows is null) return;

        var column = dataGridView.Columns[columnIndex];
        var cellRect = dataGridView.GetCellDisplayRectangle(columnIndex, -1, true);
        var location = dataGridView.PointToScreen(new Point(cellRect.Left, cellRect.Bottom));

        var popup = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = location,
            Size = new Size(Math.Max(cellRect.Width, 200), 26),
            ShowInTaskbar = false,
        };

        var textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = _filters.GetValueOrDefault(column.DataPropertyName, ""),
            BorderStyle = BorderStyle.FixedSingle,
        };

        textBox.KeyDown += (s, args) =>
        {
            if (args.KeyCode == Keys.Enter)
            {
                SetFilter(column.DataPropertyName, textBox.Text);
                popup.Close();
                args.SuppressKeyPress = true;
            }
            else if (args.KeyCode == Keys.Escape)
            {
                popup.Close();
                args.SuppressKeyPress = true;
            }
        };

        popup.Controls.Add(textBox);
        popup.Deactivate += (s, args) =>
        {
            SetFilter(column.DataPropertyName, textBox.Text);
            popup.Close();
        };
        popup.Show(this);
        textBox.Focus();
        textBox.SelectAll();
    }

    private void SetFilter(string propertyName, string filterText)
    {
        filterText = filterText.Trim();
        if (string.IsNullOrEmpty(filterText))
            _filters.Remove(propertyName);
        else
            _filters[propertyName] = filterText;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        if (_allRows is null) return;

        IEnumerable<TranslationRow> filtered = _allRows;
        foreach (var (prop, filter) in _filters)
        {
            filtered = prop switch
            {
                "French" => filtered.Where(r => r.French.Contains(filter, StringComparison.OrdinalIgnoreCase)),
                "Translation" => filtered.Where(r => r.Translation.Contains(filter, StringComparison.OrdinalIgnoreCase)),
                _ => filtered,
            };
        }

        var list = filtered.ToList();
        dataGridView.DataSource = new SortableBindingList<TranslationRow>(list);
        ClearSortGlyphs();
        statusRowCount.Text = _filters.Count > 0
            ? $"Lignes : {list.Count} / {_allRows.Count}"
            : $"Lignes : {list.Count}";
    }

    private void ClearSortGlyphs()
    {
        foreach (DataGridViewColumn col in dataGridView.Columns)
            col.HeaderCell.SortGlyphDirection = SortOrder.None;
    }

    private void BtnConfig_Click(object? sender, EventArgs e)
    {
        using var form = new ConfigForm();
        form.ShowDialog(this);
    }

    // --- Menu contextuel ---

    private void InitContextMenu()
    {
        var menuTranslate = new ToolStripMenuItem("Traduire");
        menuTranslate.Click += MenuTranslate_Click;

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(menuTranslate);

        dataGridView.CellMouseClick += (_, e) =>
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0)
                return;

            if (dataGridView.Columns[e.ColumnIndex].Name != "colTranslation")
                return;

            _contextMenuRowIndex = e.RowIndex;
            var cellRect = dataGridView.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
            contextMenu.Show(dataGridView, new Point(cellRect.Left + e.X, cellRect.Top + e.Y));
        };
    }

    private async void MenuTranslate_Click(object? sender, EventArgs e)
    {
        if (_contextMenuRowIndex < 0) return;

        var row = dataGridView.Rows[_contextMenuRowIndex].DataBoundItem as TranslationRow;
        if (row is null || string.IsNullOrWhiteSpace(row.French)) return;

        var config = AppConfig.Current;
        if (string.IsNullOrWhiteSpace(config.Key) || string.IsNullOrWhiteSpace(config.Url))
        {
            MessageBox.Show("Veuillez configurer l'URL et la clé API dans la configuration.",
                "Configuration manquante", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var previousValue = row.Translation;
        row.Translation = "Traduction en cours...";
        dataGridView.Refresh();

        try
        {
            var translation = await Translator.TranslateAsync(row.French, config, _currentLanguage.Name);
            row.Translation = translation;
        }
        catch (Exception ex)
        {
            row.Translation = previousValue;
            MessageBox.Show($"Erreur lors de la traduction :\n\n{ex.Message}",
                "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            dataGridView.Refresh();
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
