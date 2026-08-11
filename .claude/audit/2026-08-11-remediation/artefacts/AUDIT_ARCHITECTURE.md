# Audit architecture - Passes 1 et 2b

> Date : 2026-08-11  
> Base Git : `717af586cc40f3d87572e8e76b0b452ef4766b04`, avec remediations non commitees auditees dans le working tree  
> Portee : architecture physique et logique complete, 9 projets, frontieres des 12 familles, code source cle et documentation structurante  
> Mode : lecture seule du code source; aucun fichier source modifie

## Resume executif

L'architecture physique est saine et proportionnee. Le produit reste une seule Razor Class Library, les tests, outils et hotes de compatibilite convergent vers elle, et aucun projet de production ne depend d'un test ou d'un sample. Le graphe des 9 projets est acyclique, y compris la reference implicite de tous les projets non analyseur vers `OmniEurope.Analyzers`. La RCL ne reference ni Radzen, ni infrastructure de persistance, ni couche applicative etrangere a son role.

Les remediations ont ferme les ecarts architecturaux majeurs de l'audit precedent : contexte de projection partage pour les graphiques, moteurs separes pour la grille, coordination ordonnee des superpositions et retour des contextes de composition dans la surface interne. Quatre ecarts demeurent : le chemin de localisation contourne l'abstraction imposee, un renderer place dans `Internal` depend de composants publics concrets, l'hote Interactive Auto masque une dependance directe derriere une reference transitive et plusieurs fichiers contredisent encore la taxonomie canonique des familles.

Compteurs actionnables : **4 findings** - Critique 0, Eleve 0, Moyen 2, Faible 2.  
Notifications de sur-ingenierie : **2 `[INFO]` non actionnables**, exclues des compteurs et du verdict.

## Sources lues et methode

### Sources structurantes lues integralement

- `CLAUDE.md`, `.claude/code-rules.md`, `OmniEurope.Blazor.slnx`, `Directory.Build.props`, `Directory.Packages.props` et `global.json`;
- les 9 fichiers `.csproj` : production, tests, 5 hotes, analyseur et garde API;
- `docs/architecture.md`, `README.md`, `docs/component-families.md`, `docs/foundation-components.md`, `docs/form-components.md`, `docs/selection-components.md`, `docs/public-api-conventions.md`, `docs/compatibility.md`, `docs/clean-room.md`, `docs/clean-room-component-sheet.md`, `docs/csp-contract.md`, `docs/reproducibility.md`, `docs/component-roadmap.md`, `docs/migration-guide.md`, `docs/migration-aetheus.md`, `docs/localization.md`, `docs/accessibility-contract.md`, `docs/performance-budgets.md`, `docs/testing.md`, `docs/analyzers.md`, `docs/ui-conventions.md`, `docs/agents.md` et `docs/versioning.md`;
- aucun ADR n'existe dans le depot. La decision de conserver une seule RCL est portee par `docs/architecture.md:3` et `.claude/code-rules.md:17`;
- le preflight courant `.claude/audit/2026-08-11-remediation/artefacts/metrics/PREFLIGHT.md` : solution et Hybrid compiles sans avertissement, 181/181 tests reussis et couverture de lignes de 86,64 %.

### Code cle lu

- bases et transverses : `OmniComponentBase`, `OmniInputBase<TValue>`, `OmniValidatorBase<TValue>`, garde CSP, politique URI, sanitiseur, ressources et extension DI;
- moteurs et compositions : `GridProjection<TItem>`, `GridRemoteState<TItem>`, `OmniChartContext`, `OmniChartGeometry`, `OmniOverlayCoordinator`, stores et renderer de superpositions;
- facades complexes : `OmniDataGrid<TItem>`, `OmniScheduler`, `OmniHtmlEditor`, `OmniComponentsHost`, `OmniOverlayService` et leurs contextes;
- points d'entree des hotes Server, WebAssembly, Interactive Auto et MAUI Hybrid;
- garde de surface publique et analyseur Roslyn local;
- recherches croisees de namespaces, `ProjectReference`, types Omni inter-familles, contextes en cascade, localisateurs et usages Radzen.

