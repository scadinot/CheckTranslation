using System.ComponentModel;

namespace CheckTranslation;

internal sealed partial class MergeDifferenceForm : Form
{
    private readonly BindingList<MergeDifferenceDisplayRow> _rows = [];

    public MergeDifferenceResolution? Resolution { get; private set; }

    public MergeDifferenceForm(MergeDifference difference)
    {
        InitializeComponent();

        _rows.Add(new MergeDifferenceDisplayRow("Destination", difference.Destination));
        _rows.Add(new MergeDifferenceDisplayRow("Source", difference.Source));
        dataGridView.DataSource = _rows;

        for (int i = 0; i < dataGridView.Rows.Count && i < _rows.Count; i++)
            dataGridView.Rows[i].HeaderCell.Value = _rows[i].Origin;

        dataGridView.CurrentCell = null;
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

    private sealed class MergeDifferenceDisplayRow
    {
        public MergeDifferenceDisplayRow(string origin, MergeRowSnapshot snapshot)
        {
            Origin = origin;
            Project = snapshot.Project;
            File = snapshot.File;
            Key = snapshot.Key;
            French = snapshot.French;
            FrenchComment = snapshot.FrenchComment;
            Translation = snapshot.Translation;
            TranslationComment = snapshot.TranslationComment;
        }

        public string Origin { get; }
        public string Project { get; }
        public string File { get; }
        public string Key { get; }
        public string French { get; }
        public string FrenchComment { get; }
        public string Translation { get; }
        public string TranslationComment { get; }
    }
}
