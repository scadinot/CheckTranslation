namespace CheckTranslation;

/// <summary>
/// Résolution des différences d'un import de glossaire : une ligne par changement, à accepter ou
/// refuser individuellement (patron du dialog de fusion, en une seule fenêtre). Les suppressions
/// de terme arrivent décochées et sur fond rosé : une ligne perdue dans Excel ne doit pas effacer
/// un terme sans un choix explicite. À la validation, les décisions sont reportées dans les
/// <see cref="GlossaryChange.Accepted"/> de la liste fournie.
/// </summary>
internal sealed partial class GlossaryImportDiffForm : Form
{
    private static readonly Color RemovalBackColor = Color.FromArgb(255, 228, 228);

    private readonly IReadOnlyList<GlossaryChange> _changes;

    public GlossaryImportDiffForm(IReadOnlyList<GlossaryChange> changes, bool stampMismatch, IReadOnlyList<string> missingLanguages)
    {
        _changes = changes;
        InitializeComponent();

        foreach (var change in changes)
        {
            int index = grid.Rows.Add(change.Accepted, change.Source, change.FieldLabel, change.OldValue, change.NewValue);
            var row = grid.Rows[index];
            row.Tag = change;

            if (change.Kind == GlossaryChangeKind.TermRemoved)
                row.DefaultCellStyle.BackColor = RemovalBackColor;
        }

        int removals = changes.Count(change => change.Kind == GlossaryChangeKind.TermRemoved);
        int additions = changes.Count(change => change.Kind == GlossaryChangeKind.TermAdded);
        int modifications = changes.Count - removals - additions;

        lblSummary.Text = $"{changes.Count} différence(s) : {modifications} modification(s), {additions} nouveau(x) terme(s), {removals} suppression(s)."
            + (missingLanguages.Count > 0 ? $" Colonnes absentes du classeur, non comparées : {string.Join(", ", missingLanguages)}." : string.Empty)
            + (stampMismatch ? " ⚠ Le glossaire a été modifié dans l'application depuis cet export : vérifiez les lignes avant d'appliquer." : string.Empty);

        btnAll.Click += (_, _) => SetAll(true);
        btnNone.Click += (_, _) => SetAll(false);
        btnOk.Click += BtnOk_Click;
    }

    private void SetAll(bool accepted)
    {
        foreach (DataGridViewRow row in grid.Rows)
            row.Cells[colApply.Index].Value = accepted;
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        grid.EndEdit();

        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.Tag is GlossaryChange change)
                change.Accepted = row.Cells[colApply.Index].Value is true;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
