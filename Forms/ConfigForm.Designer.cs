namespace CheckTranslation;

partial class ConfigForm
{
    // This is a test designer patch
    private string testField;
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
        txtOpenAiKey = new TextBox();
        txtOpenAiUrl = new TextBox();
        lblModelName = new Label();
        lblUrl = new Label();
        txtOpenAiModelName = new ComboBox();
        txtAnthropicKey = new TextBox();
        rbAnthropic = new RadioButton();
        lblAnthropicKey = new Label();
        txtAnthropicUrl = new TextBox();
        lblAnthropicModelName = new Label();
        lblAnthropicUrl = new Label();
        txtAnthropicModelName = new ComboBox();
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
        grpAuth.Location = new Point(11, 787);
        grpAuth.Name = "grpAuth";
        grpAuth.Size = new Size(938, 205);
        grpAuth.TabIndex = 0;
        grpAuth.TabStop = false;
        grpAuth.Text = "Paramètres IA";
        // 
        // splitContainer2
        // 
        splitContainer2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        splitContainer2.Location = new Point(7, 28);
        splitContainer2.Margin = new Padding(3, 4, 3, 4);
        splitContainer2.Name = "splitContainer2";
        // 
        // splitContainer2.Panel1
        // 
        splitContainer2.Panel1.Controls.Add(lblKey);
        splitContainer2.Panel1.Controls.Add(rbOpenAi);
        splitContainer2.Panel1.Controls.Add(txtOpenAiKey);
        splitContainer2.Panel1.Controls.Add(txtOpenAiUrl);
        splitContainer2.Panel1.Controls.Add(lblModelName);
        splitContainer2.Panel1.Controls.Add(lblUrl);
        splitContainer2.Panel1.Controls.Add(txtOpenAiModelName);
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
        splitContainer2.Size = new Size(925, 171);
        splitContainer2.SplitterDistance = 460;
        splitContainer2.SplitterWidth = 5;
        splitContainer2.TabIndex = 8;
        // 
        // lblKey
        // 
        lblKey.AutoSize = true;
        lblKey.Location = new Point(7, 45);
        lblKey.Name = "lblKey";
        lblKey.Size = new Size(40, 20);
        lblKey.TabIndex = 1;
        lblKey.Text = "Key :";
        // 
        // rbOpenAi
        // 
        rbOpenAi.AutoSize = true;
        rbOpenAi.Checked = true;
        rbOpenAi.Location = new Point(11, 9);
        rbOpenAi.Margin = new Padding(3, 4, 3, 4);
        rbOpenAi.Name = "rbOpenAi";
        rbOpenAi.Size = new Size(150, 24);
        rbOpenAi.TabIndex = 0;
        rbOpenAi.TabStop = true;
        rbOpenAi.Text = "OpenAI (ChatGPT)";
        rbOpenAi.UseVisualStyleBackColor = true;
        // 
        // txtOpenAiKey
        // 
        txtOpenAiKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtOpenAiKey.Location = new Point(78, 41);
        txtOpenAiKey.Name = "txtOpenAiKey";
        txtOpenAiKey.Size = new Size(378, 27);
        txtOpenAiKey.TabIndex = 1;
        txtOpenAiKey.UseSystemPasswordChar = true;
        // 
        // txtOpenAiUrl
        // 
        txtOpenAiUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtOpenAiUrl.Location = new Point(78, 77);
        txtOpenAiUrl.Name = "txtOpenAiUrl";
        txtOpenAiUrl.Size = new Size(378, 27);
        txtOpenAiUrl.TabIndex = 2;
        // 
        // lblModelName
        // 
        lblModelName.AutoSize = true;
        lblModelName.Location = new Point(7, 117);
        lblModelName.Name = "lblModelName";
        lblModelName.Size = new Size(59, 20);
        lblModelName.TabIndex = 4;
        lblModelName.Text = "Model :";
        // 
        // lblUrl
        // 
        lblUrl.AutoSize = true;
        lblUrl.Location = new Point(7, 81);
        lblUrl.Name = "lblUrl";
        lblUrl.Size = new Size(35, 20);
        lblUrl.TabIndex = 2;
        lblUrl.Text = "Url :";
        // 
        // txtOpenAiModelName
        // 
        txtOpenAiModelName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtOpenAiModelName.FormattingEnabled = true;
        txtOpenAiModelName.Location = new Point(78, 113);
        txtOpenAiModelName.Name = "txtOpenAiModelName";
        txtOpenAiModelName.Size = new Size(378, 28);
        txtOpenAiModelName.TabIndex = 4;
        // 
        // txtAnthropicKey
        // 
        txtAnthropicKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtAnthropicKey.Location = new Point(78, 41);
        txtAnthropicKey.Name = "txtAnthropicKey";
        txtAnthropicKey.Size = new Size(379, 27);
        txtAnthropicKey.TabIndex = 5;
        txtAnthropicKey.UseSystemPasswordChar = true;
        // 
        // rbAnthropic
        // 
        rbAnthropic.AutoSize = true;
        rbAnthropic.Location = new Point(11, 9);
        rbAnthropic.Margin = new Padding(3, 4, 3, 4);
        rbAnthropic.Name = "rbAnthropic";
        rbAnthropic.Size = new Size(155, 24);
        rbAnthropic.TabIndex = 0;
        rbAnthropic.Text = "Anthropic (Claude)";
        rbAnthropic.UseVisualStyleBackColor = true;
        // 
        // lblAnthropicKey
        // 
        lblAnthropicKey.AutoSize = true;
        lblAnthropicKey.Location = new Point(7, 45);
        lblAnthropicKey.Name = "lblAnthropicKey";
        lblAnthropicKey.Size = new Size(40, 20);
        lblAnthropicKey.TabIndex = 5;
        lblAnthropicKey.Text = "Key :";
        // 
        // txtAnthropicUrl
        // 
        txtAnthropicUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtAnthropicUrl.Location = new Point(78, 80);
        txtAnthropicUrl.Name = "txtAnthropicUrl";
        txtAnthropicUrl.Size = new Size(378, 27);
        txtAnthropicUrl.TabIndex = 6;
        // 
        // lblAnthropicModelName
        // 
        lblAnthropicModelName.AutoSize = true;
        lblAnthropicModelName.Location = new Point(7, 117);
        lblAnthropicModelName.Name = "lblAnthropicModelName";
        lblAnthropicModelName.Size = new Size(59, 20);
        lblAnthropicModelName.TabIndex = 7;
        lblAnthropicModelName.Text = "Model :";
        // 
        // lblAnthropicUrl
        // 
        lblAnthropicUrl.AutoSize = true;
        lblAnthropicUrl.Location = new Point(7, 81);
        lblAnthropicUrl.Name = "lblAnthropicUrl";
        lblAnthropicUrl.Size = new Size(35, 20);
        lblAnthropicUrl.TabIndex = 6;
        lblAnthropicUrl.Text = "Url :";
        // 
        // txtAnthropicModelName
        // 
        txtAnthropicModelName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtAnthropicModelName.FormattingEnabled = true;
        txtAnthropicModelName.Location = new Point(78, 116);
        txtAnthropicModelName.Name = "txtAnthropicModelName";
        txtAnthropicModelName.Size = new Size(378, 28);
        txtAnthropicModelName.TabIndex = 7;
        // 
        // lblPrompt
        // 
        lblPrompt.AutoSize = true;
        lblPrompt.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblPrompt.Location = new Point(6, 13);
        lblPrompt.Name = "lblPrompt";
        lblPrompt.Size = new Size(92, 20);
        lblPrompt.TabIndex = 0;
        lblPrompt.Text = "Traduction :";
        // 
        // tabTranslatePrompt
        // 
        tabTranslatePrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        tabTranslatePrompt.Controls.Add(tabTranslatePreview);
        tabTranslatePrompt.Controls.Add(tabTranslateEdit);
        tabTranslatePrompt.ImageList = tabPromptIcons;
        tabTranslatePrompt.Location = new Point(6, 36);
        tabTranslatePrompt.Name = "tabTranslatePrompt";
        tabTranslatePrompt.SelectedIndex = 0;
        tabTranslatePrompt.Size = new Size(933, 348);
        tabTranslatePrompt.TabIndex = 0;
        // 
        // tabTranslatePreview
        // 
        tabTranslatePreview.Controls.Add(webTranslatePreview);
        tabTranslatePreview.ImageKey = "preview";
        tabTranslatePreview.Location = new Point(4, 29);
        tabTranslatePreview.Name = "tabTranslatePreview";
        tabTranslatePreview.Padding = new Padding(3, 4, 3, 4);
        tabTranslatePreview.Size = new Size(925, 315);
        tabTranslatePreview.TabIndex = 0;
        tabTranslatePreview.Text = "Aperçu";
        tabTranslatePreview.UseVisualStyleBackColor = true;
        // 
        // webTranslatePreview
        // 
        webTranslatePreview.Dock = DockStyle.Fill;
        webTranslatePreview.Location = new Point(3, 4);
        webTranslatePreview.MinimumSize = new Size(23, 27);
        webTranslatePreview.Name = "webTranslatePreview";
        webTranslatePreview.Size = new Size(919, 307);
        webTranslatePreview.TabIndex = 0;
        // 
        // tabTranslateEdit
        // 
        tabTranslateEdit.Controls.Add(txtTranslatePrompt);
        tabTranslateEdit.ImageKey = "edit";
        tabTranslateEdit.Location = new Point(4, 29);
        tabTranslateEdit.Name = "tabTranslateEdit";
        tabTranslateEdit.Padding = new Padding(3, 4, 3, 4);
        tabTranslateEdit.Size = new Size(925, 316);
        tabTranslateEdit.TabIndex = 1;
        tabTranslateEdit.Text = "Édition";
        tabTranslateEdit.UseVisualStyleBackColor = true;
        // 
        // txtTranslatePrompt
        // 
        txtTranslatePrompt.Dock = DockStyle.Fill;
        txtTranslatePrompt.Location = new Point(3, 4);
        txtTranslatePrompt.Multiline = true;
        txtTranslatePrompt.Name = "txtTranslatePrompt";
        txtTranslatePrompt.ScrollBars = ScrollBars.Vertical;
        txtTranslatePrompt.Size = new Size(919, 308);
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
        lblVerifyPrompt.Location = new Point(6, 13);
        lblVerifyPrompt.Name = "lblVerifyPrompt";
        lblVerifyPrompt.Size = new Size(97, 20);
        lblVerifyPrompt.TabIndex = 6;
        lblVerifyPrompt.Text = "Vérification :";
        // 
        // tabVerifyPrompt
        // 
        tabVerifyPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        tabVerifyPrompt.Controls.Add(tabVerifyPreview);
        tabVerifyPrompt.Controls.Add(tabVerifyEdit);
        tabVerifyPrompt.ImageList = tabPromptIcons;
        tabVerifyPrompt.Location = new Point(6, 36);
        tabVerifyPrompt.Name = "tabVerifyPrompt";
        tabVerifyPrompt.SelectedIndex = 0;
        tabVerifyPrompt.Size = new Size(933, 344);
        tabVerifyPrompt.TabIndex = 1;
        // 
        // tabVerifyPreview
        // 
        tabVerifyPreview.Controls.Add(webVerifyPreview);
        tabVerifyPreview.ImageKey = "preview";
        tabVerifyPreview.Location = new Point(4, 29);
        tabVerifyPreview.Name = "tabVerifyPreview";
        tabVerifyPreview.Padding = new Padding(3, 4, 3, 4);
        tabVerifyPreview.Size = new Size(925, 311);
        tabVerifyPreview.TabIndex = 0;
        tabVerifyPreview.Text = "Aperçu";
        tabVerifyPreview.UseVisualStyleBackColor = true;
        // 
        // webVerifyPreview
        // 
        webVerifyPreview.Dock = DockStyle.Fill;
        webVerifyPreview.Location = new Point(3, 4);
        webVerifyPreview.MinimumSize = new Size(23, 27);
        webVerifyPreview.Name = "webVerifyPreview";
        webVerifyPreview.Size = new Size(919, 303);
        webVerifyPreview.TabIndex = 0;
        // 
        // tabVerifyEdit
        // 
        tabVerifyEdit.Controls.Add(txtVerifyPrompt);
        tabVerifyEdit.ImageKey = "edit";
        tabVerifyEdit.Location = new Point(4, 29);
        tabVerifyEdit.Name = "tabVerifyEdit";
        tabVerifyEdit.Padding = new Padding(3, 4, 3, 4);
        tabVerifyEdit.Size = new Size(925, 310);
        tabVerifyEdit.TabIndex = 1;
        tabVerifyEdit.Text = "Édition";
        tabVerifyEdit.UseVisualStyleBackColor = true;
        // 
        // txtVerifyPrompt
        // 
        txtVerifyPrompt.Dock = DockStyle.Fill;
        txtVerifyPrompt.Location = new Point(3, 4);
        txtVerifyPrompt.Multiline = true;
        txtVerifyPrompt.Name = "txtVerifyPrompt";
        txtVerifyPrompt.ScrollBars = ScrollBars.Vertical;
        txtVerifyPrompt.Size = new Size(919, 302);
        txtVerifyPrompt.TabIndex = 1;
        // 
        // btnOk
        // 
        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new Point(779, 997);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(80, 28);
        btnOk.TabIndex = 4;
        btnOk.Text = "OK";
        // 
        // btnCancel
        // 
        btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(870, 997);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(80, 28);
        btnCancel.TabIndex = 5;
        btnCancel.Text = "Annuler";
        // 
        // splitContainer1
        // 
        splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        splitContainer1.Location = new Point(11, 12);
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
        splitContainer1.Size = new Size(938, 769);
        splitContainer1.SplitterDistance = 387;
        splitContainer1.TabIndex = 7;
        // 
        // ConfigForm
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(965, 1045);
        Controls.Add(splitContainer1);
        Controls.Add(grpAuth);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(683, 718);
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
    private TextBox txtOpenAiKey;
    private Label lblUrl;
    private TextBox txtOpenAiUrl;
    private RadioButton rbOpenAi;
    private RadioButton rbAnthropic;
    private Label lblModelName;
    private ComboBox txtOpenAiModelName;
    private Label lblAnthropicKey;
    private TextBox txtAnthropicKey;
    private Label lblAnthropicUrl;
    private TextBox txtAnthropicUrl;
    private Label lblAnthropicModelName;
    private ComboBox txtAnthropicModelName;
    private Button btnOk;
    private Button btnCancel;
    private SplitContainer splitContainer1;
    private ImageList tabPromptIcons;
    private SplitContainer splitContainer2;
}
