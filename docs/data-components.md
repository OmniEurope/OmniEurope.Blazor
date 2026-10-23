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
d'`OmniLoadingBar`. Un rafraîchissement en direct garde ses lignes et ne montre pas de barre. `NewRowHighlight` (`TimeSpan?`, nul par défaut : rien n'est marqué) fait lire en gras, pendant cette durée, une ligne qu'amène `RefreshAsync()` ; les lignes se reconnaissent par `KeyOf`, sans lequel rien n'est marqué non plus.

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
                            TextAlign="OmniAlignment.End" SortOrder="OmniDataGridSortOrder.Descending" />
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
- Une colonne alignée à la fin (`TextAlign`, un `OmniAlignment` : `Start` par défaut, `Center`, `End`) aligne aussi son titre, à la fin de l'en-tête. `Stretch` n'a pas de sens pour du texte : il se lit comme `Start`.
- `TextAlign`, `Class`, `HeaderClass`, `Visible`, `Hideable`, `Resizable`, `Sortable`, `Filterable` et
  `Groupable` complètent la déclaration (`Hideable` : voir « Réordonner, masquer et réinitialiser les
  colonnes »). `HeaderContent` remplace le titre, `FooterContent` la cellule
  de pied (fragments sans contexte).
- Une cellule de texte (colonne sans `Template`) tient sur une ligne et se termine par des points de
  suspension au lieu de déborder sur la colonne voisine. Une cellule à gabarit n'est pas rognée, pour
  ses badges, boutons, menus et champs d'édition ; `Class="omni-data-grid__cell--text"` l'inscrit
  à la même règle. En affichage en cartes (`Responsive`), le texte revient à la ligne.
- **Une colonne sans largeur se dimensionne sur son contenu.** Sans `Width`, sans largeur choisie par le
  lecteur et sans `Truncate`, la colonne prend la largeur de sa valeur la plus longue, titre compris :
  aucune cellule de texte n'est coupée par défaut. `omni-grid.js` mesure les cellules de texte des
  lignes rendues (la page courante, la fenêtre d'une grille virtualisée ; une cellule à `Template` n'est
  pas mesurée) et élargit la colonne de ce qui manque, à la première mise en page puis à chaque rendu qui
  apporte d'autres lignes ou d'autre texte (une page, un tri, un filtre, le défilement). La mesure lit
  tout avant d'écrire : un rendu coûte une mise en page, pas une par cellule. La largeur **ne fait que
  croître** tant que la grille vit, pour qu'un défilement qui fait entrer et sortir des lignes ne fasse
  pas sauter la colonne : elle ne rétrécit pas quand une page plus courte suit, et elle n'est oubliée que
  lorsque la colonne reçoit une largeur (déclarée ou choisie), passe en `Truncate`, quitte la grille ou que
  la grille est retirée de la page. Quand la somme des largeurs dépasse le cadre, le viewport défile
  de côté, et les colonnes qui n'ont aucune largeur (une colonne `Truncate`, une colonne à gabarit, une
  colonne courte) gardent le plancher `--omni-data-grid-column-min-width` (8 rem) au lieu d'être écrasées
  (`--omni-grid-fit-min`, posée par le script, que la feuille de style compare au minimum du tableau).
  Les colonnes figées voient leur décalage recalculé après chaque élargissement.
- `Truncate` (`bool`, faux par défaut) sort la colonne de cette mesure : elle garde la largeur que la
  mise en page lui donne, ses valeurs trop longues se terminent par des points de suspension et
  la valeur entière est lue dans l'infobulle de la cellule coupée. C'est le choix d'une colonne dont le texte
  long est fait pour être coupé (une description). Une largeur l'emporte toujours sur le contenu :
  `Width` déclarée, largeur choisie au glisser (`AllowColumnResize`), ajustement au double clic
  (`AllowColumnAutoFit`) ou largeur restaurée par `StateKey` ; la cellule coupée par cette largeur garde
  son ellipsis et son infobulle. `HeaderWrap="Truncate"` ne concerne que les titres : les cellules des
  colonnes sans largeur s'ajustent à leur contenu quand même.
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

`FillAvailableHeight` donne à la grille la hauteur qui reste sous elle dans la zone qui la fait
défiler (mesurée par `attachFill` dans `omni-grid.js`), jamais moins que son plancher : `MinHeight`
(`22.5rem` par défaut) pour le viewport, plus la barre d'en-tête, la barre de pied, le résumé et le
paginateur. Quand le contenu placé au-dessus remplit déjà la partie visible de cette zone (des
réglages dans un aperçu sur téléphone), la mesure vaut 0 et la grille garde ce plancher : la zone
défile jusqu'à elle. Le cadre qui réunit les barres et le tableau coupe ses coins sans devenir une
zone de défilement (`overflow: clip`) : un parent flex ou grille ne peut plus l'écraser sous son
contenu, barres et viewport compris.

Contrat d'hôte : une règle d'application qui pose sur la grille `flex: 1 1 0` et
`min-block-size: 0` remplace la hauteur et le plancher que le paquet lui donne. Dans un parent sans
hauteur, la grille mesure alors 0 px quel que soit `Height` ; le cadre garde sa hauteur mais déborde
de la grille et recouvre ce qui la suit. Pour remplir l'espace, `FillAvailableHeight` ; pour une
hauteur fixe, `Height` ; une base flex de 0 ne se donne à la grille que dans un parent dont la
hauteur est définie.

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
  lignes sont dessinées à cette hauteur (débordement coupé), pour les jeux homogènes. Sans
  `EstimatedRowHeight` (`null` par défaut), c'est la hauteur de ligne de la densité de la grille
  (jeton `--omni-grid-row-estimate` : 24, 36 ou 44 px en compacte, confortable ou aérée) : la feuille de
  style dessine les lignes à ce jeton et la virtualisation compte le même, lu par le script, si bien
  que les deux s'accordent dans toutes les densités. Une valeur donnée est imposée telle quelle, quelle
  que soit la densité. Une ligne en
  édition (`omni-data-grid__row--editing`) garde cette hauteur : ses cellules perdent leur marge
  verticale et ses contrôles tiennent dans la ligne. Les autres cellules ont leur propre interligne
  (1,25 rem) et une marge verticale ramenée à ce qui reste de la ligne une fois l'interligne et le filet
  retirés : la ligne garde sa hauteur sous toute police (`line-height: normal` suivait les métriques de
  la police, 41,2 px au lieu de 40 sous Papier).
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
- `HideFilterMenuOnSelect` (faux par défaut) ferme le menu de filtre d'un en-tête dès qu'une valeur y est choisie ; un clic à côté ou Échap le ferme toujours, quel que soit ce réglage.
- `HighlightActiveColumn` (faux par défaut) teinte toute une colonne, en-tête compris, tant qu'elle porte un tri ou un filtre.
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

