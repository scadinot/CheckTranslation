# Roadmap — Axes d'amélioration

Ce document liste les améliorations identifiées pour le projet `CheckTranslation`.

---

## 🏗️ Architecture & Maintenabilité

| Priorité | Axe | État actuel | Amélioration suggérée |
|:--------:|-----|-------------|----------------------|
| 🔴 | **Tests** | Aucun test unitaire | Ajouter un projet `CheckTranslation.Tests` (xUnit/NUnit) pour `Translator`, `ExcelReader`, `AppConfig` |
| 🟡 | **Injection de dépendances** | Couplage direct aux clients API | Extraire des interfaces (`ITranslationService`, `IExcelService`) pour faciliter les tests et le mock |
| 🟡 | **Séparation UI/Logic** | `MainForm.cs` contient la logique métier | Pattern MVVM/MVP light : extraire un `MainFormViewModel` |
| 🟢 | **Configuration** | Prompts en dur dans `AppConfig.cs` | Externaliser les prompts dans des fichiers `.md` séparés (plus facile à éditer) |
| 🟢 | **Hygiène WinForms Designer** | Corrigé : suppression d’un champ non généré (ex: `testField`) dans `ConfigForm.Designer.cs` | Maintenir la règle : ne pas modifier les fichiers `.Designer.cs` à la main, laisser Visual Studio régénérer |

---

## ⚡ Performance

| Priorité | Axe | État actuel | Amélioration suggérée |
|:--------:|-----|-------------|----------------------|
| 🟡 | **Appels API parallèles** | Les batches sont traités séquentiellement | Paralléliser avec `Task.WhenAll` (attention au rate limiting) |
| 🟢 | **Chargement Excel** | Bloque l'UI même avec `Task.Run` | Afficher une vraie progress bar (lignes lues) et non un pourcentage |
| 🟡 | **Cache des traductions** | Aucun | Cacher les traductions IA déjà effectuées (éviter de retraduire les mêmes textes) |
| 🟢 | **DataGridView** | Double-buffering activé | Utiliser la virtualisation (`VirtualMode`) pour les très gros fichiers (>50k lignes) |

---

## 🎨 UX / Interface

| Priorité | Axe | État actuel | Amélioration suggérée |
|:--------:|-----|-------------|----------------------|
| 🔴 | **Annulation** | Impossible d'annuler une traduction/vérification en cours | Ajouter un `CancellationToken` + bouton "Annuler" |
| 🟡 | **Historique** | Pas d'historique des modifications | Implémenter Undo/Redo (au moins 1 niveau) |
| 🟢 | **Recherche globale** | Filtres par colonne uniquement | Ajouter une recherche globale (toutes colonnes) |
| 🟢 | **Export** | Sauvegarde dans le fichier source uniquement | Permettre "Enregistrer sous" / Export vers un nouveau fichier |
| 🟢 | **Thème sombre** | Non supporté | Détecter le thème Windows et adapter les couleurs |
| 🟢 | **Raccourcis clavier** | Aucun | Ctrl+S (save), Ctrl+O (open), F5 (refresh), Ctrl+T (traduire sélection) |
| 🟡 | **Chargement fichier : cohérence filtres** | Les filtres UI peuvent rester saisis mais ne pas être appliqués après ouverture | Après `Ouvrir`, vider les champs de filtre *ou* réappliquer `ApplyFilters()` pour refléter l’état des filtres |

---

## 🔒 Robustesse & Sécurité

| Priorité | Axe | État actuel | Amélioration suggérée |
|:--------:|-----|-------------|----------------------|
| 🔴 | **Retry API** | Aucun retry automatique | Implémenter un retry avec backoff exponentiel (Polly) |
| 🟡 | **Timeout** | Pas de timeout explicite | Ajouter un timeout configurable sur les appels API |
| 🟢 | **Validation prompts** | Aucune | Vérifier que `{language}` est présent dans les prompts |
| 🟡 | **Fichier verrouillé** | Exception si Excel est ouvert | Détecter le verrouillage et proposer une copie temporaire |
| 🟢 | **Backup automatique** | Aucun | Créer un `.bak` avant chaque sauvegarde |
| 🔴 | **Emplacement configuration** | Le fichier `CheckTranslation.config.json` est écrit dans le dossier de l’application | Déplacer vers `%AppData%` / `%LocalAppData%` pour éviter les problèmes de droits (Program Files) |
| 🟡 | **Traduction partielle** | Si l’IA renvoie une entrée vide, la cellule peut rester sur un placeholder | En cas d’entrée vide/non parsée : restaurer l’ancienne valeur et remonter l’erreur de façon plus explicite |
| 🟡 | **Persistance vérification** | Les résultats de vérification (score/commentaire) ne sont pas forcément sauvegardés dans Excel | Écrire aussi les colonnes commentaires associées lors de la sauvegarde (pas uniquement les traductions) |

---

## 🚀 Fonctionnalités à ajouter

| Priorité | Fonctionnalité | Description |
|:--------:|----------------|-------------|
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
| 🟢 | **Documentation** | README basique | Ajouter des screenshots, GIF de démo |

---

## 🔧 Quick Wins (faciles à implémenter)

| Priorité | Amélioration |
|:--------:|--------------|
| 🟢 | **Bouton "Copier"** sur les cellules — Copier une traduction dans le presse-papier |
| 🟢 | **Tooltip sur les cellules tronquées** — Afficher le texte complet au survol |
| 🟢 | **Compteur de caractères** — Afficher la longueur de la traduction vs. source |
| 🟢 | **Indicateur "non sauvegardé"** — Astérisque dans le titre si modifications non sauvées |
| 🟢 | **Raccourci F2** — Éditer directement la cellule sélectionnée |
| 🟢 | **Son/notification** — Notification quand un batch est terminé (si fenêtre en arrière-plan) |

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
| 2025-01-XX | 1.0 | Version initiale |
| 2026-03-06 | 1.0 | Hygiène WinForms Designer : nettoyage de `ConfigForm.Designer.cs` (suppression de code non généré) |

---

*Document de planification — à mettre à jour au fil des développements.*
