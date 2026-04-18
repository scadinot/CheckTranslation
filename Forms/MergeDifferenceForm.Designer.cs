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
        rootLayout.Location = new Point(10, 9);
        rootLayout.Margin = new Padding(3, 2, 3, 2);
        rootLayout.Name = "rootLayout";
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle());
        rootLayout.RowStyles.Add(new RowStyle());
        rootLayout.Size = new Size(1716, 183);
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
        dataGridView.Dock = DockStyle.Fill;
        dataGridView.Location = new Point(3, 2);
        dataGridView.Margin = new Padding(3, 2, 3, 2);
        dataGridView.MultiSelect = false;
        dataGridView.Name = "dataGridView";
        dataGridView.ReadOnly = true;
        dataGridView.RowHeadersWidth = 110;
        dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dataGridView.Size = new Size(1710, 96);
        dataGridView.TabIndex = 0;
        // 
        // optionsPanel
        // 
        optionsPanel.AutoSize = true;
        optionsPanel.Controls.Add(chkUpdateFrenchAndComment);
        optionsPanel.Controls.Add(chkUpdateTranslationAndComment);
        optionsPanel.Dock = DockStyle.Fill;
        optionsPanel.FlowDirection = FlowDirection.TopDown;
        optionsPanel.Location = new Point(0, 100);
        optionsPanel.Margin = new Padding(0, 0, 0, 6);
        optionsPanel.Name = "optionsPanel";
        optionsPanel.Size = new Size(1716, 46);
        optionsPanel.TabIndex = 1;
        optionsPanel.WrapContents = false;
        // 
        // chkUpdateFrenchAndComment
        // 
        chkUpdateFrenchAndComment.AutoSize = true;
        chkUpdateFrenchAndComment.Checked = true;
        chkUpdateFrenchAndComment.CheckState = CheckState.Checked;
        chkUpdateFrenchAndComment.Location = new Point(3, 2);
        chkUpdateFrenchAndComment.Margin = new Padding(3, 2, 3, 2);
        chkUpdateFrenchAndComment.Name = "chkUpdateFrenchAndComment";
        chkUpdateFrenchAndComment.Size = new Size(255, 19);
        chkUpdateFrenchAndComment.TabIndex = 0;
        chkUpdateFrenchAndComment.Text = "Mettre à jour le texte/commentaire français";
        chkUpdateFrenchAndComment.UseVisualStyleBackColor = true;
        // 
        // chkUpdateTranslationAndComment
        // 
        chkUpdateTranslationAndComment.AutoSize = true;
        chkUpdateTranslationAndComment.Checked = true;
        chkUpdateTranslationAndComment.CheckState = CheckState.Checked;
        chkUpdateTranslationAndComment.Location = new Point(3, 25);
        chkUpdateTranslationAndComment.Margin = new Padding(3, 2, 3, 2);
        chkUpdateTranslationAndComment.Name = "chkUpdateTranslationAndComment";
        chkUpdateTranslationAndComment.Size = new Size(240, 19);
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
        buttonsPanel.Location = new Point(0, 152);
        buttonsPanel.Margin = new Padding(0);
        buttonsPanel.Name = "buttonsPanel";
        buttonsPanel.Size = new Size(1716, 31);
        buttonsPanel.TabIndex = 2;
        buttonsPanel.WrapContents = false;
        // 
        // btnCancelMerge
        // 
        btnCancelMerge.AutoSize = true;
        btnCancelMerge.Location = new Point(1582, 0);
        btnCancelMerge.Margin = new Padding(3, 0, 0, 0);
        btnCancelMerge.Name = "btnCancelMerge";
        btnCancelMerge.Padding = new Padding(9, 3, 9, 3);
        btnCancelMerge.Size = new Size(134, 31);
        btnCancelMerge.TabIndex = 0;
        btnCancelMerge.Text = "Annuler le merge";
        btnCancelMerge.UseVisualStyleBackColor = true;
        btnCancelMerge.Click += BtnCancelMerge_Click;
        // 
        // btnContinue
        // 
        btnContinue.AutoSize = true;
        btnContinue.Location = new Point(1474, 0);
        btnContinue.Margin = new Padding(3, 0, 0, 0);
        btnContinue.Name = "btnContinue";
        btnContinue.Padding = new Padding(9, 3, 9, 3);
        btnContinue.Size = new Size(105, 31);
        btnContinue.TabIndex = 1;
        btnContinue.Text = "Continuer";
        btnContinue.UseVisualStyleBackColor = true;
        btnContinue.Click += BtnContinue_Click;
        // 
        // MergeDifferenceForm
        // 
        AcceptButton = btnContinue;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancelMerge;
        ClientSize = new Size(1736, 201);
        ControlBox = false;
        Controls.Add(rootLayout);
        Margin = new Padding(3, 2, 3, 2);
        MaximumSize = new Size(3000, 240);
        MinimumSize = new Size(1052, 240);
        Name = "MergeDifferenceForm";
        Padding = new Padding(10, 9, 10, 9);
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
