using Markdig;
using Markdig.Extensions.GenericAttributes;
using System.ClientModel;
using Anthropic;
using Anthropic.Models.Models;
using OpenAI;

namespace CheckTranslation;

internal sealed partial class ConfigForm : Form
{
    private readonly ITranslationService _translationService;
    private bool _isLoading;
    private bool _isUpdatingProvider;
    // Un drapeau « modèles déjà chargés » par ComboBox : il y a quatre fournisseurs, et la
    // liste doit être réinvalidée dès que la clé ou l'URL de CE fournisseur change.
    private readonly HashSet<ComboBox> _modelsLoaded = [];
    private bool _isLoadingModels;

    private static readonly MarkdownPipeline MarkdownPipeline = BuildPreviewPipeline();

    /// <summary>
    /// Pipeline de rendu de l'aperçu des prompts.
    ///
    /// <c>UseAdvancedExtensions</c> embarque l'extension <i>generic attributes</i>, qui lit
    /// <c>{...}</c> comme un bloc d'attributs HTML : elle transforme
    /// « traduire vers {language} » en <c>&lt;p language=""&gt;traduire vers &lt;/p&gt;</c>. Les
    /// placeholders des prompts disparaissaient donc de l'aperçu — silencieusement, et précisément
    /// aux endroits où l'utilisateur doit vérifier qu'ils sont bien posés. On la retire en
    /// conservant tout le reste (tableaux, emphase étendue, listes de tâches…).
    /// </summary>
    private static MarkdownPipeline BuildPreviewPipeline()
    {
        var builder = new MarkdownPipelineBuilder().UseAdvancedExtensions();
        builder.Extensions.RemoveAll(extension => extension is GenericAttributesExtension);
        return builder.Build();
    }

    private const string Css = """
        body { font-family: Segoe UI, Arial, sans-serif; font-size: 10pt; padding: 12px; }
        code, pre { font-family: Consolas, 'Courier New', monospace; }
        pre { background: #f6f8fa; padding: 10px; border-radius: 6px; overflow-x: auto; }
        blockquote { border-left: 3px solid #d0d7de; margin: 0; padding-left: 12px; color: #57606a; }
        table { border-collapse: collapse; }
        th, td { border: 1px solid #d0d7de; padding: 6px 10px; }
        """;

    public ConfigForm() : this(new TranslationService())
    {
    }

    public ConfigForm(ITranslationService translationService)
    {
        _translationService = translationService;
        InitializeComponent();
        InitModelSelectors();
        InitProviderUi();
        InitMarkdownEditors();
        btnOk.Click += (_, _) => SaveConfig();
        btnClearTranslationCache.Click += BtnClearTranslationCache_Click;
        btnClearVerificationCache.Click += BtnClearVerificationCache_Click;
        LoadConfig();
    }

