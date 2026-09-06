# Registre de correction de l'audit 360

> Baseline: `.claude/audit/2026-08-11/audit-report.html`
> Plan: `plans/PLAN-003-correction-findings-audit.md`
> Last updated: 2026-08-11
> Status: 324/325 findings fermés

## Audit frais de remédiation

> Baseline fraîche: `.claude/audit/2026-08-11-remediation/AUDIT_SUMMARY.md`
> Findings consolidés: 77 (0 critique, 15 élevés, 46 moyens, 16 faibles)
> Status frais: 19/77 corrigés et vérifiés

| Lot | Périmètre | Statut | Preuve |
| --- | --- | --- | --- |
| 23 | 10 findings bibliothèque `OE-BLAZOR-001` à `009`, puis `011` | Terminé | i18n et Disabled 35/35; Scheduler/overlay/DataGrid/DatePicker 51/51; graphiques/conventions/budgets 33/33 |
| 24 | 9 findings des hôtes Catalog, Auto, Hybrid et Wasm | Terminé | Build solution 0/0 et Hybrid 0/0; Catalog fr/en + CDP; Auto fr/en + CDP; WASM langue/titre/cache/CSP + CDP; Hybrid CSP/langue/titre + WebView2 |
| 25 | Analyseurs, PublicApiGuard et gates anti-faux-vert | À faire | - |
| 26 | Dépendances, workload, actions, SBOM, licences, package et provenance | À faire | - |
| 27 | Preuves de tests, architecture et registre de conventions | À faire | - |
| 28 | Documentation, plans, backlogs, inventaires et générateurs | À faire | - |

Chaque lot ne peut être coché qu'après correction de tous ses IDs, preuve ciblée et exécution des gates pertinentes.

| Lot | Findings | Statut | Preuve |
| --- | --- | --- | --- |
| 01 | `A360-001` à `A360-015` | Terminé | Build Release 0/0; tests 79/79; sonde HTTP CSP 415/413/204, plafond 100, aucune fuite |
| 02 | `A360-016` à `A360-030` | En cours (14/15) | Build 0/0; tests 84/84; CSP 148 fichiers; 3 publications; sondes Server/Auto; `A360-023` attend l'activation GitHub privée |
| 03 | `A360-031` à `A360-045` | Terminé | Build Release 0/0; tests 89/89; overlays 6/6; API 540 signatures; CSP 154 fichiers |
| 04 | `A360-046` à `A360-060` | Terminé | Build Release 0/0; tests 92/92; navigateur focus/Tab/Échap/expiration/CSS/SVG; CSP 0 |
| 05 | `A360-061` à `A360-075` | Terminé | Build 0/0; tests 100/100; API 1 130; Auto et Hybrid CDP interactifs; catalogue 8/8 labels; CSP 163 fichiers |
| 06 | `A360-076` à `A360-090` | Terminé | Build 0/0; tests 107/107; API 1 134; Tabs Chromium; corpus 32 projets/4 607 fichiers; six sorties déterministes |
| 07 | `A360-091` à `A360-105` | Terminé | Build 0/0; tests 119/119; API 1 134; pile de dialogues Chromium; CSP 165 fichiers; Hybrid 0/0 |
| 08 | `A360-106` à `A360-120` | Terminé | Build 0/0; tests 148/148; API 1 134; GEN001-GEN008 actifs; zéro `@code` inline; trois hôtes interactifs; CSP 280 fichiers |
| 09 | `A360-121` à `A360-135` | Terminé | Build 0/0; tests 154/154; ressources 57/57; API 1 134; CSP 280 fichiers; zéro littéral français ciblé |
| 10 | `A360-136` à `A360-150` | Terminé | Build 0/0; tests 160/160; ressources 80/80; API 1 134; CSP 280 fichiers; zéro littéral français ciblé |
| 11 | `A360-151` à `A360-165` | Terminé | Build 0/0; tests 166/166; ressources bibliothèque 114/114 et Wasm 5/5; API 1 134; CSP 281 fichiers |
| 12 | `A360-166` à `A360-180` | Terminé | Build 0/0; tests 171/171; API 1 135; zéro Razor inline; Wasm, Auto et Hybrid interactifs; CSP 281 fichiers |
| 13 | `A360-181` à `A360-195` | Terminé | 15/15 code-behind présents; 0 inline; GEN004 error; build 0/0; tests 171/171; API 1 135 |
| 14 | `A360-196` à `A360-210` | Terminé | 15/15 code-behind présents; 0 inline; GEN004 error; build 0/0; tests 171/171; API 1 135 |
| 15 | `A360-211` à `A360-225` | Terminé | 15/15 code-behind présents; 0 inline; GEN004 error; build 0/0; tests 171/171; API 1 135 |
| 16 | `A360-226` à `A360-240` | Terminé | 15/15 code-behind présents; 0 inline; GEN004 error; build 0/0; tests 171/171; API 1 135 |
| 17 | `A360-241` à `A360-255` | Terminé | 15/15 code-behind présents; 0 inline; GEN004 error; build 0/0; tests 171/171; API 1 135 |
| 18 | `A360-256` à `A360-270` | Terminé | 15/15 code-behind présents; 0 inline; GEN004 error; build 0/0; tests 171/171; API 1 135 |
| 19 | `A360-271` à `A360-285` | Terminé | 7/7 code-behind; cibles tactiles 44 px; 33 types séparés; conventions 14/14; API 1 135 |
| 20 | `A360-286` à `A360-300` | Terminé | conventions 19/19; scripts 17/17 ASCII; corpus 32/4 607; couverture 110/110; provenance NuGet valide |
| 21 | `A360-301` à `A360-315` | Terminé | inventaires v2 vérifiés; faux positifs 0/10; catalogue Chromium; package contaminé rejeté; API 1 135 |
| 22 | `A360-316` à `A360-325` | Terminé | couverture Cobertura 181/181 et 86,64%; 114 packages SBOM/licences; actions SHA; dépendances/toolchain validés |

## Lot 01 - A360-001 à A360-015

Statut: terminé le 2026-08-11.

