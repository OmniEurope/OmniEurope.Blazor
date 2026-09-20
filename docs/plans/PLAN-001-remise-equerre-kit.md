<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-001 : Remise d'équerre d'OmniEurope.Blazor au standard du kit (phase A)

> Statut : **ouvert**. Plan fils du lot 7 du plan maître `_Generic` PLAN-001, établi le 2026-09-19.
> Lot 1 livré le 2026-09-19, lot 2 livré le 2026-09-20 avec le cœur de lanceur 1.0.1 et les
> exceptions mécanisées. Restent le lot 3 (contrats) et le lot 4 (`STD-I18N` de la vitrine).

## Objectif

Le dépôt suit le standard du kit `_Generic` sans perdre ses propres gardes : `ylaunch.ps1` web réduit
qui lance le catalogue et la vitrine, fichiers racine et squelette `docs/` du kit, plans numérotés
dans `docs/plans/`, et chaque constat mécanisé de `verify-rules.ps1` soit corrigé, soit porté par une
exception justifiée.

## État des lieux (2026-09-19)

- Branche `develop`, à jour avec `origin/develop` avant ce plan. `origin` est GitHub public : rien
  n'est poussé par ce plan. Pré-vol : le `.gitignore` modifié (ligne `/docs/mistakes.md` remplacée par
  `/docs/build-and-ci-pitfalls.md`, aucun secret) est commité tel quel dans `1a3c4a2`.
- Tests (Release, `dotnet test OmniEurope.Blazor.slnx --configuration Release --no-build`, TRX) :
  `OmniEurope.Blazor.Tests` 1477/1477, `OmniEurope.Analyzers.Tests` 9/9, 0 échec, 0 ignoré.
- `verify-rules.ps1 -Root <dépôt> -Warn` : 129 constats, `STD-I18N` 125, `STD-FOCUS` 2, `STD-SDKPIN` 2.
  Aucun constat dans `src/` pour `STD-I18N`.
- `audit-projects.ps1 -Structure` : 8 écarts (`.gitmessage`, `global.json`, `ylaunch.ps1`,
  `docs/adr/README.md`, `docs/plans/README.md`, `docs/retrospectives/README.md`, `docs/contracts/`,
  plans hors `docs/plans` : `plans/`).
- `global.json` : `10.0.401`, `rollForward: latestPatch`, `allowPrerelease: false`. SDK installés :
  `3.1.426`, `10.0.202`, `10.0.303`, `10.0.401` ; le dépôt résout `10.0.401`.
- `plans/` (racine, ignoré par `.gitignore`) : 5 fichiers non suivis, `PLAN-004-grille-complete.md`,
  `PLAN-007-besoins-aetheus.md`, `PLAN-008-execution-log.md`, `PLAN-008-maquette-themes.html`,
  `PLAN-008-themes-palettes-sdk.md`. Le numéro 008 est porté par trois fichiers (le plan, son journal
  et sa maquette).
- Fichiers d'agent volontairement ignorés (dépôt public) : `AGENTS.md`, `CLAUDE.md`, `docs/agents.md`,
  `docs/code-rules.md`, `docs/test-config.md`, `docs/build-and-ci-pitfalls.md`, `.claude/`, `plans/`.
- Hôtes navigables : `samples/OmniEurope.Blazor.Catalog` (Server) et `site/OmniEurope.Blazor.Showcase`
  (WebAssembly). Aucun lanceur avant ce plan (seulement `.claude/launch.json`).

## Décisions (validées le 2026-09-18 dans le plan maître)

- Décision 7 : OE reçoit un `ylaunch.ps1` web réduit, hôtes Catalog et Showcase.
- Déplacements, renommages et suppressions : listés ici et validés par l'utilisateur avant exécution.
- Constats de règles : tri entre vrais constats et faux positifs propres à une bibliothèque ; un
  ajustement de `verify-rules.ps1` se fait dans le kit, pas dans ce dépôt.

## Décisions tranchées le 2026-09-20 (décisions du plan maître du 2026-09-19)

