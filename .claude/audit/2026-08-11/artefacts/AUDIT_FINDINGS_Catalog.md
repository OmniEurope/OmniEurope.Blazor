# Findings d'audit 360 - Catalog

> Audit: 2026-08-11
> Les blocs sont ajoutés fichier par fichier. Une absence de finding est consignée explicitement par `RAS`.

<a id="catalog-imports"></a>
## `samples/OmniEurope.Blazor.Catalog/Components/_Imports.razor`

RAS. Revue de sécurité manuelle best-effort, non outillée ; aucun contenu exécutable, secret ou tiret cadratin U+2014 détecté.

<a id="catalog-app"></a>
## `samples/OmniEurope.Blazor.Catalog/Components/App.razor`

RAS. La feuille globale et les scripts restent des ressources de même origine compatibles avec la CSP annoncée ; aucun style inline, contenu distant, secret ou tiret cadratin U+2014 détecté. Revue de sécurité manuelle best-effort, non outillée.

<a id="catalog-mainlayout"></a>
## `samples/OmniEurope.Blazor.Catalog/Components/Layout/MainLayout.razor`

- [Élevé] [Style] `STD-I18N` : le libellé visible `Catalogue 110/110` est codé en dur sans `IStringLocalizer<AppStrings>` - ligne 5 - source : registre `_Generic` et `AUDIT_KIT.md` (infrastructure de localisation absente) - recommandation : Codex peut introduire le contrat de ressources du catalogue et rendre ce libellé localisable, en conservant le nom de produit comme marque.
- [Moyen] [Authenticité] Le badge `Catalogue 110/110` présente le catalogue comme exhaustif alors que `docs/component-families.md:21` précise qu'il n'illustre qu'un sous-ensemble et que `Home.razor` ne rend que 36 balises Omni distinctes - ligne 5 - source : lecture complète et comparaison documentation-réalité - recommandation : Codex peut remplacer le badge par une formulation exacte telle que `110 cibles Razor présentes` et afficher séparément la couverture réelle des scénarios du catalogue.
- [Moyen] [Fiabilité] Le layout rend directement `@Body` sans `ErrorBoundary`, contrairement au standard frontend du kit ; une exception de composant peut donc remplacer le catalogue par l'erreur globale du circuit - ligne 6 - source : `C:\Dev\_Generic\docs\coding-standards.md` - recommandation : Codex peut entourer le corps d'une frontière d'erreur localisée avec récupération observable, puis la vérifier dans le navigateur.

Sécurité : revue manuelle best-effort, non outillée ; aucun secret ni tiret cadratin U+2014 détecté.

<a id="catalog-home"></a>
## `samples/OmniEurope.Blazor.Catalog/Components/Pages/Home.razor`

- [Élevé] [Authenticité] Le texte affirme que l'hôte valide les `110 capacités inventoriées` et affiche `110/110`, mais le markup ne rend que 36 balises Omni distinctes et la documentation précise que le catalogue n'illustre qu'un sous-ensemble ; la présence de fichiers est présentée comme une validation fonctionnelle - lignes 9-13 - source : `docs/component-coverage.md:3-6`, `docs/component-families.md:21` et comptage vérifié du markup avant `@code` - recommandation : Codex peut remplacer cette affirmation par la métrique exacte de présence, générer une matrice scénario-vers-capacité et n'afficher une couverture comportementale qu'après preuve nominale, erreur et navigateur pour chaque entrée.
- [Élevé] [Style] `STD-I18N` : la page contient de nombreux titres, libellés, placeholders, messages, labels accessibles et contenus de dialogue/notification codés en dur, sans `IStringLocalizer<AppStrings>` - lignes 4-146 - source : registre `_Generic` et `AUDIT_KIT.md` - recommandation : Codex peut extraire toutes les chaînes humaines vers les ressources du catalogue, y compris les fragments construits en C#, puis ajouter une garde qui échoue sur les chaînes visibles non localisées.
- [Élevé] [Style] La logique, les modèles, les données et les callbacks sont maintenus dans un bloc `@code`, alors que le standard frontend exige un code-behind et que `GEN004` n'est pas câblé - lignes 120-159 - source : `C:\Dev\_Generic\docs\coding-standards.md` et `AUDIT_KIT.md` - recommandation : Codex peut déplacer ce bloc vers `Home.razor.cs`, garder uniquement le markup dans la page et câbler `GEN004` sans suppression.
- [Élevé] [Fiabilité] Plusieurs contrôles de formulaire n'ont aucun nom accessible associé : zone de texte, liste, autocomplete, date, slider, couleur et upload sont rendus hors `OmniFormField` et sans `aria-label` ; l'identifiant seul ne crée pas de label - lignes 64-70 - source : markup de la page et rendu des composants cibles, revue accessibilité manuelle best-effort - recommandation : Codex peut associer chaque contrôle à un label localisé via `OmniFormField` ou `aria-labelledby`, puis vérifier les noms accessibles dans un navigateur et un test bUnit.
- [Moyen] [Style] `STD-BTN` : les deux boutons d'action réels `Ouvrir le dialogue` et `Notifier` ont du texte mais aucune icône, contrairement à la convention action texte + icône - lignes 47-48 - source : registre `_Generic` - recommandation : Codex peut composer les boutons Omni avec une icône décorative et un libellé localisé, sans introduire de dépendance Radzen.
- [Moyen] [Style] `STD-DIALOG` : le dialogue ouvert par le service n'offre qu'une fermeture par croix, arrière-plan ou Échap et aucune action explicite `Fermer`/`Annuler` dans le contenu - ligne 145 - source : appel complet et contrats `OmniComponentsHost`/`OmniDialogRequest` - recommandation : Codex peut étendre le contrat Omni de requête avec un pied de dialogue, fournir une action localisée sans effet secondaire et couvrir croix, Échap et bouton explicite, sans réintroduire Radzen.

