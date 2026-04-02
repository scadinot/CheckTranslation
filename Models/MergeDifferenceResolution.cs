namespace CheckTranslation;

internal sealed record MergeDifferenceResolution(
    bool UpdateFrenchAndComment,
    bool UpdateTranslationAndComment)
{
    public bool HasAnyChange => UpdateFrenchAndComment || UpdateTranslationAndComment;
}
