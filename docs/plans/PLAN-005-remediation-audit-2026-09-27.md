<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-005 - Remédiation de l'audit 360 du 2026-09-27

> Statut : lots 1 à 4 livrés le 2026-09-27 ; restent les points hors dépôt ou à décider.
> Objectif : corriger les constats de l'audit 360 du 2026-09-27 (52 constats relevés sur `develop` à
> `4f2a2c2`) que le tri contradictoire a confirmés. Chaque correctif de comportement arrive avec un test
> qui échoue avant lui.
> Tri : 49 constats confirmés, en tout ou en partie ; 2 relèvent d'une décision du propriétaire ; 1 se
> corrige dans le kit.

## Contraintes

- Tout se fait sur `develop`, sans branche de travail.
- Pas de nouvelle API publique sans nécessité. Une rupture éventuelle est listée sous « Breaking changes »
  dans `CHANGELOG.md`, comme pour la 1.1.0.
- Aucune garde n'est affaiblie pour obtenir un vert.

## Lot 1 - vitrine, hôtes de test et documentation [fait]

HOST-001 à HOST-009, DOC-001 à DOC-006, ROOT-001.

- [x] Hôtes : `aria-busy` retiré de `#app`, plus de focus à la navigation (`STD-FOCUS`), libellés des
      contrôles, pagination de la liste 1-based, bouton Annuler en `Secondary`, apparence de l'aperçu de
      shell liée, validations requises réelles.
- [x] Docs : familles de composants, contrôle Hybrid, budgets et sonde CSP, périmètre Dependabot, menu
      d'en-tête de la grille, exception Monaco dans le README, statuts réels de PLAN-003.
- Contrôle : vitrine et hôtes construits en Release sans avertissement, `Test-Csp.ps1` vert, tests
  unitaires verts.

## Lot 2 - grille et composants de données [fait]

RCL-H009, RCL-H010, RCL-H011, RCL-H012, RCL-016, RCL-002, RCL-H002, RCL-H003, RCL-H004, RCL-H005,
RCL-H008.

- [x] `OmniDataGrid` : préparation bornée dans le temps (images), restauration d'état hors prérendu et
      tolérante aux collections absentes, sélection contrôlée par `Value` multi-pages.
- [x] Rafraîchissements concurrents, maillon nul d'un type valeur, changement de `Load` pendant un
      chargement ou après une erreur (grille, liste, planning).
- [x] Arbre déplié au départ, Kanban suivi par clé, contrat append-only du journal, en-tête de diff hors
      bornes.
- Contrôle : un test par constat, qui échoue avant le correctif ; suite complète verte.
- Relevé : 23 cas de test en échec avant correctif, verts après ; 1927 tests réussis. RCL-H005 est
  corrigé par la documentation seule (contrat append-only). L'attente bornée des images en JavaScript et
  l'annulation à la destruction ne sont vérifiées que statiquement.

## Lot 3 - composants, sécurité et accessibilité [fait]

RCL-001, RCL-003, RCL-007, RCL-010, RCL-015, RCL-B002, RCL-B003, RCL-B004, RCL-B005, RCL-B006,
RCL-B007, SEL-H002, SEL-H003, SEL-H004.

- [x] Tableur : profondeur de formule et chaîne de dépendances bornées, erreur de cellule au-delà.
- [x] URI : `DetailsHref` de notification et `href` passé par `AdditionalAttributes` d'`OmniLink`
      validés ; garde CSP d'attributs rétablie dans `OmniTabsItem` et `OmniTreeItem`.
- [x] Cycle de vie : fuites après `Dispose` (sélecteurs de date, divulgation, presse-papiers),
      désabonnement du formulaire, assistant sans fin obsolète, cascade du service d'overlays.
- [x] Accessibilité et saisie : piège de focus, Couper sans perte, identifiants ARIA uniques,
      autocomplétion synchronisée et inactive quand `Disabled`, attributs de validation de classe.
- Contrôle : un test par constat de comportement C#, une assertion de source pour le JavaScript sans
  exécuteur, suite complète verte, `Test-PublicApi.ps1` vert.
- Relevé : chaque constat C# a un test en échec avant correctif ; 1953 tests réussis, API publique
  inchangée. Le piège de focus et Couper ne sont vérifiés que statiquement (assertions de source). Trois
  ruptures sont listées dans `CHANGELOG.md` : `DetailsHref` non sûr refusé, attributs `style` et `on*`
  refusés sur `OmniTabsItem` et `OmniTreeItem`, identifiants de repli désormais propres à chaque instance.

## Lot 4 - outillage, CI et analyseurs [fait]

ANA-001, API-001, CI-001, ENG-001, ENG-002, ENG-004.

- [x] GEN004 reconnaît `@functions` et une directive après balisage ; la garde d'API sérialise la
      nullabilité des paramètres génériques (baseline régénérée).
- [x] Job Hybrid restauré en Release verrouillé puis construit en `--no-restore` ; serveur statique
      sans rejet non géré ; sondes CDP sur port dynamique ; sonde de densité en échec si rien n'est mesuré.
- Contrôle : tests de l'analyseur et de la garde d'API verts, scripts `eng/` verts en local, CI verte.
- Relevé : tests de l'analyseur (10) et auto-test de la garde d'API en échec avant correctif ; baseline
  régénérée, 8 signatures génériques retrouvent leur `?`. Sondes Catalog, WebAssembly et Auto vertes sur
  port dynamique ; serveur statique : `/%ZZ` renvoie 400 au lieu d'arrêter le processus.

## Hors audit : sonde de densité

- [x] La sonde manuelle de densité échouait sur 182 éléments (4 sélecteurs). Les entrées du menu latéral
      et les étoiles de notation dérivent désormais des jetons de densité ; la pastille de légende des
      graphiques est exemptée comme marque de texte. Sonde verte : 1240 éléments sur 39 pages.

## Hors dépôt ou à décider

- KIT-001, KIT-002 : `docs/code-rules.md` et `docs/agents.md` sont ignorés par Git ; corrigés en local
  seulement.
- SCR-001 : `-ta` de le lanceur local échoue sans bloc E2E ; le défaut est dans le cœur du kit
  le kit (copie verbatim), il se corrige là-bas puis se recopie.
- [x] DOC-007 : décision du propriétaire du 2026-09-27, les couleurs d'Essentiel restent ; la règle
  d'indépendance d'`AGENTS.md` (fichier local) précise que des valeurs isolées sont des données, et
  PLAN-004 consigne la décision.
- [x] ENG-003 : la dérive du catalogue NuGet sort désormais en avertissement (`::warning::` en CI),
  conformément à `STD-SDKPIN` ; les pins, les entrées de politique et `reviewedAt` restent bloquants.
