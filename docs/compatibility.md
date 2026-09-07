# Matrice de compatibilité

| Hôte | Cible | Niveau | Contraintes |
|---|---|---|---|
| Blazor Server / Interactive Server | .NET 10 | Compilation, fumée HTTP et contrôles navigateur vérifiés | Le catalogue répond, expose ses assets et son en-tête CSP ; les contrôles de focus, d'expiration et de noms accessibles sont exercés dans Chromium. |
| Blazor WebAssembly | .NET 10 | Compilation, publication et interaction navigateur vérifiées | La sonde sert l'artefact avec ses en-têtes de déploiement, démarre Chromium, clique `#wasm-action`, constate le compteur et la progression à 1, puis exige une console sans erreur et une CSP stricte. |
| Blazor Web App Interactive Auto | .NET 10 | Prérendu, hydratation et interaction vérifiés | La sonde publiée attend l'interactivité, clique `#auto-action`, constate le compteur à 1 et exige une console sans erreur via CDP. |
| MAUI Blazor Hybrid | .NET 10 / MAUI 10.0.90 | Compilation et exécution WebView2 vérifiées | L'application Windows minimale charge `HybridSmoke` dans `BlazorWebView`; la sonde CDP clique `#hybrid-action`, constate le compteur à 1 et exige une console sans erreur. |

La bibliothèque dépend du paquet client-compatible `Microsoft.AspNetCore.Components.Web`, n'accède pas directement au DOM depuis C# et charge un unique module JavaScript statique pour placer le focus sur la première erreur de formulaire. Les dates reposent sur `DateOnly` ou `DateTimeOffset`. Le scheduler accepte un `TimeZoneInfo`; si le consommateur ne le fournit pas, il utilise `TimeZoneInfo.Local`, donc le fuseau dépend implicitement de l'hôte. Fournir le paramètre explicitement est nécessaire pour un résultat reproductible.

Le catalogue local constitue l'hôte Server de référence avec contrôles HTTP et navigateur. WebAssembly est publié puis exercé de façon autonome par `eng/Test-WasmHost.ps1`, avec en-têtes, assets, clic, progression, console et CSP vérifiés. Interactive Auto est exercé dans Chromium après hydratation. Hybrid est construit et exécuté comme application MAUI Windows dans WebView2, puis inspecté par CDP. Les scripts échouent si l'interaction attendue n'aboutit pas ou si la console contient une erreur.
