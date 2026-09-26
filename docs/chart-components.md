# Graphiques (famille Charts)

Les graphiques sont dessinés en SVG dans une boîte de 100 sur 100, sans script ni attribut `style`.
Toutes les parties d'un `OmniChart` (séries, axes, lignes de grille, légende, titres d'axe) lisent
leurs coordonnées dans un même contexte : elles s'alignent par construction.

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
  écran étroit, et le tracé reprend toute la largeur. `ColorIndexes` donne à chaque entrée la teinte
  de la série qu'elle nomme ; sans lui, les entrées prennent 0, 1, 2 dans l'ordre.
- `OmniCategoryAxis` n'écrit que les libellés qui tiennent sans se chevaucher : quand tous ne
  tiennent pas, un sur N, le premier et le dernier toujours gardés. La largeur d'un libellé est
  estimée à 1,7 unité par caractère (0,57 em), ce qui laisse un peu de marge. Le texte de survol de
  chaque colonne, barre ou marqueur sans `Label` propre nomme sa catégorie et sa valeur
  (« 05/09 · 123 »), et le `DataTable` du graphique reste l'alternative accessible complète.
- Des colonnes ou des barres découpent l'axe des catégories en bandes, une par catégorie, et chaque
  libellé se place au milieu de sa bande. Plusieurs `OmniColumnSeries` se rangent côte à côte dans la
  bande ; toutes les séries empilées y partagent une place. Une ligne tracée avec des colonnes passe
  au-dessus de leurs centres.
- Une `OmniBarSeries` tourne le graphique : les catégories descendent à gauche, les valeurs courent en
  bas, les lignes de grille deviennent verticales.
- `OmniGridLines` trace `Count` lignes régulières sur la hauteur du tracé : avec autant de lignes que
  de graduations, chaque ligne passe par une graduation.
- Les traits (lignes, aires, axes, grille, contours des marqueurs) ont une épaisseur en pixels
  d'écran (`vector-effect: non-scaling-stroke`) : un graphique petit dans une carte ou large sur une
  page garde le même trait. Une `OmniLineSeries` n'est jamais remplie, et un marqueur est un cercle de
  la couleur de surface cerclé de la teinte de sa série.

- `OmniValueAxis.Automatic` cale l'axe sur les séries : bornes arrondies vers l'extérieur (pas de 1, 2, 2,5 ou 5
  fois une puissance de dix) pour `TickCount` graduations, de zéro (ou de la plus basse valeur) à la plus
  haute valeur, piles comprises. Sans lui, un axe garde `Minimum` et `Maximum` (0 et 100 par défaut) et un
  graphique sans axe se cale sur les données sans graduations écrites.

## Jauge

`OmniArcGauge` dessine un demi-cercle qui part de son extrémité gauche et passe par le haut ;
`OmniArcGaugeScale` y inscrit ses bornes sous les deux extrémités, et chaque `OmniArcGaugeScaleValue`
son arc et sa valeur au centre, en gras. Une valeur sans `Minimum` ni `Maximum` prend ceux de son
échelle ; une valeur qui en fixe garde les siens. La valeur est bornée : au-delà du maximum l'arc est
plein, en deçà du minimum il est vide.

## Secteurs

Un secteur unique est un disque (ou un anneau) entier, tracé en deux demi-arcs : un arc seul ne peut
pas finir là où il commence, et laissait une encoche.

## Preuves

`ChartLayoutTests` et `ChartComponentTests` fixent la géométrie (jauge, bornes héritées, colonnes
côte à côte, barres tournées, légende, disque entier) et la règle de feuille qui garde les lignes
vides. Le rendu des quatre pages de graphiques du module OE Démo d'Atlas a été contrôlé dans
Chromium sur des pages statiques produites par bUnit avec la feuille réelle, en clair, et en sombre
pour les courbes et les jauges.
