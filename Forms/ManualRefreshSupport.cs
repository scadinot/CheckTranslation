namespace CheckTranslation;

public partial class MainForm
{
    private ToolStripButton? btnRefresh;
    private bool _viewRefreshPending;

    private void InitRefreshButton()
    {
        btnRefresh = new ToolStripButton
        {
            Text = "Rafraîchir",
            Image = LoadIcon("refresh.png", 24),
            DisplayStyle = ToolStripItemDisplayStyle.Image,
        };
        btnRefresh.Click += BtnRefresh_Click;
        UpdateRefreshButtonState();
    }

    private async void BtnRefresh_Click(object? sender, EventArgs e)
    {
        if (_allRows is null || string.IsNullOrWhiteSpace(_currentFilePath) || !File.Exists(_currentFilePath))
        {
            ApplyFiltersPreservingSelection();
            RestoreStatusBar();
            return;
        }

        var previousRows = _allRows;
        var previousRowsByKey = previousRows
            .ToDictionary(BuildSyncKey, StringComparer.OrdinalIgnoreCase);

        btnRefresh!.Enabled = false;
        btnOpen.Enabled = false;
        btnSave.Enabled = false;
        btnMerge.Enabled = false;
        statusProgressBar.Visible = true;
        statusProgressBar.Style = ProgressBarStyle.Marquee;
        statusRowCount.Text = "Rafraîchissement...";

        try
        {
            var allColumns = Languages.Select(l => l.Column).ToArray();
            var activeColumn = _currentLanguage.Column;
            var refreshedRows = await Task.Run(() => _excelService.LoadWithRowProgress(_currentFilePath, allColumns, activeColumn));

            var changedFrenchRows = refreshedRows
                .Where(row => previousRowsByKey.TryGetValue(BuildSyncKey(row), out var previousRow)
                    && (!string.Equals(row.French, previousRow.French, StringComparison.Ordinal)
                        || !string.Equals(row.FrenchComment, previousRow.FrenchComment, StringComparison.Ordinal)))
                .ToList();

            if (changedFrenchRows.Count > 0)
            {
                var message = changedFrenchRows.Count == 1
                    ? "Le français ou le commentaire source a été modifié dans le fichier Excel.\n\nVoulez-vous mettre à jour la ligne affichée avec la nouvelle valeur ?"
                    : $"Le français ou le commentaire source a été modifié pour {changedFrenchRows.Count} ligne(s) dans le fichier Excel.\n\nVoulez-vous mettre à jour les lignes affichées avec les nouvelles valeurs ?";

                var result = MessageBox.Show(
                    message,
                    "Confirmation de mise à jour",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    foreach (var row in changedFrenchRows)
                    {
                        var previousRow = previousRowsByKey[BuildSyncKey(row)];
                        row.French = previousRow.French;
                        row.FrenchComment = previousRow.FrenchComment;
                    }
                }
            }

            foreach (var row in refreshedRows)
            {
                if (!previousRowsByKey.TryGetValue(BuildSyncKey(row), out var previousRow))
                    continue;

                row.Translation = previousRow.Translation;
                row.Comment = previousRow.Comment;

                foreach (var (col, value) in previousRow.Translations)
                    row.Translations[col] = value;

                foreach (var (col, value) in previousRow.Comments)
                    row.Comments[col] = value;
            }

            _allRows = refreshedRows;
            ApplyFiltersPreservingSelection();
            RestoreStatusBar();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossible de rafraîchir le fichier Excel :\n\n{ex.Message}",
                "Erreur",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            RestoreStatusBar();
        }
        finally
        {
            statusProgressBar.Style = ProgressBarStyle.Blocks;
            statusProgressBar.Visible = false;
            btnOpen.Enabled = true;
            btnSave.Enabled = _allRows is not null;
            btnMerge.Enabled = _allRows is not null;
            UpdateRefreshButtonState();
        }
    }

    private static string BuildSyncKey(TranslationRow row)
        => string.Join("\u001F", row.Project.Trim(), row.File.Trim(), row.Key.Trim());

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5 && CanRefreshView())
        {
            BtnRefresh_Click(this, EventArgs.Empty);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void MarkViewRefreshPendingIfNeeded()
    {
        SetViewRefreshPending(_filters.Count > 0 || _sortColumnIndex >= 0);
    }

    private void SetViewRefreshPending(bool pending)
    {
        _viewRefreshPending = pending;
        UpdateRefreshButtonState();
    }

    private void UpdateRefreshButtonState()
    {
        if (btnRefresh is null)
            return;

        btnRefresh.Enabled = CanRefreshView();
        btnRefresh.Text = _viewRefreshPending ? "Rafraîchir *" : "Rafraîchir";
        btnRefresh.ToolTipText = _viewRefreshPending
            ? "Réappliquer les filtres et le tri pour refléter les dernières modifications"
            : HasCurrentFileLoaded()
                ? "Recharger le fichier Excel courant et détecter les changements du français/commentaire"
                : "Tri et filtres déjà à jour";
    }

    private bool CanRefreshView()
        => _viewRefreshPending || HasCurrentFileLoaded();

    private bool HasCurrentFileLoaded()
        => _allRows is not null && !string.IsNullOrWhiteSpace(_currentFilePath) && File.Exists(_currentFilePath);
}
