# PLAN-004 : seconde demande d'une application cliente du 2026-10-23 (1.9.0)

Source : seconde demande groupée de la même application cliente que [PLAN-003](PLAN-003-demandes-application-cliente-2026-10-23.md),
six points (le sixième arrivé après les cinq premiers), décisions de son propriétaire du 2026-10-23. Livrée dans la `1.9.0` sur `release/1.9.0`, compilable
entre deux commits. Deux lots au plus en parallèle, chacun dans son worktree ; sondes de la vitrine lancées par
l'orchestrateur seul, une passe à la fois, après la fusion.

Règles communes : celles de PLAN-003 (noms suivant [public-api-conventions.md](../public-api-conventions.md),
textes du paquet dans les 24 langues, `docs/public-api.txt`, index des composants, CHANGELOG `[1.9.0]` et guide de
la famille à jour, aucun nom d'application cliente dans un fichier suivi).

## Lots

| Lot | Demande | Contenu | Critère d'acceptation | État |
|---|---|---|---|---|
| H | 1, 2 | `OmniBadge` plein : encre blanche par défaut dans tous les thèmes et tous les tons, fonds assombris juste assez pour 4,5:1 ; option `Ink` (`OmniBadgeInk` : `White`, `WhiteOutlined` avec contour noir de 1 px, `Black` sans contour sur un fond du ton éclairci pour 4,5:1), correction du demandeur qui remplace `InkOutline` | sans paramètre, `color` calculée `rgb(255, 255, 255)` pour chaque ton plein en clair et en sombre ; matrice de contraste verte sans dérogation pour l'encre blanche et pour l'encre noire ; contour en huit directions avec `WhiteOutlined`, lettres noires sans ombre avec `Black` | fait (b03234a7, 0bba549f, correction `Ink` à ce commit), matrice verte, sonde à passer |
| I | 3 | `OmniAppMenu`, ligne Mode : les trois boutons restent, un clic ou Entrée et Espace sur la ligne hors des boutons passe au mode suivant (clair, sombre, système, clair) | bUnit : clic sur le libellé et sur l'icône, clavier sur la ligne, flèches du radiogroup inchangées ; sonde Scripts | fait (16e48d52), clavier à prouver par la sonde |
| J | 4 | `OmniCodeBlock.AnsiMode` (`OmniAnsiMode`) : couleurs dessinées, copie sans séquences | bUnit : rendu `Render`, `Hide`, `Raw` ; le bouton Copier copie le texte sans séquence ; démonstration dans la vitrine | fait (a7d42e33), sonde à passer |
| K | 5 | `OmniText` : éléments `Title` et `Caption` rendus en bloc, dont les marges utilitaires s'appliquent | bUnit : élément et classe de bloc ; marge `omni-u-mb-sm` mesurée dans le navigateur sous une grille | fait (0bba549f), mesure navigateur à faire |
| L | 6 | `OmniFactTile` : dans une tuile étroite (requête de conteneur sur la tuile), la ligne passe en deux étages (libellé au-dessus, valeur dessous) au lieu de réserver une colonne de libellé qui coupe tôt ; l'ellipse avec infobulle reste le dernier recours | À 4 colonnes dans une page de 900 px, un libellé de 16 caractères et une valeur de 20 caractères sans ellipse (sonde) ; tuiles larges inchangées (colonnes alignées) | fait (4982a3f6), sonde Scripts verte avant et après (rouge sur le CSS d'origine) |
