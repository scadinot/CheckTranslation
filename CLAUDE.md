# CLAUDE.md — Guide technique et suivi

Référence complète du projet **CheckTranslation**, destinée aux humains comme aux assistants IA. Couvre l'architecture, les conventions, les avertissements, le suivi chronologique et la roadmap.

> Documentation utilisateur et installation : voir [README.md](README.md).

---

## Table des matières

1. [Aperçu](#1-aperçu)
2. [Stack technique](#2-stack-technique)
3. [Structure du projet](#3-structure-du-projet)
4. [Format Excel (ResX Resource Manager)](#4-format-excel-resx-resource-manager)
5. [Build & exécution](#5-build--exécution)
6. [Architecture](#6-architecture)
7. [Flux fonctionnels](#7-flux-fonctionnels)
8. [Détails IA (Translator + TranslationService + Glossaire)](#8-détails-ia-translator--translationservice--glossaire)
9. [Conventions de code](#9-conventions-de-code)
10. [Avertissements importants](#10-avertissements-importants)
11. [Tests](#11-tests)
12. [Suivi chronologique](#12-suivi-chronologique)
13. [Roadmap](#13-roadmap)

---

## 1. Aperçu

CheckTranslation est une application de bureau **Windows Forms** en **C# / .NET 8** (`net8.0-windows`) destinée à **contrôler, traduire et vérifier** des chaînes de ressources `.resx`. Elle sait travailler sur **deux sources** :

- un **export Excel** généré par ResX Resource Manager (`.xlsx`) ;
- les **fichiers `.resx` directement**, désignés par une solution (`.sln` / `.slnx`).

Dans les deux cas, l'application affiche les entrées dans un `DataGridView`, permet l'édition de la traduction dans une langue cible, puis réécrit la source d'origine.

Elle peut appeler les API **OpenAI** et **Anthropic**, en direct ou au travers de la passerelle **Bifrost**, pour traduire et vérifier des traductions en lot, avec cache mémoire et glossaire métier injecté dans les prompts. Elle peut aussi fusionner des traductions d'un fichier source vers un fichier destination avec résolution des conflits ligne-par-ligne.

---

## 2. Stack technique

- **Langage :** C# (références nullables activées, implicit usings activés)
- **Framework :** .NET 8.0 (`net8.0-windows`)
- **Interface :** Windows Forms
- **Build :** MSBuild via la CLI `dotnet`
- **IDE :** Visual Studio (fichier solution : `CheckTranslation.slnx`)

### Dépendances NuGet
- `ClosedXML` `0.104.2` — lecture/écriture `.xlsx`
- `OpenAI` `2.8.0` — client chat completions + listing modèles
- `Anthropic` `12.8.0` — client messages + listing modèles
- `Markdig` `0.38.0` — rendu Markdown (aperçu des prompts)
- `Polly` `8.5.2` — retry + backoff exponentiel + jitter sur les appels IA
- `Microsoft.Extensions.DependencyInjection` `9.0.0` — conteneur DI

---

## 3. Structure du projet

```
CheckTranslation/
├── CheckTranslation.slnx        # Solution
├── CheckTranslation.csproj      # Projet (WinExe, net8.0-windows)
├── Program.cs                   # Point d'entrée (STAThread, bootstrap DI)
├── Forms/
│   ├── MainForm.cs                           # Logique principale (~2080 lignes, organisée en sections)
│   ├── MainForm.Designer.cs                  # Code généré par le designer
│   ├── MainForm.resx                         # Ressources du form
│   ├── ConfigForm.cs                         # Config (prompts, IA, vidage cache)
│   ├── ConfigForm.Designer.cs
│   ├── ConfigForm.resx
│   ├── MergeDifferenceForm.cs                # Dialog résolution conflits de fusion
│   ├── MergeDifferenceForm.Designer.cs
│   ├── MergeDifferenceForm.resx
│   ├── GlossaryForm.cs                       # Éditeur du glossaire par langue
│   ├── GlossaryForm.Designer.cs
│   ├── GlossaryForm.resx
│   ├── GlossaryExtractionDialog.cs           # Extraction IA assistée (validation terme par terme)
│   ├── GlossaryExtractionDialog.Designer.cs
│   └── GlossaryExtractionDialog.resx
├── Models/
│   ├── TranslationRow.cs                # Ligne de traduction + dictionnaires par colonne
│   ├── AiProvider.cs                    # Enum OpenAI / Anthropic
│   ├── AppConfig.cs                     # Config persistante (JSON + DPAPI)
│   ├── MergeDifference.cs               # Différence source vs destination
│   ├── MergeDifferenceResolution.cs     # Décision utilisateur par ligne (fusion)
│   ├── MergeRowSnapshot.cs              # Snapshot lecture seule pour l'UI de fusion
│   ├── Glossary.cs                      # Conteneur du glossaire
│   └── GlossaryEntry.cs                 # Source / Destination / Context
├── Services/
│   ├── ITranslationSource.cs                 # Abstraction de source (Load / Save / SupportsMerge)
│   ├── ITranslationSourceFactory.cs / TranslationSourceFactory.cs  # .xlsx → Excel, .sln/.slnx → resx
│   ├── ExcelTranslationSource.cs             # Source Excel (seule à supporter la fusion)
│   ├── ResxTranslationSource.cs              # Source .resx
│   ├── IExcelService.cs / ExcelService.cs    # Façade DI pour la fusion Excel
│   ├── ExcelReader.cs                        # Lecture / écriture / fusion via ClosedXML
│   ├── ResxReader.cs                         # Lecture / écriture directe des .resx (XDocument)
│   ├── SolutionReader.cs                     # Liste des projets d'un .sln ou .slnx
│   ├── SourceLoadProgress.cs                 # record struct(Done, Total)
│   ├── ITranslationService.cs / TranslationService.cs  # Cache mémoire + batch + dédup
│   ├── Translator.cs                         # Appels bruts OpenAI/Anthropic + Polly
│   └── IGlossaryService.cs / GlossaryService.cs        # Persistance JSON par langue + fingerprint SHA256
├── Logic/
│   ├── QualityScore.cs                       # Parsing score "XXX - ..." + dégradé couleur
│   └── TranslationRowFiltering.cs            # Filtrage côté client (texte + score<N, score>=N, score:none)
├── Controls/
│   └── SortableBindingList.cs                # BindingList<T> triable
├── Resources/                                # Icônes PNG (drapeaux, toolbar, tabs)
├── Input.xlsx                                # Fichier Excel exemple (~3 Mo)
├── README.md                                 # Documentation utilisateur
└── CLAUDE.md                                 # Ce fichier (guide technique + suivi)
```

---

## 4. Format Excel (ResX Resource Manager)

Export d'une feuille `ResXResourceManager` (~22 000 lignes). Les colonnes sont 1-indexées :

| Colonne | Contenu |
|---------|---------|
| A (1) | Project |
| B (2) | File |
| C (3) | Key |
| D (4) | Comment source (contient `@Invariant` pour ignorer la ligne) |
| E (5) | Texte français (langue par défaut, pas de colonne de commentaire) |
| F | Comment.de-DE |
| G | .de-DE (allemand) |
| H…S | Autres langues : en-US, es-ES, it-IT, nl-NL, pl-PL, zh-CN (chaque langue = une colonne de commentaire précédée d'une colonne de traduction) |

Convention : pour une langue dont la colonne de traduction est `col`, la colonne de commentaire associée est `col - 1`. Cette correspondance code de langue → colonne est portée par `LanguageInfo.Column` et **ne sort pas de `ExcelReader`** : partout ailleurs, une langue est identifiée par son code.

### 4.bis Format .resx (lecture directe)

L'utilisateur ouvre une solution (`.sln` ou `.slnx`) ; `SolutionReader` en extrait les projets, puis `ResxReader` scanne chaque répertoire de projet à la recherche des `.resx`.

| Notion | Origine |
|---|---|
| `Project` | Nom du fichier projet sans extension (`ElectricalBusiness.csproj` → `ElectricalBusiness`) |
| `File` | Chemin du `.resx` **neutre** relatif au projet, sans extension, séparateurs `\` (`Properties\Msg`) |
| `Key` | Attribut `name` de l'élément `<data>` |
| Français + commentaire source | `<value>` et `<comment>` du fichier neutre (`Msg.resx`) |
| Traduction + commentaire par langue | `<value>` et `<comment>` de la variante (`Msg.de-DE.resx`) |

Ces conventions reproduisent **exactement** les colonnes `Project` / `File` / `Key` de l'export Excel : les deux sources produisent donc la même clé de corrélation `Project\|File\|Key`.

Entrées exclues au chargement (mêmes règles que ResX Resource Manager) :
- commentaire neutre contenant `@Invariant` (~30 % des lignes sur le corpus de référence) ;
- entrées non textuelles, repérées par un attribut `type` ou `mimetype` (images, icônes, blobs sérialisés) ;
- métadonnées du designer WinForms, dont la clé commence par `>>` (les clés `$this.Text`, `btnOk.Text`… sont conservées) ;
- répertoires `bin`, `obj`, `.git`, `.vs`.

Une variante de culture n'est jamais confondue avec un fichier neutre : le suffixe est validé contre la liste des cultures connues du système, y compris pour les cultures non gérées par l'application (`Msg.pt-BR.resx` n'est ni chargé, ni traité comme neutre). En second recours, un suffixe de forme BCP-47 valide mais absent de cette liste est accepté (`ca-ES-valencia`).

⚠ La validation ne peut **pas** reposer sur l'exception de `CultureInfo.GetCultureInfo` : sous ICU (le défaut depuis .NET 5, Windows compris), elle ne lève pas pour un nom quelconque bien formé — `Designer`, `Backup`, `resources` renvoient une culture « custom ». S'y fier ferait passer n'importe quel suffixe pour une culture, et le fichier neutre correspondant (`Notes.Backup.resx`) disparaîtrait silencieusement de la découverte.

---

## 5. Build & exécution

```bash
# Build debug
dotnet build

# Build release
dotnet build -c Release

# Lancer
dotnet run

# Nettoyer
dotnet clean

# Publier
dotnet publish -c Release
```

Le projet cible `net8.0-windows` → SDK .NET 8 requis, Windows uniquement.

---

## 6. Architecture

### 6.1 Bootstrap (Program.cs + DI)

`Program.Main` :
1. `AppConfig.Load()` → alimente `AppConfig.Current`.
2. `ApplicationConfiguration.Initialize()` (DPI, police).
3. Configure un `ServiceCollection` :
   - `IExcelService → ExcelService` (singleton)
   - `ITranslationService → TranslationService` (singleton)
   - `IGlossaryService → GlossaryService` (singleton)
   - `ConfigForm`, `GlossaryForm`, `GlossaryExtractionDialog` (transient + factory `Func<T>`)
   - `MainForm` (transient, reçoit les services et factories par ctor)
4. `Application.Run(MainForm)` résolu via le conteneur.

Chaque formulaire principal a un **ctor par défaut** qui instancie manuellement les services (compat Designer) et un **ctor DI** utilisé en runtime.

### 6.2 Formulaires

**`Forms/MainForm.cs`** (~2080 lignes) — `partial class` (avec `MainForm.Designer.cs`). Toute la logique applicative est dans ce fichier, organisée en **sections commentées** :
- Constructeur, InitComponent, init des colonnes / boutons / langues
- `// --- Indicateur de qualité (couleur) ---` : `DataGridView_CellFormatting` + `QualityScore.GetBackColor`
- `// --- Filtres (style ResX Resource Manager) ---` : loupe dans l'en-tête, TextBox superposé, debounce 300 ms. Les métriques de layout sont calculées au runtime selon le DPI (`_filterControlHeight`, `_columnHeaderTitleHeight`, `FilterIconWidth`, `FilterBottomMargin`) — plus aucune constante en pixels.
- Tri, menu contextuel, chargement/sauvegarde, traduire/vérifier IA, glossaire, auto-traduire
- **Verrou d'écriture disque** : `SetWritingState(bool)` positionne `_isWriting`, désactive `toolStrip` + `dataGridView` et fait annuler la fermeture par `MainForm_FormClosing`. Le refus de fermeture est signalé sans modale par `FlashCloseBlocked()` (message temporaire de 3 s dans la status bar, texte précédent restauré).
- `// --- Icônes ---`
- `// --- Persistance de disposition ---` : taille fenêtre + largeur colonnes (2 dictionnaires distincts selon mode détails)
- `// --- Bouton Rafraîchir + F5 ---` : rechargement fichier + détection changements source
- `// --- Filtre par score de vérification ---` : ComboBox dans l'en-tête Commentaire

**`Forms/ConfigForm.cs`** — dialog modal pour `AppConfig`. Tous les contrôles (prompts, fournisseur IA, clés, URLs, modèles, boutons "Vider cache trad./vérif.") sont dans le Designer. `ConfigForm.cs` contient uniquement les handlers et la logique métier.

**`Forms/MergeDifferenceForm.cs`** — dialog affichant côte à côte ligne source et ligne destination ; deux checkboxes ("reporter français+commentaire", "reporter traduction+commentaire") + bouton Continuer/Annuler. Colonnes DataGridView générées dynamiquement selon le contexte.

**`Forms/GlossaryForm.cs`** — éditeur du glossaire métier : `ComboBox` de langue + `DataGridView` triable des entrées `Source` / `Destination` / `Context`. Boutons Ajouter / Supprimer / Annuler / Enregistrer. Prompt de confirmation si modifications non enregistrées à la fermeture.

**`Forms/GlossaryExtractionDialog.cs`** — dialog d'extraction assistée : l'IA propose une liste de termes candidats (source + destination + contexte), l'utilisateur coche ceux à ajouter (édition possible inline) puis valide. Retourne `AcceptedEntries` (liste filtrée).

### 6.3 Services

**`ExcelReader` (static)** — cœur Excel :
- `Load(filePath, languages, progress)` : parse la feuille, ignore les `@Invariant`, renseigne `TranslationRow.RowNumber/Project/File/Key/FrenchComment/French` + dictionnaires `Translations[code]` et `Comments[code]` (la colonne de chaque langue vient de `LanguageInfo.Column`). Rapporte la progression via `IProgress<SourceLoadProgress>` (toutes les 10 lignes + bornes). Ne positionne pas la vue active : c'est `MainForm` qui appelle `SelectLanguage`.
- `Save(filePath, rows, languages)` : réécrit cellule par cellule les langues connues, la correspondance code → colonne étant reconstruite localement. `WriteCellValue` double une apostrophe de tête pour la préserver. La synchronisation de la vue active est faite en amont par `MainForm` (`CommitActiveLanguage`).
- `Merge(...)` / `GetMergeSourceDifferences(...)` prennent la `LanguageInfo` active : la colonne sert à écrire dans le classeur destination, le code à lire la traduction dans la ligne source.
- Corrélation des lignes via `SyncKey = Project|File|Key` (séparateur `\u001F`), détection des divergences français/commentaire, écriture sélective selon `MergeDifferenceResolution`.

**`ExcelService`** (implémente `IExcelService`) : façade DI **réduite à la fusion** (`Merge`, `GetMergeSourceDifferences`). Le chargement et la sauvegarde passent désormais par `ITranslationSource`.

**`ITranslationSource`** — abstraction des sources. Une instance est liée à un chemin et expose `Path`, `Kind` (« Excel » / « resx », affiché dans la status bar), `SupportsMerge`, `Load(languages, progress)` et `Save(rows, languages)`. Deux implémentations : `ExcelTranslationSource` (`SupportsMerge = true`) et `ResxTranslationSource` (`SupportsMerge = false`). `TranslationSourceFactory.Create(path)` choisit selon l'extension et expose `OpenFileFilter` pour l'`OpenFileDialog`.

**`SolutionReader` (static)** — projets d'une solution, aux deux formats : `.slnx` (XML, `<Project Path>` à tous les niveaux, y compris dans les `<Folder>`) et `.sln` (texte, regex sur les lignes `Project(...)`, les dossiers de solution étant écartés par le filtre d'extension). Les projets absents du disque sont ignorés ; deux projets d'un même répertoire ne sont scannés qu'une fois.

**`ResxReader` (static)** — lecture / écriture directe des `.resx` :
- `DiscoverFiles(solutionPath)` : énumère les `.resx` neutres et leur identité (Projet, Fichier). Ne lit aucun XML — partagé par le chargement et la sauvegarde pour garantir la même correspondance identité → chemin disque.
- `Load(...)` : construit les `TranslationRow` depuis le neutre + les variantes de culture, en appliquant les exclusions du §4.bis. Un `.resx` au XML invalide est ignoré (trace `Debug`) sans interrompre le chargement de la solution.
- **Asymétrie assumée sur les erreurs d'E/S** : au *chargement*, un `.resx` invalide ou inaccessible (verrouillé par Visual Studio, droits, chemin trop long) est ignoré avec une trace `Debug` — le reste de la solution se charge. À la *sauvegarde*, un fichier en échec n'est **jamais** ignoré en silence : `Save` écrit tout ce qu'il peut, collecte les échecs et lève une `IOException` les listant, que `MainForm` affiche. Ignorer silencieusement à l'écriture ferait croire à l'utilisateur que ses traductions sont enregistrées.
- `Save(...)` : **n'écrit que les variantes de culture, jamais le fichier neutre** (le français est en lecture seule dans l'UI). Écriture chirurgicale : chargement en `LoadOptions.PreserveWhitespace`, mise à jour ou insertion de l'entrée ciblée, fichier réécrit uniquement s'il a réellement changé (sauvegarde idempotente). Le BOM d'origine est préservé. Un fichier de langue absent est créé à partir de l'en-tête du neutre (déclaration, schéma XSD, `resheader`) vidé de ses données — jamais pour n'y écrire que du vide.

**`Translator` (static)** — appels bruts aux providers :
- OpenAI via `OpenAI.Chat.ChatClient`.
- Anthropic via `AnthropicClient` (endpoint normalisé : retrait de `/v1` ou `/v1/messages`), `MaxTokens = 2048`.
- **Bifrost** (passerelle auto-hébergée) : aucun client dédié. La passerelle expose un chemin par dialecte, donc `BifrostOpenAI` réutilise le client OpenAI et `BifrostAnthropic` le client Anthropic — seule l'URL de base change. `ResolveApiKey` substitue un jeton neutre quand aucune clé n'est saisie : les SDK refusent une chaîne vide, alors qu'une instance Bifrost locale n'exige pas de clé.
- `TranslateBatchAsync(texts, config, lang, glossarySection)` et `VerifyBatchAsync(pairs, ...)` : construisent une liste numérotée, remplacent `{language}` et `{glossary}` dans le system prompt, demandent une réponse strictement numérotée.
- `TranslateInBatchesAsync` / `VerifyInBatchesAsync` : découpent en lots de `BatchSize = 20` (constante `internal`, réutilisée par `TranslationService.ChunkResults`), parallélisent avec `Task.WhenAll` + `SemaphoreSlim(FixedParallelBatchRequests = 4)`.
- Retry Polly (3 tentatives, backoff exponentiel + jitter), pipeline `static readonly RetryPipeline` construit une seule fois (stateless, partagé par tous les appels), sur : `HttpRequestException` transitoire (408/429/5xx + statut nul), `TimeoutException`, `TaskCanceledException`, `ClientResultException` (408/429/5xx).
- **Callback `onBatchCompleted(batchItems, batchResults)`** invoqué dans chaque tâche parallèle juste après réception des résultats du batch → permet à `TranslationService` de reporter la progression au fil de l'eau.
- `ParseNumberedList` (regex `^\s*(\d+)[.)]\s*(.+)$`) supporte les entrées multi-lignes.

**`TranslationService`** (implémente `ITranslationService`) :
- Deux caches distincts (`Dictionary<string,string>` + `lock`) : traduction, vérification.
- Clé traduction : `Provider|Url|Model|Language|GlossaryFingerprint|Texte`.
- Clé vérification : `Provider|Url|Model|Language|GlossaryFingerprint|French|Translation`.
- Déduplication intra-lot (plusieurs lignes avec le même français → une seule requête API).
- Utilise `Translator.*InBatchesAsync` avec le callback `onBatchCompleted` pour remplir le cache et reporter la progression au fil des batches (`Interlocked.Add` sur le compteur `completed`).
- API : `UpdateTranslationCache`, `UpdateVerificationCache`, `GetTranslationCacheCount`, `GetVerificationCacheCount`, `ClearTranslationCache`, `ClearVerificationCache`.
- **Deux niveaux de correspondance de clé** : les `Get*Count` matchent `Provider|Url|Model|Language|GlossaryFingerprint` (le fingerprint est un paramètre obligatoire) → les entrées devenues inatteignables après modification du glossaire ne sont plus comptées ; les `Clear*` matchent uniquement `Provider|Url|Model` → la purge emporte aussi les entrées périmées.

**`GlossaryService`** (implémente `IGlossaryService`) :
- Persistance JSON par langue dans `%LocalAppData%\CheckTranslation`.
- `GetEntries(langueCode)` / `ReplaceEntries(langueCode, entries)` / `Save()`.
- `BuildGlossarySection(langueCode, langueName)` : produit la section texte injectée à la place du placeholder `{glossary}`. Chaîne vide si aucune entrée → aucun impact sur le prompt.
- `GetGlossaryFingerprint(langueCode)` : SHA256 des entrées triées, inclus dans les clés de cache → **invalidation automatique** dès qu'une entrée change.
- L'extraction IA réutilise `Translator.CallApiAsync` (pipeline Polly multi-providers) avec un prompt JSON strict ; filtrage des termes déjà connus avant proposition à l'utilisateur.

### 6.4 Logic

**`QualityScore`** (static) — format attendu `XXX - commentaire` (3 chiffres, 0..100). Regex `^\s*(\d{3})\s*[-–]\s*`. `GetBackColor(score)` interpole linéairement une palette pastel (rouge < 60 → orange 70 → jaune 80 → vert 90–100).

**`TranslationRowFiltering`** (static) — `Filter(allRows, filters)` sur toutes les colonnes. Pour `Comment`, supporte les pseudo-filtres :
- `score:none` → lignes sans score parsable
- `score<N` → lignes sans score OU score ≤ N (inclusif)
- `score>=N` → lignes avec score ≥ N

### 6.5 Modèles

**`TranslationRow`** (POCO) :
- Identité : `RowNumber` (ligne Excel), `Project`, `File`, `Key`.
- Source : `French`, `FrenchComment`.
- Vue langue active : `Translation`, `Comment` (modifiées directement par l'UI).
- Multi-langues : `Translations: Dictionary<int,string>`, `Comments: Dictionary<int,string>` (clé = numéro de colonne Excel).
- `SwitchLanguage(oldCol, newCol)` : pousse la vue active dans le dictionnaire sous `oldCol`, recharge depuis `newCol`. Évite le rechargement Excel à chaque switch de langue.
- `GetSyncKey()` : `Project|File|Key` avec séparateur `\u001F`.

**Fusion** :
- `MergeRowSnapshot` : record immuable `(Project, File, Key, French, FrenchComment, Translation, TranslationComment)`.
- `MergeDifference(SyncKey, Source, Destination)` : paire affichée dans le dialog.
- `MergeDifferenceResolution(UpdateFrenchAndComment, UpdateTranslationAndComment)` : décision utilisateur (bool + bool). Propriété dérivée `HasAnyChange`.

**Glossaire** :
- `Glossary` : conteneur.
- `GlossaryEntry(Source, Destination, Context)` : triplet. La structure de l'entrée reste générique, mais le stockage actuel du glossaire est indexé par `languageCode` côté cible (`EntriesByLanguage`) et suppose donc implicitement une source FR ; les autres couples source/destination ne sont pas encore pris en charge tels quels sans faire évoluer le format de clé.

### 6.6 Configuration persistante (`AppConfig`)

Fichier : `%LocalAppData%\CheckTranslation\CheckTranslation.config.json` (lecture de compatibilité sur `AppContext.BaseDirectory` si le nouveau emplacement est absent).

Contenu persisté :
- Prompts (`TranslatePrompt`, `VerifyPrompt`)
- Fournisseur sélectionné (`Provider`) et paramètres par provider (`OpenAiKey/Url/ModelName`, `AnthropicKey/Url/ModelName`, `BifrostOpenAiKey/Url/ModelName`, `BifrostAnthropicKey/Url/ModelName`)
- `ShowDetails`, `SelectedLanguageCode`, `WindowWidth/Height`
- `ColumnFillWeightsWithDetails` / `ColumnFillWeightsWithoutDetails` : largeurs de colonnes selon le mode détails

**Sécurité** : clés API chiffrées via DPAPI (`ProtectedData.Protect`, `DataProtectionScope.CurrentUser`). En cas d'échec de déchiffrement (autre utilisateur, format invalide), retourne la valeur telle quelle plutôt que de planter.

**Compat legacy** : les champs historiques `Key`/`Url`/`ModelName` (OpenAI uniquement) sont toujours reconnus au chargement. Une configuration antérieure à l'ajout de Bifrost se charge sans perte : les champs absents prennent les valeurs par défaut de la passerelle, et un nom de fournisseur inconnu retombe sur `OpenAI`.

### 6.6.bis Fournisseurs IA

Quatre entrées, chacune avec sa clé, son URL et son modèle persistés séparément — basculer de l'une à l'autre ne demande aucune ressaisie :

| `AiProvider` | Dialecte / client | URL par défaut | Modèle par défaut |
|---|---|---|---|
| `OpenAI` | OpenAI, direct | `https://api.openai.com/v1` | `gpt-5.2` |
| `Anthropic` | Anthropic, direct | `https://api.anthropic.com` | `claude-sonnet-4-6` |
| `BifrostOpenAI` | OpenAI, via passerelle | `http://localhost:8080/openai/v1` | `openai/gpt-5.2` |
| `BifrostAnthropic` | Anthropic, via passerelle | `http://localhost:8080/anthropic` | `anthropic/claude-sonnet-4-6` |

Deux prédicats portent cette classification et évitent d'éparpiller les `switch` : `AppConfig.IsBifrost(provider)` (clé facultative) et `AppConfig.UsesAnthropicDialect(provider)` (choix du SDK). En mode Bifrost, le nom de modèle est **préfixé par le fournisseur amont** (`openai/…`, `anthropic/…`), convention propre à la passerelle.

---

## 7. Flux fonctionnels

### 7.1 Démarrage
1. `AppConfig.Load()` alimente `AppConfig.Current`.
2. `Program.Main` configure la DI et lance `MainForm`.
3. `MainForm` restaure la taille de la fenêtre et les largeurs de colonnes depuis `AppConfig`.

### 7.2 Ouverture d'une source
1. Dialog `OpenFileDialog` (filtre fourni par `TranslationSourceFactory.OpenFileFilter` : `.xlsx`, `.sln`, `.slnx`) → `_currentFilePath`.
2. `TranslationSourceFactory.Create(path)` construit `_currentSource` selon l'extension.
3. `source.Load(Languages, progress)` charge les lignes (progress bar alimentée par `SourceLoadProgress(Done, Total)` — en lignes lues pour l'Excel, en fichiers neutres traités pour le .resx).
4. Les entrées `@Invariant` sont ignorées (les deux sources).
5. `row.SelectLanguage(code)` positionne la vue active : les sources remplissent les dictionnaires par code de langue mais ignorent la langue affichée.
6. Les filtres de l'UI sont réinitialisés, `DataSource` assigné, et `btnMerge` activé **seulement si** `source.SupportsMerge`.

### 7.3 Affichage / édition
- `DataGridView` avec colonnes :
  - Projet / Fichier / Clé (insérées par code, masquables)
  - Français (Designer, lecture seule)
  - Traduction (Designer, éditable)
  - Commentaire (inséré par code, lecture seule)
- `CellFormatting` colorie Traduction + Commentaire selon `QualityScore.TryParse(row.Comment)`.
- `CellEndEdit` sur Traduction → `ITranslationService.UpdateTranslationCache(...)`.

### 7.4 Changement de langue
- Clic sur un drapeau de la toolbar.
- Chaque `TranslationRow.SwitchLanguage(oldCol, newCol)` bascule la vue active.
- Filtres réinitialisés, status bar mise à jour, `AppConfig.SelectedLanguageCode` persisté.

### 7.5 Filtrage
- TextBox superposé à l'en-tête (toutes colonnes sauf Commentaire) + debounce 300 ms.
- ComboBox superposé à l'en-tête Commentaire (filtres par score).
- `TranslationRowFiltering.Filter(_allRows, _filters)` recalcule le snapshot affiché.
- `ApplyFiltersPreservingSelection()` conserve la sélection et le scroll lors des ré-applications.

### 7.6 Tri
- Clic sur l'en-tête d'une colonne → tri asc/desc (`dataGridView.Sort`).
- Indicateur (triangle) dessiné à droite du titre dans `CellPainting`.

### 7.7 Traduire / vérifier (IA)
Menu contextuel sur `colTranslation` :
- **Auto-traduire (copie existante)** : recopie des traductions connues pour le même français — sans appel IA.
- **Traduire** : via IA (batch).
- **Vérifier la traduction** : via IA (batch).
- **Extraire les termes métier…** : déclenche l'extraction IA et ouvre `GlossaryExtractionDialog`.

En multi-sélection : mêmes actions sur toute la sélection. Progress bar alimentée via `IProgress<int>`; `UseWaitCursor` pendant l'appel. Avertissement si des réponses IA n'ont pas pu être extraites.

### 7.8 Rafraîchir (bouton ou F5)
- **Fichier chargé** : `ExcelService.LoadWithRowProgress(...)` recharge depuis le disque. Détection des lignes dont le français/commentaire source a changé → confirmation utilisateur avant d'écraser les valeurs en mémoire. Les traductions des lignes inchangées sont conservées.
- **Pas de fichier / après édition** : `ApplyFiltersPreservingSelection()` réapplique filtres + tri.
- L'indicateur `Rafraîchir *` (étoile) s'affiche après une édition/traduction/vérification pour signaler qu'une ré-application filtres/tri est utile.

### 7.9 Sauvegarde
- `MainForm` pousse d'abord la vue active dans les dictionnaires (`row.CommitActiveLanguage(code)`), puis appelle `_currentSource.Save(rows, Languages)`. Sans ce commit, la langue affichée ne serait pas sauvegardée : elle ne vit que dans `Translation` / `Comment`.
- Source Excel : réécriture des cellules de toutes les langues connues. Les commentaires de vérification (format score) sont aussi persistés dans les colonnes Excel correspondantes.
- Source .resx : réécriture des seules variantes de culture réellement modifiées ; le fichier neutre n'est jamais touché.
- Toute la durée de l'opération est encadrée par `SetWritingState(true/false)` (bloc `finally`) : toolbar et grille désactivées, fermeture de la fenêtre refusée, status bar « Sauvegarde en cours… ».

### 7.10 Fusion (source Excel uniquement)
0. `SupportsMerge` vaut `false` pour la source .resx : le bouton est désactivé et `BtnMerge_Click` sort immédiatement.
1. Dialog → `_destinationFilePath`, puis `SetWritingState(true)` — le verrou couvre **toute** la fusion (analyse des différences, dialogs de résolution, écriture disque) et est levé dans le `finally`. Les `MergeDifferenceForm` restent utilisables : ce sont des dialogs modaux, la désactivation du parent ne les affecte pas.
2. `ExcelService.GetMergeSourceDifferences(...)` détecte toutes les lignes où le français ou le commentaire source diffère entre source et destination.
3. Pour chaque différence, `MergeDifferenceForm` s'ouvre. L'utilisateur choisit les champs à reporter (ou annule la fusion globalement).
4. `ExcelService.Merge(...)` applique les résolutions. Les lignes sans divergence source → report systématique de la traduction. Retour : nombre de lignes mises à jour.

### 7.11 Glossaire
- **Édition manuelle** : bouton toolbar `btnGlossary` → `GlossaryForm`. L'utilisateur sélectionne une langue, ajoute/modifie/supprime des entrées.
- **Extraction assistée** : menu contextuel "Extraire les termes métier…" sur une sélection → IA propose des candidats → `GlossaryExtractionDialog` → validation → ajout au glossaire de la langue active.
- **Injection dans les prompts** : à chaque appel de `TranslateInBatchesAsync` / `VerifyInBatchesAsync`, `MainForm` construit `glossarySection` (texte) et `glossaryFingerprint` (SHA256) pour la langue active. Le fingerprint invalide automatiquement les entrées de cache périmées.

---

## 8. Détails IA (Translator + TranslationService + Glossaire)

### 8.1 Paramètres globaux
- `BatchSize = 20`
- `Temperature = 0.1f`
- `FixedParallelBatchRequests = 4`
- `RetryCount = 3`
- `AnthropicMaxTokens = 2048`

### 8.2 Progression temps réel
Historiquement, `TranslationService` passait `null` en progress à `Translator` et ne reportait qu'après `Task.WhenAll` — la progress bar sautait de 0 à 100 % à la fin. Depuis la PR #7 :
- `Translator.ProcessBatchesAsync` accepte un callback `Action<IReadOnlyList<T>, string[]>? onBatchCompleted` invoqué dans chaque tâche parallèle juste après réception des résultats d'un batch.
- `TranslationService` passe un callback qui : remplit le cache, mappe les résultats aux indices originaux (via `pendingByText` / `pendingByPair`), incrémente `completed` via `Interlocked.Add`, reporte la progression.
- Résultat UX : la status bar s'incrémente par paliers de ~20 au rythme des retours API (toutes les 3–10 s).

### 8.3 Glossaire dans les prompts
Les prompts par défaut contiennent un placeholder `{glossary}`. Juste avant l'appel API :
```csharp
systemPrompt = systemPrompt
    .Replace("{language}", targetLanguage)
    .Replace("{glossary}", glossarySection ?? string.Empty);
```
Le `glossarySection` est construit par `GlossaryService.BuildGlossarySection(...)` sous forme :
```
Glossaire à respecter (Source → Destination) :
- "ABC" → "XYZ" (contexte : …)
- …
```
(chaîne vide si aucune entrée → le placeholder disparaît du prompt).

### 8.4 Invalidation de cache via fingerprint
La clé de cache inclut `GlossaryFingerprint` = SHA256 hex des entrées triées `(Source|Destination|Context)`. Dès qu'une entrée est ajoutée, modifiée ou supprimée, le fingerprint change → les anciennes clés ne matchent plus → les lignes concernées seront retraduites à la prochaine demande.

---

## 9. Conventions de code

- **Namespace unique** : `CheckTranslation` pour tout le projet, indépendamment des sous-dossiers.
- **Nullable enable** + annotations `?` explicites.
- **Implicit usings** enable.
- **File-scoped namespaces** (`namespace CheckTranslation;`).
- **Modificateurs d'accès** : `internal` pour les classes d'assembly (`TranslationRow`, `AppConfig`, services, modèles de fusion…). `public` uniquement pour les classes héritant de `Form`.
- **Records** pour les modèles immuables : `MergeDifference`, `MergeRowSnapshot`, `MergeDifferenceResolution`, `LanguageInfo`, `ExcelLoadProgress`.
- **Sections commentées** : `// --- Nom de section ---` pour structurer les gros fichiers (ex. `MainForm.cs`).
- **Nommage** :
  - UI (champs de contrôles) : **sans** underscore, ex. `btnOk`, `grid`, `languageCombo`.
  - État non-UI (champs privés) : **avec** underscore, ex. `_glossaryService`, `_dirty`, `_allRows`.
- **PascalCase** pour membres publics, **camelCase** pour variables locales et paramètres.

---

## 10. Avertissements importants

- **Ne PAS modifier `*.Designer.cs` à la main** — générés par le designer WinForms. Toute modif manuelle doit être reproductible par le designer, sinon elle sera écrasée.
- **Ne PAS modifier les `*.resx` manuellement** — générés par le designer.
- **Les `.resx` ont des `LogicalName` explicites dans le `.csproj`** — nécessaire car ils sont dans un sous-dossier `Forms/`. Ne pas supprimer ces entrées (`MainForm`, `ConfigForm`, `GlossaryForm`, `GlossaryExtractionDialog`).
- **Les colonnes Projet/Fichier/Clé et Commentaire sont créées programmatiquement** dans `MainForm` (`InitDetailsColumns`, `InitCommentColumn`) et insérées autour des colonnes du Designer. Ne pas les ajouter dans `MainForm.Designer.cs`.
- **Le bouton `Rafraîchir` est ajouté programmatiquement** (`InitRefreshButton` dans la section « Bouton Rafraîchir + F5 ») puis disposé par `ArrangeToolStripItems`.
- **Les ComboBox de filtre par score sont créés par code** (`TryCreateSpecialFilterControl` dans la section « Filtre par score de vérification ») et superposés aux en-têtes du DataGridView.
- **Fusion Excel** : résolution ligne-par-ligne via `MergeDifferenceForm` pour CHAQUE ligne divergente. L'utilisateur peut annuler la fusion globalement.
- **Écritures disque protégées par `SetWritingState`** — ne PAS revenir à un `btnXxx.Enabled = false` ponctuel dans `BtnSave_Click` / `BtnMerge_Click` : le verrou global est ce qui bloque aussi la fermeture (`_isWriting` lu par `MainForm_FormClosing`). Toujours appeler `SetWritingState(false)` dans un `finally`.
- **Métriques des filtres calculées au runtime** — pas de constantes en pixels : `InitFilterPanel` mesure police et `TextBox.PreferredHeight`, `UpdateFilterPanelLayout` / `DataGridView_CellPainting` / `TryLayoutSpecialFilterControl` doivent rester synchronisés sur ces champs, sinon les filtres se désalignent en DPI 125/150/200 %.
- **L'aperçu Markdown des prompts retire l'extension *generic attributes* de Markdig** — `UseAdvancedExtensions()` l'embarque, et elle lit `{...}` comme un bloc d'attributs HTML : `{language}` et `{glossary}` disparaissaient de l'aperçu sans erreur, à l'endroit précis où l'utilisateur vérifie qu'ils sont posés. Ne pas revenir à un `UseAdvancedExtensions()` nu.
- **La taille du `ConfigForm` est bornée à l'écran au chargement** (`FitToWorkingArea`) — le dialogue dépasse un écran 1080p dès 125 % de mise à l'échelle et les boutons OK / Annuler sortent du champ. La `MinimumSize` doit être bornée **avant** la fenêtre, sinon elle interdit le rétrécissement.
- **Les champs de fournisseur sont réétirés au chargement** (`StretchProviderFields`) — ancrés gauche+droite dans un `SplitContainer`, ils figent leur distance d'ancrage avant que `EndInit` n'applique le `SplitterDistance`, et s'affichent bien plus étroits que ce que le Designer indique. Élargir les champs dans le Designer ne corrigerait rien.
- **`ConfigForm.BuildCurrentConfig()` reconstruit un `AppConfig` complet** — tout nouveau champ persistant non édité dans le dialog doit y être recopié depuis `AppConfig.Current`, faute de quoi un clic sur OK le réinitialise (cas vécu avec `WindowWidth/Height` et les `ColumnFillWeights`).
- **Les `.resx` et les `.slnx` sont lus par nom local, jamais par nom qualifié** — un fichier déclarant un espace de noms ne ramènerait alors plus rien, *sans erreur* : il paraîtrait simplement vide. `ResxReader.Children` / `Attribute` et `SolutionReader.ReadSlnxProjectPaths` portent cette règle.
- **Un chargement .resx qui ne ramène rien doit se dire** — `ResxLoadReport.Describe()` nomme l'étape où la chaîne s'est arrêtée (aucun projet reconnu, aucun `.resx`, toutes les entrées exclues) et `MainForm` l'affiche. Une grille vide seule est indiscernable d'une solution sans traductions.
- **Écriture .resx : ne jamais toucher le fichier neutre** — le français est en lecture seule dans l'UI ; `ResxReader.Save` n'écrit que les variantes `<stem>.<code>.resx`.
- **Écriture .resx chirurgicale** — chargement en `PreserveWhitespace` et réécriture seulement si le contenu a changé : c'est ce qui garantit un diff minimal dans le gestionnaire de sources et une sauvegarde idempotente. Ne pas remplacer par une regénération complète du document.
- **Une langue est identifiée par son code, pas par une colonne** — `TranslationRow.Translations` / `Comments` sont indexés par code (`« de-DE »`). Le numéro de colonne Excel (`LanguageInfo.Column`) ne doit rester connu que de `ExcelReader`.
- **`Input.xlsx` est binaire** — ne pas tenter de le lire en texte. La lecture se fait via ClosedXML.
- **Le projet cible `net8.0-windows`** — Windows uniquement, SDK .NET 8 requis.
- **Icône du bouton Glossaire** : `LoadGlossaryIcon()` teste l'existence de `Resources/glossary.png` et retombe sur `Resources/config.png` si absent.
- **Clé API facultative en mode Bifrost uniquement** — `HasApiConfig` et `Translator.ResolveApiKey` s'appuient sur `AppConfig.IsBifrost`. Ne pas rendre la clé facultative pour les accès directs : l'appel partirait et échouerait côté serveur au lieu d'être bloqué en amont.
- **Les quatre `RadioButton` de fournisseur vivent dans des containers différents** (les panneaux des deux `SplitContainer` de `grpAuth`) : WinForms ne gère donc pas l'exclusivité, elle est faite à la main dans `ProviderChanged` sous le garde-fou `_isUpdatingProvider`. Tout nouveau fournisseur doit être ajouté à `ProviderButtons`, sinon il ne sera ni sélectionnable ni relu.
- **Annulation IA non disponible** : pas de `CancellationToken` exposé côté UI. Fermer l'app pendant un gros batch est tolérable mais brutal.
- **Caches mémoire non persistés** : perdus à la fermeture.

---

## 11. Tests

**État actuel** : aucun framework de test, aucun projet de test dans la solution.

**Candidats prioritaires** pour un futur `CheckTranslation.Tests/` (xUnit ou NUnit) :
- `Logic/QualityScore` (parsing + calcul couleur)
- `Logic/TranslationRowFiltering` (texte + pseudo-filtres score)
- `Services/TranslationService` (cache, dédup, invalidation par fingerprint)
- `Services/ExcelReader.Merge` (fusion + conflits)
- `Services/Translator.ParseNumberedList` (robustesse du parsing)
- `Models/AppConfig` (round-trip DPAPI + compat legacy)

**Pas de CI/CD** configuré actuellement. Pas d'`.editorconfig` ni d'analyseurs Roslyn / StyleCop.

---

## 12. Suivi chronologique

| Date | Changement |
|------|------------|
| 2026-01-XX | Version initiale |
| 2026-03-06 | Hygiène WinForms Designer : nettoyage de `ConfigForm.Designer.cs` |
| 2026-03-06 | DI : ajout de `IExcelService`/`ITranslationService` + injection dans `MainForm` |
| 2026-03-06 | Séparation UI/Logic : extraction de `Logic/QualityScore.cs` + `Logic/TranslationRowFiltering.cs` |
| 2026-03-07 | Chargement Excel : progress bar en lignes lues (`ExcelLoadProgress` + `LoadWithRowProgress`) |
| 2026-03-07 | Auto-traduction (sans IA) : copie des traductions déjà présentes (menu contextuel) |
| 2026-03-13 | Chargement fichier : réinitialisation des champs de filtre après `Ouvrir` |
| 2026-03-13 | Cache traductions IA : cache mémoire par texte/langue/config + dédup + update manuel |
| 2026-03-13 | Emplacement config : déplacement vers `%LocalAppData%\CheckTranslation` avec compat lecture |
| 2026-03-13 | Cache vérifications IA : cache mémoire distinct + compteur dédié dans la status bar |
| 2026-03-13 | Persistance vérification : sauvegarde des commentaires de vérification dans l'Excel |
| 2026-03-13 | Appels API parallèles : `Task.WhenAll` + `SemaphoreSlim` pour limiter le rate limiting |
| 2026-03-20 | Boutons « Vider cache » dans `ConfigForm` |
| 2026-03-27 | Filtre par score de vérification : ComboBox dans l'en-tête Commentaire |
| 2026-04-03 | Bouton Rafraîchir + F5 : rechargement fichier + détection changements source |
| 2026-04-10 | Persistance de disposition : taille fenêtre + largeurs colonnes (2 dictionnaires selon mode) |
| 2026-04-15 | Fusion Excel : report traductions source→destination avec résolution des conflits |
| 2026-04-17 | Refactorisation de la gestion des différences de fusion |
| 2026-04-17 | Progression temps réel des batchs IA : callback `onBatchCompleted` (PR #7) |
| 2026-04-18 | Glossaire métier : service + éditeur + extraction IA + injection dans prompts + invalidation par fingerprint (PR #8) |
| 2026-04-18 | Nettoyage layout `MergeDifferenceForm` : fenêtre élargie, marges compactes |
| 2026-04-18 | Migration des boutons « Vider cache » de `ConfigForm` vers le Designer (PR #9) |
| 2026-04-18 | Migration de `GlossaryForm` et `GlossaryExtractionDialog` vers le pattern Designer + resx (PR #10) |
| 2026-04-18 | Fusion des 3 partials de `MainForm` (`LayoutPersistence`, `ManualRefreshSupport`, `VerificationScoreFilterSupport`) dans `MainForm.cs` avec sections commentées (PR #11) |
| 2026-04-20 | Documentation : fusion de `ANALYSE_PROJET.md` + `ROADMAP.md` dans `CLAUDE.md`, 4 fichiers `.md` → 2 (PR #12) |
| 2026-04-20 | Filtres DPI-aware : hauteurs d'en-tête et de contrôles calculées depuis la police et `LogicalToDeviceUnits` au lieu de constantes en pixels (PR #13) |
| 2026-04-20 | Protection contre la fermeture pendant une écriture disque : `_isWriting` + `SetWritingState`, toolbar et grille désactivées, `FormClosing` annulé (PR #14) |
| 2026-04-20 | Fix : `ConfigForm.BuildCurrentConfig` écrasait les champs de disposition ; double alimentation du cache de vérification supprimée dans `VerifyRowsAsync` (PR #15) |
| 2026-04-20 | Fix review Copilot : compteurs de cache tenant compte du fingerprint glossaire, `LoadIcon` tolérant aux erreurs, `catch` typé dans `AppConfig.Load`, pipeline Polly partagé, `BatchSize` partagé, traces de diagnostic sur l'introspection des SDK (PR #16) |
| 2026-04-20 | Fermeture bloquée : flash non-modal de 3 s dans la status bar à la place du `MessageBox` (PR #17) |
| 2026-04-20 | `BtnMerge_Click` : protection unifiée sous `SetWritingState` pour toute la fusion, en remplacement des `Enabled` bouton par bouton (PR #18) |
| 2026-04-20 | Documentation : suivi et sections remis à jour pour les PR #12 à #18 (PR #19) |
| 2026-08-29 | **Support de la passerelle Bifrost** : deux fournisseurs supplémentaires (`BifrostOpenAI`, `BifrostAnthropic`) à côté des accès directs, clé facultative, listing des modèles factorisé pour les quatre fournisseurs |
| 2026-08-29 | **Lecture / écriture directe des `.resx`** : abstraction `ITranslationSource` (Excel + resx), `SolutionReader` (.sln / .slnx), `ResxReader` (écriture chirurgicale, BOM préservé, idempotente). Les langues sont désormais identifiées par code et non plus par colonne Excel. Fusion toujours réservée à la source Excel |
| 2026-08-29 | Import de solution : compte rendu de chargement (`ResxLoadReport`) affiché quand aucune ligne n'est trouvée ; `.slnx` lu quel que soit l'espace de noms |
| 2026-08-29 | Fix import `.resx` : les `<data>`, `<value>`, `<comment>` et leurs attributs sont lus par nom local — un `.resx` déclarant un espace de noms était lu comme vide, sans erreur. Compte rendu enrichi (éléments rencontrés, entrées binaires, métadonnées designer) |
| 2026-08-29 | Fix `ConfigForm` : placeholders `{language}` / `{glossary}` rendus dans l'aperçu (extension Markdig *generic attributes* retirée), fenêtre bornée à l'écran, champs de fournisseur réétirés à leur panneau |

---

## 13. Roadmap

Légende : 🔴 haute priorité · 🟡 moyenne · 🟢 basse / nice-to-have.

### 13.1 Architecture & maintenabilité

| Prio | Axe | État | Amélioration suggérée |
|:--:|---|---|---|
| 🔴 | **Tests** | Aucun | Projet `CheckTranslation.Tests` (xUnit/NUnit). Candidats prioritaires listés en §11. |
| 🟡 | **Séparation UI/Logic** | Partielle (`Logic/` extrait, `MainForm` désormais en sections commentées) | Extraire un `MainFormViewModel`/présenteur pour chargement/sauvegarde/traduction/fusion |
| 🟢 | **Prompts externalisés** | En dur dans `AppConfig.cs` | Déplacer dans des fichiers `.md` séparés (édition facile) |

### 13.2 Performance

| Prio | Axe | État | Amélioration suggérée |
|:--:|---|---|---|
| 🟢 | **Appels API parallèles** | ✅ `Task.WhenAll` + `SemaphoreSlim(4)` | Ajuster selon quotas réels des providers |
| 🟢 | **Chargement Excel** | ✅ Progress en lignes lues | Streaming / `VirtualMode` pour >50k lignes |
| 🟢 | **Cache traductions** | ✅ Mémoire + dédup + fingerprint glossaire | Persistance disque entre sessions |
| 🟢 | **DataGridView** | Double-buffering activé | `VirtualMode` pour très gros fichiers |

### 13.3 UX / Interface

| Prio | Axe | État | Amélioration suggérée |
|:--:|---|---|---|
| 🔴 | **Annulation** | Aucune | `CancellationToken` + bouton « Annuler » |
| 🟡 | **Historique Undo/Redo** | Aucun | Au moins 1 niveau |
| 🟢 | **Recherche globale** | Filtres par colonne uniquement | Recherche toutes colonnes |
| 🟢 | **Export** | Sauvegarde in-place uniquement | « Enregistrer sous » |
| 🟢 | **Thème sombre** | Non | Détecter le thème Windows |
| 🟡 | **Raccourcis clavier** | F5 seulement | Ctrl+S, Ctrl+O, Ctrl+T (traduire), Ctrl+V (vérifier), Ctrl+M (fusion) |
| 🟡 | **Fusion en mode .resx** | Non (Excel uniquement) | Étendre `GetMergeSourceDifferences` / `Merge` à une seconde arborescence .resx |
| 🟡 | **Fusion : résolution en masse** | 1 dialog par ligne divergente | « Tout appliquer » / « Tout ignorer » / « Appliquer aux similaires » |

### 13.4 Robustesse & sécurité

| Prio | Axe | État | Amélioration suggérée |
|:--:|---|---|---|
| 🟢 | **Retry API** | ✅ Polly (408/429/5xx + exceptions transitoires) | Ajuster selon limites réelles |
| 🟡 | **Timeout explicite** | Aucun | Timeout configurable sur les appels IA |
| 🟢 | **Validation prompts** | Aucune | Vérifier la présence de `{language}` |
| 🟡 | **Fichier Excel verrouillé** | Exception brute | Détecter + proposer copie temporaire |
| 🟢 | **Backup auto** | Aucun | `.bak` avant chaque sauvegarde |
| 🟡 | **Traduction partielle** | Entrée IA vide → placeholder | Restaurer l'ancienne valeur + message explicite |

### 13.5 Fonctionnalités à ajouter

| Prio | Fonctionnalité | Description |
|:--:|---|---|
| 🟡 | **Traduction auto au chargement** | Option pour traduire automatiquement les cellules vides |
| 🟢 | **Comparaison avant/après** | Vue diff depuis le chargement |
| 🟢 | **Statistiques** | Dashboard : % traduit, % vérifié, scores moyens par langue |
| 🟢 | **Export rapport** | HTML/PDF des vérifications |
| 🟡 | **Mode batch CLI** | Version console pour CI/CD |
| 🟢 | **Multi-fichiers** | Onglets pour plusieurs fichiers simultanés |
| 🟢 | **Historique des appels IA** | Logger requêtes/réponses pour debug |

### 13.6 DevOps & qualité

| Prio | Axe | État | Amélioration suggérée |
|:--:|---|---|---|
| 🔴 | **CI/CD** | Aucun | GitHub Actions : build + tests + release |
| 🟢 | **Version affichée** | Non | Titre ou About (`AssemblyVersion`) |
| 🟡 | **Logging** | `Debug.WriteLine` | Serilog/NLog + fichier rotatif |
| 🟢 | **Analyseurs** | Aucun | `.editorconfig` + StyleCop |
| 🟢 | **Documentation** | README à jour | Screenshots, GIF de démo |

### 13.7 Quick wins

| État | Amélioration |
|---|---|
| Partiel | **Bouton « Copier »** sur les cellules (français fait, traduction à ajouter) |
| À faire | **Tooltip** sur cellules tronquées |
| À faire | **Compteur de caractères** traduction vs source |
| À faire | **Indicateur « non sauvegardé »** dans le titre |
| À faire | **Raccourci F2** : éditer la cellule sélectionnée |
| À faire | **Son/notification** fin de batch (app en arrière-plan) |

---

*Document vivant — à maintenir à jour au fil des PR.*
