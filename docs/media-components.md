# Média : tuile vidéo et scène de vignettes (famille Media)

La famille Media regroupe les composants des scènes de vidéo : un appel, un partage d'écran (une tuile vidéo, une scène qui en place plusieurs). Elle ne dépend d'aucun
service : le flux et ce qui le porte restent dans le navigateur, et l'application relie ses participants à la scène.

| Composant | Rôle |
| --- | --- |
| `OmniMediaTile` | Une vidéo en direct (un `MediaStream` de l'application) avec son nom, ses icônes d'état, les boutons de l'hôte, le plein écran et un calque de dessin posé exactement sur l'image ; export PNG de l'image avec les traits. |
| `OmniMediaStage` | Scène d'un grand élément principal et d'au plus six vignettes (au-delà, la bande défile) : l'élément principal se change d'un clic ou au clavier, sans recréer les éléments qui jouent un flux. |

## OmniMediaStage

`OmniMediaStage<TItem>` place des éléments ; il ne dessine rien d'eux. Ce que contient un élément (une vidéo, une
image, une carte) est le rendu de `ItemTemplate`, qui remplit le cadre 16:9 que la scène lui donne.

```razor
<OmniMediaStage TItem="Participant"
                Items="Participants"
                KeyOf="participant => participant.Id"
                ItemLabel="participant => participant.Name"
                @bind-MainItem="Main"
                Label="Appel">
    <ItemTemplate Context="participant">
        <ParticipantVideo Participant="participant" />
    </ItemTemplate>
</OmniMediaStage>
```

### Paramètres

- `Items` (obligatoire) : `IReadOnlyList<TItem>`. Une liste nulle est lue comme une liste vide. Les éléments gardent leur
  ordre dans le document et au clavier ; l'élément principal n'est pas déplacé dans la liste, il est seulement dessiné
  en grand.
- `ItemTemplate` (obligatoire) : `RenderFragment<TItem>`, rendu de chaque élément, principal ou vignette. Sans lui, la
  scène refuse de s'afficher (`ArgumentNullException`).
- `KeyOf` : identité stable d'un élément, posée en `@key`. Par défaut l'élément lui-même. Deux éléments ne partagent pas
  la même clé.
