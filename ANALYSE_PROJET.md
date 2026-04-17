# Analyse détaillée — projet `CheckTranslation`

## 1) Résumé
`CheckTranslation` est une application de bureau **Windows Forms** en **C# / .NET 8** (`net8.0-windows`) destinée à **contrôler, traduire et vérifier** des chaînes de ressources (type `.resx`) à partir d'un **export Excel** généré par ResX Resource Manager. L'application charge un fichier `.xlsx`, affiche les entrées dans un `DataGridView`, permet l'édition de la traduction dans une langue cible, puis sauvegarde les modifications dans le fichier Excel. Elle peut aussi appeler des API **OpenAI** et **Anthropic** pour traduire et vérifier des traductions en lot, avec cache mémoire pendant la session, et fusionner des traductions d'un fichier source vers un fichier destination avec résolution des conflits.

## 2) Périmètre technique

### Cible et runtime
- Framework cible : `.NET 8` Windows (`net8.0-windows`)
- Type d'application : `WinExe`
- UI : `Windows Forms`
- Options : `Nullable=enable`, `ImplicitUsings=enable`

### Dépendances NuGet
- `ClosedXML` `0.104.2` : lecture/écriture de fichiers Excel `.xlsx`
- `OpenAI` `2.8.0` : client OpenAI (chat completions + listing de modèles)
- `Anthropic` `12.8.0` : client Anthropic (messages + listing de modèles)
- `Markdig` `0.38.0` : rendu Markdown pour l'aperçu des prompts
- `Polly` `8.5.2` : résilience (retry + backoff exponentiel + jitter) sur les appels API
- `Microsoft.Extensions.DependencyInjection` `9.0.0` : conteneur DI utilisé au démarrage

## 3) Structure du projet (fichiers clés)

- `Program.cs` : point d'entrée (STA), charge la config, construit le conteneur DI, résout et lance `MainForm`
- `Forms/MainForm.cs` (+ partials `MainForm.LayoutPersistence.cs`, `ManualRefreshSupport.cs`, `VerificationScoreFilterSupport.cs` et `MainForm.Designer.cs`) : UI principale (tableau, filtres, tri, menus contextuels, langues, fusion, rafraîchissement, persistance de disposition)
- `Forms/ConfigForm.cs` (+ `ConfigForm.Designer.cs`) : dialogue de configuration des paramètres API, prompts et boutons de vidage de cache
- `Forms/MergeDifferenceForm.cs` (+ Designer) : dialogue modal de résolution d'un conflit de fusion (source vs destination)
- `Models/AppConfig.cs` : persistance de configuration (JSON) + chiffrement DPAPI pour les clés API + préférences UI (taille fenêtre, largeur colonnes, langue, détails)
- `Models/TranslationRow.cs` : modèle en mémoire d'une ligne de traduction et stockage multi-langues
- `Models/MergeDifference.cs`, `MergeRowSnapshot.cs`, `MergeDifferenceResolution.cs` : modèles immuables pour le workflow de fusion
- `Services/IExcelService.cs` / `ExcelService.cs` : façade service sur `ExcelReader` pour l'usage en DI
- `Services/ExcelReader.cs` : chargement/sauvegarde/fusion Excel via ClosedXML, filtrage `@Invariant`, détection de différences
- `Services/ExcelLoadProgress.cs` : `record struct(Done, Total)` pour la progression ligne-par-ligne
- `Services/ITranslationService.cs` / `TranslationService.cs` : cache mémoire (traduction + vérification), déduplication, découpage en batch
- `Services/Translator.cs` : appels bruts OpenAI/Anthropic + parallélisation et retry Polly
- `Logic/QualityScore.cs` : parsing du score (`XXX - commentaire`, 0..100) et calcul de la couleur de fond (dégradé rouge → vert)
- `Logic/TranslationRowFiltering.cs` : filtrage côté client (texte + pseudo-filtres `score<N`, `score>=N`, `score:none`)
- `Controls/SortableBindingList.cs` : `BindingList<T>` triable côté client (utilisé pour le tri du `DataGridView`)

## 4) Flux fonctionnel de bout en bout

