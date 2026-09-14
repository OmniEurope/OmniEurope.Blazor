# Capacités par famille

Les familles ci-dessous regroupent les composants publics de la bibliothèque et décrivent leurs limites actuelles.

- Actions : boutons simples, bascules, boutons scindés et actions de menu, aux trois mêmes tailles ; un bouton à icône seule est carré.
- Charts : moteur SVG, axes, domaines partagés, séries projetées et empilements à baselines cumulées, colonnes groupées par bandes de catégories, barres horizontales sur axes tournés, légende hors du tracé et jauge en demi-cercle. Voir [chart-components.md](chart-components.md).
- Data : listes, pagination, arbre, grille virtualisable avec projection locale ou chargement distant annulable par blocs, et tableur simple (`OmniSpreadsheet`) avec formules et barre de formule. Voir [data-components.md](data-components.md).
- Diagram : carte mentale SVG éditable à la souris, au toucher et au clavier, avec barre d'actions, panneau des propriétés du nœud et relecture sans perte du format de graphe stocké. Voir [diagram-components.md](diagram-components.md).
- Editor : éditeur WYSIWYG avec face source HTML et barre de commandes extensible (`OmniHtmlEditor`), traitement de texte léger avec export HTML et texte (`OmniDocumentEditor`), éditeur de code Monaco servi par l'hôte avec repli en texte brut (`OmniCodeEditor`). Voir [editor-components.md](editor-components.md).
- Feedback : alertes à deux variantes de remplissage et emplacement d'icône, progression, barre de chargement de page (`OmniLoadingBar`, en balayage ou en remplissage continu), skeletons et indicateurs d'état.
- Forms : champs texte, formulaires typés, validation et contrôles de saisie.
- Foundation : primitives de texte, titres et types fondamentaux.
- Layout : conteneurs, cartes, panneaux, barres, sections et encadrés de réglages, et structure de page. Une rangée `OmniStack` trop étroite défile sous un chevron de chaque côté, sans barre de défilement (`Overflow="Scroll"`), ou se replie en icônes (`Overflow="Collapse"`).
- Navigation : fil d'Ariane, menus à groupes imbriqués qui déplient la branche portant la page courante, onglets, étapes et profil, avec icônes optionnelles et rendu icône seule du menu latéral.
- Overlays : hôte, dialogues empilés avec ouverture attendue par `OpenDialogAsync`, confirmation `ConfirmAsync` à la convention des boutons de dialogue (action, puis Annuler en Danger), notifications bornées et regroupables en cascade, tooltip local avec délai d'apparition (`Delay`, 700 ms par défaut, immédiat au focus clavier) et suivi du pointeur (`Tracking`), menu contextuel porté, et panneau ancré non modal (`OmniPopover` : un clic ailleurs ou Échap le ferme, Échap rend le focus au bouton, sans piège de focus).
- Pages : en-tête de page à fil d'Ariane tenu par un service dont l'hôte fournit les routes (`OmniPageHeader`, `OmniBreadcrumbService`), fiche détaillée à en-tête peint avant les données et état introuvable (`OmniDetailShell`), page de connexion et adresse de retour locale (`OmniLoginShell`, `OmniReturnUrl`), voile de connexion perdue piloté par l'hôte (`OmniConnectionOverlay`), état vide, assistant à étapes validées une à une (`OmniWizard`), liste de définitions en colonnes et date relative à date exacte en infobulle. Voir [page-components.md](page-components.md).
- Scheduling : scheduler jour/semaine/mois en `DateTimeOffset` avec fuseau explicite, frise à droite, à gauche ou alternée, diagramme de Gantt (`OmniGantt`) et déroulement d'étapes sur une échelle de temps (`OmniStepTimeline`). Voir [scheduling-components.md](scheduling-components.md).
- Selection : dropdown simple/multiple, autocomplete annulable, listes de choix, date, slider, couleur et upload.

Les noms Omni décrivent des capacités, pas des implémentations.

## Variables CSS propres à un composant

