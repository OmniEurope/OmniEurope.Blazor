<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-003 - Ajouts OE demandés par la migration d'Aetheus

> Source et détail versionnés côté Aetheus :
> - `docs/plans/PLAN-005-migration-radzen-oe.md`, lots 2 à 9 ;
> - `docs/plans/PLAN-005-verification-oe.md`, qui fait foi.
>
> Aide-mémoire du dépôt, déplacé de `plans/` (ignoré par Git) vers `docs/plans/` et renuméroté le 2026-09-20 (PLAN-001, lot 2).
> Réécrit le 2026-09-14, en remplacement de la version du 2026-09-13, qui listait des évolutions
> abandonnées depuis.
> Statut : ouvert. Aucun lot démarré. Travail en commits courts sur `develop`, sans jamais toucher
> les fichiers non commités d'une autre session.

## Règles de l'utilisateur

- **OE est la base**, utilisée par d'autres projets. Aetheus s'adapte à OE.
- Une évolution d'un composant existant n'est retenue que si elle est nécessaire **et** purement
  additive : option facultative, valeur ajoutée en fin d'énumération ou nouvelle méthode. Le
  comportement et l'aspect par défaut ne changent pas.
- Un nouveau composant n'est retenu que s'il ne doublonne rien d'existant.
- OE reste une bibliothèque de front : aucune implémentation non visuelle (HTTP, auth, SignalR,
  cache, horloge, RBAC).
- Icônes : Phosphor uniquement, jamais de glyphe Material.

## Ajouts retenus

- Lot 2 : `WasmSmoke` étendu à une grille, un dialogue et un toast, publié trimmé sous CSP stricte.
  Vérifier aussi que la virtualisation d'`OmniDataList` ne pose pas d'attribut `style`. Corriger les
  dérives de doc (popup de filtre, onglets).
- Lot 3 : 76 glyphes Phosphor ajoutés en fin d'`OmniIconName`. Couche `omni-u-*`, en classes
  nouvelles uniquement.
- Lot 4 : pont DataAnnotations vers des messages localisés, et `OmniAlert.Dismissible` (faux par
  défaut).
- Lot 5 : `OmniPopover`, panneau ancré non modal.
- Lot 6 : `OmniDataGrid.EmptyTemplate`, et la virtualisation avec lignes de groupe et de détail en
  mode `Items`. Fait : `4d24139`, fusionné dans `develop` (`886b057`).
- Lot 7 : `OmniPageHeader` et son service de fil d'Ariane, `OmniDetailShell`, `OmniLoginShell`,
  `OmniConnectionOverlay`, `OmniEmptyState`, `OmniWizard`, `OmniDescriptionList`, `OmniRelativeTime`.
- Lot 8 : `OmniResourceList`, `OmniEntityPicker`, `OmniDynamicForm`, `OmniStatusBadge` et sa table,
  `OmniLogViewer`, `OmniCodeBlock`, `OmniCodeViewer`, `OmniUnifiedDiff`.
- Lot 9 :
  - `OmniStatusStrip`, `OmniKanban`, `omni-boot.js` et l'écran de démarrage ;
  - l'extension d'`OmniMindMap` en graphe, avec une disposition en couches en C# et sans dagre ;
  - `OmniGitGraph`, `OmniGantt` ;
  - `OmniCodeEditor` et `OmniDiffViewer` (Monaco en iframe).

## Abandonné (OE couvre déjà ou design OE accepté)

- Toutes les autres évolutions : boutons, badges, typographie, espacements, listes déroulantes,
  champs, dialogues, notifications, infobulles, onglets, menus, barre latérale, palette sombre, CSS
  qui restylerait OE.
- Tous les composants prévus qui existent déjà : favori, menu trois points, navigation de section,
  champ secret, recherche, cartes de choix, chargeur, pied de dialogue, vue séparée, coquille.
