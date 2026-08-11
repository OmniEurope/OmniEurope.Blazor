# Findings d'audit 360 - OmniEurope.Blazor.Tests

> Audit de remédiation : 2026-08-11  
> Mode : Full, 34 fichiers lus intégralement  
> Preuves transversales : 181/181 tests réussis, aucun test ignoré; couverture Cobertura exploitable de 86,64 % des lignes et 67,03 % des branches. Complexité et CRAP non fiables faute d'outil autorisé; SAST transversal et scan de secrets non fiables selon `metrics/PREFLIGHT.md` et `metrics/SECURITY_SCAN.md`.
>
> Total propre à ce module : **11 findings** - Critique 0, Élevé 3, Moyen 6, Faible 2. Les findings globaux `KIT-006`, `KIT-007`, `KIT-008` et les findings de dépendances ne sont pas dupliqués ici.

<a id="tests/OmniEurope.Blazor.Tests/_Imports.razor"></a>
## `tests/OmniEurope.Blazor.Tests/_Imports.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/ChartComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/ChartComponentTests.cs`

[Moyen] [Tests] La suite graphique et ses deux hôtes n'instancient jamais `OmniBarSeries`, `OmniColumnSeries`, `OmniStackedAreaSeries` ni `OmniBarOptions`; Cobertura confirme 0 ligne couverte et 0 branche couverte pour leurs fichiers `.razor` et `.razor.cs`. Le nom général `Charts_RenderSemanticSvgSeriesAndAccessibleAlternative` ne protège donc pas ces quatre composants publics, dont le rendu et les options peuvent régresser avec 181 tests verts - lignes 10-20 - source : `metrics/coverage.cobertura.xml`, lecture intégrale de `ChartComponentTests.cs`, `ChartTestHost.razor` et `ChartProjectionTestHost.razor` - recommandation : Codex ajoutera des scénarios falsifiables pour chaque série absente, avec géométrie, domaine, données vides, valeurs négatives et sémantique accessible attendus.

<a id="tests/OmniEurope.Blazor.Tests/ChartProjectionTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/ChartProjectionTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/ChartTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/ChartTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/CollectionComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/CollectionComponentTests.cs`

RAS

<a id="tests/OmniEurope.Blazor.Tests/ComponentCspTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/ComponentCspTests.cs`

RAS

<a id="tests/OmniEurope.Blazor.Tests/ConventionGuardTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/ConventionGuardTests.cs`

[Élevé] [Authenticité] Plusieurs tests présentés comme des gates sémantiques ne vérifient que la présence de chaînes ou des listes fermées : les icônes des actions Auto/Hybrid/WASM peuvent être sans relation avec l'action identifiée car seule la présence de n'importe quel `<OmniIcon` dans le fichier est testée (lignes 37-51); `AuthenticityGates_ExerciseBrowserPayloadSelectionAndCurrentHybridDependencies` n'exécute aucun probe et passe sur de simples noms de fonctions/messages (lignes 307-330); les contrôles de localisation ne balayent que des fichiers et littéraux préénumérés (lignes 374-518). Ces gates peuvent rester vertes si le comportement réel est supprimé, commenté ou déplacé - source : lecture intégrale et critère anti-fake; les faiblesses spécifiques de `ButtonConventionGuardTests`, `DialogConventionGuardTests` et `NavActiveStateGuardTests` sont déjà comptées globalement sous `KIT-006`, `KIT-007`, `KIT-008` - recommandation : Codex remplacera les assertions de présence par des scanners structurés exhaustifs et des fixtures négatives qui démontrent le rejet, et réservera les noms `Exercise*` aux tests qui exécutent réellement le script ou le comportement ciblé.

<a id="tests/OmniEurope.Blazor.Tests/DataGridAdvancedTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/DataGridAdvancedTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/DataGridComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/DataGridComponentTests.cs`

[Moyen] [Authenticité] `DataGrid_IgnoresAnOlderRemoteRequestThatCompletesLast` termine par `Assert.Single` sans vérifier que la ligne conservée vaut `2`; l'état initial `[0]` et le résultat récent `[2]` ont tous deux une seule ligne, donc le test reste vert si aucun des deux rechargements n'est appliqué - lignes 153-184 - source : lecture intégrale - recommandation : Codex assertera le contenu et la clé de la ligne récente, l'absence des valeurs initiale et obsolète, ainsi que l'état final du compteur.

