# Plan - Remplacement complet de Radzen

> Canonical plan: `plans/PLAN-002-remplacement-radzen.md`
> Last updated: 2026-08-11
> Current scope: build and validate the complete component library first; consumer migrations are deferred to phases 14 and 15.

## Phase 1 - Fondation et publication initiale [done]
- [x] Abandonner le fork Radzen au profit d'une implémentation clean-room indépendante.
- [x] Recenser les balises Radzen dans tous les projets sous `C:\Dev`.
- [x] Établir le contrat CSP sans `unsafe-inline` ni `unsafe-eval`.
- [x] Créer la Razor Class Library, les tests et le packaging NuGet `OmniEurope.Blazor`.
- [x] Appliquer la licence EUPL-1.2 et les fichiers de gouvernance.
- [x] Implémenter les pilotes `OmniButton`, `OmniCard`, `OmniStack` et `OmniAlert`.
- [x] Prouver le build Release, les tests unitaires, le scanner CSP et le paquet NuGet.
- [x] Initialiser GitFlow avec `main`, `develop` et les préfixes standards.
- [x] Raccorder `origin` à `https://github.com/OmniEurope/OmniEurope.Blazor.git`.
- [x] Publier `main` et `develop` après autorisation explicite du contenu public.
- [x] Gate : dépôt GitHub lisible, CI verte et paquet alpha téléchargeable comme preuve de reprise.

## Phase 2 - Contrats, matrice de couverture et banc d'essai [in progress]
- [x] Séparer la construction de la bibliothèque des migrations, reportées aux phases 14 et 15.
- [x] Étendre l'inventaire aux types C#, services, enums, extensions, CSS, JavaScript et ressources Radzen.
- [ ] Corriger le parseur d'inventaire Razor, puis régénérer les paramètres, événements, templates et comportements réellement utilisés.
- [ ] Rendre le registre de couverture comportemental en reliant chaque capacité à des tests et preuves exécutables.
- [x] Définir les conventions publiques de noms, paramètres, événements, génériques et valeurs nulles.
- [x] Définir la compatibilité Blazor Server, WebAssembly, Interactive Auto et MAUI Blazor Hybrid.
- [x] Stabiliser les design tokens, thèmes clair/sombre, densités, tailles et états visuels en CSS statique.
- [x] Définir les règles HTML/ARIA, clavier, focus, annonces live et contraste applicables à chaque famille.
- [ ] Étendre le catalogue local aux états et variations réellement revendiqués pour les 110 cibles Razor.
- [ ] Ajouter une validation CSP dans un navigateur réel qui collecte les violations et échoue dès la première.
- [ ] Compléter les contrôles automatiques de rendu, accessibilité, API publique et contenu NuGet.
- [x] Définir le budget de poids, de rendu et d'allocations pour les composants simples et complexes.
- [x] Définir la politique SemVer, dépréciation, migration et rupture d'API.
- [ ] Instaurer des fiches clean-room contemporaines par capacité future sans fabriquer de preuves historiques rétroactives.
- [ ] Gate : matrice comportementale des 110 composants et banc d'essai navigateur vert avant extension de la bibliothèque.

## Phase 3 - Lot fondations, contenu et disposition [done]
- [x] Durcir les quatre pilotes existants avec tests clavier, ARIA, thèmes et paramètres supplémentaires observés.
- [x] Implémenter texte et titres avec HTML sémantique configurable.
- [x] Implémenter icônes avec SVG propre, libellés accessibles et aucun asset Radzen.
- [x] Implémenter badges, liens, images et skeletons.
- [x] Implémenter lignes, colonnes et grille de disposition responsive par classes finies.
- [x] Implémenter layout, body/main et header sémantiques.
- [x] Implémenter sidebar et sidebar toggle avec état contrôlé et focus cohérent.
- [x] Implémenter fieldset avec legend accessible.
- [x] Implémenter progress bar linéaire et circulaire avec valeurs ARIA.
- [x] Implémenter thèmes et appearance toggle sans injection de style.
- [x] Vérifier les états hover, focus, disabled, busy, loading, vide et erreur.
- [x] Documenter les correspondances de migration pour cette famille.
- [x] Gate : premier lot de 15 composants couvert par tests de rendu, sémantique et CSP.

