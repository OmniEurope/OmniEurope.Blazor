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
