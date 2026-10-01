# Surcouches (famille Overlays)

Les surcouches s'affichent au-dessus de la page : dialogues, notifications, menus, panneaux et infobulles. Les surfaces flottantes sont rendues par le portail d'`OmniComponentsHost` quand il est présent, gardées dans la fenêtre, et rendent le focus à leur déclencheur en se fermant. Le focus d'ouverture des dialogues est décrit dans [accessibility-contract.md](contracts/accessibility-contract.md).

| Composant | Rôle |
| --- | --- |
| `OmniComponentsHost` | Hôte des surcouches, placé une fois dans la mise en page : il rend les dialogues et notifications demandés par `OmniOverlayService`, la pile de notifications (position, compte à rebours, regroupement) et le portail des menus flottants. |
| `OmniDialog` | Dialogue modal ou fenêtre non modale : titre, contenu, pied, largeur au choix, intention signalée par une pastille teintée devant le titre, en-tête et pied restant neutres (`Intent`, un `OmniTone`), déplaçable ; Échap, la croix et le voile le ferment tant que `Dismissible` est vrai ; peut n'être fermé que par son contenu (`alertdialog`). |
| `OmniNotification` | Carte de notification : sévérité, texte, titre, bouton de fermeture (`CloseLabel`), action proposée (annuler, réessayer, `OnActionClick`), lien de détail validé et compte à rebours décoratif. En général demandée par `OmniOverlayService.Notify` et rendue par l'hôte. |
| `OmniTooltip` | Infobulle locale d'un élément : délai d'apparition, immédiate au focus clavier, flèche vers ce qu'elle décrit, suivi du pointeur au choix ; `Focusable` met le déclencheur d'un contenu non focalisable dans l'ordre de tabulation. |
| `OmniTitleTooltips` | Posé une fois dans la mise en page, il remplace l'infobulle native de tout élément à attribut `title` par celle du paquet (largeur bornée, délai, flèche, gardée dans la fenêtre) ; il ne rend rien lui-même. |
| `OmniPopover` | Panneau ancré non modal ouvert par un bouton : un clic ailleurs ou Échap le ferme, Échap rend le focus au bouton, sans piège de focus. `Label` nomme le bouton (qui porte `Id`), `PopupLabel` le panneau (repli sur `Label`) ; `Size` et `Disabled` règlent le bouton. |
| `OmniContextMenu` | Menu contextuel : le contenu enveloppé est le déclencheur, un clic droit ouvre le menu au pointeur, la touche Menu ou Maj+F10 sous le déclencheur ; `Open` (`bool?`) le pilote au besoin, `Disabled` le coupe. |
| `OmniOverflowMenu` | Menu « ⋮ » d'une ligne, d'une carte ou d'un en-tête : trois points sans cadre qui ouvrent une liste verticale d'actions ; `Open`/`OpenChanged`, `Label` (déclencheur) et `MenuLabel` (liste). |
| `OmniMenuItem` | Une entrée de menu (`role="menuitem"`), atteinte par les flèches de son menu et jamais par Tab : bouton ou lien (`Href`), icône en colonne puis libellé, `Disabled`, `Tone` (`OmniTone.Danger` pour une action destructrice) ; la choisir ferme le menu, rend le focus au déclencheur, puis lance l'action (`OnClick`). |

Les menus du paquet (débordement, contextuel, bouton scindé, profil, et le menu contextuel de l'éditeur HTML) partagent les mêmes entrées, `OmniMenuItem`, et un seul moteur JS (`omni-focus.js`) : flèches, Début, Fin, Échap (focus rendu), Tab et appui extérieur ferment ; chaque menu est rendu dans le portail d'`OmniComponentsHost` (classes `.omni-menu` et `.omni-menu__item`).

Détails : tailles et pied des dialogues dans [ui-conventions.md](ui-conventions.md) ; largeur et texte long de l'infobulle dans [page-components.md](page-components.md).
