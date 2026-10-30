<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-009 : Remédiation de l’audit 360 du 2026-10-30

> Statut : **ouvert, lots 1 et 2 terminés le 2026-10-30**. A360-001, A360-002, A360-008 et A360-011 vérifiés ; autres lots non commencés. Correction adjacente du seul test de menu explicitement autorisée et vérifiée.

## Objectif et périmètre

Résoudre les quinze constats de l’audit du 30 octobre par changements minimaux, avec reproduction avant modification et preuve du même scénario après. Vérifier séparément la cohérence du paquet NuGet et le comportement du site démo. Aucune modification de version, d’architecture, de politique, de seuil ou d’exception n’est comprise.

L’audit initial porte sur `develop` à `76daf06203bc285a4431823360b113633abd3a18` : 2 408 fichiers examinés, quinze constats (huit moyens, sept faibles). Ses identifiants ci-dessous sont ceux du **2026-10-30**, distincts des identifiants du PLAN-007 du 29 octobre. Rapport et preuves originaux conservés dans le dossier local de preuves de la session du 2026-10-30. Les sources et gardes du dépôt restent l’autorité.

La branche du voile `work/veil-k3m9`, commit `80ddc8c8c98048949eabe4bd14e6c9bf6d321f7d`, ajoute `OmniPageVeil.Opacity` avec défaut 85 %. Son [audit distinct](../audits/audit-branche-voile-2026-10-30.md) est intégré au présent plan. Elle ne clôt aucun des quinze constats. Ses deux écarts documentaires sont suivis séparément, sans renuméroter l’audit initial.

## Priorités et traçabilité

P1 : exception produit, donnée relue incorrectement, verrouillage ignoré, visiteur bloqué ou preuve de livraison bloquée. P2 : comportement partiel, présentation ou fiabilité d’un test. P3 : documentation et référence historique. La priorité d’intervention se distingue de la gravité de l’audit.

| Constat | Gravité | Priorité | Lot | Résultat attendu |
|---|---|---|---|---|
| A360-001 | Moyenne | P1 | 2 | Supprimer une dernière ligne avec rowspan sans exception ni perte des autres cellules. |
| A360-002 | Moyenne | P1 | 2 | Après quota localStorage, `get` et `entries` rendent la dernière valeur acceptée. |
| A360-003 | Moyenne | P1 | 3 | Un panneau ouvert ne peut plus modifier la valeur après ReadOnly ou Disabled. |
| A360-004 | Moyenne | P2 | 4 | FillAvailableHeight remplit le cadre local imbriqué et se recalcule. |
| A360-005 | Moyenne | P2 | 3 | Une variable textuelle s’insère au caret dans la source du mail. |
| A360-006 | Moyenne | P1 | 3 | Information reste refermable lorsque Dismissible est faux. |
| A360-007 | Moyenne | P1 | 5 | La porte existante de couverture accepte les six fichiers, sans assouplissement. |
| A360-008 | Moyenne | P1 | 1 | Languages à Parallel 4 couvre tous les couples, sans fragment vide ni doublon. |
| A360-009 | Faible | P2 | 4 | Les chevrons latéraux suivent la densité et conservent leur géométrie. |
| A360-010 | Faible | P2 | 4 | Les styles des deux éditeurs ciblent effectivement leurs éléments. |
| A360-011 | Faible | P2 | 1 | Le compteur du double concurrent compte exactement les appels réels. |
| A360-012 | Faible | P3 | 6 | La documentation OE0001 décrit correctement class/id et les autres attributs. |
| A360-013 | Faible | P3 | 6 | Le manifeste des preuves décrit les langues réellement parcourues. |
| A360-014 | Faible | P3 | 6 | Le guide décrit la configuration Dependabot existante. |
| A360-015 | Faible | P3 | 6 | La maquette historique initialise son aperçu sans erreur de syntaxe. |

## Lots vérifiables

### Lot 1 : fiabiliser les moyens de preuve