[Moyen] [Tests] Aucun scénario ne fait échouer le loader DataGrid et ne prouve l'état d'erreur, la possibilité de relance ni l'absence de fuite de détails; Cobertura confirme que la branche `generation == _generation` du `catch (Exception)` dans `GridRemoteState.LoadAsync` est à 0 % - lignes 110-184 et 220-234 - source : `metrics/coverage.cobertura.xml` (`Internal/GridRemoteState.cs:40`) - recommandation : Codex ajoutera un chargement fautif contrôlé, vérifiera l'erreur localisée et récupérable, puis un retry réussi; il couvrira aussi l'exception obsolète qui ne doit pas écraser le dernier état.

<a id="tests/OmniEurope.Blazor.Tests/DataGridTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/DataGridTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/FormTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/FormTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/FoundationComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/FoundationComponentTests.cs`

RAS

<a id="tests/OmniEurope.Blazor.Tests/HtmlEditorComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/HtmlEditorComponentTests.cs`

[Élevé] [Authenticité] `Editor_AppliesCommandsAndSupportsDeterministicUndoRedo` configure le mock `wrapTextSelection` pour renvoyer directement le HTML déjà mis en gras, puis affirme que le composant adopte cette même valeur; aucun test JavaScript ou navigateur n'exécute `omniInterop.js`, et le seul autre garde vérifie les noms de fonctions comme texte. Une implémentation JavaScript cassée ou constante laisse donc la commande d'édition verte - lignes 37-59 - source : lecture intégrale et recherche transversale de `wrapTextSelection`/`restoreTextSelection` - recommandation : Codex conservera ce test pour l'historique C#, mais ajoutera une preuve navigateur avec une vraie sélection, vérifiera le DOM produit, la restauration de sélection et les commandes sans sélection, puis renommera le test bUnit selon le seul contrat mocké qu'il prouve.

<a id="tests/OmniEurope.Blazor.Tests/HtmlEditorTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/HtmlEditorTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/InteractionComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/InteractionComponentTests.cs`

[Moyen] [Authenticité] `TemplateForm_ContainsExpectedInteropFailuresDuringInvalidSubmission` injecte une exception dans `focusFirstInvalid`, mais n'asserte ni que l'appel a eu lieu, ni que cette exception précise a été contenue; le seul message de validation attendu apparaît aussi lorsque l'interop n'est jamais invoqué. Le test peut donc passer si la fonctionnalité de focus est silencieusement supprimée - lignes 217-227 - source : lecture intégrale - recommandation : Codex vérifiera exactement une invocation du module, l'absence d'exception de rendu, le maintien du focus contractuel observable et la validation, avec un contrôle négatif où une exception inattendue reste détectable.

<a id="tests/OmniEurope.Blazor.Tests/LargeSelectionTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/LargeSelectionTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/LocalizationTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/LocalizationTests.cs`

[Faible] [Style] Le fichier contient deux classes publiques de test de premier niveau, `LocalizationTests` et `LocalizedComponentTests`, contrairement à la règle _Generic « One class per file - no exceptions »; cela brouille la correspondance fichier/responsabilité et la granularité des audits - lignes 11 et 44 - source : `C:\Dev\_Generic\docs\coding-standards.md:10` - recommandation : Codex déplacera `LocalizedComponentTests` dans son propre fichier sans modifier les scénarios.

<a id="tests/OmniEurope.Blazor.Tests/NavigationComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/NavigationComponentTests.cs`

RAS

<a id="tests/OmniEurope.Blazor.Tests/NavigationTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/NavigationTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/OmniEurope.Blazor.Tests.csproj"></a>
## `tests/OmniEurope.Blazor.Tests/OmniEurope.Blazor.Tests.csproj`

[Élevé] [Authenticité] Le projet câble bien `coverlet.collector`, mais aucune politique de seuil n'est associée à la collecte : la gate appelée par la CI accepte tout `line-rate` strictement supérieur à zéro et seulement 57 tests alors que la preuve courante en exécute 181. La suppression de 124 tests ou une chute de couverture de 86,64 % à une valeur quasi nulle pourrait donc rester verte - lignes 7-13 - source croisée : `eng/Test-Coverage.ps1:4,32-40`, `.github/workflows/ci.yml:42-46`, `metrics/tests.trx` et `metrics/coverage.cobertura.xml` - recommandation : Codex fixera des planchers explicites et révisables pour le nombre de tests, les lignes et les branches, avec une fixture prouvant qu'une régression sous chaque seuil échoue; les écarts de versions/licences restent exclusivement dans `AUDIT_DEPENDENCIES.md`.

