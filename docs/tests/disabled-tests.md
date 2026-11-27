# Tests désactivés

Registre des tests désactivés (règle « Running tests and failing tests ») : chaque test porte un `Skip` qui renvoie ici. Réactiver un test retire sa ligne.

| Test | Erreur | Date | Cause supposée |
| --- | --- | --- | --- |
| `MailTemplateEditorTests.ThePreview_IsDrawnAgain_WhenTheSubjectVariablesOrThemeChange` | Échec après 1,26 s en net11.0 dans la suite complète de develop (merge des paquets à jour, 27 162/27 165), message non capturé (sortie filtrée sur les lignes `failed`) ; réussi au rejeu ciblé et à la passe 3 complète (27 165/27 165). | 2026-11-27 | Probablement une attente `WaitForAssertion` (compte des rendus de l'aperçu, rendu asynchrone après le rendu Blazor) dépassée sous une machine saturée par d'autres sessions. |
