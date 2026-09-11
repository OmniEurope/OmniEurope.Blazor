# Carte mentale (famille Diagram)

La famille Diagram fournit une carte mentale interactive dessinée en SVG : `OmniMindMap`, sa barre
d'actions `OmniMindMapToolbar` et son panneau `OmniMindMapNodeProperties`. Le comportement est celui de
l'éditeur de cartes mentales de Pronoia, porté sans dépendance ni emprunt à une autre bibliothèque de
composants ; seuls le clavier et l'accessibilité ont été ajoutés, parce que le contrat les exige.

## Document

`OmniMindMapDocument` est la carte : `RootId`, `Nodes`, `Edges` et `Notes`. Il est immuable ; chaque
modification produit un nouveau document, remonté par `DocumentChanged`, et l'historique garde les
trente derniers.

`OmniMindMapDocument.FromJson` et `ToJson` lisent et écrivent le format de graphe que Pronoia stocke
(`{"rootId","nodes","edges","notes"}`), à la main et dans l'ordre des propriétés du format. Les
propriétés inconnues sont conservées à chaque niveau (`AdditionalProperties`) et réécrites telles
quelles. Les caractères accentués et les apostrophes sont écrits comme l'éditeur d'origine les écrivait,
sans échappement : un document relu puis réécrit redonne le texte stocké octet pour octet, ce que prouve
un test sur les trois graphes réels du jeu d'essai de Pronoia. Ce texte est une donnée à stocker ; il
n'est jamais inséré dans du balisage.

Un nœud porte `Id`, `Label`, `Group`, `X` et `Y` (centre), `FontSize`, `Bold`, `Italic`, `Width` et
`Height` (0 ajuste la boîte au texte). Un nœud dont l'identifiant n'est pas fait de 1 à 64 lettres,
chiffres, `_` ou `-` reste dans le document mais n'est pas dessiné, comme dans l'éditeur d'origine. Un
graphe dont tous les nœuds sont à l'origine est disposé une fois en étoile à l'ouverture, sans que cela
compte comme une modification.

```razor
<OmniMindMap @bind-Document="Carte"
             @bind-ViewState="Vue"
             ReadOnly="LectureSeule"
             AriaLabel="Carte du projet"
             NodeSelected="SurSelection">
    <ToolbarContent>
        <OmniMindMapToolbar />
    </ToolbarContent>
    <PanelContent>
        <OmniMindMapNodeProperties />
    </PanelContent>
</OmniMindMap>
```

## Paramètres et événements

| Membre | Rôle |
| --- | --- |
| `Document` / `DocumentChanged` | La carte, et la nouvelle carte après chaque modification (ajout, déplacement, renommage, couleur, suppression, lien, réorganisation, annulation, rétablissement). |
| `ReadOnly` | La carte se sélectionne, se déplace, se zoome, se centre et s'ajuste, mais rien n'est ajouté, déplacé, recoloré ni supprimé. |
| `ContextLabels` | `OmniMindMapLabels` : textes des actions pour un hôte dont le vocabulaire diffère. Un texte laissé nul vient des ressources. Le menu contextuel, la barre et les annonces emploient les mêmes. |
| `NodeSelected` | Le nœud sélectionné, ou `null` quand aucun nœud seul ne l'est (rien, un lien, plusieurs nœuds). |
| `ViewState` / `ViewStateChanged` | Décalage et zoom (0,1 à 5). Nul au premier rendu : la carte s'ajuste au canevas, et se réajuste quand le canevas change de taille tant que ni le lecteur ni l'hôte n'ont déplacé la vue. |
| `NodeRenameRequested` | Demande de renommage (double clic, F2, Entrée, menu). Sans gestionnaire, le focus va au champ texte d'un `OmniMindMapNodeProperties` de la carte. |
| `AriaLabel` | Nom accessible du canevas, « Carte mentale » par défaut. |
| `ToolbarContent`, `PanelContent` | Emplacements au-dessus et à côté du canevas. |

