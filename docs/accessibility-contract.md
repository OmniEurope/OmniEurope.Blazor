# Contrat d'accessibilité

## Règles communes

- Utiliser l'élément HTML natif lorsqu'il existe (`button`, `input`, `select`, `nav`, `table`, `fieldset`).
- Conserver un ordre de focus identique à l'ordre du document ; aucun `tabindex` positif.
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

## État de la vérification

Les tests automatisés actuels couvrent une partie du HTML sémantique, des attributs ARIA et des interactions simulées avec bUnit. Ils ne constituent pas une validation exhaustive en navigateur : les parcours clavier réels, le focus des superpositions, un moteur d'audit accessibilité et les technologies d'assistance restent à vérifier avant de revendiquer une conformité complète. `OmniTabs` change actuellement la sélection avec les flèches, mais le déplacement réel du focus et le motif `tablist`/roving `tabindex` ne sont pas encore implémentés.
