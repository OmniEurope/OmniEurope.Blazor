# Données : grille, liste, pagination, arbre et tableur

Ce lot couvre `OmniDataGrid<TItem>`, `OmniDataGridColumn<TItem>`, `OmniDataList<TItem>`, `OmniPager`,
l'arbre (`OmniTree<TValue>`, `OmniTreeItem<TValue>`), le tableur `OmniSpreadsheet`, le tableau de cartes
`OmniKanban<TItem>`, le journal `OmniLogViewer` et l'export Markdown `OmniMarkdownExportButton<TItem>`. La surface de la grille est dimensionnée sur les paramètres
réellement utilisés par les applications consommatrices.

## Sources de données

La grille accepte deux sources exclusives.

- `Items` : la collection est projetée localement. Le filtre, le tri et la pagination sont appliqués
  en mémoire par une projection pure, séparée du rendu.
- `Load` : un délégué asynchrone annulable reçoit un `OmniDataGridLoadRequest` et retourne un
  `OmniDataGridResult<TItem>`. La requête porte `Page`, `PageSize`, `Skip`, `Top`, les tris et les
  filtres actifs, ainsi qu'un `CancellationToken`. Une requête plus ancienne qui se termine après une
  plus récente est ignorée. La première requête part une fois les colonnes rendues et l'état
  enregistré (`StateKey`) lu : elle porte déjà les filtres et tris par défaut des colonnes
  (`DefaultFilterValue`, `SortOrder`), sans requête préalable non filtrée. Elle part au premier rendu
  interactif : pendant un prérendu, la grille montre son état de chargement et ne charge rien, et
  une page rendue en statique, sans mode interactif, en reste là. Une colonne rendue après cette
  première requête (sous une condition, ou ajoutée plus tard) qui déclare un filtre ou un tri par
  défaut fait recharger les lignes avec ce défaut.

`Count` impose le total lorsque l'hôte le connaît déjà. `Busy` dit que l'hôte charge lui-même les
données d'une grille alimentée par `Items` : la grille se lit occupée et montre le même indicateur que
pour ses propres requêtes.

