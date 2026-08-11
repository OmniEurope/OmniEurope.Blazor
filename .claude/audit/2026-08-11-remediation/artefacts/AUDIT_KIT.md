# Audit 360 - Passe 1b - Conformité au kit `_Generic`

Date de vérification : 2026-08-11  
Référentiel : `C:\Dev\_Generic` (`CLAUDE.md`, `docs/code-rules.md`, `docs/coding-standards.md`, `docs/roslyn-analyzers.md`, `.editorconfig`, `analyzers/`)  
Périmètre : structure et documentation du dépôt, câblage global des conventions et analyseurs, type de projet, stack et localisation. Cette passe est une lecture des sources; elle ne remplace ni le build, ni les tests, ni la revue granulaire fichier par fichier de la passe 3.

## Verdict et compte exact

- Findings actionnables : **12**
- Critique : **0**
- Élevé : **2**
- Moyen : **9**
- Faible : **1**
- INFO de proportionnalité : **1**, exclue du total

## Type de projet détecté

Le dépôt est une **Razor Class Library .NET 10** distribuée par NuGet, avec un projet de tests bUnit, trois hôtes Web dans la solution et un hôte MAUI Hybrid validé séparément sous Windows. Ce type n'est pas l'un des trois archétypes explicites du kit (Web WASM + API, MAUI Hybrid, MAUI + WiX), mais l'écart est intentionnel et clairement décrit dans `CLAUDE.md:5`, `docs/architecture.md:1` et `.claude/code-rules.md:14-26`.

La structure observée est proportionnée au produit :

- une seule RCL de production sous `src/OmniEurope.Blazor`;
- des familles de composants sous `Components/`, adaptées à une bibliothèque plutôt qu'à des tranches métier;
- un projet de tests sous `tests/OmniEurope.Blazor.Tests`;
- des hôtes Server, WebAssembly, Interactive Auto et MAUI Hybrid sous `samples/`;
- le code Windows du smoke host MAUI reste sous `Platforms/Windows/`;
- aucune couche API, persistence, dépôt, déploiement ou installateur fictif n'a été ajoutée.

Cette adaptation de structure est acceptable. Son défaut formel est traité par `KIT-001` : les dérogations ne sont pas enregistrées sous les identifiants `STD-*` que le mécanisme de fusion du kit exige.

## Documentation

Éléments conformes :

- `CLAUDE.md`, `README.md`, `CHANGELOG.md`, `CONTRIBUTING.md` et `SECURITY.md` sont présents;
- `CLAUDE.md:46-48` contient bien `## Session resume` et la lecture conditionnelle de `.claude/handoff.md`;
- l'absence actuelle de `.claude/handoff.md` n'est pas une violation, car la règle est conditionnelle;
- `docs/architecture.md`, `docs/testing.md`, `docs/analyzers.md`, `docs/ui-conventions.md`, `docs/localization.md`, les contrats CSP/accessibilité, la documentation clean-room et les documents de versionnement couvrent les responsabilités spécifiques de la RCL;
- les liens Markdown locaux contrôlés dans les fichiers du dépôt ont tous une cible existante;
- l'absence d'ADR n'est pas retenue comme finding : `CLAUDE.md` documente explicitement que les décisions déjà tracées ne doivent pas être dupliquées et qu'un ADR ne sera créé que pour une nouvelle décision durable comparant des alternatives.

Écarts documentaires : `KIT-002` et `KIT-003`.

## Conventions et analyseurs

### Câblage global observé

- `.editorconfig` reprend les conventions neutres du kit et configure `GEN001` à `GEN008`;
- `Directory.Build.props:7` active `TreatWarningsAsErrors`;
- `Directory.Build.props:12-15` référence `eng/OmniEurope.Analyzers` comme analyseur pour tous les projets consommateurs;
- `Directory.Build.props:18-21` fournit les fichiers Razor comme `AdditionalFiles` aux projets ayant `EnforceRazorCodeBehind=true`;
- tous les projets Razor de production et de démonstration positionnent cette propriété;
- `eng/OmniEurope.Analyzers/OmniEuropeConventionAnalyzer.cs:15-25` déclare les huit diagnostics;
- la gestion centrale des versions est activée dans `Directory.Packages.props` et les restaurations CI utilisent le mode verrouillé;
- les références GitHub Actions sont figées par SHA complet et Dependabot couvre NuGet et GitHub Actions.

Le câblage statique est donc présent, mais sa fidélité et sa preuve sont incomplètes : voir `KIT-004` et `KIT-005`.

### Registre `STD-*` et enforcement

