# ROADMAP.md — Feuille de route et historique

Feuille de route du projet **CheckTranslation** et suivi chronologique des changements.

> Guide technique (architecture, conventions, avertissements) : voir [CLAUDE.md](CLAUDE.md).
> Documentation utilisateur et installation : voir [README.md](README.md).

---

# Feuille de route

Légende : 🔴 haute priorité · 🟡 moyenne · 🟢 basse / nice-to-have.

## 1. Architecture & maintenabilité

| Prio | Axe | État | Amélioration suggérée |
|:--:|---|---|---|
| 🔴 | **Tests** | ✅ `CheckTranslation.Tests` (xUnit, 79 tests : QualityScore, filtres, statistiques, GlossaryDiff / Impact / Excel, ParseNumberedList, caches) | Étendre : `LayoutAnalyzer` (mesure injectée), `ExcelReader.Merge`, `AppConfig` (DPAPI + legacy) |
| 🟡 | **Séparation UI/Logic** | Partielle (`Logic/` extrait, `MainForm` désormais en sections commentées) | Extraire un `MainFormViewModel`/présenteur pour chargement/sauvegarde/traduction/fusion |
| 🟢 | **Prompts externalisés** | En dur dans `AppConfig.cs` | Déplacer dans des fichiers `.md` séparés (édition facile) |

## 2. Performance

| Prio | Axe | État | Amélioration suggérée |
|:--:|---|---|---|
| 🟢 | **Appels API parallèles** | ✅ `Task.WhenAll` + `SemaphoreSlim(4)` | Ajuster selon quotas réels des providers |
| 🟢 | **Chargement Excel** | ✅ Progress en lignes lues | Streaming / `VirtualMode` pour >50k lignes |
| 🟢 | **Cache traductions** | ✅ Mémoire + dédup + fingerprint glossaire | Persistance disque entre sessions |
| 🟢 | **DataGridView** | Double-buffering activé | `VirtualMode` pour très gros fichiers |

## 3. UX / Interface

| Prio | Axe | État | Amélioration suggérée |
|:--:|---|---|---|
| 🔴 | **Annulation** | Aucune | `CancellationToken` + bouton « Annuler » |
| 🟡 | **Historique Undo/Redo** | Aucun | Au moins 1 niveau |
| 🟢 | **Recherche globale** | Filtres par colonne uniquement | Recherche toutes colonnes |
| 🟢 | **Export** | Sauvegarde in-place uniquement | « Enregistrer sous » |
| 🟢 | **Thème sombre** | Non | Détecter le thème Windows |
| 🟡 | **Raccourcis clavier** | F5 seulement | Ctrl+S, Ctrl+O, Ctrl+T (traduire), Ctrl+V (vérifier), Ctrl+M (fusion) |
| 🟡 | **Vérification de débordement** | ✅ Chaîne complète, étalonnée sur le fichier lui-même (aucune constante), verticale comprise (lignes explicites) | Gérer la croissance vers la gauche (`Anchor`, `RightToLeft`) et le repli automatique (wrap) des contrôles fixes |
| 🟡 | **Fusion en mode .resx** | Non (Excel uniquement) | Étendre `GetMergeSourceDifferences` / `Merge` à une seconde arborescence .resx |
| 🟡 | **Fusion : résolution en masse** | 1 dialog par ligne divergente | « Tout appliquer » / « Tout ignorer » / « Appliquer aux similaires » |

## 4. Robustesse & sécurité

| Prio | Axe | État | Amélioration suggérée |
|:--:|---|---|---|
| 🟢 | **Retry API** | ✅ Polly (408/429/5xx + exceptions transitoires) | Ajuster selon limites réelles |
| 🟡 | **Timeout explicite** | Aucun | Timeout configurable sur les appels IA |
| 🟢 | **Validation prompts** | Aucune | Vérifier la présence de `{language}` |
| 🟡 | **Fichier Excel verrouillé** | Exception brute | Détecter + proposer copie temporaire |
| 🟢 | **Backup auto** | Aucun — mais l'écriture est atomique (`AtomicFile`) : plus de corruption possible en pleine écriture | `.bak` avant chaque sauvegarde — `File.Replace` accepte un nom de backup, point d'insertion tout trouvé |
| 🟡 | **Traduction partielle** | ✅ Entrée IA inexploitable → l'ancienne valeur est restaurée + message explicite | — |

