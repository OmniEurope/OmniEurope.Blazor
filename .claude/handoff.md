# Handoff - 2026-09-07

## State
Branch: develop · Last commit: 6d30a1c fix(tests): pin the suite culture and wait for the MAUI host before probing

`develop` est à jour sur `origin`. Arbre de travail propre sur le fond : `NOTICE.md`, `docs/sbom.cdx.json`, `docs/third-party-packages.json` et `src/OmniEurope.Blazor/packages.lock.json` apparaissent modifiés mais ne diffèrent que par les fins de ligne (générateurs PowerShell écrivant en CRLF sous Windows, normalisés en LF par `.gitattributes` au commit). Aucun contenu à committer.

`main` est mergé **localement** en `fe8da4a`, mais ce merge date d'avant les commits `f2e1c72`, `83ea63d` et `6d30a1c`. Il est donc périmé et doit être refait.

Suivis d'audit de session ouverts : `0`. Constats de challenge ouverts : `6` (voir `.claude/challenge-session.md`).

Une session parallèle a travaillé dans le même arbre pendant cette session (commits `f2e1c72` et `83ea63d`, couverture des variantes de la vitrine). Vérifier `git status` et l'horodatage des fichiers avant de committer quoi que ce soit.

## Done in this session
- Version du paquet portée à `1.0.0` (`.csproj`, `README.md`, SBOM, section `[Unreleased]` du changelog close en `[1.0.0]`).
- Pin SDK remplacé par une bande de fonctionnalités (`10.0.300` + `rollForward: latestPatch`), `workloadVersion` retiré, et le numéro dédupliqué dans `eng/Test-SdkBand.ps1` au lieu d'être recopié dans `ci.yml` et un test de convention.
- `.gitattributes` ajouté : LF partout, sauf `docs/third-party-licenses/` laissé sans conversion, car ces textes sont copiés octet pour octet depuis les paquets NuGet puis hachés comme preuve.
- Actions GitHub passées en runtimes node24 (`checkout` v5.1.0, `upload-artifact` v6.0.0, runtimes vérifiés dans les `action.yml`).
- 10 dépendances montées, 2 marquées `toolchain-bound` avec leur raison (`Microsoft.CodeAnalysis.CSharp` bloqué par CS9057, `xunit.v3` bloqué par la suppression de VSTest).
- Pannes CI corrigées avec reproduction locale à chaque fois : lock file HybridSmoke périmé, runtime pack absent d'un runner propre (NETSDK1112), locale navigateur non fixée sur les sondes WebAssembly et Auto, culture non fixée du banc de test, sonde MAUI sans attente de son hôte.
- `docs/mistakes.md` créé, 12 entrées, chacune avec symptôme observable, cause réelle, correctif et garde ou méthode de reproduction.

## In progress
Rien d'inachevé côté code. Le seul travail suspendu est la publication : `main` doit être re-mergé puis poussé, et la release NuGet créée ensuite.

## Next step
Attendre le verdict de la CI sur `6d30a1c`, puis refaire le merge de `develop` dans `main` et demander à l'humain de lancer `git push origin main`, le hook `guard-git-push.js` bloquant cette publication côté agent.

## Key files
- `global.json` → pin SDK par bande de fonctionnalités, source unique du numéro.
- `eng/Test-SdkBand.ps1` → garde qui lit `global.json` et applique la bande dans les deux jobs.
- `.gitattributes` → normalisation LF, avec l'exception des textes de licence.
- `.github/workflows/ci.yml` → actions node24, gardes SDK, et build MAUI sans `--no-restore`.
- `eng/Test-WasmHost.ps1`, `eng/Test-AutoHost.ps1` → paramètre `-BrowserLanguage` fixant la locale du navigateur.
- `eng/Test-HybridHost.ps1` → attente d'une cible CDP avant de sonder, paramètre `-ReadyTimeoutSeconds`.
- `tests/OmniEurope.Blazor.Tests/TestCulture.cs` → `ModuleInitializer` fixant `fr-FR` pour tout le banc.
- `eng/dependency-policy.json` → catalogue revu, statut `toolchain-bound` et sa raison.
- `docs/mistakes.md` → journal des erreurs rencontrées et de leur correctif.

## Pitfalls
- **Le hook `guard-git-push.js` refuse `main` même quand l'humain le demande explicitement dans le message courant.** Sa décision est finale côté agent ; l'humain doit lancer la commande depuis son terminal.
- **Une classe entière de bugs vient de l'environnement, pas du code** : culture, locale navigateur, fins de ligne, cache NuGet. Ces bugs sont verts sur un poste français avec un cache chaud et rouges sur un runner. Le réflexe à garder : reproduire d'abord en forçant la condition du runner (contrôle négatif), corriger ensuite. Un correctif d'environnement sans contrôle négatif ne prouve rien.
- **Le build local est impossible sans le bon SDK** : la bande `10.0.3xx` est requise. Vérifier avec `./eng/Test-SdkBand.ps1`.
- **`eng/Test-DependencyPolicy.ps1` rougit tout seul avec le temps** : il exige que chaque paquet `latest-stable` soit exactement la dernière version publiée sur nuget.org, et `reviewedAt` périme au bout de 30 jours (revu le 2026-09-07, donc échéance au 2026-10-07). Traiter la dérive avant tout autre travail, et marquer `toolchain-bound` ce qui ne peut structurellement pas suivre.
- **Les scripts `eng/*.ps1` doivent rester ASCII** : tout texte français va dans `eng/PowerShellMessages.psd1`, sinon `EngineeringPowerShellScripts_AreAsciiOnly` échoue.
- **Les preuves générées périment en silence** : après toute modification de la surface publique, de la liste des projets ou des dépendances, relancer `eng/Test-PublicApi.ps1 -Update`, `eng/Generate-ComponentCoverage.ps1` et `eng/Generate-Sbom.ps1`.
- **Dette assumée** : `xunit` reste en 3.2.2. La 4.0.0 supprime VSTest sur le SDK .NET 10 et casse toute la chaîne de preuve de couverture (`coverlet.collector`, le `.trx`, `eng/Test-Coverage.ps1`, la commande de test de `ci.yml`). C'est une migration vers Microsoft.Testing.Platform, à mener comme un travail à part entière.

## Open questions
- Faut-il engager la migration vers Microsoft.Testing.Platform pour débloquer `xunit` 4, ou rester sur la ligne 3.x tant que la couverture VSTest suffit ?
- Le contrôle catalogue en ligne de `eng/Test-DependencyPolicy.ps1` doit-il rester bloquant en CI, ou devenir une revue planifiée hors du chemin critique ? L'humain a choisi de monter les paquets plutôt que d'assouplir la garde ; la question se reposera à chaque publication amont.
- La release NuGet `1.0.0` doit-elle être créée dès que la CI est verte sur `main`, ou attendre une validation supplémentaire ?
