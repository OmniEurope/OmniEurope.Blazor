# Graphiques (famille Charts)

Les graphiques sont dessinés en SVG dans une boîte de 100 sur 100, sans script ni attribut `style`.
Toutes les parties d'un `OmniChart` (séries, axes, lignes de grille, légende, titres d'axe) lisent
leurs coordonnées dans un même contexte : elles s'alignent par construction.

## Disposition

- La zone de tracé va de 14 à 96 en largeur et de 4 à 86 en hauteur. Les valeurs de l'axe vertical
  se placent à sa gauche, centrées sur leur graduation ; les catégories dessous ; un titre d'axe
  horizontal en bas, un titre vertical tout à gauche, tourné.
- Une `OmniLegend` occupe la colonne à droite du tracé, que le graphique rétrécit à 76 tant qu'une
  légende est présente : une entrée ne couvre plus un point. `ColorIndexes` donne à chaque entrée la
  teinte de la série qu'elle nomme ; sans lui, les entrées prennent 0, 1, 2 dans l'ordre.
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
