<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-001 : Remise d'équerre d'OmniEurope.Blazor au standard du kit (phase A)

> Statut : **ouvert**. Plan fils du lot 7 du plan maître `_Generic` PLAN-001, établi le 2026-09-19.
> Lot 1 livré le 2026-09-19 ; lots 2 à 4 en attente de validation de l'utilisateur.

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

## Décisions ouvertes

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
5. **`.github/dependabot.yml`** : modèle du kit sans l'écosystème `docker` (aucune image). L'écosystème
   `github-actions` n'est pas ajouté ; les actions sont épinglées par SHA et `Test-DependencyPolicy.ps1`
   le vérifie. Choix : l'ajouter ou non.
6. **Documents privés ignorés** : `docs/build-and-ci-pitfalls.md` est de nature rétrospective (cause,
   correctif, garde) mais ignoré par Git. Choix : le garder privé, ou le suivre en
   `docs/retrospectives/RET-001-build-and-ci-pitfalls.md` (retrait de la ligne du `.gitignore`).
7. **`CHANGELOG.md`** : non modifié. Il documente le paquet NuGet ; ce plan ne change que l'outillage
   du dépôt. Choix : y ajouter une ligne d'outillage sous `[Non publié]` ou non.

## Lots

Chaque lot se termine par un contrôle mesurable avant de passer au suivant.

### Lot 1 - Lanceur, fichiers racine et squelette `docs/` (livré le 2026-09-19)
- [x] `scripts/ylaunch-core.ps1` : copie verbatim du cœur du kit 1.0.0.
- [x] `ylaunch.ps1` depuis le gabarit web du kit : composants `Catalog` (port 5270) et `Showcase`
  (port 5280), suites `Library` (`-tl`) et `Analyzers` (`-tg`), sans base de données, extension de
  propriété (décision ouverte 2). `.ylaunch.local` ajouté au `.gitignore`.
- [x] `.gitmessage` (modèle du kit), `.github/dependabot.yml` (décision ouverte 5).
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

### Lot 2 - Plans de `plans/` vers `docs/plans/` (à valider)
Déplacement de fichiers non suivis (ignorés) vers un dossier suivi : ils deviennent publics au prochain
push (décision ouverte 3). Correspondance proposée, numérotation dense après ce plan :

| Actuel (`plans/`) | Proposé (`docs/plans/`) | État constaté |
|---|---|---|
| `PLAN-004-grille-complete.md` | `PLAN-002-grille-complete.md` | 22 cases cochées sur 22 : terminé |
| `PLAN-007-besoins-aetheus.md` | `PLAN-003-besoins-aetheus.md` | statut « ouvert, aucun lot démarré » du 2026-09-14, probablement couvert depuis : à confirmer |
| `PLAN-008-themes-palettes-sdk.md` | `PLAN-004-themes-palettes-sdk.md` | 11 lots faits selon le journal : terminé |
| `PLAN-008-execution-log.md` | `PLAN-004-execution-log.md` | annexe de PLAN-004 |
| `PLAN-008-maquette-themes.html` | `PLAN-004-maquette-themes.html` | annexe de PLAN-004 (référence visuelle validée) |

- [ ] Déplacer les 5 fichiers selon la table, mettre à jour leurs références internes
  (`plans/PLAN-008-...` dans PLAN-008 et son journal) et celles des documents actifs.
- [ ] Un plan terminé n'entre pas au registre : variante recommandée, ne déplacer que les plans encore
  ouverts et archiver hors du dépôt les plans terminés, puisque Git n'en a aucun historique.
- [ ] Retirer `/plans/` du `.gitignore` une fois le dossier vide ; mettre à jour la carte
  documentaire d'`AGENTS.md` (fichier local ignoré), qui cite `plans/`.
Controle : `audit-projects.ps1 -Structure` ne signale plus « plans hors docs/plans » ; `git grep "plans/PLAN-"`
ne renvoie que des chemins `docs/plans/`.

### Lot 3 - Contrats vers `docs/contracts/` (à valider)
- [ ] `git mv docs/csp-contract.md docs/contracts/csp-contract.md` et
  `git mv docs/accessibility-contract.md docs/contracts/accessibility-contract.md` (2 fichiers suivis),
  un commit par opération, références mises à jour dans le même commit (`git grep` des deux noms :
  `README.md`, `docs/`, `AGENTS.md` local, `CHANGELOG.md` hors mentions datées).
Controle : build Release et tests avec le même décompte (1477 + 9) ; `git grep` des anciens chemins :
0 hors `CHANGELOG.md` et documents datés.

### Lot 4 - Constats `verify-rules.ps1` restants
Tri des 128 constats restants (après le lot 1) :

| Règle | Nombre | Où | Classement |
|---|---|---|---|
| `STD-SDKPIN` | 1 | `global.json` | décision ouverte 1 |
| `STD-FOCUS` | 2 | `src/.../OmniSplitButton.razor:22`, `src/.../OmniDialog.razor:25` | faux positifs de bibliothèque (décision ouverte 4) |
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
- [ ] Exclusions proposées au kit (`_Generic/verify-rules.ps1`, non modifié par ce plan) :
  - `STD-I18N` (et les autres règles d'interface sur `*.razor`) : ignorer les projets de test, par
    exemple `Get-SourceFiles @('*.razor') | Where-Object { $_.FullName -notmatch '\\tests\\|\.Tests\\' }`.
  - `STD-I18N` : ignorer une valeur qui est une URL (`^[a-z]+://`).
  - `STD-FOCUS` : ignorer `autofocus` dans un fichier dont le balisage porte `role="menu"`,
    `role="dialog"`, `role="alertdialog"` ou `aria-modal="true"` (focus d'interaction d'une
    surcouche), à condition que l'exception soit documentée dans le dépôt.
  - Nom propre (`Camille Durand`) : pas d'exclusion mécanique sûre ; exception consignée ici.
Controle : `verify-rules.ps1 -Warn` : 0 constat, ou uniquement ceux couverts par une exclusion du
kit ou un ADR de ce dépôt.

## Ordre et dépendances

Lot 1 sans prérequis. Le lot 2 attend la décision ouverte 3. Les lots 3 et 4 sont indépendants ; les
exclusions du lot 4 dépendent d'une évolution du kit.

## Critère de clôture

Critère de phase A du plan maître : `ylaunch.ps1 -s` lance et relance, `-t` rend un décompte,
`verify-rules.ps1` sans constat ou avec exceptions justifiées dans le kit, `audit-projects.ps1
-Structure` sans écart non consigné, `git status` vide sur `develop`.
