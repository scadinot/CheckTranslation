namespace CheckTranslation;

public partial class Form1 : Form
{
    private static readonly LanguageInfo[] Languages =
    [
        new("de-DE", "Allemand",    7),
        new("en-US", "Anglais",     9),
        new("es-ES", "Espagnol",   11),
        new("it-IT", "Italien",    13),
        new("nl-NL", "Néerlandais",15),
        new("pl-PL", "Polonais",   17),
        new("zh-CN", "Chinois",    19),
    ];

    private string? _currentFilePath;
    private LanguageInfo _currentLanguage = Languages[0];

    public Form1()
    {
        InitializeComponent();
        btnOpen.Image = LoadIcon("open.png", 24);
        btnSave.Image = LoadIcon("save.png", 24);
        btnOpen.Click += BtnOpen_Click;
        btnSave.Click += BtnSave_Click;
        InitLanguageMenu();
    }

    private void InitLanguageMenu()
    {
        foreach (var lang in Languages)
        {
            var item = new ToolStripMenuItem(lang.Name, LoadIcon($"{lang.Code}.png", 24));
            item.Tag = lang;
            item.Click += LanguageItem_Click;
            btnLanguage.DropDownItems.Add(item);
        }

        // Sélectionner l'allemand par défaut
        SelectLanguage(Languages[0]);
    }

    private void SelectLanguage(LanguageInfo lang)
    {
        _currentLanguage = lang;
        btnLanguage.Image = LoadIcon($"{lang.Code}.png", 24);
        btnLanguage.ToolTipText = $"Langue : {lang.Name}";
        colTranslation.HeaderText = lang.Name;
        statusLanguage.Image = LoadIcon($"{lang.Code}.png");
        statusLanguage.Text = $"Langue : {lang.Name}";

        foreach (ToolStripMenuItem item in btnLanguage.DropDownItems)
            item.Checked = item.Tag is LanguageInfo l && l == lang;
    }

    private void LanguageItem_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem item || item.Tag is not LanguageInfo lang)
            return;

        if (lang == _currentLanguage)
            return;

        if (dataGridView.DataSource is SortableBindingList<TranslationRow> rows)
        {
            int oldCol = _currentLanguage.Column;
            int newCol = lang.Column;
            foreach (var row in rows)
                row.SwitchLanguage(oldCol, newCol);

            dataGridView.Refresh();
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
        btnLanguage.Enabled = false;
        btnSave.Enabled = false;

        try
        {
            var allColumns = Languages.Select(l => l.Column).ToArray();
            var activeColumn = _currentLanguage.Column;
            var progress = new Progress<int>(percent => statusProgressBar.Value = percent);
            var rows = await Task.Run(() => ExcelReader.Load(filePath, allColumns, activeColumn, progress));

            dataGridView.DataSource = new SortableBindingList<TranslationRow>(rows);
            statusRowCount.Text = $"Lignes : {rows.Count}";
            btnSave.Enabled = true;
            btnLanguage.Enabled = true;
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
        if (_currentFilePath is null || dataGridView.DataSource is not SortableBindingList<TranslationRow> rows)
            return;

        btnSave.Enabled = false;
        btnOpen.Enabled = false;
        btnLanguage.Enabled = false;
        statusProgressBar.Visible = true;
        statusProgressBar.Style = ProgressBarStyle.Marquee;

        try
        {
            var filePath = _currentFilePath;
            var column = _currentLanguage.Column;
            await Task.Run(() => ExcelReader.Save(filePath, column, rows));
            statusRowCount.Text = $"Lignes : {rows.Count} (sauvegardé)";
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
            btnLanguage.Enabled = true;
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
