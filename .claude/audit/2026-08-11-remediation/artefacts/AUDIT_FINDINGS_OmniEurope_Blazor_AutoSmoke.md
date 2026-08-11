# Findings d'audit - OmniEurope.Blazor.AutoSmoke

Date : 2026-08-11  
Mode : Full  
Périmètre : 6 fichiers de l'hôte Interactive Auto  
Verdict du module : **2 findings actionnables** - 0 Critique, 1 Élevé, 1 Moyen, 0 Faible. **INFO : 0**.

## Méthode et preuves transversales

Les 6 fichiers ont été lus intégralement après vérification des artefacts globaux inchangés. Le préflight prouve publication, prérendu, hydratation Interactive Auto, clic navigateur, console propre, assets et CSP stricte. Les en-têtes `nosniff`, referrer et permissions sont présents; la politique autorise précisément `wasm-unsafe-eval` sans `unsafe-inline` ni `unsafe-eval`. Aucun style inline, gestionnaire HTML inline, secret, ressource distante, dépendance ou marqueur Radzen n'est présent. Le graphe de référence restant est traité par ARCH-R-03 et n'est pas dupliqué. Semgrep et gitleaks étant indisponibles, la sécurité reste best-effort sur ces axes.

## Findings actionnables

<a id="auto-001"></a>
### AUTO-001 - [Élevé] [Internationalisation / Accessibilité] L'hôte ne négocie pas la culture et annonce toujours le document en français

**Preuves :** `Components/App.razor:5` fixe `<html lang="fr">`. `Program.cs:7` enregistre la localisation via `AddOmniEuropeBlazor`, mais ne configure ni cultures supportées ni `UseRequestLocalization`. Le composant client possède pourtant des ressources fr/en et s'exécute d'abord côté serveur puis côté WebAssembly.

**Impact :** `Accept-Language: en` ne sélectionne pas normalement la ressource anglaise au prérendu; si le runtime WebAssembly adopte ensuite une autre culture, la langue peut diverger après hydratation tandis que les technologies d'assistance continuent d'annoncer du français.

**Remédiation :** configurer la négociation fr/en côté Server, conserver la culture choisie pendant l'hydratation Auto, dériver `lang` de la culture UI et tester le prérendu puis le clic dans les deux cultures.

<a id="auto-002"></a>
### AUTO-002 - [Moyen] [Accessibilité] Le document Interactive Auto n'a aucun titre

**Preuves :** `Components/App.razor:6-14` ne contient aucun élément `title`; `AutoProbe` ne rend aucun `PageTitle`, bien que `HeadOutlet` soit présent.

**Impact :** l'onglet et le nom accessible de la page restent vides ou réduits à l'URL, ce qui dégrade l'identification du document et la navigation entre fenêtres.

**Remédiation :** fournir un titre localisé via `PageTitle` ou un `title` cohérent avec la culture, puis l'affirmer dans le smoke navigateur.

## Proportionnalité et sur-ingénierie

`PROPORTIONALITY: NONE` - Les modes Server et WebAssembly, le HeadOutlet et la CSP avec `wasm-unsafe-eval` sont nécessaires à Interactive Auto. Les remédiations restent locales.

## Contrôles fichier par fichier

<a id="samples-omnieurope-blazor-autosmoke-components-imports-razor"></a>
### `samples/OmniEurope.Blazor.AutoSmoke/Components/_Imports.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-autosmoke-components-app-razor"></a>
### `samples/OmniEurope.Blazor.AutoSmoke/Components/App.razor`

Finding(s) : [AUTO-001](#auto-001), [AUTO-002](#auto-002).

Référence architecture sans duplication : [ARCH-R-03](AUDIT_ARCHITECTURE.md#arch-r-03---interactive-auto-utilise-directement-la-rcl-sans-reference-directe).

<a id="samples-omnieurope-blazor-autosmoke-omnieurope-blazor-autosmoke-csproj"></a>
### `samples/OmniEurope.Blazor.AutoSmoke/OmniEurope.Blazor.AutoSmoke.csproj`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

Référence architecture sans duplication : [ARCH-R-03](AUDIT_ARCHITECTURE.md#arch-r-03---interactive-auto-utilise-directement-la-rcl-sans-reference-directe).

<a id="samples-omnieurope-blazor-autosmoke-packages-lock-json"></a>
### `samples/OmniEurope.Blazor.AutoSmoke/packages.lock.json`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

Référence dépendances sans duplication : [DEP-002](AUDIT_DEPENDENCIES.md#dep-002---moyen-dépendances--sécurité-le-sanitizer-de-production-est-obsolète-et-absent-de-la-politique).

<a id="samples-omnieurope-blazor-autosmoke-program-cs"></a>
### `samples/OmniEurope.Blazor.AutoSmoke/Program.cs`

Finding(s) : [AUTO-001](#auto-001).

Référence architecture sans duplication : [ARCH-R-03](AUDIT_ARCHITECTURE.md#arch-r-03---interactive-auto-utilise-directement-la-rcl-sans-reference-directe).

<a id="samples-omnieurope-blazor-autosmoke-wwwroot-app-css"></a>
### `samples/OmniEurope.Blazor.AutoSmoke/wwwroot/app.css`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

## Totaux

- Critique : 0
- Élevé : 1
- Moyen : 1
- Faible : 0
- INFO, consultatif et exclu du verdict : 0
- Fichiers audités : 6/6