<a id="tests/OmniEurope.Blazor.Tests/OverlayComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/OverlayComponentTests.cs`

[Moyen] [Authenticité] `MenuAndDialog_DelegateFocusMovementAndRestorationToTheStaticModule` n'asserte jamais `restoreFocus` et ne déclenche aucune fermeture; il vérifie seulement `activateMenu`, `moveMenuFocus` et `activateDialog`. Cobertura montre en outre `FocusBoundaryAsync` à 0 ligne couverte. Le nom annonce donc une restauration et une boucle de focus que le test ne prouve pas - lignes 223-246 - source : lecture intégrale et `metrics/coverage.cobertura.xml` (`OmniDialog.razor.cs`, `FocusBoundaryAsync`) - recommandation : Codex fermera menu et dialogue, vérifiera l'invocation de restauration avec la bonne clé, déclenchera les deux sentinelles et assertera les deux valeurs du paramètre `last`.

[Moyen] [Tests] `ToggleAndSplitButtons_ExposeControlledStatesAndKeyboardMenu` configure un `OnClick` sur `OmniSplitButtonItem` mais ne clique jamais l'item; Cobertura confirme 0 ligne couverte pour `HandleClickAsync`, y compris la branche `Disabled`. Le callback public et son interdiction en état désactivé peuvent régresser sans échec - lignes 15-40 - source : `metrics/coverage.cobertura.xml` (`OmniSplitButtonItem.razor.cs:14-19`) - recommandation : Codex ajoutera des scénarios séparés qui activent un item, vérifient la fermeture/commande associée, puis prouvent qu'un item désactivé n'invoque pas son callback.

<a id="tests/OmniEurope.Blazor.Tests/OverlayPortalTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/OverlayPortalTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/packages.lock.json"></a>
## `tests/OmniEurope.Blazor.Tests/packages.lock.json`

RAS - les écarts de versions, transitifs, licences et SBOM sont centralisés dans `AUDIT_DEPENDENCIES.md` et ne sont pas dupliqués dans ce shard.

<a id="tests/OmniEurope.Blazor.Tests/PerformanceBudgetTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/PerformanceBudgetTests.cs`

[Faible] [Style] Le fichier contient deux classes de premier niveau, `PerformanceCollection` et `PerformanceBudgetTests`, malgré la règle _Generic « One class per file - no exceptions » - lignes 7-11 - source : `C:\Dev\_Generic\docs\coding-standards.md:10` - recommandation : Codex déplacera la définition de collection dans `PerformanceCollection.cs` sans modifier la sérialisation, l'échauffement ni les budgets documentés.

<a id="tests/OmniEurope.Blazor.Tests/PublicApiBoundaryTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/PublicApiBoundaryTests.cs`

RAS

<a id="tests/OmniEurope.Blazor.Tests/SchedulerComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/SchedulerComponentTests.cs`

RAS

<a id="tests/OmniEurope.Blazor.Tests/SelectionComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/SelectionComponentTests.cs`

RAS

<a id="tests/OmniEurope.Blazor.Tests/SelectionTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/SelectionTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/TemplateFormTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/TemplateFormTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/TreeControlledTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/TreeControlledTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/TreeTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/TreeTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/Usings.cs"></a>
## `tests/OmniEurope.Blazor.Tests/Usings.cs`

RAS

<a id="tests/OmniEurope.Blazor.Tests/ValidatorFailureTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/ValidatorFailureTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/ValidatorLifecycleTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/ValidatorLifecycleTestHost.razor`

RAS

## Proportionnalité et sur-ingénierie

`PROPORTIONALITY: NONE` - Les tests bUnit, hôtes Razor de fixture, mocks JS ciblés, mesures médianes et preuves navigateur externes sont proportionnés à une RCL multi-hôte. L'alternative plus simple qui conserve la sécurité consiste à rendre les gates existantes falsifiables et à compléter les quelques branches publiques absentes, sans introduire un framework de test supplémentaire ni dupliquer tous les scénarios dans chaque hôte. Cette appréciation est consultative et ne modifie ni les findings ni leur sévérité.
