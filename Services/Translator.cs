using System.ClientModel;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using OpenAI;
using OpenAI.Chat;

namespace CheckTranslation;

internal static partial class Translator
{
    private const int BatchSize = 20;
    private const float Temperature = 0.1f;
    private const long AnthropicMaxTokens = 2048;

    public static async Task<string[]> TranslateBatchAsync(IReadOnlyList<string> texts, AppConfig config, string targetLanguage)
    {
        var systemPrompt = config.TranslatePrompt.Replace("{language}", targetLanguage);

        var sb = new StringBuilder();
        for (int i = 0; i < texts.Count; i++)
            sb.AppendLine($"{i + 1}. {texts[i]}");

        var content = await CallApiAsync(systemPrompt, sb.ToString(), config);
        System.Diagnostics.Debug.WriteLine($"[TranslateBatch] Réponse IA :\n{content}");

        return ParseNumberedList(content, texts.Count);
    }

    public static async Task<string[]> VerifyBatchAsync(IReadOnlyList<(string French, string Translation)> pairs, AppConfig config, string targetLanguage)
    {
        var systemPrompt = config.VerifyPrompt.Replace("{language}", targetLanguage);

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
        return config.Provider switch
        {
            AiProvider.OpenAI => await CallOpenAiAsync(systemPrompt, userMessage, config),
            AiProvider.Anthropic => await CallAnthropicAsync(systemPrompt, userMessage, config),
            _ => throw new NotSupportedException($"Provider '{config.Provider}' not supported."),
        };
    }

    private static async Task<string> CallOpenAiAsync(string systemPrompt, string userMessage, AppConfig config)
    {
        var client = new ChatClient(
            config.ModelName,
            new ApiKeyCredential(config.Key),
            new OpenAIClientOptions { Endpoint = new Uri(config.Url) });

        var options = new ChatCompletionOptions
        {
            Temperature = Temperature,
        };

        var result = await client.CompleteChatAsync(
            [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage),
            ],
            options);

        return result.Value.Content[0].Text?.Trim() ?? string.Empty;
    }

    private static async Task<string> CallAnthropicAsync(string systemPrompt, string userMessage, AppConfig config)
    {
        try
        {
            var client = new AnthropicClient
            {
                ApiKey = config.Key,
                BaseUrl = NormalizeAnthropicEndpoint(config.Url),
            };

            var parameters = new MessageCreateParams
            {
                Model = config.ModelName,
                MaxTokens = AnthropicMaxTokens,
                Temperature = Temperature,
                System = new MessageCreateParamsSystem(systemPrompt, null),
                Messages = new List<MessageParam>
                {
                    new()
                    {
                        Role = Role.User,
                        Content = new MessageParamContent(userMessage, null),
                    },
                },
            };

            var message = await client.Messages.Create(parameters);

            var sb = new StringBuilder();
            foreach (var block in message.Content)
                if (block.TryPickText(out var textBlock))
                    sb.Append(textBlock.Text);

            return sb.ToString().Trim();
        }
        catch (AnthropicRateLimitException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException("Quota API atteint (429 Too Many Requests). Vérifiez votre plan ou attendez le reset du quota.", ex);
        }
    }

    private static string NormalizeAnthropicEndpoint(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var trimmed = url.Trim().TrimEnd('/');

        if (trimmed.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^"/v1/messages".Length];
        else if (trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^"/v1".Length];

        return trimmed;
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
