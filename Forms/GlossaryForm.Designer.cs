namespace CheckTranslation;

partial class GlossaryForm
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
        lblLanguage = new Label();
        languageCombo = new ComboBox();
        lblCount = new Label();
        grid = new DataGridView();
        colSource = new DataGridViewTextBoxColumn();
        colDestination = new DataGridViewTextBoxColumn();
        colContext = new DataGridViewTextBoxColumn();
        bottomPanel = new Panel();
        btnAdd = new Button();
        btnRemove = new Button();
        btnCancel = new Button();
        btnOk = new Button();
        topPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        bottomPanel.SuspendLayout();
        SuspendLayout();
        //
        // topPanel
        //
        topPanel.Controls.Add(lblLanguage);
        topPanel.Controls.Add(languageCombo);
        topPanel.Controls.Add(lblCount);
        topPanel.Dock = DockStyle.Top;
        topPanel.Height = 40;
        topPanel.Name = "topPanel";
        topPanel.Padding = new Padding(10, 8, 10, 4);
        //
        // lblLanguage
        //
        lblLanguage.AutoSize = true;
        lblLanguage.Location = new Point(10, 12);
        lblLanguage.Name = "lblLanguage";
        lblLanguage.Text = "Langue :";
        //
        // languageCombo
        //
        languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        languageCombo.Location = new Point(80, 8);
        languageCombo.Name = "languageCombo";
        languageCombo.Width = 220;
        //
        // lblCount
        //
        lblCount.AutoSize = true;
        lblCount.ForeColor = SystemColors.GrayText;
        lblCount.Location = new Point(320, 12);
        lblCount.Name = "lblCount";
        //
        // grid
        //
        grid.AllowUserToAddRows = true;
        grid.AllowUserToDeleteRows = true;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.Columns.AddRange(new DataGridViewColumn[] { colSource, colDestination, colContext });
        grid.Dock = DockStyle.Fill;
        grid.Name = "grid";
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        //
        // colSource
        //
        colSource.DataPropertyName = "Source";
        colSource.FillWeight = 30F;
        colSource.HeaderText = "Source";
        colSource.Name = "colSource";
        colSource.SortMode = DataGridViewColumnSortMode.Automatic;
        //
        // colDestination
        //
        colDestination.DataPropertyName = "Destination";
        colDestination.FillWeight = 30F;
        colDestination.HeaderText = "Destination";
        colDestination.Name = "colDestination";
        colDestination.SortMode = DataGridViewColumnSortMode.Automatic;
        //
        // colContext
        //
        colContext.DataPropertyName = "Context";
        colContext.FillWeight = 40F;
        colContext.HeaderText = "Contexte";
        colContext.Name = "colContext";
        colContext.SortMode = DataGridViewColumnSortMode.Automatic;
        //
        // bottomPanel
        //
        bottomPanel.Controls.Add(btnAdd);
        bottomPanel.Controls.Add(btnRemove);
        bottomPanel.Controls.Add(btnCancel);
        bottomPanel.Controls.Add(btnOk);
        bottomPanel.Dock = DockStyle.Bottom;
        bottomPanel.Height = 48;
        bottomPanel.Name = "bottomPanel";
        bottomPanel.Padding = new Padding(10, 8, 10, 8);
        //
        // btnAdd
        //
        btnAdd.Location = new Point(10, 10);
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(100, 28);
        btnAdd.Text = "Ajouter";
        btnAdd.UseVisualStyleBackColor = true;
        //
        // btnRemove
        //
        btnRemove.Location = new Point(120, 10);
        btnRemove.Name = "btnRemove";
        btnRemove.Size = new Size(100, 28);
        btnRemove.Text = "Supprimer";
        btnRemove.UseVisualStyleBackColor = true;
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
        btnOk.Text = "Enregistrer";
        btnOk.UseVisualStyleBackColor = true;
        //
        // GlossaryForm
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
        Name = "GlossaryForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Glossaire métier";
        topPanel.ResumeLayout(false);
        topPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        bottomPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private Panel topPanel;
    private Label lblLanguage;
    private ComboBox languageCombo;
    private Label lblCount;
    private DataGridView grid;
    private DataGridViewTextBoxColumn colSource;
    private DataGridViewTextBoxColumn colDestination;
    private DataGridViewTextBoxColumn colContext;
    private Panel bottomPanel;
    private Button btnAdd;
    private Button btnRemove;
    private Button btnCancel;
    private Button btnOk;
}