1. **`global.json` et `STD-SDKPIN`** : `latestPatch` conservé, exception portée par
   [ADR-001](../adr/ADR-001-global-json-rollforward-latestpatch.md) et déclarée dans
   `.config/verify-rules.json`. Les quatre gardes restent en place, inchangées.
2. **Extension de propriété dans `ylaunch.ps1`** : retirée. Le cœur du kit 1.0.1 accepte
   `$LaunchConfig.OwnedFolders`, le lanceur déclare `@("src", "samples", "site")` et ne redéfinit plus
   `Get-YOwnPrefix` ni `Test-YOwnedProcess`.
3. **Publication de la documentation de travail** : publiée. `plans/` quitte le `.gitignore`, les cinq
   documents sont suivis dans `docs/plans/`. Le dépôt reste local (aucun push vers GitHub).
4. **`STD-FOCUS` (2)** : classés faux positifs de bibliothèque, documentés dans la section « Focus
   d'ouverture d'une surcouche » de `docs/accessibility-contract.md` et exclus dans
   `.config/verify-rules.json`. Les attributs et les tests sont inchangés.
7. **`CHANGELOG.md`** : une section « Outillage du dépôt (sans effet sur le paquet publié) » est
   ajoutée sous `[Non publié]`.

## Décisions ouvertes

5. **`.github/dependabot.yml`** : l'écosystème `github-actions` reste absent (actions épinglées par
   SHA, vérifiées par `Test-DependencyPolicy.ps1`). À rouvrir si la vérification change.
6. **Documents privés ignorés** : `docs/build-and-ci-pitfalls.md` reste privé, non tranché.

### Historique des décisions 1 à 4 et 7 (énoncé d'origine du 2026-09-19)

