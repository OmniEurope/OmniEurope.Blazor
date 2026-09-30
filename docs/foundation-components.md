# Composants de fondation

Ce lot fournit 15 composants. Ils produisent du HTML sémantique, refusent les attributs `style` et les gestionnaires HTML sous forme de chaîne, et utilisent uniquement la feuille CSS statique de la bibliothèque.

## Capacités

| Besoin | Capacité OmniEurope |
|---|---|
| Titres et contenu textuel | `OmniText`, `OmniHeading` |
| Icônes et badges | `OmniIcon`, `OmniBadge` |
| Piles, rangées et colonnes | `OmniStack`, `OmniRow`, `OmniColumn` |
| Coque applicative | `OmniLayout`, `OmniBody`, `OmniMain`, `OmniHeader` |
| Barre latérale et sa bascule | `OmniSidebar`, `OmniSidebarToggle` |
| Progression linéaire ou circulaire | `OmniProgressBar` avec `Shape` |
| Thème, palette, densité et apparence | `OmniThemeScope` (`Preset`, un thème de `OmniThemePresets`, `Palette`, une palette de `OmniThemePalettes`, et `Density`) et `OmniAppearanceSettings` |

| Composant | Rôle |
| --- | --- |
| `OmniText` | Texte rendu en `span`, `p`, `strong`, `em` ou `small`, avec tons et troncature statiques. |
| `OmniHeading` | Titres `h1` à `h6` déterminés par `OmniHeadingLevel`, sur l'échelle `--omni-font-size-h1` à `h6` (2, 1,5, 1,25, 1,125, 1 et 0,875 rem), nettement décroissante. Le titre d'`OmniPageHeader` suit son niveau, h1 par défaut. |
| `OmniIcon` | Tracés Phosphor `regular` intégrés pour les usages du paquet, décoratifs par défaut ou nommés avec `Label` ; `Glyph` accepte n'importe quel autre tracé sans alourdir le paquet. Sans `Size`, l'icône prend la taille que lui donne son conteneur (badge, bouton partagé, petit bouton), la taille moyenne ailleurs ; `Size` impose une taille fixe. |
| `OmniBadge` | Étiquette courte, texte en contenu enfant, teintée par `Tone` (`OmniTone` : `Neutral` par défaut, `Accent`, `Info`, `Success`, `Warning`, `Danger`). Par défaut (`Fill="OmniFill.Solid"`), elle prend le fond et l'encre du bouton de même intention ; `Fill="OmniFill.Tonal"` en fait une pastille tonale (fond, trait et texte tirés de l'encre du ton mêlée à la surface et au texte du thème) lisible en clair comme en sombre ; `Fill="OmniFill.Outline"` n'en garde que le trait. |
| `OmniLink` | Lien natif, nommé au besoin par `Label` ; un nouvel onglet ajoute automatiquement `noopener noreferrer` (fusionné avec un `rel` de l'hôte), une icône de lien externe et une mention pour les technologies d'assistance. `OnClick` exécute une action au clic en plus de la navigation, sans l'empêcher ; sans lui, aucun gestionnaire n'est attaché. |
| `OmniImage` | Image responsive avec texte alternatif, chargement différé et dimensions natives optionnelles. |
| `OmniSkeleton` | État de chargement décoratif ou région `status` nommée, avec une à dix lignes. |
| `OmniStack` | Pile flex verticale ou horizontale (`Orientation`) avec espacement, alignement et justification typés. `Wrap` (désactivé par défaut) fait passer les éléments à la ligne ; `Overflow` (`Scroll`, `Collapse`) garde au contraire une seule ligne et l'emporte sur `Wrap`. |
| `OmniRow` | Rangée flex des `OmniColumn`, avec espacement, alignement, justification et retour à la ligne typés. Hors colonnes, une `OmniStack` horizontale (`Wrap="true"` pour passer à la ligne) suffit. |
| `OmniColumn` | Colonne sur douze unités avec variantes responsive `SmallSpan`, `MediumSpan` et `LargeSpan`. |
| `OmniLayout` | Conteneur de page pleine largeur (en-tête, corps, barre latérale) ; la largeur du contenu se règle sur `OmniMain.ContentWidth`. |
| `OmniMain` | Landmark `main`, ciblable par un lien d'évitement grâce à `FocusTarget`. `ContentWidth` centre le contenu seul (`Wide`, 90rem, ou `Content`, 72rem) ; `Scrollable` en fait le conteneur de défilement de la page. |
| `OmniHeader` | Landmark `header`, avec position collante optionnelle définie dans la feuille statique. `Brand` (nom de l'application, en gras) et `BrandLogo` (logo en image, décoratif) ouvrent la barre ; `BrandHref` en fait un lien vers l'accueil ; une bascule de barre latérale posée dans l'en-tête reste dessinée au bord, avant eux. |
| `OmniFieldset` | Groupe de champs natif avec `legend` obligatoire et état désactivé ; `Collapsible` le replie avec l'élément natif `details`, déplié tant que `Expanded` vaut vrai (par défaut). `ExpandedChanged` (facultatif) rapporte l'état quand le lecteur ouvre ou ferme le groupe, ce qui permet `@bind-Expanded` : l'événement natif `toggle` est écouté par `omni-focus.js`, sans gestionnaire en ligne, et seulement si le paramètre a un délégué ; sans lui, le groupe est rendu et se comporte comme avant, sans script. |
| `OmniProgressBar` | Progression linéaire ou circulaire, déterminée ou indéterminée, avec valeurs ARIA. L'indéterminée linéaire glisse d'un mouvement continu, sans arrêt ni retour ; sans mouvement demandé, la piste se remplit à demi-teinte. |

## Barre d'application

Les pièces de la barre supérieure de la maquette de PLAN-004 (`docs/plans/archive/PLAN-004-maquette-themes.html`) sont dans le paquet, sans feuille de l'hôte :

- logo et nom : `OmniHeader.BrandLogo` et `OmniHeader.Brand` ;
- recherche : `OmniTextBox` avec `Icon` (voir `docs/form-components.md`) ;
- pastille de la cloche : `OmniButton.Indicator` pose un point du remplissage de danger au coin haut de fin du bouton, cerné de la surface (de l'accent sur le bandeau). Il est décoratif (`aria-hidden`) : ce qu'il signale va dans le nom accessible, par exemple `Label="Notifications, 3 non lues"` ;
- avatar et menu de compte : `OmniProfileMenu` sans `Summary`, avec `Initials` et `Header`, et ses `OmniMenuItem` avec `Icon` (voir `docs/selection-components.md`).

```razor
<OmniHeader Brand="Boutique" BrandLogo="img/logo.svg">
    <OmniSidebarToggle Controls="menu" Open="open" OpenChanged="@(value => open = value)" Label="Menu" />
    <OmniTextBox Type="OmniTextBoxType.Search" aria-label="Rechercher" @bind-Value="search">
        <Icon><OmniIcon Name="OmniIconName.Search" /></Icon>
    </OmniTextBox>
    <OmniButton Variant="OmniButtonVariant.Ghost" Indicator="true" Label="Notifications, 3 non lues">
        <OmniIcon Name="OmniIconName.Bell" />
    </OmniButton>
    <OmniProfileMenu Label="Compte de Camille Martin" Initials="CM">
        <Header><strong>Camille Martin</strong><span>Administrateur</span></Header>
        <ChildContent>
            <OmniMenuItem Href="/parametres">
                <Icon><OmniIcon Name="OmniIconName.Settings" /></Icon>
                <ChildContent>Paramètres</ChildContent>
            </OmniMenuItem>
        </ChildContent>
    </OmniProfileMenu>
</OmniHeader>
```

La mise en page de la barre (où la recherche se place, l'écart entre les actions) reste celle de l'hôte.

## Icônes

Les tracés intégrés proviennent du poids `regular` de [Phosphor Icons](https://github.com/phosphor-icons/core) `2.1.1` (MIT), repris tels quels. Le paquet embarque un catalogue choisi : les tracés qu'il dessine lui-même et chaque icône que ses applications affichent, un tracé par valeur d'`OmniIconName`. Le catalogue complet n'est jamais distribué ; une application à qui il manque une icône l'ajoute au catalogue, par son nom : une valeur en fin d'`OmniIconName` et une ligne dans `src/OmniEurope.Blazor/Internal/PhosphorIcons.txt` (le nom, une tabulation, le tracé). Ce fichier est incorporé tel quel à l'assembly, en UTF-8, et le paquet le lit une fois, au premier rendu d'une icône.

Pour un tracé ponctuel, Phosphor ou non, le paramètre `Glyph` reste disponible.

```razor
@* Une icône du catalogue. *@
<OmniIcon Name="OmniIconName.Save" Label="Enregistrer" />

@* Un tracé ponctuel, ici une icône Phosphor absente du catalogue. *@
<OmniIcon Glyph="@Compass" Label="Boussole" />

@code {
    private static readonly OmniIconGlyph Compass = OmniIconGlyph.Phosphor(
        "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm0,192a88,88,0,1,1,88-88A88.1,88.1,0,0,1,128,216Zm50.34-138.34a8,8,0,0,0-8.68-1.73l-56,24a8,8,0,0,0-4.2,4.2l-24,56a8,8,0,0,0,10.41,10.51l56-24a8,8,0,0,0,4.2-4.2l24-56A8,8,0,0,0,178.34,77.66ZM140,140l-33.42,14.32L120.9,120.9,154.32,106.6Z");
}
```

`OmniIconGlyph` n'accepte que de la donnée de tracé SVG et rejette tout le reste : un glyphe porte un contour, pas du balisage. `OmniIconGlyph.Phosphor` fixe la grille de 256 unités du jeu ; le constructeur accepte une autre grille carrée.

Mesure du surcoût des onze tracés Phosphor, publication WebAssembly identique avant et après : **+6656 octets bruts, +1392 octets une fois compressés en brotli**, soit +0,9 % de l'assembly livré. Passage du catalogue à 86 icônes, `OmniEurope.Blazor.dll` en Release avant et après (2026-09-13) : **+57 856 octets bruts, +11 318 octets en brotli** (niveau maximal), soit +5,9 % de l'assembly compressé. Passage à 162 icônes (2026-09-14) : l'assembly Release mesuré par `eng/Test-Budgets.ps1` passe de 778 240 à 839 168 octets bruts, **+60 928 octets** ; la taille brotli n'a pas été mesurée à cette étape. Passage à 170 icônes (2026-09-14) : huit tracés de plus ; `OmniEurope.Blazor.dll` de `bin/Release` à 966 144 octets bruts, mesure qui inclut tout ce qui a été fusionné depuis l'étape précédente et ne s'y compare donc pas. Une garde de test échoue si le nombre de tracés intégrés diffère de celui des valeurs d'`OmniIconName`, afin qu'un import massif du catalogue ne passe pas inaperçu. Passage à 232 icônes (2026-09-27) : vingt-neuf tracés de lecture audio pour une application cliente, valeurs ajoutées en fin d'`OmniIconName`. Passage à 243 icônes (2026-09-27) : onze tracés de plus pour une application cliente, en fin d’`OmniIconName`. Catalogue sorti du code (2026-09-30, PLAN-010) : écrits en littéraux C#, les tracés coûtaient deux octets par caractère (UTF-16) ; en ressource UTF-8, `OmniEurope.Blazor.dll` en Release passe de 2 031 104 à 1 900 544 octets bruts avec le lot 2 (attributs de nullabilité réduits, −42 496 octets mesurés à part), soit **−88 064 octets** pour le catalogue. Une ressource compressée en Deflate gagnait 63 000 octets bruts de plus, mais sa lecture charge `System.IO.Compression` dans une application WebAssembly (+11 264 octets en brotli) et l'assembly compressé en brotli, ce que télécharge le navigateur, n'y gagnait que 7 407 octets : au total le téléchargement grossissait. La ressource reste donc non compressée ; `OmniEurope.Blazor.wasm` publié passe de 510 061 à 500 585 octets en brotli, lot 2 compris.

## Thèmes, palettes et densité

Un **thème** décide la forme : arrondis, épaisseur et couleur relative des bordures, ombres et lueurs, polices (piles système ou polices web servies par le paquet, voir le réglage Police), dessin des boutons, des cartes et des titres, et l'effet d'appui des boutons. Il n'écrit aucune couleur en dur : une bordure ou une lueur colorée se dit par rapport à un jeton (`var(--omni-color-accent)`, `color-mix(...)`), si bien qu'elle suit n'importe quelle palette. Une **palette** décide les couleurs : accent (et accent sombre), succès, information, avertissement, danger, surface et texte des deux modes. La fabrique du paquet en dérive les jetons de chaque mode et les déplace jusqu'aux ratios WCAG.

Toute palette peint tout thème : quinze thèmes par quinze palettes, deux cent vingt-cinq combinaisons, quatre cent cinquante jeux de jetons avec les deux modes. Ces nombres ne sont pas figés : les tests et les sondes lisent le catalogue, et un thème ou une palette de plus n'en casse aucun.

| Thème | Signature de forme | Palette par défaut |
|---|---|---|
| Essentiel | Un seul arrondi de 2,5 px partout, bordure de 1 px, élévation discrète, sans empattement, titres en 600 | Essentiel |
| Ardoise | Angles vifs, aucune ombre (cartes, boutons, alertes), boutons et titres en capitales espacées | Océan |
| Galet | Boutons pilule ombrés par-dessous et posés sur une ombre diffuse, grandes cartes sans bordure, liseré et ombre diffuse neutres, police arrondie | Forêt |
| Halo | Grandes rondeurs, boutons pilule, liseré et halo des cartes tirés de l'accent, bordure teintée | Lavande |
| Néon | Rayon court, lueurs d'accent resserrées en clair et larges en sombre, bordure à 45 % d'accent, capitales très espacées | Électrique |
| Papier | Angles vifs, filets de 2 px couleur du texte, aucune ombre (cartes, boutons, alertes), empattements | Or ancien |
| Rétro | Contours de 2 px, ombres dures décalées sans flou jusque sous les alertes pleines, capitales grasses | Braise |
| Octet | Angles vifs, cadres en escalier (cartes, boutons, alertes pleines), police d'écran, titres console en capitales | Mono |
| Nénuphar | Coins asymétriques en feuille, lueur douce d'accent, cartes sans bordure | Lagune |
| Velours | Cartes réchauffées par l'accent, reflet en haut et ombre profonde, boutons en coussin (éclairés dessus, ombrés dessous), titres à empattements | Prune |
| Relief | Néomorphisme : surfaces de la couleur de la page, paire d’ombres douces claire en haut à gauche et sombre en bas à droite, appui et champs en creux, police arrondie | Nuage |
| Givre | Verre dépoli : grandes taches pastel peintes par la portée sous le contenu, qui dérivent lentement, cartes dépolies (flou et saturation) à liseré clair, barre du haut et menu latéral en verre, calques flottants et voile de dialogue floutés, boutons pilule | Opale |
| Aplat | Design plat : aucune ombre ni dégradé, cartes pleines sans bordure, boutons pilule, appui qui ne fait que foncer, police géométrique | Pastel |
| Épure | Minimalisme : angles vifs, filets fins, aucune ombre, action principale à l’encre, très grands titres en 800, Inter | Encre |
| Trou noir | Page nue (noir pur avec Horizon en sombre), cartes à peine voilées fermées par un filet fin, anneau d'accent derrière la page dont la lueur tourne lentement, boutons qui luisent de l'accent en sombre, police géométrique | Horizon |

| Palette | Accent clair | Allure |
|---|---|---|
| Essentiel | `#4340d2` | Indigo dans les deux modes, sévérités franches |
| Océan | `#0b63ce` | Bleu franc sur fond d'écume, nuit marine en sombre |
| Forêt | `#2f6f4f` | Vert sapin sur fond de mousse, sous-bois en sombre |
| Lavande | `#7c5cbf` | Violet lavande sur blanc lilas, nuit mauve en sombre |
| Électrique | `#008c9e` | Cyan électrique et violet, nuit d'encre en sombre |
| Or ancien | `#8a6a1c` | Or bruni sur papier crème, brun chaud en sombre |
| Braise | `#c2410c` | Orange braise sur fond pêche, rougeoiement en sombre |
| Mono | `#000000` | Noir et blanc purs, seules les sévérités en couleur |
| Lagune | `#0e8a7a` | Turquoise de lagune, bleu-vert profond en sombre |
| Prune | `#7b2d6e` | Prune et rose, velours sombre en sombre |
| Nuage | `#2f6bff` | Bleu vif sur gris nuage, anthracite en sombre, rouge orangé en alerte |
| Opale | `#4f46e5` | Indigo franc sur blanc nacré, ciel, menthe et pêche en sévérités, nuit bleutée en sombre |
| Pastel | `#b04fd0` | Lilas et rose sur blanc, bleu marine en sombre, ciel en information |
| Encre | `#27466f` | Encre noire sur blanc cassé, un seul bleu d’encre discret |
| Horizon | `#d9660b` | Ambre d'accrétion sur blanc pur, noir absolu en sombre |

### Combiner un thème et une palette

`OmniThemePresets.All` donne les quinze thèmes, chacun peint de sa palette par défaut, Essentiel en premier ; `OmniThemePalettes.All` donne les quinze palettes. Sur une `OmniThemeScope` :

- `Preset` seul : le thème avec sa palette par défaut ;
- `Preset` et `Palette` : la forme du thème, les couleurs de la palette ;
- `Palette` seule : les couleurs de la palette sur la forme livrée ;
- ni l'un ni l'autre : l'apparence livrée, sans aucun script.

`OmniThemePreset.With(palette)` fait la même combinaison en code : les jetons de la palette, puis `Shape` (la forme, posée sur les deux modes), puis `DarkShape` (les réglages propres au sombre, posés sur le seul mode sombre ; Galet, Halo, Néon, Papier, Nénuphar, Velours, Relief, Givre et Trou noir y relèvent leurs cartes, leurs ombres ou leurs lueurs, et Aplat rapproche ses aplats clairs du texte clair ; Essentiel, Ardoise, Rétro, Octet et Épure n’en ont pas). Le nom et la description restent ceux du thème. Un preset écrit à la main (`new OmniThemePreset(nom, description, clair, sombre)`) a une `Shape` et une `DarkShape` vides.

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

### Relief, Givre, Aplat, Épure et Trou noir

Les quatre premiers reprennent l'esprit de quatre styles d'interface (néomorphisme, verre dépoli, design plat, minimalisme), redessinés avec les jetons du paquet ; Trou noir est une page nue que seul son accent éclaire.

Relief, Givre et Aplat sont marqués **contraste non garanti** (décision du propriétaire du 2026-09-28) : leur style prime sur les seuils de contraste. La raison est déclarée dans le catalogue (`ThemeDefinition.ContrastWaiver`) et exposée par `OmniThemePreset.ContrastWaiver`, que la vitrine affiche sous l'aperçu. Ces thèmes restent mesurés : `ThemeContrastMatrixTests` écrit leurs écarts dans la sortie du test au lieu d'échouer, et la sonde de contraste les compte sous `acceptedContrastWaiver`. Seul l'anneau de focus n'est jamais couvert : il reste plein et à 3:1 au moins dans tous les thèmes et avec toutes les palettes (`ThemePaletteTests`). Le texte et la bordure d'un contrôle focalisé suivent la dérogation comme les autres états (décision du 2026-09-28). La liste des thèmes marqués est figée dans le test : en marquer un de plus est une modification délibérée, et l'apparence livrée (Essentiel) ne peut pas l'être. Une application qui doit garantir les contrastes choisit un thème non marqué. Épure et Trou noir gardent les garanties complètes.

Ils lisent treize crochets de la feuille, neutres pour tout autre thème et remis à zéro sur chaque portée (`[data-omni-theme]`) pour qu'une portée imbriquée ne les hérite pas :

- `--omni-backdrop` : calques d'image peints par la portée derrière tout son contenu, fixés à la fenêtre (`none` par défaut). Givre y pose quatre très grandes taches en dégradés radiaux sans cœur dur, dimensionnées sur la fenêtre pour se recouvrir sous le contenu : `--omni-backdrop-middle` (l'accent), `--omni-backdrop-start` (l'information), `--omni-backdrop-end` (le succès) et la lueur `--omni-backdrop-glow` (l'avertissement en clair, le danger en sombre), sur un fond qui glisse de la page vers l'information. Trou noir y pose le disque de l'horizon à la couleur de la page, un anneau fin `--omni-backdrop-ring` et une lueur conique (`--omni-backdrop-disc`, `--omni-backdrop-glow`), tous très proches de la page (7 % d'accent au plus en clair, 20 % en sombre). Les contrôles de contraste mesurent chaque arrêt `--omni-backdrop-*` que le fond nomme ;
- `--omni-scope-motion` : l'animation de ce fond (`none` par défaut), décrite sous « Fond animé » ;
- `--omni-shell-background` : fond de la barre du haut (`.omni-header`) et du menu latéral (`.omni-sidebar__panel`), repli sur la surface de la page. Givre y pose un verre translucide, si bien que le champ de couleur court d'un bord à l'autre ; la barre et le menu portent alors le même dépoli que les cartes (un `::before` en `z-index: -1` qui applique `--omni-card-filter`), et un thème qui pose ce crochet pose donc aussi `--omni-card-filter`. Un menu latéral ouvert en surimpression, hors de ce dépoli, prend le verre dense du dialogue (`--omni-dialog-background`) ;
- `--omni-card-filter` et `--omni-scope-isolation` : le dépoli des cartes, en un seul crochet (`none` et `auto` par défaut). Toute surface peinte du fond de carte (carte, tuile de statistique, tuile de réglage, ligne d'apparence, carte à choisir, fichier d'un envoi) est positionnée et porte un pseudo-élément `::before` étiré sous elle, en `z-index: -1`, qui applique `--omni-card-filter` en `backdrop-filter` ; Givre y pose son givre et fait de la portée un contexte d'empilement (`--omni-scope-isolation: isolate`). Le pseudo-élément floute et sature le fond, et la carte pose par-dessus son remplissage translucide, son liseré et son reflet. Le filtre n'est jamais posé sur la carte, qui deviendrait le bloc conteneur des infobulles, menus et popovers fixes qu'elle contient, et la carte ne crée pas de contexte d'empilement, si bien que ses popovers passent toujours au-dessus des cartes suivantes ;
- `--omni-input-shadow` : ombre des champs (`.omni-input`, `.omni-password`), gardée sous l'anneau de focus ; Relief y creuse ses champs ;
- `--omni-input-border-color` et `--omni-input-background` : bordure et fond des mêmes champs (la bordure et la surface de la palette par défaut) ; Relief efface la bordure, Givre rend le fond translucide ;
- `--omni-grid-background` : fond du cadre d'une grille, repli sur `--omni-card-background` ; Aplat y garde la surface de la page, Givre un verre plus dense que ses cartes, lu ligne à ligne ;
- `--omni-alert-shadow` : ombre d'une alerte pleine, repli sur le relief et la lueur colorée du paquet. Une alerte pleine prend la forme de son thème : Ardoise, Papier, Aplat et Épure, dessinés sans ombre, l'effacent ; Rétro y met son ombre dure décalée et Octet son cadre en escalier. Le rayon suit la même règle par `--omni-alert-radius` (jeton de forme, 2,5 px livré) : tout thème qui arrondit ses cartes d'au moins 0,5 rem, ou qui a des angles vifs, pose le rayon de ses alertes (`ThemeFieldMotionTests`) ;
- `--omni-alert-fill-opacity` : part du remplissage d'une alerte pleine (100 % par défaut) ; Givre la ramène à 82 %, si bien que le fond se devine à travers l'alerte, verre teinté sous un liseré clair ;
- `--omni-scrim-filter` : filtre du voile d'un dialogue (`.omni-overlay`, `none` par défaut) ; le voile couvre toute la fenêtre, un filtre n'y déplace donc aucun descendant fixe ; Givre y floute la page sous un voile clair (`--omni-color-overlay` à 22 % de noir en clair) ;
- `--omni-dialog-background` : fond du dialogue, repli sur `--omni-card-background` ; Givre, dont les cartes sont translucides, lui donne un verre dense posé sur le voile flouté, et Aplat, dont les cartes sont un aplat de couleur, lui rend la surface de la page. Une fenêtre non modale n'a pas de voile sous elle : son fond est posé sur la surface opaque de la page, pour que la page ne se lise pas à travers.

Sans `backdrop-filter`, un calque flottant translucide retombe sur la surface opaque (`@supports not`).

#### Fond animé

Givre et Trou noir font bouger leur fond, et eux seuls (décision du propriétaire du 2026-09-30, figée dans `ThemeFieldMotionTests`). Le mouvement est en CSS pur, sans script :

- la feuille enregistre un angle, `@property --omni-scope-turn` (`<angle>`, non hérité, `0deg`), qu'une animation `omni-scope-turn` mène à 360 degrés ; `--omni-backdrop` est enregistré non hérité lui aussi, pour que ni l'angle ni le fond ne soient recalculés sur chaque élément de la portée à chaque image ;
- la portée porte `animation: var(--omni-scope-motion, none)` ; un thème qui bouge pose `--omni-scope-motion: omni-scope-turn <N>s linear infinite` (120 s pour Givre, 240 s pour Trou noir, jamais moins d'une minute) et lit `var(--omni-scope-turn)` dans son `--omni-backdrop` : l'orbite des taches de Givre, l'origine de la lueur conique de Trou noir ;
- le fond reste immobile, angle à zéro, quand `OmniThemeScope.BackdropMotion` vaut `false` (la portée pose `data-omni-backdrop-motion="off"`) et toujours sous `prefers-reduced-motion: reduce`. Un navigateur sans `@property` garde lui aussi le fond immobile.

`BackdropMotion` vaut `true` par défaut. `OmniAppearanceWindow` et `OmniAppearanceSettings` exposent `BackdropMotion` et `BackdropMotionChanged` : la ligne « Fond animé » (un interrupteur, texte dans les 24 langues) n'apparaît que sous un thème qui bouge son fond et seulement si l'hôte lie le changement ; l'hôte stocke la valeur et la remet à sa portée.

```razor
<OmniThemeScope Preset="@_preset" BackdropMotion="@_backdropMotion">
    <OmniAppearanceSettings @bind-Preset="_preset" @bind-BackdropMotion="_backdropMotion" />
</OmniThemeScope>
```

Coût mesuré dans la vitrine le 2026-09-30 (page Personnalisation, 1440 x 900, 180 images) : image médiane à 6,9 ms avec et sans mouvement sous Givre ; recalcul de style de 0,16 s en mouvement contre 0,05 s à l'arrêt sous Givre, 0,125 s contre 0,052 s sous Trou noir.

Le parti pris de chacun (sous les seuils pour les trois premiers) :

- Relief : cartes, champs et bouton secondaire ont la couleur de la page et ne se détachent que par une paire d'ombres douces marquées (claire en haut à gauche, sombre en bas à droite, la sombre tirée de la page assombrie) ; les champs n'ont plus de bordure et sont creusés par des ombres intérieures, l'appui passe en creux. En sombre, même logique sur l'anthracite de la palette.
- Givre : verre dépoli aéré. En clair, quatre très grandes taches pastel (lavande, ciel, menthe, pêche) sur un blanc froid, qui se recouvrent sous le contenu au lieu de rester dans les coins ; en sombre, des taches plus profondes sur une nuit bleutée, la lueur chaude prenant la teinte du danger, une aurore. Chaque tache dérive sur une petite orbite, un tour en deux minutes. La barre du haut et le menu latéral sont du même verre. L'accent reste une seule couleur franche et profonde, qui se détache de toutes les teintes du fond. Les cartes sont réellement dépolies : le fond est flouté et saturé sous elles (voir les crochets ci-dessus), puis la carte pose un remplissage laiteux translucide, un liseré clair de 1 px, un reflet en haut et une grande ombre douce ; les champs, le bouton secondaire et les grilles sont du même verre, plus dense pour les grilles. Libéré des seuils, seul l'anneau de focus reste garanti ; le texte atténué est un cran plus clair que le texte et reste lisible à l'œil sur les taches.
- Aplat : les cartes sont un aplat franc de l'accent, le bouton principal une pastille claire de l'accent à l'encre presque noire, le bouton secondaire un aplat de l'information, les grilles et le dialogue restent des panneaux de la page, cernés comme les menus d'un filet plat de 1 px qui les détache d'elle, et les alertes pleines perdent leur lueur ; le texte atténué reste celui de la palette.
- Épure : l'action principale est dessinée à l'encre du texte, son survol prend l'accent fort de la palette, seule touche de couleur avec les liens et l'onglet courant.
- Trou noir : la page est la surface de la palette et rien d'autre (noir pur avec Horizon en sombre, blanc pur en clair). Les cartes sont un voile de 4 % du texte fermé par un filet fin, si bien que le fond se voit à travers ; grilles et dialogues sont opaques, un cran au-dessus de la page. La seule lumière est l'accent : la lueur des boutons et des calques flottants en sombre, et l'anneau de l'horizon derrière la page, en haut à droite, dont la lueur fait un tour en quatre minutes. Le filet est plus ferme que celui de la palette (36 % du texte) pour tenir son plancher sur les arrêts du fond. Le thème garde les seuils de contraste avec toutes les palettes : l'anneau est donc volontairement discret, surtout en clair.

### L'apparence livrée

Sans `OmniThemeScope`, ou avec une portée sans thème ni palette, la page a l'aspect du thème Essentiel avec la palette Essentiel. Ces jetons ne sont pas écrits à la main : ils sont générés par la fabrique entre les marqueurs `omni:theme-tokens` de la feuille, pour le clair (`:root`, `[data-omni-theme="light"]`), le sombre et le mode système, et `ShippedThemeTokensTests` refuse toute retouche manuelle. Chaque jeton de couleur a donc sa valeur sombre.

### Jetons de couleur

Chaque sévérité (succès, information, avertissement, danger) et l'accent ont un jeton de texte (`--omni-color-X`, tenu à 4,5 sur la page et sur sa teinte pâle `-subtle`) et un jeton de remplissage (`--omni-color-X-fill`, avec `-fill-hover`, `-fill-active` et son encre `--omni-color-on-X-fill`) : le remplissage ne bouge que jusqu'à 3 contre la page, et c'est l'encre posée dessus qu'on pousse à 4,5, si bien que la couleur de marque reste reconnaissable. Les intentions ont en plus leurs fonds pleins : `--omni-color-info-bright` et `--omni-color-success-bright` avec l'encre `--omni-color-on-bright`, `--omni-color-warning-deep` et `--omni-color-danger-deep` avec `--omni-color-on-deep`, chacun avec son survol et son appui. Le bouton secondaire lit `--omni-color-neutral-fill`. La règle d'usage est dans [ui-conventions.md](ui-conventions.md). `ThemeContrastMatrixTests` vérifie 64 paires sur chacun des jeux du catalogue (450 le 2026-09-30 ; 4,5 pour un texte, 3 pour un remplissage ou l’accent contre la page, plancher de 1,7 pour une bordure), et, pour un thème qui peint un fond de couleur (Givre, Trou noir), le texte, le texte atténué et l’accent fort sur chaque arrêt de ce fond et sur la carte translucide posée dessus.

### Densité

`OmniDensity` a trois valeurs : `Compact`, `Comfortable` (par défaut) et `Spacious`. `OmniThemeScope.Density` règle toute la portée ; `OmniCard`, `OmniAlert`, `OmniTabs`, `OmniPanelMenu`, `OmniProfileMenu`, `OmniContextMenu` et `OmniSettingsTile` ont leur propre `Density`, nulle par défaut pour hériter, qui l'emporte pour le composant et tout ce qu'il contient. `OmniDataGrid.Density` garde sa valeur propre, non nulle. La densité est un attribut `data-omni-density` qui pose des jetons hérités (hauteur de contrôle de 1,625, 2,25 ou 2,75 rem, marges, cases du calendrier, taille des cases à cocher, des interrupteurs et des icônes d'alerte), lus par chaque composant qui a une taille propre.

### Réglages d'apparence réutilisables

`OmniAppearanceSettings` porte en ligne le mode clair/sombre/système (groupe radio segmenté) et la police,
puis une ligne « Thème, palette et tailles » dont le bouton Modifier (`Primary`, l'action principale de
la ligne) ouvre la fenêtre de l'apparence, sans voile et déplaçable. La police se règle en ligne et,
quand l'hôte lie `FontChanged`, dans la fenêtre aussi. `WindowOpen` et `WindowOpenChanged` suivent
l'ouverture de cette fenêtre, ce qui permet à l'hôte de retirer son éventuel voile de menu.
Cette fenêtre est aussi un composant, `OmniAppearanceWindow`, qu'un hôte ouvre depuis sa propre entrée
de menu (« Thème ») par `Open`/`OpenChanged`. Plus large que haute (46rem, jamais plus que l'écran), elle
range ses réglages deux par deux, dans l'ordre de lecture : thème et palette, taille du texte et taille
des contrôles, puis densité, police et fond animé ; une seule colonne dès que deux de 19rem ne tiennent
plus. À côté du nom du thème, le bouton « Aléatoire » tire un thème autre que le courant et, si la
palette est liée, une palette autre que celle en vigueur (`PresetChanged`, `PaletteChanged`, puis
`FontChanged` avec `null` pour une police choisie pour l'ancien thème). Chaque ligne n'apparaît que si l'hôte lie son changement
(`PresetChanged`, `PaletteChanged`, `FontChanged`, `TextSizeLevelChanged`, `DensityChanged`,
`ControlSizeLevelChanged`) ; la ligne « Fond animé » (`BackdropMotionChanged`) demande en plus un thème
qui bouge son fond (voir « Fond animé »). Chaque ligne est un groupe nommé par son libellé ; les boutons moins et plus
d'une échelle portent des noms distincts.
Dans les deux composants, choisir un thème lève `PresetChanged` puis `PaletteChanged` et `FontChanged`
avec `null` pour une palette ou une police choisie pour le thème précédent : le nouveau thème s'affiche
avec les siennes, l'hôte ne fait que stocker ce qu'il reçoit.
En `Compact`, libellés et commandes s'alignent sur deux colonnes et la ligne de l'apparence lit « Thème et
tailles » suivi d'un résumé (« Essentiel · Texte 5 · Confortable », taille des contrôles comprise quand
l'hôte la lie).
Les mesures de la fenêtre sont capturées à l'ouverture (`FreezeScale`) : les commandes restent stables
pendant les changements de taille et de densité, puis prennent la nouvelle échelle à la prochaine
ouverture. Le reste de l'application garde son échelle active. Un nouveau thème, une nouvelle palette
ou une nouvelle police font reprendre ces mesures, à l'échelle du moment : les libellés changent de
police, de casse et d'espacement, et une largeur capturée pour l'ancien thème les rognerait.
Le thème et la palette de référence se nomment « Essentiel » ; les hôtes qui ont stocké l'ancien nom « Défaut »
doivent le traiter comme un alias lors de la restauration de leurs préférences.
La densité est un `OmniDensity` (`Density`/`DensityChanged`), choisi dans un groupe radio segmenté
(Compacte, Confortable, Aérée), que l'hôte passe tel quel à `OmniThemeScope.Density`. La taille du texte
et la taille des contrôles proposent les niveaux 1 à 10 : un curseur suit les boutons moins et plus de
chaque réglage. Le contrôle reçoit les valeurs et émet leurs changements ; l'application conserve
la responsabilité du stockage et les applique à sa portée. Les boutons Défaut restaurent les valeurs
initiales.
Le réglage Police propose les dix polices d'`OmniThemeFonts.All` : six piles système et quatre polices web
libres (Inter, Lexend, Source Serif 4, JetBrains Mono, OFL 1.1) servies par le paquet depuis `fonts/`, avec repli système ;
celle du thème est marquée « (défaut) » et `OmniThemePresets.DefaultFontFor(preset)` la fournit.
`Font` et `FontChanged` la pilotent, `OmniThemeScope.Font` l'applique au texte et aux titres de la portée.
La palette du thème est nommée dans le sélecteur, par exemple « Océan (défaut) » pour Ardoise ;
`OmniThemePresets.DefaultPaletteFor(preset)` fournit cette valeur. `Compact` réduit le panneau pour
un menu d'en-tête. La taille du texte se pilote par les jetons de police
du site ; le composant ne modifie pas la racine du document à l'insu de son hôte. Pour reproduire
l'échelle d'une application cliente sans CSS propre à l'application, l'hôte pose `data-oe-text-size` (1 à 10) sur
`<html>` : la feuille OE applique alors 75 % à 131,25 % à la taille racine, 5 valant 100 %.
Le module `./_content/OmniEurope.Blazor/omni-appearance.js` expose `setTextSizeLevel(level)` et
`clearTextSizeLevel()` pour poser ou retirer cet attribut. La taille des contrôles suit le même
principe : `data-oe-control-size` (1 à 10) sur `<html>` règle `--omni-control-scale` de 0,75 à 1,3125,
que lisent `--omni-control-height`, `--omni-control-font` et `--omni-button-pad-x` dans les trois
densités, sans toucher au texte de la page ; `setControlSizeLevel(level)` et
`clearControlSizeLevel()` le posent ou le retirent. La fenêtre n'affiche ce réglage
que si l'hôte fournit `ControlSizeLevelChanged`, puisque c'est lui qui l'applique. La démonstration du paquet applique
le niveau choisi et le retire en quittant la page ; un aperçu vivant montre aussi le thème,
la palette et la densité sélectionnés, à côté des exemples de combinaisons fixes.

### Jetons d'échelle de la feuille

La feuille déclare sur `:root` quelques échelles que les composants lisent et qu'un hôte peut lire pour ses propres surfaces :

- Couches, de la plus basse à la plus haute : `--omni-z-sticky` (10), `--omni-z-drawer-backdrop` (25), `--omni-z-drawer` et `--omni-z-popover` (30), `--omni-z-progress` (35), `--omni-z-popover-top` (40, surface flottante ouverte depuis une autre), `--omni-z-overlay` (100), `--omni-z-portal` et `--omni-z-toast` (120), `--omni-z-window` (1200), `--omni-z-tooltip` (1300, au-dessus de la fenêtre qui porte son déclencheur), `--omni-z-blocking` (1400, voile de connexion perdue) et `--omni-z-splash` (écran de démarrage). Seules ces valeurs empilent une surface contre le reste de la page ; un petit `z-index` interne (1 à 4) ne range que les parties d'un composant.
- Durées des transitions : `--omni-duration-press` (80 ms, appui d'un bouton), `--omni-duration-fast` (120 ms, survol ou fondu), `--omni-duration-medium` (200 ms, chevron ou remplissage qui bouge) et `--omni-duration-slow` (300 ms, panneau qui glisse). Les transitions et animations respectent `prefers-reduced-motion`.
- Anneaux de focus : `--omni-focus-ring` (un seul anneau pour tous les contrôles), `--omni-focus-ring-danger` (champ invalide) et `--omni-focus-ring-inset` (anneau intérieur, pour un élément qu'un anneau extérieur ferait déborder). Ils nomment des jetons de palette et sont redéclarés sur chaque portée de thème.
- Texte : `--omni-font-size-xs` (0,6875 rem) et `--omni-font-size-label` (0,8125 rem, libellés, légendes, aides et compteurs), autour de `--omni-font-size-sm` et `--omni-font-size-base` ; `--omni-font-family-serif` (Source Serif 4 puis piles système), serif de lecture pour la prose longue comme la feuille de document.
- Les jetons de forme (échelle des titres, épaisseur de bordure, voile, forme des boutons et des titres) sont remis à leur valeur par défaut sur chaque portée `[data-omni-theme]` : une valeur posée par l'hôte sur `:root` n'atteint pas l'intérieur d'une portée de thème ; la poser sur la portée elle-même.

## Largeur du contenu et défilement

`OmniTabs.ScrollablePanels` (désactivé par défaut) fait du panneau sélectionné la zone qui défile :
les onglets prennent toute la hauteur de leur parent, qui doit donc être dimensionné (élément flex
à minimum nul ou hauteur fixe), et la barre d'onglets reste en place pendant que le contenu défile
sous elle. Le défilement s'arrête au bord du panneau au lieu de passer à la page. Un onglet placé
dans un tel panneau ne porte pas de défilement propre.

La coquille occupe toute la largeur ; `OmniMain.ContentWidth` centre le contenu seul, en-tête et barre latérale restant sur toute la largeur. Avec `Scrollable`, l'élément principal devient le conteneur de défilement de la page : il couvre toute la zone laissée par la barre latérale, si bien que sa barre de défilement reste au bord droit de la fenêtre, jamais au bord de la colonne centrée. La colonne se centre par la marge intérieure de l'élément et comprend sa gouttière, `--omni-main-gutter` (l'espacement moyen par défaut). Un enfant à `block-size: 100%` remplit exactement la zone ; un contenu plus haut la fait défiler, marge du bas comprise.

`OmniMain.AutoHideScrollbar` (désactivé par défaut, avec `Scrollable`) garde la place de la barre de défilement mais la laisse transparente : elle n'apparaît que pendant le défilement et s'efface peu après, comme une barre en surimpression.

`OmniWindowControls`, dernier enfant d'un `OmniHeader`, dessine les boutons de légende d'une fenêtre de bureau sans bordure (MAUI, WebView2) : réduire dans la zone de notification (`OnMinimizeToTray`), réduire (`OnMinimize`), agrandir ou restaurer (`OnMaximizeRestore`, `IsMaximized`) et fermer (`OnClose`). Seuls les boutons dont le rappel est posé sont rendus. Ils sont carrés, prennent toute la hauteur de l'en-tête en traversant sa marge intérieure, se touchent, et fermer occupe le coin supérieur droit, rouge au survol. `Actions` accueille d'autres boutons d'en-tête (une bascule de thème), carrés à la même hauteur, avant les boutons de légende et séparés d'eux par un petit espace. Le groupe est nommé par `Label` ; libellés des boutons localisés par défaut, remplaçables par `MinimizeToTrayLabel`, `MinimizeLabel`, `MaximizeLabel`, `RestoreLabel` et `CloseLabel` (tous `string?`).

La coquille doit avoir une hauteur bornée : une `OmniLayout` dont le corps porte un élément principal défilant devient une colonne, et l'hôte lui donne sa hauteur, par exemple celle de la fenêtre. Les deux plafonds se lisent dans `--omni-layout-wide-width` et `--omni-layout-content-width`, qu'un hôte peut poser sur un ancêtre.

```razor
<OmniLayout Class="app-shell">  @* .app-shell { block-size: 100dvh; } *@
    <OmniHeader>...</OmniHeader>
    <OmniBody>
        <OmniSidebar Open="true" Label="Navigation">...</OmniSidebar>
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
`failed`, `up`...) : `Map` (`OmniStatusMap<string>`) donne le ton de chacun, seul `OmniStatus.Tone` étant lu (`OmniTone.Neutral` pour un état absent, ou pour tous sans `Map`),
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
défaut). Le composant ne rend aucun élément : il ne prend ni `Id`, ni `Class`, ni préréglage, et un
attribut qu'on lui passe est refusé au rendu plutôt qu'ignoré. Le retrait ne dépend pas de la fin de la transition : un minuteur de 600 ms le garantit dans un
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

Le paquet livre des classes utilitaires préfixées `omni-u-`, pour mettre en forme le balisage qu'une application écrit autour des composants, pas pour restyler un composant : chaque règle vise une seule classe utilitaire, jamais la classe d'un composant (garde `UtilityClassesTests`). Déclarées en fin de feuille, elles l'emportent sur une règle de composant de même spécificité.

| Famille | Classes |
|---|---|
| Espacement | `omni-u-{m,mt,mb,ms,me,mx,my,p,pt,pb,px,py}-{0,xs,sm,md,lg,xl,2xl}`, `omni-u-mx-auto`. L'échelle est celle des jetons `--omni-space-*` : elle suit la densité. |
| Texte | `omni-u-text-{muted,accent,success,warning,danger}`, `omni-u-text-center`, `omni-u-text-end`, `omni-u-bold`, `omni-u-mono`, `omni-u-truncate`, `omni-u-nowrap`, `omni-u-small` |
| Fond et bordure | `omni-u-bg-muted`, `omni-u-bg-{accent,success,warning,danger}-subtle`, `omni-u-border-accent` (couleur de bordure seule) |
| Mise en page | `omni-u-w-100`, `omni-u-grow`, `omni-u-block`, `omni-u-rounded`, `omni-u-pointer` |
