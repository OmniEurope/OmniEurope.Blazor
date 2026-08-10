# Capacités par famille

Le registre [component-coverage.md](component-coverage.md) relie les 110 balises observées à des cibles Razor. Cette correspondance est exhaustive pour la présence des fichiers, pas pour les comportements ni la compatibilité.

- Actions et superpositions : split/toggle button, hôte de services, dialog, notifications, tooltip et menu contextuel.
- Sélection : dropdown simple/multiple, autocomplete annulable, listes de choix, date, slider, couleur et upload.
- Navigation : panel menu, fil d'Ariane, tabs, steps et menu de profil.
- Collections : data list locale/distante, pager et arbre simple/multiple.
- DataGrid : colonnes typées, tri stable, filtres, pagination distante et sélection par clé.
- Graphiques : moteur SVG, axes, séries ligne/aire/barre/colonne/secteur, légendes et jauges.
- Temps : timeline et scheduler jour/semaine/mois en `DateTimeOffset`, avec paramètre de fuseau et défaut `TimeZoneInfo.Local`.
- HTML : édition source, aperçu sanitizé, commandes déterministes, historique et outils personnalisés.

Les noms Omni décrivent des capacités. Ils ne reproduisent pas l'API Radzen ; les paramètres observés sont conservés dans [component-contracts.md](component-contracts.md) pour guider une migration ultérieure.

## Limites fonctionnelles actuelles

- La DataGrid fournit pagination, tri, filtres, groupes et sélection, mais pas de virtualisation réelle pilotée par le viewport.
- Les séries et axes SVG existent, mais les points sont encore interprétés sur une échelle fixe de 0 à 100 : les axes ne pilotent pas la transformation géométrique des séries et les séries empilées se superposent au lieu de cumuler leurs lignes de base.
- L'éditeur HTML assainit sa valeur, affiche un aperçu et conserve un historique ; ses commandes portent encore sur tout le document et ne gèrent ni sélection, ni caret, ni composition IME, ni collage spécialisé.
- Le catalogue illustre un sous-ensemble de la surface. Le registre `110/110` ne remplace pas des scénarios comportementaux, navigateur et accessibilité pour chaque cible.