- `A360-001`, `A360-006`: bornes Scheduler calculées avec les offsets du fuseau ciblé et horloge `TimeProvider` contrôlable; test DST de 23 heures et action Aujourd'hui déterministe.
- `A360-002`, `A360-007`: recompilation des expressions et réabonnement lors du remplacement du champ, du modèle ou de l'`EditContext`; test de remplacement et désabonnement de l'ancien contexte.
- `A360-003`: projection locale unique filtre, tri, total et page avec invalidation explicite; test page 2 vers filtre mono-résultat.
- `A360-004`: matrice mensuelle alignée sur le premier jour de semaine de la culture, avec cellules extérieures et en-têtes; test `fr-FR` pour août 2026.
- `A360-005`: comparaison des chemins URI normalisés sans query string ni fragment; test de route active.
- `A360-008`: collecte CSP limitée à 16 384 caractères et 100 rapports, types contrôlés, annulation propagée, contenu brut absent de `/csp-status`; preuve HTTP réelle `415`, `413`, `204`, compteur `100`.
- `A360-009` à `A360-012`: politique URI partagée autorisant relatifs, HTTP(S), mail et téléphone, et refusant les schémas actifs; tests sur les quatre composants.
- `A360-013`: exception Upload journalisée mais jamais rendue; message public remplaçable et test avec chemin secret.
- `A360-014`: tout attribut supplémentaire `on*` est refusé indépendamment du type; tests `int`, `bool` et `MarkupString`.
- `A360-015`: sanitiseur regex remplacé par `HtmlSanitizer` 9.1.973 avec allowlist stricte et parseur AngleSharp; corpus XSS et mXSS étendu.

Preuves de lot:

- Build Release gardé: 0 avertissement, 0 erreur.
- Tests gardés: 79 réussis, 0 échoué, 0 ignoré.
- Sonde HTTP locale du catalogue: mauvais type `415`, corps surdimensionné `413`, rapport valide `204`, état `500` avec `violations=100`, propriété `reports` absente.

## Lot 02 - A360-016 à A360-030

Statut: 14 findings fermés sur 15; `A360-023` reste ouvert.

- `A360-016`: suppression du chemin Data Protection temporaire explicite; les hôtes utilisent le magasin protégé par le système. Les sondes Production réelles sont vertes avec DPAPI.
- `A360-017`, `A360-025`: HSTS hors développement et en-têtes `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy` ajoutés et vérifiés sur les réponses HTTP du catalogue et d'Interactive Auto.
- `A360-018`: `/csp-status` n'expose plus les rapports bruts; uniquement un état et un compteur borné à 100.
- `A360-019`: `OmniUploadRequest.OpenReadStream` impose la taille maximale et le hook `Validate` permet l'inspection serveur du flux avant transport; contrat de signature/format documenté et testé.
- `A360-020`, `A360-021`: toutes les actions CI et publication sont épinglées sur des SHA complets officiels, versions commentées, avec Dependabot hebdomadaire pour les mises à jour revues.
- `A360-022`: scanner étendu aux événements HTML, URI `javascript:`, ressources et imports distants; fixtures sûre/malveillante exécutées avant le scan des 148 sources.
- `A360-024`: `frame-ancestors` retiré de la balise meta et livré dans `_headers`; manifeste publié contrôlé.
- `A360-026`, `A360-027`: `connect-src` borné à `'self'` dans les trois politiques; absence de `ws:` et `wss:` vérifiée sur les hôtes et le manifeste WebAssembly.
- `A360-028`: contexte de graphique partagé pour axes, domaines, projection et cumuls positifs/négatifs; tests SVG sur domaine `-50..150` et empilement `20+30` / `-10-15`.
- `A360-029`: archétype RCL et adaptation explicite du kit formalisés dans `.claude/code-rules.md`, reliés depuis `CLAUDE.md`.
- `A360-030`: infrastructure `IStringLocalizer<AppStrings>`, ressources française/anglaise, extension DI et tests de résolution ajoutés.
- `A360-023` ouvert: politique enrichie avec versions, délais, lien privé et procédure de repli, mais l'activation du canal privé GitHub n'est pas vérifiable sans session administrateur authentifiée. La page de réglage a répondu `404` en session non connectée; aucune preuve de fermeture n'est revendiquée.

Preuves partielles du lot:

- Build Release verrouillé: 0 avertissement, 0 erreur.
- Tests gardés: 84 réussis, 0 échoué, 0 ignoré.
- CSP: fixture sûre acceptée, fixture malveillante rejetée, 148 fichiers source acceptés.
- Publications Release: catalogue, WebAssembly et Interactive Auto réussies.
- WebAssembly: `_headers` publié avec `frame-ancestors 'none'`, `connect-src 'self'` et trois en-têtes de défense.
- Sondes HTTP Production: catalogue et Interactive Auto répondent 200 avec CSP stricte, assets et en-têtes de sécurité.

## Lot 03 - A360-031 à A360-045

Statut: terminé le 2026-08-11.

- `A360-031` à `A360-035`, `A360-040`, `A360-041`, `A360-043`: contexte de projection partagé entre le graphique, ses axes et ses séries; domaines explicites ou calculés, cumuls positifs/négatifs et baselines empilées cohérents. Les tests SVG couvrent le domaine `-50..150` et les piles positives/négatives.
- `A360-036`: cycle de vie des overlays centralisé dans `OmniOverlayCoordinator`; les dialogues sont empilés, les menus contextuels passent par le portail central et Escape les retire. Une boucle de rendu détectée pendant la suite complète a été corrigée puis les tests ont été rejoués.
- `A360-037`, `A360-045`: projection locale pure extraite dans `GridProjection<TItem>` et état de chargement distant annulable extrait dans `GridRemoteState<TItem>`; tests directs du filtrage, tri stable, pagination et conservation de la génération la plus récente.
- `A360-038`, `A360-042`: les types de coordination DataGrid, Tabs, Steps et Tree sont désormais internes; garde de réflexion et baseline API publique mises à jour.
- `A360-039`: sources des composants réparties dans 12 familles stables sans modifier leur namespace public.
- `A360-044`: `OmniComponentsHost` délègue les dialogues, notifications et le portail à des hôtes internes séparés.

Preuves de lot:

- Build Release verrouillé: 0 avertissement, 0 erreur.
- Tests gardés: 89 réussis, 0 échoué, 0 ignoré.
- Tests ciblés overlays: 6 réussis, dont pile de dialogues et portail de menu contextuel.
- API publique: baseline valide de 540 signatures et garde sémantique des contextes internes.
- CSP: fixture sûre acceptée, fixture malveillante rejetée, 154 fichiers source acceptés.

## Lot 04 - A360-046 à A360-060

Statut: terminé le 2026-08-11.

