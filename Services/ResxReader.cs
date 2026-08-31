using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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
internal static partial class ResxReader
{
    private const string ResxExtension = ".resx";
    private static readonly string[] IgnoredDirectories = ["bin", "obj", ".git", ".vs"];

    private static readonly HashSet<string> KnownCultureNames = new(
        CultureInfo.GetCultures(CultureTypes.AllCultures).Select(culture => culture.Name),
        StringComparer.OrdinalIgnoreCase);

    // --- Découverte ---

    /// <summary>
    /// Énumère les fichiers .resx neutres des projets de la solution. La découverte ne lit aucun
    /// XML : elle est partagée par le chargement et la sauvegarde pour garantir la même
    /// correspondance (Projet, Fichier) → chemin disque.
    /// </summary>
    public static List<ResxFileGroup> DiscoverFiles(string solutionPath)
        => DiscoverFiles(solutionPath, out _);

    /// <summary>
    /// Même découverte, en conservant de quoi expliquer un résultat vide (voir
    /// <see cref="ResxDiscovery"/>).
    /// </summary>
    public static List<ResxFileGroup> DiscoverFiles(string solutionPath, out ResxDiscovery discovery)
    {
        var scan = SolutionReader.Scan(solutionPath);
        var groups = new List<ResxFileGroup>();
        var projectsWithoutResx = new List<string>();

        foreach (var project in scan.Projects)
        {
            int before = groups.Count;

            foreach (var resxPath in EnumerateResxFiles(project.Directory).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                // Une variante de culture n'est pas un fichier neutre : elle est rattachée au sien.
                if (TryGetCulture(resxPath, out _))
                    continue;

                groups.Add(new ResxFileGroup(project.Name, BuildFileIdentity(project.Directory, resxPath), resxPath));
            }

            if (groups.Count == before)
                projectsWithoutResx.Add(project.Name);
        }

        discovery = new ResxDiscovery(scan, projectsWithoutResx);
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
    ///
    /// La validation ne peut PAS reposer sur l'exception de <see cref="CultureInfo.GetCultureInfo(string)"/> :
    /// sous ICU (le défaut depuis .NET 5, Windows compris), cette méthode ne lève pas pour un nom
    /// quelconque bien formé — « Designer », « Backup », « resources » renvoient une culture
    /// « custom ». S'y fier ferait passer n'importe quel suffixe pour une culture, et le fichier
    /// neutre correspondant disparaîtrait silencieusement de la découverte.
    ///
    /// On valide donc contre la liste des cultures connues du système, en tolérant en second
    /// recours les variantes absentes de cette liste mais syntaxiquement valides — ICU ne liste
    /// pas <c>ca-ES-valencia</c>, par exemple. Aucune borne de longueur : elle rejetterait ces
    /// mêmes noms longs et pourtant valides.
    /// </summary>
    private static bool TryGetCulture(string resxPath, out string culture)
    {
        culture = string.Empty;

        var stem = Path.GetFileNameWithoutExtension(resxPath);
        int separator = stem.LastIndexOf('.');
        if (separator < 0)
            return false;

        var candidate = stem[(separator + 1)..];
        if (candidate.Length < 2)
            return false;

        if (KnownCultureNames.Contains(candidate))
        {
            culture = candidate;
            return true;
        }

        // Le suffixe doit au minimum avoir la forme d'une culture composée (langue + au moins une
        // sous-étiquette) : un mot isolé comme « Backup » n'en est pas une.
        if (!CultureShapeRegex().IsMatch(candidate))
            return false;

        try
        {
            var info = CultureInfo.GetCultureInfo(candidate);
            culture = info.Name;
            return culture.Length > 0;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    // Forme BCP-47 : sous-étiquette de langue de 2 ou 3 lettres, suivie de 1 à 3 sous-étiquettes.
    [GeneratedRegex("^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8}){1,3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CultureShapeRegex();

    private static string BuildVariantPath(string neutralPath, string languageCode)
        => neutralPath[..^ResxExtension.Length] + "." + languageCode + ResxExtension;

    // --- Chargement ---

    public static List<TranslationRow> Load(
        string solutionPath,
        IReadOnlyList<LanguageInfo> languages,
        IProgress<SourceLoadProgress>? progress = null)
        => Load(solutionPath, languages, progress, out _);

    /// <summary>
    /// Même chargement, en produisant un compte rendu de ce qui a été parcouru. Un chargement qui
    /// ne ramène aucune ligne peut venir de trois endroits très différents — aucun projet reconnu,
    /// aucun .resx sous les projets, ou toutes les entrées exclues — et l'utilisateur ne peut pas
    /// les distinguer d'une grille vide.
    /// </summary>
    public static List<TranslationRow> Load(
        string solutionPath,
        IReadOnlyList<LanguageInfo> languages,
        IProgress<SourceLoadProgress>? progress,
        out ResxLoadReport report)
    {
        var groups = DiscoverFiles(solutionPath, out var discovery);
        var rows = new List<TranslationRow>();
        var stats = new ResxReadStats();
        int invariantEntries = 0;
        int emptyNeutralFiles = 0;

        progress?.Report(new SourceLoadProgress(0, groups.Count));

        for (int i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            var neutralEntries = ReadEntries(group.NeutralPath, stats);

            if (neutralEntries.Count == 0)
                emptyNeutralFiles++;
            else
            {
                var translationsByLanguage = languages.ToDictionary(
                    language => language.Code,
                    language => ReadEntriesByName(BuildVariantPath(group.NeutralPath, language.Code)),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var entry in neutralEntries)
                {
                    if (entry.Comment.Contains("@Invariant", StringComparison.OrdinalIgnoreCase))
                    {
                        invariantEntries++;
                        continue;
                    }

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

        report = new ResxLoadReport(discovery, groups.Count, emptyNeutralFiles, stats, invariantEntries, rows.Count);
        return rows;
    }

    private static List<ResxEntry> ReadEntries(string path, ResxReadStats? stats = null)
    {
        if (!File.Exists(path))
        {
            // Pour une variante de culture (stats non fournies), l'absence est le cas normal.
            // Pour un fichier neutre, elle veut dire qu'il a disparu entre la découverte et la
            // lecture : le compter à part, sinon le compte rendu le rangerait parmi les fichiers
            // « sans entrée traduisible » et désignerait la mauvaise cause.
            if (stats is not null)
            {
                System.Diagnostics.Debug.WriteLine($"[ResxReader] Fichier disparu depuis la découverte : {path}");
                stats.MissingFiles++;
            }

            return [];
        }

        XDocument document;
        try
        {
            document = XDocument.Load(path);
        }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            // Ni un .resx invalide ni un fichier inaccessible (verrouillé par Visual Studio, droits
            // insuffisants, chemin trop long) ne doivent interrompre le chargement de toute la
            // solution : le fichier est ignoré, le reste est chargé.
            System.Diagnostics.Debug.WriteLine($"[ResxReader] Fichier ignoré au chargement : {path} ({ex.GetType().Name} : {ex.Message})");
            if (stats is not null)
                stats.UnreadableFiles++;
            return [];
        }

        var entries = new List<ResxEntry>();

        foreach (var element in Children(document.Root, "data"))
        {
            if (stats is not null)
                stats.DataElements++;

            var name = Attribute(element, "name");

            if (string.IsNullOrEmpty(name))
                continue;

            if (name.StartsWith(">>", StringComparison.Ordinal))
            {
                if (stats is not null)
                    stats.DesignerMetadata++;
                continue;
            }

            if (Attribute(element, "type") is not null || Attribute(element, "mimetype") is not null)
            {
                if (stats is not null)
                    stats.BinaryEntries++;
                continue;
            }

            entries.Add(new ResxEntry(
                name,
                Children(element, "value").FirstOrDefault()?.Value ?? string.Empty,
                Children(element, "comment").FirstOrDefault()?.Value ?? string.Empty));
        }

        return entries;
    }

    /// <summary>
    /// Éléments fils portant ce nom local, quel que soit l'espace de noms.
    ///
    /// Un <c>.resx</c> n'en déclare normalement aucun, mais rien ne l'interdit — et un filtre sur
    /// le nom qualifié ne ramènerait alors plus rien, sans la moindre erreur : le fichier
    /// paraîtrait simplement vide de traductions. Le nom local est la seule lecture qui ne dépende
    /// pas de ce détail.
    /// </summary>
    private static IEnumerable<XElement> Children(XElement? parent, string localName)
        => parent?.Elements().Where(element => element.Name.LocalName == localName) ?? [];

    private static string? Attribute(XElement element, string localName)
        => element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == localName)?.Value;

    private static Dictionary<string, ResxEntry> ReadEntriesByName(string path)
    {
        var byName = new Dictionary<string, ResxEntry>(StringComparer.Ordinal);

        foreach (var entry in ReadEntries(path))
            byName[entry.Name] = entry;

        return byName;
    }

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
        var failures = new List<string>();

        foreach (var fileRows in rows.GroupBy(row => BuildIdentity(row.Project, row.File), StringComparer.OrdinalIgnoreCase))
        {
            if (!neutralPathByIdentity.TryGetValue(fileRows.Key, out var neutralPath))
            {
                System.Diagnostics.Debug.WriteLine($"[ResxReader] Aucun .resx neutre pour « {fileRows.Key} » : lignes ignorées.");
                continue;
            }

            foreach (var language in languages)
            {
                var variantPath = BuildVariantPath(neutralPath, language.Code);
                try
                {
                    if (SaveLanguageFile(variantPath, neutralPath, language.Code, fileRows))
                        writtenFiles++;
                }
                catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
                {
                    // Contrairement au chargement, un fichier écarté à la sauvegarde n'est JAMAIS
                    // silencieux : ce sont des traductions que l'utilisateur croirait enregistrées.
                    // On écrit tout ce qui peut l'être, puis on remonte la liste des échecs.
                    failures.Add($"{variantPath} : {ex.Message}");
                }
            }
        }

        if (failures.Count > 0)
            throw new IOException(BuildSaveFailureMessage(failures));

        return writtenFiles;
    }

    private static string BuildSaveFailureMessage(IReadOnlyList<string> failures)
    {
        const int maxListed = 10;

        var message = new StringBuilder();
        message.AppendLine($"{failures.Count} fichier(s) .resx n'ont pas pu être écrits :");

        foreach (var failure in failures.Take(maxListed))
            message.AppendLine("- " + failure);

        if (failures.Count > maxListed)
            message.AppendLine($"... et {failures.Count - maxListed} autre(s).");

        return message.ToString();
    }

    private static bool SaveLanguageFile(string variantPath, string neutralPath, string languageCode, IEnumerable<TranslationRow> rows)
    {
        var exists = File.Exists(variantPath);

        // Ne pas créer un fichier de langue pour n'y écrire que du vide.
        if (!exists && !rows.Any(row => HasContent(row, languageCode)))
            return false;

        // Les exceptions XML / accès disque remontent volontairement à Save, qui les collecte
        // et les signale : abandonner ce fichier en silence perdrait des traductions.
        var document = exists
            ? XDocument.Load(variantPath, LoadOptions.PreserveWhitespace)
            : CreateEmptyDocumentFrom(neutralPath);

        var root = document.Root;
        if (root is null)
            return false;

        var elementsByName = Children(root, "data")
            .Where(element => Attribute(element, "name") is not null)
            .GroupBy(element => Attribute(element, "name")!, StringComparer.Ordinal)
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

        // Lecture par nom local, comme au chargement ; les éléments créés reprennent l'espace de
        // noms du parent, sinon un fichier qui en déclare un se verrait injecter des xmlns="".
        var ns = element.Name.Namespace;

        var valueElement = Children(element, "value").FirstOrDefault();
        if (valueElement is null)
        {
            element.Add(new XElement(ns + "value", value));
            modified = true;
        }
        else if (!string.Equals(valueElement.Value, value, StringComparison.Ordinal))
        {
            valueElement.Value = value;
            modified = true;
        }

        var commentElement = Children(element, "comment").FirstOrDefault();
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
            (valueElement ?? element.Elements().Last()).AddAfterSelf(new XText("\n    "), new XElement(ns + "comment", comment));
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
        var ns = root.Name.Namespace;

        var element = new XElement(ns + "data",
            new XAttribute("name", key),
            new XAttribute(XNamespace.Xml + "space", "preserve"),
            new XText("\n    "),
            new XElement(ns + "value", value));

        if (comment.Length > 0)
            element.Add(new XText("\n    "), new XElement(ns + "comment", comment));

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

/// <summary>Ce qu'a parcouru la découverte : la lecture de la solution et les projets sans .resx.</summary>
internal sealed record ResxDiscovery(SolutionScan Solution, List<string> ProjectsWithoutResx);

/// <summary>Compteurs de ce qui a été rencontré en lisant les fichiers neutres.</summary>
internal sealed class ResxReadStats
{
    /// <summary>Fichiers dont le XML n'a pas pu être chargé (invalide, verrouillé, inaccessible).</summary>
    public int UnreadableFiles;

    /// <summary>Fichiers neutres disparus entre la découverte et la lecture.</summary>
    public int MissingFiles;

    /// <summary>Éléments <c>&lt;data&gt;</c> rencontrés, avant tout tri.</summary>
    public int DataElements;

    /// <summary>Entrées écartées car binaires ou sérialisées (attribut <c>type</c> ou <c>mimetype</c>).</summary>
    public int BinaryEntries;

    /// <summary>Entrées écartées car métadonnées du designer (clés « &gt;&gt;… »).</summary>
    public int DesignerMetadata;
}

/// <summary>
/// Compte rendu d'un chargement .resx, destiné à expliquer un résultat vide en langage clair.
/// </summary>
internal sealed record ResxLoadReport(
    ResxDiscovery Discovery,
    int NeutralFiles,
    int EmptyNeutralFiles,
    ResxReadStats Entries,
    int InvariantEntries,
    int Rows)
{
    /// <summary>Texte affiché à l'utilisateur. Nomme l'étape où la chaîne s'est arrêtée.</summary>
    public string Describe()
    {
        var solution = Discovery.Solution;
        var lines = new List<string>
        {
            $"Entrées déclarées par la solution : {solution.DeclaredEntries}",
            $"Projets retenus (.csproj, .vbproj, .fsproj) : {solution.Projects.Count}",
        };

        if (solution.UnsupportedProjects.Count > 0)
            lines.Add($"Projets d'un type non géré, ignorés : {solution.UnsupportedProjects.Count} ({Sample(solution.UnsupportedProjects)})");

        if (solution.MissingProjectFiles.Count > 0)
            lines.Add($"Projets déclarés mais absents du disque : {solution.MissingProjectFiles.Count} ({Sample(solution.MissingProjectFiles)})");

        lines.Add($"Fichiers .resx neutres trouvés : {NeutralFiles}");

        if (Discovery.ProjectsWithoutResx.Count > 0)
            lines.Add($"Projets sans aucun .resx : {Discovery.ProjectsWithoutResx.Count} ({Sample(Discovery.ProjectsWithoutResx)})");

        if (EmptyNeutralFiles > 0)
            lines.Add($"Fichiers sans aucune entrée traduisible : {EmptyNeutralFiles}");

        if (Entries.UnreadableFiles > 0)
            lines.Add($"Fichiers dont le XML n'a pas pu être lu : {Entries.UnreadableFiles}");

        if (Entries.MissingFiles > 0)
            lines.Add($"Fichiers disparus depuis la découverte : {Entries.MissingFiles}");

        lines.Add($"Éléments <data> rencontrés : {Entries.DataElements}");

        if (Entries.BinaryEntries > 0)
            lines.Add($"Entrées écartées car binaires ou sérialisées (type/mimetype) : {Entries.BinaryEntries}");

        if (Entries.DesignerMetadata > 0)
            lines.Add($"Entrées écartées car métadonnées du designer (>>) : {Entries.DesignerMetadata}");

        if (InvariantEntries > 0)
            lines.Add($"Entrées exclues car marquées @Invariant : {InvariantEntries}");

        lines.Add($"Lignes chargées : {Rows}");
        lines.Add(string.Empty);
        lines.Add(Diagnose());

        return string.Join(Environment.NewLine, lines);
    }

    private string Diagnose()
    {
        var solution = Discovery.Solution;

        if (solution.DeclaredEntries == 0)
            return "La solution ne déclare aucun projet lisible : le fichier est peut-être d'un format inattendu.";

        if (solution.Projects.Count == 0)
            return solution.MissingProjectFiles.Count > 0
                ? "Aucun projet retenu : les fichiers projet déclarés sont introuvables sur le disque."
                : "Aucun projet retenu : la solution ne contient aucun projet C#, VB ou F#.";

        if (NeutralFiles == 0)
            return "Aucun fichier .resx sous les répertoires de projet. S'ils sont liés depuis un répertoire extérieur au projet, ils ne sont pas trouvés — l'exploration part du répertoire du projet.";

        if (Entries.MissingFiles == NeutralFiles)
            return "Tous les fichiers .resx découverts ont disparu avant d'être lus : l'arborescence a changé pendant le chargement.";

        if (Entries.UnreadableFiles == NeutralFiles)
            return "Tous les fichiers .resx trouvés sont illisibles : XML invalide, fichiers verrouillés, ou droits insuffisants.";

        if (Entries.DataElements == 0)
            return "Aucun élément <data> n'a été rencontré : les fichiers ont été lus, mais leur structure n'est pas celle attendue d'un .resx.";

        if (Entries.BinaryEntries + Entries.DesignerMetadata >= Entries.DataElements)
            return $"Les {Entries.DataElements} entrées rencontrées sont toutes des ressources binaires ou des métadonnées du designer : aucun texte à traduire.";

        return "Des entrées ont été lues, mais aucune n'a été retenue.";
    }

    private static string Sample(List<string> values)
        => string.Join(", ", values.Take(3)) + (values.Count > 3 ? ", …" : string.Empty);
}

/// <summary>Un fichier .resx neutre et son identité fonctionnelle (projet + chemin relatif).</summary>
internal sealed record ResxFileGroup(string Project, string File, string NeutralPath);

internal sealed record ResxEntry(string Name, string Value, string Comment);
