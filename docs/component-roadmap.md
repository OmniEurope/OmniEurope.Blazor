# Feuille de route fonctionnelle

L'inventaire décrit les noms observés dans les applications. Cette feuille de route décrit leur **nature fonctionnelle**, sans reprendre l'architecture, l'API ni le code de Radzen.

## Familles observées

| Famille | Capacités observées | Direction OmniEurope |
|---|---|---|
| Fondations et disposition | layout, body, header, sidebar, stack, row, column, card, fieldset, thème | primitives HTML sémantiques, conteneurs flex/grid et design tokens statiques |
| Typographie et contenu | text, label, link, icon, image, badge, alert, skeleton | composants légers privilégiant le HTML natif |
| Actions | button, split button, toggle button | boutons accessibles, menus d'actions et états occupés |
| Formulaires | text box, password, text area, numeric, drop-down, checkbox, switch, date picker, list box, select bar, radio list, slider, color picker, autocomplete, upload | champs composables basés sur `InputBase<T>`, validation Blazor et ARIA |
| Validation | required, length, email, compare | messages et règles reliés à `EditContext`, sans duplication du moteur Blazor |
| Données | data grid, columns, data list, pager, tree, levels et items | collection, virtualisation, tri, filtre, sélection et hiérarchie par capacités séparées |
| Navigation | panel menu, breadcrumb, tabs, steps, profile menu | navigation clavier conforme aux motifs ARIA et intégration `NavigationManager` |
| Superpositions | dialog, notification, tooltip, context menu | couche de portail contrôlée, focus trap, restauration du focus et annonces live |
| Graphiques | chart, axes, legend, grid lines, markers, labels, séries line/area/bar/column/pie/donut/stacked, gauge | rendu SVG propre, palette par classes et attributs SVG, sans style inline |
| Édition riche | HTML editor et commandes bold/italic/indent/outdent/subscript/superscript/undo/redo | éditeur isolé, sorties assainies et commandes explicites ; lot tardif à risque élevé |
| Temps et planification | timeline, scheduler, day/week/month views | modèles temporels indépendants, navigation clavier et virtualisation |
| Infrastructure | components host, notification/dialog/tooltip/context services, appearance toggle | services opt-in enregistrés explicitement, aucun chargement distant automatique |

## Lots de réalisation

### Lot 1 — primitives fréquentes

`OmniButton`, `OmniStack`, `OmniCard` et `OmniAlert` constituent la tranche pilote. La suite immédiate couvre texte, icône, badge, lien, ligne/colonne et skeleton. Ce lot adresse les composants les plus répandus avec une complexité limitée.

### Lot 2 — formulaires simples

Champs texte, zone de texte, nombre, mot de passe, case à cocher, interrupteur, libellé, champ de formulaire et validateurs. L'objectif est une intégration native à `EditContext` avant d'ajouter les sélecteurs complexes.

### Lot 3 — navigation et retours utilisateur

Menu latéral, breadcrumb, onglets, étapes, dialogue, notification, tooltip et menu contextuel. Ce lot exige les premières primitives de focus et de portail.

### Lot 4 — données complexes

Liste, pagination, arbre et grille. La grille est décomposée en capacités testables : colonnes, tri, filtre, sélection, édition, virtualisation et modèles de cellule.

### Lot 5 — visualisation, planification et édition

Graphiques SVG, jauge, planificateur et éditeur riche. Ces composants sont reportés parce qu'ils concentrent le plus de surface interactive, d'accessibilité et de risques CSP.

## Règle de priorité

À complexité comparable, l'ordre est déterminé par le nombre de projets actifs dans `component-inventory.json`, puis par le nombre d'occurrences actives. Aetheus valide chaque lot sur un écran réel avant généralisation aux autres projets.