- `A360-046`, `A360-049`: les contextes DataGrid, Tabs et Steps avaient déjà été rendus internes et protégés par le test de frontière publique du lot 03.
- `A360-047`, `A360-048`: LineSeries et Markers consomment la projection partagée; les longueurs SVG utilisent désormais une culture invariante, vérifiée sous `fr-FR` et dans Chromium avec trois rayons `1.5`.
- `A360-050`: dialogues et notifications utilisent des stores internes distincts; les notifications sont bornées à cinq par défaut, remplacent la plus ancienne, expirent après sept secondes et annulent proprement leurs temporisations.
- `A360-051`, `A360-053`, `A360-054`: les sources sont rangées selon la taxonomie canonique de 12 familles; architecture, carte des familles et catalogue sont alignés, avec direction des dépendances explicitée.
- `A360-052`: les types de preuve du sample Hybrid sont internes.
- `A360-055`: le menu contextuel prend le focus, navigue par flèches/Home/End, ferme sur Échap et restaure le déclencheur via le module statique partagé.
- `A360-056`: le dialogue capture et restaure automatiquement le focus; Tab boucle sur la liste DOM réelle des éléments focalisables et les sentinelles en sont exclues.
- `A360-057`: le split button focalise son menu, gère flèches/Home/End/Échap et restaure son déclencheur.
- `A360-058`: `app.css` est maintenant lié dans l'hôte Auto après la feuille de la RCL.
- `A360-059`: le faux chemin asynchrone de sélection Autocomplete a été supprimé.
- `A360-060`: une seule définition canonique de `.omni-visually-hidden` subsiste.

Preuves de lot:

- Build Release verrouillé complet: 0 avertissement, 0 erreur.
- Tests gardés: 92 réussis, 0 échoué, 0 ignoré; overlays ciblés 8/8.
- API publique: baseline valide de 540 signatures.
- CSP: fixture sûre acceptée, fixture malveillante rejetée, 156 fichiers source acceptés.
- JavaScript: module de focus validé par `node --check`.
- Chromium réel: focus initial sur Fermer, Tab piégé, Échap ferme, focus restauré sur Ouvrir le dialogue, notification expirée, deux CSS chargées, rayons SVG `1.5`, aucune erreur console applicative et `/csp-status` à 0 violation.

## Lot 05 - A360-061 à A360-075

Statut: terminé le 2026-08-11.

- `A360-061`: le callback de la sonde WebAssembly suit la convention asynchrone avec `IncrementAsync`.
- `A360-062`: le DataGrid construit un instantané de rendu pour les colonnes visibles, les index de sélection, d'expansion et de groupes, au lieu de répéter des projections linéaires dans chaque cellule.
- `A360-063`: la sonde Interactive Auto lance Chromium par CDP, attend l'interactivité réelle, clique `#auto-action`, constate `Compteur Auto : 1` et exige une console sans erreur. La CSP autorise précisément `'wasm-unsafe-eval'` sans ouvrir `'unsafe-eval'`.
- `A360-064`: les huit contrôles du formulaire du catalogue sont associés à des libellés natifs. `OmniUpload.InputId` permet notamment au label Fichier de viser l'`input[type=file]` plutôt que son conteneur.
- `A360-065`: Hybrid est désormais une application MAUI Windows exécutable minimale. `BlazorWebView` charge `HybridSmoke`; la sonde CDP clique `#hybrid-action`, constate `1` et exige une console sans erreur.
- `A360-066`, `A360-067`: DataGrid et DataList distinguent un résultat distant vide d'un état jamais chargé, mémorisent le delegate observé et rechargent lors de son remplacement; tests dédiés avec résultats vides.
- `A360-068`: la clé Scheduler inclut plage calculée, vue, fuseau et delegate; le test couvre les chargements vides et l'invalidation de clé.
- `A360-069`: l'extraction regex a été remplacée par un outil .NET sémantique sur l'assembly compilé. Il couvre types, héritage, interfaces, génériques et contraintes, constructeurs, propriétés et paramètres Blazor, champs, événements, méthodes et opérateurs, avec auto-test intégré.
- `A360-070`: les tests de graphiques affirment explicitement le domaine `-50..150` et les géométries empilées positives et négatives.
- `A360-071`: le manifeste WebAssembly et l'hôte Auto autorisent la source CSP ciblée `'wasm-unsafe-eval'`; les gardes l'exigent et continuent de refuser `'unsafe-eval'` et `'unsafe-inline'`.
- `A360-072`: trois gardes de convention protègent l'état actif de navigation, le `type` explicite des boutons HTML et le contrat de focus/Échap/Tab des dialogues.
- `A360-073`: le contenu du catalogue est enveloppé dans une `ErrorBoundary` avec état d'erreur localisé et action de récupération.
- `A360-074`: les deux références MAUI sont coordonnées en 10.0.90; verrou régénéré, restauration verrouillée et compilation Windows réussies.
- `A360-075`: Autocomplete capture les échecs de recherche, annule les résultats obsolètes, rend un message récupérable sans détail d'exception et expose `SearchFailed`; test avec secret non divulgué.

Preuves de lot:

- Restaurations verrouillées de la solution et du sample Hybrid: réussies.
- Build Release verrouillé complet: 0 avertissement, 0 erreur; build Hybrid Windows: 0 avertissement, 0 erreur.
- Tests gardés: 100 réussis, 0 échoué, 0 ignoré.
- API publique: auto-test et baseline sémantique valides de 1 130 signatures.
- CSP: fixtures sûre/malveillante valides, 163 fichiers source acceptés, manifeste WebAssembly validé.
- Chromium Interactive Auto: compteur à 1 après hydratation, console sans erreur.
- WebView2 MAUI Hybrid: compteur à 1 après clic, console sans erreur.
- Chromium catalogue: huit contrôles visibles associés à leurs libellés natifs, aucune erreur console.

## Lot 06 - A360-076 à A360-090

Statut: terminé le 2026-08-11.

