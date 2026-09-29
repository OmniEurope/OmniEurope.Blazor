# Analyseurs de conventions

Le projet `eng/OmniEurope.Analyzers` fournit les diagnostics `GEN*` compilés avec la RCL ; `src/OmniEurope.Blazor.Analyzers` fournit `OE0001`, livré dans le paquet (voir plus bas). `.editorconfig` porte leur sévérité et `tests/OmniEurope.Blazor.Tests/ConventionGuardTests.cs` protège les conventions qui nécessitent une vue dépôt.

| Diagnostic | Contrat | Sévérité (`.editorconfig`) | Applicabilité à la RCL |
| --- | --- | --- | --- |
| `GEN001` | pas d'injection directe de contexte de données | erreur | actif, aucun contexte attendu |
| `GEN002` | accès aux données via dépôt | erreur | actif en prévention |
| `GEN003` | horloge injectée via `TimeProvider` | erreur | actif |
| `GEN004` | aucun bloc `@code` dans les fichiers Razor livrés | erreur (`*.razor`, `*.cshtml`) | actif |
| `GEN005` | ordre correct des opérations de requête | erreur | actif en prévention |
| `GEN006` | matérialisation potentiellement non bornée | avertissement, donc erreur sous `TreatWarningsAsErrors` | actif |
| `GEN007` | autorisation explicite des contrôleurs | erreur | actif en prévention, aucun contrôleur attendu |
| `GEN008` | types partiels limités aux raisons autorisées | erreur | actif ; les code-behind Razor sont reconnus |

Les descripteurs déclarent une sévérité par défaut plus basse (`GEN001` erreur, `GEN006` information, les autres avertissement) ; `.editorconfig` les relève comme ci-dessus pour ce dépôt, et les dossiers `Migrations` éteignent `GEN003` et `GEN008`.

La suite dédiée `eng/OmniEurope.Analyzers.Tests` exécute chaque diagnostic contre un cas positif et un cas négatif au moyen d'une compilation Roslyn réelle. Les règles sémantiques lient les symboles BCL, LINQ, EF et ASP.NET Core au lieu de se fier à leur texte ou à un nom homonyme. `GEN008` n'accepte comme preuve de génération que les attributs de générateurs connus; une méthode `partial` utilisateur sans corps ne constitue pas une exemption.

Les règles non applicables restent des garde-fous préventifs; elles ne justifient ni couche applicative, ni dépôt, ni contrôleur fictif.

## Analyseur livré dans le paquet : `OE0001`

Les règles `GEN*` ne servent qu'à la construction de ce dépôt. `src/OmniEurope.Blazor.Analyzers` est au contraire livré aux consommateurs : le paquet NuGet porte `analyzers/dotnet/cs/OmniEurope.Blazor.Analyzers.dll`, seule entrée admise sous `analyzers/` par `eng/Test-Package.ps1` (le compilateur qui charge l'analyseur fournit Roslyn, le paquet n'en embarque aucune copie). Il référence `Microsoft.CodeAnalysis.CSharp` `5.0.0` comme l'autre projet (STD-SDKPIN).

| Diagnostic | Sévérité | Contrat |
| --- | --- | --- |
| `OE0001` | erreur | un composant OmniEurope.Blazor ne reçoit aucun attribut en PascalCase qu'il n'a pas pour paramètre : `OmniBadge has no parameter 'IconName'` |

Les composants capturent les attributs qu'ils ne déclarent pas ; sans cette règle, un paramètre retiré ou mal orthographié compilait et devenait un attribut HTML. L'analyseur lit le code que le générateur Razor émet (bloc `OpenComponent<T>` ... `CloseComponent`) : un paramètre reconnu y est écrit `AddComponentParameter(n, nameof(T.Nom), valeur)`, un attribut inconnu garde son nom en littéral, `AddComponentParameter(n, "Nom", valeur)`. Un littéral qui commence par une majuscule ASCII, sur un composant de l'assemblage `OmniEurope.Blazor` qui n'a ni lui ni ses types de base de propriété publique `[Parameter]` de ce nom (sans tenir compte de la casse, comme Blazor), est une erreur signalée à sa ligne dans le fichier `.razor`. Les attributs en minuscules (`class`, `id`, `aria-*`, `data-*`) et les `@attributes` (`AddMultipleAttributes`) ne sont jamais signalés. Le générateur laisse ce littéral dans une zone `#line hidden` : l'emplacement est retrouvé dans le `.razor` lui-même, à partir de la dernière position que le générateur y a reliée.

Au rendu, `CspAttributeGuard` sert de filet : un attribut capturé en PascalCase lève `InvalidOperationException` (`OmniBadge has no parameter 'IconName'.`), y compris quand il n'existe qu'à l'exécution.

Consommation :

- par le paquet NuGet, rien à faire : l'analyseur accompagne la bibliothèque ;
- par `ProjectReference` vers la RCL, l'analyseur empaqueté n'arrive pas. Le projet consommateur ajoute une ligne à côté de sa référence :

  ```xml
  <ProjectReference Include="chemin/vers/src/OmniEurope.Blazor.Analyzers/OmniEurope.Blazor.Analyzers.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  ```

Dans ce dépôt, `Directory.Build.props` pose cette ligne sur la RCL, le showcase, les échantillons et les tests. La suite `tests/OmniEurope.Blazor.Analyzers.Tests` exécute la règle sur la sortie réelle du générateur Razor (fichiers `Fixtures/*.razor.g.cs.txt`, chemins neutralisés), y compris les composants génériques à type inféré que le générateur écrit dans des méthodes `TypeInference`.
