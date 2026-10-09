# Mise en page (famille Layout)

Ces composants posent la structure d'une application et d'une page : la coquille et ses landmarks, les conteneurs, les réglages et la portée de thème. Ils rendent du HTML sémantique et des classes de la feuille statique ; `OmniThemeScope`, `OmniMain`, `OmniStack`, `OmniFieldset` et `OmniBootSplash` chargent en plus un module JavaScript (jetons de thème écrits par le CSSOM, voir [csp-contract.md](contracts/csp-contract.md)).

| Composant | Rôle |
| --- | --- |
| `OmniLayout` | Coquille de page pleine largeur qui accueille l'en-tête, la barre latérale et le corps. |
| `OmniHeader` | Landmark `header` de l'application, collant ou non : marque (logo, nom, lien d'accueil), actions et boutons de fenêtre. |
| `OmniBody` | Zone flexible entre les landmarks, qui range la barre latérale et le contenu principal. |
| `OmniMain` | Landmark `main`, cible du lien d'évitement ; peut centrer le contenu à une largeur plafonnée et devenir le conteneur qui défile. |
| `OmniSkipLink` | Lien d'évitement (WCAG 2.4.1), à placer en premier dans la mise en page : caché jusqu'au focus, il donne le focus à `TargetId` (sinon au premier `main`) par script, sans suivre son adresse, qu'une balise `<base href="/">` ferait mener à la page racine. `Text` remplace « Aller au contenu principal ». |
| `OmniWindowControls` | Boutons de légende d'une fenêtre de bureau sans bordure (réduire dans la zone de notification, réduire, agrandir ou restaurer, fermer), dessinés en fin d'en-tête ; un bouton n'apparaît que si son action est fournie. |
| `OmniBootSplash` | Retire en fondu l'écran de démarrage que l'hôte écrit dans sa page, une fois l'application rendue ; il ne rend lui-même aucun élément. |
| `OmniStack` | Pile flex verticale ou horizontale, avec espacement, alignement, retour à la ligne ou défilement et repli d'une rangée trop étroite. |
| `OmniRow`, `OmniColumn` | Rangée et colonnes sur douze unités, avec variantes responsive. |
| `OmniCard` | Carte : surface qui regroupe un contenu, avec en-tête et pied facultatifs ; peut porter sa propre densité. |
| `OmniFieldset` | Groupe de champs natif avec `legend` obligatoire, désactivable, repliable. |
| `OmniSettingsSection` | Carte d'une rubrique de réglages : son titre, une ligne sur ce qu'elle change, puis ses réglages. La rubrique prend toute la largeur de son conteneur, sans plafond ; `Paired` range ses tuiles deux par ligne (chacune la moitié, empilées sous environ 24 rem par tuile), une tuile `FullWidth` prenant toute la ligne. |
| `OmniSettingsTile` | Un réglage dans une tuile de sa rubrique : icône, nom et effet à gauche, contrôle à droite ; un interrupteur ou une case à côté du nom fait de toute la tuile son libellé cliquable. |
| `OmniThemeScope` | Portée d'apparence : mode clair, sombre ou système (toujours sombre sous un thème `DarkOnly`, toujours clair sous un thème `LightOnly`), thème, palette, densité, police et fond animé, appliqués à tout son contenu ; un thème qui le demande y dessine son fond dans un `canvas` (`omni-black-hole.js`). Un mot trop long pour sa ligne y est coupé dans sa boîte (`overflow-wrap: break-word`), sauf dans le code. |

Détails : coquille, largeur du contenu, piles, rangées et colonnes dans [foundation-components.md](foundation-components.md) ; `OmniBody` et la barre latérale dans [form-components.md](form-components.md) ; thèmes, palettes et densité dans [foundation-components.md](foundation-components.md), « Thèmes, palettes et densité ».
