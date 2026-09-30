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
    <Actions><OmniButton OnClick="Modifier"><OmniIcon Name="OmniIconName.Edit" /><span>Modifier</span></OmniButton></Actions>
    <MenuContent>
        <OmniMenuItem OnClick="Archiver">Archiver</OmniMenuItem>
        <OmniMenuItem Tone="OmniTone.Danger" OnClick="Supprimer">Supprimer</OmniMenuItem>
    </MenuContent>
    <Filters><OmniTextBox @bind-Value="Recherche" /></Filters>
</OmniPageHeader>
```

Deux lignes de hauteur fixe (R-395, demande de l'équipe Aetheus), si bien que toutes les pages
commencent leur contenu à la même hauteur :

- ligne 1 : bouton retour, icône, titre, badges, actions, puis le menu « ⋮ » (`MenuContent`). Un titre
  trop long défile entre deux chevrons posés de chaque côté au lieu de passer à la ligne. Sur téléphone,
  et à toute largeur quand la ligne 1 manque de place, un bouton « Voir plus » (`aria-expanded`)
  déplie badges et actions, le menu « ⋮ » restant visible ;
- ligne 2, toujours réservée : le fil d'Ariane sous le titre, dont le dernier maillon est le titre de la
  page, ou le sous-titre quand la page n'a pas d'ancêtre à montrer (ou avec `ShowTrail="false"`).

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
| `Badges`, `Actions` | Après le titre, sur la ligne 1. Sur téléphone, et à toute largeur quand la ligne 1 manque de place, ils se replient derrière le bouton « Voir plus » (`aria-expanded`). |
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
    <Header><OmniPageHeader ShowBack="true" /></Header>
    <ChildContent>@Body</ChildContent>
</OmniDetailShell>
```

`State` vaut `Loading` (défaut), `Found` ou `NotFound`. En chargement, `Header` est peint et un
squelette (ou `LoadingContent`) le suit, le conteneur porte `aria-busy`. Le contenu est toujours rendu,
masqué par `hidden` tant que l'entité n'est pas là : un enfant qui déclenche le chargement depuis son
propre cycle de vie doit exister pour le déclencher. `NotFound` remplace l'en-tête par un état vide
(`NotFoundTitle`, `NotFoundDescription`) et un bouton retour (`BackHref`, `BackText`, sinon
l'historique) ; `NotFoundContent` remplace cet état entier.

## Page de connexion : `OmniLoginShell` et `OmniReturnUrl`

`OmniLoginShell` place `Logo` au-dessus d'une carte centrée (25rem au plus) qui porte le titre
(`Title`, « Connexion » par défaut, niveau `Level`), `Description`, le formulaire (`ChildContent`) et
`Footer`. La carte est nommée par son titre. Aucune authentification n'est embarquée.

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

`Icon` (un plateau vide par défaut, décoratif), `Title`, `Description` et `Actions`. Le titre est un
paragraphe ; `Level` en fait un titre quand l'état vide ouvre une section.

## Tuile de statistique : `OmniStatTile`

`Value` (déjà formatée), `Label` (obligatoire), `Detail` (ligne atténuée facultative) et `Icon` (carré
teinté, décoratif). Avec `OnClick`, la tuile entière devient un bouton, nommé « libellé : valeur » ; sans lui, un simple bloc que rien n'active. La valeur ne passe pas
à la ligne, le libellé et le détail peuvent se couper.

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

Les boutons restent au bas de la fenêtre quelle que soit la hauteur de l'étape : dans un conteneur qui le
range en colonne (un corps de dialogue, une page en colonne), l'assistant prend la hauteur restante et le
corps pousse le pied en bas ; une étape plus haute que la fenêtre défile sous le pied
(`omni-wizard__foot`), collé au bord bas. Le bandeau des boutons ne prend un fond
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

Un `dl` dont chaque `OmniDescriptionItem` (`Label`, la valeur en `ChildContent`, `Actions` après elle)
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
