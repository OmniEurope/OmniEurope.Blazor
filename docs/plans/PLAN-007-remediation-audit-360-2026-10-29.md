<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-007 - Remédiation de l'audit 360 du 2026-10-29

Huit constats (A360-001 à A360-008) de l'audit 360 de la révision `ba2f31fe` (1.10.0), relus contre
`develop` le 2026-10-29 : sept confirmés dans le code, l'avis CVE de A360-007 reste à vérifier.
Un point n'est « fait » qu'avec sa preuve observée (navigateur, HTTP réel, PostgreSQL réel).

| Lot | Points | Contenu | Contrôle |
|---|---|---|---|
| A | A360-001 | `OmniSelectBar` : chevrons positionnés aux bords logiques | à 390 px, chevron « suivant » au bord de fin, premier choix lisible au repos, gauche à droite et droite à gauche |
| A | A360-004 | API vitrine : en-têtes de sécurité posés à l'émission de toute réponse | `nosniff`, CSP, `no-store` présents sur 200, 400, 422, 500, 503, corps ProblemDetails conservé |
| A | A360-005 | CI : suite `OmniEurope.Blazor.Showcase.Back.Tests` exécutée avec Docker et TRX | étape présente dans `ci.yml`, suite verte en local |
| A | A360-006 | SBOM : exclusions appliquées sous la racine du dépôt, inventaire vide refusé | `Test-Sbom.ps1` vert depuis un parent caché et depuis un checkout ordinaire |
| A | A360-008 | Docs : budget du paquet à 3 Mio, Dependabot actif | lecture cohérente avec `Test-Budgets.ps1` et `.github/dependabot.yml` |
| B | A360-003 | Admission d'un nouveau visiteur atomique en base (verrou transactionnel PostgreSQL) | `MaxVisitors=1`, rafale de 12 PUT distincts : un seul visiteur en base, trois passes |
| B | A360-002 | Le serveur fait foi pour la date des préférences ; le client adopte la date retenue | horloge cliente à J+1 : la valeur distante plus récente remplace le cache après synchronisation |
| B | A360-007 | Runtime WebAssembly publié aligné sur un pack corrigé, garde de version sur l'artefact | avis vérifié ; version lue dans le `.wasm` publié |

## Journal

- 2026-10-29 : plan ouvert, lots A et B délégués en parallèle en worktrees ; décisions du propriétaire :
  autorité serveur pour les dates (rupture de contrat admise), alignement du runtime WebAssembly.
- 2026-10-29 : les huit points livrés sur `develop`, avec les limites de preuve notées ci-dessous (lots A `60c8e49e`, B `5139cb3a`, A360-004 `a685b10a`,
  garde CI `a2b1cad1`). Suites : bibliothèque 10 743, API 70, garde d'API publique, aucun échec.
  - A360-001 : à 390 px, chevron « suivant » à 323..348 px, « Primary » à 42..115 ; un vrai clic
    défile de 0 à 187 px puis le chevron « précédent » apparaît à gauche ; en `dir=rtl` le chevron
    passe à 42..67.
  - A360-002 : sur Kestrel et PostgreSQL, un PUT daté du 2026-10-30 répond 200 avec la date serveur
    (2026-10-29T10:08:58Z) ; l'écriture suivante, plus récente, l'emporte. Cache navigateur couvert par
    les tests du synchroniseur, non rejoué dans un navigateur avec une horloge décalée.
  - A360-003 : `MaxVisitors` égal au nombre de visiteurs plus un, rafale de 12 PUT de visiteurs distincts :
    un 200, onze 503, un seul visiteur ajouté.
  - A360-004 : sur Kestrel, le 400 porte `nosniff`, CSP, `X-Frame-Options` et `no-store` comme le 200.
    422, 500 et 503 non mesurés.
  - A360-005 : étape CI avec contrôle du TRX ; non exécutée sur un runner GitHub.
  - A360-006 : `Test-Sbom.ps1` vert (160 composants) ; un parent caché échouait avant le correctif.
  - A360-007 : avis GHSA-p3jp-9v83-76w9 vérifié (High, corrigé en 10.0.14). Publications WasmSmoke et
    AutoSmoke en 10.0.14 selon `Test-WasmRuntimeVersion.ps1`, qui a aussi rejeté les fichiers 10.0.13
    restés d'une publication antérieure ; `Test-WasmHost` et `Test-AutoHost` verts.
  - A360-008 : textes alignés sur `Test-Budgets.ps1` et `.github/dependabot.yml`.
- 2026-10-29 : compléments de preuve après la revue de `/next`.
  - A360-004 : les tests d'intégration (API réelle sur PostgreSQL, serveur de test) exigent les en-têtes sur
    422 (quota de clés), 429 (débit), 503 (capacité) et 500 (exception imprévue) ; suite API 71/71.
  - A360-007 : la vitrine publiée (`artifacts/showcase-smoke`) embarque aussi le runtime 10.0.14 selon
    `Test-WasmRuntimeVersion.ps1` ; la garde a son auto-test en CI (`Test-WasmRuntimeVersionFixtures.ps1`).

### Annoncé, non prouvé

- A360-002 : adoption de la date du serveur par le cache, couverte par les tests du synchroniseur (doubles),
  non rejouée dans un navigateur dont l'horloge est décalée.
- A360-005 : étapes CI de la suite API et du runtime, rejouées en local seulement, pas sur un runner GitHub.
