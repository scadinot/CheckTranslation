namespace CheckTranslation;

partial class MergeDifferenceForm
{
    private System.ComponentModel.IContainer? components = null;

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
        rootLayout = new TableLayoutPanel();
        dataGridView = new DataGridView();
        colProject = new DataGridViewTextBoxColumn();
        colFile = new DataGridViewTextBoxColumn();
        colKey = new DataGridViewTextBoxColumn();
        colFrench = new DataGridViewTextBoxColumn();
        colFrenchComment = new DataGridViewTextBoxColumn();
        colTranslation = new DataGridViewTextBoxColumn();
        colTranslationComment = new DataGridViewTextBoxColumn();
        optionsPanel = new FlowLayoutPanel();
        chkUpdateFrenchAndComment = new CheckBox();
        chkUpdateTranslationAndComment = new CheckBox();
        buttonsPanel = new FlowLayoutPanel();
        btnCancelMerge = new Button();
        btnContinue = new Button();
        rootLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dataGridView).BeginInit();
        optionsPanel.SuspendLayout();
        buttonsPanel.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(dataGridView, 0, 0);
        rootLayout.Controls.Add(optionsPanel, 0, 1);
        rootLayout.Controls.Add(buttonsPanel, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(12, 12);
        rootLayout.Name = "rootLayout";
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle());
        rootLayout.RowStyles.Add(new RowStyle());
        rootLayout.Size = new Size(1158, 199);
        rootLayout.TabIndex = 0;
        // 
        // dataGridView
        // 
        dataGridView.AllowUserToAddRows = false;
        dataGridView.AllowUserToDeleteRows = false;
        dataGridView.AllowUserToResizeRows = false;
        dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dataGridView.BackgroundColor = SystemColors.Window;
        dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dataGridView.Columns.AddRange(new DataGridViewColumn[] { colProject, colFile, colKey, colFrench, colFrenchComment, colTranslation, colTranslationComment });
        dataGridView.Dock = DockStyle.Fill;
        dataGridView.Location = new Point(3, 3);
        dataGridView.MultiSelect = false;
        dataGridView.Name = "dataGridView";
        dataGridView.ReadOnly = true;
        dataGridView.RowHeadersWidth = 110;
        dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dataGridView.Size = new Size(1152, 87);
        dataGridView.TabIndex = 0;
        // 
        // colProject
        // 
        colProject.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        colProject.FillWeight = 10F;
        colProject.HeaderText = "Projet";
        colProject.MinimumWidth = 6;
        colProject.Name = "colProject";
        colProject.ReadOnly = true;
        colProject.Width = 77;
        // 
        // colFile
        // 
        colFile.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        colFile.FillWeight = 16F;
        colFile.HeaderText = "Fichier";
        colFile.MinimumWidth = 6;
        colFile.Name = "colFile";
        colFile.ReadOnly = true;
        colFile.Width = 81;
        // 
        // colKey
        // 
        colKey.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        colKey.FillWeight = 16F;
        colKey.HeaderText = "Clé";
        colKey.MinimumWidth = 6;
        colKey.Name = "colKey";
        colKey.ReadOnly = true;
        colKey.Width = 59;
        // 
        // colFrench
        // 
        colFrench.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        colFrench.FillWeight = 18F;
        colFrench.HeaderText = "Français";
        colFrench.MinimumWidth = 6;
        colFrench.Name = "colFrench";
        colFrench.ReadOnly = true;
        colFrench.Width = 91;
        // 
        // colFrenchComment
        // 
        colFrenchComment.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        colFrenchComment.FillWeight = 18F;
        colFrenchComment.HeaderText = "Commentaire";
        colFrenchComment.MinimumWidth = 6;
        colFrenchComment.Name = "colFrenchComment";
        colFrenchComment.ReadOnly = true;
        colFrenchComment.Width = 128;
        // 
        // colTranslation
        // 
        colTranslation.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        colTranslation.FillWeight = 18F;
        colTranslation.HeaderText = "Traduction";
        colTranslation.MinimumWidth = 6;
        colTranslation.Name = "colTranslation";
        colTranslation.ReadOnly = true;
        colTranslation.Width = 108;
        // 
        // colTranslationComment
        // 
        colTranslationComment.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        colTranslationComment.FillWeight = 18F;
        colTranslationComment.HeaderText = "Commentaire";
        colTranslationComment.MinimumWidth = 6;
        colTranslationComment.Name = "colTranslationComment";
        colTranslationComment.ReadOnly = true;
        // 
        // optionsPanel
        // 
        optionsPanel.AutoSize = true;
        optionsPanel.Controls.Add(chkUpdateFrenchAndComment);
        optionsPanel.Controls.Add(chkUpdateTranslationAndComment);
        optionsPanel.Dock = DockStyle.Fill;
        optionsPanel.FlowDirection = FlowDirection.TopDown;
        optionsPanel.Location = new Point(0, 93);
        optionsPanel.Margin = new Padding(0, 0, 0, 8);
        optionsPanel.Name = "optionsPanel";
        optionsPanel.Size = new Size(1158, 60);
        optionsPanel.TabIndex = 1;
        optionsPanel.WrapContents = false;
        // 
        // chkUpdateFrenchAndComment
        // 
        chkUpdateFrenchAndComment.AutoSize = true;
        chkUpdateFrenchAndComment.Checked = true;
        chkUpdateFrenchAndComment.CheckState = CheckState.Checked;
        chkUpdateFrenchAndComment.Location = new Point(3, 3);
        chkUpdateFrenchAndComment.Name = "chkUpdateFrenchAndComment";
        chkUpdateFrenchAndComment.Size = new Size(319, 24);
        chkUpdateFrenchAndComment.TabIndex = 0;
        chkUpdateFrenchAndComment.Text = "Mettre à jour le texte/commentaire français";
        chkUpdateFrenchAndComment.UseVisualStyleBackColor = true;
        // 
        // chkUpdateTranslationAndComment
        // 
        chkUpdateTranslationAndComment.AutoSize = true;
        chkUpdateTranslationAndComment.Checked = true;
        chkUpdateTranslationAndComment.CheckState = CheckState.Checked;
        chkUpdateTranslationAndComment.Location = new Point(3, 33);
        chkUpdateTranslationAndComment.Name = "chkUpdateTranslationAndComment";
        chkUpdateTranslationAndComment.Size = new Size(299, 24);
        chkUpdateTranslationAndComment.TabIndex = 1;
        chkUpdateTranslationAndComment.Text = "Mettre à jour la traduction/commentaire";
        chkUpdateTranslationAndComment.UseVisualStyleBackColor = true;
        // 
        // buttonsPanel
        // 
        buttonsPanel.AutoSize = true;
        buttonsPanel.Controls.Add(btnCancelMerge);
        buttonsPanel.Controls.Add(btnContinue);
        buttonsPanel.Dock = DockStyle.Fill;
        buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonsPanel.Location = new Point(0, 161);
        buttonsPanel.Margin = new Padding(0);
        buttonsPanel.Name = "buttonsPanel";
        buttonsPanel.Size = new Size(1158, 38);
        buttonsPanel.TabIndex = 2;
        buttonsPanel.WrapContents = false;
        // 
        // btnCancelMerge
        // 
        btnCancelMerge.AutoSize = true;
        btnCancelMerge.Location = new Point(1005, 0);
        btnCancelMerge.Margin = new Padding(3, 0, 0, 0);
        btnCancelMerge.Name = "btnCancelMerge";
        btnCancelMerge.Padding = new Padding(10, 4, 10, 4);
        btnCancelMerge.Size = new Size(153, 38);
        btnCancelMerge.TabIndex = 0;
        btnCancelMerge.Text = "Annuler le merge";
        btnCancelMerge.UseVisualStyleBackColor = true;
        btnCancelMerge.Click += BtnCancelMerge_Click;
        // 
        // btnContinue
        // 
        btnContinue.AutoSize = true;
        btnContinue.Location = new Point(882, 0);
        btnContinue.Margin = new Padding(3, 0, 0, 0);
        btnContinue.Name = "btnContinue";
        btnContinue.Padding = new Padding(10, 4, 10, 4);
        btnContinue.Size = new Size(120, 38);
        btnContinue.TabIndex = 1;
        btnContinue.Text = "Continuer";
        btnContinue.UseVisualStyleBackColor = true;
        btnContinue.Click += BtnContinue_Click;
        // 
        // MergeDifferenceForm
        // 
        AcceptButton = btnContinue;
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancelMerge;
        ClientSize = new Size(1182, 223);
        ControlBox = false;
        Controls.Add(rootLayout);
        MaximumSize = new Size(2000, 270);
        MinimumSize = new Size(1200, 270);
        Name = "MergeDifferenceForm";
        Padding = new Padding(12);
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Conflit de fusion";
        rootLayout.ResumeLayout(false);
        rootLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)dataGridView).EndInit();
        optionsPanel.ResumeLayout(false);
        optionsPanel.PerformLayout();
        buttonsPanel.ResumeLayout(false);
        buttonsPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel rootLayout;
    private DataGridView dataGridView;
    private FlowLayoutPanel optionsPanel;
    private CheckBox chkUpdateFrenchAndComment;
    private CheckBox chkUpdateTranslationAndComment;
    private FlowLayoutPanel buttonsPanel;
    private Button btnCancelMerge;
    private Button btnContinue;
    private DataGridViewTextBoxColumn colProject;
    private DataGridViewTextBoxColumn colFile;
    private DataGridViewTextBoxColumn colKey;
    private DataGridViewTextBoxColumn colFrench;
    private DataGridViewTextBoxColumn colFrenchComment;
    private DataGridViewTextBoxColumn colTranslation;
    private DataGridViewTextBoxColumn colTranslationComment;
}
