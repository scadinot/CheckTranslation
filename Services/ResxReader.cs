using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CheckTranslation;

/// <summary>
/// Lecture / écriture directe des fichiers .resx d'une solution, sans passer par l'export Excel.
///
/// Conventions reproduites à l'identique depuis l'export ResX Resource Manager, pour que les deux
/// sources produisent les mêmes <see cref="TranslationRow"/> (et donc la même clé de corrélation) :
/// <list type="bullet">
/// <item><c>Project</c> = nom du fichier projet sans extension ;</item>
/// <item><c>File</c> = chemin du .resx neutre relatif au projet, sans extension, séparateurs « \ » ;</item>
/// <item>le .resx neutre porte le texte français, les variantes sont suffixées par la culture
/// (<c>Msg.resx</c> → <c>Msg.de-DE.resx</c>) ;</item>
/// <item>les entrées dont le commentaire neutre contient <c>@Invariant</c> sont exclues ;</item>
/// <item>les entrées non textuelles (attribut <c>type</c> ou <c>mimetype</c> : images, icônes,
/// blobs sérialisés) et les métadonnées du designer (clés « &gt;&gt;… ») sont exclues.</item>
/// </list>
/// </summary>
internal static class ResxReader
{
    private const string ResxExtension = ".resx";
    private static readonly string[] IgnoredDirectories = ["bin", "obj", ".git", ".vs"];

    // --- Découverte ---

    /// <summary>
    /// Énumère les fichiers .resx neutres des projets de la solution. La découverte ne lit aucun
    /// XML : elle est partagée par le chargement et la sauvegarde pour garantir la même
    /// correspondance (Projet, Fichier) → chemin disque.
    /// </summary>
    public static List<ResxFileGroup> DiscoverFiles(string solutionPath)
    {
        var groups = new List<ResxFileGroup>();

        foreach (var project in SolutionReader.ReadProjects(solutionPath))
        {
            foreach (var resxPath in EnumerateResxFiles(project.Directory).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                // Une variante de culture n'est pas un fichier neutre : elle est rattachée au sien.
                if (TryGetCulture(resxPath, out _))
                    continue;

                groups.Add(new ResxFileGroup(project.Name, BuildFileIdentity(project.Directory, resxPath), resxPath));
            }
        }

        return groups;
    }

    private static IEnumerable<string> EnumerateResxFiles(string projectDirectory)
    {
        var pending = new Stack<string>();
        pending.Push(projectDirectory);

        while (pending.Count > 0)
        {
            var directory = pending.Pop();

            string[] subDirectories;
            string[] files;
            try
            {
                subDirectories = Directory.GetDirectories(directory);
                files = Directory.GetFiles(directory, "*" + ResxExtension);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine($"[ResxReader] Répertoire ignoré : {directory} ({ex.Message})");
                continue;
            }

            foreach (var subDirectory in subDirectories)
            {
                var name = Path.GetFileName(subDirectory);
                if (!IgnoredDirectories.Contains(name, StringComparer.OrdinalIgnoreCase))
                    pending.Push(subDirectory);
            }

            foreach (var file in files)
                yield return file;
        }
    }

    /// <summary>Chemin relatif au projet, sans extension, séparateurs « \ » (format de l'export Excel).</summary>
    private static string BuildFileIdentity(string projectDirectory, string resxPath)
    {
        var relative = Path.GetRelativePath(projectDirectory, resxPath);
        relative = relative[..^ResxExtension.Length];
        return relative.Replace(Path.DirectorySeparatorChar, '\\').Replace('/', '\\');
    }

