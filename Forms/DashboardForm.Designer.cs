namespace CheckTranslation;

partial class DashboardForm
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
        summaryPanel = new FlowLayoutPanel();
        tabs = new TabControl();
        tabLanguages = new TabPage();
        gridLanguages = new DataGridView();
        tabProjects = new TabPage();
        gridProjects = new DataGridView();
        tabFiles = new TabPage();
        gridFiles = new DataGridView();
        tabLayout = new TabPage();
        gridLayout = new DataGridView();
        bottomPanel = new Panel();
        lblGroupLanguage = new Label();
        cmbGroupLanguage = new ComboBox();
        lblHint = new Label();
        btnCopy = new Button();
        btnClose = new Button();
        summaryPanel.SuspendLayout();
        tabs.SuspendLayout();
        tabLanguages.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)gridLanguages).BeginInit();
        tabProjects.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)gridProjects).BeginInit();
        tabFiles.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)gridFiles).BeginInit();
        tabLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)gridLayout).BeginInit();
        bottomPanel.SuspendLayout();
        SuspendLayout();
        //
        // summaryPanel
        //
        summaryPanel.AutoSize = true;
        summaryPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        summaryPanel.Dock = DockStyle.Top;
        summaryPanel.Location = new Point(0, 0);
        summaryPanel.Name = "summaryPanel";
        summaryPanel.Padding = new Padding(10, 10, 10, 4);
        summaryPanel.Size = new Size(1120, 104);
        summaryPanel.TabIndex = 0;
        summaryPanel.WrapContents = true;
        //
        // tabs
        //
        tabs.Controls.Add(tabLanguages);
        tabs.Controls.Add(tabProjects);
        tabs.Controls.Add(tabFiles);
        tabs.Controls.Add(tabLayout);
        tabs.Dock = DockStyle.Fill;
        tabs.Location = new Point(0, 104);
        tabs.Name = "tabs";
        tabs.Padding = new Point(12, 4);
        tabs.SelectedIndex = 0;
        tabs.Size = new Size(1120, 552);
        tabs.TabIndex = 1;
        //
        // tabLanguages
        //
        tabLanguages.Controls.Add(gridLanguages);
        tabLanguages.Location = new Point(4, 29);
        tabLanguages.Name = "tabLanguages";
        tabLanguages.Padding = new Padding(6);
        tabLanguages.Size = new Size(1112, 519);
        tabLanguages.TabIndex = 0;
        tabLanguages.Text = "Par langue";
        tabLanguages.UseVisualStyleBackColor = true;
        //
        // gridLanguages
        //
        gridLanguages.AllowUserToAddRows = false;
        gridLanguages.AllowUserToDeleteRows = false;
        gridLanguages.AllowUserToResizeRows = false;
        gridLanguages.BackgroundColor = SystemColors.Window;
        gridLanguages.BorderStyle = BorderStyle.None;
        gridLanguages.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        gridLanguages.Dock = DockStyle.Fill;
        gridLanguages.EditMode = DataGridViewEditMode.EditProgrammatically;
        gridLanguages.Location = new Point(6, 6);
        gridLanguages.Name = "gridLanguages";
        gridLanguages.ReadOnly = true;
        gridLanguages.RowHeadersVisible = false;
        gridLanguages.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridLanguages.Size = new Size(1100, 507);
        gridLanguages.TabIndex = 0;
        //
        // tabProjects
        //
        tabProjects.Controls.Add(gridProjects);
        tabProjects.Location = new Point(4, 29);
        tabProjects.Name = "tabProjects";
        tabProjects.Padding = new Padding(6);
        tabProjects.Size = new Size(1112, 519);
        tabProjects.TabIndex = 1;
        tabProjects.Text = "Par projet";
        tabProjects.UseVisualStyleBackColor = true;
        //
        // gridProjects
        //
        gridProjects.AllowUserToAddRows = false;
        gridProjects.AllowUserToDeleteRows = false;
        gridProjects.AllowUserToResizeRows = false;
        gridProjects.BackgroundColor = SystemColors.Window;
        gridProjects.BorderStyle = BorderStyle.None;
        gridProjects.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        gridProjects.Dock = DockStyle.Fill;
        gridProjects.EditMode = DataGridViewEditMode.EditProgrammatically;
        gridProjects.Location = new Point(6, 6);
        gridProjects.Name = "gridProjects";
        gridProjects.ReadOnly = true;
        gridProjects.RowHeadersVisible = false;
        gridProjects.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridProjects.Size = new Size(1100, 507);
        gridProjects.TabIndex = 0;
        //
        // tabFiles
        //
        tabFiles.Controls.Add(gridFiles);
        tabFiles.Location = new Point(4, 29);
        tabFiles.Name = "tabFiles";
        tabFiles.Padding = new Padding(6);
        tabFiles.Size = new Size(1112, 519);
        tabFiles.TabIndex = 2;
        tabFiles.Text = "Par fichier";
        tabFiles.UseVisualStyleBackColor = true;
        //
        // gridFiles
        //
        gridFiles.AllowUserToAddRows = false;
        gridFiles.AllowUserToDeleteRows = false;
        gridFiles.AllowUserToResizeRows = false;
        gridFiles.BackgroundColor = SystemColors.Window;
        gridFiles.BorderStyle = BorderStyle.None;
        gridFiles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        gridFiles.Dock = DockStyle.Fill;
        gridFiles.EditMode = DataGridViewEditMode.EditProgrammatically;
        gridFiles.Location = new Point(6, 6);
        gridFiles.Name = "gridFiles";
        gridFiles.ReadOnly = true;
        gridFiles.RowHeadersVisible = false;
        gridFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridFiles.Size = new Size(1100, 507);
        gridFiles.TabIndex = 0;
        //
        // tabLayout
        //
        tabLayout.Controls.Add(gridLayout);
        tabLayout.Location = new Point(4, 29);
        tabLayout.Name = "tabLayout";
        tabLayout.Padding = new Padding(6);
        tabLayout.Size = new Size(1112, 519);
        tabLayout.TabIndex = 3;
        tabLayout.Text = "Mise en page";
        tabLayout.UseVisualStyleBackColor = true;
        //
        // gridLayout
        //
        gridLayout.AllowUserToAddRows = false;
        gridLayout.AllowUserToDeleteRows = false;
        gridLayout.AllowUserToResizeRows = false;
        gridLayout.BackgroundColor = SystemColors.Window;
        gridLayout.BorderStyle = BorderStyle.None;
        gridLayout.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        gridLayout.Dock = DockStyle.Fill;
        gridLayout.EditMode = DataGridViewEditMode.EditProgrammatically;
        gridLayout.Location = new Point(6, 6);
        gridLayout.Name = "gridLayout";
        gridLayout.ReadOnly = true;
        gridLayout.RowHeadersVisible = false;
        gridLayout.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridLayout.Size = new Size(1100, 507);
        gridLayout.TabIndex = 0;
        //
        // bottomPanel
        //
        bottomPanel.Controls.Add(lblGroupLanguage);
        bottomPanel.Controls.Add(cmbGroupLanguage);
        bottomPanel.Controls.Add(lblHint);
        bottomPanel.Controls.Add(btnCopy);
        bottomPanel.Controls.Add(btnClose);
        bottomPanel.Dock = DockStyle.Bottom;
        bottomPanel.Location = new Point(0, 656);
        bottomPanel.Name = "bottomPanel";
        bottomPanel.Padding = new Padding(10, 8, 10, 8);
        bottomPanel.Size = new Size(1120, 48);
        bottomPanel.TabIndex = 2;
        //
        // lblGroupLanguage
        //
        lblGroupLanguage.AutoSize = true;
        lblGroupLanguage.Location = new Point(12, 14);
        lblGroupLanguage.Name = "lblGroupLanguage";
        lblGroupLanguage.Size = new Size(180, 20);
        lblGroupLanguage.TabIndex = 0;
        lblGroupLanguage.Text = "Projets / fichiers pour :";
        //
        // cmbGroupLanguage
        //
        cmbGroupLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbGroupLanguage.Location = new Point(198, 10);
        cmbGroupLanguage.Name = "cmbGroupLanguage";
        cmbGroupLanguage.Size = new Size(180, 28);
        cmbGroupLanguage.TabIndex = 1;
        //
        // lblHint
        //
        lblHint.AutoSize = true;
        lblHint.ForeColor = SystemColors.GrayText;
        lblHint.Location = new Point(396, 14);
        lblHint.Name = "lblHint";
        lblHint.Size = new Size(300, 20);
        lblHint.TabIndex = 2;
        lblHint.Text = "Cliquez un chiffre souligné pour filtrer la grille.";
        //
        // btnCopy
        //
        btnCopy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCopy.Location = new Point(898, 9);
        btnCopy.Name = "btnCopy";
        btnCopy.Size = new Size(100, 30);
        btnCopy.TabIndex = 3;
        btnCopy.Text = "Copier";
        btnCopy.UseVisualStyleBackColor = true;
        //
        // btnClose
        //
        btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClose.DialogResult = DialogResult.Cancel;
        btnClose.Location = new Point(1006, 9);
        btnClose.Name = "btnClose";
        btnClose.Size = new Size(100, 30);
        btnClose.TabIndex = 4;
        btnClose.Text = "Fermer";
        btnClose.UseVisualStyleBackColor = true;
        //
        // DashboardForm
        //
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnClose;
        ClientSize = new Size(1120, 704);
        Controls.Add(tabs);
        Controls.Add(summaryPanel);
        Controls.Add(bottomPanel);
        MinimizeBox = false;
        MinimumSize = new Size(720, 420);
        Name = "DashboardForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "État des traductions";
        summaryPanel.ResumeLayout(false);
        tabs.ResumeLayout(false);
        tabLanguages.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)gridLanguages).EndInit();
        tabProjects.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)gridProjects).EndInit();
        tabFiles.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)gridFiles).EndInit();
        tabLayout.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)gridLayout).EndInit();
        bottomPanel.ResumeLayout(false);
        bottomPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private FlowLayoutPanel summaryPanel;
    private TabControl tabs;
    private TabPage tabLanguages;
    private DataGridView gridLanguages;
    private TabPage tabProjects;
    private DataGridView gridProjects;
    private TabPage tabFiles;
    private DataGridView gridFiles;
    private TabPage tabLayout;
    private DataGridView gridLayout;
    private Panel bottomPanel;
    private Label lblGroupLanguage;
    private ComboBox cmbGroupLanguage;
    private Label lblHint;
    private Button btnCopy;
    private Button btnClose;
}
