# Audit architecture - Passes 1 et 2b

> Date : 2026-08-11  
> Révision source : `717af586cc40f3d87572e8e76b0b452ef4766b04`  
> Portée : structure complète, solution, 7 projets, références, documentation et code clé des frontières logiques  
> Mode : lecture seule du code et de Git

## Résumé exécutif

L'architecture physique est saine et proportionnée : une Razor Class Library constitue le produit, les tests et les hôtes de compatibilité ne dépendent que d'elle, et aucun projet de production ne dépend d'un projet de test ou d'échantillon. Le graphe des `ProjectReference` est acyclique. La bibliothèque ne référence ni Radzen, ni infrastructure de persistance, ni couche applicative étrangère à sa responsabilité de composants Blazor.

Les écarts se situent dans le découpage logique interne. Le domaine graphique ne possède pas le contexte de composition annoncé par la documentation, le sous-système de superpositions n'a pas encore sa frontière de portail, `OmniDataGrid` agrège trop de responsabilités et plusieurs contextes de composition internes ont fui dans l'API publique. Enfin, la taxonomie des familles n'est pas canonique à l'échelle du dépôt.

Compteurs actionnables : **5 constats** - Critique 0, Élevé 1, Moyen 3, Faible 1.  
Notifications informatives de sur-ingénierie : **1**, exclue des constats actionnables et du verdict.

## Sources et méthode

### Éléments lus

- inventaire complet des fichiers du dépôt hors sorties `bin`, `obj`, `artifacts` et état auto-généré `.claude/audit/**` ;
- `OmniEurope.Blazor.slnx`, les 7 fichiers `.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `global.json` et les fichiers de verrouillage ;
- documentation d'architecture, contrats publics, compatibilité, CSP, accessibilité, clean-room, reproductibilité, familles, feuille de route, migration et plans canoniques ;
- aucun fichier ADR n'existe dans le dépôt ; la décision de conserver une seule RCL est portée directement par `docs/architecture.md:3` ;
- code clé des bases et utilitaires, formulaires, sélection, navigation, collections, DataGrid, superpositions, graphiques, planification, éditeur HTML et hôtes de compatibilité ;
- usages des types de coordination publics et références inter-composants vérifiés par recherches textuelles recoupées ;
- préflight de l'audit : build Release à 0 avertissement et 0 erreur, 57 tests réussis, 0 échoué, 0 ignoré.

### Limite sémantique

Aucun MCP Roslyn n'est disponible. La détection des cycles, inversions et usages repose donc sur les `ProjectReference`, les namespaces, les compositions Razor et des recherches `rg` vérifiées. Le graphe de projets est factuel, mais l'absence de cycle de types ne bénéficie pas d'une preuve sémantique Roslyn exhaustive. Aucun score de complexité ou CRAP n'est attribué : la couverture et la complexité outillée sont indisponibles selon `metrics/PREFLIGHT.md` et `metrics/crap_report.md`. Aucun outil Python n'a été sondé ou exécuté.

### MCP utilisés

- MCP Roslyn : **aucun**, donc aucune construction sémantique du graphe de symboles ni détection outillée des cycles de types.
- Autres MCP : **aucun pour ces passes**. Les MCP Playwright, Node et connecteurs recensés au préflight ne fournissent pas d'analyse architecturale .NET pertinente.
- Substitution vérifiée : `git` en lecture seule pour le graphe de projets et la révision, `rg` pour les références et usages, puis lecture intégrale des manifests et du code clé.

## Architecture cible reconstruite

### Intention et responsabilités

1. **Produit** : `src/OmniEurope.Blazor` est une unique Razor Class Library distribuée comme paquet `OmniEurope.Blazor`.
2. **Surface publique** : `OmniEurope.Blazor.Components` expose des composants orientés capacités, leurs modèles d'entrée et leurs callbacks. La surface n'est ni un fork, ni un adaptateur binaire de Radzen.
3. **Socle interne** : `Internal` contient seulement les utilitaires non exposés, notamment la garde CSP, la composition de classes CSS et la sanitisation HTML.
4. **Assets** : `wwwroot` livre une feuille CSS et un module JavaScript statiques via `_content/OmniEurope.Blazor/`.
5. **Vérification** : un projet bUnit valide les contrats de rendu ; des hôtes Server, WebAssembly, Interactive Auto et MAUI Hybrid prouvent la compatibilité de compilation ou de publication selon la plateforme.
6. **Gouvernance** : `docs`, `eng`, les plans et les workflows décrivent les contrats, génèrent les inventaires et ferment les gates de paquetage.

La direction attendue est centripète vers la bibliothèque :

```text
OmniEurope.Blazor.AutoSmoke -> OmniEurope.Blazor.AutoSmoke.Client -> OmniEurope.Blazor
OmniEurope.Blazor.Catalog ----------------------------------------> OmniEurope.Blazor
OmniEurope.Blazor.WasmSmoke --------------------------------------> OmniEurope.Blazor
OmniEurope.Blazor.HybridSmoke ------------------------------------> OmniEurope.Blazor
OmniEurope.Blazor.Tests ------------------------------------------> OmniEurope.Blazor

