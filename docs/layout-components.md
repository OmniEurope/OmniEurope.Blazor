# Mise en page (famille Layout)

Ces composants posent la structure d'une application et d'une page : la coquille et ses landmarks, les conteneurs, les réglages et la portée de thème. Ils rendent du HTML sémantique et des classes de la feuille statique ; `OmniThemeScope`, `OmniMain`, `OmniStack`, `OmniFieldset` et `OmniBootSplash` chargent en plus un module JavaScript (jetons de thème écrits par le CSSOM, voir [csp-contract.md](contracts/csp-contract.md)) ; `OmniAccordion` n'en charge un qu'au premier focus dans la liste, pour les flèches entre les en-têtes.

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
| `OmniAccordion`, `OmniAccordionItem` | Liste de lignes dont le corps se déplie sous l'en-tête, une à la fois (défaut) ou plusieurs ensemble (`Multiple`). L'en-tête est libre (libellé, aide « ? », valeur, badge) et le composant dessine le chevron. Voir « Accordéon » plus bas. |
| `OmniSettingsSection` | Carte d'une rubrique de réglages : son titre, une ligne sur ce qu'elle change, puis ses réglages. La rubrique prend toute la largeur de son conteneur, sans plafond ; `Paired` range ses tuiles deux par ligne (chacune la moitié, empilées sous environ 24 rem par tuile), une tuile `FullWidth` prenant toute la ligne. |
| `OmniSettingsTile` | Un réglage dans une tuile de sa rubrique : icône, nom et effet à gauche, contrôle à droite ; un interrupteur ou une case à côté du nom fait de toute la tuile son libellé cliquable. |
| `OmniSettingsDangerZone` | Rubrique des actions irréversibles, en fin de page de réglages : la carte d'une `OmniSettingsSection` dont le filet et le titre prennent la couleur de danger de la palette (dans chaque thème, en clair comme en sombre), région nommée par son titre de niveau 2. `Title` vaut par défaut « Zone de danger », `Description` est facultative. |
| `OmniSettingsDangerAction` | Une action irréversible dans une tuile de la zone : nom et ce qu'elle détruit à gauche, bouton `Danger` à droite qui demande d'abord par la confirmation du paquet. Voir « Zone de danger » plus bas. |
| `OmniThemeScope` | Portée d'apparence : mode clair, sombre ou système (toujours sombre sous un thème `DarkOnly`), thème, palette, densité, police et fond animé, appliqués à tout son contenu ; un thème qui le demande y dessine son fond dans un `canvas` (`omni-black-hole.js`). Un mot trop long pour sa ligne y est coupé dans sa boîte (`overflow-wrap: break-word`), sauf dans le code. |

Détails : coquille, largeur du contenu, piles, rangées et colonnes dans [foundation-components.md](foundation-components.md) ; `OmniBody` et la barre latérale dans [form-components.md](form-components.md) ; thèmes, palettes et densité dans [foundation-components.md](foundation-components.md), « Thèmes, palettes et densité ».

## Accordéon

`OmniAccordion` range des `OmniAccordionItem` dans une liste bordée, aux lignes séparées par un filet neutre. Une ligne se compose d'un en-tête et d'un corps qui s'ouvre dessous :

```razor
<OmniAccordion HeadingLevel="OmniHeadingLevel.H3">
    <OmniAccordionItem Title="Fiabilité" Help="Anomalies qui peuvent casser le produit." Expanded="true">
        <SummaryContent>0 anomalie <OmniBadge Tone="OmniTone.Success">A</OmniBadge></SummaryContent>
        <ChildContent>Mesuré : 0, seuil : 0.</ChildContent>
    </OmniAccordionItem>
    <OmniAccordionItem Title="Couverture" @bind-Expanded="CoverageOpen">...</OmniAccordionItem>
</OmniAccordion>
```

**Paramètres de `OmniAccordion`** (avec `Id`, `Class`, `PresetName` et les attributs supplémentaires de `OmniComponentBase`) :

| Paramètre | Type | Défaut | Effet |
| --- | --- | --- | --- |
| `ChildContent` | `RenderFragment?` | | Les lignes, des `OmniAccordionItem`. |
| `Multiple` | `bool` | `false` | Vrai : plusieurs lignes ouvertes ensemble. Faux : ouvrir une ligne ferme celle qui était ouverte, qui rapporte sa fermeture par son `ExpandedChanged`. |
| `KeepContent` | `bool` | `false` | Faux : le corps n'est rendu que pendant que sa ligne est ouverte et disparaît à la fermeture (une longue liste ne coûte que ses en-têtes). Vrai : un corps ouvert une fois reste rendu, caché, ce qui garde défilement, filtres d'une grille et saisie. Une ligne jamais ouverte ne rend jamais son corps. |
| `HeadingLevel` | `OmniHeadingLevel` | `H3` | Niveau du titre (`role="heading"`, `aria-level`) qui enveloppe chaque bouton de bascule, à choisir selon le plan de la page. |
| `Density` | `OmniDensity?` | `null` | Densité propre au composant ; nul hérite de la portée de thème. |