1. **Démarrage**
   - `AppConfig.Load()` charge `CheckTranslation.config.json` depuis `%LocalAppData%\CheckTranslation` (ou l'ancien emplacement si présent) et alimente `AppConfig.Current`.
   - `Program.Main` configure un `ServiceCollection` (IExcelService, ITranslationService en singleton ; ConfigForm et MainForm en transient avec factory `Func<ConfigForm>`).
   - `Application.Run(MainForm)` est résolu par le conteneur.

2. **Ouverture d'un Excel**
   - L'utilisateur sélectionne un fichier `.xlsx`.
   - `ExcelService.LoadWithRowProgress(filePath, allColumns, activeColumn, progress)` charge les lignes, avec une progress bar alimentée par `ExcelLoadProgress(Done, Total)` toutes les 10 lignes (et aux bornes).
   - Les lignes contenant `@Invariant` dans la colonne de commentaire Excel sont ignorées.
   - Les filtres de l'UI sont réinitialisés pour éviter un affichage partiel post-chargement.

3. **Affichage / édition**
   - Les lignes sont affichées dans un `DataGridView` (colonnes Projet/Fichier/Clé insérées par code + colonnes Français/Traduction du Designer + colonne Commentaire insérée par code).
   - La colonne *traduction* est éditable ; le français est en lecture seule.
   - `DataGridView_CellFormatting` applique une couleur de fond selon le score extrait du commentaire (`QualityScore.GetBackColor`).
   - La sortie d'édition (`CellEndEdit`) met à jour le cache traduction (`ITranslationService.UpdateTranslationCache`).

4. **Changement de langue**
   - La barre d'outils contient des boutons (drapeaux) pour choisir la langue active.
   - Lors du changement, chaque `TranslationRow` bascule ses propriétés actives via `SwitchLanguage(oldCol, newCol)`.
   - Les filtres sont réinitialisés, les statuts du cache (traduction/vérification) et l'en-tête des colonnes sont mis à jour.
   - La langue sélectionnée est persistée dans `AppConfig.SelectedLanguageCode`.

5. **Filtrage**
   - Filtre texte par colonne (TextBox superposé à l'en-tête), debounced 300 ms.
   - Filtre spécial pour la colonne `Commentaire` via une `ComboBox` : "Non vérifiés" / "≤ 50..100" / "≥ 90" → traduit en pseudo-filtres `score:none`, `score<N`, `score>=N`.
   - Délégué à `TranslationRowFiltering.Filter(...)` puis rebind de la `SortableBindingList`.

6. **Tri**
   - Clic sur l'en-tête d'une colonne ; indicateur dessiné à droite dans l'en-tête.

7. **Traduire / vérifier (IA)**
   - Clic droit sur une cellule de traduction :
     - Auto-traduire (copie existante) : recopie des traductions connues pour le même français (sans appel IA)
     - Traduire : via IA
     - Vérifier la traduction : via IA
   - En multi-sélection : même actions sur la sélection (batch).
   - Progress bar alimentée via `IProgress<int>`; `UseWaitCursor` pendant l'appel.

8. **Rafraîchir (F5 / bouton)**
   - Si un fichier est chargé : recharge le fichier depuis le disque, détecte les changements de français/commentaire source, demande confirmation à l'utilisateur et conserve les traductions déjà en mémoire pour les lignes inchangées.
   - Si aucun fichier chargé ou après édition : ré-applique filtres/tri en conservant la sélection (via `ApplyFiltersPreservingSelection`).

9. **Sauvegarde**
   - `ExcelReader.Save(filePath, activeColumn, rows)` réécrit toutes les traductions de `row.Translations` et les commentaires de `row.Comments` dans les colonnes correspondantes.

10. **Fusion (Merge)**
    - Sélection d'un fichier destination.
    - `ExcelReader.GetMergeSourceDifferences` détecte, pour chaque ligne ayant la même `SyncKey = Project|File|Key`, les cas où le français ou le commentaire source diffère.
    - Pour chaque différence, `MergeDifferenceForm` est ouvert : l'utilisateur choisit s'il faut reporter (a) le français+commentaire et/ou (b) la traduction+commentaire de traduction, ou annuler la fusion.
    - `ExcelReader.Merge(...)` applique les résolutions, écrit les cellules attendues et sauvegarde le workbook ; un compteur de lignes mises à jour et de lignes ignorées est remonté.

## 5) Modèle de données

### `TranslationRow`
Une ligne de ressource (venant du `.xlsx`) :
- Identité : `Project`, `File`, `Key` (+ helper `GetSyncKey()` = `Project|File|Key` avec séparateur `\u001F`)
- Source : `French`, `FrenchComment`
- Vue langue active : `Translation`, `Comment`
- Stockage multi-langues (par colonne Excel) :
  - `Translations: Dictionary<int,string>`
  - `Comments: Dictionary<int,string>`
- `RowNumber` : ligne Excel d'origine (utilisée pour écrire au bon endroit à la sauvegarde)

Méthode clé : `SwitchLanguage(oldColumn, newColumn)`
- pousse `Translation` / `Comment` dans les dictionnaires pour l'ancienne colonne
- recharge la valeur correspondant à la nouvelle colonne

Ce design évite de recharger l'Excel à chaque changement de langue : tout est en mémoire.

### Fusion
- `MergeRowSnapshot(Project, File, Key, French, FrenchComment, Translation, TranslationComment)` : record immuable utilisé par le dialog.
- `MergeDifference(SyncKey, Source, Destination)` : paire source/destination affichée.
- `MergeDifferenceResolution(UpdateFrenchAndComment, UpdateTranslationAndComment)` : décision utilisateur par ligne (propriété dérivée `HasAnyChange`).

## 6) Lecture/écriture Excel

### Hypothèse de format (ResX Resource Manager export)
Les colonnes sont 1-indexées :
- A (`1`) : Project
- B (`2`) : File
- C (`3`) : Key
- D (`4`) : Comment source (contient `@Invariant` pour ignorer)
- E (`5`) : French

Les colonnes de traduction (par langue) et la colonne de commentaire précédente (`col-1`) sont passées à `ExcelReader.Load(...)`.

### `ExcelReader.Load(...)`
- Ouvre le workbook via `ClosedXML`.
- Itère des lignes 2..lastRow.
- Ignore la ligne si `Comment` (col D) contient `@Invariant`.
- Renseigne :
  - `TranslationRow.RowNumber`, `Project`, `File`, `Key`, `FrenchComment`, `French`
  - `Translations[col]` = valeur cellule `(r, col)`
  - `Comments[col]` = valeur cellule `(r, col-1)` (commentaires de langue en colonne précédente)
- Définit la langue active :
  - `row.Translation` = `row.Translations[activeColumn]`
  - `row.Comment` = `row.Comments[activeColumn]`
- Rapporte la progression via `IProgress<ExcelLoadProgress>` (tous les 10 lignes + bornes) pour alimenter la status bar sans saturer le thread UI.

### `ExcelReader.Save(...)`
- Synchronise `Translation` → `Translations[activeColumn]` et `Comment` → `Comments[activeColumn]`.
- Réécrit toutes les traductions et commentaires stockés en mémoire dans la feuille (colonnes `col` et `col-1`).
- Sauvegarde le fichier.
- `WriteCellValue` double une apostrophe de tête pour la préserver (évite l'interprétation Excel).

### `ExcelReader.GetMergeSourceDifferences(...)` + `ExcelReader.Merge(...)`
- Construisent un index des lignes source par `SyncKey = Project|File|Key`.
- Itèrent les lignes de la destination ; pour chaque `SyncKey` connue non-@Invariant, comparent le français et le `FrenchComment` source/destination.
- `GetMergeSourceDifferences` renvoie la liste des paires qui diffèrent pour permettre à l'UI de demander une décision par ligne.
- `Merge(...)` applique chaque décision :
  - Si aucune différence source → report systématique de la traduction (`UpdateTranslationAndComment = true`)
  - Sinon, applique la résolution fournie (peut mettre à jour le français/commentaire source et/ou la traduction/commentaire traduction)
- Renvoie le nombre de lignes modifiées.

## 7) Configuration (`AppConfig`)

### Stockage
- Fichier : `CheckTranslation.config.json` dans `%LocalAppData%\CheckTranslation`.
- Contenu : prompts (`TranslatePrompt`, `VerifyPrompt`) + choix du fournisseur IA (`Provider`) + paramètres par fournisseur (clé/endpoint/modèle) + préférences UI (`ShowDetails`, `SelectedLanguageCode`, `WindowWidth/Height`, `ColumnFillWeightsWithDetails/WithoutDetails`).

Compatibilité : l'ancien emplacement dans `AppContext.BaseDirectory` reste lu si le nouveau fichier n'existe pas encore. Les champs legacy `Key`/`Url`/`ModelName` (OpenAI uniquement) sont toujours reconnus au chargement.

### Sécurité clé API
- Les clés API sont chiffrées via DPAPI (`ProtectedData.Protect`) avec `DataProtectionScope.CurrentUser`.
- À la lecture, la clé est déchiffrée ; si le déchiffrement échoue (mauvais format / autre utilisateur), le code retourne la valeur telle quelle.

### Formulaire de configuration
- `ConfigForm` affiche et sauve :
  - `TranslatePrompt` / `VerifyPrompt` (avec onglets Édition + Aperçu Markdown via `Markdig`)
  - fournisseur (`OpenAI` / `Anthropic`) via boutons radio
  - paramètres **OpenAI** : `OpenAiKey`, `OpenAiUrl`, `OpenAiModelName`
  - paramètres **Anthropic** : `AnthropicKey`, `AnthropicUrl`, `AnthropicModelName`
  - deux boutons dynamiques ajoutés par code : "Vider cache trad." et "Vider cache vérif." → `ITranslationService.ClearTranslationCache/ClearVerificationCache`

## 8) UI principale (`MainForm`) — comportement

La classe est découpée en plusieurs `partial class` :

- `Forms/MainForm.cs` : chargement/sauvegarde, langues, filtres texte, menu contextuel, traduction/vérification IA, colorisation par score, fusion
- `Forms/MainForm.LayoutPersistence.cs` : persistance (taille fenêtre, largeur colonnes selon le mode détails on/off) au chargement et à la fermeture
- `Forms/ManualRefreshSupport.cs` : bouton `Rafraîchir` ajouté programmatiquement, raccourci F5, rechargement fichier avec détection des changements du français/commentaire source et confirmation utilisateur, marquage "rafraîchissement en attente" après traduction/vérification/auto-traduction
- `Forms/VerificationScoreFilterSupport.cs` : ComboBox superposé à l'en-tête de la colonne Commentaire pour filtrer par seuil de score

### Langues
- Définies statiquement via un tableau de `LanguageInfo(Code, Name, Column)` : en-US / de-DE / es-ES / it-IT / nl-NL / pl-PL / zh-CN.
- `Column` correspond à la colonne Excel de traduction pour la langue.

### Colonnes dynamiques "détails"
- `Project`, `File`, `Key` sont insérées en tête par code (et non via le Designer).
- Un bouton toolbar `btnDetails` (icône `columns.png`) permet d'afficher/masquer ces colonnes ; l'état est sauvegardé dans `AppConfig.ShowDetails`. Les largeurs sont persistées dans deux dictionnaires distincts (mode détails visible / masqué), pour éviter d'écraser une disposition soigneusement réglée.

### Filtres par colonne
- Icône "loupe" dessinée dans l'en-tête (`CellPainting`) ; bleue si filtre actif.
- TextBox superposé au bas de l'en-tête (avec debounce 300 ms), ou ComboBox (filtre par score) pour la colonne `Commentaire`.
- `TranslationRowFiltering.Filter(...)` applique les filtres côté client sur un snapshot.
- Reconstruit la source de données (`SortableBindingList<TranslationRow>`).

### Tri
- Clic en-tête : tri asc/desc via `dataGridView.Sort(column, direction)`.
- Indicateur de tri (triangle) dessiné à droite du titre.

### Menu contextuel
- Sur `colTranslation` : Auto-traduire (copie existante) / Traduire / Vérifier la traduction (en mono ou multi-sélection).
- Sur `colFrench` : Copier le texte français (presse-papier, mono ou multi-sélection).
- Les libellés sont adaptés au nombre de lignes sélectionnées à l'ouverture du menu.
- Pendant un batch : désactive Ouvrir/Sauvegarder, met à jour la progress bar, affiche un avertissement si certaines réponses IA n'ont pas pu être extraites (format inattendu).

### Colorisation
- `CellFormatting` lit `QualityScore.TryParse(row.Comment, ...)` et applique un dégradé rouge → vert sur `colTranslation` et `colComment`.

### Rafraîchissement de vue
- Le bouton `Rafraîchir` recharge le fichier (conserve les traductions modifiées, détecte les changements de français/commentaire source, demande confirmation).
- Après traduction/vérification/auto-traduction, un indicateur (`Rafraîchir *`) signale que la vue peut bénéficier d'un ré-application des filtres/tri.
- Pressable via F5 (raccourci `ProcessCmdKey`).

## 9) Service IA (`Translator` + `TranslationService`)

### Appels API (`Translator`)
- OpenAI : `OpenAI.Chat.ChatClient` sur `config.Url` avec `ApiKeyCredential`.
- Anthropic : `AnthropicClient` sur `BaseUrl` normalisé (retrait de `/v1` ou `/v1/messages`), `System` + `User` content, `MaxTokens = 2048`.
- Traductions spécifiques aux erreurs Anthropic (`AnthropicRateLimitException`, `AnthropicIOException`) converties en `HttpRequestException`.

### Paramètres
- `BatchSize = 20` (items par appel IA)
- `Temperature = 0.1f`
- `FixedParallelBatchRequests = 4` (concurrence simultanée)
- `RetryCount = 3` (politique Polly)

### Traduction & Vérification
- `TranslateBatchAsync(texts, ...)` : construit une liste numérotée, demande une réponse strictement numérotée au format `N. texte`.
- `VerifyBatchAsync(pairs, ...)` : requiert `N. XXX - commentaire` et interdit les références croisées entre entrées (rappelé dans le prompt par défaut).

### Traitement par lots
- `TranslateInBatchesAsync` / `VerifyInBatchesAsync` découpent la requête en lots de 20 et les exécutent en parallèle (sémaphore).
- Retry exponentiel avec jitter Polly sur :
  - `HttpRequestException` transitoire (408/429/500/502/503/504, statut nul)
  - `TimeoutException`, `TaskCanceledException`
  - `ClientResultException` avec code 408/429/5xx
- La progression est reportée en *nombre d'items traités*.

### Cache mémoire (`TranslationService`)
- Deux caches distincts (traduction, vérification), `Dictionary<string,string>` protégés par un `lock`.
- Clé traduction : `Provider|Url|Model|Language|Texte` (séparateur `\u001F`).
- Clé vérification : `Provider|Url|Model|Language|French|Translation`.
- Déduplication intra-lot (plusieurs lignes avec le même français n'envoient qu'une seule requête API).
- `UpdateTranslationCache` / `UpdateVerificationCache` pour synchroniser après saisie manuelle / réception d'une réponse valide.
- `GetTranslationCacheCount(config, targetLanguage)` / `GetVerificationCacheCount(...)` alimentent la status bar.
- `ClearTranslationCache(config)` / `ClearVerificationCache(config)` utilisés par les boutons du `ConfigForm`.
- `ChunkResults` re-chunk les résultats en lots de 20 pour coller au contrat de batch du `MainForm`.

### Parsing des réponses batch
- `ParseNumberedList(content, expectedCount)` parse une réponse de liste numérotée (regex `^\s*(\d+)[.)]\s*(.+)$`).
- Supporte des entrées sur plusieurs lignes (concaténation des lignes suivantes non vides).
- Renvoie `string[expectedCount]` (les entrées peuvent être vides si le format IA est non conforme).

## 10) Persistance UI (`MainForm.LayoutPersistence`)

- À la fermeture de la fenêtre : `WindowWidth`/`WindowHeight` sont persistés si l'état n'est pas `Minimized` (en utilisant `RestoreBounds` en mode `Normal`).
- Au chargement : restauration de la taille + largeur de colonnes selon `IsDetailsLayoutActive()` (mode détails visible ou non).
- Les largeurs de colonnes (`FillWeight`) sont sauvegardées/restaurées dans deux dictionnaires distincts, pour préserver la disposition dans chaque mode.
- Un flag `_isRestoringLayout` prévient les rebonds entre `ColumnWidthChanged` et la restauration.

## 11) Points d'attention / limitations observables

- La robustesse des batchs dépend fortement du respect des formats imposés dans les prompts.
- `AppConfig.Load()` retourne un nouvel objet si le fichier n'existe pas, mais n'affecte `Current` que si le fichier existe et est valide (comportement à connaître si on s'attend à `Current` toujours mis à jour dès le démarrage).
- Les icônes des onglets Édition/Aperçu du `ConfigForm` sont chargées depuis `Resources/eyes.png` et `Resources/pencil.png` (avec fallback en dessin GDI+ si absent).
- Les listes de modèles OpenAI/Anthropic dans `ConfigForm` sont chargées au moment de l'ouverture de la combo **Model** (nécessite une clé valide et un accès réseau).
- La fusion ouvre un dialog *par ligne divergente* : peut devenir fastidieux si beaucoup de lignes diffèrent (amélioration possible : tout-reporter / tout-ignorer / appliquer-à-tous).
- Annulation des appels IA non disponible (pas de `CancellationToken` exposé côté UI).
- Les caches mémoire ne sont pas persistés sur disque (perdus à la fermeture).

## 12) Commandes utiles
- `dotnet build`
- `dotnet run`
- `dotnet publish -c Release`

---

Document généré à partir de l'inspection des fichiers sources présents dans le workspace (branche `claude/crazy-golick-1ce14e`, commits récents incluant la fonctionnalité de fusion Excel et la persistance de disposition).
