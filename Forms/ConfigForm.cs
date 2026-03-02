using Markdig;

namespace CheckTranslation;

internal sealed partial class ConfigForm : Form
{
    private bool _isLoading;
    private bool _isUpdatingProvider;

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private const string Css = """
        body { font-family: Segoe UI, Arial, sans-serif; font-size: 10pt; padding: 12px; }
        code, pre { font-family: Consolas, 'Courier New', monospace; }
        pre { background: #f6f8fa; padding: 10px; border-radius: 6px; overflow-x: auto; }
        blockquote { border-left: 3px solid #d0d7de; margin: 0; padding-left: 12px; color: #57606a; }
        table { border-collapse: collapse; }
        th, td { border: 1px solid #d0d7de; padding: 6px 10px; }
        """;

    public ConfigForm()
    {
        InitializeComponent();
        InitProviderUi();
        InitMarkdownEditors();
        btnOk.Click += (_, _) => SaveConfig();
        LoadConfig();
    }

    private void InitProviderUi()
    {
        rbOpenAi.CheckedChanged += ProviderChanged;
        rbAnthropic.CheckedChanged += ProviderChanged;
    }

    private void ProviderChanged(object? sender, EventArgs e)
    {
        if (_isLoading || _isUpdatingProvider)
            return;

        // Les deux RadioButton sont dans deux containers différents (SplitContainer.Panel1/Panel2),
        // donc WinForms ne gère pas l'exclusivité automatiquement.
        try
        {
            _isUpdatingProvider = true;
            if (ReferenceEquals(sender, rbOpenAi) && rbOpenAi.Checked)
                rbAnthropic.Checked = false;
            else if (ReferenceEquals(sender, rbAnthropic) && rbAnthropic.Checked)
                rbOpenAi.Checked = false;
        }
        finally
        {
            _isUpdatingProvider = false;
        }

        if (rbOpenAi.Checked)
        {
            if (string.IsNullOrWhiteSpace(txtUrl.Text))
                txtUrl.Text = AppConfig.GetDefaultUrl(AiProvider.OpenAI);

            if (string.IsNullOrWhiteSpace(txtModelName.Text))
                txtModelName.Text = AppConfig.GetDefaultModelName(AiProvider.OpenAI);
        }
        else if (rbAnthropic.Checked)
        {
            if (string.IsNullOrWhiteSpace(txtAnthropicUrl.Text))
                txtAnthropicUrl.Text = AppConfig.GetDefaultUrl(AiProvider.Anthropic);

            if (string.IsNullOrWhiteSpace(txtAnthropicModelName.Text))
                txtAnthropicModelName.Text = AppConfig.GetDefaultModelName(AiProvider.Anthropic);
        }
    }

    private void InitMarkdownEditors()
    {
        EnsureTabIcons();

        SetupMarkdownPreview(txtTranslatePrompt, tabTranslatePrompt, tabTranslatePreview, webTranslatePreview);
        SetupMarkdownPreview(txtVerifyPrompt, tabVerifyPrompt, tabVerifyPreview, webVerifyPreview);

        tabTranslatePrompt.SelectedTab = tabTranslatePreview;
        tabVerifyPrompt.SelectedTab = tabVerifyPreview;
    }

    private void EnsureTabIcons()
    {
        tabPromptIcons.ImageSize = new Size(16, 16);
        tabPromptIcons.ColorDepth = ColorDepth.Depth32Bit;
        tabPromptIcons.TransparentColor = Color.Transparent;

        if (!tabPromptIcons.Images.ContainsKey("preview") || !tabPromptIcons.Images.ContainsKey("edit"))
        {
            tabPromptIcons.Images.Clear();

            var eye = LoadIconFromResources("eyes.png") ?? CreateEyeIcon();
            var pencil = LoadIconFromResources("pencil.png") ?? CreatePencilIcon();
            tabPromptIcons.Images.Add("preview", eye);
            tabPromptIcons.Images.Add("edit", pencil);
        }

        tabTranslatePrompt.ImageList = tabPromptIcons;
        tabVerifyPrompt.ImageList = tabPromptIcons;

        tabTranslatePreview.ImageKey = "preview";
        tabTranslateEdit.ImageKey = "edit";
        tabVerifyPreview.ImageKey = "preview";
        tabVerifyEdit.ImageKey = "edit";
    }

    private static Bitmap? LoadIconFromResources(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", fileName);
        if (!File.Exists(path))
            return null;

        using var original = new Bitmap(path);
        return new Bitmap(original);
    }

