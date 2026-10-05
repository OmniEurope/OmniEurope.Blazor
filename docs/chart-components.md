# Graphiques (famille Charts)

Les graphiques sont dessinés en SVG dans une boîte de 100 sur 100, sans script ni attribut `style`.
Toutes les parties d'un `OmniChart` (séries, axes, lignes de grille, légende, titres d'axe) lisent
leurs coordonnées dans un même contexte : elles s'alignent par construction.

## Parties d'un graphique

| Composant | Rôle |
| --- | --- |
| `OmniChart` | Conteneur SVG : titre, description (`Description`, `string?`, posée en `desc` et `aria-describedby` seulement quand elle est donnée), rapport largeur sur hauteur ; toutes les parties y lisent leurs coordonnées. `DataTableContent` fournit l'alternative en tableau ; sans lui, un tableau des données masqué visuellement est généré depuis les séries. `SharedTooltip` donne un texte de survol par catégorie (voir Disposition). |
| `OmniCategoryAxis`, `OmniValueAxis` | Axe des catégories et axe des valeurs (bornes fixes ou automatiques), graduations et libellés. |
| `OmniAxisTitle` | Titre d'un axe, horizontal en bas ou vertical à gauche, tourné. |
| `OmniGridLines` | Lignes de grille du tracé. |
| `OmniLineSeries`, `OmniAreaSeries` | Série en courbe, ou en aire, empilable (`Stacked`) ; `Dashed` trace la courbe, ou le contour de l'aire, en tirets. |
| `OmniColumnSeries` | Colonnes verticales groupées par catégorie, empilables (`Stacked`) ; `Horizontal="true"` en fait des barres horizontales sur axes tournés. |
| `OmniPieSeries` | Secteurs d'un disque, ou d'un anneau (`Donut`). |
| `OmniMarkers` | Points marqués sur les valeurs d'une série. |
| `OmniSeriesDataLabels` | Valeurs écrites sur les points d'une série, avec leur format (`FormatValue`). |
| `OmniLegend` | Légende hors du tracé, à droite, dessous ou dessus. |
| `OmniArcGauge`, `OmniArcGaugeScale`, `OmniArcGaugeScaleValue` | Jauge en demi-cercle, son échelle et la valeur qu'elle montre (`FormatValue`) ; le nom accessible de la jauge porte sa valeur. |

## Disposition

- La zone de tracé va de 14 à 96 en largeur et de 4 à 86 en hauteur. Les valeurs de l'axe vertical
  se placent à sa gauche, centrées sur leur graduation ; les catégories dessous ; un titre d'axe
  horizontal en bas, un titre vertical tout à gauche, tourné.