**Paramètres de `OmniAccordionItem`** (avec `Id`, posé sur le bouton de bascule, `Class`, posée sur la ligne, `PresetName` et les attributs supplémentaires, posés sur la ligne) :

| Paramètre | Type | Défaut | Effet |
| --- | --- | --- | --- |
| `Title` | `string?` | `null` | Texte de l'en-tête, nom accessible du bouton. |
| `HeaderContent` | `RenderFragment?` | `null` | Remplace `Title` : zone du libellé, dans le bouton, qui peut revenir à la ligne et porter une seconde ligne. Contenu non interactif seulement (un bouton ne contient pas de contrôle). |
| `Help` | `string?` | `null` | Texte de l'aide : une marque « ? » (un `OmniTooltip` ouvert au survol et au focus, atteint par Tab) à côté du libellé. L'utiliser n'ouvre jamais la ligne. |
| `ActionsContent` | `RenderFragment?` | `null` | Contrôles propres à la ligne (lien, menu, aide personnalisée), à côté du bouton, interactifs si besoin. Un clic ou une touche dessus ne bascule jamais la ligne. |
| `SummaryContent` | `RenderFragment?` | `null` | Valeur ou badge en fin d'en-tête, avant le chevron. Non interactif : un clic dessus bascule la ligne, et un lecteur d'écran le lit comme la description du bouton (`aria-describedby`). |
| `ChildContent` | `RenderFragment?` | `null` | Le corps, révélé sous l'en-tête. |
| `Expanded` | `bool` | `false` | État de la ligne : celui du premier rendu, puis, à chaque changement de la valeur par l'hôte, celui que la ligne prend ; entre deux changements, les clics du lecteur font foi. |
| `ExpandedChanged` | `EventCallback<bool>` | | Rapporte l'état quand le lecteur ouvre ou ferme la ligne, et `false` quand un voisin qui s'ouvre la ferme (mode une ligne à la fois) ; permet `@bind-Expanded`. Un changement posé par l'hôte n'est pas rapporté. |

Choix de conception :

- **Motif ARIA** : celui de l'accordéon de l'APG. Chaque en-tête est un titre (`role="heading"`, `aria-level`) qui contient un seul vrai `<button type="button">`, portant `aria-expanded` et `aria-controls` ; le corps est un `role="region"` nommé par ce bouton (`aria-labelledby`), présent mais `hidden` tant que la ligne est fermée, de sorte que `aria-controls` désigne toujours un élément. Le conteneur n'a pas de rôle.
- **Rien d'interactif dans le bouton** : l'aide « ? » et les contrôles d'`ActionsContent` sont des frères du bouton, dessinés à côté de lui, jamais dedans. Le bouton couvre pourtant toute la ligne : sa pseudo-boîte `::after` s'étend sur l'en-tête et passe sous les contrôles voisins (positionnés après lui dans l'arbre), si bien que cliquer le libellé, la valeur ou le badge bascule la ligne et cliquer l'aide ou un contrôle ne la bascule pas. Seul un contrôle reçoit les clics de la zone des contrôles, le vide entre eux les laisse passer.
- **Clavier** : Tab atteint chaque bouton, `Entrée` et `Espace` (natifs) basculent la ligne ; Haut, Bas, Début et Fin déplacent le focus entre les en-têtes du même accordéon (Haut sur le premier va au dernier, Bas sur le dernier au premier), sans ouvrir de ligne. Ces touches sont celles de la page de `omni-focus.js` (`data-omni-accordion`), activées au premier focus dans l'accordéon : un accordéon qu'on n'atteint pas au clavier ne charge aucun script. Un accordéon imbriqué garde ses propres en-têtes. Sans script, Tab, `Entrée` et `Espace` fonctionnent seuls.
- **Corps** : par défaut non rendu tant que la ligne est fermée, voir `KeepContent`. Le chevron est dessiné par le composant (décoratif, `aria-hidden`) et tourne de 90 degrés à l'ouverture (la transition s'arrête sous `prefers-reduced-motion`).
- **Aspect** : aucun filet coloré sur le côté, des séparateurs neutres entre les lignes, la couleur de fond et la bordure de carte du thème, les marges de la densité. L'en-tête passe à la ligne quand il est trop étroit (375 px compris), sans défilement horizontal, et ne coupe pas ses infobulles (aucun `overflow` ne rogne la liste).
- **Seul** : un `OmniAccordionItem` hors d'un `OmniAccordion` est une simple zone dépliable qui s'ouvre et se ferme seule.
- **Limites** : en mode une ligne à la fois, des lignes ouvertes ensemble par `Expanded` au départ sont affichées telles quelles ; le rôle `region` multiplie les repères pour un lecteur d'écran, au-delà de six lignes ouvertes ensemble l'APG conseille de s'en passer.

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