    private static void SetupMarkdownPreview(TextBox editor, TabControl tabs, TabPage previewPage, WebBrowser browser)
    {
        browser.AllowWebBrowserDrop = false;
        browser.IsWebBrowserContextMenuEnabled = false;
        browser.WebBrowserShortcutsEnabled = false;
        browser.ScriptErrorsSuppressed = true;

        void UpdatePreview()
        {
            var html = Markdig.Markdown.ToHtml(editor.Text ?? string.Empty, MarkdownPipeline);
            browser.DocumentText = $"""
                <!doctype html>
                <html>
                <head>
                  <meta charset=\"utf-8\">
                  <style>
                {Css}
                  </style>
                </head>
                <body>
                {html}
                </body>
                </html>
                """;
        }

        var debounce = new System.Windows.Forms.Timer { Interval = 250 };
        debounce.Tick += (_, _) =>
        {
            debounce.Stop();
            if (tabs.SelectedTab == previewPage)
                UpdatePreview();
        };

        editor.TextChanged += (_, _) =>
        {
            debounce.Stop();
            debounce.Start();
        };

        tabs.SelectedIndexChanged += (_, _) =>
        {
            if (tabs.SelectedTab == previewPage)
                UpdatePreview();
        };

        if (tabs.SelectedTab == previewPage)
            UpdatePreview();
    }

    private static Bitmap CreatePencilIcon()
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var bodyBrush = new SolidBrush(Color.FromArgb(255, 255, 204, 102));
        using var tipBrush = new SolidBrush(Color.FromArgb(255, 120, 120, 120));
        using var outlinePen = new Pen(Color.FromArgb(255, 80, 80, 80), 1);

        var body = new PointF[]
        {
            new(3, 12),
            new(12, 3),
            new(14, 5),
            new(5, 14),
        };
        g.FillPolygon(bodyBrush, body);
        g.DrawPolygon(outlinePen, body);

        var tip = new PointF[]
        {
            new(12, 3),
            new(14, 1),
            new(15, 2),
            new(14, 5),
        };
        g.FillPolygon(tipBrush, tip);
        g.DrawPolygon(outlinePen, tip);

        using var eraserBrush = new SolidBrush(Color.FromArgb(255, 240, 160, 160));
        var eraser = new PointF[]
        {
            new(3, 12),
            new(1, 14),
            new(2, 15),
            new(5, 14),
        };
        g.FillPolygon(eraserBrush, eraser);
        g.DrawPolygon(outlinePen, eraser);

        return bmp;
    }

    private static Bitmap CreateEyeIcon()
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var outlinePen = new Pen(Color.FromArgb(255, 80, 80, 80), 1);
        using var whiteBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
        using var irisBrush = new SolidBrush(Color.FromArgb(255, 70, 130, 180));
        using var pupilBrush = new SolidBrush(Color.FromArgb(255, 30, 30, 30));

        var eyeRect = new RectangleF(2, 5, 12, 6);
        g.FillEllipse(whiteBrush, eyeRect);
        g.DrawEllipse(outlinePen, eyeRect);

        g.FillEllipse(irisBrush, 6, 6.5f, 4, 4);
        g.FillEllipse(pupilBrush, 7.25f, 7.75f, 1.5f, 1.5f);

        using var highlightBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
        g.FillEllipse(highlightBrush, 8.2f, 7.2f, 1.2f, 1.2f);

        return bmp;
    }

    private void LoadConfig()
    {
        var config = AppConfig.Current;

        _isLoading = true;
        try
        {
            rbOpenAi.Checked = config.Provider != AiProvider.Anthropic;
            rbAnthropic.Checked = config.Provider == AiProvider.Anthropic;

            txtKey.Text = config.OpenAiKey;
            txtUrl.Text = config.OpenAiUrl;
            txtModelName.Text = config.OpenAiModelName;

            txtAnthropicKey.Text = config.AnthropicKey;
            txtAnthropicUrl.Text = config.AnthropicUrl;
            txtAnthropicModelName.Text = config.AnthropicModelName;
        }
        finally
        {
            _isLoading = false;
        }

        txtTranslatePrompt.Text = config.TranslatePrompt;
        txtVerifyPrompt.Text = config.VerifyPrompt;
    }

    private void SaveConfig()
    {
        var config = new AppConfig
        {
            TranslatePrompt = txtTranslatePrompt.Text.Trim(),
            VerifyPrompt = txtVerifyPrompt.Text.Trim(),

            OpenAiKey = txtKey.Text.Trim(),
            OpenAiUrl = txtUrl.Text.Trim(),
            OpenAiModelName = txtModelName.Text.Trim(),

            AnthropicKey = txtAnthropicKey.Text.Trim(),
            AnthropicUrl = txtAnthropicUrl.Text.Trim(),
            AnthropicModelName = txtAnthropicModelName.Text.Trim(),

            Provider = rbAnthropic.Checked ? AiProvider.Anthropic : AiProvider.OpenAI,
            ShowDetails = AppConfig.Current.ShowDetails,
        };
        config.Save();
    }
}