## Outils et limites

- **MCP Roslyn : absent.** Aucun graphe semantique de symboles, `find_references` ou detecteur de cycles de types n'etait disponible.
- **Substitution utilisee :** lecture integrale des manifests, graphe factuel des `ProjectReference`, namespaces, composition Razor et recherches `rg` recoupees avec les fichiers appeles.
- **Cycles de projets : preuve forte.** Les references MSBuild explicites et la reference analyseur injectee par `Directory.Build.props` sont toutes inventoriees.
- **Cycles de types : best-effort.** L'absence de cycle logique repose sur les references textuelles verifiees et la compilation propre, pas sur une analyse Roslyn exhaustive.
- **Complexite : non fiable.** Aucun outil de complexite Roslyn n'est disponible. Aucun score manuel n'est invente.
- **Python :** no Python tool, no Python probing. Aucun interpreteur ni outil Python n'a ete sonde ou execute.
- **Securite et dependances externes :** hors de cette passe architecture; les rapports specialises et le preflight restent les autorites.

## Architecture cible reconstruite

### Unite de livraison

1. `src/OmniEurope.Blazor` est l'unique RCL de production et l'unique paquet NuGet.
2. `OmniEurope.Blazor.Components` est l'espace de noms public unique des composants et de leurs contrats.
3. Les 12 dossiers sous `Components` constituent les domaines logiques canoniques sans creer d'assemblages supplementaires.
4. `Internal` contient les mecanismes non exposes. Une facade declarative publique peut en dependre; un moteur interne ne depend pas d'un composant public concret.
5. `wwwroot` contient uniquement les assets CSS et JavaScript statiques, livres comme static web assets.
6. Les valeurs par defaut localisees passent par `IStringLocalizer<AppStrings>` ou un parametre public explicite, conformement a `.claude/code-rules.md:10`.

### Frontieres externes

- `tests/OmniEurope.Blazor.Tests` prouve les contrats de rendu, API, localisation, conventions, CSP et budgets sans devenir une dependance de production.
- `Catalog`, `WasmSmoke`, `AutoSmoke.Client` et `HybridSmoke` sont des adaptateurs terminaux de plateforme qui dependent directement de la RCL.
- `AutoSmoke` heberge le client Interactive Auto et depend de ce client; toute API RCL appelee directement par le serveur doit aussi apparaitre comme reference directe.
- `OmniEurope.PublicApiGuard` depend de la RCL compilee pour extraire sa surface publique.
- `OmniEurope.Analyzers` est une dependance de compilation uniquement, injectee comme analyseur dans tous les autres projets; il ne reference aucun d'eux.
- `docs`, `eng` et les workflows gouvernent et verifient le produit sans participer a son graphe d'execution.

### Contraintes architecturales

- aucune dependance, inheritance, wrapper ou surface d'execution Radzen;
- aucun repository, bus de messages, couche Domain/Application/Infrastructure ou service metier sans besoin mesure;
- composants parents/enfants relies par parametres, `EventCallback`, delegues annulables et contextes internes;
- moteurs internes purs ou a etat borne pour la projection, le chargement et la coordination;
- comportement specifique a un hote maintenu dans `samples`;
- API publique controlee par baseline semantique et SemVer.

## Graphe des projets et dependances

```text
OmniEurope.Analyzers <--------------------------- tous les autres projets
       (Analyzer, ReferenceOutputAssembly=false)

OmniEurope.Blazor.Tests ------------------------> OmniEurope.Blazor
OmniEurope.PublicApiGuard ----------------------> OmniEurope.Blazor
OmniEurope.Blazor.Catalog ----------------------> OmniEurope.Blazor
OmniEurope.Blazor.WasmSmoke --------------------> OmniEurope.Blazor
OmniEurope.Blazor.HybridSmoke ------------------> OmniEurope.Blazor
OmniEurope.Blazor.AutoSmoke.Client -------------> OmniEurope.Blazor
OmniEurope.Blazor.AutoSmoke --------------------> AutoSmoke.Client
OmniEurope.Blazor.AutoSmoke -- usage direct ----> OmniEurope.Blazor (reference manifeste absente, ARCH-R-03)
```

