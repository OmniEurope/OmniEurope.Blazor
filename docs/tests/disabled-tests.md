# Tests désactivés

Registre des tests désactivés (règle « Running tests and failing tests ») : chaque test porte un `Skip` qui renvoie ici. Réactiver un test retire sa ligne.

| Test | Erreur | Date | Cause supposée |
| --- | --- | --- | --- |
| `DataGridInitialLoadTests.FirstRequest_CarriesTheColumnDefaults_AndIsTheOnlyOne(mode: Paged, slowLoader: True)` (ce seul cas) | `failed (canceled)` après 1,7 s en net11.0 (3 523/3 524 réussis dans la preuve des lots D3/D4), réussi à la relance (28/28). | 2026-11-22 | Attente `WaitForAssertion` du chargeur lent dépassée sous une machine chargée (worktrees en compilation en parallèle). |
| `UploadEdgeTests.Disposed_BeforeTheCallbackReturns_NeitherCompletesNorAttaches` | `failed (canceled)` après 1,6 s en net10.0 dans la suite complète de develop 75acec12 (27 057/27 062), réussi à la passe précédente et à la relance (2/2). | 2026-11-24 | La sélection lancée à part (`Task.Run` de `InputFile.UploadFiles`) peut se terminer annulée quand le composant est disposé pendant son rappel, et `WaitAsync` remonte alors l'annulation ; machine saturée (plusieurs sessions en compilation). |
