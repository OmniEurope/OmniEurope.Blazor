<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-009 : preuves du lot 1, 2026-10-30

> Lot 1 terminé le 2026-10-30. A360-008 et A360-011 clôturés par les preuves ci-dessous.

Base : `develop`, `80ddc8c8c98048949eabe4bd14e6c9bf6d321f7d`, checkout principal. Périmètre autorisé : A360-008 et A360-011 du 30 octobre, contrôles du lot 1 et commit final. Aucun push autorisé. Le rapport initial reste attaché à sa base et n’est pas réécrit pour afficher des constats clos.

## Reproductions avant correction

| Constat | Parcours réel | Résultat attendu et obtenu |
|---|---|---|
| A360-008 | Restore verrouillé et build Release de la solution, puis publication locale fraîche de la vitrine. `eng/Test-ShowcaseHost.ps1 -Probe Languages -Parallel 4` sur ce wwwroot, port 5295. | Attendu : quatre couples et aucun fragment vide. Obtenu : huit fragments ; les quatre premiers mesurent chacun 79 pages, les fragments 5 à 8 mesurent zéro page et échouent. La fusion échoue aussi, code 1, après 115 secondes. |
| A360-011 | Harnais indépendant chargeant par réflexion le vrai `CountedModule` compilé à `80ddc8c8`, deux threads synchronisés par une barrière, 100 000 paires de deux appels terminés. Source du double comparée à celle de develop avant correction. | Attendu : 2 à chaque paire. Obtenu : 1 038 compteurs incorrects, code 1. Le défaut est dans le double de test ; aucune double libération du produit n’est déduite. |

## Changements bornés

- `eng/Test-ShowcaseHost.ps1` : limite de fragments Languages passée de 8 à 4, correspondant aux deux langues et deux largeurs réellement lancées. Le calcul `2 × Parallel` et le planificateur existants restent utilisés sous cette limite. Aucun changement dans les sondes, la fusion, leurs rejets ou leurs exemptions. Commentaires et description de parallélisme du guide de test alignés.
- `DisposedWhileLoadingTests.CountedModule` : champ privé, incrément par `Interlocked.Increment`, lecture par `Volatile.Read`. Les assertions et les 500 tours du scénario concurrent existant sont conservés. Aucun code du paquet NuGet modifié.
- Le catalogue local ignoré `docs/test-config.md` est aligné sur quatre couples ; il n’est pas ajouté de force au dépôt. Plan, registre et présente annexe tracent la livraison. La synthèse publique de l’audit de la branche du voile accompagne la première inscription Git du plan ; le rapport nominatif précédent reste local, sans perte de preuve.

## Contrôles après correction

| Contrôle | Preuve observée |
|---|---|
| Build Release du projet de tests après correction | Réussi, zéro avertissement et zéro erreur. |
| Même harnais, vrai double recompilé dans develop | 100 000 paires, deux appels terminés par paire, zéro compteur incorrect, code 0. |
| Dix passages consécutifs de `DisposedWhileLoadingTests`, Release, sans rebuild | 14 réussis, zéro échec et zéro ignoré à chacun des dix passages. Chaque TRX contient le scénario `MenuController_TwoLoadsEndingTogetherOnTwoThreads_ReleaseTheModuleOnceEveryTime`, soit 500 tours par passage. Aucun retry, pause ni délai ajouté. |
| Gardes de fusion, avant et après | Une fixture cohérente acceptée ; neuf fixtures refusées avec le motif attendu : fragment absent, couple dupliqué, couple absent, couple étranger, catalogues de couples incohérents, listes de pages différentes, zéro mesure, violation CSP, erreur console. Ces fixtures prouvent les rejets du véritable script de fusion, pas un navigateur. |
| Languages, Parallel 4 | Réussi : quatre fragments de 79 parcours, fusion exhaustive acceptée, 316 mesures, 95 secondes, zéro erreur console et zéro violation CSP. |
| Languages, Parallel 2 | Réussi : quatre fragments non vides, fusion exhaustive acceptée, 316 mesures, 187 secondes, zéro erreur console et zéro violation CSP. |
| Languages, Parallel 1 | Réussi : 316 mesures, 343 secondes, zéro erreur console et zéro violation CSP. |
| Suite complète de bibliothèque, Debug | Première passe par le lanceur : 10 904 réussis et un échec documentaire. Après correction des documents et indexation des sept fichiers, même suite complète : 10 905 réussis, zéro échec et zéro ignoré, TRX contrôlé. |

Les trois passes Languages utilisent de vrais Chromium, une publication Release servie avec sa CSP et un profil privé par navigateur. Les 316 mesures sont **79 pages × 2 langues × 2 largeurs**, pas 316 pages distinctes. La publication a été produite avant la reproduction et réutilisée après : aucun fichier du paquet ou de la vitrine n’est changé par ce lot ; seuls l’orchestration de test et le double sont corrigés.

Commande ciblée, exécutée dix fois avec arrêt immédiat au moindre échec et un dossier TRX distinct par passage :

```text
dotnet test --project tests/OmniEurope.Blazor.Tests/OmniEurope.Blazor.Tests.csproj --configuration Release --no-build --no-progress --filter-class OmniEurope.Blazor.Tests.DisposedWhileLoadingTests --report-trx --report-trx-filename tests.trx --results-directory <dossier-du-passage>
```

Les commandes lourdes .NET utilisent le lanceur protégé du profil ; le lanceur applicatif officiel sérialise sa compilation dans le même créneau. Aucun seuil, baseline, exception ou assertion assoupli.

La première passe complète a refusé un terme interne dans la nouvelle entrée de registre. Le garde réel est conservé : les documents publics sont neutralisés, les références personnelles restent locales. Les nouveaux documents ont été indexés avant de réexécuter ce garde et la suite entière. Deux tests du garde réussissent ensuite, puis les 10 905 tests de la suite complète ; le changement de contenu documenté explique le passage de rouge à vert, sans relance destinée à masquer une intermittence.

## Journaux et limites

Preuves brutes conservées dans le dossier local de preuves du lot 1 daté du 2026-10-30 : `before-restore.log`, `before-build.log`, `publish.log`, `languages-before-p4.log`, `counter-before.log`, `counter-after.log`, `after-build-tests.log`, `targeted-1.log` à `targeted-10.log` et leurs TRX, `shard-guards-before.log`, `shard-guards-after.log`, `languages-after-p4.log`, `languages-after-p2.log`, `languages-after-p1.log`, `library-final.log`, son TRX et le journal du lanceur de la suite bibliothèque. Le harnais de compteur vient de l’audit initial ; sa source et son exécution sont conservées dans les artefacts de cette session. `check-shard-guards.mjs` et ses fixtures sont dans ce dossier de preuves, sans chemins personnels versionnés.

La couverture complète n’est pas remesurée pour ce lot : A360-007, déjà reproduit sur six fichiers, reste ouvert pour le lot 5. Les autres lots du plan et les deux écarts documentaires de la branche du voile ne sont pas corrigés ici. Aucun nouveau nupkg, audit global de dépendances ou parcours Hybrid/Auto n’est revendiqué. Les messages préexistants du lanceur sur les références NuGet et le précontrôle générique du kit restent des signaux distincts des deux défauts corrigés.
