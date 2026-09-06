# Conformité au kit `_Generic`

> Passe 1b de l'audit 360, exécutée le 2026-08-11. Revue globale en lecture seule du projet et de Git. Les règles granulaires par fichier restent réservées à la passe 3.

## Verdict global

**NON CONFORME au kit `_Generic` dans son état actuel.** Le dépôt est une Razor Class Library autonome destinée à remplacer Radzen, alors que le kit ne définit que les archétypes Web, Blazor Server, MAUI et MAUI avec WiX, et que son contrat UI canonique impose Radzen. Cette divergence peut être légitime pour le produit, mais elle n'est ni modélisée comme un archétype du kit, ni formalisée dans un registre local de règles. Les protections de compilation et de test attendues par le kit sont également absentes.

Compteurs de findings actionnables: **8** au total, soit **0 Critique**, **4 Élevé**, **4 Moyen**, **0 Faible**.

## Références et méthode

- Sources projet inspectées: arborescence complète, état Git, solution et chaque `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `CLAUDE.md`, `README.md`, `CHANGELOG.md`, documentation sous `docs/`, tests nommés et CI.
- Sources kit inspectées sous `C:\Dev\_Generic\`: `CLAUDE.md`, `.editorconfig`, documentation normative, registre `docs/code-rules.md`, catalogue `docs/roslyn-analyzers.md`, sources et tests de `analyzers/`, modèles d'architecture et de documentation.
- Le registre effectif est le registre canonique du kit uniquement: `.claude/code-rules.md` n'existe pas dans le projet.
- Le préflight de cette exécution établit un build Release propre, avec 0 avertissement et 0 erreur, et 57 tests réussis. Ce résultat ne prouve pas les règles `GENxxx`, car aucun analyseur `GEN001-GEN008` n'est chargé par la compilation.
- Aucun outil Python n'a été sondé ou exécuté.

## Type de projet détecté

| Élément | Observation |
|---|---|
| Type principal | Bibliothèque de composants Blazor empaquetée en NuGet, `Microsoft.NET.Sdk.Razor`, cible `net10.0` |
| Projets | Une RCL de production, un projet bUnit/xUnit et cinq hôtes de démonstration ou de smoke test, dont quatre inscrits dans la solution et un hôte Hybrid construit séparément en CI |
| Organisation | `src/`, `tests/`, `samples/`, `docs/`, `eng/`; 120 fichiers directement sous `src/OmniEurope.Blazor/Components`, sans sous-dossier de famille ou de slice |
| Intention déclarée | Remplacer progressivement les usages Radzen par une implémentation clean-room indépendante (`README.md:3`, `docs/architecture.md:15`) |
| Correspondance kit | Aucune correspondance exacte. Ce n'est ni une application `.Back/.Front/.Shared`, ni une application MAUI, ni une application MAUI avec WiX |

Les absences de `deploy/`, `Platforms/`, `Installer/`, d'une couche de données et d'un découpage `.Back/.Front/.Shared` ne sont donc pas comptées séparément comme des défauts d'application Web ou MAUI. Elles participent au finding de type non pris en charge ci-dessous.

## Structure

Points conformes ou adaptés:

- solution .NET 10 avec séparation `src/`, `tests/` et `samples/`;
- projet de tests réel et exécuté par la CI;
- architecture mono-RCL explicitement motivée dans `docs/architecture.md:3`;
- hôtes de smoke test Server, WebAssembly, Interactive Auto et Hybrid documentés et intégrés à la CI;
- Central Package Management actif, versions centralisées et fichiers de verrouillage présents.

Écarts globaux:

- aucun archétype RCL n'existe dans le kit, donc les attentes structurelles et UI héritées sont ambiguës ou contradictoires;
- les 120 fichiers de `Components/` sont à plat, alors que le kit impose des slices ou modules sous `Components/{Module}/`;
- l'unique projet de tests ne reflète pas des familles ou modules dans son arborescence et ne contient aucun dossier `Guards/` conforme au kit.

## Documentation

| Attente du kit | État du projet | Verdict |
|---|---|---|
| `CLAUDE.md` avec vue d'ensemble, stack, structure, règles et table de documentation | Le fichier ne contient que le titre et `## Session resume` (`CLAUDE.md:1-5`) | Écart |
| Section `## Session resume` | Présente | Conforme |
| `README.md` | Présent et détaillé | Conforme |
| `CHANGELOG.md` | Présent | Conforme |
| Architecture | `docs/architecture.md` présent, mais très bref et sans carte des dépendances ou contrat de gouvernance | Partiel |
| `tech-stack.md`, `code-patterns.md`, `coding-standards.md`, `testing.md` | Absents | Écart |
| Documentation de déploiement | Absente; l'absence peut être adaptée à une bibliothèque, mais l'adaptation n'est pas documentée | Partiel |
| `agent-principles.md`, `roslyn-analyzers.md`, `project-architecture.md` | Absents | Écart |
| `docs/claudes/claude-security.md`, `claude-deployment.md`, `claude-ui-patterns.md`, `claude-modules.md` | Dossier et fichiers absents | Écart |
| Documentation par module | Plusieurs documents métier par famille de composants existent, mais sans index canonique dans `CLAUDE.md` | Partiel positif |
| ADR | Aucun `docs/adr/`; les décisions structurantes sont dispersées dans `README.md`, `docs/architecture.md` et les documents clean-room | Écart inclus dans le finding documentaire |