Posés à la main, leurs paramètres sont les suivants. `OmniDataGridFilterCombo.Suggestions` (`IReadOnlyList<string>`) liste les candidats sous la saisie, réduits à ceux qui contiennent le texte tapé (casse et accents ignorés) ; `MaxSuggestions` (50 par défaut) borne le nombre de suggestions rendues et `Placeholder` est le texte indicatif de la saisie. `OmniDataGridFilterMultiSelect.Suggestions` liste les valeurs de la liste cochable ; `MaxSuggestions` (200 par défaut) borne le nombre d'options rendues à la fois et `Placeholder` est le texte de la boîte de recherche, repris dans le résumé replié tant que rien n'est coché. `OmniDataGridFilterDateRange.IncludesTime` (faux par défaut) fait choisir les heures en plus du jour.

`FilterSearchable` ajoute une boîte de recherche au-dessus d'une liste `MultiSelect` (le `Filterable` de
cette liste), pour une colonne dont le catalogue est trop long à parcourir à l'oeil. Les autres formes
l'ignorent. `FormatFilterValue` donne le texte affiché pour chaque valeur candidate (le nom traduit d'un
membre d'énumération), la valeur filtrée restant la même. Les deux noms portent le préfixe `Filter`
parce que `Filterable` et `FormatValue` désignent déjà le filtre de la colonne et ses cellules
([public-api-conventions.md](public-api-conventions.md)).

Dans la rangée de filtres, la liste `MultiSelect` est repliée sur une ligne qui résume les valeurs
cochées et s'ouvre à la demande ; dans un menu d'en-tête elle s'affiche ouverte.
`OmniDataGridFilterMultiSelect.Display` (`Compact` par défaut, ou `List`) choisit entre les
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
- `GroupLabel` (`Func<object?, int, string>?`) écrit le texte d'un en-tête de groupe à partir de la valeur du groupe et de son nombre de lignes ; nul, l'en-tête montre `Titre : valeur (nombre)`.

### Agrégats et pieds de groupe

`Aggregate` (`OmniDataGridAggregate` : `None` par défaut, `Sum`, `Average`, `Min`, `Max`, `Count`) fait
calculer une colonne. Le résultat va dans la ligne de pied de la grille, sur toutes les lignes que les
filtres retiennent (toutes les pages d'une grille à `Items`, les lignes chargées d'une grille à `Load`),
et, tant que des groupes sont actifs, dans une ligne qui ferme chaque groupe, sur les lignes du groupe.
`AggregateFormat` écrit le pied de grille (`Total : {0:n2}`), `GroupAggregateFormat` le pied de groupe
(`Sous-total : {0:n2}`) ; sans eux, `FormatString` puis la valeur en texte. `Sum` et `Average` additionnent
les nombres (en `decimal`, en `double` dès qu'une valeur est `double` ou `float`), `Min` et `Max` comparent
des valeurs d'un même type (nombres, dates, textes), `Count` compte les lignes ; une valeur vide est
ignorée. Une colonne qui a un `FooterContent` le garde dans le pied de grille.

`GroupFooterTemplate` (`RenderFragment<OmniDataGridGroupContext<TItem>>`) remplace l'agrégat dans la ligne
de pied de groupe : le contexte donne la valeur groupée (`Key`), la colonne groupée (`ColumnKey`), le niveau
(`Level`, 0 pour le regroupement extérieur) et les lignes du groupe (`Items`, groupes imbriqués compris).

```razor
<OmniDataGrid TItem="Poste" Items="Postes" AllowGrouping="true" Groups="@([new OmniDataGridGroup("Groupe")])">
    <Columns>
        <OmniDataGridColumn TItem="Poste" Property="Groupe" Title="Groupe">
            <GroupFooterTemplate>@context.Key</GroupFooterTemplate>
        </OmniDataGridColumn>
        <OmniDataGridColumn TItem="Poste" Property="Emissions" Title="tCO2e" Aggregate="OmniDataGridAggregate.Sum"
                            AggregateFormat="Total : {0:n2}" GroupAggregateFormat="Sous total : {0:n2}" />
    </Columns>
</OmniDataGrid>
```

La ligne de pied de groupe (`omni-data-grid__group-footer`, `data-omni-group-level`) n'existe que si une
colonne visible porte un `Aggregate` ou un `GroupFooterTemplate`. Elle suit la dernière ligne du groupe,
les groupes intérieurs avant les extérieurs ; un groupe replié garde la sienne sous son en-tête, un groupe
caché par un ancêtre replié n'en a pas. Comme les compteurs des en-têtes, elle porte sur les lignes de la
vue : la page affichée d'une grille paginée. L'export n'écrit ni pieds de groupe ni agrégats.

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
  `OnRowUpdate` et `OnRowEditCancel`. Une ligne qui passe en édition donne le focus à un champ, son
  texte entièrement sélectionné pour taper directement : celui de la cellule qu'on vient d'appuyer dans
  cette ligne quand elle en a un (double clic, clic que l'hôte change en `EditRowAsync`), sinon le
  premier champ modifiable de la ligne (`omni-grid.js`, `focusEditor`). Une touche tapée dans un
  éditeur reste à l'éditeur : Entrée ou Espace n'y lèvent pas `OnRowClick`.
- Dans une ligne en édition, Entrée enregistre la ligne et Échap l'annule : le script appuie sur les
  boutons « Enregistrer » et « Annuler » de la ligne, donc le même chemin qu'un clic (`OnRowUpdate`,
  `OnRowEditCancel`), après avoir fait valider au champ la valeur tapée (un clic la valide en lui
  prenant le focus, une touche non). Entrée reste à un champ multiligne (`textarea`, `contenteditable`,
  `aria-multiline="true"`) et à un bouton de la cellule (le bouton du calendrier, par exemple) ; les
  deux touches restent à un éditeur dont la liste, le calendrier ou le volet est ouvert dans la cellule
  (`aria-expanded="true"`, `details[open]`), qui les traite lui-même. Les touches avec Maj, Ctrl, Alt
  ou Méta ne sont pas prises. La ligne quittée, le focus revient à la ligne quand elle le prend, sinon à
  son bouton « Modifier ». Sans colonne d'édition (`ShowEditColumn="false"`), la ligne n'a pas ces
  boutons et les touches restent à l'hôte. Le script écoute le document en phase de capture
  (`grid/edit-focus.js`), pour lire l'éditeur dans l'état où la touche l'a trouvé.
- Une ligne en édition garde sa hauteur de lecture dans toutes les densités : une cellule de lecture fait
  deux marges verticales et une ligne de texte, un éditeur la hauteur d'un contrôle
  (`--omni-control-height`, plus haute que la ligne de texte dans les trois densités). Les cellules qui
  portent un éditeur cèdent de chaque marge la moitié de la différence, et leurs contrôles (`input`,
  `select`, `button`, `.omni-input`, `.omni-button`, y compris dans un éditeur enveloppé comme la liste
  déroulante ou le calendrier) se posent en haut de leur ligne, qui fait alors exactement la hauteur du
  contrôle. Un éditeur plus haut qu'une ligne de lecture (une zone de texte) agrandit la ligne. La
  hauteur fixe (`FixedRowHeight`) garde sa propre règle, plus bas.
- `DetailTemplate` avec `ExpandMode` (`OmniDataGridRowMode`, `Multiple` par défaut), `ShowExpandColumn`
  et `ShowExpandAll` ; le nom accessible du chevron vient des ressources. Le bouton d'en-tête de
  `ShowExpandAll` ouvre ou ferme les lignes de la page affichée, sans toucher aux autres pages, et n'est
  proposé qu'en `OmniDataGridRowMode.Multiple`.
- `ExpandedKeys` / `ExpandedKeysChanged` (`IReadOnlyList<object>`) lient les clés (`KeyOf`) des lignes dont le détail est ouvert : l'hôte garde la liste (`@bind-ExpandedKeys`) et la grille la lui rend à chaque ouverture ou fermeture, avec une seule clé en `ExpandMode` `Single`.

## Lignes en arbre

`ChildrenOf` (`Func<TItem, IReadOnlyList<TItem>?>`) fait de la grille une arborescence : `Items` donne les
lignes de premier niveau et `ChildrenOf` les enfants d'une ligne (null ou vide pour une feuille). Les lignes
sont reconnues par `KeyOf`.

```razor
<OmniDataGrid TItem="Poste" Items="Categories" KeyOf="p => p.Id" ChildrenOf="p => p.Enfants"
              CanToggleTreeRow="p => p.Niveau > 0" InitiallyExpanded="p => false"
              ToggleTreeOnRowClick="true" @bind-TreeExpandedKeys="Ouverts" OnCellDoubleClick="DetaillerAsync">
```

- **Rendu.** La colonne d'arbre (`TreeColumnKey`, à défaut la première colonne visible) commence par un retrait
  par niveau (`--omni-data-grid-tree-indent`, 1,25 rem ; le 8e niveau et au-delà partagent le même), puis le
  chevron d'une ligne qui a des enfants, ou un blanc de même largeur sur une feuille pour aligner les textes.
  La ligne porte `data-omni-tree-level` (profondeur depuis 0), sur laquelle l'hôte peut mettre en forme un
  niveau (`RowRender` donne aussi une classe par ligne).
- **État.** `TreeExpandedKeys` / `TreeExpandedKeysChanged` lient les clés des lignes ouvertes ; la grille tient
  son propre état, remplacé par une nouvelle liste de l'hôte. `InitiallyExpanded` décide une seule fois de
  l'état d'une ligne que la grille rencontre et que la liste n'a pas déjà décidée ; sans lui, tout part fermé.
  `CanToggleTreeRow` à `false` rend une ligne toujours ouverte et sans chevron : ses enfants restent visibles.
  Fermer une ligne cache toute sa descendance. `ExpandAllTreeRowsAsync()` et `CollapseAllTreeRowsAsync()`
  ouvrent ou ferment toutes les lignes qui peuvent l'être et le rapportent une fois.
- **Clavier et accessibilité.** La table prend `role="treegrid"`, chaque ligne `aria-level` (depuis 1) et, si
  elle peut s'ouvrir, `aria-expanded`. Le chevron est un vrai bouton (nom « Développer la ligne » ou « Réduire
  la ligne »), qui garde ses clics et ses touches. Avec `ToggleTreeOnRowClick`, un clic sur la ligne, Entrée
  ou Espace l'ouvrent ou la ferment, Droite l'ouvre et Gauche la ferme ; les lignes deviennent focalisables.
  Chaque clic bascule la ligne, si bien qu'un double clic la laisse dans son état (ouverte puis refermée, ou
  l'inverse) : le double clic sur une cellule (`OnCellDoubleClick`) garde son action sans changer l'arbre.
