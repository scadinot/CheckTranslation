using System.Security.Cryptography;
using System.Text.Json;

namespace CheckTranslation;

internal sealed class AppConfig
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "CheckTranslation.config.json");

    private const string DefaultOpenAiUrl = "https://api.openai.com/v1";
    private const string DefaultOpenAiModelName = "gpt-5.2";

    private const string DefaultAnthropicUrl = "https://api.anthropic.com";
    private const string DefaultAnthropicModelName = "claude-3-7-sonnet-latest";

    private const bool DefaultShowDetails = true;

    internal static string GetDefaultUrl(AiProvider provider)
        => provider == AiProvider.Anthropic ? DefaultAnthropicUrl : DefaultOpenAiUrl;

    internal static string GetDefaultModelName(AiProvider provider)
        => provider == AiProvider.Anthropic ? DefaultAnthropicModelName : DefaultOpenAiModelName;

    private const string DefaultTranslatePrompt = """
        Tu es un expert en traduction technique spécialisé en électrotechnique, normes électriques, photovoltaïque (PV) et logiciels industriels.

        **Objectif** : traduire UNIQUEMENT les textes fournis du Français vers {language}.

        ---

        ## Règles obligatoires

        ### 1) Exactitude technique
        - Utiliser la terminologie métier correcte dans {language}.
        - Aucun contresens technique.
        - Respect des termes normatifs.
        - Traduction fidèle, sans ajout ni suppression.
        - Aucune reformulation, aucune explication.

        ### 2) Qualité linguistique
        - Phrase compréhensible et naturelle dans {language}.
        - Pas d'erreur grammaticale.
        - Formulation idiomatique acceptable dans {language}.

        ### 3) Respect strict des abréviations
        - Si le texte source contient une abréviation (ex : « min. », « max. »), la traduction **doit** rester abrégée dans {language}, au même endroit.
        - Si le texte source n'est pas abrégé : traduction complète (ne pas ajouter d'abréviation).
        - **Ne jamais** développer une abréviation.
        - **Ne jamais** créer une abréviation si le texte source n'en contient pas.

        ### 4) Ne jamais traduire (conserver tel quel)
        - Sigles et acronymes techniques : `MPPT`, `PV`, `DC`, `AC`, etc.
        - Unités et notations : `V`, `A`, `%`, `Hz`, `mm²`, `kW`, `kWh`, etc.
        - Symboles : `+`, `–`, `→`, `=`, `/`, etc.
        - Variables entre accolades : `{0}`, `{1}`, `{2}`, …

        ### 5) ⚠️ Références normatives (CRITIQUE)
        - `CEI` **doit** être traduit en `IEC`.
        - Toute référence normative (CEI/NF/EN + numéro + partie + paragraphe/§) doit conserver **STRICTEMENT** :
          - Les numéros (ex : `60364`)
          - Les parties (ex : `-4-41`)
          - Les paragraphes (ex : `§ 411.3.2.2`)
        - **Aucun** élément numérique ne peut être modifié.

        > ✅ Exemple valide : `CEI 60364-4-41 § 411.3.2.2` → `IEC 60364-4-41 § 411.3.2.2`
        > ❌ Exemple invalide : `60364` → `60365`

        ### 6) Préserver exactement
        - Parenthèses, deux-points, points de suspension `…`
        - Retours à la ligne (si présents)
        - La ponctuation et l'ordre des éléments

        ---

        ## FORMAT DE SORTIE OBLIGATOIRE

        - Tu vas recevoir une **liste numérotée**.
        - Réponds avec la **même liste numérotée**.
        - Format strict pour chaque entrée : `N. texte traduit` (numéro, point, espace, texte).
        - **Une ligne par entrée.**
        - **Pas d'en-tête.**
        """;

    private const string DefaultVerifyPrompt = """
        Tu es un expert en traduction technique spécialisé en électrotechnique, normes électriques, photovoltaïque (PV) et logiciels industriels.

        **Objectif** : évaluer la qualité de traductions du Français vers {language}.

        Tu vas recevoir une **liste numérotée de paires source/traduction**.
        Tu dois analyser la qualité technique et linguistique de chaque traduction.

        ---

        ## Critères d'évaluation

        ### 1) Exactitude technique — 40 points
        - Terminologie métier correcte dans {language}.
        - Aucun contresens technique.
        - Respect des termes normatifs.
        - Traduction fidèle, sans ajout ni suppression.
        - Aucune reformulation, aucune explication.

        ### 2) Qualité linguistique — 10 points
        - Phrase compréhensible et naturelle dans {language}.
        - Pas d'erreur grammaticale.
        - Formulation idiomatique acceptable dans {language}.

        ### 3) Respect strict des abréviations — 10 points
        - Si le texte source contient une abréviation (ex : « min. », « max. »), la traduction **doit** rester abrégée, au même endroit.
        - Si le texte source n'est pas abrégé : la traduction **ne doit pas** être abrégée.
        - Aucune abréviation développée.
        - Aucune abréviation créée.

        ### 4) Éléments non traduits — 10 points
        - Sigles (`MPPT`, `PV`, `DC`, `AC`, etc.) non traduits.
        - Unités (`V`, `A`, `%`, `Hz`, `mm²`, `kW`, `kWh`…) inchangées.
        - Variables `{0}`, `{1}`, etc. intactes.
        - Symboles (`+`, `–`, `→`, `=`, `/`, etc.) conservés.

        ### 5) ⚠️ Références normatives — ERREUR CRITIQUE — 20 points
        - `CEI` **doit** être traduit en `IEC`.
        - Toute référence normative (CEI/NF/EN + numéro + partie + paragraphe/§) doit conserver **STRICTEMENT** :
          - Les numéros (ex : `60364`)
          - Les parties (ex : `-4-41`)
          - Les paragraphes (ex : `§ 411.3.2.2`)
        - **Aucun** élément numérique ne peut être modifié.

        > Si un numéro, une partie ou un paragraphe diffère → **erreur critique** → score ≤ `069`.
        > Si `CEI` n'a pas été traduit en `IEC` → **erreur critique** → score ≤ `069`.

        ### 6) Ponctuation et structure — 10 points
        - Parenthèses, deux-points, points de suspension `…` conservés.
        - Retours à la ligne respectés.
        - Ponctuation et ordre des éléments préservés.

        ---

        ## Règles de notation

        - Score global sur **100** par entrée.
        - Être **strict**. Ne pas surnoter.
        - Score minimum `000`, score maximum `100`.

        ---

        ## FORMAT DE SORTIE OBLIGATOIRE

        - Réponds avec la **même liste numérotée**.
        - Format strict pour chaque entrée : `N. XXX - commentaire en français`
          où `XXX` est un score à trois chiffres de `000` à `100`, suivi d'un tiret, et un **court commentaire en français**.

        > Exemple : `1. 085 - Traduction correcte, légère nuance manquante`

        - **Une seule ligne par entrée.**
        - **Pas d'en-tête. Pas de markdown.**
        - Chaque ligne doit être **auto-suffisante** : interdiction de faire référence à une autre entrée
          (pas de « comme au point X », « idem », « voir plus haut », « même remarque », « pareil que… », « cf. ligne… », etc.).
        """;

    public static AppConfig Current { get; private set; } = new();

    public string TranslatePrompt { get; set; } = DefaultTranslatePrompt;
    public string VerifyPrompt { get; set; } = DefaultVerifyPrompt;

    public string OpenAiKey { get; set; } = string.Empty;
    public string OpenAiUrl { get; set; } = DefaultOpenAiUrl;
    public string OpenAiModelName { get; set; } = DefaultOpenAiModelName;

    public string AnthropicKey { get; set; } = string.Empty;
    public string AnthropicUrl { get; set; } = DefaultAnthropicUrl;
    public string AnthropicModelName { get; set; } = DefaultAnthropicModelName;

    public AiProvider Provider { get; set; } = AiProvider.OpenAI;
    public bool ShowDetails { get; set; } = DefaultShowDetails;

    public string Key => Provider == AiProvider.Anthropic ? AnthropicKey : OpenAiKey;
    public string Url => Provider == AiProvider.Anthropic ? AnthropicUrl : OpenAiUrl;
    public string ModelName => Provider == AiProvider.Anthropic ? AnthropicModelName : OpenAiModelName;

    public void Save()
    {
        var dto = new ConfigDto
        {
            TranslatePrompt = TranslatePrompt,
            VerifyPrompt = VerifyPrompt,

            OpenAiKey = EncryptKey(OpenAiKey),
            OpenAiUrl = OpenAiUrl,
            OpenAiModelName = OpenAiModelName,

            AnthropicKey = EncryptKey(AnthropicKey),
            AnthropicUrl = AnthropicUrl,
            AnthropicModelName = AnthropicModelName,

            Provider = Provider.ToString(),
            ShowDetails = ShowDetails,
        };
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
        Current = this;
    }

    public static AppConfig Load()
    {
        if (!File.Exists(FilePath))
            return new AppConfig();

        ConfigDto? dto;
        try
        {
            var json = File.ReadAllText(FilePath);
            dto = JsonSerializer.Deserialize<ConfigDto>(json);
        }
        catch
        {
            return new AppConfig();
        }

        if (dto is null)
            return new AppConfig();

        var provider = Enum.TryParse<AiProvider>(dto.Provider ?? string.Empty, ignoreCase: true, out var p)
            ? p
            : AiProvider.OpenAI;

        var config = new AppConfig
        {
            TranslatePrompt = string.IsNullOrWhiteSpace(dto.TranslatePrompt) ? DefaultTranslatePrompt : dto.TranslatePrompt,
            VerifyPrompt = string.IsNullOrWhiteSpace(dto.VerifyPrompt) ? DefaultVerifyPrompt : dto.VerifyPrompt,

            OpenAiKey = DecryptKey(dto.OpenAiKey ?? dto.Key ?? string.Empty),
            OpenAiUrl = string.IsNullOrWhiteSpace(dto.OpenAiUrl ?? dto.Url) ? DefaultOpenAiUrl : (dto.OpenAiUrl ?? dto.Url)!,
            OpenAiModelName = string.IsNullOrWhiteSpace(dto.OpenAiModelName ?? dto.ModelName) ? DefaultOpenAiModelName : (dto.OpenAiModelName ?? dto.ModelName)!,

            AnthropicKey = DecryptKey(dto.AnthropicKey ?? string.Empty),
            AnthropicUrl = string.IsNullOrWhiteSpace(dto.AnthropicUrl) ? DefaultAnthropicUrl : dto.AnthropicUrl,
            AnthropicModelName = string.IsNullOrWhiteSpace(dto.AnthropicModelName) ? DefaultAnthropicModelName : dto.AnthropicModelName,

            Provider = provider,
            ShowDetails = dto.ShowDetails ?? DefaultShowDetails,
        };

        // Si un des champs a été laissé vide, on applique les valeurs par défaut du provider sélectionné.
        if (string.IsNullOrWhiteSpace(config.OpenAiUrl))
            config.OpenAiUrl = DefaultOpenAiUrl;
        if (string.IsNullOrWhiteSpace(config.OpenAiModelName))
            config.OpenAiModelName = DefaultOpenAiModelName;
        if (string.IsNullOrWhiteSpace(config.AnthropicUrl))
            config.AnthropicUrl = DefaultAnthropicUrl;
        if (string.IsNullOrWhiteSpace(config.AnthropicModelName))
            config.AnthropicModelName = DefaultAnthropicModelName;

        Current = config;
        return config;
    }

    private static string EncryptKey(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    private static string DecryptKey(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            var encrypted = Convert.FromBase64String(encryptedText);
            var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException)
        {
            return encryptedText;
        }
        catch (FormatException)
        {
            return encryptedText;
        }
    }

    private sealed class ConfigDto
    {
        public string? TranslatePrompt { get; set; }
        public string? VerifyPrompt { get; set; }

        // Legacy (OpenAI only)
        public string? Key { get; set; }
        public string? Url { get; set; }
        public string? ModelName { get; set; }

        // Provider specific
        public string? OpenAiKey { get; set; }
        public string? OpenAiUrl { get; set; }
        public string? OpenAiModelName { get; set; }
        public string? AnthropicKey { get; set; }
        public string? AnthropicUrl { get; set; }
        public string? AnthropicModelName { get; set; }

        public string? Provider { get; set; } = nameof(AiProvider.OpenAI);
        public bool? ShowDetails { get; set; } = DefaultShowDetails;
    }
}

