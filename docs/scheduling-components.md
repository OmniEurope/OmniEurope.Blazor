# Planification (famille Scheduling)

La famille Scheduling réunit ce qui place des éléments dans le temps : l'agenda (`OmniScheduler` et ses
trois vues), la frise `OmniTimeline`, le diagramme de Gantt `OmniGantt` et le déroulement d'étapes
`OmniStepTimeline`. Tout est dessiné en HTML et en SVG ; aucune position n'est posée par un attribut
`style` ni par un script : la frise se place par des classes, le Gantt et le déroulement par des
attributs géométriques SVG.

## Frise : `OmniTimeline`

| Paramètre | Rôle |
| --- | --- |
| `Layout` | `End` (défaut) : la ligne court sur le bord de début, les entrées après elle. `Start` : la ligne sur le bord de fin, les entrées avant elle, alignées contre elle. `Alternate` : la ligne au milieu, les entrées de part et d'autre, chacune son tour. |
| `FirstSide` | En `Alternate` seulement, le côté de la première entrée (`Start` ou `End`, défaut `End`) ; la suivante prend l'autre. Ignoré par les deux autres dispositions. |

Les côtés suivent le sens de lecture : `Start` est à gauche en français, à droite dans une langue lue
de droite à gauche. L'alternance est tirée du rang de l'entrée dans la liste par la feuille de style,
si bien qu'une entrée insérée au milieu décale les suivantes sans rendu supplémentaire. La frise
alternée est son propre conteneur (`container-type: inline-size`) : plus étroite que 30rem, elle
empile ses entrées après la ligne, quelle que soit la largeur de la fenêtre.

## Gantt : `OmniGantt`

```razor
<OmniGantt Tasks="Taches" @bind-Scale="Echelle" TaskClicked="Choisir" SelectedTaskId="@Choisie" />
```

Une tâche est un `OmniGanttTask` : `Id`, `Title`, `Start` et `End` (`DateOnly`, le dernier jour
inclus), `Progress` (0 à 1), `Group`, `DependsOn` (identifiants des tâches à finir avant elle) et
`ColorIndex` (rang dans la palette des graphiques).

| Paramètre | Rôle |
| --- | --- |
| `Tasks` | Les tâches, dans l'ordre des lignes. |
| `Scale` / `ScaleChanged` | Zoom : `Day` (une colonne par jour, fins de semaine grisées), `Week` (défaut, semaines ISO, du lundi) ou `Month`. |
| `ShowScalePicker` | Le choix jour, semaine, mois au-dessus du graphique ; sans liaison, le graphique garde le zoom choisi. |
| `ShowGroups` | Range les tâches sous leur groupe, chacun avec une barre qui couvre ses tâches ; les tâches sans groupe viennent d'abord. |
| `ShowDependencies` | Une flèche de la fin de chaque tâche attendue au début de la tâche qui l'attend ; quand la seconde commence trop tôt, la flèche revient par l'intervalle entre les deux lignes. Un identifiant inconnu est ignoré. |
| `ShowToday`, `Today` | Un trait pointillé au milieu de la colonne du jour ; `Today` nul prend la date courante. |
| `TaskClicked`, `SelectedTaskId` | La tâche activée par clic, Entrée ou Espace ; celle désignée est contournée et `aria-pressed`. |
| `Label`, `Culture` | Nom accessible (« Diagramme de Gantt ») et culture des mois, numéros de semaine et dates. |

L'axe couvre les tâches avec une marge : deux jours avant et trois après au zoom jour, une semaine
entière de part et d'autre au zoom semaine, les mois entiers au zoom mois. Un jour mesure 32, 14 ou
4 pixels selon le zoom : le graphique s'élargit et défile en largeur dans son cadre, la colonne des
noms restant en place. Les deux colonnes partagent une hauteur de ligne fixe en pixels (36, en-tête
44), reprise dans la feuille de style. Un libellé tient dans sa barre quand il y entre, sinon il suit
la barre ; un halo de la couleur de surface le garde lisible sur la partie remplie comme sur la grille.

