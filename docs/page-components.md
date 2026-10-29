# Pages (famille Pages)

La famille Pages réunit les compositions qui habillent une page d'application : l'en-tête et son fil
d'Ariane, la fiche détaillée, la page de connexion, le voile de connexion perdue, l'état vide,
l'assistant à étapes, la liste de définitions et la date relative. Elles sont faites des composants de
base du paquet (`OmniBreadcrumb`, `OmniHeading`, `OmniButton`, `OmniIcon`, `OmniSkeleton`,
`OmniSteps`, `OmniProgressBar`, `OmniTooltip`, `OmniCard`) et restent du côté du navigateur : aucune
ne fait d'appel réseau, ne tient de connexion, d'authentification ni d'horloge. Les données arrivent
par paramètres, délégués et fragments ; le temps, par le `TimeProvider` enregistré par l'hôte (l'horloge système sinon).

## En-tête de page : `OmniPageHeader` et `OmniBreadcrumbService`

```razor
<OmniPageHeader ShowBack="true" Subtitle="@Projet.Description">
    <Badges><OmniBadge Tone="OmniTone.Warning">Archivé</OmniBadge></Badges>
    <ActionsContent><OmniButton OnClick="Modifier"><Icon><OmniIcon Name="OmniIconName.Edit" /></Icon><ChildContent><span>Modifier</span></ChildContent></OmniButton></ActionsContent>
    <MenuContent>
        <OmniMenuItem OnClick="Archiver">Archiver</OmniMenuItem>
        <OmniMenuItem Tone="OmniTone.Danger" OnClick="Supprimer">Supprimer</OmniMenuItem>
    </MenuContent>
    <Filters><OmniTextBox @bind-Value="Recherche" /></Filters>
</OmniPageHeader>
```

Deux lignes de hauteur fixe (R-395, demande de l'équipe d'une application cliente), si bien que toutes les pages
commencent leur contenu à la même hauteur :

- ligne 1 : bouton retour, icône, titre, badges, actions, puis le menu « ⋮ » (`MenuContent`). Un titre
  trop long défile entre deux chevrons posés de chaque côté au lieu de passer à la ligne. Sur téléphone,
  et à toute largeur quand la ligne 1 manque de place, un bouton « Voir plus » (`aria-expanded`)
  déplie badges et actions, le menu « ⋮ » restant visible. Les actions épinglées
  (`PinnedActionsContent`) ne se replient jamais ;
- ligne 2, toujours réservée : le fil d'Ariane sous le titre, dont le dernier maillon est le titre de la
  page, ou le sous-titre quand la page n'a pas d'ancêtre à montrer (ou avec `ShowTrail="false"`).

Le bloc a une marge basse propre, sauf dans un `OmniStack`, dont l'écart espace déjà les blocs : la
marge s'y ajoutait et éloignait l'en-tête du bloc suivant plus que les blocs entre eux.

