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

Les cibles Razor des cinq lots sont maintenant présentes. Les descriptions ci-dessous rappellent l'ordre de construction ; elles ne signifient pas que toutes les capacités sont stabilisées. La validation comportementale se poursuit avant toute migration d'une application consommatrice.

### Lot 1 — primitives fréquentes

`OmniButton`, `OmniStack`, `OmniCard` et `OmniAlert` ont constitué la tranche pilote, ensuite étendue au texte, à l'icône, au badge, au lien, aux lignes/colonnes et au skeleton.

### Lot 2 — formulaires simples

Les cibles des champs texte, zone de texte, nombre, mot de passe, case à cocher, interrupteur, libellé, champ de formulaire et validateurs sont présentes et intégrées à `EditContext` selon leur contrat.

### Lot 3 — navigation et retours utilisateur

Les cibles du menu latéral, breadcrumb, onglets, étapes, dialogue, notification, tooltip et menu contextuel sont présentes. Les parcours de focus et superpositions restent à éprouver dans un navigateur réel.

### Lot 4 — données complexes

Les cibles de liste, pagination, arbre et grille sont présentes. La grille couvre colonnes, tri, filtre, sélection, édition et modèles de cellule ; la pagination limite le rendu, mais la virtualisation pilotée par le viewport reste à implémenter.

### Lot 5 — visualisation, planification et édition

Les cibles des graphiques SVG, jauge, planificateur et éditeur riche sont présentes. La géométrie liée aux axes et aux empilements, la sélection et la composition IME de l'éditeur, ainsi que les preuves navigateur restent à compléter.

## Règle de priorité

À complexité comparable, l'ordre a été déterminé par le nombre de projets actifs dans `component-inventory.json`, puis par le nombre d'occurrences actives. La construction de toutes les cibles précède la migration : Aetheus validera ultérieurement chaque lot sur un écran réel avant généralisation aux autres projets.