- `ItemLabel` : nom d'un élément pour le bouton de sa vignette et pour l'annonce (le nom d'un participant). Sans lui, ou
  avec un texte vide, l'élément est nommé par sa position (« Élément 3 »).
- `MainItem` et `MainItemChanged` : l'élément principal, lié dans les deux sens (`@bind-MainItem`). `null`, ou un élément
  qui n'est pas dans `Items` (comparé par `KeyOf` quand il est posé), laisse le premier élément principal. Le choix fait
  par un clic reste tant que le parent ne donne pas une autre valeur : un parent qui ne lie pas le paramètre ne le
  reprend pas. L'événement est levé avec l'élément devenu principal ; il ne l'est pas quand c'est le parent qui change la
  valeur, ni quand l'élément principal disparaît de `Items`.
- `Label` : nom accessible de la scène (groupe). Un texte localisé « Scène média » par défaut.
- `Id`, `Class` et les attributs supplémentaires vont sur l'élément le plus externe, comme pour les autres composants.

### Un conteneur plat, des clés stables

Tous les éléments sont les enfants directs d'un même élément, dans l'ordre de `Items`, chacun sous sa clé. Le rôle
(principal ou vignette) est une classe, `omni-media-stage__item--main`, et la place est celle de la feuille de style :
changer l'élément principal change une classe, jamais la structure. Le navigateur déplace les éléments existants au lieu
de les recréer : une `<video>` à l'intérieur continue de jouer, sans coupure ni nouveau flux. Les tests le prouvent par
la durée de vie des composants que rend le gabarit (aucun n'est supprimé ni recréé quand le principal change, et un
élément suit sa clé quand l'hôte réordonne la liste) et la sonde navigateur par l'identité de l'élément DOM.

### Une vignette se choisit à la souris, au toucher et au clavier

Chaque vignette porte son propre bouton, `<button type="button">` nommé « Afficher … en grand » (traduit dans les
24 langues), qui couvre la vignette sous son contenu. Un clic sur l'image tombe donc sur ce bouton, alors que les
contrôles que le contenu dessine (un bouton de micro, un lien, un champ) gardent leurs propres clics : le contenu ne
reçoit les événements du pointeur que sur ses contrôles interactifs. Le bouton est un frère du contenu et jamais un
parent ni un enfant d'un autre contrôle : aucun élément interactif n'en contient un autre. Il est dans l'ordre de
tabulation (Tab pour l'atteindre, Entrée ou Espace pour l'activer) et montre, au survol et au focus, un cadre complet et
une icône d'agrandissement ; sur un écran sans survol l'icône est toujours visible. L'élément principal n'a pas de bouton : une fois le choix fait, le focus passe à son
élément (`tabindex="-1"`, cadre de focus visible au clavier), pour qu'un utilisateur du clavier ne perde pas sa place
quand le bouton qu'il vient d'activer quitte la vignette.

Le changement est annoncé poliment (`role="status"`, `aria-live="polite"`) : « Grace est affiché en grand ».

### Disposition

La disposition suit la largeur de la scène elle-même (requêtes de conteneur), pas celle de la fenêtre :

- **Large (40rem et plus)** : l'élément principal à gauche en 16:9, jamais plus haut que `40rem`, et les vignettes en une
  colonne à droite, de la hauteur de l'élément principal ; la colonne défile verticalement quand toutes les vignettes n'y
  tiennent pas.
- **Étroit (moins de 40rem, par exemple 375 px)** : l'élément principal en haut, les vignettes en une seule rangée dessous
  qui défile de côté.
- Une vignette garde son cadre 16:9. L'élément principal est de 16:9 à la largeur qui lui reste ; plafonné en hauteur, il
  reste de la même largeur et son contenu s'y centre.
- **Un seul élément** : il est l'élément principal, sans bande de vignettes. **Aucun élément** : la scène est vide : son
  élément et son nom accessible sont dessinés, elle ne montre rien.

La disposition n'emploie ni liseré ni attribut `style` : cadres complets, fond, ombre et espacement, et des longueurs de
conteneur seulement. Le placement de l'élément principal (hors flux, contre un cadre qui ne défile pas) est ce qui le
garde en place quand la rangée de vignettes défile.

Variables CSS (lues avec une valeur de repli, posées sur la scène ou un ancêtre) :

| Variable | Défaut | Effet |
|---|---|---|
| `--omni-media-stage-main-max-height` | `40rem` | Hauteur maximale de l'élément principal. |
| `--omni-media-stage-thumb-width` | `9rem` en étroit, `14%` de la scène entre `8rem` et `12rem` en large | Largeur d'une vignette. |
| `--omni-media-stage-gap` | l'espace petit du thème (`--omni-space-sm`) | Espace entre les éléments. |

### Limites

- Les vignettes ne sont pas réordonnées selon qui parle, et la scène ne choisit jamais l'élément principal elle-même :
  c'est le parent, par `MainItem`.
- Aucune disposition en grille de tous les participants à égalité : c'est une scène à un principal.
- Le placement des vignettes à droite ou dessous est celui de la largeur de la scène ; il n'y a pas de paramètre pour
  forcer l'une des deux.

## OmniMediaTile

`OmniMediaTile` montre une vidéo en direct que l'application possède (un pair WebRTC, un partage d'écran, une caméra) :
son nom, des icônes d'état, les boutons de l'hôte, un bouton plein écran et, en option, un calque de dessin posé
exactement sur l'image. Rien ne part vers un serveur : le flux et les traits restent dans le navigateur, et l'hôte
transporte lui-même les traits d'une tuile à l'autre.

```razor
<OmniMediaTile Stream="peerStream"
               Name="Ada Lovelace"
               Muted="false"
               MicrophoneMuted="peer.MicrophoneMuted"
               Drawing="true"
               @bind-Strokes="strokes"
               StrokeAdded="SendToPeers"
               StrokesRemoved="TellPeersRemoved">
    <ActionsContent>
        <OmniButton Label="Capturer" OnClick="Capture"><Icon><OmniIcon Name="OmniIconName.Image" /></Icon></OmniButton>
    </ActionsContent>
</OmniMediaTile>
```

### Le flux appartient à l'application

`Stream` est un `IJSObjectReference` vers un `MediaStream` JavaScript que l'application a créé dans son propre module
(`getUserMedia`, `getDisplayMedia`, une connexion pair à pair, `canvas.captureStream`) et rendu à .NET comme référence.
La tuile le pose en `srcObject` de son `<video>`, le remplace quand le paramètre change et vide `srcObject` quand il
devient nul. **Elle n'arrête, ne clone et n'ajoute aucune piste** : le flux, ses pistes et la référence restent à
l'application, qui les arrête et les libère, et la référence doit vivre tant qu'elle est le `Stream` d'une tuile.
Détruire la tuile vide son `srcObject` et libère son module, sans toucher au flux ; plusieurs tuiles peuvent montrer le
même flux. Un flux qui n'est pas un `MediaStream` est refusé par le navigateur (une `JSException`).

Sans flux, la tuile est dans l'état « pas de flux » : un disque d'initiales sur un fond de surface, avec son nom et ses
icônes. Les initiales viennent de `Initials`, sinon de `Name` (la première lettre des deux premiers mots, en majuscules) ;
sans l'un ni l'autre, le disque montre une icône de personne.

### Paramètres

- `Stream` (`IJSObjectReference?`), `Name` (`string?`, affiché dans une pastille sur le bas de la vidéo : un fond sombre
  translucide, lisible sur toute image) et `Initials` (`string?`).
- `MicrophoneMuted`, `ScreenSharing`, `CameraActive` (`bool`, faux par défaut) : icônes d'état en haut à gauche
  (`OmniIconName.MicrophoneSlash`, `Monitor`, `VideoCamera`), chacune avec son nom accessible localisé.
- `Muted` (`bool`, **vrai par défaut**) : la vidéo se joue sans son, ce qui la laisse démarrer seule (la politique de lecture
  automatique des navigateurs) et évite l'écho de son propre flux. Un hôte le passe à faux pour le flux d'un pair distant ;
  le navigateur peut alors attendre le premier appui ou la première touche pour jouer le son (la tuile le relance alors).
  La vidéo est `autoplay` et `playsinline`.
- `Fit` (`OmniMediaFit`) : `Contain` (par défaut) montre toute l'image avec des bandes là où les proportions diffèrent,
  `Cover` remplit la tuile et coupe le reste (le `object-fit` de la vidéo).
- `ShowFullscreen` (`bool`, vrai par défaut) : le bouton plein écran de la barre d'actions. La tuile entière passe en
  plein écran (calque, pastilles et barre avec elle) ; l'icône et le nom du bouton suivent l'état réel du navigateur
  (`fullscreenchange`), y compris une sortie par Échap. `ToggleFullscreenAsync()` fait de même depuis le code, que le bouton
  soit montré ou non ; si le navigateur refuse (pas de geste de l'utilisateur, pas de prise en charge) rien ne change.
  Ce paramètre s'appelle `ShowFullscreen` et non `AllowFullscreen`, parce que la convention des bascules d'affichage est
  `Show` + élément ([public-api-conventions.md](public-api-conventions.md)).
- `ActionsContent` (`RenderFragment?`) : les boutons de l'hôte (capturer, enregistrer, arrêter), dans la barre d'actions en
  haut à droite de la vidéo, avant le bouton plein écran. Un bouton `Ghost` y est blanc sur le fond sombre de la barre.
- `Label` (`string?`) : nom accessible de la tuile (un groupe). Par défaut « Vidéo de {Name} », ou « Vidéo » sans nom.
- `Id`, `Class` et les attributs supplémentaires vont sur l'élément le plus externe, comme pour les autres composants.

### Le calque de dessin

`Drawing` (faux par défaut) pose un `OmniInkCanvas` **exactement sur l'image que la vidéo montre** : en `Contain` sur
l'image seule (les bandes noires sont exclues), en `Cover` sur toute l'image, qui déborde de la tuile et que la tuile
coupe. Le script de la tuile calcule ce rectangle à partir de `videoWidth` par `videoHeight` et de la boîte de l'élément,
à `loadedmetadata`, à `resize` (la piste change de taille) et par un `ResizeObserver`, et le pose par des propriétés
personnalisées écrites dans le CSSOM, jamais par un attribut `style` (CSP stricte). Les coordonnées 0 à 1 d'un trait sont
donc celles de l'image vidéo : elles sont les mêmes sur toutes les tailles de tuile, et un trait envoyé à une tuile plus
petite, carrée ou en plein écran se pose au même endroit de l'image. Tant qu'il n'y a pas d'image (pas de flux, ou
pas encore de taille), le calque n'a pas de taille.

Les paramètres sont ceux de la zone de dessin : `Strokes` et `StrokesChanged` (la valeur), `StrokeAdded`, `StrokesRemoved`
(ce que fait l'utilisateur sur cette tuile, jamais ce que l'hôte donne par `Strokes`), `DrawingTool` (`OmniInkTool`) et
`DrawingToolChanged`, `StrokeColor` (un rouge vif `#ff3b30` par défaut, lisible sur la plupart des vidéos : la couleur du
thème de la zone seule serait sombre sur une image) et `StrokeWidth` (2 par défaut). Voir « Dessin partagé » de
[editor-components.md](editor-components.md) pour le transport des traits : la tuile n'en fait aucun, l'hôte envoie
chaque `StrokeAdded` aux autres tuiles (avec le même `Id`) et retire par `Id` ce que `StrokesRemoved` annonce.

`ShowDrawingToolbar` (vrai par défaut) montre la barre de dessin (crayon, gomme, couleur, épaisseur, Annuler, Effacer)
**au-dessus de la vidéo** et non sur l'image, pour qu'elle ne la couvre jamais. C'est une barre de la tuile, avec les
textes de la zone de dessin ; le canevas, lui, n'a pas de barre propre. Sans elle, l'hôte pilote le dessin par
`DrawingTool` et ses propres boutons.

Le cadre du canevas est transparent sur la vidéo (`--omni-ink-background: transparent`) et sans bordure : un cadre qui
aurait une bordure rognerait le canevas et décalerait tous les traits.

### Export

`ExportPngAsync(CancellationToken)` et `ExportDataUrlAsync(CancellationToken)` rendent l'image vidéo courante **à sa
résolution native** (`videoWidth` par `videoHeight`, ramenée à 8192 au plus sur le grand côté en gardant les proportions)
avec les traits par-dessus aux mêmes positions relatives et à la même épaisseur proportionnelle que
`OmniInkCanvas.ExportPngAsync` (le même moteur de tracé, `wwwroot/shared/ink-render.js`, dans le navigateur). Un trait sans
couleur propre est noir. L'image ne contient ni la pastille du nom, ni les icônes, ni la barre. Avec `Drawing` éteint, les
traits ne sont pas dessinés. Sans `Stream`, ou tant que la vidéo n'a pas décodé sa première image, l'export lève
`InvalidOperationException` ; après la destruction de la tuile, `ObjectDisposedException`.

### Aspect

Une tuile 16:9 aux coins arrondis, sur un fond sombre, dont les pastilles (nom, icônes, barre) sont des boîtes pleines à
fond sombre translucide : aucun liseré, ni bordure d'un seul côté ni ombre intérieure. Elle ne bouge rien, donc
`prefers-reduced-motion` n'a rien à calmer, et elle tient à 375 px. Les pastilles laissent passer le pointeur vers le dessin, sauf les boutons.

Variables CSS (lues avec une valeur de repli, posées sur la tuile ou un ancêtre) :

| Variable | Défaut | Effet |
|---|---|---|
| `--omni-media-tile-aspect` | `16 / 9` | Proportions de la scène de la tuile (`4 / 3`, `1`). |
| `--omni-media-tile-background` | `#0b0d12` | Fond derrière la vidéo (les bandes de `Contain`). |

### Limites

- La tuile ne crée aucun flux et ne gère aucune connexion : ni `getUserMedia`, ni WebRTC, ni signalisation. La
  démonstration de la vitrine fabrique son flux dans un petit module à elle (`canvas.captureStream`), et propose la caméra en option.
- Pas de sous-titres : la vidéo est un flux en direct. Elle est cachée aux lecteurs d'écran (`aria-hidden`), la tuile est
  nommée par son `Label` et ses icônes d'état ont un texte.
- Le son d'un flux non coupé peut attendre un geste de l'utilisateur (politique du navigateur).
- Le déploiement statique de la vitrine répond `Permissions-Policy: camera=()` : la caméra y est refusée, le bouton « Utiliser ma caméra »
  de la démonstration le dit au lieu d'échouer en silence.
