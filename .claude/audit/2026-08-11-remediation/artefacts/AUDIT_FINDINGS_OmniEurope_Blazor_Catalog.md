# Findings d'audit - OmniEurope.Blazor.Catalog

Date : 2026-08-11  
Mode : Full  
Périmètre : 18 fichiers du catalogue Server  
Verdict du module : **3 findings actionnables** - 0 Critique, 1 Élevé, 1 Moyen, 1 Faible. **INFO : 1**.

## Méthode et preuves transversales

Les 18 fichiers du registre ont été lus intégralement. Les artefacts Architecture, Kit, Dépendances et tous les rapports métriques ont été relus ou vérifiés inchangés avant l'analyse. Le runtime Catalog est vert dans le préflight : HTTP, composants interactifs, focus, notification, console et CSP navigateur sont validés. Le contrôle source confirme une CSP sans `unsafe-inline` ni `unsafe-eval`, aucun style inline, 71 clés françaises et 71 clés anglaises identiques, et aucune référence ou mention Radzen. Semgrep et gitleaks n'étant pas disponibles, la sécurité reste best-effort sur ces axes.

Les findings globaux de la RCL et des dépendances ne sont pas dupliqués ici. Les fichiers concernés les référencent seulement dans leur bloc individuel.

## Findings actionnables

<a id="cat-001"></a>
### CAT-001 - [Élevé] [Internationalisation / Accessibilité] La ressource anglaise n'est pas négociée et la langue du document reste figée en français

**Preuves :** `Components/App.razor:2` fixe `<html lang="fr">`. `Program.cs:8` enregistre indirectement la localisation, mais ne configure ni cultures supportées ni `UseRequestLocalization`. Le catalogue fournit pourtant `CatalogStrings.en.resx`. Le smoke runtime ne transmet ni ne vérifie de culture anglaise.

**Impact :** un client anglophone ne peut pas sélectionner normalement la ressource anglaise par la requête; si la culture UI est imposée autrement, le contenu anglais reste annoncé aux technologies d'assistance comme français.

**Remédiation :** configurer `RequestLocalizationOptions` pour fr/en, activer le middleware avant les endpoints, dériver l'attribut `lang` de `CurrentUICulture`, puis tester une requête `Accept-Language: en` et une française.

<a id="cat-002"></a>
### CAT-002 - [Moyen] [Fiabilité / Cycle de vie] Le service de superpositions créé par Home n'est jamais disposé

**Preuves :** `Components/Pages/Home.razor.cs:12` crée `Overlays = new()`. `OmniOverlayService` implémente `IDisposable` et libère notamment les annulations de notifications; `OmniComponentsHost` ne dispose pas un service fourni par paramètre. `Home` n'implémente aucun contrat de disposition.

**Impact :** les ressources et callbacks temporisés du service peuvent survivre à la page ou au circuit, avec rétention et réveils tardifs après navigation ou destruction.

**Remédiation :** faire implémenter `IDisposable` à la page, disposer le service possédé et ajouter un test de navigation/destruction avec notification active.

<a id="cat-003"></a>
### CAT-003 - [Faible] [Authenticité documentaire] Le héros affirme à tort qu'aucun projet consommateur n'est utilisé

**Preuves :** `Home.razor:9` compose la phrase avec `HeroAfter`; les ressources fr/en affirment respectivement « et aucun projet consommateur » et « and no consumer project ». Or `OmniEurope.Blazor.Catalog.csproj:7` référence directement la RCL et `App.razor:8` charge aussi `app.css` propre au consommateur.

**Impact :** la documentation exécutable donne une description factuellement contradictoire de son architecture et peut faire croire que le rendu ne dépend d'aucun hôte ou style consommateur.

**Remédiation :** reformuler précisément la propriété réellement démontrée, par exemple l'absence de styles inline et de contournement CSP, sans nier l'existence du projet Catalog.

## Proportionnalité et sur-ingénierie

`PROPORTIONALITY: NONE` - Le catalogue Server, son collecteur CSP borné et son smoke navigateur sont proportionnés à une documentation exécutable. Les corrections actionnables restent locales et n'exigent aucune nouvelle couche ni dépendance.

<a id="cat-i01"></a>
### CAT-I01 - [INFO] [Sur-ingénierie] Deux rendus complets de page introuvable sont maintenus

