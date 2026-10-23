# PLAN-004 : seconde demande d'une application cliente du 2026-10-23 (1.9.0)

Source : seconde demande groupée de la même application cliente que [PLAN-003](PLAN-003-demandes-application-cliente-2026-10-23.md),
cinq points, décisions de son propriétaire du 2026-10-23. Livrée dans la `1.9.0` sur `release/1.9.0`, compilable
entre deux commits. Deux lots au plus en parallèle, chacun dans son worktree ; sondes de la vitrine lancées par
l'orchestrateur seul, une passe à la fois, après la fusion.

Règles communes : celles de PLAN-003 (noms suivant [public-api-conventions.md](../public-api-conventions.md),
textes du paquet dans les 24 langues, `docs/public-api.txt`, index des composants, CHANGELOG `[1.9.0]` et guide de
la famille à jour, aucun nom d'application cliente dans un fichier suivi).

## Lots

| Lot | Demande | Contenu | Critère d'acceptation | État |
|---|---|---|---|---|
| H | 1, 2 | `OmniBadge` plein : encre blanche dans tous les thèmes et tous les tons, fonds assombris juste assez pour 4,5:1 ; contour noir de 1 px du texte en option (`InkOutline`, désactivé par défaut) | `color` calculée `rgb(255, 255, 255)` pour chaque ton plein en clair et en sombre ; matrice de contraste verte sans nouvelle dérogation ; contour en huit directions quand l'option est active | à faire |
| I | 3 | `OmniAppMenu`, ligne Mode : les trois boutons restent, un clic ou Entrée et Espace sur la ligne hors des boutons passe au mode suivant (clair, sombre, système, clair) | bUnit : clic sur le libellé et sur l'icône, clavier sur la ligne, flèches du radiogroup inchangées ; sonde Scripts | à faire |
| J | 4 | `OmniCodeBlock.AnsiMode` (`OmniAnsiMode`) : couleurs dessinées, copie sans séquences | bUnit : rendu `Render`, `Hide`, `Raw` ; le bouton Copier copie le texte sans séquence ; démonstration dans la vitrine | à faire |
| K | 5 | `OmniText` : éléments `Title` et `Caption` rendus en bloc, dont les marges utilitaires s'appliquent | bUnit : élément et classe de bloc ; marge `omni-u-mb-sm` mesurée dans le navigateur sous une grille | à faire |
