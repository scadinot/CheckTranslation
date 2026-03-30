namespace CheckTranslation;

internal sealed partial class MergeDifferenceForm : Form
{
    public MergeDifferenceResolution? Resolution { get; private set; }

    public MergeDifferenceForm(MergeDifference difference)
    {
        InitializeComponent();

        dataGridView.Rows.Clear();
        dataGridView.Rows.Add(
            difference.Project,
            difference.File,
            difference.Key,
            difference.DestinationFrench,
            difference.DestinationFrenchComment,
            difference.DestinationTranslation,
            difference.DestinationTranslationComment);
        dataGridView.Rows.Add(
            difference.Project,
            difference.File,
            difference.Key,
            difference.SourceFrench,
            difference.SourceFrenchComment,
            difference.SourceTranslation,
            difference.SourceTranslationComment);

        if (dataGridView.Rows.Count >= 2)
        {
            dataGridView.Rows[0].HeaderCell.Value = "Destination";
            dataGridView.Rows[1].HeaderCell.Value = "Source";
        }

        dataGridView.ClearSelection();
    }

    public static MergeDifferenceResolution? ShowDialog(IWin32Window owner, MergeDifference difference)
    {
        using var form = new MergeDifferenceForm(difference);
        return form.ShowDialog(owner) == DialogResult.OK ? form.Resolution : null;
    }

    private void BtnContinue_Click(object? sender, EventArgs e)
    {
        Resolution = new MergeDifferenceResolution(
            chkUpdateFrenchAndComment.Checked,
            chkUpdateTranslationAndComment.Checked);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void BtnCancelMerge_Click(object? sender, EventArgs e)
    {
        Resolution = null;
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
