# Retours d'état (famille Feedback)

Ces composants disent où en est quelque chose : un message, un état, une progression, un chargement. La couleur n'est jamais le seul signal : un texte, une icône, un rôle ou un attribut ARIA l'accompagne toujours ([accessibility-contract.md](contracts/accessibility-contract.md)).

| Composant | Rôle |
| --- | --- |
| `OmniAlert` | Message dans la page (sévérité `OmniSeverity` : information, succès, avertissement, erreur), remplissage `Fill` (`OmniFill` : `Outline` par défaut, `Tonal`, `Solid`), titre facultatif, bouton de fermeture et annonce aux technologies d'assistance (`Live`). |
| `OmniBadge` | Étiquette courte dans une pastille colorée (`Tone`, un `OmniTone` ; `Fill`, un `OmniFill`) : un état, un compte, une étiquette ; une icône peut précéder le texte ou rester seule. |
| `OmniStatusBadge<TValue>` | Badge d'une valeur d'état dont la couleur, le libellé, l'icône et l'explication viennent d'une table d'états que l'hôte construit une fois par type ; avec une date et un seuil, il dit aussi quand la valeur est périmée, et le devient seul à l'écran quand le seuil passe (horloge `TimeProvider` de l'hôte). |
| `OmniStatusStrip` | Rangée d'états : dernières exécutions d'une tâche en points colorés, tranches d'une fenêtre de disponibilité en segments, point pulsant pour ce qui tourne encore ; chaque élément a son nom, son infobulle et peut mener ailleurs. Elle lit la même table d'états que `OmniStatusBadge`. |
| `OmniIcon` | Icône Phosphor intégrée, décorative par défaut ou nommée ; accepte aussi un tracé fourni par l'hôte. |
| `OmniImage` | Image responsive avec texte alternatif, chargement différé et dimensions natives facultatives. |
| `OmniProgressBar` | Avancement d'une tâche (`role="progressbar"`), en ligne ou en anneau, teinté par `Tone` (`Accent` par défaut), indéterminé tant que la fin n'est pas connue (`ShowValue` affiche alors « En cours » ou `ValueText`, jamais un faux pourcentage). |
| `OmniLoadingBar` | Barre fine de chargement de page, en balayage ou en remplissage continu, qui reste le temps de finir sa course puis s'efface. |
| `OmniSkeleton` | Silhouette de chargement, décorative ou région `status` nommée, d'une à dix lignes. |
| `OmniLogoLoader` | Indicateur de chargement au logo du site (`ChildContent` : une image, un SVG ou une `OmniIcon`), en trois tailles (`Size`) : le logo flotte de haut en bas sans jamais tourner, reste immobile si l'utilisateur demande moins de mouvement, et l'état est dit en mots (`Label`, région `status`). |

Détails : `OmniBadge`, `OmniIcon`, `OmniImage`, `OmniProgressBar` et `OmniSkeleton` dans [foundation-components.md](foundation-components.md) ; `OmniStatusStrip` dans sa section « Bande d'états et écran de démarrage ».
