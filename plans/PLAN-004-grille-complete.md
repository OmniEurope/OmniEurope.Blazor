# PLAN-004 - Grille complète et virtualisation

> Objectif : `OmniDataGrid` couvre l'intégralité de la surface observée dans
> `docs/component-contracts.md` (RadzenDataGrid + RadzenDataGridColumn), avec virtualisation à
> défilement continu et **hauteur de tableau paramétrable**.
> Source de vérité du besoin : les paramètres réellement utilisés par les projets du parc.

## Contraintes non négociables

- CSP stricte : aucun attribut `style`, aucune balise `<style>`, aucun gestionnaire HTML inline.
  Les longueurs dynamiques (hauteur du tableau, largeurs de colonnes, décalages de colonnes gelées)
  passent par des propriétés personnalisées CSS posées depuis `omni-grid.js`.
- Clean-room : aucun code, CSS, script ni test provenant de Radzen. Seuls les **noms de paramètres
  observés** guident le besoin.
- `Style` (18 occurrences) est **volontairement non implémenté** : il violerait le contrat CSP.
  Le remplacement est `Class` + `Height` + `GridLines` + `Density`.

## Lot 1 - virtualisation et hauteur de tableau [fait]

- [x] `GridVirtualWindow` : index d'offsets Fenwick, estimation puis mesure réelle des lignes.
- [x] `GridVirtualDataSource` : cache épars par blocs, éviction bornée, annulation, total distant.
- [x] `omni-grid.js` : `attach`/`detach`/`sync`/`applyLayout`/`scrollToOffset`, rAF, `ResizeObserver`.
- [x] `AllowVirtualization`, `VirtualizationOverscanCount`, `EstimatedRowHeight`, `RowHeight`,
      `VirtualBlockSize`, `ScrollToIndexAsync`.
- [x] `Height` : hauteur de tableau en longueur CSS libre (`600px`, `50vh`, `100%`), plus mode
      « remplit le conteneur ». Remplace `ViewportHeight`.

## Lot 2 - colonnes

- [x] `Property` et `SortProperty` : accès par nom de propriété (1471 occurrences observées).
- [x] `FormatString` (195 occurrences).
- [x] `Width` et `MinWidth` en longueur CSS, appliquées via `<colgroup>` et propriétés personnalisées.
- [x] `SortOrder` initial.
- [x] `Frozen` : colonne collante avec décalage calculé.
- [x] `TextAlign`, `CssClass`, `HeaderCssClass`, `Visible`, `Resizable`, `Sortable`, `Filterable`.

## Lot 3 - filtrage

- [x] `FilterMode` : `Simple`, `SimpleWithMenu`, `Advanced`.
- [x] `FilterCaseSensitivity`.
- [x] Textes : `FilterText`, `ApplyFilterText`, `ClearFilterText`, `ContainsText`, `EqualsText`,
      `NotEqualsText`, `AndOperatorText`, `OrOperatorText`.
- [~] `FilterPopupRenderMode` : sans objet, les filtres sont rendus en ligne et pilotés par `FilterMode`.

## Lot 4 - lignes, sélection, édition

- [x] `RowRender`, `RowSelect`, `RowExpand`, `RowCollapse`, `RowUpdate`.
- [x] `RowClick`, `RowDoubleClick`, `AllowRowSelectOnRowClick`.
- [x] `EditMode` (`Single`, `Multiple`) avec état d'édition tenu par la grille.
- [x] `ExpandMode`, `ShowExpandColumn`, `ShowExpandAll`, `ExpandChildItemAriaLabel`.
- [x] `KeyProperty`, `Value` / `ValueChanged` (sélection par éléments).

## Lot 5 - groupes, pagination, présentation

- [x] `AllowGrouping`, `ShowGroupPanel`, `Groups`, `AllGroupsExpanded`.
- [x] `PageSizeOptions`, `PageSizeText`, `PagerPosition`, `PagerHorizontalAlign`,
      `PagingSummaryFormat`, `PageTitleFormat`, `PageAriaLabelFormat`, libellés et titres de pages.
- [x] `GridLines`, `Responsive`, `AllowAlternatingRows`, `ColumnWidth`, `ColumnResized`.

## Preuve attendue par lot

Build Release sans avertissement, suite bUnit verte, scan CSP vert, base d'API publique régénérée.

## Hors périmètre assumé

- `Style` : incompatible avec le contrat CSP.
- `AllowAltering` : la visibilité des colonnes reste pilotée par `Visible`.
- Glisser-déposer d'en-têtes vers le panneau de regroupement.
