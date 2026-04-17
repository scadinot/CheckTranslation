# Roadmap — Axes d'amélioration

Ce document liste les améliorations identifiées pour le projet `CheckTranslation`.

---

## 🏗️ Architecture & Maintenabilité

| Priorité | Axe | État actuel | Amélioration suggérée |
|:--------:|-----|-------------|----------------------|
| 🔴 | **Tests** | Aucun test unitaire | Ajouter un projet `CheckTranslation.Tests` (xUnit/NUnit). Candidats prioritaires : `QualityScore`, `TranslationRowFiltering`, `TranslationService` (cache/dédup), `ExcelReader.Merge`, `AppConfig` (round-trip DPAPI), `Translator.ParseNumberedList` |
| 🟢 | **Injection de dépendances** | Traité : `ITranslationService`/`IExcelService` + conteneur DI (`Microsoft.Extensions.DependencyInjection`) + injection dans `MainForm` et `ConfigForm` (via factory `Func<ConfigForm>`) | Étendre progressivement l'injection aux autres écrans/services si besoin |
| 🟡 | **Séparation UI/Logic** | Partiellement traité : extraction de logique (filtrage + score qualité) hors de `MainForm.cs` dans `Logic/`, `MainForm` découpé en `partial class` (layout, refresh, score filter) | Continuer : extraire un `MainFormViewModel`/présenteur pour chargement/sauvegarde/traduction/vérification/fusion |
| 🟢 | **Configuration** | Prompts en dur dans `AppConfig.cs` | Externaliser les prompts dans des fichiers `.md` séparés (plus facile à éditer) |
| 🟢 | **Hygiène WinForms Designer** | Corrigé : suppression d'un champ non généré dans `ConfigForm.Designer.cs` ; fichiers partials ajoutés (`MainForm.LayoutPersistence`, `ManualRefreshSupport`, `VerificationScoreFilterSupport`) avec exclusion explicite des `.resx` dans le `.csproj` | Maintenir la règle : ne pas modifier les fichiers `.Designer.cs` à la main, laisser Visual Studio régénérer |

---

## ⚡ Performance

| Priorité | Axe | État actuel | Amélioration suggérée |
|:--------:|-----|-------------|----------------------|
| 🟢 | **Appels API parallèles** | Traité : les batches de traduction/vérification sont parallélisés avec `Task.WhenAll`, avec limitation de concurrence (`SemaphoreSlim`, `FixedParallelBatchRequests = 4`) pour limiter le rate limiting | Ajuster plus tard finement le niveau de parallélisme selon les providers et quotas réels |
| 🟢 | **Chargement Excel** | Traité : progress bar basée sur les lignes lues (`ExcelLoadProgress(Done, Total)`) pendant `LoadWithRowProgress`, updates UI limitées à 1 sur 10 pour éviter de saturer le thread UI | Optionnel : optimiser encore (lecture streaming / `VirtualMode`) pour très gros fichiers |
| 🟢 | **Cache des traductions** | Traité : cache mémoire conservé pendant la session, alimenté par les traductions IA, avec déduplication par texte + langue + configuration, et mise à jour si une traduction est modifiée manuellement ; un cache distinct existe aussi pour les vérifications. Vidage manuel via `ConfigForm` | Envisager plus tard une persistance disque du cache entre sessions si nécessaire |
| 🟢 | **DataGridView** | Double-buffering activé | Utiliser la virtualisation (`VirtualMode`) pour les très gros fichiers (>50k lignes) |

---

## 🎨 UX / Interface

| Priorité | Axe | État actuel | Amélioration suggérée |
|:--------:|-----|-------------|----------------------|
| 🔴 | **Annulation** | Impossible d'annuler une traduction/vérification en cours | Ajouter un `CancellationToken` + bouton "Annuler" |
| 🟡 | **Historique** | Pas d'historique des modifications | Implémenter Undo/Redo (au moins 1 niveau) |
| 🟢 | **Recherche globale** | Filtres par colonne uniquement (texte + score) | Ajouter une recherche globale (toutes colonnes) |
| 🟢 | **Export** | Sauvegarde dans le fichier source uniquement | Permettre "Enregistrer sous" / Export vers un nouveau fichier |
| 🟢 | **Thème sombre** | Non supporté | Détecter le thème Windows et adapter les couleurs |
| 🟡 | **Raccourcis clavier** | Partiellement traité : F5 = Rafraîchir | Ajouter Ctrl+S (save), Ctrl+O (open), Ctrl+T (traduire sélection), Ctrl+V (vérifier sélection), Ctrl+M (fusion) |
| 🟢 | **Chargement fichier : cohérence filtres** | Traité : les champs de filtre sont réinitialisés après `Ouvrir` pour repartir sur une vue non filtrée | Conserver ce comportement ou proposer plus tard une option utilisateur pour restaurer les filtres |
| 🟡 | **Fusion : résolution en masse** | Un dialog par ligne divergente | Ajouter des raccourcis "Tout appliquer" / "Tout ignorer" / "Appliquer à toutes les lignes similaires" |
| 🟢 | **Persistance disposition fenêtre** | Traité : taille de la fenêtre + largeur des colonnes persistées, avec dictionnaires distincts selon le mode détails on/off | Conserver ce comportement, éventuellement ajouter position (X/Y) |
| 🟢 | **Filtre par score de vérification** | Traité : ComboBox dans l'en-tête `Commentaire` avec options "Non vérifiés / ≤ 50…100 / ≥ 90" | Autoriser des seuils arbitraires saisis |
| 🟢 | **Indicateur "Rafraîchir en attente"** | Traité : le bouton affiche `Rafraîchir *` après une édition / traduction / vérification pour signaler qu'un ré-application filtres/tri est utile | — |
| 🟢 | **Copier le texte français** | Traité : menu contextuel sur `colFrench` (mono ou multi-sélection) | — |
| 🟢 | **Auto-traduire (copie existante)** | Traité : menu contextuel qui recopie les traductions déjà présentes pour un même texte français (sans appel IA) | — |

