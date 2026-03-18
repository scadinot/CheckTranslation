# CLAUDE.md - Guide pour assistants IA sur CheckTranslation

## Apercu du projet

CheckTranslation est une application de bureau Windows Forms en .NET 8.0 destinee au controle, a la traduction et a la verification des traductions d'un logiciel. Elle lit un fichier Excel exporté par ResX Resource Manager (via Visual Studio), filtre les entrees marquees `@Invariant` et affiche les traductions dans un tableau pour verification et edition.

## Stack technique

- **Langage :** C# (version recente avec references nullables et usings implicites)
- **Framework :** .NET 8.0 (`net8.0-windows`)
- **Interface :** Windows Forms (WinForms)
- **Systeme de build :** MSBuild via la CLI `dotnet`
- **Support IDE :** Visual Studio (fichier solution : `CheckTranslation.slnx`)
- **Dependances :**
  - `ClosedXML` 0.104.2 — lecture et ecriture des fichiers Excel (.xlsx)
  - `OpenAI` — appels API OpenAI (chat + listing de modèles)
  - `Anthropic` — appels API Anthropic (messages + listing de modèles)
  - `Markdig` — rendu Markdown pour l'aperçu des prompts

## Structure du projet

```
CheckTranslation/
├── CheckTranslation.slnx        # Fichier solution
├── CheckTranslation.csproj      # Fichier projet (WinExe, net8.0-windows)
├── Program.cs                   # Point d'entree (STAThread, lance MainForm)
├── Forms/
│   ├── MainForm.cs              # Logique du formulaire principal
│   ├── MainForm.Designer.cs     # Code genere par le designer (ne pas modifier)
│   ├── MainForm.resx            # Ressources du formulaire (ne pas modifier)
│   ├── ConfigForm.cs            # Dialog de configuration (prompts + IA providers)
│   ├── ConfigForm.Designer.cs   # Code genere par le designer (ne pas modifier)
│   └── ConfigForm.resx          # Ressources du dialog (ne pas modifier)
├── Models/
│   ├── TranslationRow.cs        # Modele de donnees pour une ligne de traduction
│   ├── AiProvider.cs            # Enum fournisseur IA (OpenAI / Anthropic)
│   └── AppConfig.cs             # Configuration persistante (multi-fournisseur) avec chiffrement DPAPI
├── Services/
│   ├── ExcelReader.cs           # Lecture/ecriture du fichier Excel via ClosedXML
│   └── Translator.cs            # Appel API de traduction IA (OpenAI-compatible)
├── Controls/
│   └── SortableBindingList.cs   # BindingList<T> avec tri cote client
├── Resources/                   # Icones PNG (drapeaux, open, save, config)
├── Input.xlsx                   # Fichier Excel exporté par ResX Manager (~3 Mo)
├── README.md                    # Documentation utilisateur
└── CLAUDE.md                    # Ce fichier
```

## Format du fichier Excel (Input.xlsx)

Le fichier est un export ResX Resource Manager contenant une feuille `ResXResourceManager` avec ~22 000 lignes :

| Colonne | Contenu                  |
|---------|--------------------------|
| A       | Project                  |
| B       | File                     |
| C       | Key                      |
| D       | Comment (contient `@Invariant` pour les lignes a ignorer) |
| E       | Texte francais (langue par defaut) |
| F       | Comment.de-DE            |
| G       | .de-DE (allemand)        |
| H-S     | Autres langues (en-US, es-ES, it-IT, nl-NL, pl-PL, zh-CN) |

## Commandes de build et d'execution

```bash
# Build (debug)
dotnet build

# Build (release)
dotnet build -c Release

# Lancer l'application
dotnet run

# Nettoyer les artefacts de build
dotnet clean

# Publier
dotnet publish -c Release
```

## Notes d'architecture