Accessibilité : chaque barre porte un vrai `<button>` placé par un `foreignObject`, atteint par Tab,
activé nativement par Entrée et Espace, nommé par son titre, ses dates, son avancement et ce qu'il
attend (« Serveur, du 14 septembre 2026 au 30 septembre 2026, 20 % fait, après Entretiens
utilisateurs »). Le dessin lui-même est masqué aux technologies d'assistance.

## Déroulement : `OmniStepTimeline`

Le déroulement d'une exécution, sur le modèle de celui des pipelines d'Aetheus : une ligne par étape,
son nom à gauche, une piste grise sur toute la durée de l'exécution, une barre colorée à l'endroit et
sur la durée où l'étape a tourné, la durée à droite, et sous les barres la liste des étapes jamais
démarrées (« Jamais démarrées : Confirm, Record release »), nommées plutôt que dessinées puisqu'elles
n'ont occupé aucun temps. L'en-tête porte `0s` et la durée totale (`20m15`).

Une étape est un `OmniStepTimelineStep(Name, StartedAt, CompletedAt, Status, Secondary)` :
`StartedAt` nul la range parmi les jamais démarrées, `CompletedAt` nul avec le statut `Running` la fait
courir jusqu'à `Now` (l'horloge par défaut), `Secondary` met son nom en italique et en gris
(préparation, nettoyage). `OmniStepTimelineStatus` donne la couleur : `Success` succès, `Failed`
danger, `Running` accent, `Skipped` et
`Cancelled` bordure.

`Columns` ajoute à droite des durées des colonnes de valeurs alignées d'une ligne à l'autre
(`OmniStepTimelineColumn(Title, Value)`, avec `Description` en infobulle de l'en-tête) : la durée
habituelle d'une étape, sa dernière durée, ce qu'elle a consommé. Une cellule sans valeur reste vide à sa
place ; une colonne qu'aucune étape ne remplit n'est pas dessinée. L'en-tête étant masqué aux
technologies d'assistance, chaque valeur est lue avec son titre (« Habituelle : 1m40 »).
`OmniStepTimelineStep.ExpectedDuration` fait remplir la barre d'une étape en cours jusqu'à sa durée
habituelle, puis une seconde couche repartant de la gauche mesure le dépassement, pleine au double ; la
proportion est lue (« 50 % de la durée habituelle écoulés », « au-delà de la durée habituelle »). Une
étape terminée n'a pas de remplissage.

L'axe va du premier début à la dernière fin. Une étape trop brève garde 0,6 % de largeur pour rester
visible, une fin antérieure à son début (horloges en désaccord) est ramenée à une durée nulle, et une
exécution sans durée donne à chaque barre toute la largeur. Les durées s'écrivent comme dans un
journal : `42s`, `3m05`, `1h20`. Chaque ligne annonce son statut et son départ par un texte réservé
aux lecteurs d'écran (« en échec, démarrée à 1m00, durée 2m00 »).

## Preuves et limites

- Tests bUnit : `TimelineLayoutTests`, `GanttComponentTests` (plage, lignes et groupes, barres,
  flèches, en-têtes, ligne du jour, boutons nommés, zoom non lié), `StepTimelineComponentTests`
  (placement, largeur minimale, cas dégénérés, formats, rendu), `StepTimelineColumnsTests` (colonnes,
  remplissage et dépassement, statuts).
- Rendus vérifiés dans Chromium sans serveur, sur des pages statiques produites par bUnit avec la
  feuille réelle : les quatre dispositions de la frise (et leur repli sous 30rem), le Gantt au zoom
  semaine en clair et en sombre, le déroulement réussi.
- Le Gantt ne fait pas défiler son cadre jusqu'à aujourd'hui à l'ouverture, faute de script ; ses
  libellés sont placés sur une largeur de caractère estimée, pas mesurée.