## Phase 4 - Lot actions, services et superpositions [in progress]
- [x] Finaliser les contrats d'action communs, commandes asynchrones et prévention du double clic.
- [x] Implémenter split button et ses items avec navigation clavier.
- [x] Implémenter toggle button avec états pressé et désactivé.
- [x] Créer l'hôte de services OmniEurope remplaçant `RadzenComponents`.
- [ ] Créer une véritable couche de portail pour les superpositions au lieu du rendu direct dans l'hôte.
- [ ] Implémenter dialog avec focus trap sur les éléments réellement focalisables, fermeture contrôlée et restauration du focus.
- [x] Implémenter notifications avec régions live et files d'attente.
- [x] Implémenter tooltip avec déclencheurs clavier, souris et tactile.
- [x] Implémenter context menu sans gestionnaire HTML inline.
- [ ] Gérer z-index, scroll lock, clic extérieur, Escape et superpositions imbriquées avec une pile réelle.
- [x] Ajouter les tests unitaires et les assertions d'accessibilité DOM de la famille.
- [ ] Exécuter l'intégration dans un navigateur réel avec contrôle du focus et des superpositions.
- [ ] Gate : services et superpositions validés dans un navigateur réel sous CSP stricte, sans migration consommatrice.

## Phase 5 - Lot formulaires et validation [in progress]
- [x] Définir une base commune `InputBase<T>` pour identifiants, descriptions, erreurs et états.
- [x] Implémenter text box et password avec autocomplete et révélation contrôlée.
- [x] Implémenter text area avec comptage et limites accessibles.
- [ ] Corriger Numeric pour le format DOM invariant de `input type=number` tout en conservant la culture d'affichage.
- [x] Implémenter checkbox et switch avec liaison nullable lorsque requise.
- [x] Implémenter label et form field sans dupliquer la sémantique native.
- [x] Implémenter template form autour de `EditContext`.
- [x] Implémenter required validator.
- [x] Implémenter length validator.
- [x] Implémenter email validator.
- [x] Implémenter compare validator.
- [ ] Uniformiser et localiser messages, validation différée, soumission et focus sur première erreur.
- [ ] Tester cultures, clavier, sémantique ARIA et les runtimes Server, WASM, Interactive Auto et MAUI Hybrid réellement exécutés.
- [ ] Valider avec un lecteur d'écran réel et exécuter MAUI Blazor Hybrid sur son runtime graphique.
- [ ] Gate : formulaires du catalogue fonctionnels, accessibles et validés dans un navigateur sous CSP stricte.

## Phase 6 - Lot sélecteurs et entrées avancées [in progress]
- [x] Définir le modèle commun d'options, valeurs, texte, groupes, recherche et valeur vide.
- [ ] Implémenter un chemin réellement virtualisé pour les drop-down à gros volume.
- [x] Implémenter autocomplete avec annulation, debounce et annonces de résultats.
- [ ] Exposer et tester un état d'erreur remplaçable pour les recherches asynchrones d'autocomplete.
- [x] Implémenter list box et checkbox list.
- [x] Implémenter radio button list et ses items.
- [x] Implémenter select bar et ses items.
- [x] Implémenter date picker avec culture, clavier, bornes et calendrier accessible.
- [x] Implémenter slider avec orientation, pas, bornes et valeur ARIA.
- [x] Implémenter color picker sans style inline généré.
- [x] Implémenter upload avec progression, annulation, validation et reprise selon les usages.
- [ ] Tester gros volumes réellement virtualisés, valeurs nulles, rechargements asynchrones et formulaires imbriqués.
- [ ] Gate : scénarios de saisie et sélection du catalogue verts dans un navigateur sous CSP stricte et navigation clavier complète.

