# Roadmap — Axes d'amélioration

Ce document liste les améliorations identifiées pour le projet `CheckTranslation`.

---

## 🏗️ Architecture & Maintenabilité

| Priorité | Axe | État actuel | Amélioration suggérée |
|:--------:|-----|-------------|----------------------|
| 🔴 | **Tests** | Aucun test unitaire | Ajouter un projet `CheckTranslation.Tests` (xUnit/NUnit) pour `Translator`, `ExcelReader`, `AppConfig` |
| 🟢 | **Injection de dépendances** | Traité : `ITranslationService`/`IExcelService` + conteneur DI (`Microsoft.Extensions.DependencyInjection`) + injection dans `MainForm` | Étendre progressivement l’injection aux autres écrans/services si besoin |
| 🟡 | **Séparation UI/Logic** | Partiellement traité : extraction de logique (filtrage + score qualité) hors de `MainForm.cs` | Continuer : extraire un `MainFormViewModel`/présenteur pour chargement/sauvegarde/traduction/vérification |
| 🟢 | **Configuration** | Prompts en dur dans `AppConfig.cs` | Externaliser les prompts dans des fichiers `.md` séparés (plus facile à éditer) |
| 🟢 | **Hygiène WinForms Designer** | Corrigé : suppression d’un champ non généré (ex: `testField`) dans `ConfigForm.Designer.cs` | Maintenir la règle : ne pas modifier les fichiers `.Designer.cs` à la main, laisser Visual Studio régénérer |

---

## ⚡ Performance

| Priorité | Axe | État actuel | Amélioration suggérée |
|:--------:|-----|-------------|----------------------|
| 🟡 | **Appels API parallèles** | Les batches sont traités séquentiellement | Paralléliser avec `Task.WhenAll` (attention au rate limiting) |
| 🟢 | **Chargement Excel** | Traité : progress bar basée sur les lignes lues (`Done/Total`) pendant `LoadFileAsync` | Optionnel : optimiser encore (lecture streaming / `VirtualMode`) pour très gros fichiers |
| 🟢 | **Cache des traductions** | Traité : cache mémoire conservé pendant la session, alimenté par les traductions IA, avec déduplication par texte + langue + configuration, et mise à jour si une traduction est modifiée manuellement | Envisager plus tard une persistance disque du cache entre sessions si nécessaire |
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
| 🟢 | **Chargement fichier : cohérence filtres** | Traité : les champs de filtre sont réinitialisés après `Ouvrir` pour repartir sur une vue non filtrée | Conserver ce comportement ou proposer plus tard une option utilisateur pour restaurer les filtres |

---

## 🔒 Robustesse & Sécurité

| Priorité | Axe | État actuel | Amélioration suggérée |
|:--------:|-----|-------------|----------------------|
| 🟢 | **Retry API** | Traité : retry automatique avec backoff exponentiel sur erreurs transitoires (`Polly`) | Ajuster finement les statuts/temporisations selon les limites réelles des providers |
| 🟡 | **Timeout** | Pas de timeout explicite | Ajouter un timeout configurable sur les appels API |
| 🟢 | **Validation prompts** | Aucune | Vérifier que `{language}` est présent dans les prompts |
| 🟡 | **Fichier verrouillé** | Exception si Excel est ouvert | Détecter le verrouillage et proposer une copie temporaire |
| 🟢 | **Backup automatique** | Aucun | Créer un `.bak` avant chaque sauvegarde |
| 🟢 | **Emplacement configuration** | Traité : le fichier `CheckTranslation.config.json` est désormais enregistré dans `%LocalAppData%\CheckTranslation`, avec lecture de l’ancien emplacement pour compatibilité | Conserver cette stratégie ou prévoir plus tard une migration/suppression automatique de l’ancien fichier |
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
| 2026-01-XX | 1.0 | Version initiale |
| 2026-03-06 | 1.0 | Hygiène WinForms Designer : nettoyage de `ConfigForm.Designer.cs` (suppression de code non généré) |
| 2026-03-06 | 1.0 | Injection de dépendances : ajout de `IExcelService`/`ITranslationService` et injection dans `MainForm` |
| 2026-03-06 | 1.0 | Séparation UI/Logic : extraction du filtrage et du calcul de score qualité hors de `MainForm.cs` |
| 2026-03-07 | 1.0 | Chargement Excel : progress bar en lignes lues (API `ExcelLoadProgress` + `LoadWithRowProgress`) |
| 2026-03-07 | 1.0 | Auto-traduction (sans IA) : copie des traductions déjà présentes sur d’autres lignes (menu contextuel) |
| 2026-03-13 | 1.0 | Chargement fichier : réinitialisation des champs de filtre après `Ouvrir` pour garder une vue cohérente |
| 2026-03-13 | 1.0 | Cache des traductions IA : ajout d’un cache mémoire par texte/langue/configuration, conservé pendant la session, avec déduplication des doublons et mise à jour lors d’une modification manuelle |
| 2026-03-13 | 1.0 | Emplacement configuration : déplacement de `CheckTranslation.config.json` vers `%LocalAppData%\CheckTranslation` avec compatibilité de lecture de l’ancien emplacement |

---

*Document de planification — à mettre à jour au fil des développements.*
