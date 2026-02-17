using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CheckTranslation;

internal static class Translator
{
    private static readonly HttpClient HttpClient = new();

    public static async Task<string> TranslateAsync(string frenchText, AppConfig config, string targetLanguage)
    {
        var systemPrompt = config.Prompt.Replace("{language}", targetLanguage);

        var requestBody = new
        {
            model = config.ModelName,
            temperature = 0,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = frenchText },
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
}
