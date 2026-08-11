# Findings d’audit 360 - OmniEurope.Blazor.WasmSmoke

> Audit frais : 2026-08-11
> Périmètre : 13 fichiers, mode Full, lecture intégrale.
> Les constats globaux Architecture/Kit/Dépendances ne sont pas dupliqués.

## Synthèse

| Critique | Élevé | Moyen | Faible | INFO |
|---:|---:|---:|---:|---:|
| 0 | 1 | 0 | 1 | 1 |

<a id="samplesomnieuropeblazorwasmsmokeimportsrazor"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/_Imports.razor`

RAS.

<a id="samplesomnieuropeblazorwasmsmokeapprazor"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/App.razor`

RAS.

<a id="samplesomnieuropeblazorwasmsmokeapprazorcs"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/App.razor.cs`

RAS.

<a id="samplesomnieuropeblazorwasmsmokeglobalusingscs"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/GlobalUsings.cs`

RAS.

<a id="samplesomnieuropeblazorwasmsmokeomnieuropeblazorwasmsmokecsproj"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/OmniEurope.Blazor.WasmSmoke.csproj`

RAS.

<a id="samplesomnieuropeblazorwasmsmokepackageslockjson"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/packages.lock.json`

RAS. La version transitive de `HtmlSanitizer` est couverte globalement par `DEP-002`; aucun constat supplémentaire propre à WasmSmoke.

<a id="samplesomnieuropeblazorwasmsmokeprogramcs"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/Program.cs`

RAS.

<a id="samplesomnieuropeblazorwasmsmokeresourceswasmsmokestringscs"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/Resources/WasmSmokeStrings.cs`

RAS.

<a id="samplesomnieuropeblazorwasmsmokeresourceswasmsmokestringsenresx"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/Resources/WasmSmokeStrings.en.resx`

RAS.

<a id="samplesomnieuropeblazorwasmsmokeresourceswasmsmokestringsresx"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/Resources/WasmSmokeStrings.resx`

RAS.

<a id="samplesomnieuropeblazorwasmsmokewwwrootheaders"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/wwwroot/_headers`

- [WASM-002] [Faible] [Performance] Le manifeste de déploiement applique les en-têtes de sécurité, mais ne définit aucun `Cache-Control` pour distinguer le shell mutable des ressources `_framework` fingerprintées. Le comportement dépend donc des valeurs par défaut de l’hébergeur : un `index.html` peut rester périmé ou, inversement, les plusieurs mégaoctets d’assets immuables être revalidés à chaque visite - lignes 1-5 - preuve : manifeste `_headers` complet et sortie publiée contenant des noms fingerprintés - recommandation : Codex peut ajouter une règle de non-cache/revalidation pour le shell et une règle longue `public, immutable` limitée aux assets fingerprintés, puis étendre `Test-WasmHeaders.ps1` avec des fixtures de priorité.

<a id="samplesomnieuropeblazorwasmsmokewwwrootappcss"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/wwwroot/app.css`

RAS.

<a id="samplesomnieuropeblazorwasmsmokewwwrootindexhtml"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/wwwroot/index.html`

- [WASM-001] [Élevé] [Style] `STD-I18N` n’est pas respectée dans le shell de démarrage : le document fixe `lang="fr"`, affiche un titre anglais et un message de chargement français hors `IStringLocalizer`, alors que l’application publie aussi une ressource anglaise. Sous une culture anglaise, le contenu hydraté peut donc être annoncé par les technologies d’assistance comme français, et les textes pré-hydratation restent incohérents - lignes 2, 7 et 14 - preuve : `WasmSmokeStrings.en.resx`, `App.razor:1-8` et absence de synchronisation de l’attribut `lang` - recommandation : Codex peut rendre le bootstrap linguistiquement neutre ou aligné sur la culture sélectionnée, synchroniser `document.documentElement.lang`, puis ajouter une sonde runtime française et anglaise.

## Notification de proportionnalité (INFO)

- [WASM-INFO-001] L’hôte reste proportionné à sa fonction : un composant racine, un code-behind, deux ressources et un shell statique suffisent à prouver WebAssembly. Ajouter routage, état applicatif ou abstraction de services n’apporterait aucune garantie supplémentaire; les corrections proposées restent limitées au contrat de localisation et au manifeste d’hébergement.

