namespace CheckTranslation;

partial class GlossaryExtractionDialog
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
        topPanel = new Panel();
        lblHeader = new Label();
        grid = new DataGridView();
        colSelected = new DataGridViewCheckBoxColumn();
        colSource = new DataGridViewTextBoxColumn();
        colDestination = new DataGridViewTextBoxColumn();
        colContext = new DataGridViewTextBoxColumn();
        bottomPanel = new Panel();
        btnAll = new Button();
        btnNone = new Button();
        btnCancel = new Button();
        btnOk = new Button();
        topPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        bottomPanel.SuspendLayout();
        SuspendLayout();
        //
        // topPanel
        //
        topPanel.Controls.Add(lblHeader);
        topPanel.Dock = DockStyle.Top;
        topPanel.Height = 44;
        topPanel.Name = "topPanel";
        topPanel.Padding = new Padding(10, 10, 10, 4);
        //
        // lblHeader
        //
        lblHeader.Dock = DockStyle.Fill;
        lblHeader.Name = "lblHeader";
        lblHeader.Text = "Cochez les termes à ajouter au glossaire. Vous pouvez éditer les valeurs avant validation.";
        lblHeader.TextAlign = ContentAlignment.MiddleLeft;
        //
        // grid
        //
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.Columns.AddRange(new DataGridViewColumn[] { colSelected, colSource, colDestination, colContext });
        grid.Dock = DockStyle.Fill;
        grid.Name = "grid";
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        //
        // colSelected
        //
        colSelected.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        colSelected.DataPropertyName = "Selected";
        colSelected.FillWeight = 8F;
        colSelected.HeaderText = "Ajouter";
        colSelected.Name = "colSelected";
        //
        // colSource
        //
        colSource.DataPropertyName = "Source";
        colSource.FillWeight = 25F;
        colSource.HeaderText = "Source";
        colSource.Name = "colSource";
        //
        // colDestination
        //
        colDestination.DataPropertyName = "Destination";
        colDestination.FillWeight = 25F;
        colDestination.HeaderText = "Destination";
        colDestination.Name = "colDestination";
        //
        // colContext
        //
        colContext.DataPropertyName = "Context";
        colContext.FillWeight = 42F;
        colContext.HeaderText = "Contexte";
        colContext.Name = "colContext";
        //
        // bottomPanel
        //
        bottomPanel.Controls.Add(btnAll);
        bottomPanel.Controls.Add(btnNone);
        bottomPanel.Controls.Add(btnCancel);
        bottomPanel.Controls.Add(btnOk);
        bottomPanel.Dock = DockStyle.Bottom;
        bottomPanel.Size = new Size(1100, 48);
        bottomPanel.Name = "bottomPanel";
        bottomPanel.Padding = new Padding(10, 8, 10, 8);
        //
        // btnAll
        //
        btnAll.Location = new Point(10, 10);
        btnAll.Name = "btnAll";
        btnAll.Size = new Size(110, 28);
        btnAll.Text = "Tout cocher";
        btnAll.UseVisualStyleBackColor = true;
        //
        // btnNone
        //
        btnNone.Location = new Point(130, 10);
        btnNone.Name = "btnNone";
        btnNone.Size = new Size(110, 28);
        btnNone.Text = "Tout décocher";
        btnNone.UseVisualStyleBackColor = true;
        //
        // btnCancel
        //
        btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(830, 10);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(120, 28);
        btnCancel.Text = "Annuler";
        btnCancel.UseVisualStyleBackColor = true;
        //
        // btnOk
        //
        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.Location = new Point(960, 10);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(130, 28);
        btnOk.Text = "Ajouter au glossaire";
        btnOk.UseVisualStyleBackColor = true;
        //
        // GlossaryExtractionDialog
        //
        AcceptButton = btnOk;
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(1100, 600);
        Controls.Add(grid);
        Controls.Add(topPanel);
        Controls.Add(bottomPanel);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        MinimumSize = new Size(900, 500);
        Name = "GlossaryExtractionDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Termes candidats à ajouter au glossaire";
        topPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        bottomPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private Panel topPanel;
    private Label lblHeader;
    private DataGridView grid;
    private DataGridViewCheckBoxColumn colSelected;
    private DataGridViewTextBoxColumn colSource;
    private DataGridViewTextBoxColumn colDestination;
    private DataGridViewTextBoxColumn colContext;
    private Panel bottomPanel;
    private Button btnAll;
    private Button btnNone;
    private Button btnCancel;
    private Button btnOk;
}
