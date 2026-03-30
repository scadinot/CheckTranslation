namespace CheckTranslation;

public partial class MainForm
{
    private bool _isRestoringLayout;

    private void InitLayoutPersistence()
    {
        Load += (_, _) => RestoreWindowLayout();
        FormClosing += MainForm_FormClosing;
        dataGridView.ColumnWidthChanged += DataGridView_ColumnWidthChanged_SaveLayout;
    }

    private void RestoreWindowLayout()
    {
        var config = AppConfig.Current;

        if (config.WindowWidth > 0 && config.WindowHeight > 0)
        {
            StartPosition = FormStartPosition.Manual;
            Size = new Size(config.WindowWidth, config.WindowHeight);
        }

        RestoreColumnWidths();
    }

    private void RestoreColumnWidths()
    {
        if (dataGridView.Columns.Count == 0)
            return;

        var savedWidths = IsDetailsLayoutActive()
            ? AppConfig.Current.ColumnFillWeightsWithDetails
            : AppConfig.Current.ColumnFillWeightsWithoutDetails;

        if (savedWidths.Count == 0)
            return;

        try
        {
            _isRestoringLayout = true;

            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                if (savedWidths.TryGetValue(column.Name, out var fillWeight) && fillWeight > 0)
                    column.FillWeight = fillWeight;
            }
        }
        finally
        {
            _isRestoringLayout = false;
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        SaveWindowLayout();
    }

    private void DataGridView_ColumnWidthChanged_SaveLayout(object? sender, DataGridViewColumnEventArgs e)
    {
        if (_isRestoringLayout)
            return;

        SaveColumnWidths();
    }

    private void SaveWindowLayout()
    {
        if (WindowState == FormWindowState.Minimized)
            return;

        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        var config = AppConfig.Current;
        config.WindowWidth = bounds.Width;
        config.WindowHeight = bounds.Height;
        SaveColumnWidths();
        config.Save();
    }

    private void SaveColumnWidths()
    {
        if (_isRestoringLayout)
            return;

        var target = IsDetailsLayoutActive()
            ? AppConfig.Current.ColumnFillWeightsWithDetails
            : AppConfig.Current.ColumnFillWeightsWithoutDetails;

        target.Clear();

        foreach (DataGridViewColumn column in dataGridView.Columns)
        {
            if (!column.Visible)
                continue;

            target[column.Name] = column.FillWeight;
        }
    }

    private bool IsDetailsLayoutActive()
        => colProject?.Visible ?? AppConfig.Current.ShowDetails;
}