## 5. Fonctionnalités à ajouter

| Prio | Fonctionnalité | Description |
|:--:|---|---|
| 🟡 | **Traduction auto au chargement** | Option pour traduire automatiquement les cellules vides |
| 🟢 | **Comparaison avant/après** | Vue diff depuis le chargement |
| 🟢 | **Statistiques** | ✅ Tableau de bord : par langue, par projet, par fichier, mise en page, chiffres cliquables |
| 🟢 | **Export rapport** | HTML/PDF des vérifications |
| 🟡 | **Mode batch CLI** | Version console pour CI/CD |
| 🟢 | **Multi-fichiers** | Onglets pour plusieurs fichiers simultanés |
| 🟢 | **Historique des appels IA** | Logger requêtes/réponses pour debug |

## 6. DevOps & qualité

| Prio | Axe | État | Amélioration suggérée |
|:--:|---|---|---|
| 🔴 | **CI/CD** | ✅ GitHub Actions (build + tests sur windows-latest, push master + PR) | Étape release à ajouter le moment venu |
| 🟢 | **Version affichée** | Non | Titre ou About (`AssemblyVersion`) |
| 🟡 | **Logging** | `Debug.WriteLine` | Serilog/NLog + fichier rotatif |
| 🟢 | **Analyseurs** | Aucun | `.editorconfig` + StyleCop |
| 🟢 | **Documentation** | README à jour | Screenshots, GIF de démo |

## 7. Quick wins

| État | Amélioration |
|---|---|
| Partiel | **Bouton « Copier »** sur les cellules (français fait, traduction à ajouter) |
| À faire | **Tooltip** sur cellules tronquées |
| À faire | **Compteur de caractères** traduction vs source |
| À faire | **Indicateur « non sauvegardé »** dans le titre |
| À faire | **Raccourci F2** : éditer la cellule sélectionnée |
| À faire | **Son/notification** fin de batch (app en arrière-plan) |

---

## 8. Glossaire transversal (process GLOSSAIRE.md)

| Prio | Chantier | État |
|:--:|---|---|
| 🔴 | 1. Schéma v2 + migration + projection par langue | ✅ Fait |
| 🔴 | 2. Éditeur multi-langues (grille terme × langue, statuts) | ✅ Fait |
| 🟡 | 3. Export / import Excel avec diff et backup | ✅ Fait |
| 🔴 | 4. Retraduction ciblée des lignes impactées | ✅ Fait |

---

# Historique

