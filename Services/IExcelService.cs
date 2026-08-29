namespace CheckTranslation;

/// <summary>
/// Opérations Excel qui ne relèvent pas de <see cref="ITranslationSource"/> : la fusion d'un
/// classeur source vers un classeur destination, disponible uniquement pour la source Excel.
/// </summary>
internal interface IExcelService
{
    List<MergeDifference> GetMergeSourceDifferences(string destinationFilePath, LanguageInfo activeLanguage, IReadOnlyList<TranslationRow> rows);
    int Merge(string destinationFilePath, LanguageInfo activeLanguage, IReadOnlyList<TranslationRow> rows);
    int Merge(string destinationFilePath, LanguageInfo activeLanguage, IReadOnlyList<TranslationRow> rows, IReadOnlyDictionary<string, MergeDifferenceResolution> sourceDifferenceResolutions);
}