- `A360-076`: chaque colonne DataGrid compare sa définition au cycle de paramètres, réenregistre les changements observables et désenregistre l'ancienne clé ou l'ancien contexte; test de changement de titre après le premier rendu.
- `A360-077`: Slider refuse bornes non finies ou inversées, pas non positif et valeur initiale hors plage; trois cas invalides testés.
- `A360-078`: Steps utilise la sémantique de liste d'étapes plutôt qu'un faux patron Tabs; l'étape courante expose `aria-current="step"` et chaque région est nommée par son bouton.
- `A360-079`: Tabs expose `role="tablist"`, nom accessible et tabindex roving. Le module statique gère flèches/Home/End, ignore les tabs désactivés, empêche le scroll, déplace le focus et synchronise la valeur Blazor par une méthode JS invokable.
- `A360-080`: les indisponibilités JS attendues lors du focus d'un formulaire invalide sont contenues et journalisées sans perdre les messages de validation; test avec `JSException`.
- `A360-081`: le wrapper Tooltip n'ajoute plus de tabulation par défaut; l'hôte opte explicitement pour `TabIndex="0"` lorsque son contenu non interactif doit devenir focalisable.
- `A360-082`: TreeItem synchronise les changements contrôlés d'expansion, invalide un loader réellement remplacé et expose l'exception exacte via `LoadFailed` tout en conservant un message public générique.
- `A360-083`: le gestionnaire de changement de champ n'est plus `async void`; la tâche différée contient annulation et exception, cette dernière étant observable via `ValidationFailed`, tandis que la validation synchrone explicite reste testable.
- `A360-084` à `A360-089`: `docs/radzen-corpus.json` manifeste 32 projets et 4 607 fichiers avec statuts, révisions, chemins propriétaires et SHA-256. Les générateurs ne balayent plus implicitement `C:\Dev`, vérifient strictement le manifeste, séparent actif/modèle/archivé/miroir, publient les empreintes et refusent doublons ou dérives. Les six sorties sont byte-stables sur deux régénérations.
- `A360-090`: ArcGauge borne désormais la valeur affichée et la géométrie; tests sous le minimum et au-dessus du maximum contre les chemins normalisés 0 et 100.

Preuves de lot:

- Build Release verrouillé: 0 avertissement, 0 erreur.
- Tests gardés: 107 réussis, 0 échoué, 0 ignoré.
- API publique: baseline sémantique valide de 1 134 signatures.
- CSP: 163 sources acceptées; module Tabs validé par `node --check`.
- Chromium: ArrowRight passe Liste vers Arbre, focus et tabindex suivent, panneau Arbre visible, `scrollY` reste 1527 et console sans erreur.
- Corpus: 32 projets uniques, quatre statuts, 4 607 fichiers uniques empreintés; références de manifeste cohérentes et aucune entrée surface dupliquée.
- Régénération: component inventory, surface inventory et contrats, en JSON et Markdown, ont conservé exactement leurs six SHA-256 au second passage.

## Lot 07 - A360-091 à A360-105

Statut: terminé le 2026-08-11.

- `A360-091`: le test distant DataGrid déclenche deux chargements réellement chevauchés, capture les deux jetons, exige l'annulation du premier et conserve le résultat du dernier.
- `A360-092`: tri, sélection, filtrage et pagination sont séparés; le scénario de pagination change effectivement de page et vérifie les lignes et l'index obtenus.
- `A360-093`: des scénarios dédiés couvrent regroupement, détail expansible, sauvegarde d'édition et redimensionnement de colonne.
- `A360-094`: l'attente murale fixe du validateur est remplacée par une attente fonctionnelle bornée avec `WaitForAssertion`.
- `A360-095`: le service conserve toutes les entrées de la pile; les dialogues sous-jacents restent connectés mais `inert`, leurs IDs sont uniques et la restauration de focus attend de façon bornée la fin du rendu. Chromium prouve deux dialogues, l'arbitrage d'Échap, le verrouillage du scroll et les deux restaurations de focus.
- `A360-096`: les budgets possèdent warm-up, cinq échantillons avec médiane, isolation de collection, collections GC explicites et compteur d'allocations global au processus; la distinction entre garde CI et benchmark reproductible est documentée.
- `A360-097`: les tests DST vérifient offsets locaux, durées en minutes, positions de chevauchement au printemps et double heure ambiguë avec offsets distincts en automne.
- `A360-098`: le test Scheduler provoque deux chargements chevauchés et exige l'annulation du premier jeton.
- `A360-099`: trois saisies Autocomplete rapprochées s'exécutent concurremment; une seule recherche est observée avec le dernier terme.
- `A360-100`: une page attrape-tout accessible fournit titre, explication et retour au catalogue; la sonde HTTP exige son rendu en 200.
- `A360-101`: `OmniGridLines` refuse les comptes nuls ou négatifs; les deux cas sont testés.
- `A360-102`: le focus invalide consulte `prefers-reduced-motion` et choisit `auto` au lieu de `smooth`; un garde de convention protège ce contrat.
- `A360-103`, `A360-104`: `global.json` impose SDK et workload set `10.0.302` sans roll-forward; la CI installe MAUI, vérifie exactement les deux versions puis restaure et compile Hybrid. Le poste local confirme SDK `10.0.302`, mode `workload-set` et build Hybrid réussi.
- `A360-105`: les scénarios Pager et Tree sont séparés entre contrôle de page, souris, clavier et ARIA.

Preuves de lot:

- Build Release gardé de la solution: 0 avertissement, 0 erreur; build MAUI Hybrid gardé: 0 avertissement, 0 erreur.
- Tests gardés: 119 réussis, 0 échoué, 0 ignoré.
- API publique: baseline sémantique valide de 1 134 signatures; l'adaptateur JS Tabs reste privé.
- CSP: fixtures sûre/malveillante valides et 165 fichiers source acceptés; les deux modules JavaScript passent `node --check`.
- Sonde HTTP Production: accueil, route inconnue accessible, en-têtes, assets, CSP stricte et zéro violation validés.
- Chromium: focus initial `Fermer`; deux dialogues dont un seul actif et l'autre `inert`; premier Échap restaure `#open-nested-dialog`; second Échap restaure `Ouvrir le dialogue`; overflow `hidden` puis `visible`; `scrollY` stable à 166; aucune interface d'erreur Blazor.

## Lot 08 - A360-106 à A360-120

Statut: terminé le 2026-08-11.

- `A360-106`: les matrices homogènes de composants Foundation sont devenues des théories nommées; les comportements de contenu, layout, feedback et bornes invalides sont isolés.
- `A360-107`: le test LargeSelector rerend réellement sa source de 10 000 à 10 001 options puis sélectionne et vérifie la nouvelle dernière valeur.
- `A360-108`: un analyseur Roslyn du dépôt implémente `GEN001` à `GEN008`, est référencé par tous les projets, reçoit les Razor applicables et promeut les diagnostics convenus. Une preuve négative a fait échouer le build sur un `@code` temporaire avec `GEN004`.
- `A360-109`: `.claude/code-rules.md` fournit l'overlay Omni traçable des règles incompatibles avec le remplacement clean-room de Radzen et documente l'exception étroite des fixtures Razor de tests non livrées.
- `A360-110`, `A360-112`, `A360-114`, `A360-116`: AutoSmoke, Catalog et Hybrid utilisent des ressources françaises et anglaises pour leurs textes humains; une garde exige les clés équivalentes et interdit les littéraux audités dans le markup.
- `A360-111`, `A360-113`: AutoProbe et Home utilisent un code-behind; la migration globale a déplacé les blocs inline de 108 composants de production.
- `A360-115`: `Program` n'est plus artificiellement partiel et `CspViolationStore` réside dans son fichier avec une exposition interne.
- `A360-117` à `A360-120`: AppearanceToggle, ArcGauge, Autocomplete et Breadcrumb résolvent leurs valeurs par défaut depuis les ressources de la bibliothèque, tout en conservant les surcharges publiques.