OmniEurope.Blazor -> Microsoft.AspNetCore.Components.Web + BCL
```

À l'intérieur de la RCL, les composants peuvent utiliser les primitives `Internal`; le sens inverse est interdit. Les composants parents et enfants communiquent par paramètres, `EventCallback`, callbacks annulables ou contextes en cascade. Aucune dépendance vers un hôte, les tests, le catalogue ou un consommateur n'est attendue.

### Principes de conception attendus

- conserver un seul paquet et un seul projet tant qu'aucun consommateur réel n'exige des unités de versionnement ou de déploiement distinctes ;
- garder les dépendances de plateforme à la frontière Blazor, sans couche de persistance ou de services métier dans la bibliothèque ;
- utiliser les primitives HTML, ARIA, `InputBase<TValue>`, `EditContext`, les callbacks et l'annulation comme contrats ;
- garder les mécanismes de coordination entre composants internes à la RCL ;
- faire correspondre chaque famille logique à une responsabilité cohérente, même si les dossiers conservent le namespace public unique ;
- préserver les frontières de sécurité CSP et clean-room comme contraintes architecturales, pas comme options configurables.

## Graphe réel et cycles

| Module physique | Domaine et rôle | Dépendances projet | Jugement |
|---|---|---|---|
| `Repository` | gouvernance, documentation, scripts, CI, packaging | aucune dépendance compilée | Cohérent |
| `src/OmniEurope.Blazor` | paquet de composants, modèles publics, utilitaires internes et assets | aucune `ProjectReference`; paquet `Microsoft.AspNetCore.Components.Web` | Cohérent et racine du graphe |
| `tests/OmniEurope.Blazor.Tests` | tests bUnit, interactions, CSP et budgets | `OmniEurope.Blazor` | Direction correcte |
| `samples/OmniEurope.Blazor.Catalog` | catalogue Server et sonde CSP HTTP | `OmniEurope.Blazor` | Direction correcte |
| `samples/OmniEurope.Blazor.AutoSmoke.Client` | sonde Interactive Auto côté client | `OmniEurope.Blazor` | Direction correcte |
| `samples/OmniEurope.Blazor.AutoSmoke` | hôte Server de la sonde Interactive Auto | `AutoSmoke.Client` | Direction correcte et transitivement vers la RCL |
| `samples/OmniEurope.Blazor.WasmSmoke` | sonde WebAssembly | `OmniEurope.Blazor` | Direction correcte |
| `samples/OmniEurope.Blazor.HybridSmoke` | sonde de compilation MAUI Blazor Hybrid | `OmniEurope.Blazor` | Direction correcte ; exclusion volontaire de la solution générale, build Windows séparé en CI |

Résultat des cycles et inversions :

- **Projet vers projet : RAS.** Aucun cycle dans les 7 `.csproj`; aucune référence de la RCL vers un hôte ou les tests.
- **Couche interne : RAS.** `Components` utilise `Internal`; aucun fichier `Internal` n'importe `OmniEurope.Blazor.Components`.
- **Composition de composants : RAS sur les cycles observables.** Les relations parent-enfant sont orientées : Scheduler vers ses vues, Upload vers ProgressBar, FormField vers Label, et les agrégats Tree/Tabs/Steps/DataGrid vers leurs contextes.
- **Dépendance externe : RAS.** La RCL n'a qu'une dépendance Blazor cliente ; aucune dépendance Radzen n'est présente.

## Carte module vers domaine pour les passes suivantes

### Module `Library`

| Domaine logique | Composants et types principaux | Couplage autorisé |
|---|---|---|
| Fondations, contenu et disposition | `OmniAlert`, `OmniBadge`, `OmniBody`, `OmniCard`, `OmniColumn`, `OmniFieldset`, `OmniGrid`, `OmniHeader`, `OmniHeading`, `OmniIcon`, `OmniImage`, `OmniLabel`, `OmniLayout`, `OmniLink`, `OmniMain`, `OmniProgressBar`, `OmniRow`, `OmniSidebar`, `OmniSidebarToggle`, `OmniSkeleton`, `OmniStack`, `OmniText`, `OmniThemeScope`, `OmniAppearanceToggle` | bases communes, CSS statique, garde CSP |
| Actions | `OmniButton`, `OmniToggleButton`, `OmniSplitButton`, `OmniSplitButtonItem` | fondations seulement |
| Formulaires et validation | `OmniTextBox`, `OmniTextArea`, `OmniPassword`, `OmniNumeric`, `OmniCheckBox`, `OmniNullableCheckBox`, `OmniSwitch`, `OmniNullableSwitch`, `OmniFormField`, `OmniTemplateForm`, `OmniRequiredValidator`, `OmniLengthValidator`, `OmniEmailValidator`, `OmniCompareValidator` | `InputBase<T>`, `EditContext`, module JS statique |
| Sélection et transfert | `OmniAutocomplete`, `OmniCheckBoxList`, `OmniColorPicker`, `OmniDatePicker`, `OmniDropDown`, `OmniMultiSelect`, `OmniListBox`, `OmniRadioButtonList`, `OmniRadioButtonListItem`, `OmniSelectBar`, `OmniSelectBarItem`, `OmniSlider`, `OmniUpload`, `OmniOption<T>`, `OmniUploadRequest` | formulaires et ProgressBar |
| Navigation | `OmniBreadcrumb`, `OmniBreadcrumbItem`, `OmniPanelMenu`, `OmniPanelMenuItem`, `OmniProfileMenu`, `OmniProfileMenuItem`, `OmniTabs`, `OmniTabsItem`, `OmniSteps`, `OmniStepsItem` | `NavigationManager`, contextes en cascade internes |
| Collections | `OmniDataList`, `OmniPager`, `OmniTree`, `OmniTreeItem`, `OmniTreeLevel` | callbacks annulables et contexte d'arbre interne |
| DataGrid | `OmniDataGrid`, `OmniDataGridColumn`, modèles `OmniDataGrid*` | callbacks de chargement, modèles de colonnes et projection interne |
| Superpositions | `OmniComponentsHost`, `OmniOverlayService`, `OmniDialog`, `OmniNotification`, `OmniTooltip`, `OmniContextMenu` | portail/hôte unique, état contrôlé, restauration du focus |
| Graphiques et jauges | `OmniChart`, axes, légende, grilles, marqueurs, labels, séries ligne/aire/barre/colonne/secteur/empilées, `OmniArcGauge*`, `OmniChartPoint`, `OmniChartSlice` | contexte de coordonnées et géométrie internes |
| Temps et planification | `OmniTimeline`, `OmniTimelineItem`, `OmniScheduler`, `OmniDayView`, `OmniWeekView`, `OmniMonthView`, `OmniSchedulerAppointment` | `DateTimeOffset`, `TimeZoneInfo`, callbacks annulables |
| Édition riche | `OmniHtmlEditor`, `OmniHtmlEditorTool` | sanitiseur interne, `InputBase<T>` |
| Transversal interne | `OmniComponentBase`, `OmniInputBase<T>`, `OmniValidatorBase<T>`, `CssClassBuilder`, `CspAttributeGuard`, `OmniHtmlSanitizer` | ne dépend d'aucun domaine consommateur |

### Modules de vérification

- `Tests` couvre les domaines par suites dédiées : fondations, interactions/formulaires, sélection, navigation, collections, DataGrid, superpositions, graphiques, planification, éditeur HTML, CSP et budgets.
- `Catalog` illustre plusieurs domaines dans un hôte Server, sans logique métier partagée avec la RCL.
- `AutoSmokeClient`, `AutoSmoke`, `WasmSmoke` et `HybridSmoke` sont des adaptateurs de plateforme terminaux. Ils ne doivent fournir aucun modèle ou service réutilisé par la bibliothèque.
- `Repository` porte les générateurs, la baseline API, les gates de paquet et la documentation. Ses scripts peuvent lire la RCL, jamais devenir une dépendance d'exécution.

## Découpage en domaines

### Cohésion et couplage

- **Découpage physique : pertinent.** Les unités de projet correspondent à une unité de livraison et à des hôtes de preuve. Scinder la RCL par famille créerait plusieurs assemblages et versions sans consommateur indépendant observé.
- **Couplage inter-projets : faible.** Tous les chemins convergent vers la RCL, sans dépendance latérale entre catalogue, tests et sondes, sauf le couple Server/Client imposé par Interactive Auto.
- **Couplage interne : généralement explicite.** Les composants utilisent des callbacks, `EventCallback`, `RenderFragment` et des contextes parent-enfant plutôt que des singletons globaux ou une infrastructure métier.
- **Domaines non anémiques :** formulaires, sélection, collections et planification possèdent leurs modèles et comportements proches du rendu.
- **Domaines à frontière incomplète :** graphiques et superpositions ne disposent pas encore de la coordination centrale annoncée ; DataGrid concentre sa coordination dans le composant de rendu.
- **Taxonomie : faible maturité.** La feuille de route distingue 12 familles, `component-families.md` en agrège 8 et le catalogue affiche 10 familles, sans autorité canonique commune.

### SOLID

- **SRP :** respecté par les utilitaires internes et les petits composants ; écart notable pour `OmniDataGrid`.
- **OCP :** les templates et callbacks permettent les variations observées sans framework d'extensions dédié.
- **LSP :** les bases Blazor restent minces et ne modifient que les contrats communs d'attributs, formulaires et validation.
- **ISP :** la majorité des paramètres sont portés par le composant concerné ; les contextes publics de composition exposent toutefois une interface d'implémentation inutile aux consommateurs.
- **DIP :** les opérations distantes dépendent de délégués annulables fournis par l'hôte. Une hiérarchie de repositories ou providers serait injustifiée dans cette RCL.

## Écarts actionnables par sévérité

### Élevé

#### ARCH-01 - Le domaine graphique n'a pas de système de coordonnées partagé

`src/OmniEurope.Blazor/Components/OmniChart.razor`

[Élevé] [Architecture] `OmniChart` rend directement `ChildContent`, tandis que `OmniValueAxis` calcule ses graduations indépendamment et que chaque série passe par `OmniChartGeometry.Clamp` sur une échelle fixe 0-100. Les variantes empilées repartent aussi de la même ligne de base au lieu de partager un cumul. Les axes, séries et empilements sont donc des fragments juxtaposés, pas un domaine de graphique composé - lignes 4-18 ; preuves croisées `OmniChartTypes.cs:8-13`, `OmniValueAxis.razor:3-16`, `OmniStackedColumnSeries.razor:2-7`, `OmniStackedAreaSeries.razor:1-7` et cible `docs/component-roadmap.md:17,44` - recommandation : Codex peut introduire un contexte interne de graphique qui enregistre axes et séries, calcule une projection immuable (domaines, échelles, catégories, baselines positives/négatives), puis fait rendre les composants à partir de cette projection sans modifier l'API déclarative observée ; Codex ajoutera des tests SVG sur domaines décalés, valeurs négatives, empilements et données vides avant de fermer l'écart.

### Moyen

#### ARCH-02 - La frontière de portail des superpositions annoncée n'existe pas encore

`src/OmniEurope.Blazor/Components/OmniComponentsHost.razor`

[Moyen] [Architecture] L'hôte rend directement un unique dialogue remplaçable et la liste de notifications, alors que tooltip et menu contextuel restent rendus et contrôlés localement. La famille possède donc deux modèles de cycle de vie et aucune pile centrale pour ordre, imbrication, verrouillage du scroll ou arbitrage d'Escape. Cela dévie de la cible de « couche de portail contrôlée » - lignes 4-23 ; preuves croisées `OmniOverlayTypes.cs:21-60`, `OmniTooltip.razor:3-18`, `OmniContextMenu.razor:3-43`, `docs/component-roadmap.md:16` et `plans/PLAN-002-remplacement-radzen.md:56-62` - recommandation : Codex peut conserver `OmniComponentsHost` et `OmniOverlayService` comme façade publique, ajouter un coordinateur interne de portail avec identifiants et pile ordonnée, puis faire converger dialogue, notification et les popups qui exigent un portail vers ce cycle de vie ; des tests navigateur de focus, imbrication, Escape et restauration fermeront la frontière.

#### ARCH-03 - `OmniDataGrid` concentre rendu, projection et orchestration

`src/OmniEurope.Blazor/Components/OmniDataGrid.razor`

[Moyen] [Architecture] Le composant de 451 lignes possède à la fois le registre des colonnes, les filtres et tris locaux, la pagination, le chargement distant et son annulation, la sélection, l'expansion, l'édition, le regroupement, le redimensionnement et le rendu. Cette concentration enfreint le principe de responsabilité unique et rend les changements de projection difficiles à tester sans rendu bUnit - lignes 145-451 ; preuve documentaire `plans/PLAN-002-remplacement-radzen.md:122-136` - recommandation : Codex peut préserver la façade et les paramètres publics, extraire un moteur interne pur `GridProjection<TItem>` pour filtre/tri/groupe/page et un état interne de chargement annulable, puis tester ces unités indépendamment avant d'alléger le code de rendu.

#### ARCH-04 - Des mécanismes de composition internes ont fui dans l'API publique

`src/OmniEurope.Blazor/Components/OmniDataGridTypes.cs`

[Moyen] [Architecture] `OmniDataGridColumnDefinition<TItem>` et `OmniDataGridContext<TItem>` sont publics alors que les recherches vérifiées ne trouvent que les usages internes du couple DataGrid/Column. Le même défaut concerne `OmniTabsContext`, `OmniStepsContext` et `OmniTreeContext<TValue>`. Ces types de transport en cascade sont inscrits dans `docs/public-api.txt`, ce qui expose l'implémentation parent-enfant et augmente inutilement la surface de rupture - lignes 48-69 ; preuves croisées `OmniNavigationTypes.cs:5-15`, `OmniCollectionTypes.cs:3-7`, `docs/public-api.txt:503-506,535-539` - recommandation : Codex peut confirmer une dernière fois l'absence de consommateur dans l'inventaire de migration, rendre ces cinq types internes avant 1.0, régénérer la baseline publique et ajouter une garde sémantique qui empêche les types de coordination internes de redevenir publics.

### Faible

#### ARCH-05 - La taxonomie logique n'a pas d'autorité canonique

`docs/architecture.md`

[Faible] [Architecture] Le document d'architecture ne définit que les dossiers physiques. `docs/component-roadmap.md:9-20` distingue 12 familles, `docs/component-families.md:5-12` en agrège 8 et `samples/OmniEurope.Blazor.Catalog/Components/Pages/Home.razor:13` en annonce 10. Les 105 composants Razor restent dans un dossier et un namespace uniques, avec des fichiers attrape-tout tels que `OmniFoundationTypes.cs:3-108` et `OmniSelectionTypes.cs:5-24`. Le paquet unique est pertinent, mais l'absence de carte canonique affaiblit ownership, placement et couverture par domaine - lignes 3-15 - recommandation : Codex peut faire de `docs/architecture.md` l'autorité de la taxonomie, aligner les documents et le catalogue, puis ranger les sources et tests par dossiers logiques en conservant le projet, le namespace public et le paquet uniques.

## Proportionnalité et sur-ingénierie

`PROPORTIONNALITÉ: NOTICE` - La solution la plus simple qui préserve les exigences observées est bien une RCL unique, un projet de tests, un catalogue et quatre sondes de plateformes. Une séparation en projets Domain/Application/Infrastructure, un repository générique, une interface par composant ou un bus d'événements ajouterait assemblages, DI et versionnement sans second consommateur ni contrainte mesurée. Les callbacks annulables, les contextes locaux et les trois utilitaires internes suffisent actuellement. Les sondes multi-hôtes ne sont pas superflues : elles correspondent aux quatre cibles de compatibilité documentées.

`src/OmniEurope.Blazor/Components/OmniSchedulerTypes.cs`

[INFO] [Sur-ingénierie] Le champ public optionnel `RecurrenceRule` n'a aucun lecteur dans la RCL, les tests ou les échantillons et aucun usage de récurrence n'apparaît dans les contrats observés du dépôt. Cette flexibilité spéculative ajoute dès l'alpha un contrat de syntaxe, de validation, de fuseau et de versionnement sans comportement associé ; l'alternative plus simple est de retirer le champ avant 1.0 et de ne l'ajouter qu'avec un premier cas d'usage et son moteur d'expansion - ligne 16 - **notification consultative, non actionnable, exclue des constats, des compteurs et du verdict**.

## Verdict des passes 1 et 2b

- Architecture physique et direction des dépendances : **saine**.
- Cycles de projets et inversions observées : **aucun**.
- Pertinence du paquet et du nombre de projets : **proportionnée**.
- Découpage logique interne : **à renforcer** sur graphiques, superpositions, DataGrid et encapsulation des contextes.
- Verdict architecture : **NEEDS ATTENTION**, motivé par 5 écarts actionnables, sans finding critique.

## Compteurs

| Sévérité | Nombre actionnable |
|---|---:|
| Critique | 0 |
| Élevé | 1 |
| Moyen | 3 |
| Faible | 1 |
| **Total** | **5** |

`[INFO] [Sur-ingénierie]` : 1 notification non actionnable, hors total.
