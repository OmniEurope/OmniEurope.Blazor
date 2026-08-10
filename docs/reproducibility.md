# Reproductibilité des artefacts

La compilation active `Deterministic` dans `Directory.Build.props`. Les DLL Release sont donc produites à partir d'entrées et d'un SDK verrouillés par `global.json` et les fichiers `packages.lock.json`.

## État du paquet NuGet

Avec le SDK actuellement verrouillé (`10.0.302`), deux exécutions `dotnet pack --no-build --no-restore` contiennent les mêmes fichiers fonctionnels, mais les archives `.nupkg` et `.snupkg` ne sont pas bit-à-bit identiques. NuGet régénère notamment l'identifiant du document OPC `package/services/metadata/core-properties/*.psmdcp` et les métadonnées ZIP.

La documentation officielle de [`dotnet pack`](https://learn.microsoft.com/dotnet/core/tools/dotnet-pack) expose `Deterministic` et `DeterministicTimestamp` pour les paquets à partir du SDK .NET `10.0.400`. La gate bit-à-bit doit donc rester ouverte tant que le SDK du dépôt n'a pas été mis à niveau et que le double empaquetage n'a pas produit deux SHA-256 identiques.

Les contrôles actuellement rejouables dans le dépôt vérifient séparément :

- la restauration `--locked-mode` ;
- la baseline API partielle décrite dans [public-api-conventions.md](public-api-conventions.md) ;
- le contenu du paquet principal et du paquet de symboles ;
- la dépendance client-compatible `Microsoft.AspNetCore.Components.Web` ;
- la licence EUPL-1.2 ;
- les budgets CSS, assembly et NuGet.

## Limites des preuves actuelles

- `component-coverage.md` et son JSON prouvent la présence d'une cible Razor pour chaque entrée, pas ses comportements ni son équivalence fonctionnelle.
- Le contrôle CSP du catalogue vérifie les sources, les réponses HTTP et un collecteur encore vide avant interaction ; il ne remplace pas une navigation dans un navigateur réel.
- Les inventaires Radzen et les contrats observés sont régénérables uniquement avec le même état externe de `C:\Dev`. Le dépôt ne conserve ni manifeste d'empreintes de cet instantané, ni copie des entrées. En outre, l'extraction des paramètres Razor est heuristique et produit des faux positifs documentés dans [component-contracts.md](component-contracts.md).
- Les métriques de comparaison de provenance annoncées lors de la revue ne sont pas accompagnées dans le dépôt de leurs versions de référence, scanner, paramètres et résultats bruts. Elles ne sont donc pas reproductibles indépendamment en l'état.
