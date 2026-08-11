# Matrice de compatibilité

| Hôte | Cible | Niveau | Contraintes |
|---|---|---|---|
| Blazor Server / Interactive Server | .NET 10 | Compilation et fumée HTTP vérifiées | Le catalogue répond, expose ses assets et son en-tête CSP ; les parcours interactifs en navigateur restent à valider. |
| Blazor WebAssembly | .NET 10 | Compilation et publication vérifiées | L'hôte `OmniEurope.Blazor.WasmSmoke` se publie avec l'assembly, le CSS et le module JS ; l'exécution navigateur reste à valider. |
| Blazor Web App Interactive Auto | .NET 10 | Prérendu et assets vérifiés | La sonde `OmniEurope.Blazor.AutoSmoke` compile, se publie et sert le prérendu et les assets client ; l'hydratation navigateur reste à valider. |
| MAUI Blazor Hybrid | .NET 10 / MAUI 10.0.20 | Compilation vérifiée | La sonde `OmniEurope.Blazor.HybridSmoke` compile avec `BlazorWebView`; le runtime graphique reste à valider sur une application exécutable. |

La bibliothèque dépend du paquet client-compatible `Microsoft.AspNetCore.Components.Web`, n'accède pas directement au DOM depuis C# et charge un unique module JavaScript statique pour placer le focus sur la première erreur de formulaire. Les dates reposent sur `DateOnly` ou `DateTimeOffset`. Le scheduler accepte un `TimeZoneInfo`; si le consommateur ne le fournit pas, il utilise `TimeZoneInfo.Local`, donc le fuseau dépend implicitement de l'hôte. Fournir le paramètre explicitement est nécessaire pour un résultat reproductible.

Le catalogue local constitue l'hôte Server de référence et possède un test HTTP reproductible, sans interaction navigateur. WebAssembly dispose d'un hôte de fumée compilé et publié en CI. Interactive Auto possède une sonde publiée avec contrôle HTTP du prérendu et des assets client. Hybrid possède une sonde de compilation MAUI dans un job Windows séparé. L'interactivité Server, l'exécution WebAssembly, l'hydratation Auto et l'exécution graphique Hybrid restent des gates de stabilisation 1.0 à valider dans leurs runtimes réels.