    // --- Mise en page : ce que le Designer ne peut pas garantir ---

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Après base.OnLoad : la mise à l'échelle selon le DPI est faite, les tailles lues ici
        // sont celles qui seront réellement affichées.
        StretchProviderFields();
        FitToWorkingArea();
    }

    /// <summary>
    /// Rend aux champs de chaque fournisseur toute la largeur de leur panneau.
    ///
    /// Ils sont ancrés à gauche et à droite dans un <see cref="SplitContainer"/>, mais WinForms fige
    /// les distances d'ancrage à la première mise en page — laquelle intervient avant que
    /// <c>EndInit</c> n'applique le <c>SplitterDistance</c>. Les champs héritent donc d'une marge
    /// droite calculée sur une largeur de panneau qui n'est pas la bonne, et finissent bien plus
    /// étroits que ce que le Designer indique : l'URL s'affiche tronquée alors que la moitié du
    /// panneau est vide. Réassigner la largeur ici réamorce du même coup les distances d'ancrage,
    /// donc les redimensionnements ultérieurs restent corrects.
    /// </summary>
    private void StretchProviderFields()
    {
        var gutter = LogicalToDeviceUnits(4);

        foreach (var split in grpAuth.Controls.OfType<SplitContainer>())
            foreach (var panel in new[] { split.Panel1, split.Panel2 })
                foreach (var field in panel.Controls.OfType<Control>())
                {
                    if (field is not TextBox and not ComboBox)
                        continue;

                    var width = panel.ClientSize.Width - field.Left - gutter;
                    if (width > 0)
                        field.Width = width;
                }
    }

    /// <summary>
    /// Ramène la fenêtre dans l'écran.
    ///
    /// Le dialogue est haut — quatre fournisseurs sous deux éditeurs de prompt — et sa hauteur est
    /// multipliée par la mise à l'échelle Windows : à 150 %, il dépasse un écran 1080p et les
    /// boutons OK / Annuler se retrouvent hors champ, donc hors de portée. La
    /// <see cref="Form.MinimumSize"/> subit la même mise à l'échelle : sans la borner d'abord, elle
    /// empêcherait la fenêtre de rétrécir et le réglage n'aurait aucun effet.
    ///
    /// L'écran de référence est celui du propriétaire, pas celui du dialogue : le dialogue est
    /// modal (<c>ShowDialog(this)</c>) et n'a pas encore été recentré sur son parent au moment du
    /// chargement. Se fier à sa propre position ferait dimensionner et recentrer sur l'écran
    /// principal une fenêtre qui doit s'afficher sur celui du parent.
    /// </summary>
    private void FitToWorkingArea()
    {
        var reference = Owner ?? (Control)this;
        var working = Screen.FromControl(reference).WorkingArea;

        MinimumSize = new Size(
            Math.Min(MinimumSize.Width, working.Width),
            Math.Min(MinimumSize.Height, working.Height));

        Size = new Size(
            Math.Min(Width, working.Width),
            Math.Min(Height, working.Height));

        Location = new Point(
            working.Left + Math.Max(0, (working.Width - Width) / 2),
            working.Top + Math.Max(0, (working.Height - Height) / 2));
    }

    private void BtnClearTranslationCache_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show(this,
                "Voulez-vous vider le cache de traduction pour la configuration courante ?",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var removed = _translationService.ClearTranslationCache(BuildCurrentConfig());
        MessageBox.Show(this,
            removed > 0
                ? $"{removed} entrée(s) supprimée(s) du cache de traduction."
                : "Aucune entrée à supprimer dans le cache de traduction.",
            "Cache de traduction",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void BtnClearVerificationCache_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show(this,
                "Voulez-vous vider le cache de vérification pour la configuration courante ?",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var removed = _translationService.ClearVerificationCache(BuildCurrentConfig());
        MessageBox.Show(this,
            removed > 0
                ? $"{removed} entrée(s) supprimée(s) du cache de vérification."
                : "Aucune entrée à supprimer dans le cache de vérification.",
            "Cache de vérification",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private AppConfig BuildCurrentConfig()
    {
        // Preserver les champs de disposition depuis AppConfig.Current : ils ne sont pas
        // edites dans ConfigForm mais font partie de la config persistee. Sans cette copie,
        // un clic sur OK reinitialisait WindowWidth/Height et les ColumnFillWeights du mode
        // inactif a 0 / {} -> perte definitive au redemarrage suivant.
        var current = AppConfig.Current;
        return new AppConfig
        {
            TranslatePrompt = txtTranslatePrompt.Text.Trim(),
            VerifyPrompt = txtVerifyPrompt.Text.Trim(),

            OpenAiKey = txtOpenAiKey.Text.Trim(),
            OpenAiUrl = txtOpenAiUrl.Text.Trim(),
            OpenAiModelName = txtOpenAiModelName.Text.Trim(),

            AnthropicKey = txtAnthropicKey.Text.Trim(),
            AnthropicUrl = txtAnthropicUrl.Text.Trim(),
            AnthropicModelName = txtAnthropicModelName.Text.Trim(),

            BifrostOpenAiKey = txtBifrostOpenAiKey.Text.Trim(),
            BifrostOpenAiUrl = txtBifrostOpenAiUrl.Text.Trim(),
            BifrostOpenAiModelName = txtBifrostOpenAiModelName.Text.Trim(),

            BifrostAnthropicKey = txtBifrostAnthropicKey.Text.Trim(),
            BifrostAnthropicUrl = txtBifrostAnthropicUrl.Text.Trim(),
            BifrostAnthropicModelName = txtBifrostAnthropicModelName.Text.Trim(),

            Provider = SelectedProvider,
            ShowDetails = current.ShowDetails,
            SelectedLanguageCode = current.SelectedLanguageCode,
            WindowWidth = current.WindowWidth,
            WindowHeight = current.WindowHeight,
            ColumnFillWeightsWithDetails = new Dictionary<string, float>(current.ColumnFillWeightsWithDetails, StringComparer.Ordinal),
            ColumnFillWeightsWithoutDetails = new Dictionary<string, float>(current.ColumnFillWeightsWithoutDetails, StringComparer.Ordinal),
        };
    }

        private void InitModelSelectors()
    {
        InitModelSelector(txtOpenAiModelName, txtOpenAiKey, txtOpenAiUrl, AiProvider.OpenAI);
        InitModelSelector(txtAnthropicModelName, txtAnthropicKey, txtAnthropicUrl, AiProvider.Anthropic);
        InitModelSelector(txtBifrostOpenAiModelName, txtBifrostOpenAiKey, txtBifrostOpenAiUrl, AiProvider.BifrostOpenAI);
        InitModelSelector(txtBifrostAnthropicModelName, txtBifrostAnthropicKey, txtBifrostAnthropicUrl, AiProvider.BifrostAnthropic);
    }

    private void InitModelSelector(ComboBox modelBox, TextBox keyBox, TextBox urlBox, AiProvider provider)
    {
        modelBox.DropDownStyle = ComboBoxStyle.DropDown;
        modelBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        modelBox.AutoCompleteSource = AutoCompleteSource.ListItems;

        modelBox.DropDown += async (_, _) => await TryLoadModelsAsync(provider, keyBox, urlBox, modelBox);
        modelBox.MouseDown += async (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                await TryLoadModelsAsync(provider, keyBox, urlBox, modelBox);
        };

        keyBox.TextChanged += (_, _) => _modelsLoaded.Remove(modelBox);
        urlBox.TextChanged += (_, _) => _modelsLoaded.Remove(modelBox);

        modelBox.Items.Clear();
        modelBox.Items.Add(AppConfig.GetDefaultModelName(provider));
    }

    // Les quatre RadioButton vivent dans des containers différents (les panneaux des deux
    // SplitContainer) : WinForms ne gère donc pas l'exclusivité, elle est faite à la main.
    private (RadioButton Button, AiProvider Provider)[] ProviderButtons =>
    [
        (rbOpenAi, AiProvider.OpenAI),
        (rbAnthropic, AiProvider.Anthropic),
        (rbBifrostOpenAi, AiProvider.BifrostOpenAI),
        (rbBifrostAnthropic, AiProvider.BifrostAnthropic),
    ];

    private AiProvider SelectedProvider
    {
        get
        {
            foreach (var (button, provider) in ProviderButtons)
                if (button.Checked)
                    return provider;

            return AiProvider.OpenAI;
        }
    }

    private void InitProviderUi()
    {
        foreach (var (button, _) in ProviderButtons)
            button.CheckedChanged += ProviderChanged;
    }

    private void ProviderChanged(object? sender, EventArgs e)
    {
        if (_isLoading || _isUpdatingProvider)
            return;

        if (sender is not RadioButton changed || !changed.Checked)
            return;

        try
        {
            _isUpdatingProvider = true;
            foreach (var (button, _) in ProviderButtons)
                if (!ReferenceEquals(button, changed))
                    button.Checked = false;
        }
        finally
        {
            _isUpdatingProvider = false;
        }

        ApplyProviderDefaults();
    }

    /// <summary>
    /// Complète l'URL et le modèle du fournisseur sélectionné s'ils sont vides : évite qu'un
    /// fournisseur jamais configuré soit activé sans point de terminaison.
    /// </summary>
    private void ApplyProviderDefaults()
    {
        var provider = SelectedProvider;
        var (urlBox, modelBox) = provider switch
        {
            AiProvider.Anthropic => ((Control)txtAnthropicUrl, (Control)txtAnthropicModelName),
            AiProvider.BifrostOpenAI => (txtBifrostOpenAiUrl, txtBifrostOpenAiModelName),
            AiProvider.BifrostAnthropic => (txtBifrostAnthropicUrl, txtBifrostAnthropicModelName),
            _ => (txtOpenAiUrl, txtOpenAiModelName),
        };

        if (string.IsNullOrWhiteSpace(urlBox.Text))
            urlBox.Text = AppConfig.GetDefaultUrl(provider);

        if (string.IsNullOrWhiteSpace(modelBox.Text))
            modelBox.Text = AppConfig.GetDefaultModelName(provider);
    }

    /// <summary>
    /// Charge la liste des modèles du fournisseur, via le SDK correspondant à son dialecte.
    /// Best effort : en cas d'échec (clé invalide, passerelle éteinte), la liste statique reste.
    /// </summary>
    private Task TryLoadModelsAsync(AiProvider provider, TextBox keyBox, TextBox urlBox, ComboBox modelBox)
        => AppConfig.UsesAnthropicDialect(provider)
            ? TryLoadAnthropicStyleModelsAsync(provider, keyBox, urlBox, modelBox)
            : TryLoadOpenAiStyleModelsAsync(provider, keyBox, urlBox, modelBox);

    /// <summary>
    /// Clé à présenter au SDK pour lister les modèles. Une passerelle Bifrost locale n'en exige
    /// pas, mais les SDK refusent une chaîne vide : on envoie alors un jeton neutre.
    /// </summary>
    private static string? ResolveModelListingKey(AiProvider provider, TextBox keyBox)
    {
        var key = keyBox.Text.Trim();
        if (key.Length > 0)
            return key;

        return AppConfig.IsBifrost(provider) ? AppConfig.BifrostPlaceholderApiKey : null;
    }

    private void ApplyLoadedModels(ComboBox modelBox, object[] modelIds, string currentModel)
    {
        if (!IsHandleCreated)
            return;

        BeginInvoke(() =>
        {
            modelBox.BeginUpdate();
            try
            {
                modelBox.Items.Clear();
                modelBox.Items.AddRange(modelIds);
                modelBox.Text = currentModel;
            }
            finally
            {
                modelBox.EndUpdate();
            }
        });
    }

    private async Task TryLoadOpenAiStyleModelsAsync(AiProvider provider, TextBox keyBox, TextBox urlBox, ComboBox modelBox)
    {
        if (_isLoading || _isLoadingModels || _modelsLoaded.Contains(modelBox))
            return;

        var key = ResolveModelListingKey(provider, keyBox);
        if (key is null)
            return;

        var endpoint = NormalizeOpenAiEndpoint(provider, urlBox.Text);
        var currentModel = modelBox.Text;

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

            _modelsLoaded.Add(modelBox);
            ApplyLoadedModels(modelBox, modelIds, currentModel);
        }
        catch
        {
            // best effort : clé invalide, réseau absent, passerelle éteinte -> liste statique
        }
        finally
        {
            _isLoadingModels = false;
        }
    }

    private async Task TryLoadAnthropicStyleModelsAsync(AiProvider provider, TextBox keyBox, TextBox urlBox, ComboBox modelBox)
    {
        if (_isLoading || _isLoadingModels || _modelsLoaded.Contains(modelBox))
            return;

        var key = ResolveModelListingKey(provider, keyBox);
        if (key is null)
            return;

        var baseUrl = NormalizeAnthropicBaseUrl(provider, urlBox.Text);
        var currentModel = modelBox.Text;

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

            _modelsLoaded.Add(modelBox);
            ApplyLoadedModels(modelBox, modelIds, currentModel);
        }
        catch
        {
            // best effort : si la clé est invalide / pas de réseau / etc., on garde la liste statique
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
        if (p is null)
        {
            // Si le SDK Anthropic / OpenAI renomme la propriete d'ID, la reflexion retourne null
            // silencieusement -> la liste des modeles est vide sans avertissement. On logge un
            // indice pour faciliter le diagnostic lors d'une mise a jour SDK.
            System.Diagnostics.Debug.WriteLine($"[ConfigForm] GetModelId : aucune propriete Id/ID/ModelId/ModelID sur le type '{t.FullName}'. Mise a jour SDK ?");
            return null;
        }
        return p.GetValue(model)?.ToString();
    }

    private static string NormalizeAnthropicBaseUrl(AiProvider provider, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return AppConfig.GetDefaultUrl(provider);

        var trimmed = url.Trim().TrimEnd('/');
        if (trimmed.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^"/v1/messages".Length];
        else if (trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^"/v1".Length];

        return trimmed;
    }

    private static string NormalizeOpenAiEndpoint(AiProvider provider, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return AppConfig.GetDefaultUrl(provider);

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
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigForm] ExtractIds : payload de type '{payload.GetType().FullName}' n'est ni IEnumerable ni n'expose Data/Items. Mise a jour SDK ?");
            yield break;
        }

        bool anyMatch = false;
        foreach (var model in models)
        {
            if (model is null)
                continue;

            var t = model.GetType();
            var p = t.GetProperty("Id") ?? t.GetProperty("ID") ?? t.GetProperty("Model") ?? t.GetProperty("Name");
            if (p is null)
                continue;

            anyMatch = true;
            yield return p.GetValue(model)?.ToString();
        }

        if (!anyMatch)
            System.Diagnostics.Debug.WriteLine("[ConfigForm] ExtractIds : aucun modele n'expose une propriete Id/ID/Model/Name. Mise a jour SDK ?");
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
                  <meta charset="utf-8">
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
            foreach (var (button, provider) in ProviderButtons)
                button.Checked = config.Provider == provider;

            txtOpenAiKey.Text = config.OpenAiKey;
            txtOpenAiUrl.Text = config.OpenAiUrl;
            txtOpenAiModelName.Text = config.OpenAiModelName;

            txtAnthropicKey.Text = config.AnthropicKey;
            txtAnthropicUrl.Text = config.AnthropicUrl;
            txtAnthropicModelName.Text = config.AnthropicModelName;

            txtBifrostOpenAiKey.Text = config.BifrostOpenAiKey;
            txtBifrostOpenAiUrl.Text = config.BifrostOpenAiUrl;
            txtBifrostOpenAiModelName.Text = config.BifrostOpenAiModelName;

            txtBifrostAnthropicKey.Text = config.BifrostAnthropicKey;
            txtBifrostAnthropicUrl.Text = config.BifrostAnthropicUrl;
            txtBifrostAnthropicModelName.Text = config.BifrostAnthropicModelName;
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
        var config = BuildCurrentConfig();
        config.Save();
    }
}

