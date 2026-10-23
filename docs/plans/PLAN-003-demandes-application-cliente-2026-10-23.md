# PLAN-003 : demandes d'une application cliente du 2026-10-23 (1.9.0)

Source : recette d'une application cliente du 2026-10-23, dix demandes adressées à OE. Livrées dans la `1.9.0`
sur `release/1.9.0`, qui doit rester compilable entre deux commits (l'application compile OE depuis ses
sources). Deux lots au plus en parallèle, chacun dans son worktree, relus et intégrés par l'orchestrateur ;
les sondes de la vitrine sont lancées par l'orchestrateur seul, une passe à la fois, après la fusion.

Règles communes : noms suivant [public-api-conventions.md](../public-api-conventions.md) ; textes du paquet
dans les 24 langues ; `docs/public-api.txt`, l'index des composants, le CHANGELOG `[1.9.0]` et le guide de la
famille à jour ; aucun nom d'application cliente dans un fichier suivi.

## Lots

| Vague | Lot | Demandes | Contenu | Critère d'acceptation | État |
|---|---|---|---|---|---|
| 1 | A | 1 | `OmniBadge` plein : une encre par mode (noire en clair, blanche en sombre), jetons de badge propres, `success-deep` et `info-deep` | Matrice de contraste étendue aux badges pleins : encre identique par mode, 4,5:1 au moins, exceptions listées | à faire |
| 1 | B | 5, 6 | Grille : en `Wrap`, plancher de largeur au mot le plus long du titre même avec `Width` ; `EditTrigger` (`Action`, `Cell`) | Titre jamais coupé avec `Width` déclarée (sonde) ; clic simple sur une cellule éditable ouvre la ligne, pas sur un bouton de la cellule (bUnit et sonde) | à faire |
| 2 | C | 2 | `OmniPageHeader.PinnedActionsContent`, jamais replié ; position du menu ⋮ | À 375 et 320 px, action épinglée visible à droite sans clic, menu ⋮ sur la ligne du titre (sonde) | à faire |
| 2 | D | 7 | ANSI : `OmniAnsi` (`Parse`, `Strip`), `OmniLogViewer.AnsiMode`, `OmniAnsiText`, jetons `--omni-ansi-0` à `15` | Ligne d'exemple rendue sans code, couleurs et gras ; `Raw` montre les codes ; contraste 4,5:1 dans les deux modes (test) | à faire |
| 3 | E | 3, 4 | `OmniAppMenu` : ligne Mode en trois boutons radiogroup ; `OmniAppearanceWindow` sans débordement sur les 15 thèmes | Un clic sur « sombre » passe en sombre ; flèches ; corps de la fenêtre sans défilement horizontal sur thèmes, modes, tailles de texte, densités, 1280 et 375 px (sonde) | à faire |
| 3 | F | 9, 10 | `OmniCopyButton` public ; formateurs publics de taille et de durée, utilisés par `OmniUpload` | Texte exact copié, « Copié » puis retour, refus annoncé sans exception ; `Duration` 6260 s = « 1h44min », 2532 s = « 42min », 45 s = « 45s » | à faire |
| 4 | G | 8 | `OmniFactTile` et `OmniFactTileRow` dans `OmniTileGrid` | Trois tuiles de longueurs différentes : même hauteur, icônes, libellés et valeurs alignés ; rien ne déborde à 375 px ni au texte 10 (sonde) | à faire |