Le sous-titre, quand la ligne 2 porte le fil, et les filtres viennent sous le bloc. Le script
`omni-page-header.js` (défilement du titre, repli quand la ligne 1 déborde, par l'attribut `data-compact`) est libéré par `DisposeAsync`.
Le repli et le défilement du titre supposent un conteneur qui borne la largeur de l'en-tête : dans une
piste de grille `auto` ou un élément flex sans `min-inline-size: 0`, l'en-tête grandit avec son contenu,
la ligne 1 ne déborde jamais et rien ne se replie. Donner à la piste `minmax(0, 1fr)` ou à l'élément
`min-inline-size: 0`.

| Paramètre | Rôle |
| --- | --- |
| `Title` | Le titre. Vide, c'est le dernier maillon du fil qui titre la page, et un maillon encore en chargement affiche un squelette à sa place. |
| `Level` | Niveau du titre, `H1` par défaut. |
| `Subtitle`, `Filters` | Une ligne d'explication (sur la ligne 2 sans ancêtre, sous le bloc sinon) et une rangée de filtres, sous le bloc. |
| `ShowTrail`, `TrailLabel` | Le fil d'Ariane sur la ligne 2 (vrai par défaut) et son nom accessible. `false`, la ligne 2 montre le sous-titre. La ligne garde sa hauteur dans tous les cas. |
| `ShowBack`, `BackHref`, `BackLabel` | Le bouton retour : vers `BackHref`, sinon vers l'ancêtre le plus proche qui porte un lien, sinon un pas en arrière dans l'historique du navigateur. |
| `BackVariant` | L'aspect du bouton retour : `Primary` par défaut, la même flèche d'accent sur chaque page ; `Ghost` pour un retour discret. |
| `Badges`, `ActionsContent` | Après le titre, sur la ligne 1. Sur téléphone, et à toute largeur quand la ligne 1 manque de place, ils se replient derrière le bouton « Voir plus » (`aria-expanded`). |
| `PinnedActionsContent` | Des actions jamais repliées (`null` par défaut, rien de rendu) : ni derrière « Voir plus », ni derrière un chevron, quel que soit le mode compact. À toute largeur, elles terminent la ligne 1 juste avant le menu « ⋮ ». Sous 40rem, elles prennent une ligne sous la ligne 1, alignées à la fin, le bloc grandissant de cette ligne ; le menu « ⋮ » reste sur la ligne 1, à l'extrémité de la ligne du titre, « Voir plus » fermé ou ouvert (badges et actions dépliés viennent après elles). Pour l'action de la page qui doit rester à portée, par exemple « Relancer ». |
| `MenuContent` | Les `OmniMenuItem` du menu « ⋮ » en fin de ligne 1, un `OmniOverflowMenu` ; aucun menu sans lui. Il reste visible sur téléphone. |
| `Icon` | Icône avant le titre (`aria-hidden`, couleur primaire). Le titre est alors rogné à ses capitales, si bien que le centre de l'icône tombe sur le centre du texte quelle que soit la police. |
| `Framed` | `false` par défaut : les deux lignes posées directement sur la page, sans bordure, fond ni marge intérieure, le titre commençant à l'aplomb du contenu. `true` les met dans un bloc bordé sur la surface. Le bloc garde sa hauteur fixe dans les deux cas. |

`OmniBreadcrumbService` (inscrit par `AddOmniEuropeBlazor`, portée scoped) tient le fil de la page
affichée. La bibliothèque ne connaît aucune route : l'hôte inscrit un `IOmniBreadcrumbResolver`, dont
`Resolve(relativePath)` rend le fil d'une route (chemin relatif à la base, sans requête, fragment ni
barre oblique de bord). À chaque changement de chemin, le service installe ce fil ; une navigation qui
ne change que la requête ou le fragment (onglet, filtre, page de résultats), une barre oblique finale ou
la casse garde le fil que la page a posé. La page le remplace ensuite quand elle en sait plus :

```csharp
Breadcrumb.Replace(1, Breadcrumb.Items[1] with { Text = projet.Nom, Loading = false });
```

`Set`, `Push`, `Replace` et `Reset` modifient le fil et lèvent `Changed` ; `Items`, `Current`,
`Ancestors` et `ParentHref` le lisent. Un maillon `OmniBreadcrumbEntry` porte `Text`, `Href` et
`Loading`. Sans résolveur, le fil de départ est vide.

## Fiche détaillée : `OmniDetailShell`

```razor
<OmniDetailShell State="Etat" BackHref="/serveurs" NotFoundTitle="Serveur introuvable">
    <HeaderContent><OmniPageHeader ShowBack="true" /></HeaderContent>
    <ChildContent>@Body</ChildContent>
</OmniDetailShell>
```