## Phase 7 - Lot navigation structurée [in progress]
- [x] Définir les contrats communs d'item, route, sélection, expansion et autorisation.
- [x] Implémenter panel menu et panel menu item.
- [x] Implémenter breadcrumb et breadcrumb item.
- [ ] Implémenter tabs et tabs item avec `tablist`, roving tabindex et déplacement réel du focus.
- [x] Implémenter steps et steps item avec validation de transition.
- [x] Implémenter profile menu et profile menu item.
- [x] Intégrer `NavigationManager`, routes actives et navigation annulable.
- [ ] Tester dans un navigateur clavier, focus, responsive, historique et rendu préinteractif.
- [ ] Gate : navigation du catalogue conforme aux motifs ARIA et sans régression de route.

## Phase 8 - Lot collections, listes et arbres [in progress]
- [x] Définir les contrats génériques de source locale/distante, clé stable, sélection et chargement.
- [x] Implémenter data list avec templates, états vide/chargement/erreur et virtualisation.
- [x] Implémenter pager contrôlé et compatible serveur.
- [x] Implémenter tree, tree item et tree level.
- [x] Ajouter expansion paresseuse, sélection simple/multiple et navigation clavier d'arbre.
- [ ] Gérer gros volumes, synchronisation de l'expansion contrôlée et conservation d'état.
- [ ] Ajouter des tests de performance réalistes, d'accessibilité navigateur et de rendu déterministe.
- [ ] Gate : jeux de données représentatifs verts, sans fuite d'état dans le lot.

## Phase 9 - Lot DataGrid [in progress]
- [x] Extraire exhaustivement les paramètres, événements, templates et extensions DataGrid utilisés par projet.
- [x] Définir une API de grille par capacités plutôt qu'une copie de l'API Radzen.
- [x] Implémenter table sémantique, colonnes typées et templates de cellule/en-tête.
- [x] Implémenter tri simple et multiple stable.
- [x] Implémenter filtres typés, opérateurs utilisés et composition serveur.
- [x] Implémenter pagination locale et chargement distant annulable.
- [x] Implémenter sélection simple/multiple et clés de ligne stables.
- [x] Implémenter édition en ligne ou formulaire uniquement pour les scénarios observés.
- [ ] Achever et prouver regroupement, agrégats et expansion pour les usages inventoriés.
- [x] Implémenter redimensionnement, ordre et visibilité des colonnes sans style inline.
- [ ] Implémenter une virtualisation réelle des lignes/colonnes selon les mesures.
- [ ] Construire une projection locale unique réutilisée pour page, total et groupes.
- [ ] Garantir dans un navigateur clavier, focus, annonces de tri et associations en-têtes/cellules.
- [ ] Établir des budgets sur 100, 1 000 et 10 000 lignes avec colonnes, tri, filtre et groupes réalistes.
- [ ] Gate : capacités de grille inventoriées couvertes dans le catalogue et budgets réalistes respectés.

## Phase 10 - Lot graphiques et jauges [in progress]
- [x] Extraire les combinaisons de séries, axes, formats, événements et volumes réellement utilisées.
- [x] Définir un moteur SVG propre avec palette, thèmes et attributs CSP sûrs.
- [ ] Implémenter les domaines et transformations réels de category axis et value axis.
- [x] Implémenter legend, grid lines, markers, labels et options de tooltip.
- [x] Implémenter line series.
- [ ] Implémenter area series et stacked area series avec baselines cumulées.
- [ ] Implémenter bar series, column series et stacked column series avec empilement réel.
- [x] Implémenter pie series et donut series.
- [x] Implémenter bar options réellement utilisées.
- [x] Implémenter arc gauge, scale et scale values.
- [ ] Ajouter redimensionnement, données vides, valeurs extrêmes et formats culturels sans écrêtage implicite 0-100.
- [ ] Fournir et tester une description textuelle et un tableau de données alternatif accessible.
- [ ] Tester géométrie SVG, interactions, valeurs extrêmes et performance.
- [ ] Gate : tous les types de graphiques inventoriés rendus correctement dans le catalogue sans style inline.

