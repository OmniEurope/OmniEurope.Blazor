# PLAN-010 - Composants de document et de dessin

> Statut : ouvert le 2026-11-04. Demande d'une application cliente (module de modèles Word). Version : `1.12.0`, décidée par le propriétaire le 2026-11-04 ; la publication reste à sa demande.

## Contrainte

`OmniEurope.Blazor` ne référence aucune bibliothèque de documents. Les trois composants travaillent sur un modèle neutre défini ici ; l'application cliente convertit son document vers ce modèle et applique en retour le jeu de changements.

## Lots

| Lot | Contenu | Critère d'acceptation | Statut |
| --- | --- | --- | --- |
| 1 | `OmniInkCanvas` : traits (`OmniInkStroke`, `OmniInkPoint`, coordonnées 0..1, pression), couleur, épaisseur, Annuler, Effacer, export PNG transparent (octets, data URL) | Dessin souris et tactile simulé vu dans la vitrine, clair et sombre ; Ctrl+Z et Effacer au clavier ; PNG exporté transparent à la taille demandée | Fait le 2026-11-04 : sonde `ink` verte dans Chromium (souris, stylet, doigt, clavier, sombre, export PNG) |
| 2 | `OmniDocumentViewer` : pages image ou HTML re-sanitisé, champs `data-oe-field` surlignés sur les pages HTML seulement, focusables, infobulle du libellé ; zoom, ajustement à la largeur, page n/N | Pages image et HTML vues dans la vitrine ; infobulle au survol et au focus ; un script injecté dans le HTML est retiré | Fait le 2026-11-04 : vitrine vérifiée (9 champs focalisables, infobulle au focus clavier, Entrée remonte le tag, aucun script ni `onclick` restant) ; survol non mesuré (panneau du navigateur masqué) |
| 3 | `OmniDocumentEditor` : blocs, runs (texte, `StyleKey` opaque), champs (texte, date, liste avec `AllowFreeText` pour une saisie libre hors des éléments, nombre), dessin par bloc via `OmniInkCanvas` ; `OmniDocumentChangeSet` (runs, champs, dessins avec traits et PNG) | Texte d'un run modifié, champ date et liste réglés, dessin inséré : le jeu de changements reçu par l'hôte de la vitrine les contient tous | À faire |
| 4 | Intégration : CHANGELOG, index des composants, API publique, suite complète et gates, recette navigateur | CI locale complète verte, recette des trois composants dans la vitrine | À faire |

## Hors périmètre

Barre de mise en forme, tableaux et imbrication de champs, pages PDF.