Preuves de lot:

- Structure Razor: 120 fichiers, zéro bloc `@code` inline et 111 code-behind; zéro littéral audité restant dans les quatre markups ciblés.
- Build Release gardé de la solution: 0 avertissement, 0 erreur; build MAUI Hybrid gardé: 0 avertissement, 0 erreur.
- Tests gardés: 148 réussis, 0 échoué, 0 ignoré, dont comportements localisés `fr-FR` et `en-US`.
- Analyseurs: `GEN001` à `GEN008` chargés; preuve négative `GEN004` observée puis source temporaire retirée.
- API publique: baseline sémantique valide de 1 134 signatures.
- CSP: fixtures sûre/malveillante valides et 280 fichiers source acceptés.
- Chromium Catalog et Interactive Auto, puis WebView2 MAUI Hybrid: interaction réelle réussie et consoles sans erreur.

## Lot 09 - A360-121 à A360-135

Statut: terminé le 2026-08-11.

- `A360-121`: les deux descriptions d'infobulle Chart utilisent les ressources et le paramètre public `Description` reste prioritaire lorsqu'il est fourni.
- `A360-122` à `A360-125`: CheckBox, CheckBoxList, ColorPicker et CompareValidator résolvent leurs messages de validation par culture; `Message` conserve sa surcharge publique.
- `A360-126`: le nom accessible de la région de notifications est fourni par `OmniStrings` dans l'hôte interne extrait.
- `A360-127`: ContextMenu localise son nom accessible dans le rendu direct comme dans le portail, avec priorité à `MenuLabel` explicite.
- `A360-128`: DataGrid localise chargement, erreur, relance, colonnes techniques, filtres, redimensionnement, état vide, expansion, sélection et actions d'édition, y compris les formats contenant le titre de colonne.
- `A360-129`: DataList localise chargement, erreur, relance et état vide sans retirer les fragments personnalisables existants.
- `A360-130`: DatePicker fournit son erreur de parsing depuis les ressources.
- `A360-131`: DayView localise son nom accessible par défaut et conserve la surcharge `Label`.
- `A360-132`: Dialog localise les deux sentinelles et le bouton de fermeture, tout en conservant `CloseLabel` prioritaire.
- `A360-133`: DropDown localise le placeholder et l'erreur de sélection paramétrée par nom de champ, avec priorité au placeholder fourni.
- `A360-134`: EmailValidator localise son message par défaut et conserve le paramètre `Message` prioritaire.
- `A360-135`: HtmlEditor localise son nom, la toolbar, ses huit commandes et l'aperçu; le label public explicite reste prioritaire.

Preuves de lot:

- Ressources: 57 clés françaises et 57 anglaises, ensembles strictement identiques.
- Garde source: aucun littéral français ni caractère accentué dans les sources ciblées du lot.
- Tests de localisation et conventions: 8/8; rendu réel vérifié sous `fr-FR` et `en-US`.
- Build Release gardé de la solution: 0 avertissement, 0 erreur.
- Suite globale gardée: 154 réussis, 0 échoué, 0 ignoré.
- API publique: baseline sémantique valide de 1 134 signatures.
- CSP: fixture sûre acceptée, fixture malveillante rejetée et 280 fichiers source acceptés.

## Lot 10 - A360-136 à A360-150

Statut: terminé le 2026-08-11.

- `A360-136`, `A360-139`, `A360-146`, `A360-148`: Legend, MonthView, PanelMenu et ProfileMenu résolvent leurs noms accessibles par culture et conservent les labels explicitement fournis.
- `A360-137`, `A360-138`, `A360-141`, `A360-143`, `A360-150`: LengthValidator, ListBox, NullableCheckBox, Numeric et RadioButtonList utilisent des messages localisés. Numeric ne divulgue plus le nom technique du champ et emploie `DisplayName` lorsqu'il existe.
- `A360-140`: le bouton de notification utilise une ressource pour son nom accessible.
- `A360-142`: NullableSwitch localise sa description d'état indéterminé et son erreur de parsing tout en conservant la surcharge publique.
- `A360-144`: le nouveau défaut de `OmniDialogRequest.CloseLabel` est une sentinelle vide résolue par `OmniDialog`; la frontière de rendu reconnaît aussi l'ancien défaut français produit par des clients alpha déjà compilés.
- `A360-145`: Pager localise son nom de navigation, les boutons précédent/suivant et le statut paramétré par les numéros de page.
- `A360-147`: Password localise les quatre textes afficher/masquer et conserve chaque surcharge publique.
- `A360-149`: ProgressBar localise son nom accessible et son format visible de pourcentage avec la culture UI; `ValueText` reste prioritaire.

Preuves de lot:

- Ressources: 80 clés françaises et 80 anglaises, ensembles strictement identiques.
- Garde source: aucun littéral français ni caractère accentué dans les sources ciblées du lot; aucun usage de `FieldIdentifier.FieldName` dans Numeric.
- Tests de localisation et conventions: 9/9; rendus `fr-FR` et `en-US`, contrat de dialogue à la frontière et formats de progression vérifiés.
- Build Release gardé de la solution: 0 avertissement, 0 erreur.
- Suite globale gardée: 160 réussis, 0 échoué, 0 ignoré.
- API publique: baseline intentionnellement régénérée puis validée à 1 134 signatures; seule la valeur optionnelle du défaut de dialogue évolue vers la sentinelle localisable.
- CSP: 280 fichiers source acceptés; fixtures sûre et malveillante déjà validées dans la phase.

## Lot 11 - A360-151 à A360-165

Statut: terminé le 2026-08-11.

