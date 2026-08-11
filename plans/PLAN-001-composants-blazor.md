# Plan - Bibliothèque de composants Blazor

> Last updated: 2026-08-10
> Status: superseded by `PLAN-002-remplacement-radzen.md` and the active remediation plan `PLAN-003-correction-findings-audit.md`.
> Historical scope: checkmarks below describe the state recorded on 2026-08-10 and are not current completion evidence. Current inventory, provenance, publication, and validation status is authoritative only in the successor plans and generated evidence files.

## Phase 1 - Fondation du dépôt [done]
- [x] Lire le contexte du chat précédent et confirmer l'abandon du fork Radzen.
- [x] Vérifier le dépôt GitHub cible et l'état des dossiers locaux.
- [x] Initialiser la solution, la gouvernance EUPL-1.2 et les métadonnées NuGet.
- [x] Initialiser GitFlow avec `main` et `develop`.

## Phase 2 - Inventaire Radzen [superseded]
- [x] Scanner le corpus manifesté connu à cette date sous `C:\Dev`, sans prétendre couvrir tout projet futur ou modifié après la capture.
- [x] Produire l'inventaire historique par projet et le catalogue global; les rapports courants proviennent du manifeste et des générateurs versionnés du plan 003.
- [x] Conserver un premier script de rafraîchissement, remplacé depuis par l'extraction classifiée et les preuves de provenance du plan 003.

## Phase 3 - Socle CSP et composants pilotes [done]
- [x] Définir le contrat CSP et la procédure clean-room.
- [x] Implémenter des primitives représentatives sans style inline ni JavaScript dynamique.
- [x] Ajouter des tests de rendu et des garde-fous CSP.

## Phase 4 - Publication et preuve [superseded]
- [x] Ajouter les workflows GitHub de validation et de packaging NuGet.
- [x] Construire, tester et empaqueter la solution avec le garde-fou .NET.
- [x] Vérifier le contenu du paquet et l'absence de dépendance Radzen.
- [x] Raccorder `origin` au dépôt GitHub fourni.
- [x] Publication GitFlow réalisée et suivie dans `PLAN-002-remplacement-radzen.md`; ce plan historique ne porte plus d'action ouverte.
