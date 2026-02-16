namespace CheckTranslation;

/// <summary>
/// Représente une ligne de traduction extraite du fichier Excel ResX Manager.
/// </summary>
internal sealed class TranslationRow
{
    public int RowNumber { get; set; }
    public string French { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public Dictionary<int, string> Translations { get; } = [];

    public void SwitchLanguage(int oldColumn, int newColumn)
    {
        Translations[oldColumn] = Translation;
        Translation = Translations.GetValueOrDefault(newColumn, string.Empty);
    }
}