- `A360-151`, `A360-153`, `A360-156`, `A360-159`: RequiredValidator, SelectBar, Slider et Switch résolvent leurs erreurs par culture; le paramètre `Message` du validateur requis reste prioritaire.
- `A360-152`: Scheduler localise navigation, Aujourd'hui, nom du sélecteur, trois vues, chargement, erreur et relance avec la culture explicite du calendrier.
- `A360-154`, `A360-155`, `A360-157`, `A360-158`, `A360-160`, `A360-161`, `A360-164`: Sidebar, SidebarToggle, SplitButton, Steps, Timeline, Tree et WeekView localisent leurs noms accessibles tout en conservant les surcharges publiques.
- `A360-162`: TreeItem localise développer, réduire, chargement et échec.
- `A360-163`: Upload localise la liste, la progression, les actions, validations de nombre/taille/type, pluriels, cycle de transfert, erreur par défaut et tailles Mo/Ko/o avec formatage culturel; les messages et fragments explicitement fournis restent prioritaires.
- `A360-165`: WasmSmoke possède son contrat de ressources français/anglais, injecte le localizer depuis le code-behind et ne conserve que la marque dans le markup.

Preuves de lot:

- Ressources: bibliothèque 114 clés françaises/anglaises; WasmSmoke 5 clés françaises/anglaises; parité exacte.
- Garde source: zéro caractère accentué dans les quinze sources ciblées; chaque composant appelle `OmniStrings.Get`; le sample utilise exclusivement `Text[...]` pour les textes audités.
- Tests de localisation et conventions: 10/10 avec rendus Scheduler, navigation, collections et Upload sous `fr-FR` et `en-US`.
- Build Release gardé de la solution, incluant WasmSmoke: 0 avertissement, 0 erreur.
- Suite globale gardée: 166 réussis, 0 échoué, 0 ignoré.
- API publique: baseline valide de 1 134 signatures.
- CSP: 281 fichiers source acceptés.

## Lot 12 - A360-166 à A360-180

Statut: terminé le 2026-08-11.

- `A360-166`, `A360-173` à `A360-180`: App et les huit composants cités utilisent leurs code-behind; la garde globale et `GEN004` prouvent zéro bloc `@code` dans les 120 fichiers Razor de production et samples.
- `A360-167`: `eng/Test-WasmHost.ps1` sert l'artefact publié avec les en-têtes de déploiement, vérifie assets et CSP, lance Chromium par CDP, clique `#wasm-action`, exige `Compteur : 1`, `aria-valuenow="1"` et une console sans erreur. La CI et la documentation de compatibilité exécutent désormais cette gate. Le navigateur intégré a confirmé les mêmes valeurs, des dimensions de rendu cohérentes et zéro erreur console.
- `A360-168`: `.editorconfig` fusionne désormais fins de ligne LF, groupes de fichiers, exception Markdown, conventions C# project-neutral, migrations et sévérités `GEN001` à `GEN008`; le build reste sans warning.
- `A360-169`, `A360-171`, `A360-172`: les actions auditées Auto, Catalog et Hybrid composent un `OmniIcon` décoratif avec leur texte localisé; des gardes de source les protègent.
- `A360-170`: `OmniDialogRequest` expose un footer optionnel sans casser son constructeur existant; l'hôte le transmet au dialogue. Les requêtes du catalogue fournissent une action Fermer localisée avec icône et sans effet secondaire autre que la fermeture du dialogue courant.

Preuves de lot:

- Build Release de la solution: 0 avertissement, 0 erreur; build MAUI Hybrid: 0 avertissement, 0 erreur.
- Tests ciblés conventions/overlays: 21/21; suite globale: 171 réussis, 0 échoué, 0 ignoré.
- Structure Razor: 120 fichiers et zéro `@code` inline.
- WebAssembly publié: en-têtes valides; clic -> `Compteur : 1`; progression -> 1; console sans erreur; CSP stricte; assets y compris la feuille du sample.
- Navigateur intégré: même clic et progression, dimensions effectives du shell/alerte/bouton/progression vérifiées et journal d'erreurs vide.
- Interactive Auto Chromium: compteur à 1, hydratation et console sans erreur.
- Hybrid WebView2: compteur à 1 et console sans erreur.
- API publique: baseline intentionnellement mise à jour et valide à 1 135 signatures pour la propriété Footer additive.
- CSP: 281 fichiers source acceptés.

## Lot 13 - A360-181 à A360-195

Statut: terminé le 2026-08-11.

- `A360-181` à `A360-195`: Badge, BarOptions, BarSeries, Body, Breadcrumb, BreadcrumbItem, Button, Card, CategoryAxis, Chart, ChartTooltipOptions, CheckBox, CheckBoxList, ColorPicker et Column ont chacun leur fichier `.razor.cs`; leur markup ne contient aucun bloc `@code`.

Preuves de lot:

- Contrôle nominatif: 15 composants trouvés, 15 code-behind présents, zéro manquant et zéro bloc inline.
- Garde globale: 120 fichiers Razor de production et samples, zéro `@code`; `GEN004` est promu à `error`.
- Les gates immédiatement précédentes couvrant ces sources restent vertes: build solution 0 avertissement/0 erreur, 171 tests réussis et API publique valide à 1 135 signatures.

## Lot 14 - A360-196 à A360-210

Statut: terminé le 2026-08-11.

- `A360-196` à `A360-210`: ColumnSeries, CompareValidator, ComponentsHost, ContextMenu, DataGrid, DataGridColumn, DataList, DatePicker, DayView, Dialog, DonutSeries, DropDown, EmailValidator, Fieldset et FormField ont chacun leur fichier `.razor.cs`; leur markup ne contient aucun bloc `@code`.

Preuves de lot:

- Contrôle nominatif: 15 composants trouvés, 15 code-behind présents, zéro manquant et zéro bloc inline.
- Garde globale: 120 fichiers Razor de production et samples, zéro `@code`; `GEN004` est promu à `error`.
- Les gates immédiatement précédentes couvrant ces sources restent vertes: build solution 0 avertissement/0 erreur, 171 tests réussis et API publique valide à 1 135 signatures.

## Lot 15 - A360-211 à A360-225

Statut: terminé le 2026-08-11.

- `A360-211` à `A360-225`: Grid, GridLines, Header, Heading, HtmlEditor, Icon, Image, Label, Layout, Legend, LengthValidator, LineSeries, Link, ListBox et Main ont chacun leur fichier `.razor.cs`; leur markup ne contient aucun bloc `@code`.

Preuves de lot:

