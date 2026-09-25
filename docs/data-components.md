# Données : grille, liste, pagination, arbre et tableur

Ce lot couvre `OmniDataGrid<TItem>`, `OmniDataGridColumn<TItem>`, `OmniDataList<TItem>`, `OmniPager`,
l'arbre et le tableur `OmniSpreadsheet`. La surface de la grille est dimensionnée sur les paramètres
réellement utilisés par les applications consommatrices.

## Sources de données

La grille accepte deux sources exclusives.

- `Items` : la collection est projetée localement. Le filtre, le tri et la pagination sont appliqués
  en mémoire par une projection pure, séparée du rendu.
- `Load` : un délégué asynchrone annulable reçoit un `OmniDataGridLoadRequest` et retourne un
  `OmniDataGridResult<TItem>`. La requête porte `Page`, `PageSize`, `Skip`, `Top`, les tris et les
  filtres actifs, ainsi qu'un `CancellationToken`. Une requête plus ancienne qui se termine après une
  plus récente est ignorée.

`Count` impose le total lorsque l'hôte le connaît déjà. `IsLoading` force l'état occupé.

Avec `LoadingTemplate`, la préparation initiale couvre la grille entière jusqu'à stabilisation
de la plage virtualisée et chargement des images visibles. La grille conserve sa géométrie pendant
cette attente et révèle ensuite son contenu en une fois. Une instance déjà préparée ne réaffiche
pas ce voile lorsqu'elle redevient visible ; un nouveau `IsLoading` reste affiché normalement.

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
- `Width` et `MinWidth` sont des longueurs CSS. `Frozen` colle la colonne au bord de début ; les
  colonnes de développement et de sélection qui la précèdent sont alors figées avec elle, et le
  décalage de chaque cellule figée est recalculé après chaque rendu (nouvelle page, tri, filtre).
- Le tableau ne descend jamais sous la somme des largeurs déclarées, des `MinWidth` et, pour une
  colonne sans largeur, du plancher `--omni-data-grid-column-min-width` (8 rem) : plus étroit, le
  viewport défile de côté au lieu d'écraser à zéro les colonnes sans largeur. Une largeur en
  pourcentage compte comme une colonne sans largeur. `MinWidth` est tenue exactement quand elle
  porte sur la seule colonne sans largeur ; à plusieurs, ces colonnes se partagent la place à
  parts égales au-dessus de ce minimum global.
- Une colonne alignée à la fin (`TextAlign`) aligne aussi son titre, à la fin de l'en-tête.
- `TextAlign`, `CssClass`, `HeaderCssClass`, `Visible`, `Resizable`, `Sortable`, `Filterable` et
  `Groupable` complètent la déclaration. `FooterTemplate` et `HeaderTemplate` remplacent les cellules
  correspondantes.
- Sans aucune colonne déclarée, la grille rend une colonne unique portant la valeur de l'élément.

## Hauteur du tableau

`Height` accepte n'importe quelle longueur CSS : `600px`, `50vh`, `100%`. La valeur est posée sur le
viewport en propriété personnalisée CSS depuis `omni-grid.js`, jamais en attribut `style`, ce qui
respecte le contrat CSP. Sans `Height`, le tableau grandit avec son contenu ; en virtualisation il
retombe sur la hauteur par défaut de la feuille de styles. Dès que le viewport défile (`Height`,
`FillAvailableHeight` ou virtualisation), l'en-tête entier (titres, rangée de filtres, ligne de
total en haut) reste collé en haut.

`MaxHeight` (longueur CSS, non défini par défaut) plafonne le viewport au lieu de le fixer : le
tableau prend la hauteur de son contenu jusqu'à ce plafond, puis défile. Une grille virtualisée de
trois lignes n'occupe donc que trois lignes au lieu de la hauteur virtuelle par défaut (30rem), et
une grille de dix mille lignes défile toujours en ne rendant que sa fenêtre, les espaceurs comptant
dans la hauteur du contenu. Une grille non virtualisée grandit de même jusqu'au plafond. Le plafond
passe par la propriété `--omni-grid-viewport-max`, posée par `omni-grid.js` (`applyMaxHeight`) et
lue par la classe `omni-data-grid__viewport--capped`. Il est ignoré quand `Height` ou
`FillAvailableHeight` dimensionne déjà le tableau. Sans `MaxHeight`, rien ne change : ni classe, ni
appel de script supplémentaire.

