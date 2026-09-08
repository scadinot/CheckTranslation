namespace CheckTranslation;

internal interface ITranslationSourceFactory
{
    /// <summary>
    /// Construit la source correspondant au chemin : .sln / .slnx → arborescence .resx. La
    /// fabrique survit à la source unique : elle garde l'interface à l'écart du choix concret.
    /// </summary>
    /// <exception cref="NotSupportedException">Extension non reconnue.</exception>
    ITranslationSource Create(string path);

    /// <summary>Filtre prêt à l'emploi pour un <see cref="OpenFileDialog"/>.</summary>
    string OpenFileFilter { get; }
}
