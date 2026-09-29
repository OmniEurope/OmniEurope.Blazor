# Navigation (famille Navigation)

Ces composants mènent d'une page ou d'une vue à une autre et disent où l'on est (`aria-current`). Les motifs clavier (onglets en `tabindex` itinérant, menus) sont décrits dans [accessibility-contract.md](accessibility-contract.md).

| Composant | Rôle |
| --- | --- |
| `OmniLink` | Lien natif ; un nouvel onglet ajoute `noopener noreferrer`, l'icône de lien externe et une mention pour les technologies d'assistance ; l'adresse passe par la politique d'URI du paquet. |
| `OmniBreadcrumb` | Fil d'Ariane : liste ordonnée dans un landmark de navigation nommé. |
| `OmniBreadcrumbItem` | Une étape du fil : un lien, ou la page courante (`aria-current="page"`) rendue en texte. |
| `OmniPanelMenu` | Menu de navigation à groupes imbriqués, qui déplie la branche portant la page courante ; réductible à ses icônes dans une barre latérale. |
| `OmniPanelMenuItem` | Une entrée du menu : un lien, un groupe qui se déplie, ou une action ; icône et badge de fin de ligne facultatifs. |
| `OmniProfileMenu` | Menu de compte : un bouton, avatar (initiales ou glyphe) ou résumé, qui ouvre la liste des actions du compte (`OmniMenuItem`), avec l'identité en tête. |
| `OmniSidebar` | Landmark `aside` de la barre latérale, ouverte, réduite en rail ou superposée sur petit écran. |
| `OmniSidebarToggle` | Poignée qui ouvre et ferme la barre latérale (`aria-controls`, `aria-expanded`). |
| `OmniTabs` | Onglets : liste d'onglets et panneau de l'onglet choisi, liés par `Value`. |
| `OmniTabsItem` | Un onglet et son panneau : clé (`Key`, repli sur `Title`), titre (texte ou contenu riche), icône facultative, contenu. |
| `OmniSteps` | Étapes d'un parcours, numérotées, liées par `Value` (index de l'étape courante) ; un contrôle facultatif peut refuser le passage à une autre étape. |
| `OmniStepsItem` | Une étape et son panneau : libellé, désactivation, icône facultative à la place du numéro (qui reste lu par les technologies d'assistance). |

Détails : `OmniLink` dans [foundation-components.md](foundation-components.md) ; barre latérale et poignée dans [form-components.md](form-components.md) ; fil d'Ariane tenu par un service et étapes d'un assistant dans [page-components.md](page-components.md).