Les variables de thème déclarées dans `:root` sont éditables dans le personnalisateur de la vitrine. Celles-ci se redéfinissent sur l'élément du composant qui les déclare, une valeur posée sur un ancêtre ne l'atteignant pas :

| Variable | Déclarée sur | Défaut | Effet |
|---|---|---|---|
| `--omni-tooltip-delay` | `.omni-tooltip` | `700ms` | Temps de pointeur posé avant l'apparition ; `OmniTooltip.Delay` la remplace par pas de 100 ms entre 0 et 2 s. |
| `--omni-loading-bar-color` | `.omni-loading-bar` | `var(--omni-color-accent)` | Couleur du trait de `OmniLoadingBar`. |
| `--omni-loading-bar-thickness` | `.omni-loading-bar` | `0.125rem` | Épaisseur du trait, posé dans le bord de ce qui précède la barre. |
| `--omni-main-max-width` | `.omni-main` | `100%`, sinon le plafond de `ContentWidth` | Largeur maximale de la colonne de contenu, gouttières comprises ; redéclarée sur chaque élément principal, elle ne fuit pas dans un élément principal imbriqué. |
| `--omni-data-grid-column-min-width` | `.omni-data-grid` | `8rem` | Plancher d'une colonne sans largeur : sous la somme des colonnes, le tableau défile de côté. |
| `--omni-data-grid-control-width` | `.omni-data-grid` | `2.75rem` | Largeur des colonnes de développement et de sélection. |
| `--omni-data-grid-edit-width` | `.omni-data-grid` | `5.25rem` | Largeur de la colonne d'édition. |
| `--omni-spreadsheet-column-width` | `.omni-spreadsheet` | `7.5rem` | Largeur des colonnes du tableur. |
| `--omni-spreadsheet-height` | `.omni-spreadsheet` | `26rem` | Hauteur maximale de la zone qui défile. |

Celles-ci ne sont déclarées nulle part et se lisent avec une valeur de repli : posées sur l'élément ou sur un ancêtre, elles s'appliquent.

| Variable | Lue par | Défaut | Effet |
|---|---|---|---|
| `--omni-layout-wide-width` | `OmniLayout`, `OmniMain` | `90rem` | Plafond de la largeur `Wide`. |
| `--omni-layout-content-width` | `OmniLayout`, `OmniMain` | `72rem` | Plafond de la largeur `Content`. |
| `--omni-main-gutter` | `OmniMain` défilant | `var(--omni-space-md)` | Marge intérieure de l'élément principal, gouttière de la colonne comprise. |
| `--omni-busy-veil-color` | `.omni-busy`, `.btn-busy` | `#000000` | Couleur du voile d'un bouton occupé. |

`--omni-shadow-stack`, l'ombre qu'une notification empilée jette sur celle qu'elle recouvre, est une variable de thème de `:root` : un thème sombre peut la foncer.

## Limites fonctionnelles actuelles

- La DataGrid virtualise réellement le viewport avec un défilement continu, une hauteur de tableau paramétrable en longueur CSS et un chargement distant par blocs ; avec `Items`, elle virtualise aussi les groupes et les lignes de détail ; avec `Load`, elle refuse explicitement les groupes et les détails. Voir [data-components.md](data-components.md).
- Les séries et axes SVG partagent désormais leurs domaines et projections; la couverture visuelle navigateur reste à étendre aux combinaisons de séries et de tailles de viewport.
- L'éditeur HTML s'appuie sur `document.execCommand` pour sa face visuelle ; la composition IME n'a pas été éprouvée sur un clavier réel. `OmniCodeEditor` n'utilise Monaco que si l'hôte le sert et autorise `style-src 'unsafe-inline'`.
- La carte mentale n'a pas de rendu alternatif hors SVG et ses gestes tactiles (pincement, appui long) n'ont pas été vérifiés sur un écran tactile réel.
- Le catalogue illustre un sous-ensemble de la surface. Le registre `110/110` ne remplace pas des scénarios comportementaux, navigateur et accessibilité pour chaque cible.