`State` vaut `Loading` (défaut), `Found` ou `NotFound`. En chargement, `HeaderContent` est peint et un
squelette (ou `LoadingContent`) le suit, le conteneur porte `aria-busy`. Le contenu est toujours rendu,
masqué par `hidden` tant que l'entité n'est pas là : un enfant qui déclenche le chargement depuis son
propre cycle de vie doit exister pour le déclencher. `NotFound` remplace l'en-tête par un état vide
(`NotFoundTitle`, `NotFoundDescription`) et un bouton retour (`BackHref`, `BackText`, sinon
l'historique) ; `NotFoundContent` remplace cet état entier.

## Page de connexion : `OmniLoginShell` et `OmniReturnUrl`

`OmniLoginShell` place `Logo` au-dessus d'une carte centrée (25rem au plus) qui porte le titre
(`Title`, « Connexion » par défaut, niveau `Level`), `Description`, le formulaire (`ChildContent`) et
`FooterContent`. La carte est nommée par son titre. Aucune authentification n'est embarquée. Le bouton
Connexion reste dans le formulaire de l'hôte, dans un `OmniFormActions` en fin de formulaire, aligné à
droite ; le pied de la carte ne le reçoit pas, puisqu'il serait hors du `<form>`.

Un site qui propose l'inscription passe `SignUpHref` : le pied commence alors par une ligne de texte
discrète, « Pas de compte ? S'inscrire » (`SignUpPrompt`, `SignUpText`), le lien au rang de « Mot de
passe oublié », jamais un second bouton à côté de Connexion. `SignUpContent` remplace la question et le
lien (une demande d'accès, un lien qui lance une action) en gardant la place et le style de la ligne.
Sans l'un ni l'autre, rien n'est rendu. Le pied d'une carte de connexion se lit comme du texte depuis le
début (liens, ligne de version) : il ne prend pas l'alignement en fin de ligne des actions d'une carte.

`OmniReturnUrl` garde l'adresse de retour dans l'application : `IsLocal` n'accepte qu'un chemin qui
commence par une seule barre oblique, sans schéma ni hôte (`//hote`, `/\hote`, `https://hote`), sans
barre oblique inverse ni caractère de contrôle, de 2048 caractères au plus. `Resolve(returnUrl, "/")`
rend l'adresse si elle est locale, le repli sinon ; `Append("/login", returnUrl)` l'ajoute en paramètre
de requête (`returnUrl` par défaut) seulement si elle est locale. L'hôte exclut lui-même ses propres
pages d'authentification s'il le souhaite.

## Connexion perdue : `OmniConnectionOverlay`

Un voile bloquant au-dessus de tout (`--omni-z-blocking`) tant que `State` n'est pas `Connected` :

| État | Titre par défaut | Action |
| --- | --- | --- |
| `Reconnecting` | Connexion perdue | « Se reconnecter maintenant », `OnReconnect`. Un indicateur tourne tant que `Busy` est faux, et `TimeUntilRetry` (`TimeSpan`) affiche le compte à rebours. |
| `Failed` | Connexion impossible | « Se reconnecter maintenant », `OnReconnect`. Plus de compte à rebours. |
| `Rejected` | Session interrompue | « Recharger la page », `OnReload`, sinon rechargement complet de la page. |

`Reason` s'affiche tel quel (« Cause : ... »). `Title` et `Description` remplacent les textes de l'état,
`ReconnectText` et `ReloadText` ceux des deux actions.
La carte est un `alertdialog` modal nommé et décrit ; elle prend le focus sur son action, garde Tab en
elle et rend le focus à la reconnexion. Le compte à rebours reste hors de la région vivante, qu'un
lecteur d'écran lirait sinon à chaque seconde. L'hôte tient le compte : le composant n'a pas d'horloge.

## État vide : `OmniEmptyState`

`Icon` (un plateau vide par défaut, décoratif), `Title`, `Description` et `ActionsContent`. Le titre est un
paragraphe ; `Level` en fait un titre quand l'état vide ouvre une section.

## Tuile de statistique : `OmniStatTile`

`Value` (déjà formatée), `Title` (obligatoire, la légende visible, comme `OmniNavTile.Title`), `Detail` (ligne atténuée facultative) et `Icon` (carré
teinté, décoratif). Avec `OnClick`, la tuile entière devient un bouton, nommé « titre : valeur » ; avec `Href` (`string?`, demande d'une application cliente, recette R1-20), elle devient un seul lien (`a`) de même nom et de même aspect (survol, anneau de focus), l'adresse passant par la politique d'URI du paquet comme `OmniNavTile.Href` (un schéma dangereux tel que `javascript:` lève) ; `Href` et `OnClick` ensemble lèvent `ArgumentException` ; sans l'un ni l'autre, un simple bloc que rien n'active, au rendu inchangé. Ses éléments sont des `span`, valides dans un bouton comme dans un lien ; ne placez aucun élément interactif dans `Icon` (imbrication invalide, non détectée). La racine, bouton ou lien, garde les classes de la tuile : dans une `OmniTileGrid`, elle a la même taille que ses voisines et `DoubleWidth` l'étale. La valeur ne passe pas
à la ligne. Le titre reste sur une ligne : un script le réduit à 0,6875 rem quand il ne tient pas,
puis le coupe de points de suspension, son texte entier en titre (infobulle), y compris quand `OnClick` ou `Href` apparaît ou disparaît et que la tuile change d'élément ; le détail peut se couper.
Comme `OmniNavTile`, elle prend la hauteur de sa colonne (`block-size: 100%`) : les tuiles d'une rangée
`OmniRow` ont la hauteur de la plus haute, celle qui porte un `Detail` comprise ; seule dans un bloc ou
empilée sur un téléphone, elle garde sa propre hauteur. `DoubleWidth` l'étale sur deux colonnes d'une
`OmniTileGrid` (voir [layout-components.md](layout-components.md), « Règle des tuiles »).

`WrapTitle` (`bool`, faux par défaut ; demande d'une application cliente) laisse le titre passer à la ligne, à sa
taille normale, au lieu de le garder sur une ligne qui rétrécit puis se coupe : pour une légende longue comme
« Meilleure série de bonnes réponses ». Le script de la tuile ne la touche plus (ni réduction, ni coupure, ni
infobulle de titre) et il suit le changement de la classe `omni-stat-tile--wrap` si le paramètre bascule. La tuile
grandit, les mots trop longs se coupent plutôt que de déborder. Dans une `OmniTileGrid` aux hauteurs égales (le
défaut, `grid-auto-rows: 1fr`), toutes les tuiles de la grille prennent la hauteur de la plus haute, comme avec
un `Detail` ; avec `EqualHeights="false"`, seules celles de la rangée.

## Tuile de faits : `OmniFactTile` et `OmniFactTileRow`

Une tuile de faits sur une chose (la source d'une exécution, son statut, ses durées), sœur d'`OmniStatTile` (demande d'une application cliente, recette R-28) : un titre, puis des lignes `OmniFactTileRow` d'icône, de libellé et de valeur. `OmniFactTile` : `Title` (obligatoire, passe à la ligne plutôt que de déborder), `Icon` (fragment décoratif avant le titre), `TitleAlign` (`OmniAlignment`, `Center` par défaut ; `Stretch` n'a pas de sens pour une ligne de texte et se lit `Start`), `ChildContent` (les lignes), `Href` ou `OnClick` et `DoubleWidth`. `OmniFactTileRow` : `Icon` (fragment décoratif), `Term` (obligatoire, le terme comme `OmniDescriptionItem.Term` : `Label` désigne un nom accessible, jamais un texte affiché), la valeur en `ChildContent` et `Href` facultatif pour une valeur lien (un `OmniLink`, adresse vérifiée par la politique d'URI).

```razor
<OmniTileGrid Columns="3">
    <OmniFactTile Title="Source">
        <Icon><OmniIcon Name="OmniIconName.FolderOpen" /></Icon>
        <ChildContent>
            <OmniFactTileRow Term="Dépôt" Href="depots/blazor">
                <Icon><OmniIcon Name="OmniIconName.Database" /></Icon>
                <ChildContent>omni-europe-blazor</ChildContent>
            </OmniFactTileRow>
            <OmniFactTileRow Term="Statut">
                <Icon><OmniIcon Name="OmniIconName.Info" /></Icon>
                <ChildContent><OmniBadge Tone="OmniTone.Success">Réussi</OmniBadge></ChildContent>
            </OmniFactTileRow>
        </ChildContent>
    </OmniFactTile>
</OmniTileGrid>
```

- **Alignement** : dans une tuile de 18 rem de contenu ou plus (voir « Tuile étroite » pour le reste), toutes les lignes partagent trois colonnes : l'icône (`--omni-fact-tile-icon-width`, 1,25 rem), le libellé (`--omni-fact-tile-label-width`, 6,5 rem, jamais plus de 40 % de la largeur des lignes), la valeur qui prend le reste. Chaque ligne est une sous-grille de ces colonnes (les mêmes pistes là où `subgrid` manque), et la cellule d'icône existe même sans icône : les libellés gardent leur colonne. Les largeurs sont en rem, pour que libellés et largeur suivent ensemble la taille du texte et que des tuiles de même largeur d'une `OmniTileGrid` alignent leurs colonnes au même décalage du bord ; le plafond de 40 % empêche une grande taille de texte de pousser les lignes hors d'une tuile étroite (une tuile `DoubleWidth`, plus large, garde alors un libellé plus large que ses voisines). La hauteur d'une ligne suit la densité (celle d'un contrôle). Un hôte change les largeurs une fois, sur la grille ou sur une classe d'une tuile, en posant les deux propriétés.
- **Tuile étroite** : la tuile est un conteneur de taille (`container-type: inline-size`), jamais la fenêtre : sous 18 rem de contenu (quatre tuiles sur une rangée d'`OmniTileGrid`, par exemple), la colonne de libellé partagée mangerait la place de la valeur. Les lignes quittent alors les colonnes partagées et chacune est une ligne flexible qui passe à la ligne, **décidée ligne par ligne** : l'icône (largeur fixe), le libellé (sa propre largeur) et la valeur (tout le reste, texte calé à la fin) restent sur une seule ligne tant que les trois tiennent, même avec un libellé long et une valeur courte (« ID de l'exécution » et « 2 ») ; quand ils ne tiennent pas, la valeur passe entière sur une seconde ligne, depuis le bord de l'icône, aussi large que la ligne. La coupure se fait sur la taille d'une ligne de chaque élément (le libellé, jamais plus que la place à côté de l'icône ; la valeur, jamais plus que la ligne) : la valeur descend entière avant d'être serrée, et les points de suspension restent le dernier recours pour une valeur plus large qu'une ligne entière ou un libellé plus large que la place à côté de l'icône. Dans ce mode, seules les icônes et les débuts de libellés gardent des verticales communes ; les valeurs sont calées sur le bord de fin. Le seuil est en rem, donc il suit la taille du texte, et il ne suit pas `--omni-fact-tile-label-width`. Une tuile plus large garde les trois colonnes alignées ; un libellé plus long que sa colonne y est coupé (l'infobulle le donne en entier) : un hôte élargit la colonne une fois avec `--omni-fact-tile-label-width`.
- **Coupe et infobulle** : le libellé et la valeur d'une ligne restent sur une ligne ; trop longs, ils sont coupés de points de suspension et lus en entier dans l'infobulle du paquet, au survol et au focus clavier, seulement tant qu'ils sont coupés (le même script `omni-tooltip.js` que les cellules de grille, que la tuile active à son premier rendu et libère à sa fin ; aucun nouveau script). Le texte entier reste dans la page pour les lecteurs d'écran.
- **Badges** : un `OmniBadge` dans une valeur est dessiné petit (trois quarts de la hauteur d'un contrôle de la densité, police `--omni-font-size-xs`) par la règle de la cellule, sans paramètre de taille ; il ne dépasse jamais la cellule.
- **Tuile entière cliquable** : `Href` ou `OnClick` font de la tuile un seul lien ou un seul bouton, au rendu et au focus d'`OmniStatTile` ; la vérification (les deux ensemble lèvent `ArgumentException`, adresse dangereuse lève `InvalidOperationException`) est partagée avec `OmniStatTile`. Le nom accessible est le contenu (le titre, puis chaque libellé et sa valeur) et non le seul titre, pour que les faits soient lus ; sans `Href` ni `OnClick`, la tuile est un groupe nommé par son titre. Les éléments sont des `span`, valides dans un bouton comme dans un lien. Un `Href` de ligne dans une tuile elle-même lien ou bouton lève `InvalidOperationException` (un lien ne peut pas être dans un lien ni dans un bouton) ; aucun autre élément interactif non plus dans une tuile cliquable (non détecté).
- **Dans une `OmniTileGrid`** : la tuile est un enfant direct et suit la règle des tuiles (même largeur et, par défaut, même hauteur que ses voisines, `EqualHeights="false"` sur la grille lui laissant sa hauteur naturelle ; `DoubleWidth` sur deux colonnes, rien ne déborde, voir [layout-components.md](layout-components.md), « Règle des tuiles »). Les lignes défilent quand la tuile est trop haute, et sont coupées dans une tuile cliquable (une zone qui défile dans un lien ou un bouton est un arrêt de focus imbriqué). Hors grille, la tuile prend la hauteur de sa colonne.
## Tuile de navigation : `OmniNavTile`

L'accueil d'une administration ou d'un regroupement de rubriques : `Icon` sur un carré teinté du ton
`Tone` (`Accent` par défaut), `Title` (obligatoire) et `Description` (ligne atténuée facultative), la
tuile entière étant un vrai lien vers `Href` (clic du milieu, adresse visible au survol ; adresse vérifiée
par la politique d'URI du paquet comme celle d'`OmniLink`, un schéma dangereux tel que `javascript:` lève). Au survol,
seule l'ombre change, la tuile ne bouge pas. Elle prend la hauteur de sa colonne : posées dans un
`OmniRow` d'`OmniColumn`, les tuiles d'une ligne s'alignent quelles que soient leurs descriptions ; dans
une `OmniTileGrid`, toutes les tuiles ont la même largeur et la même hauteur, et `DoubleWidth` en étale une
sur deux colonnes (« Règle des tuiles » dans [layout-components.md](layout-components.md)).

`FitTitle` (faux par défaut ; demande d'une application cliente, recette R-57) tient le titre sur une ligne
dans une colonne étroite, six tuiles par rangée par exemple : la taille de la police du titre diminue avec la
largeur de la tuile (unités de requête de conteneur, sans script) jusqu'à ce que tout le titre tienne, aucun
mot n'est coupé, et le titre et la description sont alignés à gauche au lieu d'être centrés. La taille se
calcule d'après la longueur de `Title`, arrondie à un palier (8, 12, 16, 20, 24, 32 ou 48 caractères, une
classe et jamais un style en ligne) et s'arrête à 0,75rem ; en dessous, le titre est coupé par des points
de suspension. Le calcul compte 0,6em par caractère : une police très large peut couper un titre un peu avant.

```razor
<OmniRow Gap="OmniSpacing.Medium">
    <OmniColumn Span="12" MediumSpan="4">
        <OmniNavTile Href="admin/utilisateurs" Title="Utilisateurs" Description="Comptes et rôles" Tone="OmniTone.Info">
            <Icon><OmniIcon Name="OmniIconName.Users" /></Icon>
        </OmniNavTile>
    </OmniColumn>
</OmniRow>
```

## Assistant : `OmniWizard` et `OmniWizardStep`

```razor
<OmniWizard @bind-Value="Etape" FinishText="Créer" OnFinish="CreerAsync" OnCancel="Fermer">
    <OmniWizardStep Title="Identité" CanContinue="@NomSaisi">...</OmniWizardStep>
    <OmniWizardStep Title="Options" Validate="ValiderOptionsAsync">...</OmniWizardStep>
    <OmniWizardStep Title="Récapitulatif">...</OmniWizardStep>
</OmniWizard>
```

La progression (`OmniProgressBar`, « Étape 2 sur 3 »), la liste des étapes (`OmniSteps`), l'étape
courante dans un corps unique que les boutons d'étape contrôlent, puis Précédent, Suivant ou
`FinishText`, et Annuler en `Secondary` si `OnCancel` a un gestionnaire. Revenir en arrière est libre.
Avancer, par Suivant, Terminer ou une étape déjà atteinte de la liste, demande d'abord l'étape courante :
`CanContinue` à faux désactive le bouton, `Validate` répond au clic et garde l'étape s'il rend faux
(l'étape dit pourquoi). Une étape non atteinte ne se clique pas. Quand l'utilisateur change d'étape, le
focus passe au corps ; une région de statut annonce « Étape 2 sur 3 : Options ». L'assistant ne dessine aucun
voile : dans un `OmniDialog`, Échap et la croix restent ceux du dialogue.

`OnFinish` est levé par le bouton `FinishText` une fois que la dernière étape laisse l'utilisateur continuer.

Les boutons restent à la même place quelle que soit l'étape : le corps a une hauteur contrôlée,
`BodyHeight` (longueur CSS comme `24rem`, `400px`, `50vh` ou `100%`), à défaut la propriété
`--omni-wizard-body-height` de la feuille de l'hôte, à défaut `min(24rem, 60vh)`. Une étape plus haute
défile dans le corps, au-dessus du pied (`omni-wizard__foot`) ; une étape plus courte laisse du vide. La
valeur est posée par le script d'interop comme propriété personnalisée (jamais en attribut `style`) ; sans
interactivité, la hauteur de la feuille s'applique. Le même corps sert dans un `OmniDialog`, où la hauteur
par défaut tient dans la fenêtre (`60vh`). `BodyHeight="auto"` rend l'ancien comportement : le corps suit
son contenu et, dans un conteneur en colonne, le pied se colle au bord bas. Le bandeau des boutons ne prend un fond
(`--omni-wizard-actions-background`, la surface de carte par défaut) que tant qu'il est collé, par une
requête de conteneur `scroll-state` : une étape courte ne peint aucune barre sur la page. Sans cette
requête, le bandeau reste transparent. La piste de progression a l'épaisseur commune des barres (2 px).

Les étapes prennent leur rang dans l'ordre de leur premier rendu : une étape affichée plus tard sous
condition passe en fin de liste. Un assistant aux étapes variables les déclare toutes.

L'assistant transmet à ses `OmniStepsItem`, par une valeur en cascade interne, l'identifiant de son
corps : le bouton de chaque étape contrôle ce panneau et l'élément ne rend pas de panneau à lui. Hors
d'un assistant, chaque étape garde son propre panneau.

`OmniStepsItem.Icon` (fragment, comme l'`Icon` des autres composants, en général un `OmniIcon`) affiche
une icône dans la pastille de l'étape à la place de son numéro (une étape franchie, par exemple) ;
l'icône est décorative et le numéro reste lu par les technologies d'assistance.

## Liste de définitions : `OmniDescriptionList` et `OmniDescriptionItem`

Un `dl` dont chaque `OmniDescriptionItem` (`Term`, la valeur en `ChildContent`, `ActionsContent` après elle)
est un groupe `dt`/`dd`. `Icon` (en général un `OmniIcon`, décoratif) précède le libellé, à la couleur
d'accent et à la taille fixée par le paquet, sur la même ligne que le texte. `Columns` range les éléments sur 1 à 4 colonnes (borné) ; sous 40rem, une
seule colonne.

## Infobulle : largeur et texte long

`OmniTooltip.MaxWidth` borne la largeur de la boîte ouverte : `Standard` (18 rem, défaut), `Narrow`
(12 rem) ou `Wide` (28 rem). Au-delà, le texte passe à la ligne ; il n'est jamais coupé par la largeur.
Un texte plus long que `CompactLength` (240 caractères par défaut) s'ouvre sur un aperçu coupé à la
dernière frontière de mot, terminé par une ellipse, et une action « Afficher plus » qui déplie tout ;
« Afficher moins » le replie, et quitter l'infobulle la replie aussi. La boîte d'un tel texte prend le
pointeur : un pont transparent couvre l'écart entre elle et le pointeur, et elle cesse de le suivre dès
qu'il y entre. Le texte complet reste la description accessible du déclencheur. `CompactLength` à 0
ou null garde toujours le texte entier.

## Date relative : `OmniRelativeTime`

`<OmniRelativeTime Value="Signal" RefreshInterval="TimeSpan.FromMinutes(1)" />` écrit « il y a 2 min »
(« 2 min ago » en anglais) dans un élément `time` dont `datetime` porte l'instant exact en UTC. La date
absolue (`Format`, date et heure complètes de la culture par défaut, dans `TimeZone`, le fuseau local
par défaut) s'affiche dans une infobulle au survol et au focus clavier, et décrit l'élément aux lecteurs
d'écran. L'unité est la plus grande contenue au moins une fois, arrondie vers le bas : secondes,
minutes, heures, jours, mois de trente jours, années de 365 jours ; moins de cinq secondes donne « à
l'instant », une date future « dans 5 min » (ses unités viennent de clés propres au futur,
`RelativeTimeFutureSeconds` à `RelativeTimeFutureYears`, pour les langues où l'unité s'accorde autrement
après « dans »). Maintenant est l'heure du `TimeProvider` enregistré dans les services de l'hôte
(`TimeProvider.System` s'il n'en enregistre aucun). `RefreshInterval` redessine l'étiquette sur un
minuteur créé par cette horloge, démarré après le rendu, détruit avec le composant.
