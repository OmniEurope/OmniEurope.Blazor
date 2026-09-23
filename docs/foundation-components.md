# Composants de fondation

Ce lot fournit 15 composants. Ils produisent du HTML sémantique, refusent les attributs `style` et les gestionnaires HTML sous forme de chaîne, et utilisent uniquement la feuille CSS statique de la bibliothèque.

## Capacités

| Besoin | Capacité OmniEurope |
|---|---|
| Titres et contenu textuel | `OmniText`, `OmniHeading` |
| Icônes et badges | `OmniIcon`, `OmniBadge` |
| Rangées, colonnes et grille | `OmniRow`, `OmniColumn`, `OmniGrid` |
| Coque applicative | `OmniLayout`, `OmniBody`, `OmniMain`, `OmniHeader` |
| Barre latérale et sa bascule | `OmniSidebar`, `OmniSidebarToggle` |
| Progression linéaire ou circulaire | `OmniProgressBar` avec `Shape` |
| Thème, palette, densité et apparence | `OmniThemeScope` (`Preset`, un thème de `OmniThemePresets`, `Palette`, une palette de `OmniThemePalettes`, et `Density`) et `OmniAppearanceToggle` |

| Composant | Rôle |
| --- | --- |
| `OmniText` | Texte rendu en `span`, `p`, `strong`, `em` ou `small`, avec tons et troncature statiques. |
| `OmniHeading` | Titres `h1` à `h6` déterminés par `OmniHeadingLevel`. |
| `OmniIcon` | Tracés Phosphor `regular` intégrés pour les usages du paquet, décoratifs par défaut ou nommés avec `AriaLabel` ; `Glyph` accepte n'importe quel autre tracé sans alourdir le paquet. |
| `OmniBadge` | Étiquette courte avec variantes neutre, accent, information, succès, avertissement et danger, en pastille tonale (fond, trait et texte tirés de l'encre de la variante mêlée à la surface et au texte du thème) lisible en clair comme en sombre ; `Fill="Outline"` n'en garde que le trait, `Fill="Solid"` prend le fond et l'encre du bouton de même intention. |
| `OmniLink` | Lien natif ; un nouvel onglet ajoute automatiquement `noopener noreferrer`. |
| `OmniImage` | Image responsive avec texte alternatif, chargement différé et dimensions natives optionnelles. |
| `OmniSkeleton` | État de chargement décoratif ou région `status` nommée, avec une à dix lignes. |
| `OmniRow` | Rangée flex avec espacement, alignement, justification et retour à la ligne typés. |
| `OmniColumn` | Colonne sur douze unités avec variantes responsive `SmallSpan`, `MediumSpan` et `LargeSpan`. |
| `OmniGrid` | Grille CSS de une à douze colonnes avec espacement typé. |
| `OmniLayout` | Conteneur de page pleine largeur, large ou centré sur le contenu : `Width` rétrécit toute la coquille, en-tête et barre latérale compris. |
| `OmniMain` | Landmark `main`, ciblable par un lien d'évitement grâce à `FocusTarget`. `ContentWidth` centre le contenu seul (`Wide`, 90rem, ou `Content`, 72rem) ; `Scrollable` en fait le conteneur de défilement de la page. |
| `OmniHeader` | Landmark `header`, avec position collante optionnelle définie dans la feuille statique. `Brand` (nom de l'application, en gras) et `BrandMark` (logo : un texte court dans un carré arrondi du remplissage d'accent, décoratif) ouvrent la barre ; une bascule de barre latérale posée dans l'en-tête reste dessinée au bord, avant eux. |
| `OmniFieldset` | Groupe de champs natif avec `legend` obligatoire et état désactivé ; `Collapsible` le replie avec l'élément natif `details`. `CollapsedChanged` (facultatif) rapporte l'état replié quand le lecteur ouvre ou ferme le groupe, ce qui permet `@bind-Collapsed` : l'événement natif `toggle` est écouté par `omni-focus.js`, sans gestionnaire en ligne, et seulement si le paramètre a un délégué ; sans lui, le groupe est rendu et se comporte comme avant, sans script. |
| `OmniProgressBar` | Progression linéaire ou circulaire, déterminée ou indéterminée, avec valeurs ARIA. L'indéterminée linéaire glisse d'un mouvement continu, sans arrêt ni retour ; sans mouvement demandé, la piste se remplit à demi-teinte. |

## Barre d'application

Les pièces de la barre supérieure de la maquette PLAN-008 sont dans le paquet, sans feuille de l'hôte :

- logo et nom : `OmniHeader.BrandMark` et `OmniHeader.Brand` ; sur le bandeau d'accent (`Tone="Accent"`), le logo inverse son fond et son encre ;
- recherche : `OmniTextBox` avec `Icon` (voir `docs/form-components.md`) ;
- pastille de la cloche : `OmniButton.Indicator` pose un point du remplissage de danger au coin haut de fin du bouton, cerné de la surface (de l'accent sur le bandeau). Il est décoratif (`aria-hidden`) : ce qu'il signale va dans le nom accessible, par exemple `AriaLabel="Notifications, 3 non lues"` ;
- avatar et menu de compte : `OmniProfileMenu` sans `Summary`, avec `Initials` et `Header`, et `OmniProfileMenuItem` avec `Icon` et `Description` (voir `docs/selection-components.md`).

```razor
<OmniHeader Brand="Aetheus" BrandMark="Ae">
    <OmniSidebarToggle Controls="menu" Open="open" OpenChanged="@(value => open = value)" AriaLabel="Menu" />
    <OmniTextBox Type="OmniTextBoxType.Search" aria-label="Rechercher" @bind-Value="search">
        <Icon><OmniIcon Name="OmniIconName.Search" /></Icon>
    </OmniTextBox>
    <OmniButton Variant="OmniButtonVariant.Ghost" Indicator="true" AriaLabel="Notifications, 3 non lues">
        <OmniIcon Name="OmniIconName.Bell" />
    </OmniButton>
    <OmniProfileMenu Label="Compte de Sony Tumen" Initials="ST">
        <Header><strong>Sony Tumen</strong><span>Administrateur</span></Header>
        <ChildContent>
            <OmniProfileMenuItem Description="Thème, langue, notifications">
                <Icon><OmniIcon Name="OmniIconName.Settings" /></Icon>
                <ChildContent>Paramètres</ChildContent>
            </OmniProfileMenuItem>
        </ChildContent>
    </OmniProfileMenu>
</OmniHeader>
```

La mise en page de la barre (où la recherche se place, l'écart entre les actions) reste celle de l'hôte.

## Icônes

Les tracés intégrés proviennent du poids `regular` de [Phosphor Icons](https://github.com/phosphor-icons/core) `2.1.1` (MIT), repris tels quels. Le paquet embarque un catalogue choisi : les tracés qu'il dessine lui-même et chaque icône que ses applications affichent, un tracé par valeur d'`OmniIconName`. Le catalogue complet n'est jamais distribué ; une application à qui il manque une icône l'ajoute au catalogue, par son nom.

Pour un tracé ponctuel, Phosphor ou non, le paramètre `Glyph` reste disponible.

```razor
@* Une icône du catalogue. *@
<OmniIcon Name="OmniIconName.Save" AriaLabel="Enregistrer" />

@* Un tracé ponctuel, ici une icône Phosphor absente du catalogue. *@
<OmniIcon Glyph="@Compass" AriaLabel="Boussole" />

@code {
    private static readonly OmniIconGlyph Compass = OmniIconGlyph.Phosphor(
        "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm0,192a88,88,0,1,1,88-88A88.1,88.1,0,0,1,128,216Zm50.34-138.34a8,8,0,0,0-8.68-1.73l-56,24a8,8,0,0,0-4.2,4.2l-24,56a8,8,0,0,0,10.41,10.51l56-24a8,8,0,0,0,4.2-4.2l24-56A8,8,0,0,0,178.34,77.66ZM140,140l-33.42,14.32L120.9,120.9,154.32,106.6Z");
}
```

`OmniIconGlyph` n'accepte que de la donnée de tracé SVG et rejette tout le reste : un glyphe porte un contour, pas du balisage. `OmniIconGlyph.Phosphor` fixe la grille de 256 unités du jeu ; le constructeur accepte une autre grille carrée.

Mesure du surcoût des onze tracés Phosphor, publication WebAssembly identique avant et après : **+6656 octets bruts, +1392 octets une fois compressés en brotli**, soit +0,9 % de l'assembly livré. Passage du catalogue à 86 icônes, `OmniEurope.Blazor.dll` en Release avant et après (2026-09-13) : **+57 856 octets bruts, +11 318 octets en brotli** (niveau maximal), soit +5,9 % de l'assembly compressé. Passage à 162 icônes (2026-09-14) : l'assembly Release mesuré par `eng/Test-Budgets.ps1` passe de 778 240 à 839 168 octets bruts, **+60 928 octets** ; la taille brotli n'a pas été mesurée à cette étape. Passage à 170 icônes (2026-09-14) : huit tracés de plus ; `OmniEurope.Blazor.dll` de `bin/Release` à 966 144 octets bruts, mesure qui inclut tout ce qui a été fusionné depuis l'étape précédente et ne s'y compare donc pas. Une garde de test échoue si le nombre de tracés intégrés diffère de celui des valeurs d'`OmniIconName`, afin qu'un import massif du catalogue ne passe pas inaperçu.

## Thèmes, palettes et densité

Un **thème** décide la forme : arrondis, épaisseur et couleur relative des bordures, ombres et lueurs, polices (piles système seulement, aucune webfont), dessin des boutons, des cartes et des titres, et l'effet d'appui des boutons. Il n'écrit aucune couleur en dur : une bordure ou une lueur colorée se dit par rapport à un jeton (`var(--omni-color-accent)`, `color-mix(...)`), si bien qu'elle suit n'importe quelle palette. Une **palette** décide les couleurs : accent (et accent sombre), succès, information, avertissement, danger, surface et texte des deux modes. La fabrique du paquet en dérive les jetons de chaque mode et les déplace jusqu'aux ratios WCAG.

Toute palette peint tout thème : dix thèmes par dix palettes, cent combinaisons, deux cents jeux de jetons avec les deux modes.

| Thème | Signature de forme | Palette par défaut |
|---|---|---|
| Essentiel | Un seul arrondi de 2,5 px partout, bordure de 1 px, élévation discrète, sans empattement, titres en 600 | Défaut |
| Ardoise | Angles vifs, aucune ombre, boutons et titres en capitales espacées | Océan |
| Galet | Boutons pilule, grandes cartes sans bordure, liseré et ombre diffuse, police arrondie | Forêt |
| Halo | Grandes rondeurs, boutons pilule, halo coloré tiré de l'accent, bordure teintée | Lavande |
| Néon | Rayon court, lueurs d'accent, bordure à 45 % d'accent, capitales très espacées | Électrique |
| Papier | Angles vifs, filets de 2 px couleur du texte, aucune ombre, empattements | Or ancien |
| Rétro | Contours de 2 px, ombres dures décalées sans flou, capitales grasses | Braise |
| Octet | Angles vifs, cadres en escalier, police d'écran, titres console en capitales | Mono |
| Nénuphar | Coins asymétriques en feuille, lueur douce d'accent, cartes sans bordure | Lagune |
| Velours | Biseaux à reflet interne, ombres profondes, titres à empattements | Prune |

| Palette | Accent clair | Allure |
|---|---|---|
| Défaut | `#4340d2` | Indigo en clair, violet en sombre, sévérités franches |
| Océan | `#0b63ce` | Bleu franc sur fond d'écume, nuit marine en sombre |
| Forêt | `#2f6f4f` | Vert sapin sur fond de mousse, sous-bois en sombre |
| Lavande | `#7c5cbf` | Violet lavande sur blanc lilas, nuit mauve en sombre |
| Électrique | `#008c9e` | Cyan électrique et violet, nuit d'encre en sombre |
| Or ancien | `#8a6a1c` | Or bruni sur papier crème, brun chaud en sombre |
| Braise | `#c2410c` | Orange braise sur fond pêche, rougeoiement en sombre |
| Mono | `#000000` | Noir et blanc purs, seules les sévérités en couleur |
| Lagune | `#0e8a7a` | Turquoise de lagune, bleu-vert profond en sombre |
| Prune | `#7b2d6e` | Prune et rose, velours sombre en sombre |

### Combiner un thème et une palette

`OmniThemePresets.All` donne les dix thèmes, chacun peint de sa palette par défaut, Essentiel en premier ; `OmniThemePalettes.All` donne les dix palettes. Sur une `OmniThemeScope` :

- `Preset` seul : le thème avec sa palette par défaut ;
- `Preset` et `Palette` : la forme du thème, les couleurs de la palette ;
- `Palette` seule : les couleurs de la palette sur la forme livrée ;
- ni l'un ni l'autre : l'apparence livrée, sans aucun script.

`OmniThemePreset.With(palette)` fait la même combinaison en code : les jetons de la palette, puis `Shape` (la forme, posée sur les deux modes), puis `DarkShape` (les réglages propres au sombre, posés sur le seul mode sombre ; Galet, Halo, Papier et Nénuphar y relèvent leurs cartes, les six autres thèmes n'en ont pas). Le nom et la description restent ceux du thème. Un preset écrit à la main (`new OmniThemePreset(nom, description, clair, sombre)`) a une `Shape` et une `DarkShape` vides.

```razor
<OmniThemeScope Appearance="OmniAppearance.System"
                Preset="@Halo"
                Palette="@Braise"
                Density="OmniDensity.Compact">
    ...
</OmniThemeScope>

@code {
    private static readonly OmniThemePreset Halo = OmniThemePresets.All.Single(theme => theme.Name == "Halo");
    private static readonly OmniThemePalette Braise = OmniThemePalettes.All.Single(palette => palette.Name == "Braise");

    // Même combinaison, calculée une fois : Preset="@HaloBraise", sans Palette.
    private static readonly OmniThemePreset HaloBraise = Halo.With(Braise);
}
```

Le thème ne repeint que sa portée. Les valeurs sont des surcharges des variables de la feuille, écrites par le CSSOM (`omni-theme.js`), jamais par un attribut `style` ; le mode suit `Appearance`, et `System` suit le réglage du système quand il change. Les variables de forme (`--omni-button-*`, `--omni-card-*`, `--omni-heading-*`, `--omni-border-width`) valent par défaut l'apparence livrée : une application peut aussi les redéfinir elle-même.

### L'apparence livrée

Sans `OmniThemeScope`, ou avec une portée sans thème ni palette, la page a l'aspect du thème Défaut avec la palette Défaut. Ces jetons ne sont pas écrits à la main : ils sont générés par la fabrique entre les marqueurs `omni:theme-tokens` de la feuille, pour le clair (`:root`, `[data-omni-theme="light"]`), le sombre et le mode système, et `ShippedThemeTokensTests` refuse toute retouche manuelle. Chaque jeton de couleur a donc sa valeur sombre.

### Jetons de couleur

Chaque sévérité (succès, information, avertissement, danger) et l'accent ont un jeton de texte (`--omni-color-X`, tenu à 4,5 sur la page et sur sa teinte pâle `-subtle`) et un jeton de remplissage (`--omni-color-X-fill`, avec `-fill-hover`, `-fill-active` et son encre `--omni-color-on-X-fill`) : le remplissage ne bouge que jusqu'à 3 contre la page, et c'est l'encre posée dessus qu'on pousse à 4,5, si bien que la couleur de marque reste reconnaissable. Les intentions ont en plus leurs fonds pleins : `--omni-color-info-bright` et `--omni-color-success-bright` avec l'encre `--omni-color-on-bright`, `--omni-color-warning-deep` et `--omni-color-danger-deep` avec `--omni-color-on-deep`, chacun avec son survol et son appui. Le bouton secondaire lit `--omni-color-neutral-fill`. La règle d'usage est dans [ui-conventions.md](ui-conventions.md). `ThemeContrastMatrixTests` vérifie 64 paires sur chacun des 200 jeux (4,5 pour un texte, 3 pour un remplissage ou l'accent contre la page, plancher de 1,7 pour une bordure).

### Densité

`OmniDensity` a trois valeurs : `Compact`, `Comfortable` (par défaut) et `Spacious`. `OmniThemeScope.Density` règle toute la portée ; `OmniButton`, `OmniFormField`, `OmniCard`, `OmniAlert`, `OmniTabs`, `OmniPanelMenu`, `OmniProfileMenu`, `OmniContextMenu`, `OmniSettingsTile` et `OmniUpload` ont leur propre `Density`, nulle par défaut pour hériter, qui l'emporte pour le composant et tout ce qu'il contient. `OmniDataGrid.Density` et `OmniResourceList.Density` gardent leur valeur propre, non nulle. La densité est un attribut `data-omni-density` qui pose des jetons hérités (hauteur de contrôle de 1,625, 2,25 ou 2,75 rem, marges, cases du calendrier, taille des cases à cocher, des interrupteurs et des icônes d'alerte), lus par chaque composant qui a une taille propre.

### Réglages d'apparence réutilisables

`OmniAppearanceSettings` rassemble mode clair/sombre/système, thème, palette, taille du texte et
densité. Modifier ouvre les deux derniers réglages dans une fenêtre déplaçable sans voile.
Ses mesures sont capturées à l'ouverture : les commandes restent stables pendant les changements,
puis prennent la nouvelle échelle à la prochaine ouverture. `ScaleEditorOpenChanged` informe l'hôte
afin qu'il puisse retirer son éventuel voile de menu. Le reste de l'application garde son échelle active.
Le thème et la palette de référence se nomment « Essentiel » ; les hôtes qui ont stocké l'ancien nom « Défaut »
doivent le traiter comme un alias lors de la restauration de leurs préférences.
La taille du texte et la densité proposent les niveaux 1 à 10. Le contrôle reçoit les valeurs
et émet leurs changements ; l'application conserve
la responsabilité du stockage et les applique à sa portée. Le mode Système et les boutons Défaut
restaurent les valeurs initiales.
La palette du thème est nommée dans le sélecteur, par exemple « Océan (défaut) » pour Ardoise ;
`OmniThemePresets.DefaultPaletteFor(preset)` fournit cette valeur. `Compact` réduit le panneau pour
un menu d'en-tête. Pour la densité, l'application peut associer les niveaux 1 à 3 à `Compact`,
4 à 7 à `Comfortable` et 8 à 10 à `Spacious`. La taille du texte se pilote par les jetons de police
du site ; le composant ne modifie pas la racine du document à l'insu de son hôte. Pour reproduire
l'échelle d'Atlas sans CSS propre à l'application, l'hôte pose `data-oe-text-size` (1 à 10) sur
`<html>` : la feuille OE applique alors 75 % à 131,25 % à la taille racine, 5 valant 100 %.
Le module `./_content/OmniEurope.Blazor/omni-appearance.js` expose `setTextSizeLevel(level)` et
`clearTextSizeLevel()` pour poser ou retirer cet attribut. La démonstration du paquet applique
le niveau choisi et le retire en quittant la page ; un aperçu vivant montre aussi le thème,
la palette et la densité sélectionnés, à côté des exemples de combinaisons fixes.
## Largeur du contenu et défilement

`OmniLayout.Width` rétrécit toute la coquille. Pour garder l'en-tête et la barre latérale sur toute la largeur et ne centrer que le contenu, c'est `OmniMain.ContentWidth`. Avec `Scrollable`, l'élément principal devient le conteneur de défilement de la page : il couvre toute la zone laissée par la barre latérale, si bien que sa barre de défilement reste au bord droit de la fenêtre, jamais au bord de la colonne centrée. La colonne se centre par la marge intérieure de l'élément et comprend sa gouttière, `--omni-main-gutter` (l'espacement moyen par défaut). Un enfant à `block-size: 100%` remplit exactement la zone ; un contenu plus haut la fait défiler, marge du bas comprise.

La coquille doit avoir une hauteur bornée : une `OmniLayout` dont le corps porte un élément principal défilant devient une colonne, et l'hôte lui donne sa hauteur, par exemple celle de la fenêtre. Les deux plafonds se lisent dans `--omni-layout-wide-width` et `--omni-layout-content-width`, qu'un hôte peut poser sur un ancêtre.

```razor
<OmniLayout Class="app-shell">  @* .app-shell { block-size: 100dvh; } *@
    <OmniHeader>...</OmniHeader>
    <OmniBody>
        <OmniSidebar Open="true" AriaLabel="Navigation">...</OmniSidebar>
        <OmniMain Scrollable="true" ContentWidth="OmniLayoutWidth.Content">@Body</OmniMain>
    </OmniBody>
</OmniLayout>
```

## Exemple

```razor
<OmniMain Id="content" AriaLabelledBy="page-title">
    <OmniHeading Id="page-title" Level="OmniHeadingLevel.H1">
        Importation
    </OmniHeading>

    <OmniProgressBar Label="Importation des données"
                     Value="42"
                     ShowValue="true" />
</OmniMain>
```

## Bande d'états et écran de démarrage

`OmniStatusStrip` (famille Feedback) aligne des états : les dernières exécutions d'une tâche en points
(`Shape="OmniStatusStripShape.Dot"`, par défaut) ou les tranches d'une fenêtre de disponibilité en
segments qui se partagent la largeur (`Segment`). Les états sont les mots de l'hôte (`success`,
`failed`, `up`...) : `Tones` associe à chacun une `OmniBadgeVariant` (`Neutral` pour un état absent),
`Pulsing` nomme ceux qui pulsent (ce qui est encore en cours ; le mouvement réduit l'arrête). Chaque
`OmniStatusStripItem` porte son `Label`, nom accessible et infobulle, et peut mener quelque part :
`Href` en fait un lien (adresse vérifiée, un schéma dangereux lève), `OnItemClick` fait des autres des
boutons natifs, sinon ce sont des marques `role="img"`. La liste est nommée par `Label` (« Historique des
états ») ; sans élément, `EmptyText` (« Aucun état à afficher »). Un lien ou un bouton garde une cible
de 44 px ; en segments, qui se partagent la largeur, la cible ne garde que ses 44 px de hauteur.

`OmniBootSplash` (famille Layout) retire l'écran de démarrage de la page une fois l'application rendue.
L'écran est écrit par l'hôte dans sa page, hors de l'élément où Blazor rend, pour paraître dès la
première image :

```html
<div id="omni-boot-splash" class="omni-boot-splash" role="status">
    <span class="omni-boot-splash__spinner" aria-hidden="true"></span>
    <span class="omni-visually-hidden">Chargement</span>
</div>
```

La feuille du paquet le dessine par-dessus la page, aux couleurs de l'apparence que `omni-boot.js` a
posée. Placé une fois dans la mise en page, `<OmniBootSplash />` l'efface en fondu puis le supprime après
le premier rendu, `OnHidden` recevant vrai s'il y en avait un (`SplashId`, `omni-boot-splash` par
défaut). Le retrait ne dépend pas de la fin de la transition : un minuteur de 600 ms le garantit dans un
onglet en arrière-plan, et le mouvement réduit supprime l'écran sans fondu. `omni-boot.js`, script
classique à inclure dans le `head` avant Blazor (aucun script en ligne, donc compatible
`script-src 'self'`), applique avant la première image l'apparence, le thème et la langue enregistrés
sous les clés que l'hôte nomme (`data-appearance-key`, `data-theme-key`, `data-language-key`) et
publie `window.OmniBoot` (`culture`, `hideSplash(id)`).

Preuves : `StatusStripAndBootSplashTests` (rôles et noms, tons, formes, liens, boutons, textes, appel du
retrait et son résultat).

## Validation

Les tests du lot vérifient le rendu des 15 composants, leur sémantique principale, les classes responsive, les états ARIA, les bornes numériques et l'absence de style inline. Le scanner CSP inspecte l'ensemble des sources Razor, C# et JavaScript de la bibliothèque.

## Classes utilitaires `omni-u-*`
 le balisage qu'une application écrit autour des composants, pas pour restyler un composant : chaque règle vise une seule classe utilitaire, jamais la classe d'un composant (garde `UtilityClassesTests`). Déclarées en fin de feuille, elles l'emportent sur une règle de composant de même spécificité.

| Famille | Classes |
|---|---|
| Espacement | `omni-u-{m,mt,mb,ms,me,mx,my,p,pt,pb,px,py}-{0,xs,sm,md,lg,xl,2xl}`, `omni-u-mx-auto`. L'échelle est celle des jetons `--omni-space-*` : elle suit la densité. |
| Texte | `omni-u-text-{muted,accent,success,warning,danger}`, `omni-u-text-center`, `omni-u-text-end`, `omni-u-bold`, `omni-u-mono`, `omni-u-truncate`, `omni-u-nowrap`, `omni-u-small` |
| Fond | `omni-u-bg-muted`, `omni-u-bg-{accent,success,warning,danger}-subtle` |
| Mise en page | `omni-u-w-100`, `omni-u-grow`, `omni-u-block`, `omni-u-rounded`, `omni-u-pointer` |