- **Point d'entree :** `Program.cs` — bootstrap WinForms standard avec `ApplicationConfiguration.Initialize()` et `Application.Run(new MainForm())`
- **Formulaire principal :** `Forms/MainForm.cs` / `Forms/MainForm.Designer.cs` — DataGridView avec colonnes Projet/Fichier/Cle (masquables), Francais (lecture seule) et Traduction (editable) + barre d'outils + barre de statut
- **Configuration :** `Forms/ConfigForm.cs` — dialog modal pour editer `Models/AppConfig.cs`
- **Lecture/ecriture Excel :** `Services/ExcelReader.cs` — charge le fichier via ClosedXML, ignore les lignes `@Invariant` (colonne D), lit les colonnes A/B/C/E et toutes les colonnes de traduction, puis sauvegarde les traductions et commentaires associes
- **Traduction IA :** `Services/Translator.cs` — supporte OpenAI (`OpenAI.Chat.ChatClient`) et Anthropic (`AnthropicClient`).
- **Caches IA :** `Services/TranslationService.cs` — cache memoire des traductions et des verifications pendant la session, avec deduplication et compteurs par langue
- **Sélection du modèle :** `Forms/ConfigForm.cs` — les listes de modèles OpenAI/Anthropic sont chargées via l'API au moment où l'utilisateur ouvre la liste déroulante.
- **Modele :** `Models/TranslationRow.cs` — POCO avec proprietes Project, File, Key, French, Translation et dictionnaire des traductions par colonne
- **Configuration persistante :** `Models/AppConfig.cs` — fichier `CheckTranslation.config.json` stocke dans `%LocalAppData%\CheckTranslation`, cles API chiffrees via DPAPI (`DataProtectionScope.CurrentUser`), memorise aussi `ShowDetails` et le provider selectionne
- **Tri :** `Controls/SortableBindingList.cs` — etend `BindingList<T>` pour activer le tri dans le DataGridView
- **Donnees d'entree :** `Input.xlsx` est copie dans le repertoire de sortie via `CopyToOutputDirectory`

## Conventions de code

- **Namespace :** `CheckTranslation` (unique pour tout le projet, independamment des sous-dossiers)
- **References nullables :** Activees — utiliser les annotations `?` lorsque les valeurs nulles sont possibles
- **Usings implicites :** Actives — les espaces de noms courants `System.*` et `System.Windows.Forms.*` sont importes automatiquement
- **Namespaces a portee de fichier :** Utilises dans tous les fichiers sources
- **Modificateurs d'acces :** `internal` pour les classes non exposees hors de l'assembly, `public` pour les classes de formulaires

## Avertissements importants

- **Ne PAS modifier `MainForm.Designer.cs` ni `ConfigForm.Designer.cs` a la main** — ces fichiers sont generes automatiquement par le designer Windows Forms
- **Ne PAS modifier `MainForm.resx` ni `ConfigForm.resx` manuellement** — geres par le designer
- **Les `.resx` ont des `LogicalName` explicites dans le `.csproj`** — necessaire car ils sont dans un sous-dossier `Forms/` ; ne pas supprimer ces entrees
- **Les colonnes Projet/Fichier/Cle sont creees programmatiquement** dans `MainForm.cs` (`InitDetailsColumns`) et inserees avant les colonnes du Designer ; ne pas les ajouter dans `MainForm.Designer.cs`
- **`Input.xlsx` est un fichier binaire** — ne pas tenter de le lire comme du texte ; la lecture se fait via ClosedXML dans `Services/ExcelReader.cs`
- **Le projet cible `net8.0-windows`** — il necessite le SDK .NET 8 et ne fonctionne que sous Windows

## Tests

- Aucun framework de test n'est actuellement configure
- Aucun projet de test n'existe dans la solution
- Pour ajouter des tests, utiliser un projet de test separe avec xUnit ou NUnit (ex. : `CheckTranslation.Tests/`)

## Linting et formatage

- Aucune configuration `.editorconfig`, StyleCop ou analyseur Roslyn n'existe
- Suivre les conventions C# standard (PascalCase pour les membres publics, camelCase pour les variables locales et parametres)
- Aucun pipeline CI/CD n'est configure

## Workflow Git

- **Branche principale :** `master`
- **Patterns d'exclusion :** `.gitignore` standard Visual Studio (artefacts de build, fichiers utilisateur, packages)
- **Les commits sont signes** via cle SSH
- Limiter les modifications de `Input.xlsx` — c'est un fichier binaire volumineux (~3 Mo)
