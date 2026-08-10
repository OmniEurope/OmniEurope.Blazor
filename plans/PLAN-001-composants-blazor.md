# Plan - Bibliothèque de composants Blazor

> Last updated: 2026-08-10

## Phase 1 - Fondation du dépôt [done]
- [x] Lire le contexte du chat précédent et confirmer l'abandon du fork Radzen.
- [x] Vérifier le dépôt GitHub cible et l'état des dossiers locaux.
- [x] Initialiser la solution, la gouvernance EUPL-1.2 et les métadonnées NuGet.
- [x] Initialiser GitFlow avec `main` et `develop`.

## Phase 2 - Inventaire Radzen [done]
- [x] Scanner tous les projets sous `C:\Dev` sans compter les sorties générées.
- [x] Produire l'inventaire par projet, le catalogue global et les priorités de migration.
- [x] Conserver un script reproductible pour rafraîchir l'inventaire.

## Phase 3 - Socle CSP et composants pilotes [done]
- [x] Définir le contrat CSP et la procédure clean-room.
- [x] Implémenter des primitives représentatives sans style inline ni JavaScript dynamique.
- [x] Ajouter des tests de rendu et des garde-fous CSP.

## Phase 4 - Publication et preuve [in progress]
- [x] Ajouter les workflows GitHub de validation et de packaging NuGet.
- [x] Construire, tester et empaqueter la solution avec le garde-fou .NET.
- [x] Vérifier le contenu du paquet et l'absence de dépendance Radzen.
- [x] Raccorder `origin` au dépôt GitHub fourni.
- [ ] Publier les branches GitFlow après autorisation explicite du contenu public.
