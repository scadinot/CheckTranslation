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
        lblPrompt = new Label();
        txtPrompt = new TextBox();
        lblKey = new Label();
        txtKey = new TextBox();
        lblUrl = new Label();
        txtUrl = new TextBox();
        lblModelName = new Label();
        txtModelName = new TextBox();
        btnOk = new Button();
        btnCancel = new Button();
        grpAuth.SuspendLayout();
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
        grpAuth.Location = new Point(12, 163);
        grpAuth.Name = "grpAuth";
        grpAuth.Size = new Size(637, 126);
        grpAuth.TabIndex = 0;
        grpAuth.TabStop = false;
        grpAuth.Text = "Authentification";
        // 
        // lblPrompt
        // 
        lblPrompt.AutoSize = true;
        lblPrompt.Location = new Point(18, 29);
        lblPrompt.Name = "lblPrompt";
        lblPrompt.Size = new Size(65, 20);
        lblPrompt.TabIndex = 0;
        lblPrompt.Text = "Prompt :";
        // 
        // txtPrompt
        // 
        txtPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtPrompt.Location = new Point(124, 26);
        txtPrompt.Multiline = true;
        txtPrompt.Name = "txtPrompt";
        txtPrompt.ScrollBars = ScrollBars.Vertical;
        txtPrompt.Size = new Size(519, 122);
        txtPrompt.TabIndex = 0;
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
        txtKey.Size = new Size(519, 27);
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
        txtUrl.Size = new Size(519, 27);
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
        txtModelName.Size = new Size(519, 27);
        txtModelName.TabIndex = 3;
        // 
        // btnOk
        // 
        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new Point(479, 301);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(80, 28);
        btnOk.TabIndex = 4;
        btnOk.Text = "OK";
        // 
        // btnCancel
        // 
        btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(569, 301);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(80, 28);
        btnCancel.TabIndex = 5;
        btnCancel.Text = "Annuler";
        // 
        // ConfigForm
        // 
        AcceptButton = btnOk;
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(661, 341);
        Controls.Add(lblPrompt);
        Controls.Add(grpAuth);
        Controls.Add(txtPrompt);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ConfigForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Configuration";
        grpAuth.ResumeLayout(false);
        grpAuth.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox grpAuth;
    private Label lblPrompt;
    private TextBox txtPrompt;
    private Label lblKey;
    private TextBox txtKey;
    private Label lblUrl;
    private TextBox txtUrl;
    private Label lblModelName;
    private TextBox txtModelName;
    private Button btnOk;
    private Button btnCancel;
}
