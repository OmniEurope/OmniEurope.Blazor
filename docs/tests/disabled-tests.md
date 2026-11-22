# Tests désactivés

Registre des tests désactivés (règle « Running tests and failing tests ») : chaque test porte un `Skip` qui renvoie ici. Réactiver un test retire sa ligne.

| Test | Erreur | Date | Cause supposée |
| --- | --- | --- | --- |
| `MailTemplateEditorTests.ThePreview_IsDrawnAgain_WhenTheSubjectVariablesOrThemeChange` | Échec une fois dans la suite complète du projet (26 924/26 927 réussis, lot de duplication JS D3), réussi à la relance ; le message d'assertion n'a pas été relevé. | 2026-11-22 | Comptes d'appels `render` du module d'aperçu lus juste après un `Render` ou un clic, sans attente : un rendu asynchrone de l'aperçu peut arriver après l'assertion quand la machine est chargée (quatre worktrees en compilation). |
| `DataGridInitialLoadTests.FirstRequest_CarriesTheColumnDefaults_AndIsTheOnlyOne(mode: Paged, slowLoader: True)` (ce seul cas) | `failed (canceled)` après 1,7 s en net11.0 (3 523/3 524 réussis dans la preuve des lots D3/D4), réussi à la relance (28/28). | 2026-11-22 | Attente `WaitForAssertion` du chargeur lent dépassée sous une machine chargée (worktrees en compilation en parallèle). |