| Projet | Role | References de projet | Verdict |
| --- | --- | --- | --- |
| `src/OmniEurope.Blazor` | RCL et paquet | analyseur seulement, sans assembly runtime | Racine saine |
| `tests/OmniEurope.Blazor.Tests` | bUnit et contrats | RCL + analyseur | Direction correcte |
| `eng/OmniEurope.PublicApiGuard` | extraction de l'API publique | RCL + analyseur | Direction correcte |
| `eng/OmniEurope.Analyzers` | diagnostics GEN001-GEN008 | aucune | Feuille saine |
| `samples/OmniEurope.Blazor.Catalog` | reference Server | RCL + analyseur | Direction correcte |
| `samples/OmniEurope.Blazor.WasmSmoke` | sonde WebAssembly | RCL + analyseur | Direction correcte |
| `samples/OmniEurope.Blazor.AutoSmoke.Client` | composant Interactive Auto client | RCL + analyseur | Direction correcte |
| `samples/OmniEurope.Blazor.AutoSmoke` | serveur Interactive Auto | client + analyseur | Dependence RCL directe masquee |
| `samples/OmniEurope.Blazor.HybridSmoke` | sonde MAUI Windows | RCL + analyseur | Direction correcte; hors solution generale et compile separement en CI |

**Cycles :** aucun cycle de projet. Le graphe d'execution est oriente vers la RCL; le graphe de compilation ajoute seulement l'analyseur comme feuille commune. Aucun package Radzen n'apparait dans un manifeste de production.

## Carte module vers domaine

### Module `OmniEurope.Blazor`

| Domaine physique | Responsabilite observee | Dependances logiques admises | Evaluation |
| --- | --- | --- | --- |
| `Foundation` | base de composant, apparence, densite et enums fondamentaux | BCL, Blazor, `Internal` transversal | Cohesif; niveau le plus bas |
| `Layout` | structure de page, grille, piles, cartes, titres et landmarks | `Foundation` | Cohesif, avec un ecart de taxonomie pour `OmniHeading` |
| `Feedback` | alertes, badges, progression, skeleton et contenu visuel | `Foundation` | Fonctionnel, mais mele feedback et primitives de contenu |
| `Actions` | boutons, boutons scindes, bascules et apparence | `Foundation` | Cohesif hors listes radio mal placees |
| `Forms` | `InputBase`, champs, formulaires et validateurs | `Foundation`, Blazor Forms, localisation | Cohesif; frontiere avec `Selection` a clarifier |
| `Selection` | options, listes, autocomplete, slider et upload | `Forms`, `Feedback`, `Foundation` | Cohesif; reutilise les bases formulaire |
| `Navigation` | liens, menus, breadcrumb, onglets, etapes et sidebar | `Foundation`, `Feedback`, `NavigationManager` | Cohesif; contextes internes |
| `Data` | liste, pager, arbre et grille | `Foundation`, moteurs `Internal` | Cohesif; moteurs de projection et chargement maintenant separes |
| `Charts` | SVG, axes, series, domaines et jauges | `Foundation`, contexte interne de projection | Cohesif; domaines et empilements centralises |
| `Scheduling` | timeline et scheduler jour/semaine/mois | `Foundation`, temps et callbacks annulables | Cohesif; contrat de recurrence speculatif en INFO |
| `Overlays` | hote, service, dialogs, notifications, tooltip et menu contextuel | `Foundation`, coordinateur et stores internes | Fonctionnel; renderer inverse une frontiere logique |
| `Editor` | edition HTML, historique, selection et sanitisation | `Forms`, sanitiseur et module JS internes | Petit mais justifie par un risque et un contrat distincts |
| `Internal` | CSP, URI, CSS, localisation, sanitisation, projection, chargement, stores et coordination | contrats de donnees des composants, jamais hotes/tests | Transversal necessaire; un renderer concret est mal place |
| `wwwroot` | CSS et interop focus/editeur | APIs navigateur | Frontiere statique correcte |