- Contrôle nominatif: 15 composants trouvés, 15 code-behind présents, zéro manquant et zéro bloc inline.
- Garde globale: 120 fichiers Razor de production et samples, zéro `@code`; `GEN004` est promu à `error`.
- Les gates de phase couvrant ces sources sont vertes: build solution 0 avertissement/0 erreur, 171 tests réussis, API publique valide à 1 135 signatures, WebAssembly/Auto/Hybrid interactifs et CSP sur 281 fichiers.

## Lot 16 - A360-226 à A360-240

Statut: terminé le 2026-08-11.

- `A360-226` à `A360-240`: Markers, MonthView, MultiSelect, Notification, NullableCheckBox, NullableSwitch, Numeric, Pager, PanelMenu, PanelMenuItem, Password, PieSeries, ProfileMenu, ProfileMenuItem et ProgressBar ont chacun leur fichier `.razor.cs`; leur markup ne contient aucun bloc `@code`.

Preuves de lot:

- Contrôle nominatif: 15 composants trouvés, 15 code-behind présents, zéro manquant et zéro bloc inline.
- Garde globale: 120 fichiers Razor de production et samples, zéro `@code`; `GEN004` est promu à `error`.
- Les gates de phase couvrant ces sources restent vertes: build solution 0 avertissement/0 erreur, 171 tests réussis, API publique valide à 1 135 signatures, WebAssembly/Auto/Hybrid interactifs et CSP sur 281 fichiers.

## Lot 17 - A360-241 à A360-255

Statut: terminé le 2026-08-11.

- `A360-241` à `A360-255`: RadioButtonList, RadioButtonListItem, RequiredValidator, Row, Scheduler, SelectBar, SelectBarItem, SeriesDataLabels, Sidebar, SidebarToggle, Skeleton, Slider, SplitButton, SplitButtonItem et Stack ont chacun leur fichier `.razor.cs`; leur markup ne contient aucun bloc `@code`.

Preuves de lot:

- Contrôle nominatif: 15 composants trouvés, 15 code-behind présents, zéro manquant et zéro bloc inline.
- Garde globale: 120 fichiers Razor de production et samples, zéro `@code`; `GEN004` est promu à `error`.
- Les gates de phase couvrant ces sources restent vertes: build solution 0 avertissement/0 erreur, 171 tests réussis et API publique valide à 1 135 signatures.

## Lot 18 - A360-256 à A360-270

Statut: terminé le 2026-08-11.

- `A360-256` à `A360-270`: StackedAreaSeries, StackedColumnSeries, Steps, StepsItem, Switch, Tabs, TabsItem, TemplateForm, Text, TextArea, TextBox, ThemeScope, Timeline, TimelineItem et ToggleButton ont chacun leur fichier `.razor.cs`; leur markup ne contient aucun bloc `@code`.

Preuves de lot:

- Contrôle nominatif: 15 composants trouvés, 15 code-behind présents, zéro manquant et zéro bloc inline.
- Garde globale: 120 fichiers Razor de production et samples, zéro `@code`; `GEN004` est promu à `error`.
- Les gates de phase couvrant ces sources restent vertes: build solution 0 avertissement/0 erreur, 171 tests réussis et API publique valide à 1 135 signatures.

## Lot 19 - A360-271 à A360-285

Statut: terminé le 2026-08-11.

- `A360-271` à `A360-277`: Tooltip, Tree, TreeItem, TreeLevel, Upload, ValueAxis et WeekView ont chacun leur fichier `.razor.cs` et aucun bloc `@code` inline.
- `A360-278`: les zones tactiles de fermeture de dialogue, rejet de notification, expansion/sélection d'arbre et expansion de ligne de grille ont une dimension minimale de 2,75 rem, soit 44 px à la taille racine usuelle.
- `A360-279`: `.editorconfig` reste aligné avec les conventions neutres du kit, LF, exception Markdown et sévérités `GEN001` à `GEN008`; sa garde ciblée reste verte.
- `A360-280`: le bouton d'action WebAssembly associe maintenant une icône Omni décorative à son libellé localisé.
- `A360-281` à `A360-285`: les anciens conteneurs Chart, DataGrid, Foundation, Navigation et Overlay ont été supprimés; leurs 33 types possèdent chacun un fichier dédié dans le même namespace et sans changement d'API.

Preuves de lot:

- Contrôle nominatif Razor: 7 composants, 7 code-behind et zéro bloc inline.
- Gardes de conventions: 14/14 réussies, incluant taille tactile, icône Wasm, retrait des cinq conteneurs et exactement une déclaration de type dans chacun des 33 fichiers attendus.
- Compilation de la bibliothèque et des tests: zéro avertissement et zéro erreur.
- API publique: test de baseline réussi à 1 135 signatures après séparation physique des types.

## Lot 20 - A360-286 à A360-300

Statut: terminé le 2026-08-11.

- `A360-286` à `A360-288`: SchedulerView, SchedulerAppointment, Option, UploadRequest et les quatre contrats Stack résident désormais chacun dans un fichier dédié; le vrai code-behind `OmniStack.razor.cs` ne contient que le composant partiel.
- `A360-289` à `A360-293`: les textes français des scripts ont été externalisés dans `eng/PowerShellMessages.psd1`; les 17 scripts `eng/*.ps1` sont strictement ASCII sans perte des messages humains.
- `A360-294`: les deux U+2014 du texte EUPL-1.2 sont conservés comme unique exception juridique, reliée à la publication officielle de la Commission européenne; la gate interdit ce caractère partout ailleurs dans le périmètre produit.
- `A360-295` et `A360-297`: `CLAUDE.md` contient produit, stack, structure, commandes, règles, carte documentaire et frontière clean-room. Les contrats spécialisés de tests, analyseurs, UI et agents sont désormais canoniques sous `docs/`; aucun ADR artificiel n'est créé pour des décisions déjà tracées.
- `A360-296`: `.claude/test-config.md` décrit et pilote les gates unitaires, intégration, paquet, Server, WebAssembly, Auto et Hybrid avec leurs prérequis et nettoyages.
- `A360-298`: `global.json` désactive explicitement le roll-forward et verrouille réellement SDK et workload set `10.0.302`; README et garde automatisée concordent.
- `A360-299`: le catalogue ne présente plus 110 présences comme une validation fonctionnelle. Il affiche exactement 37 balises Omni distinctes illustrées et `docs/catalog-scenarios.json` fournit la matrice exacte, explicitement limitée à l'illustration.
- `A360-300`: la CI produit et vérifie un manifeste de provenance avec commit, run et SHA-256 des deux paquets. La release exige un workflow CI `push` réussi pour le même commit, télécharge son artefact exact, revérifie provenance, version, contenu et symboles, puis publie sans restore, build ni pack.

