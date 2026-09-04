namespace CheckTranslation;

partial class GlossaryImportDiffForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        lblSummary = new Label();
        grid = new DataGridView();
        colApply = new DataGridViewCheckBoxColumn();
        colTerm = new DataGridViewTextBoxColumn();
        colField = new DataGridViewTextBoxColumn();
        colOld = new DataGridViewTextBoxColumn();
        colNew = new DataGridViewTextBoxColumn();
        bottomPanel = new Panel();
        btnAll = new Button();
        btnNone = new Button();
        btnCancel = new Button();
        btnOk = new Button();
        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        bottomPanel.SuspendLayout();
        SuspendLayout();
        //
        // lblSummary
        //
        lblSummary.Dock = DockStyle.Top;
        lblSummary.Height = 36;
        lblSummary.Name = "lblSummary";
        lblSummary.Padding = new Padding(10, 8, 10, 4);
        lblSummary.TextAlign = ContentAlignment.MiddleLeft;
        //
        // grid
        //
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.Columns.AddRange(new DataGridViewColumn[] { colApply, colTerm, colField, colOld, colNew });
        grid.Dock = DockStyle.Fill;
        grid.Name = "grid";
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        //
        // colApply
        //
        colApply.FillWeight = 8F;
        colApply.HeaderText = "Appliquer";
        colApply.Name = "colApply";
        colApply.SortMode = DataGridViewColumnSortMode.Automatic;
        //
        // colTerm
        //
        colTerm.FillWeight = 18F;
        colTerm.HeaderText = "Terme";
        colTerm.Name = "colTerm";
        colTerm.ReadOnly = true;
        colTerm.SortMode = DataGridViewColumnSortMode.Automatic;
        //
        // colField
        //
        colField.FillWeight = 14F;
        colField.HeaderText = "Champ";
        colField.Name = "colField";
        colField.ReadOnly = true;
        colField.SortMode = DataGridViewColumnSortMode.Automatic;
        //
        // colOld
        //
        colOld.FillWeight = 30F;
        colOld.HeaderText = "Valeur actuelle";
        colOld.Name = "colOld";
        colOld.ReadOnly = true;
        colOld.SortMode = DataGridViewColumnSortMode.Automatic;
        //
        // colNew
        //
        colNew.FillWeight = 30F;
        colNew.HeaderText = "Valeur importée";
        colNew.Name = "colNew";
        colNew.ReadOnly = true;
        colNew.SortMode = DataGridViewColumnSortMode.Automatic;
        //
        // bottomPanel
        //
        bottomPanel.Controls.Add(btnAll);
        bottomPanel.Controls.Add(btnNone);
        bottomPanel.Controls.Add(btnCancel);
        bottomPanel.Controls.Add(btnOk);
        bottomPanel.Dock = DockStyle.Bottom;
        bottomPanel.Height = 48;
        bottomPanel.Name = "bottomPanel";
        bottomPanel.Padding = new Padding(10, 8, 10, 8);
        //
        // btnAll
        //
        btnAll.Location = new Point(10, 10);
        btnAll.Name = "btnAll";
        btnAll.Size = new Size(120, 28);
        btnAll.Text = "Tout accepter";
        btnAll.UseVisualStyleBackColor = true;
        //
        // btnNone
        //
        btnNone.Location = new Point(140, 10);
        btnNone.Name = "btnNone";
        btnNone.Size = new Size(120, 28);
        btnNone.Text = "Tout refuser";
        btnNone.UseVisualStyleBackColor = true;
        //
        // btnCancel
        //
        btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(870, 10);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(100, 28);
        btnCancel.Text = "Annuler";
        btnCancel.UseVisualStyleBackColor = true;
        //
        // btnOk
        //
        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.Location = new Point(980, 10);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(100, 28);
        btnOk.Text = "Appliquer";
        btnOk.UseVisualStyleBackColor = true;
        //
        // GlossaryImportDiffForm
        //
        AcceptButton = btnOk;
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(1100, 560);
        Controls.Add(grid);
        Controls.Add(lblSummary);
        Controls.Add(bottomPanel);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        MinimumSize = new Size(900, 420);
        Name = "GlossaryImportDiffForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Import du glossaire — différences";
        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        bottomPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private Label lblSummary;
    private DataGridView grid;
    private DataGridViewCheckBoxColumn colApply;
    private DataGridViewTextBoxColumn colTerm;
    private DataGridViewTextBoxColumn colField;
    private DataGridViewTextBoxColumn colOld;
    private DataGridViewTextBoxColumn colNew;
    private Panel bottomPanel;
    private Button btnAll;
    private Button btnNone;
    private Button btnCancel;
    private Button btnOk;
}
