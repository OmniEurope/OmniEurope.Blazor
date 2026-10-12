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
| `OmniSettingsDangerZone` | Rubrique des actions irréversibles, en fin de page de réglages : la carte d'une `OmniSettingsSection` dont le filet et le titre prennent la couleur de danger de la palette (dans chaque thème, en clair comme en sombre), région nommée par son titre de niveau 2. `Title` vaut par défaut « Zone de danger », `Description` est facultative. |
| `OmniSettingsDangerAction` | Une action irréversible dans une tuile de la zone : nom et ce qu'elle détruit à gauche, bouton `Danger` à droite qui demande d'abord par la confirmation du paquet. Voir « Zone de danger » plus bas. |
| `OmniThemeScope` | Portée d'apparence : mode clair, sombre ou système (toujours sombre sous un thème `DarkOnly`), thème, palette, densité, police et fond animé, appliqués à tout son contenu ; un thème qui le demande y dessine son fond dans un `canvas` (`omni-black-hole.js`). Un mot trop long pour sa ligne y est coupé dans sa boîte (`overflow-wrap: break-word`), sauf dans le code. |

Détails : coquille, largeur du contenu, piles, rangées et colonnes dans [foundation-components.md](foundation-components.md) ; `OmniBody` et la barre latérale dans [form-components.md](form-components.md) ; thèmes, palettes et densité dans [foundation-components.md](foundation-components.md), « Thèmes, palettes et densité ».

## Zone de danger

Une page de réglages range ses actions irréversibles (supprimer un projet, réinitialiser un compte) dans une `OmniSettingsDangerZone`, une `OmniSettingsDangerAction` par action :

```razor
<OmniSettingsDangerZone Description="Les actions de cette zone ne peuvent pas être annulées.">
    <OmniSettingsDangerAction Title="Supprimer le projet"
                              Description="Le projet, ses rapports et son historique sont effacés."
                              ConfirmMessage="@($"Le projet « {_project.Name} » et ses rapports seront supprimés définitivement.")"
                              OnConfirm="DeleteProjectAsync" OnConfirmError="ShowError" />
</OmniSettingsDangerZone>
```

- Le bouton (`ActionText`, « Supprimer » par défaut ; `ActionIcon`, `OmniIconName.Delete` par défaut) ouvre `OmniOverlayService.ConfirmAsync` : titre `ConfirmTitle` (par défaut le `Title` de l'action), question `ConfirmMessage` (par défaut « Cette action est irréversible. Voulez-vous continuer ? » ; mieux vaut y nommer ce qui disparaît), action `ConfirmText` (par défaut le texte du bouton) en `Danger`, avec la même icône, puis Annuler. Sans `OmniComponentsHost` autour, la question est celle du navigateur (`confirm`), comme pour `OmniUnsavedChangesGuard`.
- Seule la confirmation appelle `OnConfirm` ; Annuler, Échap, le bouton de fermeture et le fond ne font rien, et le dialogue rend le focus au bouton. Le bouton reste occupé (`omni-busy`, `aria-busy`, jamais `disabled`, STD-BUSY) jusqu'à la fin de la tâche ; un clic pendant ce temps est ignoré.
- Un échec d'`OnConfirm` va à `OnConfirmError`, qui reçoit l'exception (à l'hôte de le dire, par exemple par `OmniOverlayService.Notify`) ; sans gestionnaire, l'exception remonte au moteur de rendu, comme pour `OmniMarkdownExportButton.OnExportError`. Une annulation (`OperationCanceledException`) n'y passe jamais.
- `Disabled` désactive le bouton (action interdite à cet utilisateur).
- Le bouton garde son texte visible pour nom accessible ; il est décrit (`aria-describedby`) par le nom et la description de la tuile, donc un lecteur d'écran entend quelle action il lance. `Id` va sur le bouton, avec les attributs supplémentaires ; `Class` sur la tuile.
- Le paquet n'a pas de saisie de confirmation (taper le nom du projet) : la confirmation est le dialogue du paquet.
