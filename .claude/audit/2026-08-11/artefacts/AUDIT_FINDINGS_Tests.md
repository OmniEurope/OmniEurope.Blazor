# Findings d'audit 360 - Tests

> Audit: 2026-08-11
> Les blocs sont ajoutés fichier par fichier. Une absence de finding est consignée explicitement par `RAS`.

<a id="tests/OmniEurope.Blazor.Tests/_Imports.razor"></a>
## `tests/OmniEurope.Blazor.Tests/_Imports.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/ChartComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/ChartComponentTests.cs`

[Élevé] [Tests] La suite graphique n'asserte aucune coordonnée SVG, projection de domaine, valeur négative ni cumul empilé; elle se limite aux rôles, textes et nombres de marqueurs/chemins, de sorte que l'échelle fixe 0-100 et les empilements sans cumul décrits par ARCH-01 peuvent rester verts - lignes 7-28 - source : `AUDIT_ARCHITECTURE.md` ARCH-01 et recherche transversale vérifiée dans `ChartComponentTests.cs`/`ChartTestHost.razor`; couverture outillée indisponible - recommandation : Codex ajoutera des assertions géométriques sur domaines décalés, valeurs négatives, séries empilées positives/négatives et données vides, à partir d'une projection attendue indépendante du rendu.

[Moyen] [Tests] `ArcGauge_ClampsAndAnnouncesItsValue` annonce une vérification du bornage, mais ne fournit aucun cas hors limites et n'asserte qu'une valeur nominale `75` ainsi que le préfixe syntaxique `M ` du chemin SVG; une régression supprimant le clamp pourrait donc rester verte - lignes 21-27 - source : lecture intégrale, couverture outillée indisponible - recommandation : Codex ajoutera des rendus avec valeurs sous le minimum et au-dessus du maximum, puis vérifiera la valeur annoncée et la géométrie normalisée attendue.

<a id="tests/OmniEurope.Blazor.Tests/ChartTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/ChartTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/CollectionComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/CollectionComponentTests.cs`

[Faible] [Tests] `PagerAndTree_AreControlledAndKeyboardOperable` couvre dans une seule méthode deux composants indépendants, un changement de page, deux sélections d'arbre et l'ARIA; cette agrégation brouille l'Arrange/Act/Assert et rend l'échec moins localisable - lignes 45-64 - source : critère additionnel « un comportement par test » - recommandation : Codex séparera le scénario Pager des scénarios Tree souris, clavier et accessibilité, avec un acte principal et des assertions cohérentes par test.

<a id="tests/OmniEurope.Blazor.Tests/ComponentCspTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/ComponentCspTests.cs`

RAS

<a id="tests/OmniEurope.Blazor.Tests/DataGridComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/DataGridComponentTests.cs`

[Moyen] [Tests] `DataGrid_SortsFiltersPagesAndSelectsByStableKey` affirme couvrir la pagination, mais n'effectue aucune action de page ni assertion sur l'index ou les lignes de la page suivante; elle agrège en outre tri, sélection et filtrage dans le même scénario - lignes 8-24 - source : lecture intégrale, couverture outillée indisponible - recommandation : Codex isolera un test de pagination qui change effectivement de page et vérifie le sous-ensemble attendu, puis séparera les autres comportements pour rendre chaque branche falsifiable.

[Moyen] [Tests] `DataGrid_UsesCancelableRemoteRequestsAndTotalCount` ne provoque aucun chevauchement de requêtes et vérifie seulement que le jeton initial n'est pas déjà annulé; elle ne prouve donc pas que la requête précédente est annulée lors d'un rechargement - lignes 27-42 - source : lecture intégrale - recommandation : Codex pilotera deux chargements contrôlés, capturera leurs jetons, déclenchera le second avant la fin du premier et vérifiera l'annulation du premier ainsi que la conservation du résultat le plus récent.