    /// <summary>
    /// Détecte le suffixe de culture d'un .resx (<c>Msg.de-DE.resx</c> → « de-DE »). Toute culture
    /// valide est reconnue, pas seulement celles gérées par l'application : c'est ce qui permet de
    /// ne pas confondre une variante non gérée avec un fichier neutre.
    /// </summary>
    private static bool TryGetCulture(string resxPath, out string culture)
    {
        culture = string.Empty;

        var stem = Path.GetFileNameWithoutExtension(resxPath);
        int separator = stem.LastIndexOf('.');
        if (separator < 0)
            return false;

        var candidate = stem[(separator + 1)..];
        if (candidate.Length is < 2 or > 12)
            return false;

        try
        {
            // GetCultureInfo lève pour un nom inconnu ; la culture invariante ("") est écartée
            // par le test de longueur ci-dessus.
            var info = CultureInfo.GetCultureInfo(candidate);
            culture = info.Name;
            return !string.IsNullOrEmpty(culture);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    private static string BuildVariantPath(string neutralPath, string languageCode)
        => neutralPath[..^ResxExtension.Length] + "." + languageCode + ResxExtension;

    // --- Chargement ---

    public static List<TranslationRow> Load(
        string solutionPath,
        IReadOnlyList<LanguageInfo> languages,
        IProgress<SourceLoadProgress>? progress = null)
    {
        var groups = DiscoverFiles(solutionPath);
        var rows = new List<TranslationRow>();

        progress?.Report(new SourceLoadProgress(0, groups.Count));

        for (int i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            var neutralEntries = ReadEntries(group.NeutralPath);

            if (neutralEntries.Count > 0)
            {
                var translationsByLanguage = languages.ToDictionary(
                    language => language.Code,
                    language => ReadEntriesByName(BuildVariantPath(group.NeutralPath, language.Code)),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var entry in neutralEntries)
                {
                    if (entry.Comment.Contains("@Invariant", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var row = new TranslationRow
                    {
                        Project = group.Project,
                        File = group.File,
                        Key = entry.Name,
                        French = entry.Value,
                        FrenchComment = entry.Comment,
                    };

                    foreach (var language in languages)
                    {
                        var found = translationsByLanguage[language.Code].GetValueOrDefault(entry.Name);
                        row.Translations[language.Code] = found?.Value ?? string.Empty;
                        row.Comments[language.Code] = found?.Comment ?? string.Empty;
                    }

                    rows.Add(row);
                }
            }

            int done = i + 1;
            if (done == 1 || done == groups.Count || done % 5 == 0)
                progress?.Report(new SourceLoadProgress(done, groups.Count));
        }

        return rows;
    }

    private static List<ResxEntry> ReadEntries(string path)
    {
        if (!File.Exists(path))
            return [];

        XDocument document;
        try
        {
            document = XDocument.Load(path);
        }
        catch (XmlException ex)
        {
            // Un .resx invalide ne doit pas interrompre le chargement de toute la solution.
            System.Diagnostics.Debug.WriteLine($"[ResxReader] XML invalide, fichier ignoré : {path} ({ex.Message})");
            return [];
        }

        var entries = new List<ResxEntry>();

        foreach (var element in document.Root?.Elements("data") ?? [])
        {
            var name = element.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name) || !IsTranslatable(element, name))
                continue;

            entries.Add(new ResxEntry(
                name,
                element.Element("value")?.Value ?? string.Empty,
                element.Element("comment")?.Value ?? string.Empty));
        }

        return entries;
    }

    private static Dictionary<string, ResxEntry> ReadEntriesByName(string path)
    {
        var byName = new Dictionary<string, ResxEntry>(StringComparer.Ordinal);

        foreach (var entry in ReadEntries(path))
            byName[entry.Name] = entry;

        return byName;
    }

    /// <summary>
    /// Une entrée est traduisible si c'est une chaîne (ni <c>type</c> ni <c>mimetype</c> : ces
    /// attributs signalent une ressource binaire ou sérialisée) et si ce n'est pas une métadonnée
    /// du designer WinForms (clés « &gt;&gt;btnOk.Name », « &gt;&gt;btnOk.Type »…).
    /// </summary>
    private static bool IsTranslatable(XElement element, string name)
        => element.Attribute("type") is null
            && element.Attribute("mimetype") is null
            && !name.StartsWith(">>", StringComparison.Ordinal);

    // --- Sauvegarde ---

    /// <summary>
    /// Réécrit les variantes de culture. Le fichier neutre n'est jamais modifié (le français est
    /// en lecture seule dans l'application). Retourne le nombre de fichiers effectivement écrits.
    /// </summary>
    public static int Save(string solutionPath, IReadOnlyList<TranslationRow> rows, IReadOnlyList<LanguageInfo> languages)
    {
        var neutralPathByIdentity = DiscoverFiles(solutionPath)
            .ToDictionary(group => BuildIdentity(group.Project, group.File), group => group.NeutralPath, StringComparer.OrdinalIgnoreCase);

        int writtenFiles = 0;

        foreach (var fileRows in rows.GroupBy(row => BuildIdentity(row.Project, row.File), StringComparer.OrdinalIgnoreCase))
        {
            if (!neutralPathByIdentity.TryGetValue(fileRows.Key, out var neutralPath))
            {
                System.Diagnostics.Debug.WriteLine($"[ResxReader] Aucun .resx neutre pour « {fileRows.Key} » : lignes ignorées.");
                continue;
            }

            foreach (var language in languages)
            {
                if (SaveLanguageFile(BuildVariantPath(neutralPath, language.Code), neutralPath, language.Code, fileRows))
                    writtenFiles++;
            }
        }

        return writtenFiles;
    }

    private static bool SaveLanguageFile(string variantPath, string neutralPath, string languageCode, IEnumerable<TranslationRow> rows)
    {
        var exists = File.Exists(variantPath);

        // Ne pas créer un fichier de langue pour n'y écrire que du vide.
        if (!exists && !rows.Any(row => HasContent(row, languageCode)))
            return false;

        XDocument document;
        try
        {
            document = exists
                ? XDocument.Load(variantPath, LoadOptions.PreserveWhitespace)
                : CreateEmptyDocumentFrom(neutralPath);
        }
        catch (XmlException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ResxReader] XML invalide, écriture annulée : {variantPath} ({ex.Message})");
            return false;
        }

        var root = document.Root;
        if (root is null)
            return false;

        var elementsByName = root.Elements("data")
            .Where(element => element.Attribute("name") is not null)
            .GroupBy(element => element.Attribute("name")!.Value, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        bool modified = false;

        foreach (var row in rows)
        {
            var value = row.Translations.GetValueOrDefault(languageCode, string.Empty);
            var comment = row.Comments.GetValueOrDefault(languageCode, string.Empty);

            if (elementsByName.TryGetValue(row.Key, out var element))
                modified |= UpdateElement(element, value, comment);
            else if (value.Length > 0 || comment.Length > 0)
            {
                AppendElement(root, row.Key, value, comment);
                modified = true;
            }
        }

        if (!modified)
            return false;

        WriteDocument(document, variantPath, exists);
        return true;
    }

    private static bool HasContent(TranslationRow row, string languageCode)
        => row.Translations.GetValueOrDefault(languageCode, string.Empty).Length > 0
            || row.Comments.GetValueOrDefault(languageCode, string.Empty).Length > 0;

    private static bool UpdateElement(XElement element, string value, string comment)
    {
        bool modified = false;

        var valueElement = element.Element("value");
        if (valueElement is null)
        {
            element.Add(new XElement("value", value));
            modified = true;
        }
        else if (!string.Equals(valueElement.Value, value, StringComparison.Ordinal))
        {
            valueElement.Value = value;
            modified = true;
        }

        var commentElement = element.Element("comment");
        if (comment.Length == 0)
        {
            if (commentElement is not null)
            {
                // Retirer aussi le nœud d'indentation qui précède, sinon une ligne vide subsiste.
                if (commentElement.PreviousNode is XText whitespace && whitespace.Value.Trim().Length == 0)
                    whitespace.Remove();
                commentElement.Remove();
                modified = true;
            }
        }
        else if (commentElement is null)
        {
            (valueElement ?? element.Elements().Last()).AddAfterSelf(new XText("\n    "), new XElement("comment", comment));
            modified = true;
        }
        else if (!string.Equals(commentElement.Value, comment, StringComparison.Ordinal))
        {
            commentElement.Value = comment;
            modified = true;
        }

        return modified;
    }

    private static void AppendElement(XElement root, string key, string value, string comment)
    {
        var element = new XElement("data",
            new XAttribute("name", key),
            new XAttribute(XNamespace.Xml + "space", "preserve"),
            new XText("\n    "),
            new XElement("value", value));

        if (comment.Length > 0)
            element.Add(new XText("\n    "), new XElement("comment", comment));

        element.Add(new XText("\n  "));

        root.Add(new XText("  "), element, new XText("\n"));
    }

    /// <summary>
    /// Construit un document vide pour une nouvelle langue à partir du fichier neutre : on
    /// conserve l'en-tête (déclaration, schéma XSD, resheaders) et on retire toutes les données.
    /// Garantit un .resx valide et conforme aux conventions du projet.
    /// </summary>
    private static XDocument CreateEmptyDocumentFrom(string neutralPath)
    {
        var document = XDocument.Load(neutralPath, LoadOptions.PreserveWhitespace);

        foreach (var element in document.Root?.Elements().Where(e => e.Name.LocalName is "data" or "metadata").ToList() ?? [])
        {
            if (element.PreviousNode is XText whitespace && whitespace.Value.Trim().Length == 0)
                whitespace.Remove();
            element.Remove();
        }

        return document;
    }

    private static void WriteDocument(XDocument document, string path, bool fileExists)
    {
        // Préserver la présence ou l'absence de BOM du fichier d'origine : un basculement
        // produirait un diff sur tout le fichier dans le gestionnaire de sources.
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: fileExists && HasByteOrderMark(path));

        var settings = new XmlWriterSettings
        {
            Encoding = encoding,
            // Indent = false : le document est chargé avec PreserveWhitespace, la mise en forme
            // existante est donc conservée telle quelle (diff minimal).
            Indent = false,
        };

        using var writer = XmlWriter.Create(path, settings);
        document.Save(writer);
    }

    private static bool HasByteOrderMark(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> prefix = stackalloc byte[3];
            return stream.Read(prefix) == 3 && prefix is [0xEF, 0xBB, 0xBF];
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static string BuildIdentity(string project, string file)
        => project.Trim() + "|" + file.Trim();
}

/// <summary>Un fichier .resx neutre et son identité fonctionnelle (projet + chemin relatif).</summary>
internal sealed record ResxFileGroup(string Project, string File, string NeutralPath);

internal sealed record ResxEntry(string Name, string Value, string Comment);
