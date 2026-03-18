using System.ClientModel;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using OpenAI;
using OpenAI.Chat;
using Polly;
using Polly.Retry;

namespace CheckTranslation;

internal static partial class Translator
{
    private const int BatchSize = 20;
    private const float Temperature = 0.1f;
    private const long AnthropicMaxTokens = 2048;
    private const int RetryCount = 3;
    private const int FixedParallelBatchRequests = 4;

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

    public static Task<IReadOnlyList<string[]>> VerifyInBatchesAsync(IReadOnlyList<(string French, string Translation)> pairs, AppConfig config, string targetLanguage, IProgress<int>? progress = null)
        => ProcessBatchesAsync(pairs, batch => VerifyBatchAsync(batch, config, targetLanguage), progress);

    public static Task<IReadOnlyList<string[]>> TranslateInBatchesAsync(IReadOnlyList<string> texts, AppConfig config, string targetLanguage, IProgress<int>? progress = null)
        => ProcessBatchesAsync(texts, batch => TranslateBatchAsync(batch, config, targetLanguage), progress);

    private static async Task<IReadOnlyList<string[]>> ProcessBatchesAsync<T>(IReadOnlyList<T> items, Func<IReadOnlyList<T>, Task<string[]>> processBatchAsync, IProgress<int>? progress)
    {
        var batches = Chunk(items);
        var results = new string[batches.Count][];
        int maxParallelRequests = FixedParallelBatchRequests;
        using var throttler = new SemaphoreSlim(maxParallelRequests);
        int done = 0;

        var tasks = batches.Select(async batchInfo =>
        {
            await throttler.WaitAsync();
            try
            {
                var result = await processBatchAsync(batchInfo.Items);
                results[batchInfo.Index] = result;

                var completed = Interlocked.Add(ref done, batchInfo.Items.Count);
                progress?.Report(completed);
            }
            finally
            {
                throttler.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results;
    }

    private static List<(int Index, IReadOnlyList<T> Items)> Chunk<T>(IReadOnlyList<T> items)
    {
        var batches = new List<(int Index, IReadOnlyList<T> Items)>();

        for (int i = 0; i < items.Count; i += BatchSize)
            batches.Add((batches.Count, items.Skip(i).Take(BatchSize).ToArray()));

        return batches;
    }

    private static async Task<string> CallApiAsync(string systemPrompt, string userMessage, AppConfig config)
    {
        var retryPolicy = CreateRetryPolicy();

        return await retryPolicy.ExecuteAsync(async _ =>
        {
            return config.Provider switch
            {
                AiProvider.OpenAI => await CallOpenAiAsync(systemPrompt, userMessage, config),
                AiProvider.Anthropic => await CallAnthropicAsync(systemPrompt, userMessage, config),
                _ => throw new NotSupportedException($"Provider '{config.Provider}' not supported."),
            };
        }, CancellationToken.None);
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
        catch (AnthropicIOException ex)
        {
            throw new HttpRequestException("Erreur d'entrée/sortie lors de l'appel Anthropic. Réessayez avec moins de requêtes parallèles ou vérifiez la stabilité réseau.", ex);
        }
    }

    private static ResiliencePipeline<string> CreateRetryPolicy()
    {
        return new ResiliencePipelineBuilder<string>()
            .AddRetry(new RetryStrategyOptions<string>
            {
                MaxRetryAttempts = RetryCount,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<string>()
                    .Handle<HttpRequestException>(IsTransientHttpException)
                    .Handle<TimeoutException>()
                    .Handle<TaskCanceledException>()
                    .Handle<ClientResultException>(IsTransientClientResultException),
                OnRetry = args =>
                {
                    System.Diagnostics.Debug.WriteLine($"[Translator] Retry {args.AttemptNumber + 1}/{RetryCount} après erreur transitoire.");
                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }

    private static bool IsTransientHttpException(HttpRequestException ex)
    {
        return ex.StatusCode is null
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            or HttpStatusCode.InternalServerError;
    }

    private static bool IsTransientClientResultException(ClientResultException ex)
    {
        return ex.Status switch
        {
            408 or 429 or 500 or 502 or 503 or 504 => true,
            _ => false,
        };
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

