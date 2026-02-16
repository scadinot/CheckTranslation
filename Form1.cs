namespace CheckTransation;

public partial class Form1 : Form
{
    private string? _currentFilePath;

    public Form1()
    {
        InitializeComponent();
        btnOpen.Click += BtnOpen_Click;
        btnSave.Click += BtnSave_Click;
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
        statusProgressBar.Visible = true;
        statusProgressBar.Value = 0;
        statusFileName.Text = $"Fichier : {Path.GetFileName(_currentFilePath)}";
        statusRowCount.Text = "Chargement...";
        statusLanguage.Text = "Langue : Allemand";
        dataGridView.AutoGenerateColumns = false;
        btnOpen.Enabled = false;

        try
        {
            var progress = new Progress<int>(percent => statusProgressBar.Value = percent);
            var rows = await Task.Run(() => ExcelReader.Load(_currentFilePath, progress));

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
        if (_currentFilePath is null || dataGridView.DataSource is not SortableBindingList<TranslationRow> rows)
            return;

        btnSave.Enabled = false;
        btnOpen.Enabled = false;
        statusProgressBar.Visible = true;
        statusProgressBar.Style = ProgressBarStyle.Marquee;

        try
        {
            var filePath = _currentFilePath;
            await Task.Run(() => ExcelReader.Save(filePath, rows));
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
        }
    }
}
