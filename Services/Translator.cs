using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

namespace CheckTranslation;

internal static partial class Translator
{
    private const int BatchSize = 20;
    private const float DefaultTemperature = 0f;

#pragma warning disable OPENAI001

    private static OpenAIClient? _client;
    private static string? _clientKey;
    private static string? _clientEndpoint;

    public static async Task<string> TranslateAsync(string frenchText, AppConfig config, string targetLanguage)
    {
        var systemPrompt = config.TranslatePrompt.Replace("{language}", targetLanguage);

        return await CallApiAsync(systemPrompt, frenchText, config);
    }

    private const string TranslateBatchAsyncInstruction =
        "\n"
        + "FORMAT OBLIGATOIRE : "
        + "Tu vas recevoir une liste numérotée. "
        + "Réponds avec la même liste numérotée. "
        + "Format strict pour chaque entrée : \"1. texte traduit\" (numéro, point, espace, texte) "
        + "Une ligne par entrée. Pas d'en-tête."
        ;

    public static async Task<string[]> TranslateBatchAsync(IReadOnlyList<string> texts, AppConfig config, string targetLanguage)
    {
        var systemPrompt = config.TranslatePrompt.Replace("{language}", targetLanguage) + TranslateBatchAsyncInstruction;

        var sb = new StringBuilder();
        for (int i = 0; i < texts.Count; i++)
            sb.AppendLine($"{i + 1}. {texts[i]}");

        var content = await CallApiAsync(systemPrompt, sb.ToString(), config);
        System.Diagnostics.Debug.WriteLine($"[TranslateBatch] Réponse IA :\n{content}");

        return ParseNumberedList(content, texts.Count);
    }

    private const string VerifyScoreInstruction =
        "\n"
        + "FORMAT OBLIGATOIRE : "
        + "Ta réponse entière doit utiliser le format exact : \"XXX - commentaire\" où XXX est un score à trois chiffres de 000 à 100, suivi d'un tiret, et un court commentaire en français. "
        + "Exemple : \"085 - Traduction correcte, légère nuance manquante\". "
        + "Rien d'autre. Pas de markdown."
        ;

    private const string VerifyBatchAsyncInstruction =
        "\n"
        + "Tu vas recevoir une liste numérotée de paires source/traduction. "
        + "Interdiction de faire référence à une autre entrée (pas de « comme au point X », « idem », « voir plus haut », « même remarque », « pareil que… », « cf. ligne… », etc.). "
        + "Chaque ligne de sortie doit être auto-suffisante et ne dépendre d’aucun contexte hors de l’entrée correspondante. "
        + "Réponds avec la même liste numérotée. "
        + "Format strict pour chaque entrée : \"N. XXX - commentaire en français\". "
        + "Une seule ligne par entrée. Pas d'en-tête. "
        ;

    public static async Task<string> VerifyAsync(string frenchText, string translation, string targetLanguage, AppConfig config)
    {
        var systemPrompt = config.VerifyPrompt.Replace("{language}", targetLanguage) + VerifyScoreInstruction;
        var userMessage = $"Texte source (français) :\n{frenchText}\n\nTraduction ({targetLanguage}) :\n{translation}";

        return await CallApiAsync(systemPrompt, userMessage, config);
    }

    public static async Task<string[]> VerifyBatchAsync(IReadOnlyList<(string French, string Translation)> pairs, AppConfig config, string targetLanguage)
    {
        var systemPrompt = config.VerifyPrompt.Replace("{language}", targetLanguage) + VerifyScoreInstruction + VerifyBatchAsyncInstruction;

        var sb = new StringBuilder();
        for (int i = 0; i < pairs.Count; i++)
            sb.AppendLine($"{i + 1}. Source: {pairs[i].French} | Traduction: {pairs[i].Translation}");

        var content = await CallApiAsync(systemPrompt, sb.ToString(), config);
        System.Diagnostics.Debug.WriteLine($"[VerifyBatch] Réponse IA :\n{content}");

        return ParseNumberedList(content, pairs.Count);
    }

