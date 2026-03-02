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
        components = new System.ComponentModel.Container();
        grpAuth = new GroupBox();
        splitContainer2 = new SplitContainer();
        lblKey = new Label();
        rbOpenAi = new RadioButton();
        txtKey = new TextBox();
        txtUrl = new TextBox();
        lblModelName = new Label();
        lblUrl = new Label();
        txtModelName = new TextBox();
        txtAnthropicKey = new TextBox();
        rbAnthropic = new RadioButton();
        lblAnthropicKey = new Label();
        txtAnthropicUrl = new TextBox();
        lblAnthropicModelName = new Label();
        lblAnthropicUrl = new Label();
        txtAnthropicModelName = new TextBox();
        lblPrompt = new Label();
        tabTranslatePrompt = new TabControl();
        tabTranslatePreview = new TabPage();
        webTranslatePreview = new WebBrowser();
        tabTranslateEdit = new TabPage();
        txtTranslatePrompt = new TextBox();
        tabPromptIcons = new ImageList(components);
        lblVerifyPrompt = new Label();
        tabVerifyPrompt = new TabControl();
        tabVerifyPreview = new TabPage();
        webVerifyPreview = new WebBrowser();
        tabVerifyEdit = new TabPage();
        txtVerifyPrompt = new TextBox();
        btnOk = new Button();
        btnCancel = new Button();
        splitContainer1 = new SplitContainer();
        grpAuth.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
        splitContainer2.Panel1.SuspendLayout();
        splitContainer2.Panel2.SuspendLayout();
        splitContainer2.SuspendLayout();
        tabTranslatePrompt.SuspendLayout();
        tabTranslatePreview.SuspendLayout();
        tabTranslateEdit.SuspendLayout();
        tabVerifyPrompt.SuspendLayout();
        tabVerifyPreview.SuspendLayout();
        tabVerifyEdit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
        splitContainer1.Panel1.SuspendLayout();
        splitContainer1.Panel2.SuspendLayout();
        splitContainer1.SuspendLayout();
        SuspendLayout();
        // 
        // grpAuth
        // 
        grpAuth.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        grpAuth.Controls.Add(splitContainer2);
        grpAuth.Location = new Point(10, 590);
        grpAuth.Margin = new Padding(3, 2, 3, 2);
        grpAuth.Name = "grpAuth";
        grpAuth.Padding = new Padding(3, 2, 3, 2);
        grpAuth.Size = new Size(821, 154);
        grpAuth.TabIndex = 0;
        grpAuth.TabStop = false;
        grpAuth.Text = "Paramètres IA";
        // 
        // splitContainer2
        // 
        splitContainer2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        splitContainer2.Location = new Point(6, 21);
        splitContainer2.Name = "splitContainer2";
        // 
        // splitContainer2.Panel1
        // 
        splitContainer2.Panel1.Controls.Add(lblKey);
        splitContainer2.Panel1.Controls.Add(rbOpenAi);
        splitContainer2.Panel1.Controls.Add(txtKey);
        splitContainer2.Panel1.Controls.Add(txtUrl);
        splitContainer2.Panel1.Controls.Add(lblModelName);
        splitContainer2.Panel1.Controls.Add(lblUrl);
        splitContainer2.Panel1.Controls.Add(txtModelName);
        // 
        // splitContainer2.Panel2
        // 
        splitContainer2.Panel2.Controls.Add(txtAnthropicKey);
        splitContainer2.Panel2.Controls.Add(rbAnthropic);
        splitContainer2.Panel2.Controls.Add(lblAnthropicKey);
        splitContainer2.Panel2.Controls.Add(txtAnthropicUrl);
        splitContainer2.Panel2.Controls.Add(lblAnthropicModelName);
        splitContainer2.Panel2.Controls.Add(lblAnthropicUrl);
        splitContainer2.Panel2.Controls.Add(txtAnthropicModelName);
        splitContainer2.Size = new Size(809, 128);
        splitContainer2.SplitterDistance = 403;
        splitContainer2.TabIndex = 8;
        // 
        // lblKey
        // 
        lblKey.AutoSize = true;
        lblKey.Location = new Point(6, 34);
        lblKey.Name = "lblKey";
        lblKey.Size = new Size(32, 15);
        lblKey.TabIndex = 1;
        lblKey.Text = "Key :";
        // 
        // rbOpenAi
        // 
        rbOpenAi.AutoSize = true;
        rbOpenAi.Checked = true;
        rbOpenAi.Location = new Point(10, 7);
        rbOpenAi.Name = "rbOpenAi";
        rbOpenAi.Size = new Size(123, 19);
        rbOpenAi.TabIndex = 0;
        rbOpenAi.TabStop = true;
        rbOpenAi.Text = "OpenAI (ChatGPT)";
        rbOpenAi.UseVisualStyleBackColor = true;
        // 
        // txtKey
        // 
        txtKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtKey.Location = new Point(68, 31);
        txtKey.Margin = new Padding(3, 2, 3, 2);
        txtKey.Name = "txtKey";
        txtKey.Size = new Size(332, 23);
        txtKey.TabIndex = 1;
        txtKey.UseSystemPasswordChar = true;
        // 
        // txtUrl
        // 
        txtUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtUrl.Location = new Point(68, 58);
        txtUrl.Margin = new Padding(3, 2, 3, 2);
        txtUrl.Name = "txtUrl";
        txtUrl.Size = new Size(332, 23);
        txtUrl.TabIndex = 2;
        // 
        // lblModelName
        // 
        lblModelName.AutoSize = true;
        lblModelName.Location = new Point(6, 88);
        lblModelName.Name = "lblModelName";
        lblModelName.Size = new Size(47, 15);
        lblModelName.TabIndex = 4;
        lblModelName.Text = "Model :";
        // 
        // lblUrl
        // 
        lblUrl.AutoSize = true;
        lblUrl.Location = new Point(6, 61);
        lblUrl.Name = "lblUrl";
        lblUrl.Size = new Size(28, 15);
        lblUrl.TabIndex = 2;
        lblUrl.Text = "Url :";
        // 
        // txtModelName
        // 
        txtModelName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtModelName.Location = new Point(68, 85);
        txtModelName.Margin = new Padding(3, 2, 3, 2);
        txtModelName.Name = "txtModelName";
        txtModelName.Size = new Size(332, 23);
        txtModelName.TabIndex = 4;
        // 
        // txtAnthropicKey
        // 
        txtAnthropicKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtAnthropicKey.Location = new Point(68, 31);
        txtAnthropicKey.Margin = new Padding(3, 2, 3, 2);
        txtAnthropicKey.Name = "txtAnthropicKey";
        txtAnthropicKey.Size = new Size(331, 23);
        txtAnthropicKey.TabIndex = 5;
        txtAnthropicKey.UseSystemPasswordChar = true;
        // 
        // rbAnthropic
        // 
        rbAnthropic.AutoSize = true;
        rbAnthropic.Location = new Point(10, 7);
        rbAnthropic.Name = "rbAnthropic";
        rbAnthropic.Size = new Size(126, 19);
        rbAnthropic.TabIndex = 0;
        rbAnthropic.Text = "Anthropic (Claude)";
        rbAnthropic.UseVisualStyleBackColor = true;
        // 
        // lblAnthropicKey
        // 
        lblAnthropicKey.AutoSize = true;
        lblAnthropicKey.Location = new Point(6, 34);
        lblAnthropicKey.Name = "lblAnthropicKey";
        lblAnthropicKey.Size = new Size(32, 15);
        lblAnthropicKey.TabIndex = 5;
        lblAnthropicKey.Text = "Key :";
        // 
        // txtAnthropicUrl
        // 
        txtAnthropicUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtAnthropicUrl.Location = new Point(68, 60);
        txtAnthropicUrl.Margin = new Padding(3, 2, 3, 2);
        txtAnthropicUrl.Name = "txtAnthropicUrl";
        txtAnthropicUrl.Size = new Size(330, 23);
        txtAnthropicUrl.TabIndex = 6;
        // 
        // lblAnthropicModelName
        // 
        lblAnthropicModelName.AutoSize = true;
        lblAnthropicModelName.Location = new Point(6, 88);
        lblAnthropicModelName.Name = "lblAnthropicModelName";
        lblAnthropicModelName.Size = new Size(47, 15);
        lblAnthropicModelName.TabIndex = 7;
        lblAnthropicModelName.Text = "Model :";
        // 
        // lblAnthropicUrl
        // 
        lblAnthropicUrl.AutoSize = true;
        lblAnthropicUrl.Location = new Point(6, 61);
        lblAnthropicUrl.Name = "lblAnthropicUrl";
        lblAnthropicUrl.Size = new Size(28, 15);
        lblAnthropicUrl.TabIndex = 6;
        lblAnthropicUrl.Text = "Url :";
        // 
        // txtAnthropicModelName
        // 
        txtAnthropicModelName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtAnthropicModelName.Location = new Point(68, 87);
        txtAnthropicModelName.Margin = new Padding(3, 2, 3, 2);
        txtAnthropicModelName.Name = "txtAnthropicModelName";
        txtAnthropicModelName.Size = new Size(330, 23);
        txtAnthropicModelName.TabIndex = 7;
        // 
        // lblPrompt
        // 
        lblPrompt.AutoSize = true;
        lblPrompt.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblPrompt.Location = new Point(5, 10);
        lblPrompt.Name = "lblPrompt";
        lblPrompt.Size = new Size(72, 15);
        lblPrompt.TabIndex = 0;
        lblPrompt.Text = "Traduction :";
        // 
        // tabTranslatePrompt
        // 
        tabTranslatePrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        tabTranslatePrompt.Controls.Add(tabTranslatePreview);
        tabTranslatePrompt.Controls.Add(tabTranslateEdit);
        tabTranslatePrompt.ImageList = tabPromptIcons;
        tabTranslatePrompt.Location = new Point(5, 27);
        tabTranslatePrompt.Margin = new Padding(3, 2, 3, 2);
        tabTranslatePrompt.Name = "tabTranslatePrompt";
        tabTranslatePrompt.SelectedIndex = 0;
        tabTranslatePrompt.Size = new Size(816, 262);
        tabTranslatePrompt.TabIndex = 0;
        // 
        // tabTranslatePreview
        // 
        tabTranslatePreview.Controls.Add(webTranslatePreview);
        tabTranslatePreview.ImageKey = "preview";
        tabTranslatePreview.Location = new Point(4, 24);
        tabTranslatePreview.Margin = new Padding(3, 2, 3, 2);
        tabTranslatePreview.Name = "tabTranslatePreview";
        tabTranslatePreview.Padding = new Padding(3);
        tabTranslatePreview.Size = new Size(808, 234);
        tabTranslatePreview.TabIndex = 0;
        tabTranslatePreview.Text = "Aperçu";
        tabTranslatePreview.UseVisualStyleBackColor = true;
        // 
        // webTranslatePreview
        // 
        webTranslatePreview.Dock = DockStyle.Fill;
        webTranslatePreview.Location = new Point(3, 3);
        webTranslatePreview.Margin = new Padding(3, 2, 3, 2);
        webTranslatePreview.MinimumSize = new Size(20, 20);
        webTranslatePreview.Name = "webTranslatePreview";
        webTranslatePreview.Size = new Size(802, 228);
        webTranslatePreview.TabIndex = 0;
        // 
        // tabTranslateEdit
        // 
        tabTranslateEdit.Controls.Add(txtTranslatePrompt);
        tabTranslateEdit.ImageKey = "edit";
        tabTranslateEdit.Location = new Point(4, 24);
        tabTranslateEdit.Margin = new Padding(3, 2, 3, 2);
        tabTranslateEdit.Name = "tabTranslateEdit";
        tabTranslateEdit.Padding = new Padding(3);
        tabTranslateEdit.Size = new Size(573, 173);
        tabTranslateEdit.TabIndex = 1;
        tabTranslateEdit.Text = "Édition";
        tabTranslateEdit.UseVisualStyleBackColor = true;
        // 
        // txtTranslatePrompt
        // 
        txtTranslatePrompt.Dock = DockStyle.Fill;
        txtTranslatePrompt.Location = new Point(3, 3);
        txtTranslatePrompt.Margin = new Padding(3, 2, 3, 2);
        txtTranslatePrompt.Multiline = true;
        txtTranslatePrompt.Name = "txtTranslatePrompt";
        txtTranslatePrompt.ScrollBars = ScrollBars.Vertical;
        txtTranslatePrompt.Size = new Size(567, 167);
        txtTranslatePrompt.TabIndex = 0;
        // 
        // tabPromptIcons
        // 
        tabPromptIcons.ColorDepth = ColorDepth.Depth32Bit;
        tabPromptIcons.ImageSize = new Size(16, 16);
        tabPromptIcons.TransparentColor = Color.Transparent;
        // 
        // lblVerifyPrompt
        // 
        lblVerifyPrompt.AutoSize = true;
        lblVerifyPrompt.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblVerifyPrompt.Location = new Point(5, 10);
        lblVerifyPrompt.Name = "lblVerifyPrompt";
        lblVerifyPrompt.Size = new Size(77, 15);
        lblVerifyPrompt.TabIndex = 6;
        lblVerifyPrompt.Text = "Vérification :";
        // 
        // tabVerifyPrompt
        // 
        tabVerifyPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        tabVerifyPrompt.Controls.Add(tabVerifyPreview);
        tabVerifyPrompt.Controls.Add(tabVerifyEdit);
        tabVerifyPrompt.ImageList = tabPromptIcons;
        tabVerifyPrompt.Location = new Point(5, 27);
        tabVerifyPrompt.Margin = new Padding(3, 2, 3, 2);
        tabVerifyPrompt.Name = "tabVerifyPrompt";
        tabVerifyPrompt.SelectedIndex = 0;
        tabVerifyPrompt.Size = new Size(816, 256);
        tabVerifyPrompt.TabIndex = 1;
        // 
        // tabVerifyPreview
        // 
        tabVerifyPreview.Controls.Add(webVerifyPreview);
        tabVerifyPreview.ImageKey = "preview";
        tabVerifyPreview.Location = new Point(4, 24);
        tabVerifyPreview.Margin = new Padding(3, 2, 3, 2);
        tabVerifyPreview.Name = "tabVerifyPreview";
        tabVerifyPreview.Padding = new Padding(3);
        tabVerifyPreview.Size = new Size(808, 228);
        tabVerifyPreview.TabIndex = 0;
        tabVerifyPreview.Text = "Aperçu";
        tabVerifyPreview.UseVisualStyleBackColor = true;
        // 
        // webVerifyPreview
        // 
        webVerifyPreview.Dock = DockStyle.Fill;
        webVerifyPreview.Location = new Point(3, 3);
        webVerifyPreview.Margin = new Padding(3, 2, 3, 2);
        webVerifyPreview.MinimumSize = new Size(20, 20);
        webVerifyPreview.Name = "webVerifyPreview";
        webVerifyPreview.Size = new Size(802, 222);
        webVerifyPreview.TabIndex = 0;
        // 
        // tabVerifyEdit
        // 
        tabVerifyEdit.Controls.Add(txtVerifyPrompt);
        tabVerifyEdit.ImageKey = "edit";
        tabVerifyEdit.Location = new Point(4, 24);
        tabVerifyEdit.Margin = new Padding(3, 2, 3, 2);
        tabVerifyEdit.Name = "tabVerifyEdit";
        tabVerifyEdit.Padding = new Padding(3);
        tabVerifyEdit.Size = new Size(573, 169);
        tabVerifyEdit.TabIndex = 1;
        tabVerifyEdit.Text = "Édition";
        tabVerifyEdit.UseVisualStyleBackColor = true;
        // 
        // txtVerifyPrompt
        // 
        txtVerifyPrompt.Dock = DockStyle.Fill;
        txtVerifyPrompt.Location = new Point(3, 3);
        txtVerifyPrompt.Margin = new Padding(3, 2, 3, 2);
        txtVerifyPrompt.Multiline = true;
        txtVerifyPrompt.Name = "txtVerifyPrompt";
        txtVerifyPrompt.ScrollBars = ScrollBars.Vertical;
        txtVerifyPrompt.Size = new Size(567, 163);
        txtVerifyPrompt.TabIndex = 1;
        // 
        // btnOk
        // 
        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new Point(682, 748);
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
        btnCancel.Location = new Point(761, 748);
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
        splitContainer1.Panel1.Controls.Add(tabTranslatePrompt);
        splitContainer1.Panel1.Controls.Add(lblPrompt);
        // 
        // splitContainer1.Panel2
        // 
        splitContainer1.Panel2.Controls.Add(tabVerifyPrompt);
        splitContainer1.Panel2.Controls.Add(lblVerifyPrompt);
        splitContainer1.Size = new Size(821, 577);
        splitContainer1.SplitterDistance = 291;
        splitContainer1.SplitterWidth = 3;
        splitContainer1.TabIndex = 7;
        // 
        // ConfigForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(844, 784);
        Controls.Add(splitContainer1);
        Controls.Add(grpAuth);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        Margin = new Padding(3, 2, 3, 2);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(600, 550);
        Name = "ConfigForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Configuration";
        grpAuth.ResumeLayout(false);
        splitContainer2.Panel1.ResumeLayout(false);
        splitContainer2.Panel1.PerformLayout();
        splitContainer2.Panel2.ResumeLayout(false);
        splitContainer2.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
        splitContainer2.ResumeLayout(false);
        tabTranslatePrompt.ResumeLayout(false);
        tabTranslatePreview.ResumeLayout(false);
        tabTranslateEdit.ResumeLayout(false);
        tabTranslateEdit.PerformLayout();
        tabVerifyPrompt.ResumeLayout(false);
        tabVerifyPreview.ResumeLayout(false);
        tabVerifyEdit.ResumeLayout(false);
        tabVerifyEdit.PerformLayout();
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
    private TabControl tabTranslatePrompt;
    private TabPage tabTranslatePreview;
    private WebBrowser webTranslatePreview;
    private TabPage tabTranslateEdit;
    private TextBox txtTranslatePrompt;
    private Label lblVerifyPrompt;
    private TabControl tabVerifyPrompt;
    private TabPage tabVerifyPreview;
    private WebBrowser webVerifyPreview;
    private TabPage tabVerifyEdit;
    private TextBox txtVerifyPrompt;
    private Label lblKey;
    private TextBox txtKey;
    private Label lblUrl;
    private TextBox txtUrl;
    private RadioButton rbOpenAi;
    private RadioButton rbAnthropic;
    private Label lblModelName;
    private TextBox txtModelName;
    private Label lblAnthropicKey;
    private TextBox txtAnthropicKey;
    private Label lblAnthropicUrl;
    private TextBox txtAnthropicUrl;
    private Label lblAnthropicModelName;
    private TextBox txtAnthropicModelName;
    private Button btnOk;
    private Button btnCancel;
    private SplitContainer splitContainer1;
    private ImageList tabPromptIcons;
    private SplitContainer splitContainer2;
}
