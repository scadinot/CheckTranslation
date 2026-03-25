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

        int insertIndex = toolStrip.Items.IndexOf(btnConfig);
        toolStrip.Items.Insert(insertIndex, btnRefresh);
        UpdateRefreshButtonState();
    }

    private void BtnRefresh_Click(object? sender, EventArgs e)
    {
        ApplyFiltersPreservingSelection();
        RestoreStatusBar();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5 && btnRefresh?.Enabled == true)
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

        btnRefresh.Enabled = _viewRefreshPending;
        btnRefresh.Text = _viewRefreshPending ? "Rafraîchir *" : "Rafraîchir";
        btnRefresh.ToolTipText = _viewRefreshPending
            ? "Réappliquer les filtres et le tri pour refléter les dernières modifications"
            : "Tri et filtres déjà à jour";
    }
}
