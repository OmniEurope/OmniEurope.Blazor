# Données : grille, liste, pagination et arbre

Ce lot couvre `OmniDataGrid<TItem>`, `OmniDataGridColumn<TItem>`, `OmniDataList<TItem>`, `OmniPager`
et l'arbre. La surface de la grille est dimensionnée sur les paramètres réellement observés dans les
projets consommateurs, recensés dans [component-contracts.md](component-contracts.md).

## Sources de données

La grille accepte deux sources exclusives.

- `Items` : la collection est projetée localement. Le filtre, le tri et la pagination sont appliqués
  en mémoire par une projection pure, séparée du rendu.
- `Load` : un délégué asynchrone annulable reçoit un `OmniDataGridLoadRequest` et retourne un
  `OmniDataGridResult<TItem>`. La requête porte `Page`, `PageSize`, `Skip`, `Top`, les tris et les
  filtres actifs, ainsi qu'un `CancellationToken`. Une requête plus ancienne qui se termine après une
  plus récente est ignorée.

`Count` impose le total lorsque l'hôte le connaît déjà. `IsLoading` force l'état occupé.

## Colonnes

Une colonne se déclare par lambda ou par nom de propriété.

```razor
<OmniDataGrid TItem="Order" Items="@orders" Height="600px">
    <Columns>
        <OmniDataGridColumn TItem="Order" Property="Customer.Name" Title="Client" Filterable="true" Frozen="true" Width="220px" />
        <OmniDataGridColumn TItem="Order" Property="Total" Title="Total" FormatString="{0:n2}"
                            TextAlign="OmniDataGridTextAlign.End" SortOrder="OmniDataGridSortOrder.Descending" />
    </Columns>
</OmniDataGrid>
```

- `Property` accepte un chemin pointé (`Customer.Name`). Un maillon nul rend `null` au lieu de lever.
  `SortProperty` sépare la valeur triée de la valeur affichée.
- `Key` est facultatif : il retombe sur `Property`, puis sur `Title`.
- `FormatString` applique un format composite, `Format` un délégué, `Template` un rendu libre.
- `Width` et `MinWidth` sont des longueurs CSS. `Frozen` colle la colonne au bord de début.
- `TextAlign`, `CssClass`, `HeaderCssClass`, `Visible`, `Resizable`, `Sortable`, `Filterable` et
  `Groupable` complètent la déclaration. `FooterTemplate` et `HeaderTemplate` remplacent les cellules
  correspondantes.
- Sans aucune colonne déclarée, la grille rend une colonne unique portant la valeur de l'élément.

## Hauteur du tableau

`Height` accepte n'importe quelle longueur CSS : `600px`, `50vh`, `100%`. La valeur est posée sur le
viewport en propriété personnalisée CSS depuis `omni-grid.js`, jamais en attribut `style`, ce qui
respecte le contrat CSP. Sans `Height`, le tableau grandit avec son contenu ; en virtualisation il
retombe sur la hauteur par défaut de la feuille de styles.

Le script observe le viewport avec un `ResizeObserver` : une hauteur en pourcentage ou dépendante de
la mise en page suit les changements de taille du conteneur sans recharger la grille.

## Virtualisation et défilement continu

`AllowVirtualization` remplace la pagination par un défilement sur l'intégralité du jeu de lignes.

```razor
<OmniDataGrid TItem="LogLine"
              Load="LoadWindowAsync"
              AllowVirtualization="true"
              Height="70vh"
              EstimatedRowHeight="36"
              VirtualBlockSize="200"
              VirtualizationOverscanCount="6">
```

Mécanique :

- `GridVirtualWindow` tient les décalages verticaux dans un arbre de Fenwick. Chaque ligne part de
  `EstimatedRowHeight`, puis sa hauteur réelle mesurée dans le navigateur remplace l'estimation ; les
  lignes suivantes se décalent en conséquence. Recherche de position et mise à jour restent
  logarithmiques, y compris sur des millions de lignes.
- `RowHeight` fige la hauteur des lignes et supprime toute mesure, pour les jeux homogènes.
- Deux lignes d'espacement encadrent la fenêtre rendue. Leur hauteur est posée en propriété
  personnalisée par le script, ce qui donne une barre de défilement couvrant tout le total sans
  rendre les lignes absentes.
- Avec `Load`, `GridVirtualDataSource` charge par blocs de `VirtualBlockSize` lignes autour de la
  fenêtre, ignore les réponses obsolètes et évince les blocs éloignés : un défilement sans fin ne
  fait pas croître le cache indéfiniment. Les lignes non encore chargées rendent une ligne
  d'attente annoncée aux lecteurs d'écran.
