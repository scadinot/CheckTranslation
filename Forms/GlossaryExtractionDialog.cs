using System.ComponentModel;

namespace CheckTranslation;

internal sealed partial class GlossaryExtractionDialog : Form
{
    private readonly BindingList<CandidateRow> _candidates = new();

    public IReadOnlyList<GlossaryEntry> AcceptedEntries { get; private set; } = Array.Empty<GlossaryEntry>();

    public GlossaryExtractionDialog()
    {
        InitializeComponent();

        grid.DataSource = _candidates;
        grid.CellContentClick += Grid_CellContentClick;
        btnAll.Click += (_, _) => SetAll(true);
        btnNone.Click += (_, _) => SetAll(false);
        btnOk.Click += BtnOk_Click;
    }

    public void SetCandidates(IReadOnlyList<GlossaryEntry> entries, string languageName)
    {
        _candidates.Clear();
        foreach (var entry in entries)
        {
            _candidates.Add(new CandidateRow
            {
                Selected = true,
                Source = entry.Source,
                Destination = entry.Destination,
                Context = entry.Context,
            });
        }
        lblHeader.Text = $"{entries.Count} terme(s) candidat(s) pour {languageName}. Cochez ceux à ajouter (édition possible).";
    }

    private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
            return;
        var columnName = grid.Columns[e.ColumnIndex].Name;
        if (columnName == "colSelected")
            grid.EndEdit();
    }

    private void SetAll(bool value)
    {
        foreach (var row in _candidates)
            row.Selected = value;
        grid.Refresh();
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        grid.EndEdit();

        AcceptedEntries = _candidates
            .Where(r => r.Selected
                && !string.IsNullOrWhiteSpace(r.Source)
                && !string.IsNullOrWhiteSpace(r.Destination))
            .Select(r => new GlossaryEntry
            {
                Source = r.Source.Trim(),
                Destination = r.Destination.Trim(),
                Context = r.Context.Trim(),
            })
            .ToList();

        DialogResult = DialogResult.OK;
        Close();
    }

    private sealed class CandidateRow
    {
        public bool Selected { get; set; } = true;
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
    }
}