Le script observe le viewport avec un `ResizeObserver` : une hauteur en pourcentage ou dépendante de
la mise en page suit les changements de taille du conteneur sans recharger la grille.

`WheelScrollScope` (sélecteur CSS, non défini par défaut) nomme un ancêtre de la grille, par exemple
la page qui la contient. Un tour de molette vertical au-dessus de cet ancêtre fait alors défiler les
lignes, sans viser le tableau. La molette garde son comportement natif au-dessus de la grille, au-dessus
d'une autre zone qui peut encore défiler dans ce sens, avec Maj (défilement horizontal) ou avec Ctrl
(zoom). Une grille non affichée, par exemple dans un onglet masqué qui partage le même ancêtre, laisse
passer l'événement. Arrivées en bout de liste, les lignes s'arrêtent sans faire défiler la page.
`attachWheelScope` et `detachWheelScope` d'`omni-grid.js` portent l'écoute ; aucun attribut `style`.

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

Groupes et détails virtualisés : avec `Items`, la grille virtualise aussi en présence de `GroupBy`,
`Groups` ou `DetailTemplate`. L'unité de défilement devient la « case » d'un élément : ses en-têtes
de groupe ouverts avant lui, sa ligne, et sa ligne de détail si elle est ouverte. Chaque `tr` porte
`data-omni-slot`, le script additionne les hauteurs d'une même case, et replier un groupe ou ouvrir
un détail recalcule les cases puis remet les mesures à zéro. Un groupe replié devient une case
réduite à ses en-têtes.

Limites assumées : avec `Load`, la virtualisation refuse `GroupBy`, `Groups` et `DetailTemplate` par
une exception explicite, car le serveur ne livre que des blocs de lignes et aucun groupe ne se
calcule sans l'ensemble. La pagination est ignorée dans ce mode.

`EmptyTemplate` remplace le texte `EmptyText` quand la grille n'a aucune ligne ; sans lui, le
texte reste affiché comme avant.

`OmniDataList` avec `Virtualize` applique la même mécanique sans viewport propre : il suit l'ancêtre
qui défile, ou la page, ne rend que les éléments proches de la zone visible, mesure leur hauteur
(écart de la grille compris) et dimensionne ses deux espaceurs par la propriété personnalisée
`--omni-data-list-spacer`, posée depuis `omni-grid.js`. Aucun attribut `style` n'est rendu.

## Tri, filtres et regroupements

- `AllowSorting`, `AllowFiltering`, `AllowPaging`, `AllowColumnResize` et `AllowGrouping` coupent les
  fonctions au niveau de la grille ; les paramètres de colonne affinent au niveau de la colonne.
