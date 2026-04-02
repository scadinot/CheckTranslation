namespace CheckTranslation;

internal sealed record MergeRowSnapshot(
    string Project,
    string File,
    string Key,
    string French,
    string FrenchComment,
    string Translation,
    string TranslationComment);