Le dépôt possède **26 fichiers sous `docs/`**, dont plusieurs contrats spécialisés utiles. Le finding porte sur leur absence de carte canonique et sur les documents de gouvernance manquants, pas sur une absence générale de documentation.

## Conventions et `.editorconfig`

Le projet conserve les bases `utf-8`, espaces, indentation 4 pour C#/Razor et indentation 2 pour les formats de configuration. Il diverge toutefois du modèle `_Generic` sur les points globaux suivants:

- `end_of_line = crlf` au lieu de `lf` (`.editorconfig:5`);
- aucune règle générale `indent_style` ou `indent_size` pour CSS, JavaScript, HTML et PowerShell;
- les fichiers Markdown héritent de `trim_trailing_whitespace = true`, alors que le kit le désactive;
- absence de toutes les conventions C# du kit: ordre des `using`, accolades, namespace file-scoped en warning, règles `var`, indentation des `case` et constructeurs primaires;
- absence de toutes les entrées `dotnet_diagnostic.GEN001` à `GEN008`.

`TreatWarningsAsErrors=true` est correctement activé dans `Directory.Build.props:7`, mais il ne peut faire échouer le build que pour des diagnostics réellement chargés.

## Câblage `GEN001-GEN008`

Constats globaux vérifiés sur tous les projets de production, de test et d'échantillon:

- **0** référence de projet avec `OutputItemType="Analyzer"`;
- **0** inclusion `<AdditionalFiles Include="**\*.razor" />` pour `GEN004`;
- **0** configuration de sévérité `dotnet_diagnostic.GENxxx`;
- aucun projet d'analyseurs dans la solution;
- le build propre du préflight n'exerce donc aucun diagnostic `GENxxx`.

| ID | Applicabilité au type détecté | Câblage | Résultat |
|---|---|---|---|
| `GEN001` | Non applicable actuellement: aucun EF Core ni `AppDbContext` | Absent | N/A technologique, non câblé |
| `GEN002` | Non applicable actuellement: aucun `IUnitOfWork` ni accès de dépôt | Absent | N/A technologique, non câblé |
| `GEN003` | Non applicable selon la famille kit `EF + Blazor`; à requalifier dans l'overlay RCL | Absent | N/A actuel, non câblé |
| `GEN004` | Applicable aux composants Razor de production | Absent, y compris `AdditionalFiles` | Non appliqué |
| `GEN005` | Non applicable actuellement: aucun EF Core | Absent | N/A technologique, non câblé |
| `GEN006` | Non applicable actuellement: aucun repository EF | Absent | N/A technologique, non câblé |
| `GEN007` | Non applicable actuellement: aucun contrôleur API dans la production | Absent | N/A technologique, non câblé |
| `GEN008` | Applicable au code C# de production | Absent | Non appliqué |

