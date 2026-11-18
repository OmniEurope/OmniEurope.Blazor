# PLAN-001 - Recette

Fichier réservé de la recette : tout ce qui a été demandé et n'est pas terminé, ligne par ligne, avec son état (`[ ]` à faire, `[~]` en cours, `[x]` fait avec sa preuve, `[!]` bloqué avec la raison).

## Recette

| N° | État | Demande | Critère d'acceptation |
| --- | --- | --- | --- |
| 1 | `[ ]` plus tard | Demande d'une application cliente du 2026-11-17 : appliquer l'apparence avant le premier affichage. OE pose lui-même, avant le premier rendu Blazor, les réglages d'apparence de l'utilisateur (thème, mode, densité, taille du texte et des contrôles, autres réglages d'`OmniAppearanceWindow`) sur `<html>`, depuis ce que `OmniPreferences` sait déjà, pour que ni l'écran de démarrage ni la première page ne s'affichent dans le mauvais thème ou à la mauvaise échelle. Aujourd'hui l'application recopie 9 réglages dans `localStorage` pour éviter ce scintillement, un écart à la règle des préférences en base seulement (OEAPP-07) ; elle supprimera sa copie quand OE fera cet amorçage. À traiter plus tard, pas maintenant. | Une application OE sans script d'amorçage local, avec un thème sombre et une échelle non par défaut enregistrés, affiche son écran de démarrage et sa première page directement dans ce thème et à cette échelle (aucun rendu intermédiaire dans le thème par défaut), sans rien stocker hors `OmniPreferences` ; un test le fige. |
| 2 | `[~]` en cours | Demande d'une application cliente du 2026-11-18 : tuile vidéo d'un flux `MediaStream` avec calque de dessin partagé, et scène d'un flux principal et de vignettes. Détail et contrat d'API dans [PLAN-002](PLAN-002-composants-media-partage-ecran.md). | Composants documentés et démontrés dans la galerie avec un flux de démonstration ; dessin visible sur deux instances qui s'échangent les traits ; export PNG vidéo + traits ; aucun liseré ; vérifiés à 1440 et 375 px dans un navigateur. |
