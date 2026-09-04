using System.ClientModel;
using System.Collections.Concurrent;
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
    internal const int BatchSize = 20;
    private const float Temperature = 0.1f;
    // Plafond de SORTIE, pas un coût : seul le texte effectivement produit est facturé. À 2048,
    // l'extraction de termes (JSON verbeux : terme + traduction + contexte par entrée) était
    // tronquée dès un lot de 20 textes — tableau jamais fermé, parseur muet, zéro candidat.
    // Une traduction de 20 libellés longs pouvait subir le même sort en silence.
    private const long AnthropicMaxTokens = 8192;
    private const int RetryCount = 3;
    private const int FixedParallelBatchRequests = 4;

    // Le pipeline Polly est stateless : une seule instance partagée pour toutes les invocations
    // (plus d'allocation par appel).
    private static readonly ResiliencePipeline<string> RetryPipeline = BuildRetryPipeline();

    // Les modèles récents (Claude Sonnet 5 et au-delà) refusent le paramètre temperature —
    // Bedrock transforme la dépréciation en erreur de validation. Plutôt qu'une liste à
    // maintenir, l'application apprend : au premier refus, le couple fournisseur-modèle est
    // mémorisé ici et l'appel rejoué sans température ; les suivants ne l'envoient plus.
    // Les modèles qui l'acceptent continuent de la recevoir (0.1 stabilise les traductions).
    private static readonly ConcurrentDictionary<string, bool> TemperatureRejectedByModel = new(StringComparer.OrdinalIgnoreCase);

    private static string TemperatureKey(AppConfig config)
        => string.Join("\u001F", config.Provider, config.Url, config.ModelName);

    private static bool SendsTemperature(AppConfig config)
        => !TemperatureRejectedByModel.ContainsKey(TemperatureKey(config));

    /// <summary>
    /// Reconnaît le refus du paramètre temperature dans un message d'erreur, quel que soit le
    /// dialecte : Bedrock répond « `temperature` is deprecated for this model », d'autres
    /// variantes disent « not supported ». Le contrôle du nom du paramètre évite de confondre
    /// avec un refus d'un autre paramètre.
    /// </summary>
    private static bool IsTemperatureRejection(string? message)
        => message is not null
            && message.Contains("temperature", StringComparison.OrdinalIgnoreCase)
            && (message.Contains("deprecated", StringComparison.OrdinalIgnoreCase)
                || message.Contains("not supported", StringComparison.OrdinalIgnoreCase)
                || message.Contains("unsupported", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Mémorise le refus. Le rejeu ne dépend PAS du résultat : sous appels parallèles, plusieurs
    /// requêtes envoient la température avant que le refus soit mémorisé, et chacune doit être
    /// rejouée — pas seulement celle qui gagne le TryAdd, sinon les autres feraient tomber tout
    /// le Task.WhenAll du batch. L'anti-boucle est ailleurs : le rejeu n'envoie plus la
    /// température, son propre filtre d'exception ne peut donc plus correspondre.
    /// </summary>
    private static void MarkTemperatureRejected(AppConfig config)
        => TemperatureRejectedByModel.TryAdd(TemperatureKey(config), true);

    public static async Task<string[]> TranslateBatchAsync(IReadOnlyList<string> texts, AppConfig config, string targetLanguage, string glossarySection)
    {
        var systemPrompt = config.TranslatePrompt
            .Replace("{language}", targetLanguage)
            .Replace("{glossary}", glossarySection ?? string.Empty);

        var sb = new StringBuilder();
        for (int i = 0; i < texts.Count; i++)
            sb.AppendLine($"{i + 1}. {texts[i]}");

        var content = await CallApiAsync(systemPrompt, sb.ToString(), config);
        System.Diagnostics.Debug.WriteLine($"[TranslateBatch] Réponse IA :\n{content}");

        return ParseNumberedList(content, texts.Count);
    }

    public static async Task<string[]> VerifyBatchAsync(IReadOnlyList<(string French, string Translation)> pairs, AppConfig config, string targetLanguage, string glossarySection)
    {
        var systemPrompt = config.VerifyPrompt
            .Replace("{language}", targetLanguage)
            .Replace("{glossary}", glossarySection ?? string.Empty);

        var sb = new StringBuilder();
        for (int i = 0; i < pairs.Count; i++)
            sb.AppendLine($"{i + 1}. Source : {pairs[i].French} | Traduction : {pairs[i].Translation}");

        var content = await CallApiAsync(systemPrompt, sb.ToString(), config);
        System.Diagnostics.Debug.WriteLine($"[VerifyBatch] Réponse IA :\n{content}");

        return ParseNumberedList(content, pairs.Count);
    }

    public static Task<IReadOnlyList<string[]>> VerifyInBatchesAsync(IReadOnlyList<(string French, string Translation)> pairs, AppConfig config, string targetLanguage, string glossarySection, IProgress<int>? progress = null, Action<IReadOnlyList<(string French, string Translation)>, string[]>? onBatchCompleted = null)
        => ProcessBatchesAsync(pairs, batch => VerifyBatchAsync(batch, config, targetLanguage, glossarySection), progress, onBatchCompleted);

    public static Task<IReadOnlyList<string[]>> TranslateInBatchesAsync(IReadOnlyList<string> texts, AppConfig config, string targetLanguage, string glossarySection, IProgress<int>? progress = null, Action<IReadOnlyList<string>, string[]>? onBatchCompleted = null)
        => ProcessBatchesAsync(texts, batch => TranslateBatchAsync(batch, config, targetLanguage, glossarySection), progress, onBatchCompleted);

    private static async Task<IReadOnlyList<string[]>> ProcessBatchesAsync<T>(IReadOnlyList<T> items, Func<IReadOnlyList<T>, Task<string[]>> processBatchAsync, IProgress<int>? progress, Action<IReadOnlyList<T>, string[]>? onBatchCompleted = null)
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

                onBatchCompleted?.Invoke(batchInfo.Items, result);

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

    internal static async Task<string> CallApiAsync(string systemPrompt, string userMessage, AppConfig config)
    {
        return await RetryPipeline.ExecuteAsync(async _ =>
        {
            // Bifrost expose un chemin par dialecte : on réutilise donc tel quel le client du
            // dialecte correspondant, seule l'URL de base change.
            return config.Provider switch
            {
                AiProvider.OpenAI or AiProvider.BifrostOpenAI => await CallOpenAiAsync(systemPrompt, userMessage, config),
                AiProvider.Anthropic or AiProvider.BifrostAnthropic => await CallAnthropicAsync(systemPrompt, userMessage, config),
                _ => throw new NotSupportedException($"Provider '{config.Provider}' not supported."),
            };
        }, CancellationToken.None);
    }

    private static async Task<string> CallOpenAiAsync(string systemPrompt, string userMessage, AppConfig config)
    {
        var client = new ChatClient(
            config.ModelName,
            new ApiKeyCredential(ResolveApiKey(config)),
            new OpenAIClientOptions { Endpoint = new Uri(config.Url) });

        bool sentTemperature = SendsTemperature(config);
        var options = new ChatCompletionOptions();
        if (sentTemperature)
            options.Temperature = Temperature;

        try
        {
            var result = await client.CompleteChatAsync(
                [
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userMessage),
                ],
                options);

            return result.Value.Content[0].Text?.Trim() ?? string.Empty;
        }
        catch (ClientResultException ex) when (sentTemperature && IsTemperatureRejection(ex.Message))
        {
            // Cet appel a envoyé la température et le modèle l'a refusée : rejouer sans elle.
            // Le rejeu part avec sentTemperature à false, son filtre ne peut plus correspondre.
            MarkTemperatureRejected(config);
            return await CallOpenAiAsync(systemPrompt, userMessage, config);
        }
    }

    private static async Task<string> CallAnthropicAsync(string systemPrompt, string userMessage, AppConfig config)
    {
        bool sentTemperature = SendsTemperature(config);

        try
        {
            var client = new AnthropicClient
            {
                ApiKey = ResolveApiKey(config),
                BaseUrl = NormalizeAnthropicEndpoint(config.Url),
            };

            var parameters = new MessageCreateParams
            {
                Model = config.ModelName,
                MaxTokens = AnthropicMaxTokens,
                Temperature = sentTemperature ? Temperature : null,
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
        catch (Exception ex) when (sentTemperature && IsTemperatureRejection(ex.Message))
        {
            // Cet appel a envoyé la température et le modèle l'a refusée : rejouer sans elle
            // (même logique que le dialecte OpenAI). Le rejeu part avec sentTemperature à false,
            // son filtre ne peut plus correspondre — pas de boucle possible.
            MarkTemperatureRejected(config);
            return await CallAnthropicAsync(systemPrompt, userMessage, config);
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

    private static ResiliencePipeline<string> BuildRetryPipeline()
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

    /// <summary>
    /// Clé transmise au SDK. Une instance Bifrost locale n'exige pas de clé — les clés des
    /// fournisseurs amont vivent côté passerelle — mais les deux SDK refusent une chaîne vide :
    /// on leur passe alors un jeton neutre, que la passerelle ignore.
    ///
    /// Hors Bifrost, aucune clé n'est fabriquée : on renvoie une chaîne vide pour que l'appel
    /// échoue localement et explicitement, plutôt que de partir sur le réseau avec un jeton
    /// bidon ou une valeur faite d'espaces.
    /// </summary>
    private static string ResolveApiKey(AppConfig config)
    {
        var key = config.Key?.Trim() ?? string.Empty;
        if (key.Length > 0)
            return key;

        return AppConfig.IsBifrost(config.Provider) ? AppConfig.BifrostPlaceholderApiKey : string.Empty;
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

    // internal (et non private) : candidat de test prioritaire du §11 de CLAUDE.md — le parsing
    // des réponses IA est le point le plus exposé aux régressions silencieuses.
    internal static string[] ParseNumberedList(string content, int expectedCount)
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

