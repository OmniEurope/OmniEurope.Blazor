# Findings d'audit 360 - WasmSmoke

> Audit: 2026-08-11
> Les blocs sont ajoutés fichier par fichier. Une absence de finding est consignée explicitement par `RAS`.

<a id="wasmsmoke-imports"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/_Imports.razor`

RAS. Les imports sont minimaux et cohérents avec le rôle d'hôte WebAssembly terminal ; aucun contenu exécutable, secret, tiret cadratin U+2014 ou écart applicable au registre de règles n'est détecté. Revue de sécurité manuelle best-effort, non outillée.

<a id="wasmsmoke-app"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/App.razor`

- [Élevé] [Style] `STD-I18N` : le titre de page, le titre principal, le message de succès, le compteur et le libellé de progression sont codés en dur sans `IStringLocalizer<AppStrings>` - lignes 1, 4, 6-8 - source : registre `_Generic` et `AUDIT_KIT.md` (infrastructure de localisation absente) - recommandation : Codex peut ajouter le contrat de ressources propre au smoke host, localiser les chaînes descriptives et conserver `OmniEurope.Blazor` comme nom de marque.
- [Élevé] [Style] La logique d'état et le callback restent dans un bloc `@code`, alors que le standard frontend exige un fichier code-behind et que `GEN004` n'est pas câblé - lignes 12-20 - source : `C:\Dev\_Generic\docs\coding-standards.md` et `AUDIT_KIT.md` - recommandation : Codex peut déplacer l'état et le callback vers `App.razor.cs`, puis câbler `GEN004` sans suppression.
- [Moyen] [Style] `STD-BTN` : le bouton d'action possède un texte mais aucune icône, contrairement à la convention action texte + icône hors grille - ligne 7 - source : registre `_Generic` - recommandation : Codex peut ajouter une icône décorative adaptée au composant Omni tout en conservant le libellé localisé, sans réintroduire Radzen.
- [Faible] [Qualité] `Increment` renvoie un `Task` terminé mais son nom n'emploie pas le suffixe `Async`, ce qui masque son contrat asynchrone et diverge du standard du kit - lignes 15-18 - source : `C:\Dev\_Generic\docs\coding-standards.md` - recommandation : Codex peut renommer le callback en `IncrementAsync` sans changer son comportement, ou employer un callback synchrone si le contrat `OmniButton` le permet.

Sécurité : contenu statique et état local borné de 0 à 10 ; aucun secret, entrée non fiable, injection, ressource non libérée, fonctionnalité simulée ou tiret cadratin U+2014 détecté. Revue best-effort, non outillée. `Task.CompletedTask` correspond ici à un callback qui effectue réellement l'incrément et n'est pas un placeholder d'authenticité.

<a id="wasmsmoke-project"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/OmniEurope.Blazor.WasmSmoke.csproj`

RAS. Le projet WebAssembly reste un adaptateur terminal minimal : il référence uniquement la RCL attendue, épingle ses packages par la gestion centralisée, garde le DevServer privé et active le verrouillage de résolution. `AUDIT_DEPENDENCIES.md` ne relève ni vulnérabilité, dépréciation, conflit ni dérive propre à ce projet. L'absence globale des analyseurs `GENxxx` est déjà consignée dans `AUDIT_KIT.md` et n'est pas dupliquée ici. Aucun secret ni tiret cadratin U+2014 détecté.

<a id="wasmsmoke-lock"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/packages.lock.json`

RAS. Le verrou schéma v2 couvre 32 entrées, toutes résolues en `10.0.10`, avec hachages de contenu et la seule référence projet attendue. `AUDIT_DEPENDENCIES.md` confirme l'absence de vulnérabilité, de dépréciation, de conflit et de package obsolète propre à WasmSmoke. Aucun secret ni tiret cadratin U+2014 détecté.