## Phase 11 - Lot chronologie et planification [in progress]
- [ ] Définir le contrat de récurrence et documenter ou implémenter son expansion selon les usages.
- [x] Implémenter timeline et timeline item.
- [x] Implémenter scheduler avec source locale/distante et événements contrôlés.
- [x] Implémenter day view, week view et month view.
- [ ] Gérer et prouver culture, fuseaux explicites, heure d'été, chevauchements et navigation temporelle.
- [ ] Injecter un `TimeProvider` pour rendre l'action Aujourd'hui déterministe.
- [ ] Garantir dans un navigateur clavier, libellés temporels et alternative accessible.
- [ ] Tester changements de fuseau, offsets DST, bornes, volumes et rendu responsive.
- [ ] Gate : scénarios temporels représentatifs verts sur dates limites dans le catalogue.

## Phase 12 - Lot éditeur HTML [in progress]
- [x] Recenser commandes, formats, collages, pièces jointes et sorties réellement utilisés.
- [x] Définir le modèle de document, la politique de sanitisation et les formats autorisés.
- [x] Implémenter l'éditeur sans `execCommand`, `eval` ni gestionnaire inline.
- [x] Implémenter bold, italic, subscript et superscript.
- [x] Implémenter indent et outdent.
- [x] Implémenter undo et redo déterministes.
- [x] Implémenter separator et custom tool via une API contrôlée.
- [ ] Gérer collage, sélection, IME, clavier et lecture d'écran avec conservation réelle du caret.
- [x] Ajouter les tests de sécurité XSS, sérialisation et round-trip.
- [ ] Borner l'historique par nombre ou octets et éviter la sanitisation complète du document à chaque frappe.
- [ ] Exécuter la collecte CSP de l'éditeur dans un navigateur réel.
- [ ] Gate : documents représentatifs préservés en round-trip, payloads XSS bloqués et interactions navigateur validées.

## Phase 13 - Consolidation audit, preuves et qualité [in progress]
- [ ] Exécuter le plan canonique de correction exhaustive `plans/PLAN-003-correction-findings-audit.md` avant les migrations consommatrices.
- [ ] Batch A : corriger les paramètres dynamiques DataGridColumn, Numeric invariant, Tree contrôlé et focus trap Dialog avec régressions ciblées.
- [ ] Batch A : borner `/csp-report`, masquer les messages internes Upload et renforcer les recommandations de validation serveur des fichiers.
- [ ] Batch A : stabiliser les tests de performance et d'annulation sans délais fixes, avec échauffement et synchronisation explicite.
- [ ] Batch A : décider et appliquer par lots la convention `.razor.cs`/`GEN004` sans changement de comportement.
- [ ] Batch B : remplacer la gate CSP HTTP par un scénario navigateur exercé et une collecte de rapports réelle.
- [ ] Batch B : remplacer la baseline API regex par une extraction sémantique exhaustive et des fixtures négatifs par catégorie de membre public.
- [ ] Batch B : lier la couverture 110/110 et le catalogue à des tests comportementaux, états et variantes prouvés.
- [ ] Batch B : compléter les tests DataGrid, charts, Scheduler DST, HtmlEditor, navigateur et accessibilité outillée.
- [ ] Batch B : paramétrer ou localiser toutes les chaînes visibles et labels accessibles selon `STD-I18N`.
- [ ] Batch B : corriger puis verrouiller en CI `Generate-RadzenSurfaceInventory.ps1` et `Generate-RadzenInventory.ps1` afin que contrats et inventaires ne réintroduisent plus de formulations obsolètes.
- [ ] Batch B : documenter la stratégie de tests multi-hôte, navigateur, accessibilité et limites de chaque gate.
- [ ] Batch C : produire dans une tâche isolée un audit de provenance reproductible avec versions/SHA, manifestes, hashes, scanner versionné et résultats bruts.
- [ ] Batch C : ajouter une gate CI de provenance sur sources, DLL/PDB, CSS, JavaScript, NuGet et symboles avec paquet négatif contaminé.
- [ ] Batch C : faire publier uniquement un artefact CI ayant passé tests, contenu NuGet et provenance.
- [ ] Batch C : conserver des fiches clean-room datées pour les évolutions futures sans fabriquer de preuves rétroactives.
- [ ] Batch C : borner les affirmations de provenance à la copie directe ou mécanique réellement testée.
- [ ] Gate : aucun finding `critical` ou `high` ouvert, preuves reproductibles et documentation/générateurs synchronisés avant migration consommatrice.