Performance : les collections sont petites et bornées, la recherche locale est proportionnée et aucun N+1, I/O bloquant ou ressource non libérée n'est observé. Sécurité : contenu de démonstration statique, sans entrée injectée dans `AddMarkupContent`; revue best-effort, non outillée. Aucun tiret cadratin U+2014 détecté.

<a id="catalog-routes"></a>
## `samples/OmniEurope.Blazor.Catalog/Components/Routes.razor`

- [Faible] [Fiabilité] Le routeur ne fournit aucun contenu `NotFound` ; une URL inconnue rend une zone principale vide sans titre, explication ni chemin de retour - lignes 1-5 - source : lecture complète du routeur - recommandation : Codex peut ajouter une vue 404 localisée et accessible, distincte des routes paramétrées, puis vérifier sa réponse et son rendu.

`STD-FOCUS` : conforme, aucun `FocusOnNavigate` ni autofocus. Sécurité : revue manuelle best-effort, non outillée ; aucun secret ni tiret cadratin U+2014 détecté.

<a id="catalog-project"></a>
## `samples/OmniEurope.Blazor.Catalog/OmniEurope.Blazor.Catalog.csproj`

RAS. Le projet Web ne référence que la RCL attendue, hérite du ciblage, de la nullabilité, du déterminisme et des avertissements traités en erreurs, et n'ajoute aucune dépendance externe ou flottante. Aucun tiret cadratin U+2014 détecté.

<a id="catalog-lock"></a>
## `samples/OmniEurope.Blazor.Catalog/packages.lock.json`

RAS. Le verrou schéma v2 résout l'asset SDK `10.0.10` avec hachage de contenu et la seule référence projet attendue ; `AUDIT_DEPENDENCIES.md` ne relève ni vulnérabilité, dépréciation, conflit ni dépendance propre au catalogue. Aucun secret ni tiret cadratin U+2014 détecté.

<a id="catalog-program"></a>
## `samples/OmniEurope.Blazor.Catalog/Program.cs`