<a id="wasmsmoke-program"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/Program.cs`

RAS. L'entrée WebAssembly se limite à la composition standard de `App` et `HeadOutlet`, puis attend correctement la durée de vie de l'hôte. Elle n'expose aucun endpoint, secret, état global, I/O bloquant, suppression de contrôle ou abstraction superflue. Revue de sécurité manuelle best-effort, non outillée ; aucun tiret cadratin U+2014 détecté.

<a id="wasmsmoke-index"></a>
## `samples/OmniEurope.Blazor.WasmSmoke/wwwroot/index.html`

- [Élevé] [Fiabilité] La CSP limite `script-src` à `'self'` sans `'wasm-unsafe-eval'`, alors que la documentation Microsoft .NET 10 exige cette source pour permettre au runtime Mono côté client de fonctionner ; le smoke host risque donc d'échouer avant le rendu interactif - ligne 6 - source : [Microsoft Learn, CSP Blazor .NET 10](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/content-security-policy?view=aspnetcore-10.0) et `docs/compatibility.md` qui confirme l'absence de validation navigateur - recommandation : Codex peut ajouter uniquement `'wasm-unsafe-eval'` à `script-src`, publier l'hôte puis prouver dans un navigateur que le runtime démarre, que le compteur fonctionne et qu'aucune violation CSP inattendue n'est émise.
- [Élevé] [Style] `STD-UIVERIFY` : la compatibilité WebAssembly est déclarée au niveau compilation et publication uniquement ; `docs/compatibility.md` indique explicitement que l'exécution navigateur reste à valider, et le préflight ne fournit aucune preuve visuelle ou console - lignes 6-13 - source : registre `_Generic`, `docs/compatibility.md` et `metrics/PREFLIGHT.md` - recommandation : Codex peut ajouter une vérification navigateur reproductible du chargement, du clic, de la progression, de la console et des violations CSP au pipeline de compatibilité.
- [Moyen] [Sécurité] `frame-ancestors 'none'` est livré dans une balise `meta`, où cette directive doit être ignorée ; la page paraît donc protégée contre l'encapsulation alors que cette protection n'est pas appliquée par la politique locale - ligne 6 - source : [W3C CSP Level 3, section `frame-ancestors`](https://w3c.github.io/webappsec-csp/#directive-frame-ancestors) et `docs/csp-contract.md` - recommandation : Codex peut faire émettre `frame-ancestors 'none'` comme en-tête HTTP par les hôtes de déploiement, vérifier l'en-tête dans la sonde publiée et retirer la directive trompeuse de la balise `meta`.
- [Faible] [Sécurité] `connect-src 'self' ws: wss:` autorise des connexions WebSocket vers n'importe quelle origine, alors que ce client autonome n'observe aucun besoin de connexion WebSocket hors développement - ligne 6 - source : revue manuelle best-effort, non outillée, et politique indicative plus restrictive de `docs/csp-contract.md` - recommandation : Codex peut séparer la CSP de développement de celle publiée, borner `connect-src` aux origines effectivement requises et prouver le démarrage WebAssembly sous la politique resserrée.

Le texte de bootstrap français et le titre technique sont cohérents avec `lang="fr"` pour cet hôte de test statique ; le scope strict de `STD-I18N` vise Razor et le code-behind, donc aucun finding de localisation supplémentaire n'est émis ici. Aucun secret, ressource distante, style inline, gestionnaire inline, stub ou tiret cadratin U+2014 détecté.

## Synthèse du module `WasmSmoke`

- Cohérence et architecture : RAS. Les six fichiers forment un adaptateur WebAssembly terminal minimal qui dépend uniquement de la RCL, conformément à `AUDIT_ARCHITECTURE.md`.
- Complétude et fiabilité : le graphe compile et se publie selon le préflight et la matrice de compatibilité, mais la CSP actuelle peut empêcher le runtime Mono de démarrer et aucune preuve navigateur ne ferme ce risque.
- Sécurité : revue manuelle best-effort, non outillée. La CSP statique est stricte sur les scripts, styles, objets et ressources distantes, mais `frame-ancestors` n'est pas applicable via `meta` et les schémas WebSocket sont trop larges. SAST et scan historique de secrets restent non fiables selon `metrics/`.
- Performance : l'état du composant est borné, aucun accès réseau applicatif, N+1, blocage, allocation non bornée ou ressource libérable n'est observé. Complexité, couverture et CRAP restent non fiables selon `metrics/`.
- UI clean-room : l'absence de Radzen n'est pas un finding. Les recommandations conservent les composants Omni et n'imposent aucune réintroduction de Radzen.
- Authenticité : aucun stub, contrôle neutralisé, seuil abaissé, donnée factice de production ou fonctionnalité silencieusement désactivée n'est détecté. La documentation reconnaît honnêtement que l'exécution navigateur n'est pas encore validée.
- Proportionnalité : `PROPORTIONALITY: NONE`. L'alternative la plus simple viable reste ce hôte WebAssembly de six fichiers ; aucune couche, abstraction, dépendance, configuration optionnelle ou extension spéculative n'est observée.
- Compteurs : 8 findings actionnables, soit Critique 0, Élevé 4, Moyen 2, Faible 2. Les 6 fichiers sont en mode `Full`.
