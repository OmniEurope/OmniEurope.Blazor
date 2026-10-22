# Navigation (famille Navigation)

Ces composants mènent d'une page ou d'une vue à une autre et disent où l'on est (`aria-current`). Les motifs clavier (onglets en `tabindex` itinérant, menus) sont décrits dans [accessibility-contract.md](contracts/accessibility-contract.md).

| Composant | Rôle |
| --- | --- |
| `OmniLink` | Lien natif ; un nouvel onglet ajoute `noopener noreferrer`, l'icône de lien externe et une mention pour les technologies d'assistance ; l'adresse passe par la politique d'URI du paquet. |
| `OmniBreadcrumb` | Fil d'Ariane : liste ordonnée dans un landmark de navigation nommé. |
| `OmniBreadcrumbItem` | Une étape du fil : un lien, ou la page courante (`aria-current="page"`) rendue en texte. |
| `OmniPanelMenu` | Menu de navigation à groupes imbriqués, qui déplie la branche portant la page courante ; réductible à ses icônes dans une barre latérale. |
| `OmniPanelMenuItem` | Une entrée du menu : un lien, un groupe qui se déplie, ou une action ; icône et badge de fin de ligne facultatifs. |
| `OmniProfileMenu` | Menu de compte : un bouton, avatar (initiales ou glyphe) ou résumé, qui ouvre la liste des actions du compte (`OmniMenuItem`), avec l'identité en tête. |
| `OmniAppMenu` | Menu standard du bout de la barre d'application, le même dans toutes les applications (voir plus bas). |
| `OmniSidebar` | Landmark `aside` de la barre latérale, ouverte (12,5 rem par défaut, `--omni-sidebar-width`), réduite en rail ou superposée sur petit écran ; un menu poussé que l'hôte peut fermer se superpose sur téléphone. |
| `OmniSidebarToggle` | Poignée qui ouvre et ferme la barre latérale (`aria-controls`, `aria-expanded`) ; son glyphe ouvert suit le mode du menu (`Reveal`). |
| `OmniTabs` | Onglets : liste d'onglets et panneau de l'onglet choisi, liés par `Value`. |
| `OmniTabsItem` | Un onglet et son panneau : clé (`Key`, repli sur `Title`), titre (texte ou contenu riche), icône facultative, contenu. |
| `OmniSteps` | Étapes d'un parcours, numérotées, liées par `Value` (index de l'étape courante) ; un contrôle facultatif peut refuser le passage à une autre étape. |
| `OmniStepsItem` | Une étape et son panneau : libellé, désactivation, icône facultative à la place du numéro (qui reste lu par les technologies d'assistance). |

## Menu de l'application : `OmniAppMenu`

Un `OmniPopover` en bout de barre d'application, nommé par `Label` (texte localisé par défaut). Son bouton montre le nom de l'utilisateur (`UserName`) quand il y en a un, sinon une icône seule. De haut en bas : l'utilisateur et son rôle en pastille (`UserName`, `Role`), les lignes propres à l'application (`ChildContent`), la langue quand `Languages` (des `OmniAppMenuLanguage` : `Code`, `Name`, `FlagSource` facultatif) en offre au moins deux (`Language`, `LanguageChanged`, drapeau par `ShowFlags` ; la liste native des langues est étirée sur toute la ligne, un clic n’importe où sur la ligne l’ouvre, et la ligne n’a qu’un élément actionnable), la ligne Mode (`Appearance`, et `AppearanceChanged`, obligatoire : un seul bouton sur toute la ligne, dont le texte à droite nomme le mode en vigueur ; un clic passe au mode suivant, clair, sombre, système, puis de nouveau clair), la ligne Thème (`OnThemeOpen`, obligatoire : l'hôte ouvre sa fenêtre d'apparence), les paramètres (`OnSettingsOpen`), puis la version et la déconnexion (`Version`, `OnSignOut`). Une ligne facultative ne s'affiche que si l'hôte fournit ce qu'il lui faut ; `OnThemeOpen`, `OnSettingsOpen` et `OnSignOut` sont levés une fois le menu fermé, alors que le mode et la langue changent menu ouvert. Sous un thème `DarkOnly` passé dans `Preset`, la ligne Mode est désactivée, figée sur Sombre, avec la mention « Ce thème est toujours sombre ». `Open` et `OpenChanged` laissent l'hôte tenir l'ouverture ; `Disabled` empêche d'ouvrir. Le panneau ouvert prend la largeur de sa ligne la plus large (au moins 10rem, au plus le plafond du popover : 24rem ou la fenêtre), pas une largeur fixe.

## Chevrons de défilement

Une bande d'onglets (`OmniTabs`), le menu d'`OmniSidebar`, une rangée `OmniStack` en `Scroll` et `OmniSelectBar` qui débordent défilent sans barre, sous un chevron de chaque côté qui cache encore des éléments (`focus/tabs.js`, `configureOverflow`). Le chevron est posé sur le bord de la boîte qui défile, hors du flux : la boîte garde sa taille et son contenu sa position, qu'un chevron paraisse ou non, si bien qu'atteindre la butée ne fait jamais sauter le contenu. Il est très légèrement transparent (90 % du fond de ce qui porte les éléments, `--omni-tabs-edge` pour les onglets) et reste lisible. La butée se juge avec une marge d'hystérésis : un chevron s'allume quand plus de 4 px restent cachés de son côté et s'éteint quand 2 px ou moins le sont. Un élément focalisé au clavier est amené à l'intérieur de la boîte et hors de la zone des chevrons. Les chevrons sont destinés au pointeur : hors de l'ordre de tabulation et de l'arbre d'accessibilité. `OmniSidebar` rend le menu et ses chevrons dans un conteneur `.omni-sidebar__scroller`, qui porte le positionnement.

Les flèches gauche et droite d'une bande d'onglets (et d'un groupe radio à roulement de focus tel qu'`OmniSelectBar`) suivent le sens de lecture : dans une page `dir="rtl"`, la flèche droite va à l'onglet précédent. Le script lit la direction réelle de la bande ; avant qu'il soit chargé, `OmniTabs` se règle sur la langue de l'interface (`CultureInfo.CurrentUICulture`).

Détails : `OmniLink` dans [foundation-components.md](foundation-components.md) ; barre latérale et poignée dans [form-components.md](form-components.md) ; fil d'Ariane tenu par un service et étapes d'un assistant dans [page-components.md](page-components.md).
