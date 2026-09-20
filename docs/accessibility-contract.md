# Contrat d'accessibilité

## Règles communes

- Utiliser l'élément HTML natif lorsqu'il existe (`button`, `input`, `select`, `nav`, `table`, `fieldset`).
- Conserver un ordre de focus identique à l'ordre du document ; la bibliothèque n'émet aucun `tabindex` positif par défaut et les consommateurs ne doivent pas en fournir via `TabIndex` ou `AdditionalAttributes`.
- Afficher un anneau `:focus-visible` contrasté et respecter `prefers-reduced-motion`.
- Exposer `disabled`, `aria-busy`, `aria-invalid`, `aria-current`, `aria-selected` et `aria-expanded` sous forme de chaînes ARIA valides.
- Relier libellés, descriptions et erreurs avec `for`, `aria-describedby` et les landmarks appropriés.
- Annoncer les résultats asynchrones, notifications, progression et erreurs avec des régions live `polite`; réserver `assertive` aux erreurs bloquantes.

## Motifs clavier

| Famille | Commandes minimales |
|---|---|
| Boutons et choix | `Entrée` et `Espace` via les éléments natifs. |
| Menus et superpositions | `Échap` ferme; flèches ouvrent ou parcourent lorsque le motif le prévoit. |
| Tabs | Gauche/Droite, Début/Fin, roving `tabindex`. |
| Arbre | Droite développe, Gauche réduit, Entrée/Espace sélectionne sans remonter à l'ancêtre. |
| Grille | En-têtes de tri et filtres atteignables; associations `th`/`td` et annonce `aria-sort`. |

Les couleurs d'état ne sont jamais l'unique information : texte, icône, rôle ou attribut ARIA complète toujours le signal visuel.

## Focus d'ouverture d'une surcouche (exception documentée à `STD-FOCUS`)

La règle `STD-FOCUS` du kit `_Generic` interdit `autofocus` : une page ne réclame jamais le focus
d'elle-même au démarrage ni pendant la navigation. Deux composants de la bibliothèque portent
pourtant cet attribut, et c'est voulu :

| Composant | Élément | Pourquoi |
|---|---|---|
| `OmniSplitButton` | `div.omni-split-button__menu`, `role="menu"`, `tabindex="-1"` | Le menu n'existe dans le DOM qu'après un clic de l'utilisateur sur le chevron. Le focus doit entrer dans le menu pour que `Échap`, les flèches et le piège de focus fonctionnent (`omni-focus.js`, `activateMenu` ; `restoreFocus` rend le focus au déclencheur à la fermeture). |
| `OmniDialog` | `button.omni-dialog__close` | Le dialogue est modal (`role="dialog"`, `aria-modal="true"`) et ouvert par l'utilisateur. Déplacer le focus dans le dialogue à l'ouverture est exigé par `STD-DIALOG` ; le bouton de fermeture est la cible la moins destructrice. Le balisage est figé par `OptInEvolutionTests`. |

Dans les deux cas le focus suit une action de l'utilisateur sur une surcouche qu'il vient d'ouvrir,
jamais un rendu de page. L'exclusion mécanique correspondante est déclarée dans
`.config/verify-rules.json` avec cette section pour raison.

## État de la vérification

Les tests automatisés actuels couvrent une partie du HTML sémantique, des attributs ARIA et des interactions simulées avec bUnit. Ils ne constituent pas une validation exhaustive en navigateur : les parcours clavier réels, le focus des superpositions, un moteur d'audit accessibilité et les technologies d'assistance restent à vérifier avant de revendiquer une conformité complète. `OmniTabs` expose les rôles `tablist`, `tab` et `tabpanel`, applique un `tabindex` roving `0/-1`, et ses flèches, `Début` et `Fin` déplacent le focus DOM vers l'onglet choisi avant de le sélectionner (`omni-focus.js`, `configureTabs`). Ce parcours n'est pas encore vérifié avec une technologie d'assistance.