- `AspectRatio` non posé, le graphique choisit : 2 pour une série temporelle (plus de 12 catégories,
  libellés ou points d'une série, sur un graphique vertical), 1 (carré) sinon. Toute valeur posée,
  1 compris, est respectée. Un dessin plus large que haut porte `omni-chart__svg--wide` : sous
  40 rem de fenêtre, le texte de ses axes passe de 3 à 4,5 unités, et l'axe des catégories est
  éclairci pour cette taille.
- `OmniLegend.Position` vaut `Auto` par défaut : la légende occupe la colonne à droite du tracé (24
  de large, le tracé s'arrête à 76 dans un carré) tant que son entrée la plus longue y tient ou tient
  dans un cinquième du dessin ; au-delà, elle passe sous le graphique. `Right` la garde à droite et
  élargit la colonne à l'entrée la plus longue, jusqu'à 40 % du dessin. `Bottom` (ou `Auto` avec des
  entrées longues) la dessine sous le SVG en liste HTML (`ul.omni-chart__legend--below`) : elle
  garde la taille de texte de la page au lieu de rétrécir avec le dessin, passe à la ligne sur un
  écran étroit, et le tracé reprend toute la largeur. `Top` dessine la même liste au-dessus du SVG
  (`ul.omni-chart__legend--above`), le tracé gardant lui aussi toute la largeur. Les entrées se construisent depuis les séries du graphique,
  chacune avec la teinte de la série qu'elle nomme ; `Items` les remplace. `Label` (`string?`) nomme la
  légende.
- `OmniCategoryAxis` n'écrit que les libellés qui tiennent sans se chevaucher : quand tous ne
  tiennent pas, un sur N, le premier et le dernier toujours gardés. La largeur d'un libellé est
  estimée à 1,7 unité par caractère (0,57 em), ce qui laisse un peu de marge. Le texte de survol de
  chaque colonne, barre ou marqueur sans `Label` propre nomme sa catégorie et sa valeur
  (« 05/09 · 123 »), et le tableau de données du graphique (`DataTableContent`, ou celui généré) reste
  l'alternative accessible complète. Les nombres (graduations, valeurs, texte de survol) suivent la culture
  courante, et `FormatValue` d'`OmniValueAxis`, d'`OmniSeriesDataLabels` et d'`OmniArcGaugeScaleValue`
  remplace leur texte.
- `OmniChart.SharedTooltip` remplace ces textes par point par un texte par catégorie : une bande
  transparente par catégorie (`omni-chart__hover-band`), sur toute la hauteur du tracé (toute sa
  largeur pour des barres), dessinée après les séries pour recevoir le pointeur partout, porte un
  `title` SVG de la forme « mars », puis « Ventes · 12 » et « Objectif · 15 », une ligne par série dans
  leur ordre (une série sans titre prend le nom de repli du tableau, « Série 2 »). Les valeurs y sont
  écrites par le `FormatValue` d'`OmniValueAxis`, sinon dans la culture courante. Les catégories se
  comptent par rang, comme pour l'axe et le tableau : la bande `i` couvre les points de rang `i` ;
  avec des colonnes ou des barres elle est exactement leur bande, avec des courbes seules elle va
  d'un point à mi-chemin des points voisins. Les bandes sont `aria-hidden`, le tableau de données
  restant l'alternative accessible ; la bande survolée s'assombrit légèrement. Le texte apparaît par
  l'infobulle native du navigateur, sans script ni style inline.
- Des colonnes ou des barres découpent l'axe des catégories en bandes, une par catégorie, et chaque
  libellé se place au milieu de sa bande. Plusieurs `OmniColumnSeries` se rangent côte à côte dans la
  bande ; toutes les séries empilées y partagent une place. Une ligne tracée avec des colonnes passe
  au-dessus de leurs centres.
- Une `OmniColumnSeries Horizontal="true"` tourne le graphique : les catégories descendent à gauche, les
  valeurs courent en bas, les lignes de grille deviennent verticales ; `Stacked` les empile comme des
  colonnes. Les couleurs de série 5 à 7 sont générées pour chaque palette, à distance de son accent.
- `OmniGridLines` trace `Count` lignes régulières sur la hauteur du tracé : avec autant de lignes que
  de graduations, chaque ligne passe par une graduation.
- Les traits (lignes, aires, axes, grille, contours des marqueurs) ont une épaisseur en pixels
  d'écran (`vector-effect: non-scaling-stroke`) : un graphique petit dans une carte ou large sur une
  page garde le même trait. Une `OmniLineSeries` n'est jamais remplie, et un marqueur est un cercle de
  la couleur de surface cerclé de la teinte de sa série.
- `Dashed` d'`OmniLineSeries` et d'`OmniAreaSeries` trace la courbe, ou le contour de l'aire, en tirets
  (`omni-chart__line--dashed` : 8 pixels, 7 d'écart, les bouts arrondis allongeant chaque tiret ;
  `omni-chart__area--dashed` : 6 et 4) ; le remplissage d'une aire ne change pas. Le trait restant non
  mis à l'échelle, les tirets se mesurent aussi en pixels d'écran et gardent leur longueur à toute
  taille du dessin.

- `OmniValueAxis.Automatic` cale l'axe sur les séries : bornes arrondies vers l'extérieur (pas de 1, 2, 2,5 ou 5
  fois une puissance de dix) pour `TickCount` graduations, de zéro (ou de la plus basse valeur) à la plus
  haute valeur, piles comprises. Sans lui, un axe garde `Minimum` et `Maximum` (0 et 100 par défaut) et un
  graphique sans axe se cale sur les données sans graduations écrites.

## Jauge

`OmniArcGauge` dessine un demi-cercle qui part de son extrémité gauche et passe par le haut ;
`OmniArcGaugeScale` y inscrit ses bornes sous les deux extrémités, et chaque `OmniArcGaugeScaleValue`
son arc et sa valeur au centre, en gras. Une valeur prend les bornes de son échelle (0 à 100 hors
d'une échelle). La valeur est bornée : au-delà du maximum l'arc est
plein, en deçà du minimum il est vide.

## Secteurs

Un secteur unique est un disque (ou un anneau) entier, tracé en deux demi-arcs : un arc seul ne peut
pas finir là où il commence, et laissait une encoche.

## Preuves

`ChartLayoutTests` et `ChartComponentTests` fixent la géométrie (jauge, bornes héritées, colonnes
côte à côte, barres tournées, légende, disque entier) et la règle de feuille qui garde les lignes
vides. Le rendu des quatre pages de graphiques du module OE Démo d'une application cliente a été contrôlé dans
Chromium sur des pages statiques produites par bUnit avec la feuille réelle, en clair, et en sombre
pour les courbes et les jauges.

`ChartSeriesOptionsTests` fixe les tirets (classes et règles de feuille), les bandes du texte de
survol partagé (une par catégorie, dernières du dessin, `aria-hidden`, géométrie en colonnes, en
courbes seules et en barres, texte et `FormatValue`) et la légende `Top`. Contrôlé dans Chromium sur
une page statique produite par bUnit avec la feuille réelle, servie sous CSP stricte (console vide) :
tirets de 8/7 et 6/4 pixels, trait non mis à l'échelle, remplissage de l'aire gardé, légende avant le
dessin, et le point au centre d'une colonne touche la bande de sa catégorie (dont le `title` porte les
trois séries) qui s'assombrit au survol. L'infobulle native elle-même n'est pas capturable et n'a pas
été vue à l'écran.
