<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-003 - Ajouts OE demandés par la migration d'Aetheus

> Source et détail versionnés côté Aetheus :
> - `docs/plans/PLAN-005-migration-radzen-oe.md`, lots 2 à 9 ;
> - `docs/plans/PLAN-005-verification-oe.md`, qui fait foi.
>
> Aide-mémoire du dépôt, déplacé de `plans/` (ignoré par Git) vers `docs/plans/` et renuméroté le 2026-09-20 (PLAN-001, lot 2).
> Réécrit le 2026-09-14, en remplacement de la version du 2026-09-13, qui listait des évolutions
> abandonnées depuis.
> Statut : terminé. Lots 2 à 9 livrés sur `develop`, constaté contre `src/` le 2026-09-27 ; aucun
> reliquat côté OE.

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

Tous livrés sur `develop` ; revérifiés contre `src/` le 2026-09-27.

- Lot 2 : fait. `WasmSmoke` rend une grille et une liste virtualisées, un dialogue et une
  notification, exigés par `eng/Test-WasmHost.ps1` (`--assert-present`) sur l'artefact publié en
  Release sous CSP stricte. `OmniDataList` virtualisée ne pose plus d'attribut `style` (espaceurs
  dimensionnés par `omni-grid.js`). Dérives de doc (popup de filtre, onglets) corrigées. Preuve :
  `7516925`.
- Lot 3 : fait. `OmniIconName` passé de 86 à 162 glyphes Phosphor (`71929dc`), et couche
  `omni-u-*` en classes nouvelles (`bf59aa0`).
- Lot 4 : fait. `OmniDataAnnotationsValidator` (messages localisés) et `OmniAlert.Dismissible`,
  faux par défaut (`5d40c25`).
- Lot 5 : fait. `OmniPopover` (`a3ead9e`).
- Lot 6 : fait. `OmniDataGrid.EmptyTemplate`, et la virtualisation avec lignes de groupe et de
  détail en mode `Items` : `4d24139`, fusionné dans `develop` (`886b057`).
- Lot 7 : fait. `OmniPageHeader` et `OmniBreadcrumbService`, `OmniDetailShell`, `OmniLoginShell`,
  `OmniConnectionOverlay`, `OmniEmptyState`, `OmniWizard`, `OmniDescriptionList`,
  `OmniRelativeTime`, dans la famille `Pages` (`0ef620d`).
- Lot 8 : fait. `OmniResourceList`, `OmniEntityPicker`, `OmniDynamicForm`, `OmniStatusBadge` et sa
  table `OmniStatusMap`, `OmniLogViewer`, `OmniCodeBlock`, `OmniCodeViewer`, `OmniUnifiedDiff`
  (`02da73f`, terminé par `ec5405f`).
- Lot 9 : fait (`ec5405f`, `07e658a`) :
  - `OmniStatusStrip`, `OmniKanban`, `omni-boot.js` et l'écran de démarrage ;
  - l'extension d'`OmniMindMap` en graphe, avec la disposition en couches C# `OmniGraphLayout`,
    sans dagre ;
  - `OmniGitGraph`, `OmniGantt` ;
  - `OmniCodeEditor` et `OmniDiffViewer`. Écart assumé avec la demande : Monaco n'est pas chargé
    en iframe mais servi par l'hôte depuis sa propre origine, avec le moteur `PlainText` sous CSP
    stricte (voir `docs/csp-contract.md`).

Reliquat côté OE : aucun. La vérification de bout en bout reste celle d'Aetheus
(`PLAN-005-verification-oe.md`).

## Abandonné (OE couvre déjà ou design OE accepté)

- Toutes les autres évolutions : boutons, badges, typographie, espacements, listes déroulantes,
  champs, dialogues, notifications, infobulles, onglets, menus, barre latérale, palette sombre, CSS
  qui restylerait OE.
- Tous les composants prévus qui existent déjà : favori, menu trois points, navigation de section,
  champ secret, recherche, cartes de choix, chargeur, pied de dialogue, vue séparée, coquille.