- [x] **A360-008** : reproduire `eng/Test-ShowcaseHost.ps1 -Probe Languages -Parallel 4` sur une publication fraîche. Borner les fragments au nombre réel de couples langue et largeur, dans le partitionnement existant. Garder le rejet des fragments sans mesure. Vérifier aussi Parallel 1 et 2, la fusion exhaustive et les gardes de fragments.
- [x] **A360-011** : reprendre la reproduction du vrai `CountedModule` compilé dans `DisposedWhileLoadingTests.cs`. Rendre son incrément atomique et sa lecture synchronisée, sans changer l’assertion de libération unique ni le produit.

Contrôle : Languages parcourt exactement `nombre de pages découvertes × 2 langues × 2 largeurs` (316 à la base auditée), chaque couple une seule fois. Pour le compteur : 100 000 paires de deux appels terminés doivent toutes compter 2 ; puis au moins dix exécutions consécutives des tests concernés dans les conditions concurrentes, sans retry ni attente ajoutée pour masquer le défaut. Ce lot précède les preuves dépendantes de ces outils, malgré la gravité faible du compteur.

Lot 1 livré : Languages passe avec Parallel 4, 2 et 1 (316 mesures à chaque passe). Le compteur passe de 1 038 erreurs à zéro sur 100 000 paires ; dix passages consécutifs des 14 tests ciblés réussissent. La suite complète compte 10 905 réussites après mise en conformité documentaire. Voir les [preuves du lot 1](PLAN-009-lot-1-verifications.md).

### Lot 2 : protéger la page et les préférences, P1

- [x] **A360-001** : dans `wwwroot/html-editor/tables.js`, reproduire le collage d’une cellule B avec `rowspan="2"` en dernière ligne, puis Delete row. Traiter l’absence de ligne suivante dans le chemin existant. Rejouer une suppression ordinaire, des cellules fusionnées valides et la conservation des cellules voisines.
- [x] **A360-002** : dans `wwwroot/omni-preferences.js`, reproduire un vrai quota localStorage sur une origine de test isolée : ancienne valeur persistée, écriture nouvelle non persistable, lecture par `get` et `entries`. Faire primer le repli mémoire approprié et contrôler sa réconciliation après une nouvelle écriture persistante réussie, ainsi que remove et clear.

Contrôle : dans un vrai navigateur, l’éditeur reste utilisable, la cellule supprimée disparaît et les cellules attendues restent présentes, sans JSException. Pour les préférences, les deux lectures rendent la nouvelle valeur après échec persistant ; après libération du quota et écriture réussie, elles rendent la nouvelle valeur persistée. Extraire le module du nupkg frais et comparer son SHA avant la preuve. Nettoyer uniquement les clés de test. Cette preuve JS ne vaut pas preuve C#/HTTP/PostgreSQL : ajouter le parcours consommateur réel si le contrat de cette couche est concerné par le correctif.

Lot 2 livré : les deux défauts sont reproduits avant modification puis vérifiés sur les mêmes parcours. Quatre suppressions de tableau contrôlées dans la démo ; quota natif, tombstone et réconciliation contrôlés avec le module extrait du nupkg. Le test adjacent du menu est corrigé sur accord explicite, dix passages consécutifs réussissent et la suite complète compte 10 908 réussites. Voir les [preuves du lot 2](PLAN-009-lot-2-verifications.md).

### Lot 3 : verrouillage et actions utilisateur, P1 puis P2

- [ ] **A360-003, P1** : reproduire l’ouverture du calendrier puis l’activation de ReadOnly au clavier, sans clic extérieur, et le choix d’un jour. Fermer le panneau ou refuser la mutation dans le chemin existant. Vérifier Disabled et les chemins analogues TimePicker/DateTimePicker avant de décider quels fichiers doivent changer ; leurs défauts ne sont pas encore prouvés en exécution.
- [ ] **A360-006, P1** : corriger seulement la composition de `DialogsExample.razor` pour donner une action explicite de fermeture quand Dismissible est faux. Conserver le contrat de la primitive : Échap et fond ne doivent pas devenir des fermetures implicites dans ce cas.
- [ ] **A360-005, P2** : dans `OmniMailTemplateEditor`, insérer la variable textuelle dans la face source active avec le chemin et la sélection existants. Rejouer aussi sujet, face visuelle, remplacement de sélection et variable de type Block.