Preuves de lot:

- Gardes de conventions: 19/19 réussies, incluant types par fichier, ASCII, exception EUPL, matrice catalogue, pin SDK et chaîne NuGet.
- PowerShell: 17/17 scripts ASCII, zéro erreur de parsing et ressource UTF-8 importée avec 50 entrées; corpus manifeste validé sur 32 projets et 4 607 fichiers indexés en mode structure.
- Couverture: générateur réparé pour parcourir les sous-dossiers, exécution stable deux fois à 110/110 et artefacts canoniques régénérés.
- Provenance locale: manifeste, deux hashes, dépôt/commit/run et version `v0.1.0-alpha.1` validés; contenu NuGet 13 entrées et symboles 5 entrées.
- API publique: baseline toujours valide à 1 135 signatures après la séparation des contrats.

## Lot 21 - A360-301 à A360-315

Statut: terminé le 2026-08-11.

- `A360-301`, `A360-304`, `A360-305`, `A360-307` et `A360-312`: l'ancien balayage lexical a été remplacé par un analyseur de tags Razor qui ignore commentaires et contenu des expressions, sépare attributs, références qualifiées, namespaces, paquets, ressources et sélecteurs CSS, puis conserve fichier, ligne, projet, statut et SHA-256 par occurrence. Les rapports schema v2 ont été régénérés depuis un manifeste courant de 4 603 fichiers.
- `A360-302`, `A360-306` et `A360-313`: le registre ne contient plus `implemented`/`planned`. Il sépare 110 présences de fichiers, 103 entrées dont la cible est nommée par un test, 47 reliées au catalogue et 6 reliées à une preuve navigateur; les tableaux expliquent explicitement la portée limitée de chaque facette.
- `A360-303`: l'extracteur compilé couvre types, classes/interfaces/structs/delegates, héritage, composants, constructeurs, paramètres et défauts, propriétés et `[Parameter, EditorRequired]`, champs, événements, opérateurs, méthodes et contraintes génériques. Son auto-test et la baseline de 1 135 signatures passent.
- `A360-308`: le catalogue est maintenant chargé dans Chromium depuis `about:blank` après installation d'un observateur `securitypolicyviolation`; cinq familles sont parcourues, dialogue/focus/fermeture et notification live sont exercés, puis console, violations client et collecteur serveur post-navigation doivent rester vides.
- `A360-309`: la gate NuGet inspecte désormais le contenu binaire UTF-8/UTF-16 de DLL, PDB, CSS et JavaScript ainsi que les références/types/membres des métadonnées managées. Une fixture injectant le token interdit dans un JavaScript au nom neutre est effectivement rejetée.
- `A360-310`: badge, héro et métrique catalogue décrivent une démonstration partielle de 37 balises distinctes, sans affirmation 110/110.
- `A360-311`: gras, italique, indice, exposant et indentation enveloppent la sélection réelle du textarea via le module JS statique; sélection et focus sont restaurés, l'historique undo/redo reste déterministe et le HTML retourné est resanitisé.
- `A360-314`: le plan 001 est explicitement supersédé; ses cases sont bornées à l'état historique et ses affirmations inventaire/publication renvoient aux plans 002 et 003.
- `A360-315`: les deux packages MAUI sont coordonnés en 10.0.90 dans les versions centrales et les verrous directs; les dépendances verrouillées sont lues sémantiquement par la garde.

Preuves de lot:

- Inventaires v2: 24 562 observations de surface, 110 composants et 55 274 preuves de paramètres; zéro chemin, ligne ou hash invalide par rapport au manifeste.
- Faux positifs explicitement cités par l'audit: 0/10 dans les deux JSON, incluant `FailureCount`, `Decision`, `Verdict` et les quatre noms de tests Radzen.
- Fixtures du parseur: commentaires, expression ternaire, indexeur localisé, attributs HTML minuscules et identifiant de test correctement exclus.
- Registre de couverture schema v2: stable sur deux générations, 110 présences sans propriété `implemented` ni `planned`, facettes vides sérialisées en tableaux explicites.
- Catalogue Chromium: cinq familles, dialogue, focus, fermeture, notification, console propre, zéro violation CSP client et serveur; ports 5187/9222 libérés après la sonde.
- Éditeur et conventions: 32/32 tests ciblés, puis 22/22 gardes exhaustives.
- Paquet: paquet propre accepté, contenu 13 entrées et symboles 5 entrées; paquet contaminé rejeté par inspection du payload.
- API publique: 1 135 signatures validées.

## Lot 22 - A360-316 à A360-325

Statut: terminé le 2026-08-11.

- `A360-316` et `A360-325`: `coverlet.collector` 10.0.1 est épinglé centralement et privé au projet de tests. La CI produit un Cobertura et un TRX, puis exige un rapport unique exploitable, tous les tests verts, des lignes sources et une couverture non nulle.
- `A360-317`: toutes les actions GitHub sont référencées par SHA complet; NuGet/login est également immuable. Dependabot gère séparément les mises à jour contrôlées des actions et des packages NuGet.
- `A360-318`: les deux dépendances MAUI et leur verrou restent alignés en 10.0.90.
- `A360-319`, `A360-322` et `A360-324`: le catalogue officiel NuGet confirme `bunit` 2.8.6 comme dernière stable le 2026-08-11; la version 2.9.0 citée par l'audit n'existe pas. Cette décision et sa source sont versionnées dans `eng/dependency-policy.json` et `docs/dependencies.md`.
- `A360-320` et `A360-323`: un SBOM CycloneDX 1.6 et un registre de licences sont générés depuis les neuf verrous. Les licences par expression, fichier et URL sont classées; les douze textes embarqués dans les packages sont conservés, empreintés et intégrés au paquet NuGet avec NOTICE et SBOM.
- `A360-321`: SDK, roll-forward et workload set sont figés et vérifiés à 10.0.302.

Preuves de lot:

- Tests et couverture réels: 181/181 réussis; Cobertura exploitable sur 2 703 lignes valides, dont 86,64% couvertes.
- SBOM/licences: 114 couples package/version uniques, 114 licences non vides, douze fichiers de licence conservés avec SHA-256 et zéro divergence aux verrous.
- Politique: versions centrales, verrous bUnit/coverlet/MAUI, SDK, workload set et toutes les références `uses:` validés automatiquement.
- PowerShell: zéro erreur de parsing et zéro octet non ASCII dans l'ensemble des scripts `eng/*.ps1`.
