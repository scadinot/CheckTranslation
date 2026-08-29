namespace CheckTranslation;

/// <summary>
/// Source « fichiers .resx » : la solution (.sln / .slnx) désigne les projets, dont les .resx
/// sont lus et réécrits directement. La fusion n'est pas encore disponible pour cette source.
/// </summary>
internal sealed class ResxTranslationSource(string solutionPath) : ITranslationSource
{
    public string Path { get; } = solutionPath;

    public string Kind => "resx";

    public bool SupportsMerge => false;

    public bool SupportsLayoutCheck => true;

    public List<TranslationRow> Load(IReadOnlyList<LanguageInfo> languages, IProgress<SourceLoadProgress>? progress = null)
        => ResxReader.Load(Path, languages, progress);

    public void Save(IReadOnlyList<TranslationRow> rows, IReadOnlyList<LanguageInfo> languages)
        => ResxReader.Save(Path, rows, languages);
}
