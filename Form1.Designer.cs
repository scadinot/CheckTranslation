namespace CheckTranslation;

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
        colFrench = new DataGridViewTextBoxColumn();
        colTranslation = new DataGridViewTextBoxColumn();
        toolStrip = new ToolStrip();
        btnOpen = new ToolStripButton();
        btnSave = new ToolStripButton();
        btnLanguage = new ToolStripDropDownButton();
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
            colFrench,
            colTranslation
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
        // colTranslation
        //
        colTranslation.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        colTranslation.DataPropertyName = "Translation";
        colTranslation.DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True };
        colTranslation.FillWeight = 50;
        colTranslation.HeaderText = "Traduction";
        colTranslation.Name = "colTranslation";
        colTranslation.ReadOnly = false;
        //
        // toolStrip
        //
        toolStrip.ImageScalingSize = new Size(24, 24);
        toolStrip.Items.AddRange(new ToolStripItem[] { btnOpen, btnSave, new ToolStripSeparator(), btnLanguage });
        toolStrip.Location = new Point(0, 0);
        toolStrip.Name = "toolStrip";
        toolStrip.Size = new Size(1200, 31);
        toolStrip.TabIndex = 2;
        //
        // btnOpen
        //
        btnOpen.DisplayStyle = ToolStripItemDisplayStyle.Image;
        btnOpen.Name = "btnOpen";
        btnOpen.ToolTipText = "Ouvrir";
        //
        // btnSave
        //
        btnSave.DisplayStyle = ToolStripItemDisplayStyle.Image;
        btnSave.Enabled = false;
        btnSave.Name = "btnSave";
        btnSave.ToolTipText = "Sauvegarder";
        //
        // btnLanguage
        //
        btnLanguage.DisplayStyle = ToolStripItemDisplayStyle.Image;
        btnLanguage.Enabled = false;
        btnLanguage.Name = "btnLanguage";
        btnLanguage.ToolTipText = "Langue à contrôler";
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
        Text = "CheckTranslation - Contrôle des traductions";
        ((System.ComponentModel.ISupportInitialize)dataGridView).EndInit();
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
    private ToolStripDropDownButton btnLanguage;
    private StatusStrip statusStrip;
    private ToolStripProgressBar statusProgressBar;
    private ToolStripStatusLabel statusFileName;
    private ToolStripStatusLabel statusRowCount;
    private ToolStripStatusLabel statusLanguage;
}
