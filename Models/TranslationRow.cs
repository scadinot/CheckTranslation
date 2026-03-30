namespace CheckTranslation;

/// <summary>
/// Représente une ligne de traduction extraite du fichier Excel ResX Manager.
/// </summary>
internal sealed class TranslationRow
{
    public int RowNumber { get; set; }
    public string Project { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string FrenchComment { get; set; } = string.Empty;
    public string French { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public Dictionary<int, string> Translations { get; } = [];
    public Dictionary<int, string> Comments { get; } = [];

    public void SwitchLanguage(int oldColumn, int newColumn)
    {
        Translations[oldColumn] = Translation;
        Translation = Translations.GetValueOrDefault(newColumn, string.Empty);
        Comments[oldColumn] = Comment;
        Comment = Comments.GetValueOrDefault(newColumn, string.Empty);
    }

    public string GetSyncKey()
        => string.Join("\u001F", Project.Trim(), File.Trim(), Key.Trim());
}
