# PLAN-002 - Composants média pour le partage d'écran

Demande d'une application cliente du 2026-11-18 (module de partage d'écran WebRTC pair à pair, salons de 6 au plus) : une tuile vidéo d'un flux `MediaStream` avec un calque de dessin partagé, et une scène d'un flux principal et de vignettes. Rien ne part vers un serveur : le flux et les traits restent dans le navigateur, l'application transporte les traits elle-même.

## Contrat d'API (choix de l'orchestrateur, 2026-11-18)

### Lot 1 : `OmniInkCanvas` collaboratif (famille Editor)

- `OmniInkStroke.Id` (`string?`, `init`, null par défaut) : la zone donne un identifiant neuf (`Guid` "N") à chaque trait qu'elle dessine ; un trait venu de l'hôte garde le sien.
- `StrokeAdded` (`EventCallback<OmniInkStroke>`) : levé pour chaque trait dessiné sur la zone (après `ValueChanged`), jamais pour un `Value` donné par l'hôte.
- `StrokesRemoved` (`EventCallback<IReadOnlyList<OmniInkStroke>>`) : levé pour chaque retrait fait par la zone (gomme, annuler, tout effacer, au pointeur, au clavier, à la barre, ou par `UndoAsync`/`ClearAsync`), jamais pour un `Value` donné par l'hôte.
- `Tool` (`OmniInkTool` : `Pen` par défaut, `Eraser`) et `ToolChanged` : la gomme retire chaque trait entier qu'elle touche ; la barre propose crayon et gomme.
- Annuler retire le dernier trait dessiné sur cette zone qui est encore dans le dessin, à défaut le dernier trait (identique en usage seul).

### Lot 2 : `OmniMediaTile` (nouvelle famille Media)

- `Stream` (`IJSObjectReference?`, un `MediaStream` JavaScript de l'application) posé en `srcObject` d'un `<video>` ; null = état « pas de flux » (initiales sur un disque).
- `Name`, `Initials` (déduites de `Name` par défaut), `MicrophoneMuted`, `ScreenSharing`, `CameraActive` (icônes d'état), `Muted` (son coupé, vrai par défaut), `Fit` (`OmniMediaFit` : `Contain` par défaut, `Cover`), plein écran (`AllowFullscreen`, `ToggleFullscreenAsync`), `ActionsContent`, `Label`.
- Calque de dessin optionnel (`Drawing`, `Strokes`/`StrokesChanged`, `StrokeAdded`, `StrokesRemoved`, `DrawingTool`) : un `OmniInkCanvas` posé exactement sur l'image vidéo affichée (bandes noires exclues en `Contain`, image entière débordante en `Cover`), coordonnées 0..1 de l'image vidéo, identiques sur toutes les tailles.
- `ExportPngAsync` / `ExportDataUrlAsync` : image vidéo courante à sa résolution native et traits par-dessus, en PNG, dans le navigateur.

### Lot 3 : `OmniMediaStage<TItem>` (famille Media)

- `Items`, `ItemTemplate`, `ItemKey`, `MainItem`/`MainItemChanged` (premier élément par défaut), `Label`.
- Un conteneur plat à clés stables : changer le principal déplace les éléments sans recréer les `<video>`.
- Clic sur une vignette ou son bouton « Afficher en grand » (clavier) : bascule du principal.
- Responsive par requête de conteneur : large = principal à gauche, vignettes à droite ou dessous ; 375 px = principal en haut, vignettes défilantes dessous. Démontré avec 6 vignettes.

## Lots

| Lot | Objet | État |
|---|---|---|
| 1 | `OmniInkCanvas` collaboratif | livré (44556fb3), tests et sonde ink rejoués |
| 2 | `OmniMediaTile` + démonstration complète (deux tuiles qui s'échangent les traits, export PNG) | à faire, après le lot 1 |
| 3 | `OmniMediaStage<TItem>` | à faire, en parallèle du lot 1 |

## Critère d'acceptation (de la demande)

Composants documentés ; démontrés dans la galerie avec un flux de démonstration (`canvas.captureStream`, `getUserMedia` en option) ; dessin visible sur deux instances qui s'échangent les traits ; export PNG vidéo + traits ; aucun liseré ; vérifiés à 1440 et 375 px dans un vrai navigateur. La publication du paquet reste décidée par le propriétaire.