---

## 🔒 Robustesse & Sécurité

| Priorité | Axe | État actuel | Amélioration suggérée |
|:--------:|-----|-------------|----------------------|
| 🟢 | **Retry API** | Traité : retry automatique avec backoff exponentiel + jitter sur erreurs transitoires (`Polly`, 3 tentatives) sur HTTP 408/429/5xx, `HttpRequestException`, `TimeoutException`, `TaskCanceledException`, `ClientResultException` | Ajuster finement les statuts/temporisations selon les limites réelles des providers |
| 🟡 | **Timeout** | Pas de timeout explicite | Ajouter un timeout configurable sur les appels API |
| 🟢 | **Validation prompts** | Aucune | Vérifier que `{language}` est présent dans les prompts |
| 🟡 | **Fichier verrouillé** | Exception si Excel est ouvert | Détecter le verrouillage et proposer une copie temporaire |
| 🟢 | **Backup automatique** | Aucun | Créer un `.bak` avant chaque sauvegarde |
| 🟢 | **Emplacement configuration** | Traité : le fichier `CheckTranslation.config.json` est désormais enregistré dans `%LocalAppData%\CheckTranslation`, avec lecture de l'ancien emplacement pour compatibilité | Conserver cette stratégie ou prévoir plus tard une migration/suppression automatique de l'ancien fichier |
| 🟡 | **Traduction partielle** | Si l'IA renvoie une entrée vide, la cellule peut rester sur un placeholder | En cas d'entrée vide/non parsée : restaurer l'ancienne valeur et remonter l'erreur de façon plus explicite |
| 🟢 | **Persistance vérification** | Traité : les résultats de vérification (score/commentaire) sont aussi sauvegardés dans les colonnes commentaires associées | Conserver ce comportement et éventuellement ajouter plus tard une validation explicite des colonnes de destination |
| 🟢 | **Détection des changements du français source** | Traité : le bouton/rafraîchissement F5 recharge le fichier, détecte les lignes où le français ou le commentaire source a changé, et demande confirmation avant d'écraser les valeurs en mémoire | — |

---

## 🚀 Fonctionnalités à ajouter

| Priorité | Fonctionnalité | Description |
|:--------:|----------------|-------------|
| 🟢 | **Fusion Excel** | Traité : report des traductions d'un fichier source vers un fichier destination via la clé `Project\|File\|Key`, avec détection et résolution des conflits ligne-par-ligne (`MergeDifferenceForm`) |
| 🟡 | **Traduction automatique au chargement** | Option pour traduire automatiquement les cellules vides |
| 🟢 | **Comparaison avant/après** | Vue diff pour voir les modifications depuis le chargement |
| 🟢 | **Statistiques** | Dashboard : % traduit, % vérifié, scores moyens par langue |
| 🟢 | **Export rapport** | Générer un rapport HTML/PDF des vérifications (scores, commentaires) |
| 🟡 | **Mode batch CLI** | Version console pour intégration CI/CD |
| 🟢 | **Multi-fichiers** | Ouvrir plusieurs fichiers Excel en onglets |
| 🟡 | **Glossaire** | Dictionnaire de termes techniques à respecter (injecté dans les prompts) |
| 🟢 | **Historique des appels IA** | Logger les requêtes/réponses pour debug |

---

## 📦 DevOps & Qualité

