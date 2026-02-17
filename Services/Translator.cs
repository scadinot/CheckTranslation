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
            + "\nYou will receive a numbered list. Reply with the same numbered list, each line translated. Keep the numbering. Do not add any explanation.";

        var sb = new StringBuilder();
        for (int i = 0; i < texts.Count; i++)
            sb.AppendLine($"{i + 1}. {texts[i]}");

        var content = await CallApiAsync(systemPrompt, sb.ToString(), config);
        return ParseNumberedList(content, texts.Count);
    }

    public static async Task<string> VerifyAsync(string frenchText, string translation, string targetLanguage, AppConfig config)
    {
        var systemPrompt = config.VerifyPrompt.Replace("{language}", targetLanguage);
        var userMessage = $"Texte source (français) :\n{frenchText}\n\nTraduction ({targetLanguage}) :\n{translation}";
        return await CallApiAsync(systemPrompt, userMessage, config);
    }

    public static async Task<string[]> VerifyBatchAsync(
        IReadOnlyList<(string French, string Translation)> pairs,
        AppConfig config,
        string targetLanguage)
    {
        var systemPrompt = config.VerifyPrompt.Replace("{language}", targetLanguage)
            + "\nYou will receive a numbered list of source/translation pairs. Reply with the same numbered list, each line containing your verification comment. Keep the numbering. Do not add any explanation.";

        var sb = new StringBuilder();
        for (int i = 0; i < pairs.Count; i++)
            sb.AppendLine($"{i + 1}. Source: {pairs[i].French} | Traduction: {pairs[i].Translation}");

        var content = await CallApiAsync(systemPrompt, sb.ToString(), config);
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
            model = config.ModelName,
            temperature = 0,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage },
            },
        };

        var json = JsonSerializer.Serialize(requestBody);

        var baseUri = new Uri(config.Url.TrimEnd('/') + "/");
        var endpoint = new Uri(baseUri, "chat/completions");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Key);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()?.Trim() ?? string.Empty;
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

    [GeneratedRegex(@"^(\d+)\.\s*(.+)$")]
    private static partial Regex NumberedLineRegex();
}
