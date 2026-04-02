namespace CheckTranslation;

internal sealed record MergeDifference(
    string SyncKey,
    MergeRowSnapshot Source,
    MergeRowSnapshot Destination);
