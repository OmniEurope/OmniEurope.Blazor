# Findings d'audit 360 - AutoSmokeClient

> Audit: 2026-08-11
> Les blocs sont ajoutés fichier par fichier. Une absence de finding est consignée explicitement par `RAS`.

<a id="samples-omnieurope-blazor-autosmokeclient_importsrazor"></a>
## `samples/OmniEurope.Blazor.AutoSmoke.Client/_Imports.razor`

**RAS.** Fichier intégralement lu. Les deux imports sont nécessaires au rendu Blazor et à l'usage des composants OmniEurope; aucun markup, chaîne visible, style, script, secret, suppression de contrôle, stub, code mort ou caractère U+2014. Les règles UI du kit sont non applicables à ce fichier d'imports et aucune dépendance Radzen n'est introduite. Sécurité: best-effort, non outillée. Complexité, couverture et CRAP: non fiables selon les métriques de préflight.

`PROPORTIONALITY: NONE` - deux imports directs constituent l'alternative viable la plus simple; aucune abstraction ni indirection n'est ajoutée.

<a id="samples-omnieurope-blazor-autosmokeclient-autoprobe"></a>
## `samples/OmniEurope.Blazor.AutoSmoke.Client/AutoProbe.razor`

- [Élevé] [Style] Le composant contient toute sa logique dans un bloc `@code`, contrairement au pattern code-behind `.razor` + `.razor.cs` imposé par les standards du kit et non protégé ici par `GEN004` - lignes 8-16 - source: `C:\Dev\_Generic\docs\coding-standards.md`, `AUDIT_KIT.md` et build sans analyseur `GEN004` - recommandation: Codex peut déplacer l'état et le gestionnaire dans `AutoProbe.razor.cs`, sans modifier le contrat de rendu ni introduire Radzen.
- [Élevé] [Style] **STD-I18N**: les textes visibles `OmniEurope.Blazor Interactive Auto` et `Compteur Auto :` sont codés en dur, sans `IStringLocalizer<AppStrings>` ni contrat local de ressources - lignes 4-5 - source: registre `STD-I18N` et `AUDIT_KIT.md` - recommandation: Codex peut définir la localisation adaptée aux samples de la RCL, fournir les clés et ressources par défaut, puis injecter le localizer dans le code-behind; aucune dépendance Radzen n'est nécessaire.
- [Élevé] [Fiabilité] **STD-UIVERIFY**: le seul comportement interactif de la sonde est l'incrément du compteur après hydratation Interactive Auto, mais les preuves actuelles couvrent uniquement compilation, publication, prérendu et assets; l'hydratation navigateur reste explicitement non validée - lignes 3-5 - source: `docs/compatibility.md` et `README.md` - recommandation: Codex peut ajouter une vérification navigateur automatisée qui attend l'interactivité, clique `#auto-action`, constate l'incrément et exige une console sans erreur.
- [Moyen] [Style] **STD-BTN**: le bouton d'action expose un libellé mais aucun pictogramme, alors que la règle adaptée aux boutons Omni exige texte court et icône hors grille - ligne 5 - source: registre `STD-BTN`; `OmniButton` accepte un `RenderFragment`, donc un `OmniIcon` peut être composé sans Radzen - recommandation: Codex peut ajouter une icône Omni décorative avec le libellé conservé et une sémantique accessible inchangée.

Sécurité: revue best-effort, non outillée; aucun secret, entrée non fiable, injection, HTML brut, style inline, autofocus ou ressource distante n'est présent. Authenticité: le `Task.CompletedTask` de la ligne 14 termine un vrai gestionnaire synchrone et n'est pas un stub, mais la preuve runtime d'hydratation manque comme indiqué ci-dessus. Aucun caractère U+2014. Complexité, couverture et CRAP: non fiables selon les métriques de préflight.

`PROPORTIONALITY: NOTICE` - pour un incrément purement synchrone, un gestionnaire `void Increment()` est plus simple que la création d'un contrat `Task` terminé; le coût est une indirection asynchrone sans attente ni annulation. Cette notification est consultative, non actionnable et exclue des constats.

<a id="samples-omnieurope-blazor-autosmokeclient-csproj"></a>
## `samples/OmniEurope.Blazor.AutoSmoke.Client/OmniEurope.Blazor.AutoSmoke.Client.csproj`

