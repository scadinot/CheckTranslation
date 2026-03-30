namespace CheckTranslation;

internal sealed record MergeDifference(
    string SyncKey,
    string Project,
    string File,
    string Key,
    string SourceFrench,
    string DestinationFrench,
    string SourceFrenchComment,
    string DestinationFrenchComment,
    string SourceTranslation,
    string DestinationTranslation,
    string SourceTranslationComment,
    string DestinationTranslationComment);