La recherche globale confirme que 105 des 106 fichiers Razor de production contiennent actuellement un bloc `@code`. Ce chiffre n'est pas converti ici en findings par fichier, car cette vérification appartient à la passe 3. Il prouve en revanche que le build vert ne peut pas servir de preuve d'application de `GEN004`.

## Registre des code-rules et tests de règles

Le registre canonique `C:\Dev\_Generic\docs\code-rules.md` est accessible et contient 13 règles `STD-*`. Aucun overlay `.claude/code-rules.md` n'existe.

| Couche déclarée | Vérification globale | Résultat |
|---|---|---|
| `analyzer:GEN008` pour `STD-PARTIAL` | Aucun analyseur référencé | Non appliqué |
| `test:NavActiveStateGuardTests` pour `STD-NAV` | Test absent; les deux routes détectées appartiennent aux hôtes d'échantillon et aucun menu Radzen n'existe | Non câblé, applicabilité à formaliser |
| `test:ButtonConventionGuardTests` pour `STD-BTN` | Test absent | Non câblé |
| `test:DialogConventionGuardTests` pour `STD-DIALOG` | Test absent | Non câblé |
| Règles `audit` | Le registre impose Radzen, alors que le produit expose volontairement `OmniButton`, `OmniDialog`, `OmniDataGrid`, etc. | Contrat incompatible sans overlay |

La CI exécute déjà `dotnet test OmniEurope.Blazor.slnx`; les tests de règles échoueraient donc bien en CI s'ils existaient et étaient adaptés. Le défaut est l'absence des gardes et de leur contrat local, pas l'absence d'une étape de test.

## Alignement de stack

| Attente kit | État | Verdict |
|---|---|---|
| .NET 10 | `net10.0`, SDK `10.0.302` | Conforme |
| Central Package Management | Actif dans `Directory.Packages.props` | Conforme |
| Zéro warning / zéro erreur | `TreatWarningsAsErrors=true`; préflight vert | Conforme pour les diagnostics chargés |
| Radzen pour l'UI | Aucun package Radzen; remplacement clean-room explicite | Divergence intentionnelle, non formalisée |
| `IStringLocalizer<AppStrings>` | Aucune référence `IStringLocalizer`, aucun `AppStrings`, aucun `.resx` | Non conforme |
| Repository / UnitOfWork | Aucune persistance dans la RCL | Non applicable |
| `IDbContextFactory` MAUI | Le projet principal n'est pas une app MAUI; Hybrid n'est qu'une sonde | Non applicable |
| Tests UI | bUnit + xUnit présents, 57 tests verts | Conforme, sauf gardes de conventions |

## Écarts par sévérité

### Critique

RAS.

### Élevé

1. **[Élevé] [Architecture] Projet non conforme au kit `_Generic` et type RCL non modélisé.** Le type détecté ne correspond à aucun archétype du kit, et son objectif de remplacement de Radzen contredit le contrat UI canonique (`README.md:3`, `docs/architecture.md:15`, `C:\Dev\_Generic\CLAUDE.md:5-8`, `C:\Dev\_Generic\docs\code-rules.md:76-85`). Cette ambiguïté rend les règles héritées inapplicables sans adaptation explicite. Recommandation: Codex devra formaliser un archétype RCL dans la gouvernance du projet, préciser les règles du kit conservées, adaptées ou non applicables, puis relier cette décision depuis `CLAUDE.md`.

2. **[Élevé] [Conventions] `GEN004` et `GEN008` ne sont pas appliqués, et aucun `GEN001-GEN008` n'est chargé.** Aucun `.csproj` ne référence l'assembly d'analyseurs, aucun `.razor` n'est exposé comme `AdditionalFiles`, et `.editorconfig` ne configure aucune sévérité. Le build vert ne couvre donc pas ces conventions; la présence de blocs `@code` dans 105/106 fichiers Razor de production en est une preuve observable globale. Recommandation: Codex devra intégrer l'assembly d'analyseurs, câbler les projets applicables, migrer les violations existantes sans désactiver les règles, puis promouvoir les diagnostics applicables jusqu'à l'échec de build exigé par le kit.

