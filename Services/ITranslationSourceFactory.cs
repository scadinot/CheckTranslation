namespace CheckTranslation;

internal interface ITranslationSourceFactory
{
    /// <summary>
    /// Construit la source correspondant à l'extension du chemin : .xlsx → Excel,
    /// .sln / .slnx → arborescence .resx.
    /// </summary>
    /// <exception cref="NotSupportedException">Extension non reconnue.</exception>
    ITranslationSource Create(string path);

    /// <summary>Filtre prêt à l'emploi pour un <see cref="OpenFileDialog"/>.</summary>
    string OpenFileFilter { get; }
}
