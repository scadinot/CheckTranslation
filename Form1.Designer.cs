namespace CheckTransation;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        dataGridView = new DataGridView();
        colProject = new DataGridViewTextBoxColumn();
        colFile = new DataGridViewTextBoxColumn();
        colKey = new DataGridViewTextBoxColumn();
        colFrench = new DataGridViewTextBoxColumn();
        colGerman = new DataGridViewTextBoxColumn();
        toolStrip = new ToolStrip();
        btnOpen = new ToolStripButton();
        btnSave = new ToolStripButton();
        statusStrip = new StatusStrip();
        statusProgressBar = new ToolStripProgressBar();
        statusFileName = new ToolStripStatusLabel();
        statusRowCount = new ToolStripStatusLabel();
        statusLanguage = new ToolStripStatusLabel();
        ((System.ComponentModel.ISupportInitialize)dataGridView).BeginInit();
        statusStrip.SuspendLayout();
        SuspendLayout();
        //
        // dataGridView
        //
        dataGridView.AllowUserToAddRows = false;
        dataGridView.AllowUserToDeleteRows = false;
        dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
        dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dataGridView.Columns.AddRange(new DataGridViewColumn[]
        {
            colProject,
            colFile,
            colKey,
            colFrench,
            colGerman
        });
        dataGridView.Dock = DockStyle.Fill;
        dataGridView.Location = new Point(0, 0);
        dataGridView.Name = "dataGridView";
        dataGridView.ReadOnly = false;
        dataGridView.RowHeadersVisible = false;
        dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dataGridView.Size = new Size(1200, 628);
        dataGridView.TabIndex = 0;
        //
        // colProject
        //
        colProject.DataPropertyName = "Project";
        colProject.HeaderText = "Projet";
        colProject.Name = "colProject";
        colProject.ReadOnly = true;
        colProject.Visible = false;
        colProject.Width = 140;
        //
        // colFile
        //
        colFile.DataPropertyName = "File";
        colFile.HeaderText = "Fichier";
        colFile.Name = "colFile";
        colFile.ReadOnly = true;
        colFile.Visible = false;
        colFile.Width = 140;
        //
        // colKey
        //
        colKey.DataPropertyName = "Key";
        colKey.HeaderText = "Clé";
        colKey.Name = "colKey";
        colKey.ReadOnly = true;
        colKey.Visible = false;
        colKey.Width = 200;
        //
        // colFrench
        //
        colFrench.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        colFrench.DataPropertyName = "French";
        colFrench.DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True };
        colFrench.FillWeight = 50;
        colFrench.HeaderText = "Francais";
        colFrench.Name = "colFrench";
        colFrench.ReadOnly = true;
        //
        // colGerman
        //
        colGerman.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        colGerman.DataPropertyName = "German";
        colGerman.DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True };
        colGerman.FillWeight = 50;
        colGerman.HeaderText = "Allemand";
        colGerman.Name = "colGerman";
        colGerman.ReadOnly = false;
        //
        // toolStrip
        //
        toolStrip.Items.AddRange(new ToolStripItem[] { btnOpen, btnSave });
        toolStrip.Location = new Point(0, 0);
        toolStrip.Name = "toolStrip";
        toolStrip.Size = new Size(1200, 25);
        toolStrip.TabIndex = 2;
        //
        // btnOpen
        //
        btnOpen.Image = SystemIcons.Application.ToBitmap();
        btnOpen.ImageTransparentColor = Color.Magenta;
        btnOpen.Name = "btnOpen";
        btnOpen.Size = new Size(60, 22);
        btnOpen.Text = "Ouvrir";
        btnOpen.ToolTipText = "Ouvrir un fichier Excel de traductions";
        //
        // btnSave
        //
        btnSave.Enabled = false;
        btnSave.Image = SystemIcons.Application.ToBitmap();
        btnSave.ImageTransparentColor = Color.Magenta;
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(80, 22);
        btnSave.Text = "Sauvegarder";
        btnSave.ToolTipText = "Sauvegarder les modifications dans le fichier Excel";
        //
        // statusStrip
        //
        statusStrip.Items.AddRange(new ToolStripItem[] { statusProgressBar, statusFileName, statusRowCount, statusLanguage });
        statusStrip.Location = new Point(0, 628);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1200, 22);
        statusStrip.TabIndex = 1;
        //
        // statusProgressBar
        //
        statusProgressBar.Name = "statusProgressBar";
        statusProgressBar.Size = new Size(150, 16);
        statusProgressBar.Visible = false;
        //
        // statusFileName
        //
        statusFileName.BorderSides = ToolStripStatusLabelBorderSides.All;
        statusFileName.BorderStyle = Border3DStyle.SunkenOuter;
        statusFileName.Name = "statusFileName";
        statusFileName.Size = new Size(350, 17);
        statusFileName.Text = "";
        statusFileName.TextAlign = ContentAlignment.MiddleLeft;
        //
        // statusRowCount
        //
        statusRowCount.BorderSides = ToolStripStatusLabelBorderSides.All;
        statusRowCount.BorderStyle = Border3DStyle.SunkenOuter;
        statusRowCount.Name = "statusRowCount";
        statusRowCount.Size = new Size(150, 17);
        statusRowCount.Text = "";
        statusRowCount.TextAlign = ContentAlignment.MiddleLeft;
        //
        // statusLanguage
        //
        statusLanguage.BorderSides = ToolStripStatusLabelBorderSides.All;
        statusLanguage.BorderStyle = Border3DStyle.SunkenOuter;
        statusLanguage.Name = "statusLanguage";
        statusLanguage.Size = new Size(150, 17);
        statusLanguage.Text = "";
        statusLanguage.TextAlign = ContentAlignment.MiddleLeft;
        //
        // Form1
        //
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 650);
        Controls.Add(dataGridView);
        Controls.Add(statusStrip);
        Controls.Add(toolStrip);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CheckTransation - Contrôle des traductions";
        ((System.ComponentModel.ISupportInitialize)dataGridView).EndInit();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private DataGridView dataGridView;
    private DataGridViewTextBoxColumn colProject;
    private DataGridViewTextBoxColumn colFile;
    private DataGridViewTextBoxColumn colKey;
    private DataGridViewTextBoxColumn colFrench;
    private DataGridViewTextBoxColumn colGerman;
    private ToolStrip toolStrip;
    private ToolStripButton btnOpen;
    private ToolStripButton btnSave;
    private StatusStrip statusStrip;
    private ToolStripProgressBar statusProgressBar;
    private ToolStripStatusLabel statusFileName;
    private ToolStripStatusLabel statusRowCount;
    private ToolStripStatusLabel statusLanguage;
}
