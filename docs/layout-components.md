# Mise en page (famille Layout)

Ces composants posent la structure d'une application et d'une page : la coquille et ses landmarks, les conteneurs, les réglages et la portée de thème. Ils rendent du HTML sémantique et des classes de la feuille statique ; `OmniThemeScope`, `OmniMain`, `OmniStack`, `OmniFieldset` et `OmniBootSplash` chargent en plus un module JavaScript (jetons de thème écrits par le CSSOM, voir [csp-contract.md](contracts/csp-contract.md)) ; `OmniAccordion` n'en charge un qu'au premier focus dans la liste, pour les flèches entre les en-têtes.

| Composant | Rôle |
| --- | --- |
| `OmniLayout` | Coquille de page pleine largeur qui accueille l'en-tête, la barre latérale et le corps. |
| `OmniHeader` | Landmark `header` de l'application, collant ou non : marque (logo, nom, lien d'accueil), actions et boutons de fenêtre ; `Surface` (`OmniHeaderSurface` : `Surface`, par défaut, ou `Accent`, la bande de l'application) en règle la peinture. |
| `OmniBody` | Zone flexible entre les landmarks, qui range la barre latérale et le contenu principal. |
| `OmniMain` | Landmark `main`, cible du lien d'évitement ; peut centrer le contenu à une largeur plafonnée et devenir le conteneur qui défile. |
| `OmniSkipLink` | Lien d'évitement (WCAG 2.4.1), à placer en premier dans la mise en page : caché jusqu'au focus, il donne le focus à `TargetId` (sinon au premier `main`) par script, sans suivre son adresse, qu'une balise `<base href="/">` ferait mener à la page racine. `Text` remplace « Aller au contenu principal ». |
| `OmniWindowControls` | Boutons de légende d'une fenêtre de bureau sans bordure (réduire dans la zone de notification, réduire, agrandir ou restaurer, fermer), dessinés en fin d'en-tête ; un bouton n'apparaît que si son action est fournie. |
| `OmniBootSplash` | Retire en fondu l'écran de démarrage que l'hôte écrit dans sa page, une fois l'application rendue ; il ne rend lui-même aucun élément. |
| `OmniStack` | Pile flex verticale ou horizontale (`Orientation`, un `OmniOrientation`), avec espacement, alignement, retour à la ligne ou défilement et repli d'une rangée trop étroite. |
| `OmniRow`, `OmniColumn` | Rangée et colonnes sur douze unités, avec variantes responsive. |
| `OmniCard` | Carte : surface qui regroupe un contenu, avec en-tête et pied facultatifs (`HeaderContent`, `FooterContent`) ; peut porter sa propre densité ; `DoubleWidth` l'étale sur deux colonnes d'une `OmniTileGrid` ; `Href` ou `OnClick` en font un seul lien ou un seul bouton (voir « Carte cliquable »). |
| `OmniTileGrid` | Grille de tuiles : `OmniCard`, `OmniSelectableCard`, `OmniStatTile`, `OmniFactTile` et `OmniNavTile` posées dedans ont la même largeur et, par défaut, exactement la même hauteur (`EqualHeights="false"` rend à chaque tuile sa hauteur naturelle), une tuile `DoubleWidth` en occupe deux, rien ne déborde. Voir « Règle des tuiles ». |
| `OmniFieldset` | Groupe de champs natif avec `legend` obligatoire, désactivable, repliable. |
| `OmniAccordion`, `OmniAccordionItem` | Liste de lignes dont le corps se déplie sous l'en-tête, une à la fois (défaut) ou plusieurs ensemble (`Multiple`). L'en-tête est libre (libellé, aide « ? », valeur, badge) et le composant dessine le chevron. Voir « Accordéon » plus bas. |
| `OmniDivider` | Filet fin de 1 px entre deux blocs, horizontal ou vertical (`Orientation`), sans marge : l'écart de la pile qui le porte l'espace. Voir « Séparateur » plus bas. |
| `OmniSettingsSection` | Carte d'une rubrique de réglages : son titre, une ligne sur ce qu'elle change, puis ses réglages. La rubrique prend toute la largeur de son conteneur, sans plafond ; `Paired` range ses tuiles deux par ligne (chacune la moitié, empilées sous environ 24 rem par tuile), une tuile `FullWidth` prenant toute la ligne. |
| `OmniSettingsTile` | Un réglage dans une tuile de sa rubrique : icône, nom et effet à gauche, contrôle à droite ; un interrupteur ou une case à côté du nom fait de toute la tuile son libellé cliquable. |
| `OmniSettingsDangerZone` | Rubrique des actions irréversibles, en fin de page de réglages : la carte d'une `OmniSettingsSection` dont le filet et le titre prennent la couleur de danger de la palette (dans chaque thème, en clair comme en sombre), région nommée par son titre de niveau 2. `Title` vaut par défaut « Zone de danger », `Description` est facultative. |
| `OmniSettingsDangerAction` | Une action irréversible dans une tuile de la zone : nom et ce qu'elle détruit à gauche, bouton `Danger` à droite qui demande d'abord par la confirmation du paquet. Voir « Zone de danger » plus bas. |
| `OmniThemeScope` | Portée d'apparence : mode clair, sombre ou système (toujours sombre sous un thème `DarkOnly`), thème, palette, densité, police et fond animé, appliqués à tout son contenu ; un thème qui le demande y dessine son fond dans un `canvas` (`omni-black-hole.js`). Un mot trop long pour sa ligne y est coupé dans sa boîte (`overflow-wrap: break-word`), sauf dans le code. |

Paramètres à préciser. `OmniHeader.Sticky` (faux par défaut) garde l'en-tête en haut de la page pendant qu'elle défile. `OmniMain.AriaLabelledBy` (`string?`, nul par défaut) est l'`id` de l'élément qui nomme le repère `main` (`aria-labelledby`), le titre de la page par exemple. `OmniStack.Align` (`OmniAlignment` : `Start`, `Center`, `End`, `Stretch` par défaut) dit comment les éléments se placent en travers de la direction de la pile, et `OmniStack.Justify` (`OmniJustification` : `Start` par défaut, `Center`, `End`, `Between`) comment ils se répartissent le long de cette direction. `OmniRow.Align` (`Stretch` par défaut : colonnes de même hauteur) place les colonnes en hauteur et `OmniRow.Justify` (`Start` par défaut) les répartit le long de la rangée. `OmniColumn.Span` (12 par défaut, toute la rangée) est la part de la rangée en douzièmes, à toutes les largeurs.

Détails : coquille, largeur du contenu, piles, rangées et colonnes dans [foundation-components.md](foundation-components.md) ; `OmniBody` et la barre latérale dans [form-components.md](form-components.md) ; thèmes, palettes et densité dans [foundation-components.md](foundation-components.md), « Thèmes, palettes et densité ».

## Règle des tuiles

Toutes les tuiles du paquet (`OmniCard`, `OmniSelectableCard`, `OmniStatTile`, `OmniFactTile`, `OmniNavTile`) obéissent à une règle de taille commune (demande d'une application cliente, R1-20), portée par `OmniTileGrid` : une grille CSS dont les tuiles sont les enfants directs.

- **Largeur** : `Columns` colonnes égales (de 1 à 6, 4 par défaut), `Gap` pour l'espacement (`OmniSpacing.Medium` par défaut). Comme les colonnes d'`OmniRow`, la grille suit les seuils de fenêtre du paquet : une colonne sous 40 rem, au plus deux de 40 rem à 64 rem, toutes les colonnes à partir de 64 rem. Toute tuile a donc la même largeur, à l'espacement près.
- **Hauteur** : chaque rangée prend la hauteur de la plus haute tuile de la grille (`grid-auto-rows: 1fr`), et chaque tuile la remplit : des tuiles de contenus différents (avec ou sans `Detail`, description longue ou absente) ont exactement la même hauteur, sur toutes les rangées. `EqualHeights="false"` (`true` par défaut) lève cette égalité : chaque rangée prend la hauteur de sa propre plus haute tuile (`grid-auto-rows: auto`) et chaque tuile garde la hauteur de son contenu, alignée en haut de sa rangée (`align-items: start`, plus de `block-size: 100%`), si bien qu'une tuile de faits de deux lignes n'est pas étirée à la hauteur d'une voisine de huit. Largeurs et colonnes ne changent pas. `OmniSelectableCardGroup.Columns` n'expose pas ce paramètre : ses cartes gardent des hauteurs égales.
- **Tuile double** : `DoubleWidth="true"` (sur chacune des cinq tuiles) étale la tuile sur deux colonnes de la rangée. Elle ne le fait que là où la grille a deux colonnes ou plus : sur une colonne, elle est aussi large que les autres, et la grille n'ouvre jamais de colonne implicite qui déborderait.
- **Aucun débordement** : une tuile peut rétrécir sous son contenu (`min-inline-size: 0`) ; ses mots coupent ou passent à la ligne dans la tuile (`overflow-wrap: anywhere` ; la valeur et le libellé d'une `OmniStatTile` restent sur une ligne, coupés de points de suspension ; le libellé et la valeur d'une ligne d'`OmniFactTile` aussi, lus en entier dans l'infobulle ; son titre passe à la ligne), et le corps d'une `OmniCard` ou les lignes d'une `OmniFactTile` défilent (en-tête et pied, ou titre, restent) au lieu de déborder.
- **Tuile cliquable** : `OmniStatTile`, `OmniFactTile` (`Href`, `OnClick`) et `OmniCard` (`Href`, `OnClick`) peuvent être un seul lien ou un seul bouton sans changer de place : les classes de grille et de `DoubleWidth` restent sur l'élément racine (le lien ou le bouton), qui est l'enfant direct de la grille. Voir « Carte cliquable ».
- **Contenu aligné** : dans une tuile de 18 rem de contenu ou plus, les lignes d'une `OmniFactTile` (icône, libellé, valeur) partagent trois colonnes de largeurs fixes (icône 1,25 rem, libellé 6,5 rem, la valeur prend le reste), exprimées en rem et non en part de la tuile : les icônes, les libellés et les valeurs tombent sur les mêmes verticales d'une ligne à l'autre et d'une tuile à l'autre de la grille, une tuile `DoubleWidth` comprise, et suivent la taille du texte. Sous 18 rem (quatre tuiles sur une rangée), chaque ligne décide seule : libellé et valeur sur une même ligne quand ils tiennent ensemble, la valeur sous le libellé sinon. Voir [page-components.md](page-components.md), « Tuile de faits ».
- **Hors grille**, les tuiles gardent leur comportement : `OmniNavTile`, `OmniFactTile` et `OmniStatTile` prennent la hauteur de leur `OmniColumn`, une carte et une carte à choisir celle de leur contenu. `OmniSelectableCardGroup.Columns` applique la même règle aux cartes d'un groupe.

```razor
<OmniTileGrid Columns="4">
    <OmniStatTile Value="12,4 k" Title="Jetons" Detail="Réinitialisation à 18:00"><Icon><OmniIcon Name="OmniIconName.Database" /></Icon></OmniStatTile>
    <OmniStatTile Value="8,2 k" Title="Semaine" />
    <OmniNavTile Href="admin/utilisateurs" Title="Utilisateurs" Description="Comptes et rôles" DoubleWidth="true" />
    <OmniCard DoubleWidth="true"><ChildContent>Le contenu de la carte.</ChildContent></OmniCard>
</OmniTileGrid>
```

Le nombre de colonnes, l'espacement et `EqualHeights="false"` sont des classes (`omni-tile-grid--columns-N`, `omni-tile-grid--gap-*`, `omni-tile-grid--natural-heights`) : la grille n'écrit aucun attribut `style` (CSP stricte). Preuve : tests bUnit (`TileRuleAndBadgeWidthTests`), mesures en navigateur dans la vitrine (page « Mise en page »).

## Carte cliquable

Demande d'une application cliente (recette R1-20) : une tuile de grille qui mène quelque part ou déclenche une action, sans envelopper l'`OmniCard` d'un `div` ou d'un `a` natif (ce qui sortait la tuile de la règle de taille).

- **`Href`** (`string?`) : la carte entière est un seul vrai lien (`a`). L'adresse passe par la politique d'URI du paquet, comme `OmniNavTile.Href` : un schéma dangereux tel que `javascript:` lève `InvalidOperationException`. Un lien accepte du contenu de flux : l'en-tête, le corps et le pied sont des `div`, et un titre (`h3`) placé dans l'en-tête reste exposé aux technologies d'assistance.
- **`OnClick`** (`EventCallback<MouseEventArgs>`, le même type que `OmniStatTile.OnClick`) : la carte entière est un seul vrai bouton (`button type="button"`), qui répond à `Entrée` et `Espace`. Un bouton n'accepte que du contenu phrasé : l'en-tête, le corps et le pied sont des `span` dessinés en blocs, et `HeaderContent`, `ChildContent` et `FooterContent` ne doivent contenir que du texte, des `span`, un `OmniIcon`, pas de titre, de paragraphe ni de `div`. Un bouton expose son contenu comme son nom accessible (ses descendants ne sont pas des éléments séparés) ; un `aria-label` passé en attribut supplémentaire le raccourcit.
- **Les deux ensemble** lèvent `ArgumentException` (`Href` ou `OnClick`, pas les deux). Sans l'un ni l'autre, le rendu est exactement celui d'avant (`section`).
- **Aucun élément interactif à l'intérieur** : un bouton, un lien ou un champ dans une carte cliquable est une imbrication invalide, que le composant ne détecte pas. Une carte dont le pied porte des actions reste une carte simple.
- **Apparence** : la racine est focalisable au clavier avec l'anneau de focus du paquet (`--omni-focus-ring`, comme `OmniStatTile`) ; au survol, le fond prend la teinte de la tuile de statistique cliquable. Le bouton reprend la police, la couleur et l'alignement de la carte, prend toute la largeur de sa cellule et toute sa hauteur dans une grille. Le corps d'une carte cliquable est coupé au lieu de défiler (une zone défilante dans un lien ou un bouton est un arrêt de focus imbriqué) : la carte résume, le détail est derrière le clic. Aucun attribut `style`, aucun script.

```razor
<OmniTileGrid Columns="4">
    <OmniCard OnClick="Ouvrir" DoubleWidth="true">
        <HeaderContent>Dossiers</HeaderContent>
        <ChildContent>12 dossiers à traiter</ChildContent>
    </OmniCard>
    <OmniCard Href="admin/utilisateurs">
        <HeaderContent><span>Utilisateurs</span></HeaderContent>
        <ChildContent><div>Comptes et rôles</div></ChildContent>
    </OmniCard>
    <OmniStatTile Href="admin/jetons" Value="12,4 k" Title="Jetons" />
</OmniTileGrid>
```

Preuve : tests bUnit (`ClickableTilesTests`) pour la racine, les attributs, le clic, les classes de grille, l'adresse refusée et les deux paramètres ensemble ; les tailles mesurées et l'anneau de focus se vérifient en navigateur dans la vitrine (page « Mise en page »).

## Accordéon

`OmniAccordion` range des `OmniAccordionItem` dans une liste bordée, aux lignes séparées par un filet neutre. Une ligne se compose d'un en-tête et d'un corps qui s'ouvre dessous :

```razor
<OmniAccordion Level="OmniHeadingLevel.H3">
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
| `Level` | `OmniHeadingLevel` | `H3` | Niveau du titre (`role="heading"`, `aria-level`) qui enveloppe chaque bouton de bascule, à choisir selon le plan de la page. |
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

## Corps de page : l'espacement entre les blocs

Les blocs frères d'une page (en-tête de page, alertes, cartes, grilles de tuiles, grilles de données, onglets)
sont séparés par le conteneur, jamais par une marge posée sur chaque bloc : le corps de la page est un
`OmniStack` sans paramètre, vertical et à `Gap` `Medium` (`--omni-space-md`, qui suit la densité). Aucune racine
de bloc du paquet ne porte de marge extérieure ; la seule, celle de `OmniPageHeader` (`--omni-space-lg` sous
lui hors d'une pile), est annulée dans un `OmniStack`, si bien que chaque écart entre deux blocs frères vaut le
même jeton, de l'en-tête au dernier bloc. Une section qui demande plus d'air est un `OmniStack` imbriqué ou un
`Gap` plus grand sur la pile, pas une marge.

```razor
<OmniStack>
    <OmniPageHeader Title="Exécution 2" />
    <OmniAlert Severity="OmniSeverity.Info">…</OmniAlert>
    <OmniTileGrid Columns="4">…</OmniTileGrid>
    <OmniDataGrid … FillAvailableHeight="true" />
</OmniStack>
```

## Séparateur

`OmniDivider` dessine un filet fin entre deux blocs distincts, typiquement entre les enfants d'un `OmniStack` : le récapitulatif d'un assistant sépare ainsi le projet, l'architecture, les pipelines et ce qui reste à faire avant la création.

```razor
<OmniStack Gap="OmniSpacing.Medium">
    <p>Projet</p>
    <OmniDivider />
    <p>Architecture</p>
</OmniStack>

<OmniStack Orientation="OmniOrientation.Horizontal" Gap="OmniSpacing.Medium">
    <span>Brouillon</span>
    <OmniDivider Orientation="OmniOrientation.Vertical" />
    <span>Publié</span>
</OmniStack>
```

| Paramètre | Type | Défaut | Rôle |
| --- | --- | --- | --- |
| `Orientation` | `OmniOrientation` | `Horizontal` | Direction du filet lui-même, pas de la pile qui le porte : `Horizontal` court sur la largeur et sépare des blocs empilés, `Vertical` court sur la hauteur et sépare des blocs côte à côte. |

- **Balisage** : un seul `div` avec `role="separator"` et `aria-orientation` (`horizontal` ou `vertical`), sans contenu ni style en ligne. Un `div` plutôt qu'un `hr` : l'`hr` apporte ses marges et sa bordure, qu'un filet vertical devrait défaire, et un seul élément garde le balisage, l'état ARIA et le style identiques dans les deux sens. `Id`, `Class`, `PresetName` et les attributs supplémentaires de `OmniComponentBase` passent sur cet élément. Une valeur hors de l'énumération est dessinée à l'horizontale.
- **Espacement** : le séparateur n'a aucune marge ; c'est l'écart (`Gap`) de la pile qui le porte qui l'espace du bloc précédent et du suivant. Il s'étire sur l'axe transversal de la pile (`align-self: stretch`) et ne rétrécit pas sur l'axe principal.
- **Aspect** : un filet de 1 px de la couleur de bordure du thème (`--omni-color-border`), sans filet coloré sur le côté ; un filet vertical a une hauteur minimale de `1em` quand la rangée n'en impose aucune.

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

- Le bouton (`ActionText`, « Supprimer » par défaut ; `Icon`, un fragment, en général une `OmniIcon` ; `OmniIconName.Delete` par défaut) ouvre `OmniOverlayService.ConfirmAsync` : titre `ConfirmTitle` (par défaut le `Title` de l'action), question `ConfirmMessage` (par défaut « Cette action est irréversible. Voulez-vous continuer ? » ; mieux vaut y nommer ce qui disparaît), action `ConfirmText` (par défaut le texte du bouton) en `Danger`, avec la même icône, puis Annuler. Sans `OmniComponentsHost` autour, la question est celle du navigateur (`confirm`), comme pour `OmniUnsavedChangesGuard`.
- Seule la confirmation appelle `OnConfirm` ; Annuler, Échap, le bouton de fermeture et le fond ne font rien, et le dialogue rend le focus au bouton. Le bouton reste occupé (`omni-busy`, `aria-busy`, jamais `disabled`, STD-BUSY) jusqu'à la fin de la tâche ; un clic pendant ce temps est ignoré.
- Un échec d'`OnConfirm` va à `OnConfirmError`, qui reçoit l'exception (à l'hôte de le dire, par exemple par `OmniOverlayService.Notify`) ; sans gestionnaire, l'exception remonte au moteur de rendu, comme pour `OmniMarkdownExportButton.OnExportError`. Une annulation (`OperationCanceledException`) n'y passe jamais.
- `Disabled` désactive le bouton (action interdite à cet utilisateur).
- Le bouton garde son texte visible pour nom accessible ; il est décrit (`aria-describedby`) par le nom et la description de la tuile, donc un lecteur d'écran entend quelle action il lance. `Id` va sur le bouton, avec les attributs supplémentaires ; `Class` sur la tuile.
- Le paquet n'a pas de saisie de confirmation (taper le nom du projet) : la confirmation est le dialogue du paquet.