## Phase 14 - Sortie complète de Radzen dans Aetheus [todo]
- [ ] Recalculer la baseline Aetheus sur Razor, C#, projets, CSS, JavaScript et ressources.
- [ ] Migrer chaque écran restant selon le registre de couverture, sans changement métier opportuniste.
- [ ] Remplacer les modèles, enums, services et extensions Radzen encore référencés en C#.
- [ ] Retirer imports, composants hôtes, thèmes, feuilles, scripts et initialisation Radzen.
- [ ] Retirer `PackageReference Radzen.Blazor` et verrouiller sa non-réintroduction.
- [ ] Exécuter tests unitaires, intégration, E2E et scénarios métier Aetheus.
- [ ] Lancer Aetheus avec son lanceur canonique et vérifier visuellement les parcours via CDP/Playwright adapté.
- [ ] Collecter les violations CSP sur plusieurs lancements propres et exiger zéro violation imputable à l'UI.
- [ ] Comparer accessibilité, temps de rendu, poids et consommation mémoire à la baseline.
- [ ] Regénérer l'inventaire et prouver zéro balise, type, service ou ressource Radzen dans Aetheus.
- [ ] Gate : Aetheus fonctionne sans paquet Radzen et sert de référence de migration pour le parc.

## Phase 15 - Migration de tous les autres projets [todo]
- [ ] Migrer Astraia.Front et Astraia.Perf, puis utiliser Astraia comme validation de couverture maximale.
- [ ] Migrer Lexaidos.Web.
- [ ] Migrer Bellwether.
- [ ] Migrer Pronoia.Shared.Blazor, Pronoia.Front, Pronoia.App et leurs tests.
- [ ] Migrer Orpheus.
- [ ] Migrer Atlas.
- [ ] Migrer Phaios.Web et Phaios.Desktop.
- [ ] Migrer Ates.Shared.Blazor, Ates.Front et Ates.App.
- [ ] Migrer Ats.Front et Olbios.Front.
- [ ] Migrer Portfolio.Editor, PoeUtils et Hyb.
- [ ] Mettre à jour les modèles `_Generic` pour qu'aucun nouveau projet ne réintroduise Radzen.
- [ ] Revalider chaque projet avec son launcher, ses tests et sa politique CSP propres.
- [ ] Traiter les projets archivés : migration s'ils sont réactivables, sinon gel documenté sans influencer la release active.
- [ ] Gate : zéro dépendance ou usage Radzen dans tous les projets actifs et modèles de création.

## Phase 16 - Stabilisation 1.0 et maintenance [todo]
- [ ] Regénérer l'inventaire global et conserver le rapport zéro Radzen comme artefact de release.
- [ ] Exécuter la matrice complète Server, WASM, Auto et Hybrid sur les plateformes disponibles.
- [ ] Exécuter les audits accessibilité automatisés et les parcours clavier manuels instrumentés.
- [ ] Exécuter les tests CSP navigateur avec collecte de rapports sur tout le catalogue.
- [ ] Vérifier les budgets de poids, rendu, allocations et gros volumes avec scénarios réalistes.
- [ ] Geler et comparer exhaustivement la surface API publique.
- [ ] Finaliser documentation, exemples, guides de migration et matrice de compatibilité après fermeture des gates.
- [ ] Vérifier le paquet NuGet, symboles, provenance, licence et reproductibilité.
  - Contenu nominal, symboles, dépendance client et licence sont inspectés ; le scan de contenu, la provenance reproductible et la reproductibilité bit-à-bit restent ouverts, voir `docs/reproducibility.md`.
- [ ] Publier une release candidate consommée par Aetheus et Astraia sans référence projet locale.
- [ ] Corriger les régressions observables sans affaiblir tests, CSP ou accessibilité.
- [ ] Publier `OmniEurope.Blazor` 1.0 sur GitHub et NuGet.
- [ ] Installer une veille de régression empêchant toute réintroduction de Radzen.
- [ ] Gate : release 1.0 reproductible, projets actifs verts et preuve publique de suppression de Radzen.