- [Élevé] [Sécurité] `/csp-report` lit intégralement chaque corps sans vérifier le type, sans limite applicative adaptée à un petit rapport, sans propager `RequestAborted`, puis conserve chaque chaîne sans borne dans un singleton ; des requêtes répétées peuvent donc épuiser mémoire et CPU - lignes 23-27 et 36-39 - source : `metrics/SECURITY_SCAN.md`, revue manuelle best-effort non outillée et backlog déjà tracé dans `.claude/auditsession.md:30` - recommandation : Codex peut accepter uniquement les types CSP attendus, limiter strictement le corps et le nombre/la taille des entrées retenues, annuler la lecture avec la requête et ajouter des tests négatifs de dépassement et de concurrence.
- [Élevé] [Style] `STD-PARTIAL` : `Program` est déclaré `partial` sans consommateur `WebApplicationFactory`, source-générateur, code-behind ou autre exception autorisée ; le fichier contient aussi le service public `CspViolationStore`, contrairement à la règle un type par fichier et à la moindre exposition - lignes 36-42 - source : registre `_Generic` et recherche exhaustive des références - recommandation : Codex peut retirer la déclaration `partial` tant qu'aucun test d'intégration ne l'exige, déplacer le store dans son propre fichier et le rendre `internal`.
- [Moyen] [Sécurité] `/csp-status` est public et renvoie les rapports bruts fournis à l'endpoint précédent ; ces données non fiables peuvent contenir URL de document, référent, source et chaînes arbitraires, ce qui expose des détails internes au lieu d'un simple état de sonde - lignes 29-31 - source : revue manuelle best-effort non outillée - recommandation : Codex peut limiter la réponse publique à des compteurs bornés, réserver le détail à l'environnement de test avec contrôle d'accès et neutraliser les champs avant journalisation.
- [Moyen] [Sécurité] Le pipeline ajoute uniquement CSP et omet les protections Web attendues par le kit, notamment `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy` et HSTS hors développement - lignes 13-22 - source : `C:\Dev\_Generic\docs\coding-standards.md` - recommandation : Codex peut centraliser ces en-têtes, activer HSTS uniquement sur un déploiement HTTPS non local, puis ajouter une vérification HTTP de leurs valeurs.
- [Faible] [Sécurité] `connect-src 'self' ws: wss:` autorise des connexions WebSocket vers toute origine, au-delà du seul hub Blazor de l'hôte - lignes 16-19 - source : contrat CSP et backlog `.claude/auditsession.md:38`, revue best-effort non outillée - recommandation : Codex peut vérifier la prise en charge WebSocket de `'self'` sur les navigateurs cibles, puis borner les sources aux schémas/hôtes réellement nécessaires sans affaiblir le mode Interactive Server.

L'exemption antiforgery de l'endpoint de rapports est intentionnelle pour les rapports navigateur ; elle ne dispense pas des limites et validations ci-dessus. Aucun secret, stub, suppression de contrôle, I/O bloquant, score CRAP fabriqué ou tiret cadratin U+2014 détecté. Complexité, couverture, SAST et historique de secrets restent non fiables selon `metrics/`.

<a id="catalog-css"></a>
## `samples/OmniEurope.Blazor.Catalog/wwwroot/app.css`

RAS. Les styles du catalogue sont centralisés dans l'unique feuille globale, utilisent des unités responsives pour les dimensions structurantes et ne réintroduisent ni style local/inline, ni ressource distante, ni règle de grille Radzen inapplicable au produit clean-room. Aucun tiret cadratin U+2014 détecté.

## Synthèse du module `Catalog`

- Cohérence et architecture : RAS. L'hôte terminal dépend uniquement de la RCL, ne contient aucune logique métier partagée et respecte la direction décrite dans `AUDIT_ARCHITECTURE.md`.
- Complétude : la surface montrée est utile comme démonstration, mais l'affirmation `110/110` dépasse la preuve réelle ; le finding d'authenticité ci-dessus couvre l'écart sans dupliquer la dette documentaire globale.
- Sécurité : revue manuelle best-effort, non outillée. Les endpoints CSP concentrent le risque actionnable ; SAST, scan historique de secrets, complexité, couverture et CRAP restent non fiables selon `metrics/`.
- UI clean-room : l'absence de Radzen n'est pas un finding. Les recommandations de conventions conservent les composants Omni et n'imposent aucune réintroduction de Radzen.
- Proportionnalité : `PROPORTIONALITY: NONE`. L'alternative la plus simple viable reste ce petit hôte Web avec une page, une feuille globale et une sonde CSP ; aucune couche, abstraction, dépendance ou extension spéculative supplémentaire n'est observée.
- Authenticité : aucune donnée de démonstration n'est prise pour une donnée de production, aucun stub, test neutralisé, seuil abaissé ou fonctionnalité silencieusement désactivée n'est détecté hors l'affirmation de couverture déjà consignée.
- Compteurs : 15 findings actionnables, soit Critique 0, Élevé 7, Moyen 6, Faible 2. Les 9 fichiers sont en mode `Full`.