    public static async Task<IReadOnlyList<string[]>> VerifyInBatchesAsync(IReadOnlyList<(string French, string Translation)> pairs, AppConfig config, string targetLanguage, IProgress<int>? progress = null)
    {
        var results = new List<string[]>();
        int done = 0;

        for (int i = 0; i < pairs.Count; i += BatchSize)
        {
            var batch = pairs.Skip(i).Take(BatchSize).ToList();
            var verified = await VerifyBatchAsync(batch, config, targetLanguage);
            results.Add(verified);
            done += batch.Count;
            progress?.Report(done);
        }

        return results;
    }

    public static async Task<IReadOnlyList<string[]>> TranslateInBatchesAsync(IReadOnlyList<string> texts, AppConfig config, string targetLanguage, IProgress<int>? progress = null)
    {
        var results = new List<string[]>();
        int done = 0;

        for (int i = 0; i < texts.Count; i += BatchSize)
        {
            var batch = texts.Skip(i).Take(BatchSize).ToList();
            var translated = await TranslateBatchAsync(batch, config, targetLanguage);
            results.Add(translated);
            done += batch.Count;
            progress?.Report(done);
        }

        return results;
    }

    private static async Task<string> CallApiAsync(string systemPrompt, string userMessage, AppConfig config)
    {
        try
        {
            var client = GetClient(config);
            var responses = client.GetResponsesClient(config.ModelName);

            var options = new CreateResponseOptions
            {
                Instructions = systemPrompt,
                Temperature = DefaultTemperature,
            };

            options.InputItems.Add(ResponseItem.CreateUserMessageItem(userMessage));

            var result = await responses.CreateResponseAsync(options);
            var responseJson = result.GetRawResponse().Content.ToString();
            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.TryGetProperty("output_text", out var outText))
                return outText.GetString()?.Trim() ?? string.Empty;

            // fallback (si output_text absent)
            if (doc.RootElement.TryGetProperty("output", out var output))
            {
                foreach (var item in output.EnumerateArray())
                    if (item.TryGetProperty("content", out var contentArr))
                        foreach (var c in contentArr.EnumerateArray())
                            if (c.TryGetProperty("text", out var txt))
                                return txt.GetString()?.Trim() ?? string.Empty;
            }

            return string.Empty;
        }
        catch (ClientResultException ex) when (ex.Status == 429)
        {
            throw new HttpRequestException("Quota API atteint (429 Too Many Requests). Vérifiez votre plan ou attendez le reset du quota.", ex);
        }
    }

    private static OpenAIClient GetClient(AppConfig config)
    {
        var endpoint = NormalizeEndpoint(config.Url);
        if (_client is not null && _clientKey == config.Key && _clientEndpoint == endpoint)
            return _client;

        var options = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(endpoint))
            options.Endpoint = new Uri(endpoint);

        _client = new OpenAIClient(new ApiKeyCredential(config.Key), options);
        _clientKey = config.Key;
        _clientEndpoint = endpoint;
        return _client;
    }

    private static string NormalizeEndpoint(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var trimmed = url.Trim().TrimEnd('/');

        if (trimmed.EndsWith("/v1/responses", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^"/responses".Length];

        if (trimmed.EndsWith("/v1/chat/completions", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^"/chat/completions".Length];

        if (trimmed.EndsWith("/responses", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^"/responses".Length];

        return trimmed.TrimEnd('/') + "/";
    }

    private static string[] ParseNumberedList(string content, int expectedCount)
    {
        var results = new string[expectedCount];
        var lines = content.Split('\n');
        int currentIndex = -1;
        var currentText = new StringBuilder();

        foreach (var line in lines)
        {
            var match = NumberedLineRegex().Match(line);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int index)
                && index >= 1 && index <= expectedCount)
            {
                if (currentIndex >= 0)
                    results[currentIndex] = currentText.ToString().Trim();

                currentIndex = index - 1;
                currentText.Clear();
                currentText.Append(match.Groups[2].Value.Trim());
            }
            else if (currentIndex >= 0 && !string.IsNullOrWhiteSpace(line))
            {
                currentText.Append(' ').Append(line.Trim());
            }
        }

        if (currentIndex >= 0)
            results[currentIndex] = currentText.ToString().Trim();

        return results;
    }

    [GeneratedRegex(@"^\s*(\d+)[.)]\s*(.+)$")]
    private static partial Regex NumberedLineRegex();
}
