# Analyseurs de conventions

Le projet `eng/OmniEurope.Analyzers` fournit les diagnostics compilés avec la RCL. `.editorconfig` porte leur sévérité et `tests/OmniEurope.Blazor.Tests/ConventionGuardTests.cs` protège les conventions qui nécessitent une vue dépôt.

| Diagnostic | Contrat | Applicabilité à la RCL |
| --- | --- | --- |
| `GEN001` | pas d'injection directe de contexte de données | actif, aucun contexte attendu |
| `GEN002` | accès aux données via dépôt | actif en prévention |
| `GEN003` | horloge injectée via `TimeProvider` | actif |
| `GEN004` | aucun bloc `@code` dans les fichiers Razor livrés | actif et promu à erreur |
| `GEN005` | ordre correct des opérations de requête | actif en prévention |
| `GEN006` | matérialisation potentiellement non bornée | avertissement informatif |
| `GEN007` | autorisation explicite des contrôleurs | actif en prévention, aucun contrôleur attendu |
| `GEN008` | types partiels limités aux raisons autorisées | actif et promu à erreur; les code-behind Razor sont reconnus |

La suite dédiée `eng/OmniEurope.Analyzers.Tests` exécute chaque diagnostic contre un cas positif et un cas négatif au moyen d'une compilation Roslyn réelle. Les règles sémantiques lient les symboles BCL, LINQ, EF et ASP.NET Core au lieu de se fier à leur texte ou à un nom homonyme. `GEN008` n'accepte comme preuve de génération que les attributs de générateurs connus; une méthode `partial` utilisateur sans corps ne constitue pas une exemption.

Les règles non applicables restent des garde-fous préventifs; elles ne justifient ni couche applicative, ni dépôt, ni contrôleur fictif.