3. **[Élevé] [Conventions] Registre de règles incompatible et sans overlay local.** `.claude/code-rules.md` est absent, alors que `STD-RADZEN`, `STD-BTN`, `STD-DIALOG`, `STD-GRID`, `STD-FORM` et `STD-TABS` décrivent des composants Radzen que ce produit remplace volontairement. L'audit canonique classerait donc le coeur même du produit comme violation, sans contrat Omni équivalent. Recommandation: Codex devra créer un overlay complet et traçable qui conserve les IDs pertinents ou ajoute des IDs Omni dédiés, adapte les recettes de détection aux composants de la bibliothèque et documente chaque exception de portée.

4. **[Élevé] [Stack] Infrastructure de localisation obligatoire absente.** Aucun `IStringLocalizer<AppStrings>`, marqueur `AppStrings` ou fichier `.resx` n'est présent, alors que `STD-I18N` est de sévérité élevée et que le kit l'exige au niveau projet. Recommandation: Codex devra définir le contrat de localisation propre à une bibliothèque réutilisable, fournir des ressources par défaut et des points de surcharge par l'hôte, puis câbler les tests globaux correspondants.

### Moyen

1. **[Moyen] [Documentation] `CLAUDE.md` et le paquet documentaire canonique sont incomplets.** `CLAUDE.md` ne contient que la reprise de session; il manque la table de documentation, la stack, la structure, les commandes, les règles et les liens vers la gouvernance. Les documents canoniques de stack, patterns, standards, tests, agents, analyseurs et UI sont absents; les décisions sont dispersées, sans ADR. Recommandation: Codex devra transformer les 26 documents existants en carte canonique, créer uniquement les documents de gouvernance manquants qui ont une portée réelle pour la RCL, et consigner les décisions structurantes sous forme d'ADR.

2. **[Moyen] [Tests] Les trois gardes de conventions du kit sont absentes.** Aucun `NavActiveStateGuardTests`, `ButtonConventionGuardTests` ou `DialogConventionGuardTests` n'existe. La CI est prête à les exécuter, mais aucune protection ne relie actuellement les règles de navigation, boutons et dialogues à un test de source. Recommandation: après stabilisation de l'overlay, Codex devra ajouter des gardes Omni adaptées, couvrir les hôtes réellement concernés et prouver leur exécution dans la suite existante.

3. **[Moyen] [Style] `.editorconfig` est une version minimale non alignée.** Le fichier diffère sur les fins de ligne, le traitement Markdown et toutes les conventions C# du modèle; il omet aussi les sévérités d'analyseurs. Recommandation: Codex devra fusionner les règles project-neutral du kit, préserver uniquement les divergences explicitement justifiées et faire valider le résultat par un build sans warning.

4. **[Moyen] [Architecture] Les composants ne suivent aucun découpage vertical ou par famille.** Les 120 fichiers de `src/OmniEurope.Blazor/Components` sont tous à la racine du dossier, malgré les familles fonctionnelles déjà documentées. Cela concentre la navigation, les noms et les responsabilités dans un répertoire unique. Recommandation: Codex devra concevoir un découpage RCL par familles stables, préserver l'API publique et les espaces de noms nécessaires, puis aligner tests et documentation sur cette structure.

### Faible

RAS.

## Limites et frontières de cette passe

- Aucun finding granulaire n'est émis ici pour un bloc `@code`, une classe `partial`, un style, une chaîne ou un composant individuel. Ces contrôles appartiennent à la passe 3.
- Les règles EF, Repository, UnitOfWork, contrôleur Web API, MAUI et WiX sont marquées non applicables lorsque le manifeste et la structure globaux prouvent l'absence de la technologie correspondante.
- L'absence de Roslyn MCP n'affecte pas les constats de câblage, qui sont établis directement à partir des manifestes, de `.editorconfig`, des noms de tests et de la CI.

## Compteurs finaux

| Sévérité | Nombre |
|---|---:|
| Critique | 0 |
| Élevé | 4 |
| Moyen | 4 |
| Faible | 0 |
| **Total actionnable** | **8** |