Chargement (R-432, demande de l'équipe d'une application cliente) : tout chargement, requête de la grille (premier
chargement compris, page, tri, filtre, bloc d'une grille virtualisée demandé au défilement) ou
chargement de l'hôte signalé par `Busy`, dessine une barre entre les en-têtes et la première ligne, sur
le tableau seul, dans une ligne sans hauteur : rien ne bouge quand elle vient ou part, et les lignes déjà
là restent en place et lisibles dessous ; une grille sans ligne encore garde son corps vide sous la barre
(ou `LoadingContent`). `ShowLoadingBar` (vrai par défaut) à `false` remplace au contraire les lignes, à
chaque chargement, par la ligne de chargement (`LoadingContent` ou le texte localisé). `LoadingBarMode`
choisit entre le balayage (`Sweep`, par défaut) et le remplissage continu (`Continuous`)
d'`OmniLoadingBar`. Un rafraîchissement en direct garde ses lignes et ne montre pas de barre.

Échec : quand `Load` lève, la grille affiche un message localisé suivi d'un bouton de reprise ;
`ErrorContent` (`RenderFragment<Exception>`) remplace le message, le bouton restant, et `OnLoadError`
reçoit l'exception pour que l'hôte la journalise (une requête annulée n'est pas un échec).
`OmniDataList` a le même `OnLoadError`.

Avec `LoadingContent`, la préparation initiale montre le gabarit sous les en-têtes jusqu'à
stabilisation de la plage virtualisée et chargement des images visibles. La grille conserve sa
géométrie pendant cette attente et révèle ensuite son contenu en une fois. Une instance déjà préparée
ne réaffiche pas ce gabarit lorsqu'elle redevient visible.

## Colonnes

Une colonne se déclare par lambda ou par nom de propriété.

```razor
<OmniDataGrid TItem="Order" Items="@orders" Height="600px">
    <Columns>
        <OmniDataGridColumn TItem="Order" Property="Customer.Name" Title="Client" Frozen="true" Width="220px" />
        <OmniDataGridColumn TItem="Order" Property="Total" Title="Total" FormatString="{0:n2}" Filterable="false"
                            TextAlign="OmniDataGridTextAlign.End" SortOrder="OmniDataGridSortOrder.Descending" />
    </Columns>
</OmniDataGrid>
```

- `Property` accepte un chemin pointé (`Customer.Name`). Un maillon nul rend `null` au lieu de lever.
  `SortProperty` sépare la valeur triée de la valeur affichée.
- `Key` est facultatif : il retombe sur `Property`, puis sur `Title`.
- `FormatString` applique un format composite, `Template` un rendu libre.
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
- `TextAlign`, `Class`, `HeaderClass`, `Visible`, `Resizable`, `Sortable`, `Filterable` et
  `Groupable` complètent la déclaration. `HeaderContent` remplace le titre, `FooterContent` la cellule
  de pied (fragments sans contexte).
- Une cellule de texte (colonne sans `Template`) tient sur une ligne et se termine par des points de
  suspension au lieu de déborder sur la colonne voisine. Une cellule à gabarit n'est pas rognée, pour
  ses badges, boutons, menus et champs d'édition ; `Class="omni-data-grid__cell--text"` l'inscrit
  à la même règle. En affichage en cartes (`Responsive`), le texte revient à la ligne.
- Des colonnes déclarées dans un `@foreach` peuvent capturer la variable de boucle dans leur
  `Template` ou leur `Value` : un délégué issu de la même lambda ne réinscrit pas la colonne. Les
  cellules montrent l'état capturé du rendu du parent dès ce rendu (la grille se redessine une fois de
  plus dans le même lot), et un `FilterValues` ou un `FilterOperators` écrit en littéral dans la boucle
  est comparé par son contenu.
- `AllowColumnAutoFit` (désactivé par défaut) donne au double clic sur le bord droit d'une colonne,
  ou à Entrée sur sa poignée, le comportement d'Excel : la colonne prend la largeur de son contenu le
  plus large. `AutoFit` sur la colonne l'emporte dans les deux sens ; sans valeur, elle suit la grille.
  Le geste est distinct du glisser d'`AllowColumnResize` : une colonne peut s'ajuster sans se glisser,
  et l'inverse. La mesure prend le titre, les lignes affichées et le texte de toutes les autres lignes
  chargées, fourni par .NET (valeur ou `FormatString`), y compris les lignes virtualisées
  hors écran. Une colonne à `Template` n'est mesurée que sur ses lignes affichées, son rendu n'étant
  pas un texte connu de .NET. La largeur retenue respecte le plancher de la grille et le `MinWidth` de
  la colonne, est persistée avec l'état (`StateKey`) et annoncée une seule fois par `OnColumnResize`.
- Sans aucune colonne déclarée, la grille rend une colonne unique portant la valeur de l'élément.

### Détacher les colonnes figées

Des colonnes figées larges mangent la place des colonnes qui défilent. Dès que le tableau défile de
côté, un bouton posé sur le bord de fin des colonnes figées, au niveau de l'en-tête, permet de les
détacher le temps de lire le reste de la ligne. Le cycle est une machine à trois états tenue par la
grille elle-même : état propre à chaque instance, jamais persisté (ni paramètre, ni stockage du
navigateur), perdu quand la grille est retirée de la page.

| État | Défilement horizontal | Colonnes figées | Bouton |
|---|---|---|---|
| **Figé, au début** | `scrollLeft` à 0 | collées au bord de début | masqué (`hidden`) |
| **Figé, défilé** | `scrollLeft` différent de 0 | collées, les lignes passent dessous | visible, `aria-pressed="false"`, icône cadenas ouvert, infobulle « Détacher les colonnes figées » |
| **Détaché** | `scrollLeft` différent de 0 | défilent avec le reste de la ligne (plus de `position: sticky`, plus d'ombre) | visible au même endroit, `aria-pressed="true"`, icône cadenas fermé, infobulle « Refiger les colonnes » |

Transitions :

1. Figé, au début → Figé, défilé : le tableau quitte le début.
2. Figé, défilé → Figé, au début : retour au début.
3. Figé, défilé → Détaché : clic, Entrée ou Espace sur le bouton.
4. Détaché → Figé, défilé : second clic sur le bouton, sans toucher au défilement.
5. Détaché → Figé, au début : retour au début. Le bouton se masque ; le défilement de côté suivant
   repart de la transition 1, colonnes figées (le détachement ne survit pas au retour au début).

Un nouveau défilement horizontal en état Détaché **ne refige pas** les colonnes. C'est le choix le
moins surprenant : on détache précisément pour faire défiler le reste de la ligne, refiger au premier
cran de molette annulerait le geste qui vient d'être demandé, et l'inertie d'un pavé tactile, qui
prolonge le défilement juste après le clic, refigerait les colonnes sans action de l'utilisateur.
Seuls le bouton et le retour au début referment le cycle, et aucun état n'est ambigu : un
détachement n'existe jamais au début du tableau. Le défilement vertical n'a aucun effet.

Accessibilité : un vrai `<button type="button">` hors de la zone défilante, atteint à la tabulation
juste avant le tableau et activé par Entrée ou Espace. Son nom accessible reste « Détacher les
colonnes figées » dans les deux états (motif du bouton bascule : `aria-pressed` dit si le
détachement est actif) ; l'infobulle et l'icône décrivent l'action suivante. Les textes viennent des
ressources localisées (`GridDetachFrozen`, `GridRefreezeFrozen`). Masqué au début du tableau, il sort
de l'ordre de tabulation ; s'il avait le focus à ce moment (retour au début à la molette juste après
un clic), le focus retombe sur le document, comme pour tout élément masqué.

Mise en oeuvre : le script ne prévient .NET qu'au franchissement de la position de début, jamais à
chaque image de défilement ; il replace le bouton pendant le défilement et après chaque rendu. En
écriture de droite à gauche, un `scrollLeft` négatif compte comme défilé.

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

`ScrollMode` choisit comment atteindre les lignes hors écran : `Paged` (par défaut) rend `PageSize` lignes sous
une barre de pagination, `Virtual` remplace la pagination par un défilement sur l'intégralité du jeu de lignes,
`All` rend toutes les lignes d'un coup, sans pagination ni virtualisation.

```razor
<OmniDataGrid TItem="LogLine"
              Load="LoadWindowAsync"
              ScrollMode="OmniDataGridScrollMode.Virtual"
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
- `FixedRowHeight` fait d'`EstimatedRowHeight` la hauteur exacte de chaque ligne : aucune mesure, et les
  lignes sont dessinées à cette hauteur (débordement coupé), pour les jeux homogènes.
- `VirtualizationOverscanCount` (3 par défaut) rend autant de lignes au-delà de chaque bord du viewport.
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

Groupes et détails virtualisés : avec `Items`, la grille virtualise aussi en présence de `Groups`
ou `DetailTemplate`. L'unité de défilement devient la « case » d'un élément : ses en-têtes
de groupe ouverts avant lui, sa ligne, et sa ligne de détail si elle est ouverte. Chaque `tr` porte
`data-omni-slot`, le script additionne les hauteurs d'une même case, et replier un groupe ou ouvrir
un détail recalcule les cases puis remet les mesures à zéro. Un groupe replié devient une case
réduite à ses en-têtes.

Limites assumées : avec `Load`, la virtualisation refuse `Groups` et `DetailTemplate` par
une exception explicite, car le serveur ne livre que des blocs de lignes et aucun groupe ne se
calcule sans l'ensemble. La pagination est ignorée dans ce mode.

`EmptyContent` remplace le texte `EmptyText` quand la grille n'a aucune ligne ; sans lui, le
texte reste affiché comme avant.

`OmniDataList` avec `Virtualize` applique la même mécanique sans viewport propre : il suit l'ancêtre
qui défile, ou la page, ne rend que les éléments proches de la zone visible, mesure leur hauteur
(écart de la grille compris) et dimensionne ses deux espaceurs par la propriété personnalisée
`--omni-data-list-spacer`, posée depuis `omni-grid.js`. Aucun attribut `style` n'est rendu.

## Tri, filtres et regroupements

- `AllowSorting`, `Filterable`, `AllowColumnResize` et `AllowGrouping` coupent les
  fonctions au niveau de la grille ; les paramètres de colonne affinent au niveau de la colonne.
  Sur la grille, les trois premiers valent `true` par défaut et `AllowGrouping` vaut `false`. Sur une
  colonne, `Sortable` et `Groupable` valent `true` par défaut.
- Le `Filterable` d'une colonne a trois états. Non réglé, il suit celui de la grille, à condition que la
  colonne ait de quoi filtrer (`Property`, `Value`, `FilterPredicate` ou `FilterTemplate`) : une colonne
  faite d'un seul `Template` (actions de ligne) n'a jamais de filtre sans le demander. `false` retire le
  filtre d'une colonne dans une grille filtrée ; `true` en donne un à une colonne dans une grille dont
  `Filterable` vaut `false`. Les deux usages : tout filtrer et exclure quelques colonnes, ou ne rien
  filtrer et choisir les colonnes une à une.
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
- `ShowHeaderFilterMenu`, actif par défaut depuis la recette R-041, range l'éditeur de filtre
  (opérateur en `SimpleWithMenu`, les deux conditions en `Advanced`) dans un menu ouvert par l'entonnoir
  du titre : l'en-tête reste sur une ligne. `ShowHeaderFilterMenu="false"` rend à la place la rangée de
  filtres en ligne sous l'en-tête.
- Les panneaux (menu d'en-tête, filtre avancé, liste cochable repliée) et la liste de suggestions
  du filtre `Combo` sont placés en position fixe par `omni-grid.js` sous leur déclencheur : le
  viewport qui défile ne les rogne plus. Un seul est ouvert à la fois ; un clic ailleurs ou Échap le
  ferme, appliquer ou effacer aussi.
- `OmniDataGridFilterOperator` couvre contient, ne contient pas, égal, différent, commence par, finit
  par, supérieur, supérieur ou égal, inférieur, inférieur ou égal, est nul, n'est pas nul, est vide
  et n'est pas vide, plus `In` et `NotIn` (la ligne correspond à l'une des valeurs candidates, ou à
  aucune) : `In` est l'opérateur du filtre `MultiSelect`, et leur valeur à plusieurs candidats se lit
  et s'écrit par `OmniDataGridFilterValues`. Les filtres ignorent la casse ; `CaseSensitiveFilters` la fait compter.
  Les accents comptent par défaut : `IgnoreDiacritics` les fait ignorer, et un hôte qui filtre
  lui-même derrière `Load` applique la même règle par `OmniDataGridFilterText.Normalize`.

### Forme du contrôle de filtre

`FilterType` (`OmniDataGridColumnFilterType`) choisit la forme du contrôle, sur un seul axe, parmi six formes :

| `FilterType` | Contrôle rendu |
| --- | --- |
| `Text` (défaut) | saisie libre, comparée avec `FilterOperator` |
| `Select` | liste déroulante fermée des valeurs distinctes, comparée par égalité |
| `Combo` | saisie libre avec liste de suggestions |
| `MultiSelect` | liste cochable, la ligne correspond à une valeur cochée |
| `DateRange` | début et fin facultatifs (`OmniDataGridFilterDateRange`) ; un jour seul couvre toute la journée, `FilterIncludesTime` ajoute les heures, et un chargeur distant reçoit deux bornes (`OmniDataGridDateRange`) |
| `Number` | saisie numérique et opérateurs ordonnés (égal, supérieur, inférieur...), pour une colonne dont la valeur est lue par une fonction et dont la grille ne peut pas déduire le type |

Les contrôles de `Combo`, `MultiSelect` et `DateRange` sont aussi publics, `OmniDataGridFilterCombo` (`OnPick` au choix d'une suggestion), `OmniDataGridFilterMultiSelect` (`Filterable`, `FormatValue`) et `OmniDataGridFilterDateRange`, pour être posés à la main dans un `FilterTemplate` ; ils rendent leurs attributs supplémentaires et leur `Id`.

`FilterSearchable` ajoute une boîte de recherche au-dessus d'une liste `MultiSelect` (le `Filterable` de
cette liste), pour une colonne dont le catalogue est trop long à parcourir à l'oeil. Les autres formes
l'ignorent. `FormatFilterValue` donne le texte affiché pour chaque valeur candidate (le nom traduit d'un
membre d'énumération), la valeur filtrée restant la même. Les deux noms portent le préfixe `Filter`
parce que `Filterable` et `FormatValue` désignent déjà le filtre de la colonne et ses cellules
([public-api-conventions.md](public-api-conventions.md)).

Dans la rangée de filtres, la liste `MultiSelect` est repliée sur une ligne qui résume les valeurs
cochées et s'ouvre à la demande ; dans un menu d'en-tête elle s'affiche ouverte.
`OmniDataGridFilterMultiSelect.Presentation` (`Compact` par défaut, ou `List`) choisit entre les
deux quand il est posé à la main dans un `FilterTemplate`, et `OmniDataGridFilterContext.InPopover`
dit au modèle où il est rendu.

```razor
<OmniDataGridColumn TItem="Order" Property="Country" Title="Pays"
                    FilterType="OmniDataGridColumnFilterType.MultiSelect" FilterSearchable="true" />
```

`FilterValues` (`IReadOnlyList<string>?`) impose la liste des candidats, proposés dans l'ordre donné et sans tri (des niveaux de TRACE à FATAL, par exemple), quand la grille ne peut pas la déduire, notamment sur une
grille alimentée par `Load` qui ne voit que la page courante.

### Filtre entièrement sur mesure

Quand aucune des six formes ne convient, `FilterTemplate` remplace le contrôle sans toucher à la
grille. Le contexte porte l'identifiant à poser sur le champ, la valeur courante, les valeurs
candidates, le texte indicatif et le rappel qui applique une nouvelle valeur.

```razor
<OmniDataGridColumn TItem="Order" Property="Total" Title="Total">
    <FilterTemplate Context="filter">
        <input id="@filter.Id" class="omni-input" type="number" value="@filter.Value"
               placeholder="@filter.Placeholder"
               @onchange="args => filter.ValueChanged(args.Value?.ToString() ?? string.Empty)" />
    </FilterTemplate>
</OmniDataGridColumn>
```

La valeur écrite reste une chaîne : elle traverse la projection, la requête `Load` et l'état persisté
comme n'importe quel autre filtre.

- Les textes des filtres (libellés, opérateurs, Appliquer, Effacer) viennent des ressources du paquet ;
  la grille n'a plus de paramètre de texte par instance, un hôte les remplace par
  `AddOmniEuropeTextOverrides` ([localization.md](localization.md)).
- `Groups` liste les regroupements actifs par clé de colonne, `GroupsChanged` les publie, et le
  panneau `ShowGroupPanel` les affiche avec un retrait par regroupement. `AllGroupsExpanded` fixe
  l'état initial ; chaque en-tête de groupe se replie individuellement.

## Sélection, lignes et édition

- `SelectionMode` et `Value`/`ValueChanged` (lignes choisies, `IReadOnlyList<TItem>`) ; `KeyOf` donne la
  clé qui reconnaît une ligne d'une page à l'autre. La page, la taille de page et la sélection sont un
  état interne de la grille, recopié depuis les paramètres quand l'hôte les change.
- En sélection multiple, une case d'en-tête coche ou décoche les lignes sélectionnables de la page
  affichée ; les sélections des autres pages restent en place. Elle n'existe pas en virtualisation.
- `OnRowClick`, `OnRowDoubleClick`, `OnRowExpand`, `OnRowCollapse` et
  `AllowRowSelectOnRowClick`. Une ligne cliquable devient atteignable au clavier et répond à Entrée
  et Espace. Les cellules de contrôle (case, chevron, boutons d'édition) et les cellules d'une ligne
  en édition gardent leurs clics et leurs touches : cocher une case n'est pas aussi un clic de
  ligne qui la décocherait, et un espace tapé dans un éditeur ne sélectionne pas la ligne.
- `OnRowClick` et `OnRowDoubleClick` reçoivent un `OmniDataGridRowMouseEventArgs<TItem>` : l'élément, sa
  position et les touches Ctrl, Maj, Alt et Méta tenues (toutes fausses quand la ligne est activée au
  clavier), pour sélectionner au Ctrl ou Maj clic et agir sur un clic simple. `OnRowContextMenu` reçoit le
  même objet au clic droit d'une ligne, sans le menu du navigateur ; l'événement remonte, si bien qu'un
  `OmniContextMenu` qui englobe la grille s'ouvre au pointeur.
- `OnCellClick` et `OnCellDoubleClick` reçoivent un `OmniDataGridCellMouseEventArgs<TItem>` : l'élément et sa
  position (`RowIndex`), la colonne de la cellule (`ColumnKey`, `ColumnTitle`, `ColumnProperty`, `ColumnIndex`
  parmi les colonnes de données visibles, colonnes de contrôle non comptées), la valeur lue par la colonne
  (`Value`, non formatée) et les touches et la position du pointeur, pour ouvrir le détail du chiffre
  double-cliqué. L'événement de cellule part d'abord, celui de ligne (`OnRowClick`, `OnRowDoubleClick`)
  ensuite ; les deux restent actifs ensemble. Ce sont des événements de pointeur seulement : le clavier
  active des lignes, pas des cellules, et l'hôte donne au clavier un autre chemin vers la même action.
  Ni les cellules de contrôle ni une ligne en édition ne les lèvent ; sans gestionnaire, la cellule ne
  porte aucun écouteur.
- `RowRender` reçoit un `OmniDataGridRowRenderArgs<TItem>` : classe CSS supplémentaire (`Class`), ligne non
  sélectionnable, ligne non dépliable. Il ne peut pas produire de style inline.
- `ShowEditColumn`, actif par défaut, ajoute la colonne d'actions d'édition dès qu'au moins une
  colonne visible porte un `EditTemplate` ; le mettre à `false` retire cette colonne et laisse l'hôte
  déclencher l'édition lui-même. Ses actions sont des boutons icône (crayon, coche, croix) nommés et
  titrés « Modifier », « Enregistrer » et « Annuler », dans une vraie cellule de tableau.
- `EditMode` (`OmniDataGridRowMode` : `Single` par défaut, `Multiple`). La grille tient son propre état
  d'édition via `EditRowAsync`, `UpdateRowAsync` et `CancelEditAsync`, et le signale par `OnRowEdit`,
  `OnRowUpdate` et `OnRowEditCancel`.
- `DetailTemplate` avec `ExpandMode` (`OmniDataGridRowMode`, `Multiple` par défaut), `ShowExpandColumn`
  et `ShowExpandAll` ; le nom accessible du chevron vient des ressources. Le bouton d'en-tête de
  `ShowExpandAll` ouvre ou ferme les lignes de la page affichée, sans toucher aux autres pages, et n'est
  proposé qu'en `OmniDataGridRowMode.Multiple`.

## Pagination

`OmniPager` sert la grille et reste utilisable seul : boutons précédente, numéros de page et suivante, première et dernière avec `ShowFirstLast`, sélecteur de taille de page. Ses textes (titres et noms accessibles des boutons, libellé de taille de page, résumé) viennent des ressources du paquet, remplaçables par `AddOmniEuropeTextOverrides` ([localization.md](localization.md)) ; il n'a aucun paramètre de texte par bouton, et chaque bouton numéroté est nommé par son chiffre.

- Seul, il prend ses propres paramètres : `Page`/`PageChanged` et `PageCount`, `PageSize`/`PageSizeChanged` et `PageSizeOptions`, `NumericPageCount` (nombre de numéros affichés), `ShowFirstLast`, `HorizontalAlign`, `Label` (nom de la navigation) et `Disabled`.
- Dans la grille, on ne le pose pas : la grille le rend et le règle par ses paramètres à elle, `PageSize`/`PageSizeChanged`, `PageSizeOptions`, `NumericPageCount`, `PagerHorizontalAlign` (transmis comme `HorizontalAlign`), `PagerPosition` (`OmniDataGridPosition` : `Bottom`, `Top` ou `TopAndBottom`, comme `FooterPosition` pour la ligne de total) et `ShowPagingSummary` (résumé localisé « premier à dernier sur total ») ; première et dernière y sont toujours montrées.

Dans la grille, la barre n'apparaît qu'en `ScrollMode` `Paged` et s'il existe plus d'une page. `AlwaysShowPager` la maintient visible même sur une page
unique, pour une mise en page qui ne doit pas se réorganiser au fil des filtres. Une liste locale
qui rétrécit sous la page courante est montrée depuis sa dernière page, et `PageChanged` le dit à
l'hôte : la barre et le résumé n'annoncent plus « page 5 sur 1 ».

## Présentation

`GridLines` (`Horizontal` par défaut, `None`, `Vertical`, `Both`), `Density`, `AllowAlternatingRows` et
`Responsive`. En mode responsive, chaque cellule
porte son intitulé en attribut `data-omni-label` et la feuille de styles empile la ligne en carte
sous 40 rem.

## Ce qui n'est délibérément pas fourni

- `Style` : un attribut `style` violerait le contrat CSP. Le remplacement est `Class`, `Height`,
  `GridLines`, `Density` et les largeurs de colonnes.
- Le glisser-déposer d'en-têtes vers le panneau de regroupement : le regroupement se pilote par le
  bouton d'en-tête et par `Groups`.
- Un rendu de filtre au choix de l'hôte hors des deux emplacements fournis : la rangée de filtres, ou
  le menu d'en-tête (`ShowHeaderFilterMenu`) ; `FilterTemplate` remplace le contrôle, pas son cadre.
- Le sélecteur de colonnes visibles : `Visible` reste piloté par l'hôte.

## Arbre : `OmniTree` et `OmniTreeItem`

`OmniTree<TValue>` est un arbre (`role="tree"`) d'`OmniTreeItem<TValue>`, à sélection simple ou multiple
(`aria-multiselectable`), la sélection étant liée par `Value`/`ValueChanged` ; `Label` (`string?`) le nomme.
Chaque entrée (`role="treeitem"`) déclare ses enfants dans son contenu ou les charge à la demande à sa
première ouverture (état de chargement annoncé, échec signalé et rapporté par `OnLoadError`), y compris
quand elle est ouverte d'emblée. Au clavier, Droite développe, Gauche
réduit, Entrée et Espace sélectionnent sans remonter à l'ancêtre ([accessibility-contract.md](contracts/accessibility-contract.md)).

### Ligne enrichie

`OmniTreeItem.TextContent` remplace le texte de la ligne par un fragment (icône, libellé mis en forme,
mention), indépendamment des éléments enfants de `ChildContent`. Il est rendu dans le bouton de la
ligne, donc sans élément interactif, et `Text` devient alors le nom accessible de la ligne. Sans lui,
la ligne affiche `Text` comme avant.

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
  (formule qui dépend d'elle-même) et `#ERROR!` (formule illisible, ou imbrication de parenthèses, de signes ou de références de cellules au-delà de 256 niveaux). Une erreur se propage aux
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
  `Label`, `ActiveCellChanged` (adresse A1). `AddRowAsync` et `AddColumnAsync` sont publiques.
- **CSP.** Aucun attribut `style`. `omni-spreadsheet.js` ne décide que des touches dont le navigateur
  garde l'effet (Blazor ne sait pas annuler une touche au cas par cas), place le curseur en fin de
  saisie et amène la cellule active dans la zone visible. La largeur des colonnes
  (`--omni-spreadsheet-column-width`, 7,5 rem) et la hauteur de la zone qui défile
  (`--omni-spreadsheet-height`, 26 rem) se redéfinissent sur une classe de l'hôte.

Hors périmètre, délibérément : sélection de plages, copier-coller, recopie de formules, formats de
nombre par cellule, largeur propre à une colonne, insertion ou suppression au milieu de la feuille,
fonctions conditionnelles et opérateurs de comparaison.

## Journal : `OmniLogViewer`

`OmniLogViewer` affiche un journal : lignes numérotées avec leur heure et leur sévérité, avertissements et
erreurs teintés. Il suit la dernière ligne tant que des lignes arrivent et lâche prise dès que le lecteur
remonte, avec un bouton pour revenir à la plus récente. Un filtre de niveau masque les lignes sous une
sévérité (`ShowLevelFilter`, `MinimumLevel`), et une recherche (`ShowSearch`, `SearchText`) marque chaque
occurrence d'un texte et passe de l'une à l'autre sans masquer de ligne ; `Wrap` fait passer les lignes
longues à la ligne. Le composant
n'ouvre aucune connexion : l'hôte lui passe les lignes, en ajout seul (une ligne remplacée avant la
dernière position déjà vue n'est pas détectée ; une liste plus courte ou dont cette ligne a changé repart
de zéro).

## Export Markdown : `OmniMarkdownExportButton`

Une grille exporte par sa propre barre (`ExportFormats`, section suivante). `OmniMarkdownExportButton<TItem>`
sert aux exports hors grille : une liste de cartes, un tableau de bord, un rapport. C'est un bouton qui
télécharge des lignes en fichier Markdown (`OmniMarkdownTableExporter`, service enregistré par
`AddOmniEuropeBlazor`), pour une lecture par une IA : toutes les lignes annoncées, lues par le fournisseur
de pages de l'export, et non la seule page affichée. Il est occupé pendant la lecture, et une page en échec ne produit aucun fichier.
`OnExport` reçoit le document produit une fois le fichier remis (une exception de son gestionnaire remonte, ce n'est pas un échec de l'export), `OnExportError` l'exception d'un échec ; `Text` (`string?`) remplace
le libellé localisé du bouton. Quand la source n'annonce pas de total et que la lecture s'arrête sur une
page pleine à la limite de lignes, le document écrit « N sur au moins M » et une note le dit :
`OmniMarkdownTableDocument.TotalIsLowerBound` est alors vrai et `IsComplete` faux. Le fichier se nomme
`{FileName}-{yyyy-MM-dd-HHmm}.md`, heure UTC (`FileName` vaut `export` par défaut).

## Barre d'export de la grille : `ExportFormats`

`OmniDataGrid` porte une barre « Tout exporter » dès que `ExportFormats` nomme au moins un format que
quelqu'un sait écrire : `Markdown` et `Csv` sont écrits par le paquet, sans dépendance ; `Excel` et `Pdf`
le sont par l'hôte, qui enregistre un `IOmniTableExportRenderer` (un format que personne n'écrit n'a pas
de bouton). `ExportPosition` place la barre sous le tableau (défaut), au-dessus, ou aux deux endroits. Chaque bouton porte l'icône de fichier de son format (`FileMd`, `FileCsv`, `FileXls`, `FilePdf`) et la variante `Secondary` (gris neutre), une exportation étant une autre action de sa zone ; `ExportVariants` en donne une autre par format (le Markdown en `Primary`, par exemple).

```razor
<OmniDataGrid TItem="Commande" Load="ChargerAsync" KeyOf="@(c => c.Id)"
              ExportFormats="Formats" ExportFileName="commandes" ExportTitle="Commandes"
              ExportFields="@(new OmniTableExportField[] { new("Application", "Boutique") })">
    <Columns>
        <OmniDataGridColumn TItem="Commande" Property="Reference" Title="Référence" />
        <OmniDataGridColumn TItem="Commande" Title="Statut" ExportValue="@(c => Libelle(c.Statut))">
            <Template Context="c"><OmniBadge>@Libelle(c.Statut)</OmniBadge></Template>
        </OmniDataGridColumn>
    </Columns>
</OmniDataGrid>
```

- **Lignes.** Toutes celles que les filtres en cours retiennent, dans le tri en cours, et non la page ou
  la fenêtre affichée : l'ensemble filtré et trié d'une grille à `Items` ; pour une grille à `Load`, des
  appels successifs de 200 lignes avec les tris et filtres en cours, jusqu'au total annoncé ou à
  `ExportRowLimit` (5 000 par défaut). `ExportLoad` remplace `Load` pour cette lecture quand `Load` fait
  plus que répondre à sa requête (il retient la page servie, par exemple). L'export ne change rien à ce
  que la grille affiche ; un bouton « Annuler » l'interrompt, et quitter la page aussi.
- **Colonnes.** Les colonnes visibles qui lisent une valeur (`Property` ou `Value`). Une colonne faite
  d'un seul `Template` (actions) n'est pas écrite, sauf si elle donne `ExportValue`, qui remplace aussi la
  valeur d'une colonne dont le gabarit montre autre chose (un statut traduit). `Exportable="false"` retire
  une colonne de l'export.
- **Document.** `OmniTableExportDocument` : titre (`ExportTitle`, à défaut `Caption`), lignes d'en-tête
  (`ExportFields`, puis un filtre actif par ligne, ajouté par la grille), colonnes avec leur nature
  (`Text`, `Number`, `Date`, `Boolean`), cellules (le texte affiché, et la valeur typée quand il y en a
  une), heure de génération, total annoncé, limite, culture. Il ne porte ni délégué ni composant : il se
  sérialise tel quel en JSON, ce qui permet à un hôte WebAssembly de l'envoyer à son serveur, qui y écrit
  le classeur ou le PDF.
- **CSV.** RFC 4180, fins de ligne CRLF, UTF-8 avec marque d'ordre d'octets ; séparateur `;` quand la
  culture écrit les décimales avec une virgule ; un nombre est écrit en valeur, sans séparateur de
  milliers ; un texte qui commence par `=`, `+`, `-`, `@`, une tabulation ou un retour chariot est
  précédé d'une apostrophe, pour qu'un tableur ne l'exécute pas comme une formule.
- **Fichier.** `{ExportFileName}-{yyyy-MM-dd-HHmm}.{extension}`, heure UTC (`shop-logs-2026-10-01-0840.md`) ; sans `ExportFileName`, le nom vient d'`ExportTitle`, puis de `Caption`, en minuscules sans accents et avec des traits d'union (`export` à défaut). `OnExport` reçoit le document
  une fois le fichier remis au navigateur. Un export coupé par la limite le dit dans la barre ; un échec
  y affiche « L'export a échoué. », ne produit aucun fichier et passe l'exception à `OnExportError`.
  Une exception levée par le gestionnaire `OnExport` n'est pas un échec de l'export (le fichier est
  déjà remis) : elle remonte comme celle de tout gestionnaire d'événement.
- **Hors grille.** `OmniTableExporter` (service enregistré par `AddOmniEuropeBlazor`) écrit un document
  dans un format : `Supports`, `RenderAsync`, `ToMarkdown`, `ToCsv`.

```csharp
// Hôte : les formats que le paquet n'écrit pas.
builder.Services.AddScoped<IOmniTableExportRenderer, RenduParLeServeur>();

public sealed class RenduParLeServeur(HttpClient http) : IOmniTableExportRenderer
{
    public bool Supports(OmniTableExportFormat format) => format is OmniTableExportFormat.Excel or OmniTableExportFormat.Pdf;

    public async Task<OmniTableExportFile> RenderAsync(OmniTableExportDocument document, OmniTableExportFormat format, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync($"api/exports/{format}", document, cancellationToken);
        response.EnsureSuccessStatusCode();
        return new(await response.Content.ReadAsByteArrayAsync(cancellationToken),
            response.Content.Headers.ContentType!.ToString(), format == OmniTableExportFormat.Pdf ? "pdf" : "xlsx");
    }
}
```

## Tableau de cartes : `OmniKanban`

`OmniKanban<TItem>` range des cartes en colonnes et laisse le lecteur déplacer une carte d'une colonne
ou d'une place à une autre, à la souris ou entièrement au clavier. Le tableau ne modifie jamais
`Items` : un déplacement est rapporté par `OnItemMove` (`OmniKanbanMove<TItem>(Item, FromColumn,
ToColumn, Index)`, `Index` compté parmi les autres cartes de la colonne d'arrivée) et l'hôte l'applique
puis le sauvegarde ; sans cela la carte reste où elle était. Sans gestionnaire `OnItemMove`, les cartes
ne se déplacent pas du tout (tableau en lecture).

```razor
<OmniKanban TItem="Dossier" Columns="Colonnes" Items="Dossiers" ColumnOf="@(d => d.Etat)"
            KeyOf="@(d => d.Id)" ItemLabel="@(d => d.Reference)" OnItemMove="DeplacerAsync">
    <CardTemplate Context="d"><strong>@d.Reference</strong> : @d.Demandeur</CardTemplate>
</OmniKanban>
```

- **Paramètres.** `Columns` (`OmniKanbanColumn(Key, Title)`, dans l'ordre de dessin), `Items`, `ColumnOf`
  (une carte dont la clé ne nomme aucune colonne n'est pas dessinée), `CardTemplate`, `KeyOf` (identité
  stable des éléments, l'élément lui-même par défaut), `ItemLabel` (nom de la carte dans les annonces,
  son texte par défaut), `ColumnHeaderTemplate` (remplace le titre et le compteur), `Label`
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
  abandon, tableau sans gestionnaire de déplacement, glisser-déposer, libération du pont).
