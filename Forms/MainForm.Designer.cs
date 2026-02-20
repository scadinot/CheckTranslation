namespace CheckTranslation;

partial class MainForm
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        dataGridView = new DataGridView();
        colFrench = new DataGridViewTextBoxColumn();
        colTranslation = new DataGridViewTextBoxColumn();
        toolStrip = new ToolStrip();
        btnOpen = new ToolStripButton();
        btnSave = new ToolStripButton();
        btnConfig = new ToolStripButton();
        statusStrip = new StatusStrip();
        statusProgressBar = new ToolStripProgressBar();
        statusFileName = new ToolStripStatusLabel();
        statusRowCount = new ToolStripStatusLabel();
        statusLanguage = new ToolStripStatusLabel();
        ((System.ComponentModel.ISupportInitialize)dataGridView).BeginInit();
        toolStrip.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // dataGridView
        // 
        dataGridView.AllowUserToAddRows = false;
        dataGridView.AllowUserToDeleteRows = false;
        dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
        dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dataGridView.Columns.AddRange(new DataGridViewColumn[] { colFrench, colTranslation });
        dataGridView.Dock = DockStyle.Fill;
        dataGridView.Location = new Point(0, 25);
        dataGridView.Name = "dataGridView";
        dataGridView.RowHeadersVisible = false;
        dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dataGridView.Size = new Size(1200, 603);
        dataGridView.TabIndex = 0;
        // 
        // colFrench
        // 
        colFrench.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        colFrench.DataPropertyName = "French";
        colFrench.FillWeight = 50F;
        colFrench.HeaderText = "Francais";
        colFrench.Name = "colFrench";
        colFrench.ReadOnly = true;
        // 
        // colTranslation
        // 
        colTranslation.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        colTranslation.DataPropertyName = "Translation";
        colTranslation.FillWeight = 50F;
        colTranslation.HeaderText = "Traduction";
        colTranslation.Name = "colTranslation";
        // 
        // toolStrip
        // 
        toolStrip.ImageScalingSize = new Size(24, 24);
        toolStrip.Items.AddRange(new ToolStripItem[] { btnOpen, btnSave, btnConfig });
        toolStrip.Location = new Point(0, 0);
        toolStrip.Name = "toolStrip";
        toolStrip.Size = new Size(1200, 25);
        toolStrip.TabIndex = 2;
        // 
        // btnOpen
        // 
        btnOpen.DisplayStyle = ToolStripItemDisplayStyle.Image;
        btnOpen.Name = "btnOpen";
        btnOpen.Size = new Size(23, 22);
        btnOpen.ToolTipText = "Ouvrir";
        // 
        // btnSave
        // 
        btnSave.DisplayStyle = ToolStripItemDisplayStyle.Image;
        btnSave.Enabled = false;
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(23, 22);
        btnSave.ToolTipText = "Sauvegarder";
        // 
        // btnConfig
        // 
        btnConfig.DisplayStyle = ToolStripItemDisplayStyle.Image;
        btnConfig.Name = "btnConfig";
        btnConfig.Size = new Size(23, 22);
        btnConfig.ToolTipText = "Configuration";
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
        statusFileName.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Bottom;
        statusFileName.BorderStyle = Border3DStyle.SunkenOuter;
        statusFileName.Name = "statusFileName";
        statusFileName.Size = new Size(4, 17);
        statusFileName.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // statusRowCount
        // 
        statusRowCount.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Bottom;
        statusRowCount.BorderStyle = Border3DStyle.SunkenOuter;
        statusRowCount.Name = "statusRowCount";
        statusRowCount.Size = new Size(4, 17);
        statusRowCount.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // statusLanguage
        // 
        statusLanguage.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Bottom;
        statusLanguage.BorderStyle = Border3DStyle.SunkenOuter;
        statusLanguage.Name = "statusLanguage";
        statusLanguage.Size = new Size(4, 17);
        statusLanguage.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 650);
        Controls.Add(dataGridView);
        Controls.Add(statusStrip);
        Controls.Add(toolStrip);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CheckTranslation - Contrôle des traductions";
        ((System.ComponentModel.ISupportInitialize)dataGridView).EndInit();
        toolStrip.ResumeLayout(false);
        toolStrip.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private DataGridView dataGridView;
    private DataGridViewTextBoxColumn colFrench;
    private DataGridViewTextBoxColumn colTranslation;
    private ToolStrip toolStrip;
    private ToolStripButton btnOpen;
    private ToolStripButton btnSave;
    private readonly List<ToolStripButton> _languageButtons = new();
    private ToolStripButton btnConfig;
    private StatusStrip statusStrip;
    private ToolStripProgressBar statusProgressBar;
    private ToolStripStatusLabel statusFileName;
    private ToolStripStatusLabel statusRowCount;
    private ToolStripStatusLabel statusLanguage;
}
