# Reproductibilité des artefacts

## Texte légal officiel

Le fichier `LICENSE` conserve textuellement les deux tirets cadratins du texte anglais officiel EUPL-1.2. Ils constituent l'unique exception à la gate typographique U+2014; le code, les scripts, les configurations et les autres documents doivent rester sans ce caractère. La version de référence est publiée par la Commission européenne dans les [textes officiels EUPL-1.2](https://interoperable-europe.ec.europa.eu/collection/eupl/eupl-text-eupl-12).

La compilation active `Deterministic` dans `Directory.Build.props` et les dépendances sont verrouillées par les fichiers `packages.lock.json`. `global.json` impose exactement le SDK `10.0.302` avec `rollForward: disable` et le workload set `10.0.302`. La CI enregistre le SDK et le mode workload-set, installe `maui-windows`, puis exige que `dotnet --version` et `dotnet workload --version` valent tous deux exactement `10.0.302`; un poste sans ces versions échoue explicitement au lieu de sélectionner un patch différent.

## État du paquet NuGet

Lors d'une vérification locale non archivée avec le SDK `10.0.302`, deux exécutions `dotnet pack --no-build --no-restore` contenaient les mêmes fichiers fonctionnels, mais les archives `.nupkg` et `.snupkg` n'étaient pas bit-à-bit identiques. NuGet régénérait notamment l'identifiant du document OPC `package/services/metadata/core-properties/*.psmdcp` et les métadonnées ZIP.

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
- Les inventaires Radzen et les contrats observés sont régénérés depuis [radzen-corpus.json](radzen-corpus.json), qui sépare projets actifs, modèles, archives et miroir, et conserve pour chaque projet sa révision, l'empreinte du fichier projet, l'empreinte agrégée et chaque fichier source scanné. `eng/Test-RadzenCorpus.ps1` refuse les doublons, statuts inconnus et empreintes internes incohérentes; l'option `-VerifyFiles` contrôle en plus les fichiers externes et refuse toute dérive. Les deux générateurs activent toujours cette vérification stricte. L'instantané n'inclut pas le contenu des projets externes; une reproduction exige donc un corpus dont les fichiers correspondent exactement aux hashes versionnés. L'extraction des paramètres Razor reste heuristique et ses limites sont publiées dans [component-contracts.md](component-contracts.md).
- Les métriques de comparaison de provenance annoncées lors de la revue ne sont pas accompagnées dans le dépôt de leurs versions de référence, scanner, paramètres et résultats bruts. Elles ne sont donc pas reproductibles indépendamment en l'état.
- Le dépôt ne conserve pas les hashes, listings ou journaux de la double exécution de `dotnet pack` citée plus haut ; ce constat historique n'est donc pas reproductible indépendamment en l'état.
