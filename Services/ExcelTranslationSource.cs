namespace CheckTranslation;

/// <summary>Source « export Excel ResX Resource Manager » (.xlsx). Seule source à gérer la fusion.</summary>
internal sealed class ExcelTranslationSource(string path) : ITranslationSource
{
    public string Path { get; } = path;

    public string Kind => "Excel";

    public bool SupportsMerge => true;

    public bool SupportsLayoutCheck => false;

    public List<TranslationRow> Load(IReadOnlyList<LanguageInfo> languages, IProgress<SourceLoadProgress>? progress = null)
        => ExcelReader.Load(Path, languages, progress);

    public void Save(IReadOnlyList<TranslationRow> rows, IReadOnlyList<LanguageInfo> languages)
        => ExcelReader.Save(Path, rows, languages);
}