Contrôle : navigateur réel, valeur et événement lié inchangés après verrouillage ; réactivation permettant une sélection normale. Le dialogue Information s’ouvre puis se ferme par son action et restitue le focus, avec les deux réglages Dismissible. En source mail, le token apparaît exactement au caret, une seule fois, puis reste présent après aller-retour visuel/source ; les autres modes conservent leur comportement. Ajouter les tests ciblés de ces contrats sans modifier l’API.

### Lot 4 : disposition et homogénéité visuelle, P2

- [ ] **A360-004** : instrumenter d’abord le choix d’ancêtre dans `wwwroot/omni-grid.js`, sa hauteur utile et les recalculs. Le cadre mesuré à 506 px avec une grille à 128 px est prouvé, mais le choix d’ancêtre n’a pas encore été isolé comme cause. Corriger uniquement après confirmation ; couvrir cadre imbriqué, redimensionnement, dialogue et cadre simple.
- [ ] **A360-009** : utiliser les jetons de densité existants pour les chevrons latéraux dans `05-layout.css`, sans créer d’exception STD-BTN. Préserver l’hystérésis, les butées et l’absence de déplacement du contenu lorsque le chevron apparaît.
- [ ] **A360-010** : fermer la règle CSS incomplète de `site/OmniEurope.Blazor.Showcase/wwwroot/app.css` avant les règles des éditeurs. Aucun autre ajustement esthétique.

Contrôle : à 1280 × 900, mesurer le cadre et la grille remplie, sans l’écart vide initial de 338 px, puis redimensionner et vérifier la mise à jour. Rejouer Scripts pour la grille et les chevrons, et Density ; les trois invariants de remplissage et les 42 mesures de chevrons initialement en échec doivent être satisfaits, sans nouvelle exception. Vérifier les deux pages d’éditeur : bordure et padding réellement appliqués, cadre du comparateur à 24 rem (384 px avec racine à 16 px). Captures et styles calculés, clair/sombre et largeur mobile, console et CSP contrôlées. Vérifier aussi le voile à 85 % et 100 % car il partage la feuille de disposition.

### Lot 5 : rétablir la porte de couverture, P1 bloquant la livraison

- [ ] **A360-007** : recalculer la couverture après les lots précédents, puis couvrir les scénarios publics encore manquants des six fichiers : `OmniValidationSummary.razor`, son code-behind, `OmniMain.razor.cs`, `OmniPanelMenuFilter.cs`, `OmniPanelMenuItem.razor.cs` et `OmniSiteSearch.razor.cs`. Inclure le renommage d’une entrée déjà filtrée et la notification de ses ancêtres.

Contrôle : un unique rapport Cobertura frais et le TRX correspondant, tests réellement exécutés, puis `eng/Test-Coverage.ps1` et `eng/Test-Crap.ps1` acceptés. La base auditée avait une ligne et vingt branches manquantes ; remesurer les positions après fusion. Ne pas tester des détails sans comportement observable uniquement pour atteindre un chiffre. Garder baseline, exceptions, seuils et assertions ; si un chemin est prouvé inaccessible, demander une décision explicite plutôt que le masquer. Des tests verts seuls ne clôturent pas les autres défauts.

### Lot 6 : remettre les descriptions en accord, P3