### Modules de verification et de gouvernance

| Module | Domaine | Frontiere attendue |
| --- | --- | --- |
| `Tests` | preuves de rendu, interactions, API, conventions et budgets | depend de la RCL seulement |
| `Catalog` | documentation executable Server et collecte CSP | terminal, aucune logique reutilisee par la RCL |
| `WasmSmoke` | preuve WebAssembly | terminal |
| `AutoSmoke.Client` + `AutoSmoke` | preuve Interactive Auto client/serveur | couple impose par la plateforme, sans logique produit |
| `HybridSmoke` | preuve MAUI Blazor Hybrid Windows | terminal et platform-specific |
| `Analyzers` | politiques de compilation | dependance compile-time, sans dependance produit |
| `PublicApiGuard` | preuve de surface publique | outil terminal |
| `Repository` | documentation, generation, paquetage et CI | hors execution du paquet |

## Decoupage en domaines

### Cohesion et couplage

- **Assemblages : adaptes.** Les domaines partagent un cycle de livraison, le meme espace public, les memes assets et aucun consommateur n'exige une version independante. Les scinder en projets multiplierait package references, versionnement et DI sans gain mesure.
- **Domaines complexes : non anemiques.** `Charts`, `Data`, `Forms`, `Selection`, `Overlays` et `Scheduling` contiennent comportements, contrats et preuves propres. `Editor`, plus petit, a une frontiere justifiee par la sanitisation et l'interop de selection.
- **Fondation : volontairement anemique.** Ses 15 fichiers sont des bases et enums stables; cette faible taille est correcte pour un niveau transversal et ne justifie pas une fusion opportuniste.
- **Couplage inter-familles : globalement acyclique.** Les recherches de types montrent principalement les sens `Selection -> Forms`, `Editor -> Forms`, et familles de rendu vers `Foundation`. Le seul edge etranger a l'intention de domaine, `Actions -> Selection/Forms`, vient des listes radio mal placees.
- **Moteurs complexes : correctement extraits.** Grille, graphiques et superpositions ne concentrent plus toute leur coordination dans le markup public.
- **Surface publique : correctement encapsulee.** `OmniDataGridContext`, `OmniDataGridColumnDefinition`, `OmniTabsContext`, `OmniStepsContext`, `OmniTreeContext`, `OmniChartContext`, `GridProjection`, `GridRemoteState` et `OmniOverlayCoordinator` sont absents de `docs/public-api.txt`.

### Direction interne reelle

Le sens dominant respecte la cible : composants publics vers bases et moteurs internes. Les moteurs de grille dependent de contrats de donnees (`OmniDataGridSort`, filtres, resultats), sans referencer `OmniDataGrid<TItem>` lui-meme. Les stores de superpositions dependent de records de transport publics, sans rendre de composant. En revanche, `OmniOverlayHosts` ouvre directement `OmniDialog` et `OmniNotification` depuis le namespace `Internal`, ce qui cree un aller-retour logique `Components <-> Internal` detaille dans ARCH-R-02.

## Ecarts actionnables par severite

### Critique

Aucun.

### Eleve

Aucun.

### Moyen

#### ARCH-R-01 - La localisation de rendu contourne l'abstraction imposee

`src/OmniEurope.Blazor/Internal/OmniStrings.cs:6`