`Components/Pages/NotFound.razor` possède une route catch-all qui absorbe toute URL inconnue, tandis que `Components/Routes.razor` maintient en plus un fragment `Router.NotFound` équivalent. Le second chemin est redondant dans la table de routes actuelle; une seule vue partagée ou un seul mécanisme suffit. **Notification consultative, non actionnable, exclue des findings et du verdict.**

## Contrôles fichier par fichier

<a id="samples-omnieurope-blazor-catalog-components-imports-razor"></a>
### `samples/OmniEurope.Blazor.Catalog/Components/_Imports.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-catalog-components-app-razor"></a>
### `samples/OmniEurope.Blazor.Catalog/Components/App.razor`

Finding(s) : [CAT-001](#cat-001).

<a id="samples-omnieurope-blazor-catalog-components-layout-mainlayout-razor"></a>
### `samples/OmniEurope.Blazor.Catalog/Components/Layout/MainLayout.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-catalog-components-layout-mainlayout-razor-cs"></a>
### `samples/OmniEurope.Blazor.Catalog/Components/Layout/MainLayout.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-catalog-components-pages-home-razor"></a>
### `samples/OmniEurope.Blazor.Catalog/Components/Pages/Home.razor`

Finding(s) : [CAT-003](#cat-003).

Référence inter-module sans duplication : [OE-BLAZOR-001](AUDIT_FINDINGS_OmniEurope_Blazor.md#oe-blazor-001) pour le libellé par défaut de `OmniTabs`.

<a id="samples-omnieurope-blazor-catalog-components-pages-home-razor-cs"></a>
### `samples/OmniEurope.Blazor.Catalog/Components/Pages/Home.razor.cs`

Finding(s) : [CAT-002](#cat-002).

<a id="samples-omnieurope-blazor-catalog-components-pages-notfound-razor"></a>
### `samples/OmniEurope.Blazor.Catalog/Components/Pages/NotFound.razor`

Finding(s) : [CAT-I01](#cat-i01).

<a id="samples-omnieurope-blazor-catalog-components-pages-notfound-razor-cs"></a>
### `samples/OmniEurope.Blazor.Catalog/Components/Pages/NotFound.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-catalog-components-routes-razor"></a>
### `samples/OmniEurope.Blazor.Catalog/Components/Routes.razor`

Finding(s) : [CAT-I01](#cat-i01).

<a id="samples-omnieurope-blazor-catalog-cspviolationstore-cs"></a>
### `samples/OmniEurope.Blazor.Catalog/CspViolationStore.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-catalog-globalusings-cs"></a>
### `samples/OmniEurope.Blazor.Catalog/GlobalUsings.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-catalog-omnieurope-blazor-catalog-csproj"></a>
### `samples/OmniEurope.Blazor.Catalog/OmniEurope.Blazor.Catalog.csproj`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-catalog-packages-lock-json"></a>
### `samples/OmniEurope.Blazor.Catalog/packages.lock.json`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

Référence inter-passes sans duplication : [DEP-002](AUDIT_DEPENDENCIES.md#dep-002---moyen-dépendances--sécurité-le-sanitizer-de-production-est-obsolète-et-absent-de-la-politique).

<a id="samples-omnieurope-blazor-catalog-program-cs"></a>
### `samples/OmniEurope.Blazor.Catalog/Program.cs`

Finding(s) : [CAT-001](#cat-001).

<a id="samples-omnieurope-blazor-catalog-resources-catalogstrings-cs"></a>
### `samples/OmniEurope.Blazor.Catalog/Resources/CatalogStrings.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-catalog-resources-catalogstrings-en-resx"></a>
### `samples/OmniEurope.Blazor.Catalog/Resources/CatalogStrings.en.resx`

Finding(s) : [CAT-001](#cat-001), [CAT-003](#cat-003).

<a id="samples-omnieurope-blazor-catalog-resources-catalogstrings-resx"></a>
### `samples/OmniEurope.Blazor.Catalog/Resources/CatalogStrings.resx`

Finding(s) : [CAT-003](#cat-003).

<a id="samples-omnieurope-blazor-catalog-wwwroot-app-css"></a>
### `samples/OmniEurope.Blazor.Catalog/wwwroot/app.css`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

## Totaux

- Critique : 0
- Élevé : 1
- Moyen : 1
- Faible : 1
- INFO, consultatif et exclu du verdict : 1
- Fichiers audités : 18/18
