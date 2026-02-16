namespace CheckTransation;

/// <summary>
/// Représente une ligne de traduction extraite du fichier Excel ResX Manager.
/// </summary>
internal sealed class TranslationRow
{
    public int RowNumber { get; set; }
    public string Project { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string French { get; set; } = string.Empty;
    public string German { get; set; } = string.Empty;
}