[Moyen] [Architecture] `OmniStrings` construit directement un `ResourceManager` statique et les 111 appels `OmniStrings.Get` repartis dans 53 fichiers passent par ce singleton ambiant, tandis que `AddOmniEuropeBlazor` ne fait qu'enregistrer `AddLocalization`. Aucun composant de la RCL n'injecte `IStringLocalizer<AppStrings>`. Le chemin d'execution contredit donc `.claude/code-rules.md:10` et l'affirmation de `docs/localization.md:9`; il masque la dependance de localisation, rend l'inscription DI sans effet sur les composants eux-memes et interdit les implementations ou decorateurs de localizer fournis par l'hote - lignes 6-20, `OmniEuropeBlazorServiceCollectionExtensions.cs:5-9`, recherche `IStringLocalizer|OmniStrings.Get` - recommandation : Codex peut etablir un point d'acces localise injecte pour les bases de composants et transmettre explicitement le localizer aux renderers non composants, conserver les parametres publics de remplacement, puis prouver par tests qu'un `IStringLocalizer<AppStrings>` remplace est effectivement consulte.

#### ARCH-R-02 - Le renderer de superpositions inverse la frontiere `Components -> Internal`

`src/OmniEurope.Blazor/Internal/OmniOverlayHosts.cs:3`

[Moyen] [Architecture] Le document cible interdit aux moteurs `Internal` de dependre d'un composant public concret (`docs/architecture.md:21`), mais `OmniOverlayHosts` importe `OmniEurope.Blazor.Components` puis instancie directement `OmniDialog` et `OmniNotification` aux lignes 23 et 58. `OmniComponentsHost`, composant public, appelle ensuite ce renderer interne aux lignes 11-13. Cette boucle logique ne forme pas un cycle d'assembly, mais brouille l'ownership et permet a `Internal` de piloter les parametres et le cycle de rendu de facades publiques - source : references directes et composition Razor - recommandation : Codex peut deplacer ce renderer interne dans le domaine `Components/Overlays` (namespace public conserve, type toujours `internal`) ou rendre la composition dans le code-behind de l'hote, tout en laissant `Internal` limiter sa dependance aux stores, coordinateurs et contrats sans composant concret.

### Faible

#### ARCH-R-03 - Interactive Auto utilise directement la RCL sans reference directe

`samples/OmniEurope.Blazor.AutoSmoke/OmniEurope.Blazor.AutoSmoke.csproj:7`

[Faible] [Architecture] Le serveur `AutoSmoke` ne declare que `AutoSmoke.Client`, mais son `Program.cs:7` appelle directement `AddOmniEuropeBlazor` et son `App.razor:11` nomme l'asset de la RCL. La compilation reussit grace a la transitivite de `AutoSmoke.Client -> OmniEurope.Blazor`, ce qui masque une dependance reelle et couple le serveur a un detail du graphe client - lignes 7-10 du projet, preuve croisee des points d'entree - recommandation : Codex peut ajouter une `ProjectReference` directe vers la RCL au serveur Auto, conserver la reference client pour l'assembly interactif, puis verifier restore verrouille, build et runtime Auto.

#### ARCH-R-04 - La taxonomie canonique ne correspond pas encore au placement physique

`docs/architecture.md:23`

[Faible] [Architecture] Les 12 dossiers sont declares canoniques et `docs/component-families.md` definit leurs responsabilites, mais les listes radio documentees comme `Selection` (`docs/selection-components.md:7-12`) sont sous `Components/Actions`, `OmniDatePicker` et `OmniColorPicker` documentes comme selection avancee (`docs/selection-components.md:23-28`) sont sous `Forms`, tandis que `OmniText` et `OmniHeading`, decrits comme `Foundation` (`docs/component-families.md:11`), sont respectivement sous `Feedback` et `Layout`. Cela introduit notamment les edges artificiels `Actions -> Selection` et `Actions -> Forms` et rend l'ownership, les recherches et l'audit par domaine moins fiables - source : inventaire des dossiers et recherche des references inter-familles - recommandation : Codex peut choisir et documenter une regle unique de placement, deplacer les fichiers vers la famille canonique sans changer leur namespace public, puis regenerer les registres de couverture et verifier que chaque vue agregee conserve exactement les 12 noms.

