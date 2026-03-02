using Markdig;

namespace CheckTranslation;

internal sealed partial class ConfigForm : Form
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public ConfigForm()
    {
        InitializeComponent();
        InitMarkdownEditors();
        btnOk.Click += (_, _) => SaveConfig();
        LoadConfig();
    }

    private void InitMarkdownEditors()
    {
        SetupMarkdownEditorWithPreview(txtTranslatePrompt);
        SetupMarkdownEditorWithPreview(txtVerifyPrompt);
    }

    private static void SetupMarkdownEditorWithPreview(TextBox editor)
    {
        var parent = editor.Parent;
        if (parent is null)
            return;

        var bounds = editor.Bounds;
        var anchor = editor.Anchor;

        var tabs = new TabControl
        {
            Location = bounds.Location,
            Size = bounds.Size,
            Anchor = anchor,
        };

        var editPage = new TabPage("Édition")
        {
            Padding = new Padding(3),
        };

        var previewPage = new TabPage("Aperçu")
        {
            Padding = new Padding(3),
        };
        var images = new ImageList
        {
            ImageSize = new Size(16, 16),
            ColorDepth = ColorDepth.Depth32Bit,
        };
        images.Images.Add("preview", CreateEyeIcon());
        images.Images.Add("edit", CreatePencilIcon());

        tabs.ImageList = images;
        previewPage.ImageKey = "preview";
        editPage.ImageKey = "edit";

        var browser = new WebBrowser
        {
            Dock = DockStyle.Fill,
            AllowWebBrowserDrop = false,
            IsWebBrowserContextMenuEnabled = false,
            WebBrowserShortcutsEnabled = false,
            ScriptErrorsSuppressed = true,
        };

        parent.Controls.Remove(editor);

        editor.Dock = DockStyle.Fill;
        editPage.Controls.Add(editor);

        previewPage.Controls.Add(browser);
        tabs.TabPages.Add(previewPage);
        tabs.TabPages.Add(editPage);
        parent.Controls.Add(tabs);
        tabs.BringToFront();
        tabs.SelectedTab = previewPage;

        void UpdatePreview()
        {
            var html = Markdig.Markdown.ToHtml(editor.Text ?? string.Empty, MarkdownPipeline);

            const string css = """
                body { font-family: Segoe UI, Arial, sans-serif; font-size: 10pt; padding: 12px; }
                code, pre { font-family: Consolas, 'Courier New', monospace; }
                pre { background: #f6f8fa; padding: 10px; border-radius: 6px; overflow-x: auto; }
                blockquote { border-left: 3px solid #d0d7de; margin: 0; padding-left: 12px; color: #57606a; }
                table { border-collapse: collapse; }
                th, td { border: 1px solid #d0d7de; padding: 6px 10px; }
                """;

            browser.DocumentText = $"""
                <!doctype html>
                <html>
                <head>
                  <meta charset="utf-8">
                  <style>
                {css}
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
        txtTranslatePrompt.Text = config.TranslatePrompt;
        txtVerifyPrompt.Text = config.VerifyPrompt;
        txtKey.Text = config.Key;
        txtUrl.Text = config.Url;
        txtModelName.Text = config.ModelName;
    }

    private void SaveConfig()
    {
        var config = new AppConfig
        {
            TranslatePrompt = txtTranslatePrompt.Text.Trim(),
            VerifyPrompt = txtVerifyPrompt.Text.Trim(),
            Key = txtKey.Text.Trim(),
            Url = txtUrl.Text.Trim(),
            ModelName = txtModelName.Text.Trim(),
            ShowDetails = AppConfig.Current.ShowDetails,
        };
        config.Save();
    }
}