| Règle | Couche canonique | État global observé |
|---|---|---|
| `STD-PARTIAL` | `analyzer:GEN008` + audit | Analyseur référencé et promu; fidélité partielle à l'exception source-generator, voir `KIT-004`. |
| `STD-RADZEN` | audit | La RCL clean-room doit l'inverser, mais l'overlay ne redéfinit pas formellement l'ID, voir `KIT-001`. |
| `STD-STYLE` | audit | Contrat CSP, `eng/Test-Csp.ps1` et fixtures exécutés en CI; la recette reste auditée fichier par fichier. |
| `STD-I18N` | audit | `AddLocalization()`, `AppStrings` FR/EN et tests de ressources sont présents; la revue des littéraux appartient à la passe 3. |
| `STD-UIVERIFY` | audit | Les hôtes Catalog, WASM, Auto et Hybrid ont des sondes navigateur/WebView2 câblées en CI. |
| `STD-FOCUS` | audit | Les sondes et tests de focus existent; les violations de fichiers éventuelles relèvent de la passe 3. |
| `STD-BTN` | audit + `ButtonConventionGuardTests` | Test présent mais recette incomplète, voir `KIT-006`. |
| `STD-DIALOG` | audit + `DialogConventionGuardTests` | Test présent mais recette incomplète, voir `KIT-007`. |
| `STD-GRID` | audit | Règle Radzen destinée à être adaptée à `OmniDataGrid`; adaptation non enregistrée par ID, voir `KIT-001`. |
| `STD-FORM` | audit | Règle Radzen destinée à être adaptée aux entrées Omni; adaptation non enregistrée par ID, voir `KIT-001`. |
| `STD-NAV` | `NavActiveStateGuardTests` + audit | Test présent mais il ne vérifie pas la couverture routes/items, voir `KIT-008`. |
| `STD-GRIDTITLE` | audit | Règle sélecteur Radzen destinée à être non applicable; statut non enregistré par ID, voir `KIT-001`. |
| `STD-TABS` | audit | Le wrapper Radzen canonique est non applicable à `OmniTabs`; statut non enregistré par ID, voir `KIT-001`. |

## Stack et localisation

Points conformes :

- cible `net10.0`, SDK et workload set `10.0.302` verrouillés sans roll-forward;
- nullable, builds déterministes et avertissements traités comme erreurs;
- Central Package Management;
- `Microsoft.Extensions.Localization`, `AddLocalization()`, marqueur public `AppStrings`, ressource française de repli et ressource anglaise;
- aucune dépendance Radzen dans les projets;
- `Microsoft.Maui.Controls` et `Microsoft.AspNetCore.Components.WebView.Maui` sont alignés en `10.0.90`;
- `Microsoft.NET.Test.Sdk 18.8.1`, `xunit.v3 3.2.2`, `xunit.runner.visualstudio 3.1.5` et `coverlet.collector 10.0.1` correspondent aux versions stables publiées au moment de cette passe.

La règle du kit imposant les dernières versions stables n'est toutefois pas satisfaite pour trois dépendances directes, et le gate local ne peut pas le détecter : voir `KIT-009` à `KIT-012`.

## Écarts par sévérité

### Élevé

#### KIT-001 - Overlay non fusionnable avec le registre canonique

`[Élevé] [Conventions]` `.claude/code-rules.md:5-30` décrit les règles héritées, adaptées et non applicables uniquement en prose. Il ne contient ni table de registre, ni sections `STD-*`, ni champs Statement/Scope/Layer/Detection/Severity/Exceptions. Or `C:\Dev\_Generic\docs\code-rules.md:367-373` n'autorise un overlay à gagner la fusion que sur un même identifiant. En l'état, `STD-RADZEN`, les règles Radzen de grille/formulaire/onglets et leurs severités canoniques restent formellement héritées, alors que la documentation locale affirme l'inverse. Les audits et contributeurs ne disposent donc pas d'une autorité déterministe. Codex doit convertir l'overlay au format du registre, redéfinir explicitement chaque ID adapté ou non applicable et conserver une recette de détection vérifiable pour l'équivalent Omni.

#### KIT-009 - La politique de dépendances affirme à tort que `bunit 2.9.0` n'existe pas

