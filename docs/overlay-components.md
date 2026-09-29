# Surcouches (famille Overlays)

Les surcouches s'affichent au-dessus de la page : dialogues, notifications, menus, panneaux et infobulles. Les surfaces flottantes sont rendues par le portail d'`OmniComponentsHost` quand il est présent, gardées dans la fenêtre, et rendent le focus à leur déclencheur en se fermant. Le focus d'ouverture des dialogues est décrit dans [accessibility-contract.md](accessibility-contract.md).

| Composant | Rôle |
| --- | --- |
| `OmniComponentsHost` | Hôte des surcouches, placé une fois dans la mise en page : il rend les dialogues et notifications demandés par `OmniOverlayService`, la pile de notifications (position, compte à rebours, regroupement) et le portail des menus flottants. |
| `OmniDialog` | Dialogue modal ou fenêtre non modale : titre, contenu, pied, largeur au choix, intention teintée, déplaçable ; peut n'être fermé que par son contenu (`alertdialog`). |
| `OmniNotification` | Carte de notification : sévérité, texte, titre, bouton de fermeture, action proposée (annuler, réessayer), lien de détail validé et compte à rebours décoratif. En général demandée par `OmniOverlayService.Notify` et rendue par l'hôte. |
| `OmniTooltip` | Infobulle locale d'un élément : délai d'apparition, immédiate au focus clavier, flèche vers ce qu'elle décrit, suivi du pointeur au choix. |
| `OmniTitleTooltips` | Posé une fois dans la mise en page, il remplace l'infobulle native de tout élément à attribut `title` par celle du paquet (largeur bornée, délai, flèche, gardée dans la fenêtre) ; il ne rend rien lui-même. |
| `OmniPopover` | Panneau ancré non modal ouvert par un bouton : un clic ailleurs ou Échap le ferme, Échap rend le focus au bouton, sans piège de focus. |
| `OmniContextMenu` | Menu contextuel : le contenu enveloppé est le déclencheur, un clic droit ouvre le menu au pointeur, la touche Menu ou Maj+F10 sous le déclencheur. |
| `OmniOverflowMenu` | Menu « ⋮ » d'une ligne, d'une carte ou d'un en-tête : trois points sans cadre qui ouvrent une liste verticale d'actions. |
| `OmniMenuItem` | Une entrée de menu (`role="menuitem"`), atteinte par les flèches de son menu et jamais par Tab : bouton ou lien, icône en colonne puis libellé ; la choisir ferme le menu, rend le focus au déclencheur, puis lance l'action. |

Les menus du paquet (débordement, contextuel, bouton scindé, profil) partagent le même clavier : flèches, Début, Fin, Échap (focus rendu), Tab et appui extérieur ferment. PLAN-007 unifie leurs entrées sur `OmniMenuItem` ; la liste ci-dessus suit l'état du code au moment de l'écriture.

Détails : tailles et pied des dialogues dans [ui-conventions.md](ui-conventions.md) ; largeur et texte long de l'infobulle dans [page-components.md](page-components.md).
