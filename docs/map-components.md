# Cartes : carte à tuiles, tracés et repères (famille Maps)

La famille Maps montre des lieux et des trajets sur une carte du monde. Le moteur est celui de la bibliothèque
(décision du propriétaire du 2026-11-19) : aucune bibliothèque de cartes tierce n'est utilisée ni copiée. La carte pose
des tuiles d'images de 256 pixels en Web Mercator, comme OpenStreetMap, et dessine par-dessus les tracés en SVG et les
repères en vrais boutons.

| Composant | Rôle |
| --- | --- |
| `OmniMap` | Carte à tuiles qu'on déplace et qu'on zoome à la souris, au doigt et au clavier, avec des tracés colorés, des repères cliquables et l'attribution des données toujours visible. |

## OmniMap

```razor
<OmniMap @bind-Center="Center"
         @bind-Zoom="Zoom"
         FitToContent="true"
         Lines="Lines"
         Markers="Markers"
         OnMarkerClick="Open"
         OnMapClick="Pick"
         Label="Trajet du jour" />

@code {
    private OmniGeoPoint Center { get; set; } = new(50.8466, 4.3528);
    private int Zoom { get; set; } = 13;

    private readonly IReadOnlyList<OmniMapLine> Lines =
    [
        new() { Points = [new(50.8270, 4.3720), new(50.8450, 4.3570)], Tone = OmniTone.Success, Tooltip = "08:00 à 08:05, sec" },
        new() { Points = [new(50.8450, 4.3570), new(50.8600, 4.3610)], Tone = OmniTone.Danger, Width = 7, Tooltip = "08:05 à 08:10, forte pluie" },
    ];

    private readonly IReadOnlyList<OmniMapMarker> Markers =
    [
        new() { Point = new(50.8270, 4.3720), Label = "Départ : place Flagey", Kind = OmniMapMarkerKind.Start },
        new() { Point = new(50.8600, 4.3610), Label = "Arrivée : gare du Nord", Kind = OmniMapMarkerKind.End },
    ];
}
```

### Paramètres

| Paramètre | Défaut | Rôle |
| --- | --- | --- |
| `Center` | Bruxelles (50.8466, 4.3528) | Centre de la vue, lié dans les deux sens avec `CenterChanged` : la carte le rapporte après un déplacement ou un zoom de l'utilisateur. |
| `Zoom` | `13` | Niveau de zoom entier, lié dans les deux sens avec `ZoomChanged`, borné par `MinZoom` et `MaxZoom`. |
| `MinZoom`, `MaxZoom` | `2`, `19` | Bornes du zoom, ramenées entre 0 et 22 ; `MinZoom` au-dessus de `MaxZoom` est refusé. |
| `FitToContent` | `false` | Cadre la vue sur tous les tracés et les repères : au premier rendu, puis chaque fois que la liste `Lines` ou `Markers` est remplacée (une autre liste, pas la même liste modifiée) et quand l'option passe à `true`. |
| `Lines` | vide | Les tracés (`OmniMapLine`). |
| `Markers` | vide | Les repères (`OmniMapMarker`). |
| `OnMarkerClick` | aucun | Un repère cliqué, ou activé par Entrée ou Espace (ce sont de vrais boutons). |
| `OnMapClick` | aucun | Le lieu sous le pointeur quand on clique la carte sans la glisser ; un glisser, un clic sur un repère, sur les boutons de zoom ou sur l'attribution n'en lève pas. |
| `TileUrl` | `OmniMap.DefaultTileUrl` (les tuiles d'OpenStreetMap) | Modèle d'adresse des tuiles avec exactement `{z}`, `{x}` et `{y}`, commençant par `https://`, `http://` ou un seul `/` ; tout autre modèle est refusé par une `ArgumentException`. |
| `Attribution` | l'attribution d'OpenStreetMap traduite, liée à sa page de copyright | Texte d'attribution des tuiles, toujours affiché dans le coin bas de la carte. Un serveur de tuiles autre que celui d'OpenStreetMap donne ici sa propre attribution. |
| `Label` | « Carte » traduit | Nom accessible de la région de la carte. |

`ZoomInAsync()` et `ZoomOutAsync()` zooment d'un niveau autour du centre, comme les boutons + et -.

`OmniGeoPoint` est un lieu en degrés décimaux (WGS 84) : `Latitude`, nord positif, et `Longitude`, est positif. Le Web
Mercator ne montre pas les pôles : une latitude est ramenée entre plus et moins `OmniGeoPoint.MaxLatitude`, une
longitude entre -180 et 180.

`OmniMapLine` est un tracé d'au moins deux `Points` : `Tone` (`OmniTone`, `Accent` par défaut) donne sa couleur au
thème ; `Color` la remplace par une couleur littérale (hexadécimale, `rgb()`, `rgba()`, `hsl()` ou `hsla()`), écrite en
attribut SVG `stroke`, et toute autre valeur est ignorée ; `Width` est l'épaisseur en pixels (`5` par défaut, au-dessus
de 0 et au plus 40) ; `Tooltip` s'affiche au survol par l'infobulle de la bibliothèque et figure dans la description
accessible de la carte ; `Class` s'ajoute au groupe SVG du tracé ; `Key` le suit d'un rendu à l'autre.

`OmniMapMarker` est un repère : `Point`, `Label` (obligatoire, nom du bouton et infobulle), `Kind`, `Icon` (remplace
l'icône du genre) et `Key`. `OmniMapMarkerKind` donne la forme : `Point` (épingle, par défaut), `Start` (départ, disque
vert), `Waypoint` (étape, petit disque), `End` (arrivée, disque rouge).

### Gestes et clavier

- Glisser (souris, doigt ou stylet) déplace la carte ; au-delà de 4 pixels, ce n'est plus un clic.
- La molette zoome d'un niveau autour du pointeur ; un double clic zoome autour du point cliqué, Maj et double clic
  dézoome. Les boutons + et - du coin haut zooment autour du centre, désactivés aux bornes.
- La carte reçoit le focus au clavier (anneau de focus visible) : les flèches la déplacent de 80 pixels, + et -
  zooment. Tab parcourt ensuite les repères ; un repère hors de la vue qui reçoit le focus est ramené au centre.
- Il n'y a aucune animation : la préférence de mouvement réduit est respectée sans réglage.

### Rendu et réseau

- Seules les tuiles de la vue et d'un anneau autour d'elle sont dans la page ; une tuile qui ne se charge pas reste
  cachée et laisse voir la surface de la carte, sans image cassée.
- Les tuiles sont demandées avec la politique de référent `strict-origin-when-cross-origin` : le serveur d'OpenStreetMap
  exige un référent, même quand la page sert `Referrer-Policy: no-referrer`.
- Les positions sont écrites par le CSSOM (propriétés personnalisées), jamais par un attribut `style` ; l'hôte doit
  autoriser l'origine des tuiles dans `img-src` (voir [le contrat CSP](contracts/csp-contract.md)).
- La hauteur se règle par `--omni-map-height` (`24rem` par défaut) sur la carte ou sur un ancêtre.
- Les couleurs viennent des jetons du thème, en clair comme en sombre ; un tracé est posé sur une bordure de la couleur
  de la surface, pour rester lisible sur les tuiles.

### Limites

- Pas de zoom à deux doigts ; les boutons, la molette et le double clic zooment.
- Les tracés et les repères ne se répètent pas quand on fait le tour du monde vers l'est ou l'ouest ; les tuiles, si.
- Chaque clic d'un double clic lève aussi `OnMapClick`.
