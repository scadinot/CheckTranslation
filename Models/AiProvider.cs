namespace CheckTranslation;

internal enum AiProvider
{
    /// <summary>API OpenAI en direct.</summary>
    OpenAI,

    /// <summary>API Anthropic en direct.</summary>
    Anthropic,

    /// <summary>Passerelle Bifrost, via son chemin compatible OpenAI (<c>/openai/v1</c>).</summary>
    BifrostOpenAI,

    /// <summary>Passerelle Bifrost, via son chemin compatible Anthropic (<c>/anthropic</c>).</summary>
    BifrostAnthropic,
}
