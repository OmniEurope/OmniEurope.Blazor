# Tests désactivés

Registre des tests désactivés (règle « Running tests and failing tests ») : chaque test porte un `Skip` qui renvoie ici. Réactiver un test retire sa ligne.

| Test | Erreur | Date | Cause supposée |
| --- | --- | --- | --- |
| `AppearanceSettingsTests.Window_keeps_a_palette_and_a_font_picked_while_the_host_still_writes_the_theme` | `WaitForFailedException` : la condition n'a jamais été vérifiée, le dispatcher du rendu n'a pas tourné avant le délai (2,3 s). Vert à la première passe complète, rouge à la troisième (avec couverture). | 2026-11-08 | Intermittent : pool de threads affamé par la suite parallèle (bUnit le signale lui-même), pas un défaut du composant. |