- `ScrollToIndexAsync(index)` amène une ligne précise en haut du viewport.
- La table porte `aria-rowcount` et chaque ligne `aria-rowindex`, puisque le DOM ne contient qu'une
  fenêtre.

Limites assumées : la virtualisation refuse `GroupBy`, `Groups` et `DetailTemplate` par une exception
explicite, car ces rendus rompent la correspondance « une ligne pour un index » dont dépend la
géométrie de défilement. La pagination est ignorée dans ce mode.

## Tri, filtres et regroupements

- `AllowSorting`, `AllowFiltering`, `AllowPaging`, `AllowColumnResize` et `AllowGrouping` coupent les
  fonctions au niveau de la grille ; les paramètres de colonne affinent au niveau de la colonne.
- `FilterMode` vaut `Simple` (une saisie par colonne), `SimpleWithMenu` (saisie plus sélecteur
  d'opérateur) ou `Advanced` (deux conditions jointes par `Et`/`Ou`, appliquées sur action explicite).
- `OmniDataGridFilterOperator` couvre contient, ne contient pas, égal, différent, commence par, finit
  par, supérieur, supérieur ou égal, inférieur, inférieur ou égal, est nul, n'est pas nul, est vide
  et n'est pas vide. `FilterCaseSensitivity` choisit la comparaison.
- Les libellés `FilterText`, `ApplyFilterText`, `ClearFilterText`, `ContainsText`, `EqualsText`,
  `NotEqualsText`, `AndOperatorText` et `OrOperatorText` remplacent les textes par défaut, eux-mêmes
  localisés.
- `Groups` liste les regroupements actifs par clé de colonne, `GroupsChanged` les publie, et le
  panneau `ShowGroupPanel` les affiche avec un retrait par regroupement. `AllGroupsExpanded` fixe
  l'état initial ; chaque en-tête de groupe se replie individuellement.

## Sélection, lignes et édition

- `SelectionMode`, `SelectedKeys`/`SelectedKeysChanged`, `Value`/`ValueChanged`, `KeySelector` ou
  `KeyProperty`.
- `RowClick`, `RowDoubleClick`, `RowSelect`, `RowExpand`, `RowCollapse` et
  `AllowRowSelectOnRowClick`. Une ligne cliquable devient atteignable au clavier et répond à Entrée
  et Espace.
- `RowRender` reçoit un `OmniDataGridRowRenderArgs<TItem>` : classe CSS supplémentaire, ligne non
  sélectionnable, ligne non dépliable. Il ne peut pas produire de style inline.
- `EditMode` vaut `Single` ou `Multiple`. La grille tient son propre état d'édition via
  `EditRowAsync`, `UpdateRowAsync` et `CancelEditAsync` ; `IsEditing` reprend la main quand l'hôte
  préfère gérer l'état lui-même.
- `DetailTemplate` avec `ExpandMode`, `ShowExpandColumn`, `ShowExpandAll` et
  `ExpandChildItemAriaLabel`.

## Pagination

`OmniPager` sert la grille et reste utilisable seul : boutons première, précédente, numéros de page,
suivante et dernière, sélecteur `PageSizeOptions`, libellés et titres par bouton,
`PageTitleFormat`/`PageAriaLabelFormat` pour les numéros, alignement `PagerHorizontalAlign`.
`PagerPosition` place la barre en haut, en bas ou aux deux. `ShowPagingSummary` et
`PagingSummaryFormat` produisent le résumé « premier à dernier sur total ».

## Présentation

`GridLines`, `Density`, `AllowAlternatingRows` et `Responsive`. En mode responsive, chaque cellule
porte son intitulé en attribut `data-omni-label` et la feuille de styles empile la ligne en carte
sous 40 rem.

## Ce qui n'est délibérément pas fourni

- `Style` : un attribut `style` violerait le contrat CSP. Le remplacement est `Class`, `Height`,
  `GridLines`, `Density` et les largeurs de colonnes.
- Le glisser-déposer d'en-têtes vers le panneau de regroupement : le regroupement se pilote par le
  bouton d'en-tête et par `Groups`.
- Le choix de rendu d'une fenêtre surgissante de filtre : les filtres sont rendus en ligne, pilotés
  par `FilterMode`.
- Le sélecteur de colonnes visibles : `Visible` reste piloté par l'hôte.
