using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CheckTranslation;

internal static partial class Translator
{
    private const int BatchSize = 20;
    private static readonly HttpClient HttpClient = new();

    public static async Task<string> TranslateAsync(string frenchText, AppConfig config, string targetLanguage)
    {
        var systemPrompt = config.TranslatePrompt.Replace("{language}", targetLanguage);
        return await CallApiAsync(systemPrompt, frenchText, config);
    }

    public static async Task<string[]> TranslateBatchAsync(IReadOnlyList<string> texts, AppConfig config, string targetLanguage)
    {
        var systemPrompt = config.TranslatePrompt.Replace("{language}", targetLanguage)
            + "\nIMPORTANT FORMAT: You will receive a numbered list. Reply ONLY with the same numbered list, each line translated."
            + " Use the exact format \"1. translated text\" (number, dot, space, text). One line per item. No headers, no explanation, no markdown.";

        var sb = new StringBuilder();
        for (int i = 0; i < texts.Count; i++)
            sb.AppendLine($"{i + 1}. {texts[i]}");

        var content = await CallApiAsync(systemPrompt, sb.ToString(), config);
        System.Diagnostics.Debug.WriteLine($"[TranslateBatch] Réponse IA :\n{content}");
        return ParseNumberedList(content, texts.Count);
    }

    private const string VerifyScoreInstruction =
        "\nFORMAT OBLIGATOIRE : Ta réponse entière doit utiliser le format exact : \"XXX/100 - commentaire\" où XXX est un score à trois chiffres de 000 à 100, suivi d'un slash, 100, un tiret, et un court commentaire en français. Exemple : \"085/100 - Traduction correcte, légère nuance manquante\". Rien d'autre. Pas de markdown.";

    public static async Task<string> VerifyAsync(string frenchText, string translation, string targetLanguage, AppConfig config)
    {
        var systemPrompt = config.VerifyPrompt.Replace("{language}", targetLanguage) + VerifyScoreInstruction;
        var userMessage = $"Texte source (français) :\n{frenchText}\n\nTraduction ({targetLanguage}) :\n{translation}";
        return await CallApiAsync(systemPrompt, userMessage, config);
    }

    public static async Task<string[]> VerifyBatchAsync(
        IReadOnlyList<(string French, string Translation)> pairs,
        AppConfig config,
        string targetLanguage)
    {
        var systemPrompt = config.VerifyPrompt.Replace("{language}", targetLanguage) + VerifyScoreInstruction
            + "\nTu vas recevoir une liste numérotée de paires source/traduction."
            + " Réponds avec la même liste numérotée. Chaque entrée : \"N. XXX/100 - commentaire en français\". Une ligne par entrée. Pas d'en-tête.";

        var sb = new StringBuilder();
        for (int i = 0; i < pairs.Count; i++)
            sb.AppendLine($"{i + 1}. Source: {pairs[i].French} | Traduction: {pairs[i].Translation}");

        var content = await CallApiAsync(systemPrompt, sb.ToString(), config);
        System.Diagnostics.Debug.WriteLine($"[VerifyBatch] Réponse IA :\n{content}");
        return ParseNumberedList(content, pairs.Count);
    }

    public static async Task<IReadOnlyList<string[]>> VerifyInBatchesAsync(
        IReadOnlyList<(string French, string Translation)> pairs,
        AppConfig config,
        string targetLanguage,
        IProgress<int>? progress = null)
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

    public static async Task<IReadOnlyList<string[]>> TranslateInBatchesAsync(
        IReadOnlyList<string> texts,
        AppConfig config,
        string targetLanguage,
        IProgress<int>? progress = null)
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
        var requestBody = new
        {
            model = config.ModelName, // "gpt-5-mini"
            input = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage },
            }
        };

        var json = JsonSerializer.Serialize(requestBody);

        var baseUri = new Uri(config.Url.TrimEnd('/') + "/");
        var endpoint = new Uri(baseUri, "responses");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Key);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await HttpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            throw new HttpRequestException("Quota API atteint (429 Too Many Requests). Vérifiez votre plan ou attendez le reset du quota.");

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
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
