# GLOSSAIRE.md — Process du glossaire transversal

Process de gouvernance du glossaire métier : constitution, contrôle par les équipes externes,
et retraduction ciblée des lignes impactées par une correction.

> Guide technique : voir [CLAUDE.md](CLAUDE.md). Feuille de route : voir [ROADMAP.md](ROADMAP.md).

---

## 1. Le modèle : un glossaire transversal

Une entrée = un terme métier français, avec toutes ses traductions. Le terme est l'unité de
gouvernance ; les langues n'en sont que les colonnes.

| Champ | Contenu |
|---|---|
| Source | Terme français canonique (singulier, non conjugué), identité de l'entrée |
| Contexte | Définition métier courte, commune à toutes les langues |
| en-US … zh-CN | La traduction imposée par langue, vide tant que non tranchée |
| Statut | Proposé / En contrôle / Validé |
| Commentaire réviseur | Rempli par les équipes externes au retour |

Deux règles : **seuls les termes Validé sont injectés** dans les prompts (traduction ET
vérification), et l'injection est une **projection par langue** — un terme sans traduction
allemande n'apparaît pas dans les prompts allemands. L'empreinte de cache est calculée par
langue sur cette projection : corriger une colonne n'invalide que les caches de sa langue.

## 2. Le cycle en cinq phases

1. **Constitution** — l'extraction IA (« Extraire les termes métier… ») verse des termes Proposé,
   l'éditeur consolide. Les termes naissent Proposé.
2. **Export pour contrôle externe** — classeur Excel au format du tableau ci-dessus (une ligne
   par terme), daté et portant l'empreinte du glossaire à l'export. Seuls les termes Proposé
   passent En contrôle : les Validé restent injectés dans les prompts pendant toute la durée du
   contrôle, et repassent Validé au retour s'ils reviennent inchangés.
3. **Retour et import** — lecture du classeur corrigé, différences présentées terme par terme
   (patron du dialog de fusion), backup daté de l'ancien glossaire avant remplacement, compte
   rendu des changements. Les termes acceptés passent Validé. Un terme En contrôle revenu
   strictement inchangé repasse Validé **seulement si le classeur couvre toutes les langues** :
   un classeur restreint à une équipe de langue ne dit rien des autres colonnes, son silence ne
   clôt pas le contrôle. Un classeur complet revenu sans aucune correction clôt, lui, le contrôle
   de tous ses termes.
4. **Retraduction ciblée** — pour chaque changement (terme, langue), les lignes dont le français
   contient le terme source sont sélectionnées et retraduites, puis re-vérifiées, langue par
   langue. L'empreinte garantit que le cache ne ressert pas les anciennes traductions.
   Limite assumée : la détection est une inclusion insensible à la casse — les formes fléchies
   éloignées du terme canonique peuvent lui échapper, et l'approximation retraduira parfois une
   ligne de trop plutôt qu'une de moins.
5. **Boucle** — le glossaire vit ; chaque campagne rejoue les phases 2 à 4. Le tableau de bord
   mesure l'effet (scores avant / après par langue).

## 3. État d'avancement des chantiers

| Chantier | Contenu | État |
|---|---|---|
| 1. Schéma v2 + migration | `GlossaryTerm` transversal, statuts, projection par langue, empreinte compatible | ✅ Fait |
| 2. Éditeur multi-langues | `GlossaryForm` en grille terme × langue, colonne statut | ✅ Fait |
| 3. Export / import Excel | ClosedXML, diff à l'import, backup daté | ✅ Fait |
| 4. Retraduction ciblée | Détection des lignes impactées, batchs enchaînés par langue, compte rendu | À faire |

Décisions ouvertes avant le chantier 4 : sort des lignes dont un terme a été supprimé du
glossaire (retraduire ou laisser), et politique de correspondance (inclusion simple ou détection
des formes fléchies).

---

*Document vivant — chaque chantier met ce process et son état à jour.*
