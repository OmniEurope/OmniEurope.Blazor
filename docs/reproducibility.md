# Reproductibilité des artefacts

## Texte légal officiel

Le fichier `LICENSE` conserve textuellement les deux tirets cadratins du texte anglais officiel EUPL-1.2. Ils constituent l'unique exception à la gate typographique U+2014; le code, les scripts, les configurations et les autres documents doivent rester sans ce caractère. La version de référence est publiée par la Commission européenne dans les [textes officiels EUPL-1.2](https://interoperable-europe.ec.europa.eu/collection/eupl/eupl-text-eupl-12).

La compilation active `Deterministic` dans `Directory.Build.props` et les dépendances sont verrouillées par les fichiers `packages.lock.json`. `global.json` déclare un plancher SDK (`version: 11.0.100`, `rollForward: latestFeature`) : toute bande `11.0` à partir de `11.0.100` convient, et les paquets implicites du SDK sont épinglés dans `Directory.Build.targets`, si bien que les verrous ne dépendent pas de la bande résolue (ADR-002). `eng/Test-SdkBand.ps1` lit `global.json` et applique ce plancher dans les deux jobs de CI, sans recopier le numéro. Le workload set n'est plus figé, car `setup-dotnet` n'en installe aucun et le pin faisait échouer la résolution MSBuild de tout projet, y compris ceux qui n'utilisent aucun workload; seul le job MAUI installe `maui-windows`.

`.gitattributes` impose LF à tout fichier suivi, sauf `docs/third-party-licenses/`, laissé sans conversion. Ces textes sont copiés octet pour octet depuis les paquets NuGet puis hachés comme preuve : les convertir ferait diverger le hash entre un poste Windows et la CI pour un contenu identique.

## État du paquet NuGet

Jusqu'au 2026-11-14, deux exécutions `dotnet pack --no-build` du même build donnaient les mêmes fichiers, mais des archives `.nupkg` et `.snupkg` différentes : seuls les horodatages des entrées ZIP changeaient, posés à l'heure de l'empaquetage (constaté le 2026-11-14 avec le SDK `11.0.100` : contenus décompressés identiques, horodatages `07:16:14` puis `07:16:20`).

`Deterministic` est actif dans `Directory.Build.props` et NuGet le reçoit, mais il lui faut aussi `DeterministicTimestamp`, une date et non un booléen (la valeur `true` est ignorée sans avertissement). `OmniEurope.Blazor.csproj` fixe `DeterministicTimestamp` à `2000-01-01T00:00:00Z` : deux empaquetages du même build sont alors identiques octet pour octet (SHA-256 égaux pour le `.nupkg` et le `.snupkg`, vérifié le 2026-11-14). `eng/Test-DeterministicPack.ps1` le prouve à chaque CI : il empaquette une seconde fois le build Release, dans un dossier neuf, et exige les mêmes SHA-256 que le paquet publié. La garantie porte sur deux empaquetages d'un même build ; deux builds sur deux machines ne donnent les mêmes assemblys que si les chemins sources sont normalisés, ce que fait `ContinuousIntegrationBuild` en CI seulement.

Les contrôles actuellement rejouables dans le dépôt vérifient séparément :

- la restauration `--locked-mode` ;
- la baseline API complète décrite dans [public-api-conventions.md](public-api-conventions.md) ;
- le contenu du paquet principal et du paquet de symboles ;
- deux empaquetages du même build identiques octet pour octet (`eng/Test-DeterministicPack.ps1`) ;
- les dépendances du paquet (`HtmlSanitizer`, `Microsoft.AspNetCore.Components.Web`, `Microsoft.Extensions.Localization`), comparées aux versions centrales ;
- la licence EUPL-1.2 ;
- les budgets de taille de l'assembly et du paquet NuGet, et la minification de la feuille livrée.

## Limites des preuves actuelles

- Le contrôle CSP du catalogue vérifie les sources et les réponses HTTP, puis `eng/Test-CatalogProbe.mjs` pilote Chromium par CDP (navigation, dialogue, focus, notification, éditeur) et exige une console sans erreur et aucune violation CSP ; il ne couvre que ces parcours, pas chaque composant. Les huit sondes de la vitrine (`eng/Test-ShowcaseHost.ps1`, voir [testing.md](testing.md)) couvrent davantage de parcours, mais restent une porte manuelle, hors CI.
- Les métriques de comparaison de provenance annoncées lors de la revue ne sont pas accompagnées dans le dépôt de leurs versions de référence, scanner, paramètres et résultats bruts. Elles ne sont donc pas reproductibles indépendamment en l'état.
- La double exécution de `dotnet pack` d'avant le 2026-11-14 n'a pas laissé de hashes ; la porte `eng/Test-DeterministicPack.ps1` les écrit désormais dans le journal de chaque CI.
