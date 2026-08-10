# Plan - Bibliothèque de composants Blazor

> Canonical plan: `plans/PLAN-001-composants-blazor.md`
> Last updated: 2026-08-10

## Phase 1 - Fondation du dépôt [in progress]
- [x] Lire le contexte du chat précédent et confirmer l'abandon du fork Radzen.
- [x] Vérifier le dépôt GitHub cible et l'état des dossiers locaux.
- [ ] Initialiser la solution, la gouvernance EUPL-1.2 et les métadonnées NuGet.
- [ ] Initialiser GitFlow avec `main` et `develop`.

## Phase 2 - Inventaire Radzen [todo]
- [ ] Scanner tous les projets sous `C:\Dev` sans compter les sorties générées.
- [ ] Produire l'inventaire par projet, le catalogue global et les priorités de migration.
- [ ] Conserver un script reproductible pour rafraîchir l'inventaire.

## Phase 3 - Socle CSP et composants pilotes [todo]
- [ ] Définir le contrat CSP et la procédure clean-room.
- [ ] Implémenter des primitives représentatives sans style inline ni JavaScript dynamique.
- [ ] Ajouter des tests de rendu et des garde-fous CSP.

## Phase 4 - Publication et preuve [todo]
- [ ] Ajouter les workflows GitHub de validation et de packaging NuGet.
- [ ] Construire, tester et empaqueter la solution avec le garde-fou .NET.
- [ ] Vérifier le contenu du paquet et l'absence de dépendance Radzen.
- [ ] Raccorder `origin` au dépôt GitHub fourni et publier les branches GitFlow.
