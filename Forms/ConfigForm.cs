using Markdig;
using System.ClientModel;
using Anthropic;
using Anthropic.Models.Models;
using OpenAI;

namespace CheckTranslation;

internal sealed partial class ConfigForm : Form
{
    private bool _isLoading;
    private bool _isUpdatingProvider;
    private bool _openAiModelsLoaded;
    private bool _anthropicModelsLoaded;
    private bool _isLoadingModels;

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
        InitModelSelectors();
        InitProviderUi();
        InitMarkdownEditors();
        btnOk.Click += (_, _) => SaveConfig();
        LoadConfig();
    }

        private void InitModelSelectors()
    {
        txtOpenAiModelName.DropDownStyle = ComboBoxStyle.DropDown;
        txtOpenAiModelName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        txtOpenAiModelName.AutoCompleteSource = AutoCompleteSource.ListItems;

        txtOpenAiModelName.DropDown += async (_, _) => await TryLoadOpenAiModelsAsync();
        txtOpenAiModelName.MouseDown += async (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                await TryLoadOpenAiModelsAsync();
        };
        txtOpenAiKey.TextChanged += (_, _) => _openAiModelsLoaded = false;
        txtOpenAiUrl.TextChanged += (_, _) => _openAiModelsLoaded = false;

        txtAnthropicModelName.DropDownStyle = ComboBoxStyle.DropDown;
        txtAnthropicModelName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        txtAnthropicModelName.AutoCompleteSource = AutoCompleteSource.ListItems;

        txtAnthropicModelName.DropDown += async (_, _) => await TryLoadAnthropicModelsAsync();
        txtAnthropicModelName.MouseDown += async (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                await TryLoadAnthropicModelsAsync();
        };
        txtAnthropicKey.TextChanged += (_, _) => _anthropicModelsLoaded = false;
        txtAnthropicUrl.TextChanged += (_, _) => _anthropicModelsLoaded = false;

        txtOpenAiModelName.Items.Clear();
        txtOpenAiModelName.Items.Add(AppConfig.GetDefaultModelName(AiProvider.OpenAI));

        txtAnthropicModelName.Items.Clear();
        txtAnthropicModelName.Items.Add(AppConfig.GetDefaultModelName(AiProvider.Anthropic));
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
            if (string.IsNullOrWhiteSpace(txtOpenAiUrl.Text))
                txtOpenAiUrl.Text = AppConfig.GetDefaultUrl(AiProvider.OpenAI);

            if (string.IsNullOrWhiteSpace(txtOpenAiModelName.Text))
                txtOpenAiModelName.Text = AppConfig.GetDefaultModelName(AiProvider.OpenAI);
        }
        else if (rbAnthropic.Checked)
        {
            if (string.IsNullOrWhiteSpace(txtAnthropicUrl.Text))
                txtAnthropicUrl.Text = AppConfig.GetDefaultUrl(AiProvider.Anthropic);

            if (string.IsNullOrWhiteSpace(txtAnthropicModelName.Text))
                txtAnthropicModelName.Text = AppConfig.GetDefaultModelName(AiProvider.Anthropic);
        }
    }

        private async Task TryLoadOpenAiModelsAsync()
    {
        if (_isLoading)
            return;

        if (_isLoadingModels || _openAiModelsLoaded)
            return;

        var key = txtOpenAiKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
            return;

        var endpoint = NormalizeOpenAiEndpoint(txtOpenAiUrl.Text);
        var currentModel = txtOpenAiModelName.Text;

        try
        {
            _isLoadingModels = true;

            var client = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions { Endpoint = new Uri(endpoint) });
            var modelClient = client.GetOpenAIModelClient();

            var response = await modelClient.GetModelsAsync(CancellationToken.None);
            var payload = UnwrapClientResult(response);
            if (payload is null)
                return;

            var modelIds = ExtractIds(payload)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .Cast<object>()
                .ToArray();

            if (modelIds.Length == 0)
                return;

            _openAiModelsLoaded = true;

            if (IsHandleCreated)
            {
                BeginInvoke(() =>
                {
                    txtOpenAiModelName.BeginUpdate();
                    try
                    {
                        txtOpenAiModelName.Items.Clear();
                        txtOpenAiModelName.Items.AddRange(modelIds);
                        txtOpenAiModelName.Text = currentModel;
                    }
                    finally
                    {
                        txtOpenAiModelName.EndUpdate();
                    }
                });
            }
        }
        catch
        {
            // best effort
        }
        finally
        {
            _isLoadingModels = false;
        }
    }


    private async Task TryLoadAnthropicModelsAsync()
    {
        if (_isLoading)
            return;

        if (_isLoadingModels || _anthropicModelsLoaded)
            return;

        var key = txtAnthropicKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
            return;

        var baseUrl = NormalizeAnthropicBaseUrl(txtAnthropicUrl.Text);
        var currentModel = txtAnthropicModelName.Text;

        try
        {
            _isLoadingModels = true;
            var client = new AnthropicClient
            {
                ApiKey = key,
                BaseUrl = baseUrl,
            };

            var page = await client.Models.List(new ModelListParams(), CancellationToken.None);

            var modelIds = page.Items
                .Select(GetModelId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .Cast<object>()
                .ToArray();

            if (modelIds.Length == 0)
                return;

            _anthropicModelsLoaded = true;

            if (IsHandleCreated)
            {
                BeginInvoke(() =>
                {
                    txtAnthropicModelName.BeginUpdate();
                    try
                    {
                        txtAnthropicModelName.Items.Clear();
                        txtAnthropicModelName.Items.AddRange(modelIds);
                        txtAnthropicModelName.Text = currentModel;
                    }
                    finally
                    {
                        txtAnthropicModelName.EndUpdate();
                    }
                });
            }
        }
        catch
        {
            // best effort: si la clé est invalide / pas de réseau / etc., on garde la liste statique
        }
        finally
        {
            _isLoadingModels = false;
        }
    }

    private static string? GetModelId(object model)
    {
        var t = model.GetType();
        var p = t.GetProperty("ID") ?? t.GetProperty("Id") ?? t.GetProperty("ModelID") ?? t.GetProperty("ModelId");
        return p?.GetValue(model)?.ToString();
    }

    private static string NormalizeAnthropicBaseUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return AppConfig.GetDefaultUrl(AiProvider.Anthropic);

        var trimmed = url.Trim().TrimEnd('/');
        if (trimmed.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^"/v1/messages".Length];
        else if (trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^"/v1".Length];

        return trimmed;
    }

    private static string NormalizeOpenAiEndpoint(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return AppConfig.GetDefaultUrl(AiProvider.OpenAI);

        var trimmed = url.Trim().TrimEnd('/');

        if (trimmed.EndsWith("/v1/responses", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^"/responses".Length];
        if (trimmed.EndsWith("/v1/chat/completions", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^"/chat/completions".Length];
        if (trimmed.EndsWith("/responses", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^"/responses".Length];

        return trimmed;
    }

    private static object? UnwrapClientResult(object? maybeClientResult)
    {
        if (maybeClientResult is null)
            return null;

        var valueProp = maybeClientResult.GetType().GetProperty("Value");
        return valueProp?.GetValue(maybeClientResult) ?? maybeClientResult;
    }

    private static IEnumerable<string?> ExtractIds(object payload)
    {
        System.Collections.IEnumerable? models = payload as System.Collections.IEnumerable;

        if (models is null)
        {
            var dataProp = payload.GetType().GetProperty("Data") ?? payload.GetType().GetProperty("Items");
            models = dataProp?.GetValue(payload) as System.Collections.IEnumerable;
        }

        if (models is null)
            yield break;

        foreach (var model in models)
        {
            if (model is null)
                continue;

            var t = model.GetType();
            var p = t.GetProperty("Id") ?? t.GetProperty("ID") ?? t.GetProperty("Model") ?? t.GetProperty("Name");
            if (p is null)
                continue;

            yield return p.GetValue(model)?.ToString();
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

            txtOpenAiKey.Text = config.OpenAiKey;
            txtOpenAiUrl.Text = config.OpenAiUrl;
            txtOpenAiModelName.Text = config.OpenAiModelName;

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

            OpenAiKey = txtOpenAiKey.Text.Trim(),
            OpenAiUrl = txtOpenAiUrl.Text.Trim(),
            OpenAiModelName = txtOpenAiModelName.Text.Trim(),

            AnthropicKey = txtAnthropicKey.Text.Trim(),
            AnthropicUrl = txtAnthropicUrl.Text.Trim(),
            AnthropicModelName = txtAnthropicModelName.Text.Trim(),

            Provider = rbAnthropic.Checked ? AiProvider.Anthropic : AiProvider.OpenAI,
            ShowDetails = AppConfig.Current.ShowDetails,
        };
        config.Save();
    }
}