- **Tri et filtres.** Le tri range les frères sous leur parent, à chaque niveau. Un filtre garde les lignes
  qu'il retient, leurs ancêtres (pour les situer) et leurs descendants ; tant qu'il est actif, chaque ligne
  gardée est montrée ouverte, sans toucher à l'état enregistré.
- **Limites.** Une grille en arbre rend toutes ses lignes : `ScrollMode`, la pagination et la virtualisation ne
  s'y appliquent pas, ni le regroupement (`Groups` est ignoré). `Load` est refusé par une exception, l'arbre
  demandant toutes les lignes. L'agrégat de pied de grille porte sur les lignes de premier niveau ; l'export
  écrit toutes les lignes retenues, à tous les niveaux et dans l'ordre de l'arbre, sans retrait.

## Pagination

`OmniPager` sert la grille et reste utilisable seul : boutons précédente, numéros de page et suivante, première et dernière avec `ShowFirstLast`, sélecteur de taille de page. Ses textes (titres et noms accessibles des boutons, libellé de taille de page, résumé) viennent des ressources du paquet, remplaçables par `AddOmniEuropeTextOverrides` ([localization.md](localization.md)) ; il n'a aucun paramètre de texte par bouton, et chaque bouton numéroté est nommé par son chiffre.

- Seul, il prend ses propres paramètres : `Page`/`PageChanged` et `PageCount`, `PageSize`/`PageSizeChanged` et `PageSizeOptions`, `NumericPageCount` (nombre de numéros affichés), `ShowFirstLast`, `Justify`, `Label` (nom de la navigation) et `Disabled`.
- Dans la grille, on ne le pose pas : la grille le rend et le règle par ses paramètres à elle, `PageSize`/`PageSizeChanged`, `PageSizeOptions`, `NumericPageCount`, `PagerJustify` (transmis comme `Justify`), `PagerPlacement` (`OmniDataGridPlacement` : `Bottom`, `Top` ou `TopAndBottom`, comme `FooterPlacement` pour la ligne de total) et `ShowPagingSummary` (résumé localisé « premier à dernier sur total ») ; première et dernière y sont toujours montrées.

Dans la grille, la barre n'apparaît qu'en `ScrollMode` `Paged` et s'il existe plus d'une page. `AlwaysShowPager` la maintient visible même sur une page
unique, pour une mise en page qui ne doit pas se réorganiser au fil des filtres. Une liste locale
qui rétrécit sous la page courante est montrée depuis sa dernière page, et `PageChanged` le dit à
l'hôte : la barre et le résumé n'annoncent plus « page 5 sur 1 ».

## Présentation

`GridLines` (`Horizontal` par défaut, `None`, `Vertical`, `Both`), `Density` (`OmniDensity?` : nulle par défaut, la grille suit alors la densité de l'`OmniThemeScope` qui l'entoure, `Comfortable` hors portée ; une valeur la fixe pour la grille seule), `AllowAlternatingRows` et
`Responsive`. En mode responsive, chaque cellule
porte son intitulé en attribut `data-omni-label` et la feuille de styles empile la ligne en carte
sous 40 rem.