`[Élevé] [Stack/Authenticité]` `Directory.Packages.props:6`, `eng/dependency-policy.json:3-9` et `docs/dependencies.md:7` figent `bunit 2.8.6` et présentent ce choix comme vérifié le 11 août 2026, avec l'affirmation explicite que `2.9.0` n'existe pas. La page officielle [bunit 2.9.0](https://www.nuget.org/packages/bunit/) indique au contraire une publication stable le 3 août 2026, compatible avec `net10.0`. Le gate valide donc une preuve devenue factuellement fausse avant sa propre date de revue. Codex doit mettre à jour bUnit et tous les verrous/artefacts SBOM associés, corriger la documentation et faire échouer la politique lorsqu'une preuve de fraîcheur ne correspond plus au catalogue officiel.

### Moyen

#### KIT-002 - Point d'entrée `AGENTS.md` absent

`[Moyen] [Documentation]` Le fichier racine `AGENTS.md` est absent, alors que `C:\Dev\_Generic\CLAUDE.md` impose la copie de `docs/agents-template.md` et que `docs/agents.md` local suppose déjà un parcours de lecture pour les contributeurs automatisés. Sans ce point d'entrée fin, certains hôtes ne découvrent pas automatiquement `CLAUDE.md`, l'overlay et le plan actif. Codex doit ajouter le délégateur racine adapté au nom du projet, sans y dupliquer les règles canoniques.

#### KIT-004 - L'analyseur local diverge du comportement canonique de trois diagnostics

`[Moyen] [Analyseurs]` `eng/OmniEurope.Analyzers/OmniEuropeConventionAnalyzer.cs:110` omet les exemptions `*Job`, `*Storage` et `AuditLogger` de `GEN002`; `:131-134` omet les gardes `FirstOrDefaultAsync` et `SingleOrDefaultAsync` de `GEN006`; `:137-145` ne reconnaît pas les propriétés `partial` sans corps couvertes par l'exception source-generator de `GEN008`. Le projet annonce pourtant une implémentation de `GEN001-GEN008` conforme au kit dans `docs/analyzers.md`. Ces divergences peuvent faire échouer le build sur des cas explicitement autorisés par le référentiel. Codex doit réaligner ces trois chemins sur les sources canoniques de `C:\Dev\_Generic\analyzers` ou documenter des overrides `STD-*` précis si une divergence est réellement voulue.

#### KIT-005 - Aucun test positif/négatif ne prouve les huit diagnostics

`[Moyen] [Tests/Analyseurs]` Aucun projet `OmniEurope.Analyzers.Tests` n'existe dans `eng/` ou dans `OmniEurope.Blazor.slnx`, et la recherche de `GEN001-GEN008` dans les tests ne trouve que deux assertions textuelles sur `.editorconfig` dans `ConventionGuardTests.cs:69-80`. Le catalogue du kit exige un cas positif et un cas négatif par règle; vérifier la présence d'une chaîne de configuration ne prouve pas que l'analyseur détecte ou exempte correctement. Codex doit ajouter une suite Roslyn dédiée couvrant chaque diagnostic, ses exemptions et le câblage Razor `AdditionalFiles`, puis l'inclure dans la solution et la CI.

#### KIT-006 - `ButtonConventionGuardTests` ne couvre pas `STD-BTN`

`[Moyen] [Conventions/Tests]` `tests/OmniEurope.Blazor.Tests/ConventionGuardTests.cs:22-34` vérifie uniquement la présence d'un attribut HTML `type` sur les balises `<button>`. Il ne couvre ni texte + icône hors grille, ni bouton icon-only localisé en grille, ni largeur, ni taille minimale, ni couleur par rôle, ni cohérence transversale, alors que le nom du test est exactement celui déclaré par le registre. Le test donne donc une fausse impression d'enforcement. Codex doit soit implémenter toute la recette `STD-BTN`, soit renommer ce test et conserver `STD-BTN` comme contrôle audit explicite dans un override RCL.

#### KIT-007 - `DialogConventionGuardTests` ne couvre pas `STD-DIALOG`

`[Moyen] [Conventions/Tests]` `tests/OmniEurope.Blazor.Tests/ConventionGuardTests.cs:349-362` confirme rôle, modalité, Escape et interop de focus sur un seul composant. Il ne balaie pas les appels de dialogue, les opt-outs de fermeture, la présence d'une action Fermer/Annuler sans effet de bord, ni les confirmations destructives à deux actions. Le test ne couvre donc pas les cas requis par le registre. Codex doit étendre la garde à tous les usages concernés ou formaliser dans l'override `STD-DIALOG` la frontière de responsabilité entre la RCL et ses hôtes, avec des tests correspondant exactement à cette nouvelle recette.

#### KIT-008 - `NavActiveStateGuardTests` ne vérifie aucune couverture route/navigation

`[Moyen] [Conventions/Tests]` `tests/OmniEurope.Blazor.Tests/ConventionGuardTests.cs:10-19` recherche quatre fragments d'implémentation dans `OmniPanelMenuItem`; il ne collecte ni les routes `@page`, ni les chemins d'items, ni les doublons, ni les groupes de navigation comme l'exige `STD-NAV`. Le nom canonique du test masque donc une absence d'enforcement. Codex doit soit fournir le scanner route/item demandé pour les hôtes qui possèdent une navigation, soit déclarer formellement `STD-NAV` non applicable à la RCL et renommer le test actuel selon le comportement qu'il prouve réellement.

#### KIT-010 - `HtmlSanitizer` n'est pas à la dernière version stable

`[Moyen] [Dépendances]` `Directory.Packages.props:8` utilise `HtmlSanitizer 9.1.973`, tandis que la page officielle [HtmlSanitizer 9.2.995](https://www.nuget.org/packages/HtmlSanitizer) publie `9.2.995` comme version stable courante. La dépendance protège une surface HTML sensible, et le kit impose la dernière stable. Codex doit mettre à jour le package, régénérer les verrous, le SBOM et les notices, puis rejouer les tests du sanitiseur, CSP, éditeur HTML et paquet.

#### KIT-011 - `Microsoft.CodeAnalysis.CSharp` est très en retard sur la stable compatible

`[Moyen] [Dépendances/Analyseurs]` `Directory.Packages.props:12` utilise `Microsoft.CodeAnalysis.CSharp 4.12.0`; la page officielle [Microsoft.CodeAnalysis.CSharp 5.6.0](https://www.nuget.org/packages/microsoft.codeanalysis.csharp) indique `5.6.0` comme stable courante et compatible `netstandard2.0`. L'analyseur local compile donc contre une génération de Roslyn ancienne malgré la règle de fraîcheur du kit. Codex doit migrer la référence, régénérer les verrous/SBOM et valider la suite d'analyseurs dédiée demandée par `KIT-005`.

#### KIT-012 - Le gate de dépendances ne vérifie pas toutes les références directes ni leur fraîcheur

`[Moyen] [Enforcement]` `eng/Test-DependencyPolicy.ps1:14-48` contrôle seulement les quatre packages listés dans `eng/dependency-policy.json`, puis compare ces versions statiques aux fichiers centraux et de verrouillage. Il ne vérifie ni les autres références directes, dont `HtmlSanitizer` et Roslyn, ni l'état courant du catalogue. La CI peut donc être verte avec des dépendances non conformes et une affirmation fausse comme `KIT-009`. Codex doit couvrir chaque `PackageVersion` direct par une politique révisable et ajouter une preuve de contrôle des versions stables, tout en conservant Dependabot comme mécanisme de proposition et non comme preuve de conformité instantanée.

### Faible

#### KIT-003 - Carte documentaire non conforme au format et correspondances du kit implicites

`[Faible] [Documentation]` `CLAUDE.md:35-44` possède une carte documentaire sous forme de puces, pas la table demandée par le kit. De plus, les équivalents locaux de `tech-stack.md`, `code-patterns.md`, `coding-standards.md` et `agent-principles.md` sont dispersés entre `CLAUDE.md`, `docs/dependencies.md`, `docs/public-api-conventions.md`, `docs/ui-conventions.md` et `.claude/code-rules.md`, sans matrice disant explicitement quel document remplace chaque entrée canonique ni quelles entrées sont non applicables à une RCL. Codex doit convertir la carte en table et y déclarer ces correspondances, sans créer de documentation dupliquée.

## INFO proportionnalité et sur-ingénierie

`[INFO] [Sur-ingénierie]` **Aucune notification d'excès de conception.** Pour le besoin actuel, une RCL unique avec moteurs internes, un projet de tests et des hôtes de preuve est plus simple que le découpage `.Back/.Front/.Shared`, Repository/UnitOfWork ou une couche applicative par composant. Ces couches n'ont ni responsabilité autonome, ni deuxième consommateur démontré, ni contrainte mesurée ici. Les ajouter augmenterait les références, la documentation et les points de publication sans préserver davantage de comportement, sécurité ou testabilité. Cette appréciation est consultative et ne modifie pas le verdict ni le compte des findings.

## Limites de cette passe

- Aucun build ou test n'a été lancé dans ce shard de lecture seule; le câblage est vérifié dans les fichiers, pas par une exécution locale.
- Les violations granulaires dans les composants, CSS, JavaScript et tests appartiennent à la passe 3 et ne sont pas comptées ici.
- Les dépendances ont été comparées aux pages officielles NuGet uniquement pour les références directes nécessaires à l'alignement de stack; l'audit de dépendances dédié reste l'autorité exhaustive pour vulnérabilités, licences et transitifs.