[Moyen] [Tests] Aucun scénario DataGrid ne couvre regroupement, expansion, édition ou redimensionnement, alors que ces responsabilités publiques sont concentrées dans `OmniDataGrid`; une régression de ces branches ne serait détectée par aucune assertion observable du module - lignes 8-71 - source : `AUDIT_ARCHITECTURE.md` ARCH-03 et recherche transversale sans référence à ces comportements dans la suite/fixture; couverture outillée indisponible - recommandation : Codex ajoutera des scénarios isolés et falsifiables pour chaque branche, y compris les erreurs et transitions d'état.


<a id="tests/OmniEurope.Blazor.Tests/DataGridTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/DataGridTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/FormTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/FormTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/FoundationComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/FoundationComponentTests.cs`

[Faible] [Tests] Quatre tests batchent respectivement 15 composants de fondation, quatre composants de contenu, cinq composants de layout et quatre composants de feedback; un échec ne désigne donc pas directement le composant ou le comportement fautif et plusieurs actes indépendants partagent une seule méthode - lignes 9-137 - source : critère additionnel « un comportement par test » - recommandation : Codex convertira les matrices homogènes en théories nommées par composant et isolera les comportements sémantiques ou les validations qui n'ont pas la même cause d'échec.

<a id="tests/OmniEurope.Blazor.Tests/HtmlEditorComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/HtmlEditorComponentTests.cs`

RAS

<a id="tests/OmniEurope.Blazor.Tests/HtmlEditorTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/HtmlEditorTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/InteractionComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/InteractionComponentTests.cs`

[Moyen] [Fiabilité] `DelayedValidator_CancelsStaleFieldValidation` dépend d'une attente murale fixe de 100 ms pour dépasser un délai de validation de 20 ms; sous charge ou ordonnanceur lent, le test peut observer un état intermédiaire et devenir intermittent - lignes 126-139 - source : lecture intégrale, critère de déterminisme - recommandation : Codex exposera ou injectera une horloge/temporisation contrôlable, ou attendra l'état fonctionnel avec une borne explicite plutôt qu'un `Task.Delay` arbitraire.

<a id="tests/OmniEurope.Blazor.Tests/LargeSelectionTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/LargeSelectionTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/NavigationComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/NavigationComponentTests.cs`

RAS

<a id="tests/OmniEurope.Blazor.Tests/NavigationTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/NavigationTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/OmniEurope.Blazor.Tests.csproj"></a>
## `tests/OmniEurope.Blazor.Tests/OmniEurope.Blazor.Tests.csproj`

[Moyen] [Fiabilité] Le projet de tests ne référence aucun collecteur compatible avec la commande `--collect:"XPlat Code Coverage"`; le préflight réussit 57 tests mais échoue à produire la couverture, rendant couverture et CRAP non fiables - lignes 7-12 - source : `metrics/PREFLIGHT.md` et `AUDIT_DEPENDENCIES.md` D-002 - recommandation : Codex alignera la commande et le package de couverture sur une chaîne unique épinglée, puis exigera la production vérifiée d'un fichier de couverture exploitable.

<a id="tests/OmniEurope.Blazor.Tests/OverlayComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/OverlayComponentTests.cs`

[Moyen] [Tests] Les scénarios de superposition ne couvrent que l'ouverture/fermeture nominale d'un dialogue et d'une notification; aucune assertion ne protège l'ordre ou l'imbrication, l'arbitrage d'Escape, le verrouillage du scroll ni la restauration du focus, alors que ces comportements constituent la frontière de portail attendue - lignes 37-112 - source : `AUDIT_ARCHITECTURE.md` ARCH-02, couverture outillée indisponible - recommandation : après convergence vers un coordinateur de portail, Codex ajoutera des tests navigateur falsifiables sur pile imbriquée, Escape, focus initial/restauré et scroll.

<a id="tests/OmniEurope.Blazor.Tests/packages.lock.json"></a>
## `tests/OmniEurope.Blazor.Tests/packages.lock.json`

