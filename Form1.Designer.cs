namespace CheckTransation
{
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
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)dataGridView).BeginInit();
            statusStrip.SuspendLayout();
            SuspendLayout();
            //
            // dataGridView
            //
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
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
            dataGridView.ReadOnly = true;
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
            colProject.Width = 140;
            //
            // colFile
            //
            colFile.DataPropertyName = "File";
            colFile.HeaderText = "Fichier";
            colFile.Name = "colFile";
            colFile.ReadOnly = true;
            colFile.Width = 140;
            //
            // colKey
            //
            colKey.DataPropertyName = "Key";
            colKey.HeaderText = "Cle";
            colKey.Name = "colKey";
            colKey.ReadOnly = true;
            colKey.Width = 200;
            //
            // colFrench
            //
            colFrench.DataPropertyName = "French";
            colFrench.DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True };
            colFrench.HeaderText = "Francais";
            colFrench.Name = "colFrench";
            colFrench.ReadOnly = true;
            colFrench.Width = 350;
            //
            // colGerman
            //
            colGerman.DataPropertyName = "German";
            colGerman.DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True };
            colGerman.HeaderText = "Allemand";
            colGerman.Name = "colGerman";
            colGerman.ReadOnly = true;
            colGerman.Width = 350;
            //
            // statusStrip
            //
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
            statusStrip.Location = new Point(0, 628);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1200, 22);
            statusStrip.TabIndex = 1;
            //
            // statusLabel
            //
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(0, 17);
            statusLabel.Text = "";
            //
            // Form1
            //
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 650);
            Controls.Add(dataGridView);
            Controls.Add(statusStrip);
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
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
    }
}