1. **`global.json` et `STD-SDKPIN`** (1 constat restant). La règle veut `rollForward: latestFeature`.
   Quatre gardes du dépôt imposent `latestPatch` : `ConventionGuardTests.GlobalJson_PinsTheFeatureBandWithoutAWorkloadVersion`,
   `eng/Test-SdkBand.ps1` (appelé par les deux jobs de `.github/workflows/ci.yml`),
   `eng/dependency-policy.json` (`toolchain.rollForward`) vérifié par `eng/Test-DependencyPolicy.ps1`.
   Raison documentée (`docs/dependencies.md`, `docs/reproducibility.md`, journal du plan local
   PLAN-008 lot 3) : la restauration verrouillée des hôtes WebAssembly échoue en `NU1004` sous une
   autre bande. Choix : garder `latestPatch` et écrire `docs/adr/ADR-001` qui porte l'exception à
   `STD-SDKPIN` (recommandé, conforme à l'ADR-003 du kit), ou passer en `latestFeature` en réécrivant
   les quatre gardes et en acceptant de régénérer les fichiers de verrouillage à chaque bande.
2. **Extension de propriété dans `ylaunch.ps1`**. Le cœur 1.0.0 ne possède que `<racine>\src\` ; les
   hôtes vivent dans `samples\` et `site\`. Sans extension, une relance ne peut pas arrêter
   l'instance précédente (le port est vu comme tenu par une autre extraction d'OE, qui reçoit alors de
   nouveaux ports) et l'arrêt laisse les hôtes tourner. Le `ylaunch.ps1` racine redéfinit donc, après
   le chargement du cœur, `Get-YOwnPrefix` (racine de l'extraction, pour des messages exacts) et
   `Test-YOwnedProcess` (le test du cœur, inchangé, appliqué à `src\`, `samples\` et `site\`). La copie
   du cœur reste verbatim (`verify-launcher-core.ps1` : OK). Choix : garder cette extension jusqu'à un
   cœur du kit qui accepte une liste de dossiers possédés dans `$LaunchConfig` (changement du kit, hors
   de ce dépôt), puis la retirer ; ou la refuser, et alors la relance n'est plus prouvable.
3. **Publication de la documentation de travail**. `origin` est public et `plans/` était ignoré
   volontairement. Ce plan, `docs/plans/README.md`, `docs/adr/`, `docs/contracts/` et
   `docs/retrospectives/` sont commités sur `develop` et seront publiés au prochain push. Choix :
   les publier (standard du kit), ou ignorer `docs/plans/` sauf le registre (écart au kit à porter par
   un ADR). À trancher avant tout push de `develop`.
4. **`STD-FOCUS` (2)** : `src/OmniEurope.Blazor/Components/Actions/OmniSplitButton.razor:22` (menu
   ouvert par clic) et `src/OmniEurope.Blazor/Components/Overlays/OmniDialog.razor:25` (bouton de
   fermeture d'un dialogue modal). Ce sont des focus d'interaction sur une surcouche ouverte par
   l'utilisateur, pas un focus de démarrage ou de navigation ; le registre local adapte `STD-FOCUS`
   dans ce sens et `STD-DIALOG` exige le déplacement du focus dans le dialogue. Le focus réel est géré
   par `omni-focus.js` (`activateMenu`, `restoreFocus`) ; l'attribut du dialogue est figé par
   `OptInEvolutionTests` (ligne 64). Classement : faux positifs de bibliothèque. Choix : documenter
   l'exception dans `docs/accessibility-contract.md` et adopter l'exclusion proposée au lot 4, ou
   retirer les attributs (changement de comportement et de test, hors phase A).
7. **`CHANGELOG.md`** : non modifié. Il documente le paquet NuGet ; ce plan ne change que l'outillage
   du dépôt. Choix : y ajouter une ligne d'outillage sous `[Non publié]` ou non.

## Lots

Chaque lot se termine par un contrôle mesurable avant de passer au suivant.

### Lot 1 - Lanceur, fichiers racine et squelette `docs/` (livré le 2026-09-19)
- [x] `scripts/ylaunch-core.ps1` : copie verbatim du cœur du kit 1.0.0.
- [x] `ylaunch.ps1` depuis le gabarit web du kit : composants `Catalog` (port 5270) et `Showcase`
  (port 5280), suites `Library` (`-tl`) et `Analyzers` (`-tg`), sans base de données, extension de
  propriété (décision ouverte 2). `.ylaunch.local` ajouté au `.gitignore`.
- [x] `.github/dependabot.yml` (décision ouverte 5). Le `.gitmessage` ajouté ce jour-là a été retiré
  depuis : le plan maître abandonne cette exigence partout (décisions complémentaires du 2026-09-19).
- [x] `docs/adr/README.md`, `docs/plans/README.md`, `docs/retrospectives/README.md`,
  `docs/contracts/README.md`, ce plan.
Controle : `verify-launcher-core.ps1 -Path <dépôt>` : `OK version 1.0.0`. `ylaunch.ps1 -t -s` : garde
SDK (`SDK 10.0.401 (floor 10.0.401, latest published 10.0.401)`), ligne `[rules]`, build Debug vert,
`Library` 1477/1477 et `Analyzers` 9/9 (TRX), identiques au décompte Release d'avant. `ylaunch.ps1 -s` :
`Catalog ready`, `Showcase ready`, `GET /` en 200 sur 5270 et 5280 (et `/_framework/blazor.webassembly.js`
en 200). Seconde exécution `-s` : elle arrête les 4 processus de la première (`OmniEurope.Blazor.Catalog.exe`,
le serveur de développement WebAssembly et les deux `dotnet run`), redémarre sur les mêmes ports
avec de nouveaux PID, qui répondent 200 après l'arrêt de la première exécution (sortie 1 de celle-ci,
comportement du cœur quand un composant s'arrête). `verify-rules.ps1` : `STD-SDKPIN` passe de 2 à 1
(129 constats vers 128). `audit-projects.ps1 -Structure` : 8 écarts vers 2 (`global.json`, décision
ouverte 1 ; `plans/`, lot 2). Après le lot, restore verrouillé, build Release à 0 avertissement et
tests Release : 1477/1477 et 9/9, identiques à l'avant.

### Lot 2 - Plans, exceptions mécanisées et cœur de lanceur 1.0.1 (livré le 2026-09-20)
Déplacement de fichiers non suivis (ignorés) vers un dossier suivi : ils deviennent publics au prochain
push (décision 3, tranchée : publiés). Correspondance appliquée, numérotation dense après ce plan :

| Avant (`plans/`) | Après (`docs/plans/`) | État constaté |
|---|---|---|
| `PLAN-004-grille-complete.md` | `PLAN-002-grille-complete.md` | 22 cases cochées sur 22 : terminé |
| `PLAN-007-besoins-aetheus.md` | `PLAN-003-besoins-aetheus.md` | statut « ouvert, aucun lot démarré » du 2026-09-14, à confirmer contre l'état livré |
| `PLAN-008-themes-palettes-sdk.md` | `PLAN-004-themes-palettes-sdk.md` | 11 lots faits selon le journal : terminé |
| `PLAN-008-execution-log.md` | `PLAN-004-execution-log.md` | annexe de PLAN-004 |
| `PLAN-008-maquette-themes.html` | `PLAN-004-maquette-themes.html` | annexe de PLAN-004 (référence visuelle validée) |

- [x] Les 5 fichiers déplacés et renumérotés, titres et références internes mis à jour
  (`plans/PLAN-008-*` devient `PLAN-004-*`), en-tête SPDX ajouté aux trois fichiers qui n'en avaient
  pas. Les plans terminés sont conservés : Git n'a aucun historique d'eux, les supprimer perdrait la
  trace du travail livré.
- [x] `/plans/` retiré du `.gitignore` ; registre `docs/plans/README.md` réécrit (plans actifs et
  plans livrés) ; carte documentaire d'`AGENTS.md` et `docs/agents.md` (fichiers locaux ignorés)
  mises à jour, ainsi que `.claude/plan.md`.
- [x] `docs/adr/ADR-001-global-json-rollforward-latestpatch.md` écrit et indexé ;
  `.config/verify-rules.json` déclare l'exclusion `STD-SDKPIN` (raison : cet ADR) et l'exclusion
  `STD-FOCUS` des deux composants, documentée par une section de `docs/accessibility-contract.md`.
- [x] `scripts/ylaunch-core.ps1` passé au cœur du kit 1.0.1 (copie verbatim) ; l'extension de
  propriété du `ylaunch.ps1` racine (`Get-YOwnPrefix`, `Test-YOwnedProcess`) est remplacée par
  `OwnedFolders = @("src", "samples", "site")` dans `$LaunchConfig`.

Controle du 2026-09-20 : `verify-launcher-core.ps1 -Path <dépôt>` : `OK version 1.0.1`, 1 conforme,
0 écart. `ylaunch.ps1 -t` : `Library` 1477/1477, `Analyzers` 9/9, total 1486, 0 échec, 0 ignoré (TRX),
identiques au décompte d'avant le lot, code de sortie 0. `ylaunch.ps1 -s` : `Catalog ready`,
`Showcase ready`, `GET /` en 200 sur 5270 et 5280. Seconde exécution `-s` : elle nomme et arrête les
4 processus de la première (`dotnet.exe` PID 27092 sous `samples\`, `dotnet.exe` PID 4932 et 19732
sous `site\`, `OmniEurope.Blazor.Catalog.exe` PID 3364 sous `samples\`), la première sort en 1
(`Showcase stopped (state Completed, exit code -1)`) et la seconde répond 200 sur les deux ports.
`verify-rules.ps1 -Warn` : 128 constats avant, 125 après (les 3 exclusions sont imprimées avec leur
raison) ; le reliquat est entièrement `STD-I18N` (lot 4).

### Lot 3 - Contrats vers `docs/contracts/` (à valider)
- [ ] `git mv docs/csp-contract.md docs/contracts/csp-contract.md` et
  `git mv docs/accessibility-contract.md docs/contracts/accessibility-contract.md` (2 fichiers suivis),
  un commit par opération, références mises à jour dans le même commit (`git grep` des deux noms :
  `README.md`, `docs/`, `AGENTS.md` local, `CHANGELOG.md` hors mentions datées).
Controle : build Release et tests avec le même décompte (1477 + 9) ; `git grep` des anciens chemins :
0 hors `CHANGELOG.md` et documents datés.

### Lot 4 - Constats `verify-rules.ps1` restants
Tri des 128 constats d'après le lot 1. Le lot 2 en a réglé 3 par exclusion déclarée : il en reste 125,
tous `STD-I18N`.

| Règle | Nombre | Où | Classement |
|---|---|---|---|
| `STD-SDKPIN` | 1 | `global.json` | exclu au lot 2, raison ADR-001 |
| `STD-FOCUS` | 2 | `src/.../OmniSplitButton.razor:22`, `src/.../OmniDialog.razor:25` | exclus au lot 2, raison `docs/accessibility-contract.md` |
| `STD-I18N` | 9 | `tests/OmniEurope.Blazor.Tests/*TestHost.razor` (`ChartProjectionTestHost` 2, `ChartTestHost` 2, `NumericColumnsTestHost` 2, `NavigationTestHost` 1, `SelectionTestHost` 1, `WizardTestHost` 1) | faux positifs : hôtes de test bUnit, jamais livrés |
| `STD-I18N` | 2 | `site/.../Demos/FormDemo.razor:8` (`Camille Durand`), `:39` (`https://exemple.eu`) | faux positifs : nom propre et URL d'exemple, valeurs non traduisibles |
| `STD-I18N` | 114 | 24 démos de `site/OmniEurope.Blazor.Showcase/Components/Demos/` | vrais constats |

Preuve du classement `STD-I18N` : la bibliothèque (`src/`) n'a aucun constat ; ses textes par défaut
passent par `IStringLocalizer<AppStrings>` (`docs/localization.md`). La vitrine est bilingue :
`Resources/ShowcaseStrings.resx` et `ShowcaseStrings.en.resx` (533 clés chacune), 9 démos déjà
localisées par `IStringLocalizer<ShowcaseStrings>`. Les 114 textes en dur restent en français quand
la vitrine s'affiche en anglais. Le message du kit (`IStringLocalizer<AppStrings>`) ne s'applique pas
à la vitrine : la cible est `ShowcaseStrings`.

- [ ] Localiser les 114 textes des démos dans `ShowcaseStrings.resx` / `.en.resx`, par lots de 15
  fichiers au plus (pages `.razor` : hors phase A, règle 7 du plan maître ; OE n'est pas réécrit par la
  phase B, ce lot reste donc ici).
- [x] `STD-FOCUS` : réglé au lot 2 par `.config/verify-rules.json`, le mécanisme d'exclusion par
  dépôt livré dans le kit le 2026-09-19 (voir les `.NOTES` de `verify-rules.ps1`).
- [ ] Reste à proposer au kit (`_Generic/verify-rules.ps1`, non modifié par ce plan) :
  - `STD-I18N` (et les autres règles d'interface sur `*.razor`) : ignorer les projets de test, par
    exemple `Get-SourceFiles @('*.razor') | Where-Object { $_.FullName -notmatch '\\tests\\|\.Tests\\' }`.
  - `STD-I18N` : ignorer une valeur qui est une URL (`^[a-z]+://`).
  - Nom propre (`Camille Durand`) : pas d'exclusion mécanique sûre ; exception consignée ici.
Controle : `verify-rules.ps1 -Warn` : 0 constat, ou uniquement ceux couverts par une exclusion du
kit ou un ADR de ce dépôt.

## Ordre et dépendances

Lots 1 et 2 livrés. Les lots 3 et 4 sont indépendants ; le reliquat du lot 4 (les 125 constats
`STD-I18N`) dépend soit de la localisation des démos de la vitrine, soit d'une évolution du kit.

## Critère de clôture

Critère de phase A du plan maître : `ylaunch.ps1 -s` lance et relance, `-t` rend un décompte,
`verify-rules.ps1` sans constat ou avec exceptions justifiées dans le kit, `audit-projects.ps1
-Structure` sans écart non consigné, `git status` vide sur `develop`.