[Faible] [Dépendances] Le verrou conserve `bunit` 2.8.6 alors que 2.9.0 est disponible, avec notamment `AngleSharp.Css` en version beta et Microsoft Testing Platform 1.9.1 dans le graphe résolu - lignes 5-23, 55-70 et 339-367 - source : `AUDIT_DEPENDENCIES.md` D-004 et scan NuGet `--outdated` - recommandation : Codex mettra bUnit à niveau dans une branche isolée, régénérera le verrou et revalidera les tests CSP, interactions et budgets avant d'accepter les transitifs.

<a id="tests/OmniEurope.Blazor.Tests/PerformanceBudgetTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/PerformanceBudgetTests.cs`

[Moyen] [Fiabilité] Les trois budgets reposent sur un seul chronométrage sans warm-up ni isolation de la concurrence, et mesurent les allocations du seul thread courant; ils sont sensibles à la charge de la machine et peuvent ignorer des allocations effectuées ailleurs tout en donnant une impression de gate de performance fiable - lignes 9-57 - source : lecture intégrale, aucune métrique/complexité outillée disponible - recommandation : Codex séparera un benchmark reproductible avec warm-up et statistiques d'une garde CI calibrée sur un environnement contrôlé, et utilisera une mesure d'allocations adaptée à tout le processus ou au code réellement exercé.

<a id="tests/OmniEurope.Blazor.Tests/SchedulerComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/SchedulerComponentTests.cs`

[Moyen] [Tests] `Scheduler_LoadsTheVisibleRangeWithCancellation` vérifie seulement que le jeton du chargement initial n'est pas annulé; aucun rechargement concurrent ne prouve l'annulation de la requête précédente - lignes 49-70 - source : lecture intégrale - recommandation : Codex capturera les jetons de deux chargements chevauchés et assertera l'annulation du premier après le déclenchement du second.

[Moyen] [Tests] `Scheduler_PreservesOverlapsAcrossADaylightSavingBoundary` ne vérifie que la présence de deux libellés et le nombre d'éléments; le test resterait vert si le fuseau ou la transition DST étaient ignorés et si les positions/durées étaient erronées - lignes 106-125 - source : lecture intégrale, couverture outillée indisponible - recommandation : Codex assertera les instants locaux projetés, les durées et les positions/relations de chevauchement attendues autour du saut horaire, avec un cas ambigu d'automne en complément.

<a id="tests/OmniEurope.Blazor.Tests/SelectionComponentTests.cs"></a>
## `tests/OmniEurope.Blazor.Tests/SelectionComponentTests.cs`

[Moyen] [Tests] `Autocomplete_DebouncesAnnouncesAndSelectsAResult` n'émet qu'une seule saisie avant d'attendre le résultat; le test ne peut donc pas prouver le debounce annoncé ni la suppression d'une requête intermédiaire dans la fenêtre configurée - lignes 53-66 - source : lecture intégrale - recommandation : Codex pilotera plusieurs saisies rapprochées avec une temporisation contrôlée et assertera qu'une seule recherche, portant le dernier terme, est lancée après la fenêtre.

[Faible] [Tests] `LargeSelector_ReloadsAndSelectsTheLastTypedValue` ne déclenche aucun rechargement ou rerendu: il vérifie seulement le nombre initial d'options et un changement de sélection - lignes 123-132 - source : lecture intégrale - recommandation : Codex renommera le test selon ce qu'il prouve ou ajoutera un vrai changement de source suivi d'un rerendu et de l'assertion sur la dernière valeur conservée.

<a id="tests/OmniEurope.Blazor.Tests/SelectionTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/SelectionTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/TemplateFormTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/TemplateFormTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/TreeTestHost.razor"></a>
## `tests/OmniEurope.Blazor.Tests/TreeTestHost.razor`

RAS

<a id="tests/OmniEurope.Blazor.Tests/Usings.cs"></a>
## `tests/OmniEurope.Blazor.Tests/Usings.cs`

RAS
