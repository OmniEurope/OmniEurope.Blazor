# Pages (famille Pages)

La famille Pages réunit les compositions qui habillent une page d'application : l'en-tête et son fil
d'Ariane, la fiche détaillée, la page de connexion, le voile de connexion perdue, l'état vide,
l'assistant à étapes, la liste de définitions et la date relative. Elles sont faites des composants de
base du paquet (`OmniBreadcrumb`, `OmniHeading`, `OmniButton`, `OmniIcon`, `OmniSkeleton`,
`OmniSteps`, `OmniProgressBar`, `OmniTooltip`, `OmniCard`) et restent du côté du navigateur : aucune
ne fait d'appel réseau, ne tient de connexion, d'authentification ni d'horloge. Les données arrivent
par paramètres, délégués et fragments ; le temps, par un paramètre ou un `TimeProvider`.

## En-tête de page : `OmniPageHeader` et `OmniBreadcrumbService`

```razor
<OmniPageHeader ShowBack="true" Subtitle="@Projet.Description">
    <Badges><OmniBadge Variant="OmniBadgeVariant.Warning">Archivé</OmniBadge></Badges>
    <Actions><OmniButton OnClick="Modifier"><OmniIcon Name="OmniIconName.Edit" /><span>Modifier</span></OmniButton></Actions>
    <Filters><OmniTextBox @bind-Value="Recherche" /></Filters>
</OmniPageHeader>
```

Un bloc encadré : les ancêtres sur la première ligne, puis retour, titre, badges et actions. Le sous-titre
et les filtres viennent sous le cadre.

| Paramètre | Rôle |
| --- | --- |
| `Title` | Le titre. Vide, c'est le dernier maillon du fil qui titre la page, et un maillon encore en chargement affiche un squelette à sa place. |
| `Level` | Niveau du titre, `H1` par défaut. |
| `Subtitle`, `Filters` | Une ligne d'explication et une rangée de filtres, sous le cadre. |
| `ShowTrail`, `TrailLabel` | La ligne des ancêtres (par défaut) et son nom accessible. Sans ancêtre, la ligne garde sa hauteur sans rendre de repère vide, pour que toutes les pages commencent leur contenu à la même hauteur. |
| `ShowBack`, `BackHref`, `BackLabel` | Le bouton retour : vers `BackHref`, sinon vers l'ancêtre le plus proche qui porte un lien, sinon un pas en arrière dans l'historique du navigateur. |
| `Badges`, `Actions` | Après le titre. Sous 40rem de large, ils se replient derrière un bouton (`aria-expanded`, `aria-controls`) et prennent une ligne entière une fois ouverts. |
| `Icon` | Icône avant le titre (`aria-hidden`, couleur primaire). Le titre est alors rogné à ses capitales, si bien que le centre de l'icône tombe sur le centre du texte quelle que soit la police. |

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

`OmniReturnUrl` garde l'adresse de retour dans l'application : `IsLocal` n'accepte qu'un chemin qui
commence par une seule barre oblique, sans schéma ni hôte (`//hote`, `/\hote`, `https://hote`), sans
barre oblique inverse ni caractère de contrôle, de 2048 caractères au plus. `Resolve(returnUrl, "/")`
rend l'adresse si elle est locale, le repli sinon ; `Append("/login", returnUrl)` l'ajoute en paramètre
de requête (`returnUrl` par défaut) seulement si elle est locale. L'hôte exclut lui-même ses propres
pages d'authentification s'il le souhaite.

## Connexion perdue : `OmniConnectionOverlay`

Un voile bloquant au-dessus de tout (`z-index` 130) tant que `State` n'est pas `Connected` :

| État | Titre par défaut | Action |
| --- | --- | --- |
| `Reconnecting` | Connexion perdue | « Se reconnecter maintenant », `OnReconnect`. Un indicateur tourne tant que `Busy` est faux, et `SecondsUntilRetry` affiche le compte à rebours. |
| `Failed` | Connexion impossible | « Se reconnecter maintenant », `OnReconnect`. Plus de compte à rebours. |
| `Rejected` | Session interrompue | « Recharger la page », `OnReload`, sinon rechargement complet de la page. |

`Reason` s'affiche tel quel (« Cause : ... »). `Title` et `Description` remplacent les textes de l'état.
La carte est un `alertdialog` modal nommé et décrit ; elle prend le focus sur son action, garde Tab en
elle et rend le focus à la reconnexion. Le compte à rebours reste hors de la région vivante, qu'un
lecteur d'écran lirait sinon à chaque seconde. L'hôte tient le compte : le composant n'a pas d'horloge.

## État vide : `OmniEmptyState`

`Icon` (un plateau vide par défaut, décoratif), `Title`, `Description` et `Actions`. Le titre est un
paragraphe ; `Level` en fait un titre quand l'état vide ouvre une section.

## Tuile de statistique : `OmniStatTile`

`Value` (déjà formatée), `Label` (obligatoire), `Detail` (ligne atténuée facultative) et `Icon` (carré
teinté, décoratif). Avec `OnClick`, la tuile entière devient un bouton, nommé par `AriaLabel` ou, à
défaut, par « libellé : valeur » ; sans lui, un simple bloc que rien n'active. La valeur ne passe pas
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

Les étapes prennent leur rang dans l'ordre de leur premier rendu : une étape affichée plus tard sous
condition passe en fin de liste. Un assistant aux étapes variables les déclare toutes.

`OmniStepsItem` gagne pour cela `PanelId` : l'identifiant d'un panneau rendu ailleurs, que le bouton
contrôle, l'élément ne rendant alors pas de panneau à lui. Laissé nul, rien ne change.

## Liste de définitions : `OmniDescriptionList` et `OmniDescriptionItem`

Un `dl` dont chaque `OmniDescriptionItem` (`Label`, la valeur en `ChildContent`, `Actions` après elle)
est un groupe `dt`/`dd`. `Columns` range les éléments sur 1 à 4 colonnes (borné) ; sous 40rem, une
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
l'instant », une date future « dans 5 min ». Maintenant vaut `Now`, sinon l'heure de `TimeProvider`
(`TimeProvider.System` par défaut). `RefreshInterval` redessine l'étiquette sur un minuteur créé par ce
`TimeProvider`, démarré après le rendu, détruit avec le composant ; un `Now` fixé n'en crée aucun.