| Priorité | Axe | État actuel | Amélioration suggérée |
|:--------:|-----|-------------|----------------------|
| 🔴 | **CI/CD** | Aucun pipeline | GitHub Actions : build + tests + release |
| 🟢 | **Versioning** | Pas de version affichée | Afficher la version dans le titre ou About (via `AssemblyVersion`) |
| 🟡 | **Logging** | `Debug.WriteLine` uniquement | Ajouter un vrai logger (Serilog/NLog) avec fichier rotatif |
| 🟢 | **Analyseurs** | Aucun | Activer les analyseurs Roslyn (.editorconfig + StyleCop) |
| 🟢 | **Documentation** | README complet (fonctionnalités + prérequis + config) | Ajouter des screenshots, GIF de démo |

---

## 🔧 Quick Wins (faciles à implémenter)

| Priorité | Amélioration | État |
|:--------:|--------------|------|
| 🟢 | **Bouton "Copier"** sur les cellules — Copier une traduction dans le presse-papier | Partiellement : copier le français est fait (menu contextuel) ; copier la traduction reste à ajouter |
| 🟢 | **Tooltip sur les cellules tronquées** — Afficher le texte complet au survol | À faire |
| 🟢 | **Compteur de caractères** — Afficher la longueur de la traduction vs. source | À faire |
| 🟢 | **Indicateur "non sauvegardé"** — Astérisque dans le titre si modifications non sauvées | À faire |
| 🟢 | **Raccourci F2** — Éditer directement la cellule sélectionnée | À faire |
| 🟢 | **Son/notification** — Notification quand un batch est terminé (si fenêtre en arrière-plan) | À faire |

---

## Légende des priorités

| Symbole | Signification |
|:-------:|---------------|
| 🔴 | Haute priorité — Impact fort sur la qualité/robustesse |
| 🟡 | Moyenne priorité — Amélioration significative |
| 🟢 | Basse priorité — Nice to have |

---

## Suivi

| Date | Version | Changements |
|------|---------|-------------|
| 2026-01-XX | 1.0 | Version initiale |
| 2026-03-06 | 1.0 | Hygiène WinForms Designer : nettoyage de `ConfigForm.Designer.cs` (suppression de code non généré) |
| 2026-03-06 | 1.0 | Injection de dépendances : ajout de `IExcelService`/`ITranslationService` et injection dans `MainForm` |
| 2026-03-06 | 1.0 | Séparation UI/Logic : extraction du filtrage (`Logic/TranslationRowFiltering.cs`) et du calcul de score qualité (`Logic/QualityScore.cs`) hors de `MainForm.cs` |
| 2026-03-07 | 1.0 | Chargement Excel : progress bar en lignes lues (API `ExcelLoadProgress` + `LoadWithRowProgress`) |
| 2026-03-07 | 1.0 | Auto-traduction (sans IA) : copie des traductions déjà présentes sur d'autres lignes (menu contextuel) |
| 2026-03-13 | 1.0 | Chargement fichier : réinitialisation des champs de filtre après `Ouvrir` pour garder une vue cohérente |
| 2026-03-13 | 1.0 | Cache des traductions IA : ajout d'un cache mémoire par texte/langue/configuration, conservé pendant la session, avec déduplication des doublons et mise à jour lors d'une modification manuelle |
| 2026-03-13 | 1.0 | Emplacement configuration : déplacement de `CheckTranslation.config.json` vers `%LocalAppData%\CheckTranslation` avec compatibilité de lecture de l'ancien emplacement |
| 2026-03-13 | 1.0 | Cache des vérifications IA : ajout d'un cache mémoire distinct par source/traduction/langue/configuration, avec compteur dédié dans la barre d'état |
| 2026-03-13 | 1.0 | Persistance vérification : sauvegarde des commentaires de vérification dans les colonnes Excel associées |
| 2026-03-13 | 1.0 | Appels API parallèles : traitement des batchs avec `Task.WhenAll` et limitation de concurrence pour limiter le rate limiting |
| 2026-03-20 | 1.0 | Boutons "Vider cache" : ajout dans `ConfigForm` pour purger manuellement les caches traduction/vérification |
| 2026-03-27 | 1.0 | Filtre par score de vérification : ComboBox dans l'en-tête de la colonne Commentaire (`VerificationScoreFilterSupport`) |
| 2026-04-03 | 1.0 | Bouton Rafraîchir + raccourci F5 : rechargement du fichier avec détection des changements de français/commentaire source et confirmation utilisateur (`ManualRefreshSupport`) |
| 2026-04-10 | 1.0 | Persistance de disposition : taille de fenêtre et largeur des colonnes persistées dans `AppConfig` avec dictionnaires distincts selon le mode détails on/off (`MainForm.LayoutPersistence`) |
| 2026-04-15 | 1.0 | Fusion Excel : report des traductions d'un fichier source vers un fichier destination avec détection et résolution des conflits (`MergeDifferenceForm`, `ExcelReader.Merge`, modèles `MergeDifference*`) |
| 2026-04-17 | 1.0 | Refactorisation de la gestion des différences de fusion |

---

*Document de planification — à mettre à jour au fil des développements.*