> Les renvois « §N » de ce tableau visent les sections de [CLAUDE.md](CLAUDE.md), où ce suivi
> vivait à l'origine ; les anciens « §12 » et « §13 » désignent les deux parties de ce
> document-ci (historique et feuille de route).

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
| 2026-08-29 | **Vérification de mise en page** : `FormGeometryReader` (géométrie + filiation `>>X.Parent`), `LayoutAnalyzer` (troncatures et collisions, régression par rapport au français), `GdiTextWidthMeasurer` (mesure GDI à 96 ppp), colonne et filtre dans la grille. Réservée à la source `.resx` |
| 2026-08-29 | Fix `ConfigForm` : placeholders `{language}` / `{glossary}` rendus dans l'aperçu (extension Markdig *generic attributes* retirée), fenêtre bornée à l'écran, champs de fournisseur réétirés à leur panneau |
| 2026-08-30 | Mise en page : mesure **en rapport** plutôt qu'en absolu — la `Size` sérialisée des contrôles `AutoSize` sert d'étalon par formulaire, avec repli sur la médiane de la solution. Supprime `extraPadding` et l'hypothèse d'un `.resx` à 96 ppp |
| 2026-08-30 | **Tableau de bord de l'état des traductions** : `TranslationStatistics` (calcul pur) + `DashboardForm` (cartes, par langue / projet / fichier / mise en page, barres d'avancement). Chiffres cliquables : un clic bascule la langue et filtre la grille. Pseudo-filtres `translation:none` / `done` / `same` et tranches de score fermées |
| 2026-08-30 | Vérification de mise en page **multi-langues en une passe** : verdicts stockés par code de langue dans `TranslationRow`, analyse lancée au chargement et au rafraîchissement pour les sept langues à la fois (×1,8 au lieu de ×4,1), plus aucun recalcul au changement de langue. Onglet « Mise en page » du tableau de bord comparant les sept langues. Compte rendu distinguant « aucun défaut » de « rien n'a pu être analysé » |
| 2026-08-31 | Diagnostic chiffré du premier chargement plus lent : compilation étagée de .NET. Trois contre-mesures mises en œuvre et mesurées (ReadyToRun 6 %, préchauffage 8 %, `TieredCompilation=0` −28 % / +47 %), aucune retenue (§5.1 et §5.2) |
| 2026-08-31 | Documentation : arborescence du §3 remise à niveau (`Models/LayoutStatus.cs`, dossier `FormTest/`, taille réelle de `MainForm.cs`) ; `FormTest` documenté comme banc d'essai manuel de la vérification de mise en page (§11) |
| 2026-08-31 | Gel de l'UI pendant les batchs IA : `TranslateRowsAsync` / `VerifyRowsAsync` / `ExtractTermsRowsAsync` gèlent toolbar + grille (au lieu de Ouvrir/Sauver seuls) — changer de langue, fusionner ou rafraîchir pendant un batch corrompait les traductions (placeholders committés, fusionnés ou orphelins). `CanRefreshView` vérifie `toolStrip.Enabled` : F5 contournait tous les gels via `ProcessCmdKey` |
| 2026-08-31 | Trois écarts corrigés suite à l'analyse détaillée : la sauvegarde `.resx` lit désormais le XML par noms locaux comme le chargement (les éléments créés reprennent l'espace de noms du parent) ; l'aperçu Markdown des prompts émet un `<meta charset>` valide (backslashes parasites retirés) ; le fingerprint du glossaire est calculé sur les entrées triées, comme documenté au §8.4 |
| 2026-08-31 | Gel de l'UI étendu au chargement et au rafraîchissement (revue Copilot PR #30) : `LoadFileAsync` et `BtnRefresh_Click` gèlent toolbar + grille — F5 pouvait ré-entrer dans `BtnRefresh_Click` (deux chargements concurrents) et les drapeaux restaient cliquables pendant un rechargement (vues actives mélangées entre anciennes et nouvelles lignes) |
| 2026-08-31 | Mise en page : dimension verticale par retours à la ligne explicites — la hauteur d'un `AutoSize` suit le rapport des nombres de lignes (collisions verticales détectées), un contrôle fixe est confronté à la hauteur d'étalon de sa police (médiane des `AutoSize`, police par police, `Troncature (hauteur)` dans la grille). Le wrap reste hors périmètre (roadmap §13.3) |
| 2026-08-31 | **Arborescence de la solution** (style ResX Resource Manager) : panneau gauche à cases à cocher Projet → Fichier restreignant l'affichage de la grille. `SplitContainer` créé par code (grille réparentée), propagation projet ↔ fichiers, conservation des cases au rafraîchissement, recochage par le drill-down, gel via `dataGridView.EnabledChanged`, largeur et visibilité persistées (`ShowSolutionTree` / `SolutionTreeWidth`) |
| 2026-08-31 | Bandeau de l'arborescence : case maîtresse « tout cocher / décocher » à trois états et filtre du panneau, agissant sur les éléments visibles. États cochés portés par `_uncheckedFiles` et non par les nœuds — filtrer l'arbre ne perd aucun décochage. Gel étendu à `Panel1` entier |
| 2026-08-31 | Case maîtresse asymétrique : cocher fait de la sélection exactement les éléments visibles (le masqué est décoché, même s'il l'était), décocher vide tout, visible ou non — filtrer puis cocher devient le geste « ne garder que ça » |
| 2026-08-31 | Colonnes Projet et Fichier supprimées, l'arborescence porte cette information ; Clé toujours visible, bouton bascule « détails » retiré. Persistance simplifiée : `ColumnFillWeights` unique (compat lecture de l'ancien jeu), `ShowDetails` retiré. Le drill-down par projet / fichier passe par la sélection exacte dans l'arbre (`SelectTreeExactly`) |
| 2026-09-01 | Synchronisation de sélection grille ↔ arbre : la ligne courante de la grille sélectionne et fait défiler son fichier dans l'arbre ; un nœud de l'arbre sélectionne les lignes affichées du fichier ou du projet et défile jusqu'à la première. Verrou `_isSyncingSelection` contre les boucles, nœuds retrouvés par `_fileNodesByKey` |
| 2026-09-01 | Fix échec d'ouverture : `LoadFileAsync` revient à l'état de la source précédente quand le chargement échoue — `_currentFilePath` n'est plus affecté par `BtnOpen_Click` mais dans le chemin de succès, la status bar (fichier + lignes) est restaurée dans le `catch`, `btnSave` rétabli dans le `finally` (`_allRows is not null`, comme le rafraîchissement). Auparavant : anciennes lignes affichées mais non sauvegardables, nom du fichier en échec dans la status bar, F5 rechargeant l'ancienne source sous le mauvais libellé |
| 2026-09-01 | **Écritures disque atomiques** : `AtomicFile` (temporaire `<cible>.tmp` du même répertoire puis `File.Replace` / `File.Move`) utilisé par `ResxReader.Save`, `AppConfig.Save` et `GlossaryService.Save` — un crash ou une coupure en pleine écriture ne corrompt plus le fichier cible. BOM préservé, écriture seulement si modifié et remontée des échecs fichier par fichier inchangés ; pas de `.bak` systématique (« Backup auto » reste en roadmap §13.4) |
| 2026-09-01 | Traduction partielle (roadmap §13.4) : une entrée inexploitable dans une réponse IA restaure la valeur d'avant le batch au lieu de laisser le placeholder « Traduction en cours... » / « Vérification... » — qui pouvait être sauvegardé tel quel dans le .resx ou l'Excel. Messages d'avertissement explicités (les lignes ont retrouvé leur valeur précédente) |
| 2026-09-01 | Documentation scindée : la feuille de route et l'historique quittent CLAUDE.md pour ROADMAP.md (ce fichier). CLAUDE.md recentré sur le guide technique (§1 à §11), le seul contenu chargé automatiquement par les sessions assistées ; renvois croisés mis à jour (README, `AtomicFile.cs`) |
| 2026-09-02 | Température auto-adaptative : les modèles récents (Claude Sonnet 5+) refusent le paramètre `temperature` (validation Bedrock) — `Translator` mémorise le refus par fournisseur-modèle au premier appel et rejoue sans, sur les deux dialectes. Les modèles qui l'acceptent continuent de recevoir 0.1 |
| 2026-09-02 | **Glossaire transversal (chantier 1 de GLOSSAIRE.md)** : `GlossaryTerm` (un terme FR, ses traductions par langue, statut Proposé / En contrôle / Validé, commentaire réviseur), migration v1 idempotente au chargement (termes migrés Validé, empreinte projetée inchangée, caches préservés), surface par langue conservée par projection — éditeur et extraction inchangés. Seuls les termes Validé sont injectés. Process complet consigné dans GLOSSAIRE.md |
| 2026-09-04 | Glossaire : éditeur multi-langues (chantier 2) — `GlossaryForm` en grille transversale terme × langue (colonnes dynamiques depuis `MainForm.Languages`), statut éditable, commentaire réviseur en lecture seule, doublons refusés. L'extraction verse désormais des termes Proposé (`AddProposedTerms`), sans écraser une case déjà tranchée ; nouvelles API `GetTerms` / `ReplaceTerms` |
| 2026-09-04 | Glossaire : export / import Excel (chantier 3) — export daté portant l'empreinte du glossaire (feuille Infos), colonnes de langue retrouvées par leur code à l'import (réordonnancement toléré), diff par terme et par champ (`GlossaryDiff`, logique pure) avec acceptation individuelle (`GlossaryImportDiffForm`, suppressions décochées par défaut), backup daté avant application. Les Proposé exportés passent En contrôle, les Validé restent injectés, un terme En contrôle revenu inchangé repasse Validé |
| 2026-09-04 | Correctif : extraction de termes métier muette — sur le dialecte Anthropic, le plafond de sortie de 2048 tokens tronquait la réponse JSON dès un lot de 20 textes (reproduit : 6469 caractères, tableau jamais fermé) ; le parseur avalait l'exception et l'UI affichait « Aucun nouveau terme métier n'a été proposé ». `AnthropicMaxTokens` → 8192, lots d'extraction → 10 textes, `ExtractCandidatesAsync` retourne un bilan (`GlossaryExtractionResult` : échecs d'appel, réponses illisibles, troncature, déjà connus) et `MainForm` distingue erreur / extraction partielle / vraie absence de termes. Parseur rendu `internal` et testé (6 tests) |
| 2026-09-04 | Correctif : boutons Enregistrer / Annuler hors champ dans `GlossaryForm`, `GlossaryImportDiffForm` et `GlossaryExtractionDialog` — les panneaux du bas ne recevaient que leur `Height`, les distances d'ancrage droite se figeaient sur la largeur par défaut (200 px) et les boutons sortaient du panneau au dock (mesuré : x=1995 dans un panneau de 1138 px). `Size` complète posée comme le fait le vrai Designer ; `FitToWorkingArea` factorisé en `ScreenFit.Apply` (helper partagé) et appliqué aux trois dialogs en plus de `ConfigForm` |
| 2026-09-04 | Tests + CI — projet `CheckTranslation.Tests` (xUnit, 79 tests) sur la logique pure : `QualityScore`, `TranslationRowFiltering`, `TranslationStatistics` (dont la cohérence tranche comptée / tranche filtrée), `GlossaryDiff` (statuts, promotion sous couverture complète, doublon refusé), `GlossaryImpact`, `GlossaryExcel` (aller-retour + refus des classeurs ambigus), `Translator.ParseNumberedList` (comportements caractérisés), caches de `TranslationService` (fingerprint, purge par modèle), `GlossaryService.NormalizeCell`. `InternalsVisibleTo` + exclusion du sous-dossier du glob de l'exe. Workflow GitHub Actions (build + tests, windows-latest, push master + PR), badge dans le README |
| 2026-09-04 | Glossaire : retraduction ciblée (chantier 4) — la projection prompts (termes Validé, par langue) est photographiée avant/après l'éditeur de glossaire (`GetPromptEntries`) ; `GlossaryImpact` (logique pure) en déduit les termes dont la contrainte a changé (apparus ou modifiés — les supprimés n'impactent pas, décision assumée) et sélectionne les lignes par inclusion insensible à la casse. Après confirmation avec compte par langue, retraduction + re-vérification par batchs enchaînés, écrites dans les dictionnaires par code (jamais la vue active), verdicts de mise en page invalidés, compte rendu final |

---

*Document vivant — chaque changement ajoute sa ligne à l'historique et met la feuille de route à jour.*