Les lignes alternent par défaut (`AllowAlternatingRows`, `true` depuis la 1.8.0) : une ligne sur deux
prend une part de la couleur du texte posée sur le cadre, lue dans le jeton de portée
`--omni-grid-stripe-share` (4 % par défaut et sur les portées claires, 5 % sur les portées sombres, où
4 % laissait deux lignes voisines à 1,055:1 sur un cadre noir ; `DataGridEditRowAndDefaultsTests`
mesure chaque thème et chaque palette en sombre). `AllowAlternatingRows="false"` garde des lignes d'un
seul fond, séparées par leur filet.

Les titres de l'en-tête prennent la taille du texte des cellules : `--omni-grid-header-font-size`, que
la feuille lit avec `1em` pour repli (la taille de la grille, `--omni-font-size-base` par défaut).
L'en-tête garde le remplissage des cellules et grandit avec ses titres comme une ligne avec son texte.
Pour les titres de 12 px d'avant la 1.8.0, poser sur la grille ou la portée
`--omni-grid-header-font-size: var(--omni-font-size-sm)`.

`HighlightRowOnHover` teinte la ligne sous le pointeur ; `HighlightColumnOnHover` (désactivé par défaut)
teinte la colonne entière de la cellule du corps sous le pointeur, en-tête, rangée de filtres et pied
compris, pour suivre un chiffre dans un tableau large. Le survol d'un en-tête, d'une cellule de contrôle
ou d'une grille imbriquée ne teinte rien. `HighlightOnHover="false"` sur une colonne la retire du jeu :
survolée, elle ne teinte rien (classe `omni-data-grid__column--no-hover`). Mise en oeuvre : la racine porte
`omni-data-grid--column-hover` et `grid/column-hover.js` (`attachColumnHover`, `detachColumnHover`
d'`omni-grid.js`) pose et retire la classe `omni-data-grid__column--hover` sur les cellules de la colonne,
sans aller-retour .NET ni attribut `style` ; la feuille peint la teinte en image de fond, par-dessus la
couleur d'une cellule figée et l'état de la ligne. Une cellule redessinée par Blazor pendant le survol
reprend sa teinte au mouvement suivant du pointeur.

## Enregistrement de l'état (`StateKey`)

`StateKey` active l'enregistrement de l'état de la grille : filtres, tris, largeurs choisies, ordre et
visibilité des colonnes. Sans `StateKey`, rien n'est lu ni écrit. L'état est une préférence de
l'utilisateur tenue par `OmniPreferences`, sous la clé `omni.grid.` suivie de `StateKey`
(`omni.grid.commandes` pour `StateKey="commandes"`) : dans la base de l'application quand elle
enregistre un `IOmniPreferenceStore` (`AddOmniEuropePreferences`), dans le cache `localStorage` du
navigateur sinon. Chaque tri, filtre, redimensionnement ou changement de disposition réécrit la
préférence.

- Format : JSON (conventions web de `System.Text.Json`, noms en camelCase) avec un numéro `version`.
  La version 2 ajoute `columnOrder` (clés des colonnes dans l'ordre choisi, `null` pour l'ordre
  déclaré) et `columnVisibility` (clé vers visible ou non, seulement pour les colonnes que
  l'utilisateur a changées). Un état de la version 1 (filtres, tris, largeurs seulement) se relit sans
  perte : l'ordre et la visibilité restent ceux déclarés. Une partie absente ou une entrée incomplète
  est ignorée, le reste est restauré ; une valeur illisible garde l'état déclaré.
- Identité des colonnes : la `Key` de la colonne (à défaut `Property`, puis `Title`), la même que pour
  les tris, filtres et largeurs.
- Ordre : une colonne absente de l'ordre enregistré (ajoutée depuis) se place juste après la colonne
  déclarée avant elle ; une clé enregistrée dont la colonne n'existe plus est ignorée. Les colonnes
  figées (`Frozen`) gardent leurs places figées : l'ordre permute les colonnes figées entre elles et
  les autres entre elles, jamais d'un groupe à l'autre, car une colonne figée est collée au bord par la
  largeur des colonnes figées qui la précèdent.
- Visibilité : `Visible` sur la colonne est la visibilité déclarée ; une visibilité enregistrée
  l'emporte, mais seulement là où le lecteur pourrait la défaire : sur une grille à `AllowColumnHide`
  et pour une colonne `Hideable`. Sans `AllowColumnHide`, ou pour une colonne `Hideable="false"`, l'état
  enregistré (base, autre onglet, ancienne disposition) ne masque rien : la visibilité déclarée
  s'applique, et l'état gardé revaut si l'hôte permet de nouveau de masquer. Un état qui masquerait
  toutes les colonnes garde, dans l'ordre affiché, la première colonne déclarée visible, comme le sélecteur en garde toujours
  une. Une colonne masquée disparaît de l'en-tête, des cellules, du pied, des filtres et de
  l'export, et garde son tri et son filtre.
- Réinitialisation de la disposition (`ResetLayoutAsync`, ou l'action du sélecteur de colonnes) : l'ordre,
  la visibilité et les largeurs reviennent à la déclaration, et l'état est réécrit sans eux
  (`columnOrder` et `columnVisibility` à `null`, `columnWidths` vide) ; les filtres et les tris
  restent, et les lignes ne sont pas rechargées. La préférence n'est pas supprimée : la supprimer
  emporterait aussi les filtres et les tris (décision du propriétaire, PLAN-019, lot 4). Les autres postes
  de l'utilisateur reçoivent cette disposition comme tout état modifié.
- Changement reçu : un état modifié ailleurs (un autre poste par la base, un autre onglet du même
  navigateur) est réappliqué à la grille affichée, sans recharger la page. Les lignes ne sont
  rechargées (retour à la première page) que si les filtres ou les tris ont changé. Une préférence
  supprimée ailleurs ramène la grille à sa déclaration.
- Prérendu : avec une base, l'état est lu dès le prérendu ; sans base, le navigateur n'est pas encore
  joignable et l'état est restauré au rendu interactif. La première requête d'une grille `Load` attend
  cette lecture.
- Migration depuis les versions antérieures à 1.8 : `IOmniDataGridStateStore`,
  `OmniLocalStorageDataGridStateStore` et le paramètre `StateStore` sont retirés. Une grille qui ne
  trouve pas sa préférence mais trouve dans `localStorage`, sous la `StateKey` seule, l'entrée qu'y
  écrivait l'ancien magasin par défaut (un objet portant `Filters`, `Sorts` et `ColumnWidths`) la reprend
  une fois comme préférence, puis supprime l'entrée. Une autre valeur rangée sous cette clé appartient à
  l'application et n'est pas touchée. Cette reprise n'a lieu que sans base (portée `local`) : la clé nue
  est commune à tous les utilisateurs du navigateur, et avec un `IOmniPreferenceStore` elle pourrait
  porter la grille d'un autre utilisateur ; l'entrée est alors laissée en place (voir
  [preferences.md](preferences.md), « Migration vers la 1.8.0 »). Un stockage refusé par le navigateur
  ne reprend rien, et la grille s'affiche. Un état tenu par un `IOmniDataGridStateStore` propre à
  l'application n'est pas repris : il se migre côté application vers son `IOmniPreferenceStore`, sous
  la clé `omni.grid.<StateKey>`.

## Réordonner, masquer et réinitialiser les colonnes

Trois options de la grille, toutes désactivées par défaut, rendent la disposition des colonnes au
lecteur ; avec `StateKey`, elle est gardée (voir plus haut) et suit l'utilisateur d'un poste à l'autre.

- `AllowColumnReorder` (`bool`, `false`) : un en-tête se glisse sur un autre à la souris. Au-delà de
  six pixels l'appui devient un glisser : l'en-tête glissé pâlit, un trait de la couleur d'accent sur le
  bord de début ou de fin de l'en-tête survolé marque où la colonne se posera (la moitié survolée
  décide), et le lâcher la place sans trier la colonne ; en deçà, l'appui reste un clic qui trie. Échap
  annule le glisser. La poignée de redimensionnement et le menu de filtre gardent leurs propres gestes.
  Une colonne figée ne se pose que parmi les figées, une autre parmi les autres : aucun trait n'apparaît
  sur un en-tête de l'autre groupe. Le geste vit dans `grid/reorder.js` (`attachReorder`,
  `detachReorder` d'`omni-grid.js`), par des classes seulement, sans attribut `style` ; .NET n'est appelé
  qu'au lâcher (`OnColumnDroppedAsync`). Chaque déplacement, au glisser comme au clavier, lève
  `OnColumnReorder` (`OmniDataGridColumnOrderChange` : `Key` de la colonne déplacée, `Order`, les clés
  de toutes les colonnes dans le nouvel ordre, masquées comprises) ; ni une réinitialisation ni un état
  reçu d'ailleurs ne le lèvent.
- `AllowColumnHide` (`bool`, `false`) : une case par colonne affiche ou masque la colonne. La dernière
  colonne visible garde sa case désactivée : une grille sans colonne n'aurait plus rien à montrer.
  `OmniDataGridColumn.Hideable` (`bool`, `true`) à `false` garde une colonne telle que déclarée
  (colonne d'actions, colonne qui nomme la ligne) : elle n'est pas listée quand le sélecteur ne fait que
  masquer, et sa case est désactivée quand il réordonne aussi.
- Le sélecteur de colonnes : dès que l'une des deux options est active, la barre d'en-tête (voir « Barres
  d'en-tête et de pied de la grille, export ») s'affiche, même sans `ShowHeaderBar`, et porte à sa fin un
  bouton à icône seule (`Columns`) nommé et titré « Choisir les colonnes ». Il ouvre un `OmniPopover` :
  une ligne par colonne dans l'ordre affiché, colonnes masquées comprises, avec la case (`OmniCheckBox`
  nommée par le titre de la colonne, ou par sa clé sans titre) puis, avec `AllowColumnReorder`, deux
  flèches « Déplacer … à gauche » et « Déplacer … à droite » : c'est le chemin du clavier et des
  technologies d'assistance pour réordonner. Une flèche qui sortirait de la liste ou franchirait la
  limite des colonnes figées est désactivée. Après un déplacement, le focus revient sur la même flèche
  de la colonne déplacée, ou sur l'autre quand celle-ci ne peut plus servir. Sans `AllowColumnHide`, la
  ligne montre le nom sans case. Le panneau tient dans un écran de 375 px ; pendant qu'il est ouvert, le
  cadre de la grille cesse de rogner son contenu, pour ne pas couper le panneau au pied d'une grille
  courte.
- Réinitialisation : `ResetLayoutAsync()` ramène l'ordre, la visibilité et les largeurs à la
  déclaration, garde les filtres et les tris, et enregistre l'état (voir « Enregistrement de l'état »).
  `ShowResetLayout` (`bool`, `false` : décision du propriétaire, l'action existe mais reste masquée par
  défaut) ajoute au pied du sélecteur l'action « Réinitialiser la disposition », qui fait la même chose ;
  un hôte qui ne l'affiche pas peut offrir la méthode ailleurs.
- Annonces : la grille tient une zone `role="status"` polie, présente dès que le sélecteur existe, qui
  dit chaque changement : « Colonne Montant déplacée en position 2 sur 5 » (place parmi les colonnes
  affichées ; pour une colonne masquée déplacée dans le sélecteur, sa place dans la liste), « Colonne
  Pays masquée », « Colonne Pays affichée », « Disposition des colonnes réinitialisée » (après l'action du
  sélecteur seulement : un appel de `ResetLayoutAsync()` par l'hôte n'annonce rien). Les textes sont
  dans les 24 langues (`GridColumnChooser`, `GridColumnMoveLeft`, `GridColumnMoveRight`,
  `GridColumnShown`, `GridColumnHidden`, `GridColumnMoved`, `GridResetLayout`, `GridLayoutReset`).
- L'en-tête et toutes les cellules d'une colonne sont rendus avec la clé de la colonne (`@key`) : une
  colonne déplacée emporte ses éléments et les composants de ses gabarits (un filtre en cours de saisie
  reste à sa colonne).

La démonstration « Grille, vue complète » de la vitrine montre les trois options, `Hideable="false"` sur
la référence et `ShowResetLayout`, avec une `StateKey` : la disposition survit au rechargement. La
sonde des scripts (`eng/scripts-probe/columns.mjs`, `OMNI_SCRIPTS_ONLY=columns`) le vérifie dans
Chromium : glisser, Échap, limite figée, clavier et focus, masquage, rechargement, réinitialisation,
375 px, sans violation CSP ni erreur de console.

## Ce qui n'est délibérément pas fourni

- `Style` : un attribut `style` violerait le contrat CSP. Le remplacement est `Class`, `Height`,
  `GridLines`, `Density` et les largeurs de colonnes.
- Le glisser-déposer d'en-têtes vers le panneau de regroupement : le regroupement se pilote par le
  bouton d'en-tête et par `Groups`.
- Un rendu de filtre au choix de l'hôte hors des deux emplacements fournis : la rangée de filtres, ou
  le menu d'en-tête (`ShowHeaderFilterMenu`) ; `FilterTemplate` remplace le contrôle, pas son cadre.
- Le glisser d'un en-tête au toucher : sur un écran tactile, un appui sur l'en-tête fait défiler le
  tableau ; le sélecteur de colonnes reste le chemin pour réordonner.
- Un événement pour le masquage d'une colonne : seul le déplacement est rapporté (`OnColumnReorder`) ;
  la visibilité choisie se lit dans l'état enregistré.

## Arbre : `OmniTree` et `OmniTreeItem`

`OmniTree<TValue>` est un arbre (`role="tree"`) d'`OmniTreeItem<TValue>`, à sélection simple ou multiple
(`aria-multiselectable`), la sélection étant liée par `Value`/`ValueChanged` ; `Label` (`string?`) le nomme.
Chaque entrée (`role="treeitem"`) déclare ses enfants dans son contenu ou les charge à la demande à sa
première ouverture (état de chargement annoncé, échec signalé et rapporté par `OnLoadError`), y compris
quand elle est ouverte d'emblée. Au clavier, Droite développe, Gauche
réduit, Entrée et Espace sélectionnent sans remonter à l'ancêtre ([accessibility-contract.md](contracts/accessibility-contract.md)).

`OmniTreeItem.LoadChildren` (`Func<CancellationToken, Task>?`) charge les enfants à la première ouverture de l'entrée, que l'hôte rend ensuite dans `ChildContent` ; une ouverture plus récente annule un chargement encore en cours.

### Ligne enrichie

`OmniTreeItem.TextContent` remplace le texte de la ligne par un fragment (icône, libellé mis en forme,
mention), indépendamment des éléments enfants de `ChildContent`. Il est rendu dans le bouton de la
ligne, donc sans élément interactif, et `Text` devient alors le nom accessible de la ligne. Sans lui,
la ligne affiche `Text` comme avant.

### Tout ouvrir, tout fermer

`ExpandAllAsync()` ouvre chaque entrée qui a des enfants, puis celles qu'elle révèle à mesure qu'elles se
rendent (une branche à chargement à la demande charge ses enfants) ; `CollapseAllAsync()` les ferme toutes.
Chaque entrée le rapporte par son `ExpandedChanged`, si bien qu'un `@bind-Expanded` de l'hôte suit.

### Glisser-déposer

`AllowDragDrop` (désactivé par défaut) laisse glisser une entrée sur une autre à la souris (glisser-déposer
HTML) ; l'arbre ne se réorganise pas lui-même : `OnItemDropped` reçoit un `OmniTreeDropEventArgs<TValue>`
(`Dragged`, la valeur glissée, et `Target`, celle de l'entrée d'arrivée), et l'hôte fait de la cible le
nouveau parent dans ses données, puis rend l'arbre de nouveau (un `@key` par entrée garde l'état de chacune).

```razor
<OmniTree TValue="string" AllowDragDrop="true" CanDrag="n => n.Length > 1" CanDrop="(n, cible) => cible != n"
          OnItemDropped="Ranger">
```

- **Règles.** `CanDrag` dit si une entrée se glisse (toutes les entrées actives par défaut) ; `CanDrop` dit si
  l'entrée glissée peut arriver sur une cible. Jamais sur elle-même ni dans sa propre branche : le paquet
  l'interdit avant de demander à l'hôte. Une cible refusée n'est pas mise en évidence et le navigateur montre
  le dépôt interdit (le `dragover` n'est pas annulé).
- **Retour visuel.** La ligne glissée s'estompe (`omni-tree__row--dragging`), la ligne qui accepte l'entrée
  sous le pointeur prend un fond teinté et un contour en pointillé (`omni-tree__row--drop-target`), la
  racine porte `omni-tree--dragging` pendant le glisser. Aucun attribut `style`.
- **Script et CSP.** `omni-tree.js` (`attachTreeDrag`, `detachTreeDrag`) ne fait que donner au glisser la
  donnée que certains navigateurs exigent pour le commencer ; l'état reste en .NET, sans gestionnaire en
  ligne.
- **Limites.** Pas d'équivalent clavier : un hôte qui en a besoin propose sa propre action « Déplacer vers ».
  Pas de dépôt entre deux entrées ni à la racine : le dépôt se fait sur une entrée, qui devient le parent.
  Vérifié dans Chromium ; un glisser qui commence sur le bouton de la ligne n'est pas garanti dans tous les
  navigateurs.

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
- **Taille.** Une feuille compte au plus `MaxRows` (10 000) lignes, `MaxColumns` (500, soit jusqu'à la colonne
  `SF`) colonnes et `MaxCells` (250 000) cellules : chaque modification recopie les cellules et le composant
  les dessine toutes, sans virtualisation. `Create`, `FromRows`, `WithInput` (une adresse lointaine comme
  `A9999999` n'alloue plus les lignes intermédiaires), `AddRow` et `AddColumn` lèvent une
  `ArgumentOutOfRangeException` au-delà ; `FromJson` lève la `JsonException` documentée. Les boutons d'ajout
  du composant se désactivent à la limite.
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
occurrence d'un texte et passe d'une ligne qui le contient à la suivante (le compteur compte ces lignes) sans masquer de ligne ; `Wrap` fait passer les lignes
longues à la ligne. Sans `Wrap`, le journal défile en largeur et le numéro et l'heure de chaque ligne,
regroupés dans une gouttière (`omni-log-viewer__gutter`), restent collés à son début, opaques, sur la
teinte de leur ligne. Le composant
n'ouvre aucune connexion : l'hôte lui passe les lignes, en ajout seul (une ligne remplacée avant la
dernière position déjà vue n'est pas détectée ; une liste plus courte ou dont cette ligne a changé repart
de zéro).

Paramètres du journal. `Lines` (`IReadOnlyList<OmniLogLine>`, obligatoire) donne les lignes, de la plus ancienne à la plus récente. `Follow` (vrai par défaut) dit si la vue suit la dernière ligne : le lecteur qui remonte la coupe, un retour au bas ou le bouton la rétablit, et `FollowChanged` rapporte les deux. `MinimumLevelChanged` est levé quand le filtre de niveau change et `SearchTextChanged` quand le texte cherché change, si bien que `MinimumLevel` et `SearchText` se lient par `@bind`. `ShowLineNumbers` (vrai par défaut) montre le numéro de chaque ligne, compté dans tout le journal même quand un filtre masque des lignes ; `ShowTimestamps` (vrai par défaut) montre avant le texte l'heure de la ligne (`OmniLogLine.Timestamp`), au format `TimestampFormat` (`HH:mm:ss` par défaut) et dans le fuseau `TimeZone` (le fuseau local par défaut) ; `ShowLevels` (faux par défaut) écrit le nom du niveau de chaque ligne, les avertissements et les erreurs restant teintés et nommés aux lecteurs d'écran. AnsiMode (OmniAnsiMode, Render par défaut) lit les séquences ANSI d'une ligne (section suivante).

### Séquences ANSI : `AnsiMode`, `OmniAnsiText`, `OmniAnsi`

Un journal de programme en ligne de commande porte des séquences d'échappement (`ESC [ 32 m`) : sans traitement, le navigateur
dessine l'octet d'échappement comme rien et laisse `[32m` à l'écran. `AnsiMode` (`OmniAnsiMode`, `Render` par défaut) dit ce qu'`OmniLogViewer` en fait ; la ligne
n'est jamais modifiée, seul l'affichage change :

| `AnsiMode` | Affichage |
| --- | --- |
| `Render` | Les couleurs, le gras, le texte atténué, l'italique et le souligné sont dessinés ; aucune séquence n'est visible. |
| `Hide` | Les séquences sont retirées sans rien dessiner : le texte seul. |
| `Raw` | La ligne telle qu'écrite, séquences comprises (l'octet d'échappement lui-même ne se dessine pas, le reste de la séquence reste visible). |

La recherche (`ShowSearch`), le marquage de ses occurrences et son compteur portent sur le texte affiché : sans les séquences en `Render`
et en `Hide` (une occurrence peut chevaucher deux plages de couleurs différentes, elle est marquée dans les deux), sur la ligne brute en
`Raw` ; changer `AnsiMode` relance la recherche. Les numéros de ligne ne changent pas. `OmniLogViewer` n'offre ni export ni copie : l'hôte
qui exporte le journal écrit `OmniLogLine.Text` tel qu'il l'a stocké, séquences comprises, et la sélection d'un texte à l'écran (`Render`,
`Hide`) se copie sans séquence.

`OmniAnsiText` (`Text`, `Mode`, `OmniAnsiMode`, `Render` par défaut) fait de même pour une ligne hors d'un journal, une cellule de grille par exemple
(`<OmniAnsiText Text="@row.Message" />`). Il écrit un `span` (`omni-ansi`) ; la chasse aux espaces et le retour à la ligne sont ceux de son hôte.
`OmniAnsi` est la même lecture sans composant : `OmniAnsi.Parse(texte)` rend les plages (`OmniAnsiSegment` : `Text`, `Bold`, `Dim`, `Italic`,
`Underline`, `Foreground`, `Background`, les couleurs étant un indice de 0 à 15 ou `null`), `OmniAnsi.Strip(texte)` le texte seul.

Ce qui est reconnu, par un lecteur écrit à la main (aucune expression régulière, donc aucun retour sur trace ; une séquence de plus de 256 caractères, 4096
pour une chaîne, n'en est pas une) :

- Les séquences de mise en forme `ESC [ … m` (et l'introducteur d'un seul caractère U+009B) : réinitialisation (0), gras (1), atténué (2), italique (3),
  souligné (4, `4:0` l'éteint), leurs fins (22, 23, 24), les seize couleurs de texte (30 à 37, 90 à 97) et de fond (40 à 47, 100 à 107) et leurs défauts (39, 49).
  Les couleurs étendues (`38;5;n`, `38;2;r;g;b`, avec des points-virgules ou des deux-points) sont lues en entier puis ramenées à la plus proche des seize, pour que
  la teinte reste garantie lisible ; un code incomplet est consommé sans qu'aucun chiffre ne s'affiche. Les autres codes (clignotement, vidéo inverse, barré…) sont lus et ignorés.
- Toute autre séquence de contrôle (déplacement du curseur, effacement, titre de fenêtre, lien hypertexte, chaîne de contrôle d'appareil) est retirée à l'affichage, ainsi qu'un échappement isolé.
  Le texte hors séquences est conservé caractère pour caractère.
- Un code entre crochets **sans** son octet d'échappement (`[90m`) est du texte ordinaire et reste affiché : il ne se distingue pas d'un mot entre crochets du journal, et
  rien ne doit se perdre d'une ligne. Un hôte dont le transport supprime l'octet d'échappement le rétablit avant de stocker la ligne.
- Une séquence mal formée perd son introducteur et garde le reste comme texte ; une séquence coupée par la fin de la ligne est retirée en entier.

Couleurs. Les seize couleurs sont les jetons `--omni-ansi-0` à `--omni-ansi-15` (0 à 7 : noir, rouge, vert, jaune, bleu, magenta, cyan, blanc ; 8 à 15 : leurs versions vives),
générés avec chaque palette dans les deux modes, avec des valeurs propres au clair et au sombre : en clair le « noir » est l'encre forte et le « blanc » un gris
foncé, en sombre l'inverse, une couleur vive est toujours plus claire que sa normale. Chacune est tenue à 4,5:1 au moins sur la surface du journal (fond de grille, de carte ou de page), sur la
surface teintée de ce qu'un fond de couleur y ajoute, sur la teinte d'une ligne d'avertissement ou d'erreur et sur celle d'une occurrence de recherche, dans chaque thème et chaque palette
(`AnsiThemeContrastTests`). Règle pour un fond de couleur : `omni-ansi-bg-N` peint 12 % de la couleur sur la surface ; le texte par défaut et chacune des seize couleurs de texte
y gardent 4,5:1 (mêmes tests, contre le pire fond possible). Un texte atténué prend la couleur de texte atténuée du thème, jamais une opacité qui ferait baisser le rapport.
Un hôte qui redéfinit un de ces jetons répond lui-même de son contraste.
## Export Markdown : `OmniMarkdownExportButton`

Une grille exporte par ses propres barres (section suivante). `OmniMarkdownExportButton<TItem>`
sert aux exports hors grille : une liste de cartes, un tableau de bord, un rapport. C'est un bouton qui
télécharge des lignes en fichier Markdown, pour une lecture par une IA : toutes les lignes annoncées, lues par le fournisseur
de pages de l'export, et non la seule page affichée. Il est occupé pendant la lecture, et une page en échec ne produit aucun fichier.

Un seul modèle d'export sert à la grille et à ce bouton, et le Markdown n'y est qu'un format parmi les autres
(`OmniTableExportFormat`). `Export` décrit la source par un `OmniTableExport<TItem>` : `Title`, `Columns`
(des `OmniTableExportColumn<TItem>` : titre et texte de la cellule), `LoadPage` (une `OmniTableExportPageRequest`
numérotée à partir de 1 qui donne `Skip`), `Fields` (des `OmniTableExportField`, comme `ExportFields` de la grille),
`RowLimit`, `PageSize`, `RowKey` (une ligne lue deux fois n'est écrite qu'une fois) et `EmptyText`. Le service
`OmniTableExporter` (enregistré par `AddOmniEuropeBlazor`) en tire un `OmniTableExportDocument` par `ReadAsync`,
que `RenderAsync` écrit dans n'importe quel format : un hôte qui veut aussi du CSV ou de l'Excel de cette source
ne lit pas ses pages deux fois. `ToMarkdown(document)` donne le texte Markdown, `EscapeCell` et `EscapeInline`
l'échappement d'une cellule et d'une ligne d'en-tête.
`OnExport` reçoit l'`OmniTableExportDocument` produit une fois le fichier remis (une exception de son gestionnaire remonte, ce n'est pas un échec de l'export), `OnExportError` l'exception d'un échec ; `Text` (`string?`) remplace
le libellé localisé du bouton. Quand la source n'annonce pas de total et que la lecture s'arrête sur une
page pleine à la limite de lignes, le document écrit « N sur au moins M » et une note le dit :
`OmniTableExportDocument.TotalIsLowerBound` est alors vrai et `IsComplete` faux. Le fichier se nomme
`{FileName}-{yyyy-MM-dd-HHmm}.md`, heure UTC (`FileName` vaut `export` par défaut).

`OmniMarkdownExportButton.Variant` (`OmniButtonVariant`, `Secondary` par défaut) règle l'emphase du bouton, comme celle d'`OmniButton`.

## Barres d'en-tête et de pied de la grille, export

`ShowHeaderBar` et `ShowFooterBar` (`bool`, `false`) ajoutent une barre au-dessus ou au-dessous des lignes,
dans le cadre du tableau : la bordure et le fond qui entouraient le tableau entourent alors barres et lignes
ensemble, chaque barre séparée des lignes par un trait. Sans barre, la grille se rend comme avant. Chaque
barre reçoit le contenu de l'hôte (`HeaderBarContent`, `FooterBarContent`, `RenderFragment`) et les
boutons d'export, au début de la barre par défaut ; `HeaderBarExport` et `FooterBarExport`
(`OmniDataGridBarExport` : `Start` par défaut, `End`, `None`) les mettent à la fin ou les retirent, le
contenu prenant le reste. Deux barres affichées portent donc chacune les boutons, sauf `None` sur l'une
d'elles. Une barre masquée ne porte rien.

Les formats se choisissent un par un : `ExportMarkdown`, `ExportCsv`, `ExportExcel`, `ExportPdf` (`bool`,
`true` par défaut). `Markdown`, `Csv` et `Excel` sont écrits par le paquet, sans dépendance ; `Pdf` l'est
par l'hôte, qui enregistre un `IOmniTableExportRenderer` (un format que personne n'écrit n'a pas de bouton ;
un moteur de l'hôte qui écrit l'un des trois premiers remplace celui du paquet). Chaque bouton est une icône
seule, plus petite qu'un bouton `Small` et qui suit la densité, nommée et titrée « Exporter en Markdown »
(`FileMd`, `FileCsv`, `FileXls`, `FilePdf`) ; le groupe se nomme « Export du tableau ». Les boutons sont
en variante `Secondary` (gris neutre), une exportation étant une autre action de sa zone ; `ExportVariants`
en donne une autre par format (le Markdown en `Primary`, par exemple).
`ShowExportButtonText` (`bool`, `false`) écrit le nom du format à côté de l'icône (« Markdown », « CSV »,
« Excel », « PDF »), pour une barre large où l'icône seule se lit mal ; le nom accessible et le titre
restent « Exporter en Markdown », qui contient le texte visible (WCAG 2.5.3), et le bouton d'annulation
affiche de même « Annuler ».
`BarButtonSize` (`OmniControlSize?`, `null`) règle la taille des boutons des barres : boutons d'export,
leur annulation et déclencheur du sélecteur de colonnes. Nul, ils restent compacts, plus petits qu'un bouton
`Small` (classe `omni-data-grid__bar-button--compact`), pour une barre basse ; une taille en fait des
`OmniButton` de cette taille, comme les autres boutons de la page (`OmniControlSize.Medium` pour la taille
par défaut des boutons).

```razor
<OmniDataGrid TItem="Commande" Load="ChargerAsync" KeyOf="@(c => c.Id)"
              ShowFooterBar="true" FooterBarExport="OmniDataGridBarExport.End"
              ExportCsv="false" ExportExcel="false" ExportPdf="false"
              ExportFileName="commandes" ExportTitle="Commandes"
              ExportFields="@(new OmniTableExportField[] { new("Application", "Boutique") })">
    <FooterBarContent><span>Données à J-1</span></FooterBarContent>
    <Columns>
        <OmniDataGridColumn TItem="Commande" Property="Reference" Title="Référence" />
        <OmniDataGridColumn TItem="Commande" Title="Statut" ExportValue="@(c => Libelle(c.Statut))">
            <Template Context="c"><OmniBadge>@Libelle(c.Statut)</OmniBadge></Template>
        </OmniDataGridColumn>
    </Columns>
</OmniDataGrid>
```

- **Anciens paramètres retirés en 1.9.0.** ExportFormats et ExportPosition n'existent plus : les barres (ShowHeaderBar,
  ShowFooterBar) et les quatre interrupteurs de formats ci-dessus les remplacent.

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
- **Excel.** Un classeur Office Open XML (`.xlsx`) écrit par la seule bibliothèque de base
  (`System.IO.Compression` et un écrivain XML), donc aussi sous WebAssembly, sans paquet NuGet de plus
  (`OmniTableExporter.ToXlsx`). Une feuille nommée d'après le titre (31 caractères au plus, sans les
  caractères `[ ] : * ? / \` qu'un nom de feuille refuse), une ligne d'en-tête en gras, figée au défilement
  et porteuse d'un filtre automatique, puis une ligne par ligne exportée : un nombre en nombre, une date en
  date (format court de la machine, ou `yyyy-mm-dd hh:mm` quand elle a une heure), un booléen en booléen, le
  reste en texte. Chaque colonne est aussi large que son plus long texte, entre 8 et 60 caractères. Aucune
  cellule ne porte de formule ; un texte qui commence comme une formule (`=`, `+`, `-`, `@`, tabulation,
  retour chariot) est marqué comme texte (`quotePrefix`) : le modifier dans le tableur ne l'exécute pas. Les
  caractères de contrôle interdits en XML sont retirés. Comme en CSV, le titre et les lignes d'en-tête du
  document ne sont pas écrits dans la feuille.
- **Fichier.** `{ExportFileName}-{yyyy-MM-dd-HHmm}.{extension}`, heure UTC (`shop-logs-2026-10-01-0840.md`) ; sans `ExportFileName`, le nom vient d'`ExportTitle`, puis de `Caption`, en minuscules sans accents et avec des traits d'union (`export` à défaut). `OnExport` reçoit le document
  une fois le fichier remis au navigateur. Un export coupé par la limite le dit dans la barre ; un échec
  y affiche « L'export a échoué. », ne produit aucun fichier et passe l'exception à `OnExportError`.
  Une exception levée par le gestionnaire `OnExport` n'est pas un échec de l'export (le fichier est
  déjà remis) : elle remonte comme celle de tout gestionnaire d'événement.
- **Hors grille.** `OmniTableExporter` (service enregistré par `AddOmniEuropeBlazor`) écrit un document
  dans un format : `Supports`, `RenderAsync`, `ToMarkdown`, `ToCsv`, `ToXlsx` (et `XlsxContentType`).
  `OmniTableExportDownloader` (même enregistrement) écrit un tableau que l'hôte construit lui-même, un
  compte de résultat ou un bilan, et le remet au navigateur : `DownloadAsync(document, format, fileName)`
  télécharge `{fileName}.{extension}` et rend le fichier.

```csharp
@inject OmniTableExportDownloader Telechargement

var document = new OmniTableExportDocument
{
    Title = "Compte de résultat 2026",
    Columns = [new("Poste", OmniTableExportValueKind.Text), new("2026", OmniTableExportValueKind.Number)],
    Rows = Postes.Select(p => (IReadOnlyList<OmniTableExportCell>)[new(p.Libelle), new(p.Montant.ToString("n2")) { Number = p.Montant }]).ToArray()
};
await Telechargement.DownloadAsync(document, OmniTableExportFormat.Excel, "compte-de-resultat-2026");
```

```csharp
// Hôte : le format que le paquet n'écrit pas (et, ici, son propre classeur au lieu de celui du paquet).
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
