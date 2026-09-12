# Capacités par famille

Les familles ci-dessous regroupent les composants publics de la bibliothèque et décrivent leurs limites actuelles.

- Actions : boutons simples, bascules, boutons scindés et actions de menu.
- Charts : moteur SVG, axes, domaines partagés, séries projetées et empilements à baselines cumulées.
- Data : listes, pagination, arbre et grille virtualisable avec projection locale ou chargement distant annulable par blocs.
- Editor : édition source HTML, aperçu sanitizé, historique et outils personnalisés.
- Feedback : alertes à deux variantes de remplissage et emplacement d'icône, progression, barre de chargement de page (`OmniLoadingBar`, en balayage ou en remplissage continu), skeletons et indicateurs d'état.
- Forms : champs texte, formulaires typés, validation et contrôles de saisie.
- Foundation : primitives de texte, titres et types fondamentaux.
- Layout : conteneurs, cartes, panneaux, barres, sections et encadrés de réglages, et structure de page. Une rangée `OmniStack` trop étroite défile sous un chevron de chaque côté, sans barre de défilement (`Overflow="Scroll"`), ou se replie en icônes (`Overflow="Collapse"`).
- Navigation : fil d'Ariane, menus à groupes imbriqués qui déplient la branche portant la page courante, onglets, étapes et profil, avec icônes optionnelles et rendu icône seule du menu latéral.
- Overlays : hôte, dialogues empilés avec ouverture attendue par `OpenDialogAsync`, notifications bornées et regroupables en cascade, tooltip local avec délai d'apparition (`Delay`, 700 ms par défaut, immédiat au focus clavier) et suivi du pointeur (`Tracking`), et menu contextuel porté.
- Scheduling : timeline et scheduler jour/semaine/mois en `DateTimeOffset`, avec fuseau explicite.
- Selection : dropdown simple/multiple, autocomplete annulable, listes de choix, date, slider, couleur et upload.

Les noms Omni décrivent des capacités, pas des implémentations.

## Variables CSS propres à un composant

Les variables de thème déclarées dans `:root` sont éditables dans le personnalisateur de la vitrine. Celles-ci se redéfinissent sur l'élément du composant qui les déclare, une valeur posée sur un ancêtre ne l'atteignant pas :

| Variable | Déclarée sur | Défaut | Effet |
|---|---|---|---|
| `--omni-tooltip-delay` | `.omni-tooltip` | `700ms` | Temps de pointeur posé avant l'apparition ; `OmniTooltip.Delay` la remplace par pas de 100 ms entre 0 et 2 s. |
| `--omni-loading-bar-color` | `.omni-loading-bar` | `var(--omni-color-accent)` | Couleur du trait de `OmniLoadingBar`. |
| `--omni-loading-bar-thickness` | `.omni-loading-bar` | `0.125rem` | Épaisseur du trait, posé dans le bord de ce qui précède la barre. |

`--omni-shadow-stack`, l'ombre qu'une notification empilée jette sur celle qu'elle recouvre, est une variable de thème de `:root` : un thème sombre peut la foncer.

## Limites fonctionnelles actuelles

- La DataGrid virtualise réellement le viewport avec un défilement continu, une hauteur de tableau paramétrable en longueur CSS et un chargement distant par blocs ; elle refuse explicitement de virtualiser en présence de groupes ou de lignes de détail. Voir [data-components.md](data-components.md).
- Les séries et axes SVG partagent désormais leurs domaines et projections; la couverture visuelle navigateur reste à étendre aux combinaisons de séries et de tailles de viewport.
- L'éditeur HTML assainit sa valeur, affiche un aperçu et conserve un historique ; ses commandes portent encore sur tout le document et ne gèrent ni sélection, ni caret, ni composition IME, ni collage spécialisé.
- Le catalogue illustre un sous-ensemble de la surface. Le registre `110/110` ne remplace pas des scénarios comportementaux, navigateur et accessibilité pour chaque cible.