## Proportionnalite et sur-ingenierie

`PROPORTIONNALITE: ADAPTEE` - La solution la plus simple qui preserve les contraintes observees est une RCL unique, un projet de tests, deux outils de compilation et cinq hotes/adaptateurs de plateforme. Une separation Domain/Application/Infrastructure, un repository, une interface par composant ou un bus global ajouterait des assemblies, de la DI et du versionnement sans second consommateur ni responsabilite de deploiement independante. Les moteurs internes, callbacks annulables et contextes locaux sont proportionnes aux comportements actuels.

### Notifications consultatives, non actionnables

`src/OmniEurope.Blazor/Components/Overlays/OmniComponentsHost.razor:6`

[INFO] [Sur-ingenierie] Le service `OmniOverlayService` est cascade autour de `ChildContent`, mais aucune `[CascadingParameter]` de ce type n'existe dans la RCL, les tests ou les samples. Cette extension ambiante ajoute une dependance implicite et un noeud de rendu sans cas d'usage actuel; l'alternative plus simple est de conserver le service comme reference explicite passee a l'hote et de n'ajouter le cascade qu'avec un premier composant consommateur. **Notification consultative, non actionnable, exclue des findings et du verdict.**

`src/OmniEurope.Blazor/Components/Scheduling/OmniSchedulerAppointment.cs:9`

[INFO] [Sur-ingenierie] `RecurrenceRule` est un contrat public optionnel sans lecteur dans la RCL, les tests, les samples ou les documents de scenarios. Il engage pourtant une future grammaire, les fuseaux, les exceptions et le versionnement public; l'alternative plus simple est de ne porter ce champ qu'avec un premier besoin observable et son moteur d'expansion. **Notification consultative, non actionnable, exclue des findings et du verdict.**

## Conformites et ecarts precedents fermes

- **Graphiques : conforme.** `OmniChartContext` centralise enregistrement, domaines, projection et baselines positives/negatives; les contextes restent internes.
- **DataGrid : conforme au niveau architecture.** `GridProjection<TItem>` et `GridRemoteState<TItem>` separent projection pure et cycle de chargement de la facade de rendu.
- **Superpositions : progression conforme.** Un coordinateur et des stores internes fournissent pile de dialogs, notifications bornees et portail ordonne; seul le placement du renderer concret reste en ecart.
- **Encapsulation : conforme.** Les contextes de grille, arbre, tabs, steps et chart, ainsi que les definitions de colonnes, ne figurent plus dans la baseline publique.
- **Clean-room : conforme au graphe d'execution.** Aucun manifeste ou code de production ne depend d'un package, namespace ou composant Radzen. Les occurrences dans `eng` sont limitees aux detecteurs et fixtures d'inventaire.
- **Compatibilite des hotes : conforme architecturalement.** Le comportement specifique Server, WASM, Auto et MAUI reste aux frontieres `samples`.

## Verdict des passes 1 et 2b

- Architecture physique et unite de livraison : **saine et proportionnee**.
- Direction des dependances de projets : **acyclique**, avec une reference directe a expliciter dans AutoSmoke.
- Encapsulation des moteurs et contextes : **globalement saine**, avec un renderer concret mal place.
- Decoupage logique : **pertinent**, mais placement de quatre groupes a realigner avec la taxonomie canonique.
- Abstraction de localisation : **non conforme au contrat impose**.
- Verdict architecture : **NEEDS ATTENTION**, motive par 4 findings actionnables, sans finding critique ni eleve.

## Compteurs exacts

| Severite | Nombre actionnable |
| --- | ---: |
| Critique | 0 |
| Eleve | 0 |
| Moyen | 2 |
| Faible | 2 |
| **Total** | **4** |

`[INFO] [Sur-ingenierie]` : 2 notifications non actionnables, hors total.
