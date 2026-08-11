# Findings d'audit 360 - OmniEurope.Analyzers

> Audit de remédiation : 2026-08-11  
> Mode : Full, 3 fichiers lus intégralement  
> Preuves transversales : build Release global sans avertissement, 181/181 tests réussis, mais aucune classe de l'analyseur dans Cobertura et aucun test Roslyn positif/négatif. La complexité et le CRAP sont non fiables faute d'outil autorisé; le SAST et le scan de secrets sont également non fiables selon `metrics/PREFLIGHT.md` et `metrics/SECURITY_SCAN.md`.
>
> Total propre à ce module : **4 findings** - Critique 0, Élevé 1, Moyen 3, Faible 0. Les constats globaux `KIT-004`, `KIT-005`, `KIT-011` et `DEP-004` sont référencés mais non dupliqués.

<a id="eng/OmniEurope.Analyzers/OmniEurope.Analyzers.csproj"></a>
## `eng/OmniEurope.Analyzers/OmniEurope.Analyzers.csproj`

RAS - le ciblage `netstandard2.0`, l'isolation `PrivateAssets="all"`, le verrou NuGet et l'usage interne de `RS2008` sont cohérents. L'obsolescence de Roslyn est déjà comptée globalement sous `KIT-011` et `DEP-004`.

<a id="eng/OmniEurope.Analyzers/OmniEuropeConventionAnalyzer.cs"></a>
## `eng/OmniEurope.Analyzers/OmniEuropeConventionAnalyzer.cs`

[Élevé] [Sécurité] `GEN007` considère qu'un contrôleur est autorisé dès qu'un attribut de la hiérarchie porte le simple nom `AuthorizeAttribute` ou `AllowAnonymousAttribute`, sans vérifier son symbole ni son assembly. Un attribut local sans rapport avec `Microsoft.AspNetCore.Authorization` peut donc neutraliser la gate tout en laissant l'endpoint sans politique d'autorisation réelle; le build resterait vert malgré la promesse de sécurité explicite - lignes 81-89 - source : lecture intégrale, analyse sémantique best-effort non outillée et contrat `docs/analyzers.md` - recommandation : Codex résoudra les symboles framework connus depuis la compilation, comparera avec `SymbolEqualityComparer.Default` et ajoutera des fixtures adversariales avec des attributs homonymes qui doivent rester rejetés.

[Moyen] [Fiabilité] `GEN003` ne reconnaît que l'expression syntaxique composée de l'identifiant nu `DateTime` suivi de `Now` ou `UtcNow`, puis exempte toute initialisation de propriété. `System.DateTime.UtcNow`, un alias C# ou une propriété de service initialisée avec l'horloge ambiante contournent ainsi la règle pourtant promue à erreur; inversement, un type applicatif homonyme `DateTime` peut produire un faux positif - lignes 98-105, `.editorconfig:41` - source : lecture intégrale et contrat `docs/analyzers.md` - recommandation : Codex liera le membre au symbole BCL `System.DateTime`, limitera l'exception aux modèles réellement autorisés et prouvera les formes qualifiées, aliasées, positives et négatives par tests Roslyn.

[Moyen] [Fiabilité] `GEN004`, `GEN005` et `GEN006` décident sur du texte ou des noms de méthodes sans identité sémantique : `StartsWith("@code")` peut confondre une expression telle que `@codependent` ou du contenu commenté avec un bloc Razor; toute API ayant une chaîne `.OrderBy().Include()` peut déclencher `GEN005`; tout `.ToList()` dans un chemin contenant `Repository` peut déclencher `GEN006`, même hors EF. Comme ces diagnostics deviennent bloquants via `.editorconfig` et `TreatWarningsAsErrors`, du code conforme peut être refusé par la compilation - lignes 47-65 et 114-134, `.editorconfig:23,42-43`, `Directory.Build.props:7` - source : lecture intégrale - recommandation : Codex renforcera la frontière lexicale Razor, utilisera les symboles EF/LINQ et une portée de type explicite, puis ajoutera des fixtures homonymes qui doivent rester sans diagnostic.

[Moyen] [Authenticité] `GEN008` exempte une classe entière dès qu'elle contient une méthode `partial` sans corps, sans vérifier qu'un générateur de source la prend réellement en charge. Une simple déclaration `partial void Hook();` suffit donc à rendre verte une classe partielle ordinaire et à contourner `STD-PARTIAL`; cette exemption est plus large que la promesse « source-generator pairing » - lignes 137-145 - source : lecture intégrale et `C:\Dev\_Generic\docs\roslyn-analyzers.md` - recommandation : Codex exigera une preuve sémantique de génération prise en charge ou une exception explicitement déclarée, puis ajoutera une fixture négative avec une méthode partielle utilisateur. L'omission distincte des propriétés partielles générées reste exclusivement comptée sous `KIT-004`.

Les autres divergences au référentiel (`GEN002`, `GEN006` et l'exception de propriété de `GEN008`) sont déjà comptées sous `KIT-004`. L'absence complète de tests positifs/négatifs GEN001-GEN008 est déjà comptée sous `KIT-005` et n'est pas dupliquée ici.

<a id="eng/OmniEurope.Analyzers/packages.lock.json"></a>
## `eng/OmniEurope.Analyzers/packages.lock.json`

RAS - le verrou est cohérent et chaque entrée possède une version résolue et un `contentHash`; la fraîcheur de Roslyn et de ses transitifs reste centralisée sous `DEP-004` et `KIT-011`.

## Proportionnalité et sur-ingénierie

`PROPORTIONNALITE: ADAPTEE` - réunir huit diagnostics courts dans un unique analyseur scellé, avec exécution concurrente et sans dépendance runtime vers la RCL, reste la solution la plus simple pour un outil interne à cycle de livraison unique. Les séparer en huit packages ou introduire une infrastructure générique de règles ajouterait publication, configuration et indirection sans second consommateur; aucune notification de sur-ingénierie n'est retenue.