`OmniMindMapToolbar` propose exactement les actions de la page d'origine : nœud, supprimer, dupliquer,
lien, centrer, réorganiser, ajuster ; en lecture seule, centrer et ajuster seulement.
`OmniMindMapNodeProperties` édite le texte, la couleur (liste et pastilles), la taille de police (8 à
48), le gras, l'italique, la largeur (0 à 500) et la hauteur (0 à 300) du nœud sélectionné ; il ne
montre rien sans sélection et reste visible mais désactivé en lecture seule. Tous deux doivent être
placés dans leur emplacement de la carte et lèvent sinon.

## Pointeur, clavier et annonces

Au pointeur : glisser un nœud, glisser une sélection multiple, lasso sur le fond, panoramique au bouton
du milieu ou au doigt, zoom à la molette ou au pincement, double clic sur un nœud pour le renommer et
sur le fond pour y créer un nœud, menu contextuel au clic droit ou à l'appui long.

Le canevas est un `role="application"` nommé, décrit par une aide clavier, avec `aria-activedescendant`
sur le nœud sélectionné et une région live polie qui annonce sélection, ajout, suppression, lien, zoom,
annulation et rétablissement.

| Touche | Action |
| --- | --- |
| Flèches | Nœud le plus proche dans cette direction |
| Début | Nœud central |
| Maj + flèches | Déplacer la sélection de 10 unités |
| Entrée, F2 | Renommer ; en mode lien, choisir l'extrémité |
| N, D, C | Nouveau nœud, dupliquer, centrer |
| Suppr, Retour arrière | Supprimer la sélection |
| +, -, 0 | Zoom avant, arrière, ajuster |
| Ctrl+Z, Ctrl+Y (ou Ctrl+Maj+Z) | Annuler, rétablir |
| Menu, Maj+F10 | Menu contextuel ; flèches pour le parcourir, Échap pour le fermer et rendre le focus |
| Échap | Fermer le menu, annuler le lien, sinon désélectionner |

## CSP et thème

Razor rend tout le dessin ; `omni-mindmap.js` ne fait que suivre le geste en cours en écrivant des
attributs SVG (`transform`, `d`, `x`, `y`, `width`, `height`, `visibility`) et des classes, puis rend le
résultat à .NET. Il n'écrit ni `style`, ni propriété CSS, ni `innerHTML`. Le menu contextuel est placé
par les attributs géométriques d'un `foreignObject`. La carte fonctionne donc sous `style-src 'self'`.

Chaque couleur de nœud est un groupe de trois jetons, `--omni-mindmap-{groupe}-fill`, `-stroke` et
`-text`, pour `root`, `green`, `blue`, `yellow`, `red`, `pink`, `orange`, `purple`, `teal`, `indigo`,
`gray` et `note`, plus `--omni-mindmap-edge` pour les liens. Le texte de chaque groupe atteint au moins
4,5:1 sur son fond. La sélection, le lien en cours et le lasso reprennent les jetons d'accent, de succès
et de danger du thème. `prefers-reduced-motion` coupe la transition du contour des nœuds.

## Preuves et limites

- Tests bUnit : format et aller-retour, rendu, événements, clavier, lecture seule, menu, mode lien,
  historique borné, barre et panneau, ressources fr et en.
- `eng/Test-ShowcaseMindMapProbe.mjs` pilote la démonstration de la vitrine publiée dans Chromium avec des
  événements de confiance (glisser, molette, clavier, menu contextuel, double clic) et échoue sur toute
  violation CSP ou erreur console. Il n'est pas encore branché dans la CI : l'hôte le lance à la main
  contre la vitrine publiée et un navigateur ouvert avec `--remote-debugging-port`.
- Le pincement et l'appui long n'ont pas été exercés sur un écran tactile réel.
- On ne crée pas de nœud au-delà de 500 ni de lien au-delà de 1 000, comme dans l'éditeur d'origine. Un
  document plus grand reçu de l'hôte est en revanche dessiné et réécrit en entier, là où l'éditeur
  d'origine le tronquait au chargement.