- [ ] **A360-012** : corriger la phrase de `docs/analyzers.md` selon le traitement réel des attributs et les exceptions déjà documentées. Vérifier class/id et un attribut minuscule admissible avec l’analyseur réellement embarqué, sans nouveau test unitaire de prose Markdown.
- [ ] **A360-013** : mettre `docs/browser-scenarios.json` en accord avec les deux langues et deux largeurs réellement exécutées. Distinguer une ancienne mesure de langues longues si elle reste citée. Contrôler la syntaxe JSON et le résultat Languages du lot 1.
- [ ] **A360-014** : aligner `docs/dependencies.md` sur `.github/dependabot.yml`, sans changer la fréquence ni la politique. Une configuration lue ne sera pas présentée comme une proposition GitHub réellement créée.
- [ ] **A360-015** : réparer uniquement l’apostrophe invalide du script de la maquette archivée PLAN-004, avec mention datée de réparation documentaire. Vérifier la syntaxe du script réel puis l’initialisation et les interactions de l’aperçu dans un navigateur.
- [ ] **VEIL-DOC-001, branche du voile** : remplacer le compte rendu « opaque » à 85 % de `eng/scripts-probe/veil.mjs` par la mesure effectivement vérifiée, puis relire le journal de la sonde réelle.
- [ ] **VEIL-DOC-002, branche du voile** : ranger le changement de défaut du voile sous `Changed` ou `Breaking changes` dans `CHANGELOG.md`, avec la migration `Opacity="100"` déjà présente, conformément à `docs/versioning.md`. Ne choisir aucune nouvelle version.

Contrôle : les quatre descriptions initiales et les deux écarts de branche correspondent aux sources relues et aux preuves citées. Les deux derniers points sont des constats supplémentaires faibles, pas une correction implicite de l’audit initial. Aucun de ces correctifs n’est exécuté lors de la préparation du plan.

### Lot 7 : recette transversale et cohérence du paquet NuGet

- [ ] Refaire restore verrouillé, build Release sans avertissement, suites appropriées, couverture et CRAP, avec les gardes d’architecture et de conventions actives. Utiliser le lanceur le lanceur local officiel pour les parcours applicatifs.
- [ ] Produire un nupkg local de la version déjà choisie, vérifier API publique, contenu, analyseur, budgets, ressources et CSP avec les gardes natifs. Aucune publication. Consommer le nupkg dans un projet isolé sans ProjectReference ni cache de l’ancien paquet ; prouver les correctifs de scripts sur les assets réellement extraits/livrés.
- [ ] Rejouer les contrats d’homogénéité concernés : ReadOnly/Disabled, modes source/visuel, portée et opacité du voile, densité, API/documentation/ressources/démo. Les contrats communs doivent rester identiques entre composants comparables, sauf décision documentée existante.
- [ ] Vérifier les parcours réels de la démo sur publication fraîche, y compris Scripts, Pickers, Density et Languages après réparation de leurs blocages. Pour un changement partagé touchant Server, WebAssembly, Auto ou Hybrid, utiliser les smoke hosts natifs pertinents et préciser les hôtes réellement éprouvés.

Contrôle : journal de clôture par constat, avec base Git, fichiers strictement nécessaires, reproduction avant, commande/observation après et limite éventuelle. Zéro clôture sur une seule compilation ou sur le rapport d’un agent non revu. Toute autre intermittence rencontrée reste un défaut ouvert, instrumenté avant correction ; ne pas ajouter de retry. Si une preuve complète ne peut pas être obtenue dans le périmètre autorisé, livrer le résultat comme partiel.

## Ordre, décisions et clôture

Ordre proposé : lot 1 (outils de preuve), lot 2 (exception et préférence), lot 3 (verrouillage et actions), lot 4 (disposition), lot 5 (couverture finale), lot 6 (documentation), lot 7 (recette du paquet et de la démo). Le lot 6 ne dépend pas des correctifs produit, mais ses descriptions de mesures doivent utiliser les résultats frais. Le lot 5 est P1, exécuté après les correctifs pour éviter une mesure périmée.

Avant chaque lot, confirmer l’état Git et réutiliser la reproduction conservée. Un correctif non reproductible, un changement d’API, une version, une politique ou une extension architecturale exige une décision supplémentaire. Ne pas corriger un défaut adjacent par opportunité. Les demandes du 2026-10-30 autorisent les lots 1 et 2, leurs commits finaux et la correction ciblée du test du menu. Aucun autre lot, push ou déploiement autorisé.

Clôture : quinze constats initiaux et deux écarts de branche disposent chacun d’une preuve observée ou d’une décision explicite traçable ; les gardes applicables passent sans assouplissement ; les limites d’environnement sont écrites. Tant que ces critères ne sont pas réunis, conserver ce plan ouvert.
