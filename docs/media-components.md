# Média : scène d'un flux principal et de vignettes (famille Media)

La famille Media regroupe les composants des scènes de vidéo : un appel, un partage d'écran. Elle ne dépend d'aucun
service : le flux et ce qui le porte restent dans le navigateur, et l'application relie ses participants à la scène.

| Composant | Rôle |
| --- | --- |
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
