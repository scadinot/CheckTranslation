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
        grpAuth.Location = new Point(10, 206);
        grpAuth.Margin = new Padding(3, 2, 3, 2);
        grpAuth.Name = "grpAuth";
        grpAuth.Padding = new Padding(3, 2, 3, 2);
        grpAuth.Size = new Size(401, 94);
        grpAuth.TabIndex = 0;
        grpAuth.TabStop = false;
        grpAuth.Text = "Authentification";
        // 
        // lblKey
        // 
        lblKey.AutoSize = true;
        lblKey.Location = new Point(5, 17);
        lblKey.Name = "lblKey";
        lblKey.Size = new Size(32, 15);
        lblKey.TabIndex = 1;
        lblKey.Text = "Key :";
        // 
        // txtKey
        // 
        txtKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtKey.Location = new Point(98, 17);
        txtKey.Margin = new Padding(3, 2, 3, 2);
        txtKey.Name = "txtKey";
        txtKey.Size = new Size(289, 23);
        txtKey.TabIndex = 1;
        txtKey.UseSystemPasswordChar = true;
        // 
        // lblUrl
        // 
        lblUrl.AutoSize = true;
        lblUrl.Location = new Point(5, 44);
        lblUrl.Name = "lblUrl";
        lblUrl.Size = new Size(28, 15);
        lblUrl.TabIndex = 2;
        lblUrl.Text = "Url :";
        // 
        // txtUrl
        // 
        txtUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtUrl.Location = new Point(98, 42);
        txtUrl.Margin = new Padding(3, 2, 3, 2);
        txtUrl.Name = "txtUrl";
        txtUrl.Size = new Size(289, 23);
        txtUrl.TabIndex = 2;
        // 
        // lblModelName
        // 
        lblModelName.AutoSize = true;
        lblModelName.Location = new Point(5, 69);
        lblModelName.Name = "lblModelName";
        lblModelName.Size = new Size(80, 15);
        lblModelName.TabIndex = 3;
        lblModelName.Text = "Model name :";
        // 
        // txtModelName
        // 
        txtModelName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtModelName.Location = new Point(98, 67);
        txtModelName.Margin = new Padding(3, 2, 3, 2);
        txtModelName.Name = "txtModelName";
        txtModelName.Size = new Size(289, 23);
        txtModelName.TabIndex = 3;
        // 
        // lblPrompt
        // 
        lblPrompt.AutoSize = true;
        lblPrompt.Location = new Point(5, 15);
        lblPrompt.Name = "lblPrompt";
        lblPrompt.Size = new Size(70, 15);
        lblPrompt.TabIndex = 0;
        lblPrompt.Text = "Traduction :";
        // 
        // txtTranslatePrompt
        // 
        txtTranslatePrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtTranslatePrompt.Location = new Point(98, 13);
        txtTranslatePrompt.Margin = new Padding(3, 2, 3, 2);
        txtTranslatePrompt.Multiline = true;
        txtTranslatePrompt.Name = "txtTranslatePrompt";
        txtTranslatePrompt.ScrollBars = ScrollBars.Vertical;
        txtTranslatePrompt.Size = new Size(289, 76);
        txtTranslatePrompt.TabIndex = 0;
        // 
        // lblVerifyPrompt
        // 
        lblVerifyPrompt.AutoSize = true;
        lblVerifyPrompt.Location = new Point(5, 14);
        lblVerifyPrompt.Name = "lblVerifyPrompt";
        lblVerifyPrompt.Size = new Size(72, 15);
        lblVerifyPrompt.TabIndex = 6;
        lblVerifyPrompt.Text = "Vérification :";
        // 
        // txtVerifyPrompt
        // 
        txtVerifyPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtVerifyPrompt.Location = new Point(98, 12);
        txtVerifyPrompt.Margin = new Padding(3, 2, 3, 2);
        txtVerifyPrompt.Multiline = true;
        txtVerifyPrompt.Name = "txtVerifyPrompt";
        txtVerifyPrompt.ScrollBars = ScrollBars.Vertical;
        txtVerifyPrompt.Size = new Size(289, 77);
        txtVerifyPrompt.TabIndex = 1;
        // 
        // btnOk
        // 
        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new Point(262, 310);
        btnOk.Margin = new Padding(3, 2, 3, 2);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(70, 21);
        btnOk.TabIndex = 4;
        btnOk.Text = "OK";
        // 
        // btnCancel
        // 
        btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(341, 310);
        btnCancel.Margin = new Padding(3, 2, 3, 2);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(70, 21);
        btnCancel.TabIndex = 5;
        btnCancel.Text = "Annuler";
        // 
        // splitContainer1
        // 
        splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        splitContainer1.Location = new Point(10, 9);
        splitContainer1.Margin = new Padding(3, 2, 3, 2);
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
        splitContainer1.Size = new Size(401, 193);
        splitContainer1.SplitterDistance = 95;
        splitContainer1.SplitterWidth = 3;
        splitContainer1.TabIndex = 7;
        // 
        // ConfigForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(424, 346);
        Controls.Add(splitContainer1);
        Controls.Add(grpAuth);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        Margin = new Padding(3, 2, 3, 2);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(440, 385);
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
