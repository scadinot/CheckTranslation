namespace CheckTranslation;

partial class ConfigForm
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
        grpAuth = new GroupBox();
        lblKey = new Label();
        txtKey = new TextBox();
        lblUrl = new Label();
        txtUrl = new TextBox();
        lblModelName = new Label();
        txtModelName = new TextBox();
        lblPrompt = new Label();
        txtTranslatePrompt = new TextBox();
        lblVerifyPrompt = new Label();
        txtVerifyPrompt = new TextBox();
        btnOk = new Button();
        btnCancel = new Button();
        splitContainer1 = new SplitContainer();
        grpAuth.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
        splitContainer1.Panel1.SuspendLayout();
        splitContainer1.Panel2.SuspendLayout();
        splitContainer1.SuspendLayout();
        SuspendLayout();
        // 
        // grpAuth
        // 
        grpAuth.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        grpAuth.Controls.Add(lblKey);
        grpAuth.Controls.Add(txtKey);
        grpAuth.Controls.Add(lblUrl);
        grpAuth.Controls.Add(txtUrl);
        grpAuth.Controls.Add(lblModelName);
        grpAuth.Controls.Add(txtModelName);
        grpAuth.Location = new Point(12, 275);
        grpAuth.Name = "grpAuth";
        grpAuth.Size = new Size(458, 126);
        grpAuth.TabIndex = 0;
        grpAuth.TabStop = false;
        grpAuth.Text = "Authentification";
        // 
        // lblKey
        // 
        lblKey.AutoSize = true;
        lblKey.Location = new Point(6, 23);
        lblKey.Name = "lblKey";
        lblKey.Size = new Size(40, 20);
        lblKey.TabIndex = 1;
        lblKey.Text = "Key :";
        // 
        // txtKey
        // 
        txtKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtKey.Location = new Point(112, 23);
        txtKey.Name = "txtKey";
        txtKey.Size = new Size(330, 27);
        txtKey.TabIndex = 1;
        txtKey.UseSystemPasswordChar = true;
        // 
        // lblUrl
        // 
        lblUrl.AutoSize = true;
        lblUrl.Location = new Point(6, 59);
        lblUrl.Name = "lblUrl";
        lblUrl.Size = new Size(35, 20);
        lblUrl.TabIndex = 2;
        lblUrl.Text = "Url :";
        // 
        // txtUrl
        // 
        txtUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtUrl.Location = new Point(112, 56);
        txtUrl.Name = "txtUrl";
        txtUrl.Size = new Size(330, 27);
        txtUrl.TabIndex = 2;
        // 
        // lblModelName
        // 
        lblModelName.AutoSize = true;
        lblModelName.Location = new Point(6, 92);
        lblModelName.Name = "lblModelName";
        lblModelName.Size = new Size(100, 20);
        lblModelName.TabIndex = 3;
        lblModelName.Text = "Model name :";
        // 
        // txtModelName
        // 
        txtModelName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtModelName.Location = new Point(112, 89);
        txtModelName.Name = "txtModelName";
        txtModelName.Size = new Size(330, 27);
        txtModelName.TabIndex = 3;
        // 
        // lblPrompt
        // 
        lblPrompt.AutoSize = true;
        lblPrompt.Location = new Point(6, 20);
        lblPrompt.Name = "lblPrompt";
        lblPrompt.Size = new Size(86, 20);
        lblPrompt.TabIndex = 0;
        lblPrompt.Text = "Traduction :";
        // 
        // txtTranslatePrompt
        // 
        txtTranslatePrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtTranslatePrompt.Location = new Point(112, 17);
        txtTranslatePrompt.Multiline = true;
        txtTranslatePrompt.Name = "txtTranslatePrompt";
        txtTranslatePrompt.ScrollBars = ScrollBars.Vertical;
        txtTranslatePrompt.Size = new Size(330, 100);
        txtTranslatePrompt.TabIndex = 0;
        // 
        // lblVerifyPrompt
        // 
        lblVerifyPrompt.AutoSize = true;
        lblVerifyPrompt.Location = new Point(6, 19);
        lblVerifyPrompt.Name = "lblVerifyPrompt";
        lblVerifyPrompt.Size = new Size(91, 20);
        lblVerifyPrompt.TabIndex = 6;
        lblVerifyPrompt.Text = "Vérification :";
        // 
        // txtVerifyPrompt
        // 
        txtVerifyPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtVerifyPrompt.Location = new Point(112, 16);
        txtVerifyPrompt.Multiline = true;
        txtVerifyPrompt.Name = "txtVerifyPrompt";
        txtVerifyPrompt.ScrollBars = ScrollBars.Vertical;
        txtVerifyPrompt.Size = new Size(330, 100);
        txtVerifyPrompt.TabIndex = 1;
        // 
        // btnOk
        // 
        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new Point(300, 413);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(80, 28);
        btnOk.TabIndex = 4;
        btnOk.Text = "OK";
        // 
        // btnCancel
        // 
        btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(390, 413);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(80, 28);
        btnCancel.TabIndex = 5;
        btnCancel.Text = "Annuler";
        // 
        // splitContainer1
        // 
        splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        splitContainer1.Location = new Point(12, 12);
        splitContainer1.Name = "splitContainer1";
        splitContainer1.Orientation = Orientation.Horizontal;
        // 
        // splitContainer1.Panel1
        // 
        splitContainer1.Panel1.Controls.Add(txtTranslatePrompt);
        splitContainer1.Panel1.Controls.Add(lblPrompt);
        // 
        // splitContainer1.Panel2
        // 
        splitContainer1.Panel2.Controls.Add(txtVerifyPrompt);
        splitContainer1.Panel2.Controls.Add(lblVerifyPrompt);
        splitContainer1.Size = new Size(458, 257);
        splitContainer1.SplitterDistance = 127;
        splitContainer1.TabIndex = 7;
        // 
        // ConfigForm
        // 
        AcceptButton = btnOk;
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(482, 453);
        Controls.Add(splitContainer1);
        Controls.Add(grpAuth);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(500, 500);
        Name = "ConfigForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Configuration";
        grpAuth.ResumeLayout(false);
        grpAuth.PerformLayout();
        splitContainer1.Panel1.ResumeLayout(false);
        splitContainer1.Panel1.PerformLayout();
        splitContainer1.Panel2.ResumeLayout(false);
        splitContainer1.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
        splitContainer1.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private GroupBox grpAuth;
    private Label lblPrompt;
    private TextBox txtTranslatePrompt;
    private Label lblVerifyPrompt;
    private TextBox txtVerifyPrompt;
    private Label lblKey;
    private TextBox txtKey;
    private Label lblUrl;
    private TextBox txtUrl;
    private Label lblModelName;
    private TextBox txtModelName;
    private Button btnOk;
    private Button btnCancel;
    private SplitContainer splitContainer1;
}