- `FilterMode` vaut `Simple` (une saisie par colonne), `SimpleWithMenu` (saisie plus sélecteur
  d'opérateur) ou `Advanced` (deux conditions jointes par `Et`/`Ou`, appliquées sur action explicite).
  En `Advanced`, la rangée de filtres montre un déclencheur qui résume la condition appliquée et
  ouvre un panneau ; deux conditions n'y tiennent pas sur la hauteur d'un contrôle.
- La rangée de filtres garde la hauteur d'un contrôle quelle que soit la forme du filtre. Une croix
  « Effacer » apparaît à côté d'un filtre, et seulement tant qu'il filtre ; dans les panneaux,
  « Effacer » est toujours le même bouton carré réduit à sa croix, de la taille standard d'un contrôle,
  nommé « Effacer » pour les lecteurs d'écran, quelle que soit la forme du filtre.
- `OmniDataGridColumnFilterType.Number` donne une saisie numérique et les opérateurs ordonnés à une
  colonne lue par `Value`, dont la grille ne connaît pas le type. `FilterOperators` restreint et ordonne
  les opérateurs proposés par une colonne ; un opérateur que son type ne permet pas est écarté.
- Dans un panneau, une condition simple garde opérateur, valeur et « Effacer » sur une même ligne ; la
  liste cochable, la plage de dates et le filtre avancé restent empilés.
- `ShowHeaderFilterMenu` range le même éditeur que la rangée (opérateur en `SimpleWithMenu`, les deux
  conditions en `Advanced`) dans un menu ouvert par l'entonnoir du titre, et retire la rangée.
- Les panneaux (menu d'en-tête, filtre avancé, liste cochable repliée) et la liste de suggestions
  du filtre `Combo` sont placés en position fixe par `omni-grid.js` sous leur déclencheur : le
  viewport qui défile ne les rogne plus. Un seul est ouvert à la fois ; un clic ailleurs ou Échap le
  ferme, appliquer ou effacer aussi.
- `OmniDataGridFilterOperator` couvre contient, ne contient pas, égal, différent, commence par, finit
  par, supérieur, supérieur ou égal, inférieur, inférieur ou égal, est nul, n'est pas nul, est vide
  et n'est pas vide. `FilterCaseSensitivity` choisit la comparaison.
### Forme du contrôle de filtre

`FilterType` choisit la forme du contrôle, sur un seul axe :

| `FilterType` | Contrôle rendu |
| --- | --- |
| `Text` (défaut) | saisie libre, comparée avec `FilterOperator` |
| `Select` | liste déroulante fermée des valeurs distinctes, comparée par égalité |
| `Combo` | saisie libre avec liste de suggestions |
| `MultiSelect` | liste cochable, la ligne correspond à une valeur cochée |

`FilterSearchable` ajoute une boîte de recherche au-dessus d'une liste `MultiSelect`, pour une colonne
dont le catalogue est trop long à parcourir à l'oeil. Les autres formes l'ignorent.

Dans la rangée de filtres, la liste `MultiSelect` est repliée sur une ligne qui résume les valeurs
cochées et s'ouvre à la demande ; dans un menu d'en-tête elle s'affiche ouverte.
`OmniDataGridFilterMultiSelect.Presentation` (`Compact` par défaut, ou `List`) choisit entre les
deux quand il est posé à la main dans un `FilterTemplate`, et `OmniDataGridFilterContext.InPopover`
dit au modèle où il est rendu.

```razor
<OmniDataGridColumn TItem="Order" Property="Country" Title="Pays" Filterable="true"
                    FilterType="OmniDataGridColumnFilterType.MultiSelect" FilterSearchable="true" />
```

`FilterValues` impose la liste des candidats quand la grille ne peut pas la déduire, notamment sur une
grille alimentée par `Load` qui ne voit que la page courante.

### Filtre entièrement sur mesure

Quand aucune des quatre formes ne convient, `FilterTemplate` remplace le contrôle sans toucher à la
grille. Le contexte porte l'identifiant à poser sur le champ, la valeur courante, les valeurs
candidates, le texte indicatif et le rappel qui applique une nouvelle valeur.

```razor
<OmniDataGridColumn TItem="Order" Property="Total" Title="Total" Filterable="true">
    <FilterTemplate Context="filter">
        <input id="@filter.Id" class="omni-input" type="number" value="@filter.Value"
               placeholder="@filter.Placeholder"
               @onchange="args => filter.ValueChanged(args.Value?.ToString() ?? string.Empty)" />
    </FilterTemplate>
</OmniDataGridColumn>
```

La valeur écrite reste une chaîne : elle traverse la projection, la requête `Load` et l'état persisté
comme n'importe quel autre filtre.

- Les libellés `FilterText`, `ApplyFilterText`, `ClearFilterText`, `ContainsText`, `EqualsText`,
  `NotEqualsText`, `AndOperatorText` et `OrOperatorText` remplacent les textes par défaut, eux-mêmes
  localisés.
- `Groups` liste les regroupements actifs par clé de colonne, `GroupsChanged` les publie, et le
  panneau `ShowGroupPanel` les affiche avec un retrait par regroupement. `AllGroupsExpanded` fixe
  l'état initial ; chaque en-tête de groupe se replie individuellement.

## Sélection, lignes et édition

- `SelectionMode`, `SelectedKeys`/`SelectedKeysChanged`, `Value`/`ValueChanged`, `KeySelector` ou
  `KeyProperty`.
- En sélection multiple, une case d'en-tête coche ou décoche les lignes sélectionnables de la page
  affichée ; les sélections des autres pages restent en place. Elle n'existe pas en virtualisation.
- `RowClick`, `RowDoubleClick`, `RowSelect`, `RowExpand`, `RowCollapse` et
  `AllowRowSelectOnRowClick`. Une ligne cliquable devient atteignable au clavier et répond à Entrée
  et Espace. Les cellules de contrôle (case, chevron, boutons d'édition) et les cellules d'une ligne
  en édition gardent leurs clics et leurs touches : cocher une case n'est pas aussi un clic de
  ligne qui la décocherait, et un espace tapé dans un éditeur ne sélectionne pas la ligne.
- `RowRender` reçoit un `OmniDataGridRowRenderArgs<TItem>` : classe CSS supplémentaire, ligne non
  sélectionnable, ligne non dépliable. Il ne peut pas produire de style inline.
- `ShowEditColumn`, actif par défaut, ajoute la colonne d'actions d'édition dès qu'au moins une
  colonne visible porte un `EditTemplate` ; le mettre à `false` retire cette colonne et laisse l'hôte
  déclencher l'édition lui-même. Ses actions sont des boutons icône (crayon, coche, croix) nommés et
  titrés « Modifier », « Enregistrer » et « Annuler », dans une vraie cellule de tableau.
- `EditMode` vaut `Single` ou `Multiple`. La grille tient son propre état d'édition via
  `EditRowAsync`, `UpdateRowAsync` et `CancelEditAsync` ; `IsEditing` reprend la main quand l'hôte
  préfère gérer l'état lui-même.
- `DetailTemplate` avec `ExpandMode`, `ShowExpandColumn`, `ShowExpandAll` et
  `ExpandChildItemAriaLabel`. Le bouton d'en-tête de `ShowExpandAll` ouvre ou ferme les lignes de la
  page affichée, sans toucher aux autres pages, et n'est proposé qu'en `ExpandMode.Multiple`.

## Pagination

`OmniPager` sert la grille et reste utilisable seul : boutons première, précédente, numéros de page,
suivante et dernière, sélecteur `PageSizeOptions`, libellés et titres par bouton,
`PageTitleFormat`/`PageAriaLabelFormat` pour les numéros, alignement `PagerHorizontalAlign`.
`PagerPosition` place la barre en haut, en bas ou aux deux. `ShowPagingSummary` et
`PagingSummaryFormat` produisent le résumé « premier à dernier sur total ».

Dans la grille, la barre n'apparaît que si `AllowPaging` est actif, que la virtualisation ne l'a pas
remplacée et qu'il existe plus d'une page. `AlwaysShowPager` la maintient visible même sur une page
unique, pour une mise en page qui ne doit pas se réorganiser au fil des filtres. Une liste locale
qui rétrécit sous la page courante est montrée depuis sa dernière page, et `PageChanged` le dit à
l'hôte : la barre et le résumé n'annoncent plus « page 5 sur 1 ».

## Présentation

`GridLines`, `Density`, `AllowAlternatingRows` et `Responsive`. En mode responsive, chaque cellule
porte son intitulé en attribut `data-omni-label` et la feuille de styles empile la ligne en carte
sous 40 rem.

## Ce qui n'est délibérément pas fourni

- `Style` : un attribut `style` violerait le contrat CSP. Le remplacement est `Class`, `Height`,
  `GridLines`, `Density` et les largeurs de colonnes.
- Le glisser-déposer d'en-têtes vers le panneau de regroupement : le regroupement se pilote par le
  bouton d'en-tête et par `Groups`.
- Une fenêtre surgissante de filtre avec choix de l'opérateur : l'édition complète (opérateur, deux
  conditions) reste dans la ligne de filtre pilotée par `FilterMode`. `ShowHeaderFilterMenu` ajoute
  seulement, dans l'en-tête, une fenêtre limitée à la valeur.
- Un rendu de filtre au choix de l'hôte hors des deux emplacements fournis : la rangée de filtres, ou
  le menu d'en-tête (`ShowHeaderFilterMenu`) ; `FilterTemplate` remplace le contrôle, pas son cadre.
- Le sélecteur de colonnes visibles : `Visible` reste piloté par l'hôte.

## Tableur

`OmniSpreadsheet` est un tableur simple, pas un Excel : une grille de cellules nommées par des lettres
de colonne et des numéros de ligne, une cellule active, la saisie en place, une barre de formule et
des formules calculées sur la feuille.

```razor
<OmniSpreadsheet @bind-Value="budget" ShowFormulaBar="true" ShowGridLines="true" />

@code {
    private OmniSpreadsheetData budget = OmniSpreadsheetData.FromRows(
    [
        ["Poste", "Janvier", "Février"],
        ["Loyer", "850", "850"],
        ["Total", "=SUM(B2:B2)", "=SOMME(C2:C2)"]
    ], rowCount: 10, columnCount: 6);
}
```

- **Modèle.** `OmniSpreadsheetData` est immuable et sérialisable : `ColumnCount` et `Rows`, des lignes
  de saisies telles que tapées (`12`, `Loyer`, `=SUM(B2:B6)`), jamais de résultats. `GetInput`,
  `WithInput` (qui agrandit la feuille pour atteindre une position), `AddRow`, `AddColumn`,
  `Evaluate` (la valeur calculée, en `OmniSpreadsheetValue`), `ColumnName`, `Address`, puis `ToJson`
  et `FromJson` (`{"columnCount":3,"rows":[[...]]}`, écrits et lus sans réflexion, donc sûrs au
  découpage). Chaque modification produit une nouvelle feuille, remontée par `ValueChanged`.
- **Saisies.** Un nombre se lit dans la culture courante ou avec un point ; un `%` final divise par
  cent ; une apostrophe initiale force le texte ; `=` ouvre une formule.
- **Formules.** Références `B3` (ou `$B$3`, sans effet d'ancrage faute de recopie), plages `A1:A5`
  en argument, `+ - * / ^`, signe, `%` final, parenthèses, texte entre guillemets, et les fonctions
  `SUM`, `AVERAGE`, `MIN`, `MAX`, `COUNT`, `ROUND`, `ABS`, aussi sous `SOMME`, `MOYENNE`, `NB` et
  `ARRONDI`. Les arguments se séparent par `,` ou `;`, et le séparateur décimal d'une formule est le
  point. Dans une plage, le texte et le vide sont ignorés ; en arithmétique, le vide vaut zéro.
- **Erreurs.** `#REF!` (référence hors de la feuille), `#DIV/0!`, `#NAME?` (fonction inconnue),
  `#VALUE!` (texte ou plage là où un nombre est attendu), `#NUM!` (résultat non fini), `#CIRC!`
  (formule qui dépend d'elle-même) et `#ERROR!` (formule illisible). Une erreur se propage aux
  cellules qui la lisent. Chaque cellule est calculée une fois par version de la feuille.
- **Clavier.** Flèches (Ctrl pour aller au bord), Tab et Maj+Tab le long de la ligne, la feuille
  étant quittée au bord de la ligne ; Entrée ou F2 ouvre la cellule, taper la remplace, Entrée
  valide et descend, Tab valide et passe à droite, Échap annule, Suppr vide ; Début, Fin, Ctrl+Début,
  Ctrl+Fin, Page haut et Page bas. En saisie par remplacement, une flèche valide et déplace ; en
  modification (Entrée, F2, double clic), elle déplace le curseur. La barre de formule modifie la
  cellule active de la même façon.
- **Accessibilité.** Un seul arrêt de focus, `role="grid"`, la cellule active annoncée par
  `aria-activedescendant`, en-têtes de ligne et de colonne, `aria-readonly` en lecture seule.
- **Paramètres.** `ReadOnly`, `ShowFormulaBar`, `ShowGridLines`, `AllowAddRows`, `AllowAddColumns`,
  `AriaLabel`, `ActiveCellChanged` (adresse A1). `AddRowAsync` et `AddColumnAsync` sont publiques.
- **CSP.** Aucun attribut `style`. `omni-spreadsheet.js` ne décide que des touches dont le navigateur
  garde l'effet (Blazor ne sait pas annuler une touche au cas par cas), place le curseur en fin de
  saisie et amène la cellule active dans la zone visible. La largeur des colonnes
  (`--omni-spreadsheet-column-width`, 7,5 rem) et la hauteur de la zone qui défile
  (`--omni-spreadsheet-height`, 26 rem) se redéfinissent sur une classe de l'hôte.

Hors périmètre, délibérément : sélection de plages, copier-coller, recopie de formules, formats de
nombre par cellule, largeur propre à une colonne, insertion ou suppression au milieu de la feuille,
fonctions conditionnelles et opérateurs de comparaison.

## Tableau de cartes : `OmniKanban`

`OmniKanban<TItem>` range des cartes en colonnes et laisse le lecteur déplacer une carte d'une colonne
ou d'une place à une autre, à la souris ou entièrement au clavier. Le tableau ne modifie jamais
`Items` : un déplacement est rapporté par `OnItemMoved` (`OmniKanbanMove<TItem>(Item, FromColumn,
ToColumn, Index)`, `Index` compté parmi les autres cartes de la colonne d'arrivée) et l'hôte l'applique
puis le sauvegarde ; sans cela la carte reste où elle était.

```razor
<OmniKanban TItem="Dossier" Columns="Colonnes" Items="Dossiers" ColumnOf="@(d => d.Etat)"
            KeyOf="@(d => d.Id)" ItemLabel="@(d => d.Reference)" OnItemMoved="DeplacerAsync">
    <CardTemplate Context="d"><strong>@d.Reference</strong> : @d.Demandeur</CardTemplate>
</OmniKanban>
```

- **Paramètres.** `Columns` (`OmniKanbanColumn(Key, Title)`, dans l'ordre de dessin), `Items`, `ColumnOf`
  (une carte dont la clé ne nomme aucune colonne n'est pas dessinée), `CardTemplate`, `KeyOf` (identité
  stable des éléments, l'élément lui-même par défaut), `ItemLabel` (nom de la carte dans les annonces,
  son texte par défaut), `ColumnHeaderTemplate` (remplace le titre et le compteur), `ReadOnly`, `Label`
  (« Tableau de cartes » par défaut) et `EmptyColumnText` (« Aucune carte »).
- **Clavier.** Chaque carte prend le focus. Espace ou Entrée la saisit ; les flèches la portent le long
  de sa colonne et d'une colonne à l'autre, la carte étant dessinée là où elle tomberait ; Espace ou
  Entrée la dépose et Échap la remet en place. Sans carte saisie, les flèches déplacent le focus d'une
  carte à l'autre (une colonne vide est sautée). Une touche frappée sur un contrôle placé dans une carte
  reste à ce contrôle. Une touche sur une autre carte abandonne le déplacement en cours.
- **Annonces.** Une région live polie annonce la saisie, chaque position (« A : colonne En cours,
  position 2 sur 3. »), le dépôt et l'annulation ; une annonce répétée est rendue différente pour être
  relue. L'aide clavier est reliée à chaque carte par `aria-describedby`.
- **Accessibilité.** Le tableau est une `region` nommée ; chaque colonne est une `section` et une liste
  nommées par leur en-tête, dont le compteur est lu en toutes lettres (« Cartes : 2 »).
- **Souris.** Glisser-déposer natif : la colonne visée se teinte et un emplacement en pointillé s'ouvre
  avant la carte que la carte glissée précéderait. Un dépôt à sa propre place ne rapporte rien.
- **Script et CSP.** `omni-kanban.js` ne fait que ce que Razor ne peut pas : garder les touches des
  contrôles internes, annuler le défilement des flèches et de l'espace, transmettre les touches de la
  carte à .NET par une référence `DotNetObjectReference` libérée à la destruction, déplacer le focus et
  fournir aux navigateurs qui l'exigent la donnée d'un glisser. Il n'écrit aucun style et ne dépend
  d'aucune image d'animation. Aucun état ne se marque par un trait latéral : fond, pointillé et ombre.
- **Preuves.** `KanbanComponentTests` (rendu, rôles et noms, déplacement clavier complet, annulation,
  abandon, lecture seule, glisser-déposer, libération du pont).
