# Contrat d'accessibilité

## Règles communes

- Utiliser l'élément HTML natif lorsqu'il existe (`button`, `input`, `select`, `nav`, `table`, `fieldset`).
- Conserver un ordre de focus identique à l'ordre du document ; la bibliothèque n'émet aucun `tabindex` positif par défaut et les consommateurs ne doivent pas en fournir via `TabIndex` ou `AdditionalAttributes`.
- Afficher un anneau `:focus-visible` contrasté et respecter `prefers-reduced-motion`.
- Tenir les ratios de contraste (4,5 pour un texte, 3 pour un remplissage ou une marque, plancher de 1,7 pour une bordure) sur chacune des 392 combinaisons thème x palette x mode. Exception déclarée : les thèmes Relief, Givre et Aplat sont marqués « contraste non garanti » (décision du 2026-09-28, `OmniThemePreset.ContrastWaiver`) ; leurs écarts sont mesurés et comptés comme acceptés au lieu de faire échouer, sauf l'anneau de focus, obligatoire partout. Pour tous les autres thèmes, Épure compris : `ThemeContrastMatrixTests` le vérifie sur les jetons, la sonde `eng/Test-ThemeContrastProbe.mjs` sur la page peinte, un fond en dégradé étant mesuré sur chaque couleur qu'il traverse. Ces quatre thèmes dessinent un anneau de focus plein (liseré de surface puis anneau opaque de l'accent ou du texte), là où leur relief, leur transparence ou leurs aplats effaceraient l'anneau translucide livré. Les compromis de style consentis pour cela sont décrits dans [foundation-components.md](foundation-components.md).
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
d'elle-même au démarrage ni pendant la navigation. Un seul élément de la bibliothèque porte
pourtant cet attribut, et c'est voulu :

| Composant | Élément | Pourquoi |
|---|---|---|
| `OmniDialog` | `button.omni-dialog__close`, seulement sur un dialogue modal qui a une croix (`autofocus="@Modal"`) | Le dialogue est modal (`role="dialog"`, `aria-modal="true"`) et ouvert par l'utilisateur. Déplacer le focus dans le dialogue à l'ouverture est exigé par `STD-DIALOG` ; le bouton de fermeture est la cible la moins destructrice. Le balisage est figé par `OptInEvolutionTests`. |

Les menus n'utilisent pas `autofocus` (le bouton scindé le portait avant PLAN-007, exception retirée
de `.config/verify-rules.json`). Un seul moteur, dans `omni-focus.js`, sert les menus de débordement
(`OmniOverflowMenu`), contextuel (`OmniContextMenu`), du bouton scindé (`OmniSplitButton`), du profil
(`OmniProfileMenu`) et le menu contextuel d'`OmniHtmlEditor` :

- `openMenu` pose le menu dans le portail d'`OmniComponentsHost`, le garde dans la fenêtre (au-dessus du déclencheur faute de place en dessous) et donne le focus à sa première entrée, ou à la dernière quand la flèche haut l'a ouvert ;
- les touches sont routées par .NET, et `moveMenuFocus` parcourt les entrées (flèches, Début, Fin) ;
- `closeMenu` rend le focus au déclencheur sur Échap, sur le choix d'une entrée ou sur un nouvel appui du déclencheur ; Tab ferme le menu et reprend l'ordre de tabulation depuis le déclencheur ; un appui hors du menu le ferme sans reprendre le focus quand celui-ci est parti sur un autre contrôle.

Le focus d'ouverture d'`OmniDialog` dépend de sa forme (`omni-focus.js`, `activateDialog` et `activateWindow`, qui visent le premier élément focalisable du dialogue, sentinelles exclues) :

- modal et fermable (par défaut) : le focus va au bouton de fermeture, premier élément focalisable, et reste piégé dans le dialogue ;
- `Dismissible="false"` : aucun bouton de fermeture n'est rendu ; le dialogue devient `role="alertdialog"`, décrit par son contenu (`aria-describedby`), et le focus va au premier élément focalisable du contenu ou du pied, ou, faute d'élément, au dialogue lui-même (`tabindex="-1"`). Le piège de focus tient aussi contre un appui sur le voile ;
- `Modal="false"` : fenêtre non modale (`aria-modal="false"`, sans voile ni sentinelles) ; le focus va au premier élément focalisable, bouton de fermeture compris, mais il n'est pas piégé : Tab peut sortir vers la page, qui reste utilisable.

À la fermeture, le focus revient à l'élément qui l'avait avant l'ouverture (`restoreFocus`).

Dans tous ces cas le focus suit une action de l'utilisateur sur une surcouche qu'il vient d'ouvrir,
jamais un rendu de page. L'exclusion mécanique correspondante est déclarée dans
`.config/verify-rules.json` avec cette section pour raison.

## État de la vérification

Les tests automatisés actuels couvrent une partie du HTML sémantique, des attributs ARIA et des interactions simulées avec bUnit. Ils ne constituent pas une validation exhaustive en navigateur : les parcours clavier réels, le focus des superpositions, un moteur d'audit accessibilité et les technologies d'assistance restent à vérifier avant de revendiquer une conformité complète. `OmniTabs` expose les rôles `tablist`, `tab` et `tabpanel`, applique un `tabindex` roving `0/-1`, et ses flèches, `Début` et `Fin` déplacent le focus DOM vers l'onglet choisi avant de le sélectionner (`omni-focus.js`, `configureTabs`). Ce parcours n'est pas encore vérifié avec une technologie d'assistance.