**RAS.** Fichier intégralement lu. Le SDK Blazor WebAssembly, le verrou NuGet, la référence centralisée à `Microsoft.AspNetCore.Components.WebAssembly` et la dépendance directe vers la RCL respectent la cible architecturale du client Interactive Auto. Le manifeste ne contient ni version flottante, suppression d'analyse, `NoWarn`, secret, dépendance Radzen, package inutile observable, référence inversée ou caractère U+2014. `AUDIT_DEPENDENCIES.md` établit zéro vulnérabilité et zéro dépréciation pour ce graphe; le build Release du préflight est propre. Sécurité supply-chain: fiable pour les avis NuGet de la révision, mais SAST et scan historique des secrets non fiables. Complexité, couverture et CRAP: non applicables au XML du manifeste.

`PROPORTIONALITY: NONE` - un SDK, un package de plateforme et une référence de projet constituent le manifeste minimal pour cette sonde; aucune couche ou extension spéculative n'est présente.

<a id="samples-omnieurope-blazor-autosmokeclient-lock"></a>
## `samples/OmniEurope.Blazor.AutoSmoke.Client/packages.lock.json`

**RAS.** Fichier intégralement lu, 267 lignes. Le verrou v2 couvre 31 entrées `net10.0`, leurs hachages de contenu, la référence projet OmniEurope et la cible secondaire `net10.0/browser-wasm`; toutes les briques Microsoft de ce graphe sont alignées en `10.0.10`. Aucun package Radzen, version preview, conflit, downgrade, dépendance abandonnée, suppression de contrôle, valeur secrète ou caractère U+2014 n'est présent. `AUDIT_DEPENDENCIES.md` établit zéro vulnérabilité et zéro dépréciation pour ce module, et ne classe aucune dépendance AutoSmokeClient comme obsolète. Sécurité supply-chain: fiable pour les avis NuGet et les versions verrouillées; SAST et scan historique des secrets non fiables. Complexité, couverture et CRAP: non applicables au verrou JSON.

`PROPORTIONALITY: NONE` - le graphe correspond aux transitifs requis par le SDK WebAssembly et la RCL; aucun package direct superflu n'est observable et une réduction manuelle du verrou casserait sa génération déterministe.

<a id="samples-omnieurope-blazor-autosmokeclient-program"></a>
## `samples/OmniEurope.Blazor.AutoSmoke.Client/Program.cs`

**RAS.** Fichier intégralement lu. Le point d'entrée à instructions de niveau supérieur crée et exécute directement le `WebAssemblyHost` requis pour l'hydratation Interactive Auto. Aucun service inutile, blocage synchrone, fire-and-forget, ressource non libérée, secret, entrée non validée, suppression d'analyse, stub, faux résultat, dépendance Radzen ou caractère U+2014. La référence réelle depuis l'hôte Server et l'enregistrement de l'assembly client confirment que ce point d'entrée appartient à un flux câblé, pas à du code orphelin. Sécurité: best-effort, non outillée. Complexité, couverture et CRAP: non fiables selon les métriques de préflight.

`PROPORTIONALITY: NONE` - les deux instructions exécutables constituent le bootstrap WebAssembly minimal; ajouter DI, configuration ou wrappers sans besoin observé serait plus complexe.

## Revue du module dans son ensemble - Passe 2c

Le module est cohésif et correctement placé comme adaptateur terminal Interactive Auto. Il ne contient aucun modèle métier, service réutilisable ou dépendance latérale; son graphe converge vers la RCL puis est consommé par l'hôte `AutoSmoke`, conformément à `AUDIT_ARCHITECTURE.md`. La duplication structurelle avec les autres sondes est justifiée par des cibles runtime distinctes. Aucun cycle, couplage interdit, dépendance Radzen, copie d'expression Radzen, asset tiers ou entorse clean-room n'est observé.

La complétude fonctionnelle reste limitée par le finding `STD-UIVERIFY` du bloc `AutoProbe.razor`: le module compile et se publie, mais son hydratation et son interaction ne disposent pas encore d'une preuve navigateur. Les autres constats granulaires concernent le code-behind, la localisation et la convention de bouton. Aucun constat supplémentaire de portée module n'est ajouté.

`PROPORTIONALITY: NONE` - séparer Server et Client est requis par le modèle Interactive Auto; fusionner les deux projets supprimerait la frontière d'hydratation, tandis qu'ajouter une couche de service ou une abstraction de compteur serait injustifié.
