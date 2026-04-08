namespace CheckTranslation;

internal sealed class Glossary
{
    public Dictionary<string, List<GlossaryEntry>> EntriesByLanguage { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
