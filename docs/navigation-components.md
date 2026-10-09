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

Un `OmniPopover` en bout de barre d'application, nommé par `Label` (texte localisé par défaut). Son bouton montre le nom de l'utilisateur (`UserName`) quand il y en a un, sinon une icône seule. De haut en bas : l'utilisateur et son rôle en pastille (`UserName`, `Role`), les lignes propres à l'application (`ChildContent`), la langue quand `Languages` (des `OmniAppMenuLanguage` : `Code`, `Name`, `FlagSource` facultatif) en offre au moins deux (`Language`, `LanguageChanged`, drapeau par `ShowFlags`), le mode clair, sombre ou système en trois boutons carrés joints (`Appearance`, et `AppearanceChanged`, obligatoire), la ligne Thème (`OnTheme`, obligatoire : l'hôte ouvre sa fenêtre d'apparence), les paramètres (`OnSettings`), puis la version et la déconnexion (`Version`, `OnSignOut`). Une ligne facultative ne s'affiche que si l'hôte fournit ce qu'il lui faut ; `OnTheme`, `OnSettings` et `OnSignOut` sont levés une fois le menu fermé, alors que le mode et la langue changent menu ouvert. Sous un thème `DarkOnly` passé dans `Preset`, le choix du mode est figé sur Sombre avec la mention « Ce thème est toujours sombre » ; sous un thème `LightOnly`, sur Clair avec « Ce thème est toujours clair ». `Open` et `OpenChanged` laissent l'hôte tenir l'ouverture ; `Disabled` empêche d'ouvrir.

Détails : `OmniLink` dans [foundation-components.md](foundation-components.md) ; barre latérale et poignée dans [form-components.md](form-components.md) ; fil d'Ariane tenu par un service et étapes d'un assistant dans [page-components.md](page-components.md).
