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
  - `OpenAI` 2.8.0 — appels API OpenAI (chat + listing de modèles)
  - `Anthropic` 12.8.0 — appels API Anthropic (messages + listing de modèles)
  - `Markdig` 0.38.0 — rendu Markdown pour l'aperçu des prompts
  - `Polly` 8.5.2 — resilience (retry + backoff exponentiel avec jitter) sur les appels IA
  - `Microsoft.Extensions.DependencyInjection` 9.0.0 — conteneur DI utilisé dans `Program.cs`

## Structure du projet

```
CheckTranslation/
├── CheckTranslation.slnx        # Fichier solution
├── CheckTranslation.csproj      # Fichier projet (WinExe, net8.0-windows)
├── Program.cs                   # Point d'entree (STAThread, bootstrap DI, lance MainForm)
├── Forms/
│   ├── MainForm.cs                           # Logique principale (chargement, sauvegarde, langues, filtres)
│   ├── MainForm.Designer.cs                  # Code genere par le designer (ne pas modifier)
│   ├── MainForm.resx                         # Ressources du formulaire (ne pas modifier)
│   ├── MainForm.LayoutPersistence.cs         # Partial : persistance taille fenêtre + largeur colonnes
│   ├── ManualRefreshSupport.cs               # Partial : bouton rafraîchir + F5, rechargement fichier
│   ├── VerificationScoreFilterSupport.cs     # Partial : filtre combobox par score de vérification
│   ├── ConfigForm.cs                         # Dialog de configuration (prompts + IA + vider cache)
│   ├── ConfigForm.Designer.cs                # Code genere par le designer (ne pas modifier)
│   ├── ConfigForm.resx                       # Ressources du dialog (ne pas modifier)
│   ├── MergeDifferenceForm.cs                # Dialog de résolution des conflits de fusion
│   ├── MergeDifferenceForm.Designer.cs       # Code genere par le designer
│   └── MergeDifferenceForm.resx              # Ressources du dialog
├── Models/
│   ├── TranslationRow.cs                # Modele de donnees pour une ligne (incl. FrenchComment, GetSyncKey)
│   ├── AiProvider.cs                    # Enum fournisseur IA (OpenAI / Anthropic)
│   ├── AppConfig.cs                     # Configuration persistante (multi-fournisseur) avec chiffrement DPAPI
│   ├── MergeDifference.cs               # Record représentant une différence entre source/destination
│   ├── MergeDifferenceResolution.cs     # Record de décision utilisateur (maj français/traduction)
│   └── MergeRowSnapshot.cs              # Record snapshot d'une ligne pour l'UI de fusion
├── Services/
│   ├── IExcelService.cs                 # Interface service Excel (Load/Save/Merge)
│   ├── ExcelService.cs                  # Implementation IExcelService (façade sur ExcelReader)
│   ├── ExcelReader.cs                   # Lecture/ecriture/fusion Excel via ClosedXML
│   ├── ExcelLoadProgress.cs             # Record struct (Done, Total) pour progression ligne-par-ligne
│   ├── ITranslationService.cs           # Interface cache + batch traduction/vérification
│   ├── TranslationService.cs            # Implementation : cache mémoire + batch + dédup
│   └── Translator.cs                    # Appels IA bruts (OpenAI + Anthropic) + retry Polly
├── Logic/
│   ├── QualityScore.cs                  # Parsing du score "XXX - ..." + couleur de fond (dégradé rouge→vert)
│   └── TranslationRowFiltering.cs       # Filtrage côté client (texte + score<X, score>=X, score:none)
├── Controls/
│   └── SortableBindingList.cs           # BindingList<T> avec tri cote client
├── Resources/                           # Icones PNG (drapeaux, open, save, config, merge, refresh, columns, eyes, pencil)
├── Input.xlsx                           # Fichier Excel exporté par ResX Manager (~3 Mo)
├── README.md                            # Documentation utilisateur
├── ANALYSE_PROJET.md                    # Analyse détaillée (architecture, flux, points d'attention)
├── ROADMAP.md                           # Feuille de route / suivi des améliorations
└── CLAUDE.md                            # Ce fichier
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

### Bootstrap
- **Point d'entree :** `Program.cs` — bootstrap WinForms standard avec `ApplicationConfiguration.Initialize()`, configuration d'un `ServiceCollection` (`IExcelService`/`ITranslationService` en singleton, `ConfigForm` et `MainForm` en transient avec factory `Func<ConfigForm>`), puis `Application.Run(MainForm)`.
- **Injection de dépendances :** `MainForm` et `ConfigForm` acceptent un constructeur avec leurs services (avec fallback de constructeur sans paramètre instanciant manuellement les services pour la compatibilité Designer).

### Formulaires
- **Formulaire principal :** `Forms/MainForm.cs` — classe `partial` découpée en plusieurs fichiers (`MainForm.LayoutPersistence.cs`, `ManualRefreshSupport.cs`, `VerificationScoreFilterSupport.cs`) pour séparer les responsabilités.
  - DataGridView avec colonnes Projet/Fichier/Cle (masquables), Francais (lecture seule), Traduction (editable) et Commentaire (score).
  - Barre d'outils : Ouvrir / Sauvegarder / Fusionner / Afficher détails / Config / Drapeaux / Rafraîchir.
  - Barre de statut : fichier, lignes, sélection, cache traduction, cache vérification, provider IA + modèle, langue active.
  - Menu contextuel sur `colTranslation` : Auto-traduire (copie existante), Traduire, Vérifier.
  - Menu contextuel sur `colFrench` : Copier le texte français.
  - Raccourci **F5** pour rafraîchir (rechargement fichier + détection des changements sur le français source + conservation des traductions en mémoire).
- **Fusion Excel :** `Forms/MergeDifferenceForm.cs` — dialog affichant côte à côte la ligne source et la ligne destination pour qu'on choisisse (via deux checkboxes) s'il faut reporter le français/commentaire et/ou la traduction/commentaire de traduction.
- **Configuration :** `Forms/ConfigForm.cs` — dialog modal pour editer `Models/AppConfig.cs` + boutons "Vider cache trad." / "Vider cache vérif." agissant sur `ITranslationService`.

### Services
- **Lecture/ecriture Excel :** `Services/ExcelReader.cs` — charge le fichier via ClosedXML, ignore les lignes `@Invariant` (colonne D), lit les colonnes A/B/C/E + toutes les colonnes de traduction. Sauvegarde `Save()`. Fusion `Merge()` avec détection des différences (`GetMergeSourceDifferences()`) et résolution par ligne via `MergeDifferenceResolution`. Utilise une `SyncKey = Project|File|Key` (séparateur `\u001F`) pour corréler les lignes source/destination.
- **Façade Excel :** `Services/ExcelService.cs` (implémente `IExcelService`) — expose `Load`, `LoadWithRowProgress`, `Save`, `Merge`, `GetMergeSourceDifferences`.
- **Progression chargement :** `Services/ExcelLoadProgress.cs` — `record struct(Done, Total)` permettant d'afficher une progress bar en lignes lues plutôt qu'en pourcentage.
- **Traduction IA :** `Services/Translator.cs` — supporte OpenAI (`OpenAI.Chat.ChatClient`) et Anthropic (`AnthropicClient`). Batchs parallélisés (`Task.WhenAll`) avec limitation de concurrence (`SemaphoreSlim`, `FixedParallelBatchRequests = 4`). Retry exponentiel avec jitter via Polly (3 tentatives) sur les erreurs HTTP transitoires (408/429/500/502/503/504, `HttpRequestException`, `TimeoutException`, `TaskCanceledException`). `BatchSize = 20`, `Temperature = 0.1`.
- **Service traduction :** `Services/TranslationService.cs` (implémente `ITranslationService`) — cache mémoire (traduction + vérification) avec clé `Provider|Url|Model|Language|Texte` (séparateur `\u001F`), déduplication des doublons intra-lot, compteurs par langue, mises à jour manuelles via `UpdateTranslationCache` / `UpdateVerificationCache`, vidage via `ClearTranslationCache` / `ClearVerificationCache`.
- **Sélection du modèle :** `Forms/ConfigForm.cs` — les listes de modèles OpenAI/Anthropic sont chargées via l'API au moment où l'utilisateur ouvre la liste déroulante.

### Logic
- **Score de qualité :** `Logic/QualityScore.cs` — parsing du format `XXX - commentaire` (regex, 0-100) et calcul d'une couleur de fond en dégradé (rouge `<60` → orange `70` → jaune `80` → vert `90-100`). Utilisé par `MainForm.DataGridView_CellFormatting` pour coloriser les cellules `colTranslation` et `colComment`.
- **Filtrage :** `Logic/TranslationRowFiltering.cs` — filtrage côté client sur toutes les colonnes ; pour `Comment`, supporte en plus les pseudo-filtres `score<N`, `score>=N`, `score:none` utilisés par le ComboBox de filtre par score.

### Modèles
- **Ligne :** `Models/TranslationRow.cs` — POCO avec `Project`, `File`, `Key`, `FrenchComment`, `French`, `Translation`, `Comment` + dictionnaires `Translations` et `Comments` par colonne. Méthodes `SwitchLanguage(oldCol, newCol)` et `GetSyncKey()`.
- **Fusion :** `Models/MergeDifference.cs` (Source vs Destination), `Models/MergeRowSnapshot.cs` (snapshot lecture seule d'une ligne pour l'UI), `Models/MergeDifferenceResolution.cs` (décision utilisateur).
- **Configuration persistante :** `Models/AppConfig.cs` — fichier `CheckTranslation.config.json` stocke dans `%LocalAppData%\CheckTranslation`, cles API chiffrees via DPAPI (`DataProtectionScope.CurrentUser`). Persiste aussi :
  - `ShowDetails` (visibilité colonnes Projet/Fichier/Clé)
  - `Provider` sélectionné + URL/Modèle par provider
  - `SelectedLanguageCode`
  - `WindowWidth`/`WindowHeight`
  - `ColumnFillWeightsWithDetails` / `ColumnFillWeightsWithoutDetails` (persistance fine des largeurs selon le mode)
  - Lecture de l'ancien emplacement (`AppContext.BaseDirectory`) conservée pour compatibilité.

### UI helpers
- **Tri :** `Controls/SortableBindingList.cs` — etend `BindingList<T>` pour activer le tri dans le DataGridView.
- **Donnees d'entree :** `Input.xlsx` est copie dans le repertoire de sortie via `CopyToOutputDirectory`.

## Conventions de code

- **Namespace :** `CheckTranslation` (unique pour tout le projet, independamment des sous-dossiers)
- **References nullables :** Activees — utiliser les annotations `?` lorsque les valeurs nulles sont possibles
- **Usings implicites :** Actives — les espaces de noms courants `System.*` et `System.Windows.Forms.*` sont importes automatiquement
- **Namespaces a portee de fichier :** Utilises dans tous les fichiers sources
- **Modificateurs d'acces :** `internal` pour les classes non exposees hors de l'assembly (`TranslationRow`, `AppConfig`, services, modèles de fusion…), `public` pour les classes de formulaires héritant de `Form`
- **Records :** préférés pour les modèles immuables (`MergeDifference`, `MergeRowSnapshot`, `MergeDifferenceResolution`, `LanguageInfo`, `ExcelLoadProgress`)
- **Partials :** pour découper `MainForm` par responsabilité (layout, refresh, score filter)

## Avertissements importants

- **Ne PAS modifier `*.Designer.cs` a la main** — ces fichiers (`MainForm`, `ConfigForm`, `MergeDifferenceForm`) sont generes automatiquement par le designer Windows Forms
- **Ne PAS modifier les `*.resx` manuellement** — geres par le designer
- **Les `.resx` ont des `LogicalName` explicites dans le `.csproj`** — necessaire car ils sont dans un sous-dossier `Forms/` ; ne pas supprimer ces entrees. Les fichiers partials (`MainForm.LayoutPersistence.resx`, `ManualRefreshSupport.resx`, `VerificationScoreFilterSupport.resx`) sont explicitement exclus via `<EmbeddedResource Remove="..."/>`.
- **Les colonnes Projet/Fichier/Cle sont creees programmatiquement** dans `MainForm.cs` (`InitDetailsColumns`) et inserees avant les colonnes du Designer ; ne pas les ajouter dans `MainForm.Designer.cs`. La colonne `Commentaire` est aussi créée par code (`InitCommentColumn`).
- **Le bouton `Rafraîchir` est ajouté programmatiquement** (`ManualRefreshSupport.InitRefreshButton`) puis disposé par `ArrangeToolStripItems`.
- **Les filtres ComboBox de score sont créés par code** (`VerificationScoreFilterSupport.TryCreateSpecialFilterControl`) et superposés aux en-têtes du DataGridView.
- **`Input.xlsx` est un fichier binaire** — ne pas tenter de le lire comme du texte ; la lecture se fait via ClosedXML dans `Services/ExcelReader.cs`
- **Le projet cible `net8.0-windows`** — il necessite le SDK .NET 8 et ne fonctionne que sous Windows
- **Fusion Excel :** la résolution des différences se fait par paire Source/Destination via `MergeDifferenceForm`, affichée pour CHAQUE ligne ayant un français ou un commentaire différent. L'utilisateur peut annuler la fusion globalement.

## Tests

- Aucun framework de test n'est actuellement configure
- Aucun projet de test n'existe dans la solution
- Pour ajouter des tests, utiliser un projet de test separe avec xUnit ou NUnit (ex. : `CheckTranslation.Tests/`)
- Candidats naturels pour des tests unitaires : `QualityScore`, `TranslationRowFiltering`, `TranslationService` (cache/dédup), `ExcelReader.Merge`, `AppConfig` (round-trip DPAPI), `Translator.ParseNumberedList`.

## Linting et formatage

- Aucune configuration `.editorconfig`, StyleCop ou analyseur Roslyn n'existe
- Suivre les conventions C# standard (PascalCase pour les membres publics, camelCase pour les variables locales et parametres)
- Aucun pipeline CI/CD n'est configure

## Workflow Git

- **Branche principale :** `master`
- **Patterns d'exclusion :** `.gitignore` standard Visual Studio (artefacts de build, fichiers utilisateur, packages)
- **Les commits sont signes** via cle SSH
- Limiter les modifications de `Input.xlsx` — c'est un fichier binaire volumineux (~3 Mo)
