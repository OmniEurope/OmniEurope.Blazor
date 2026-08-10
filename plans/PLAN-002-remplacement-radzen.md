# Plan - Remplacement complet de Radzen

> Canonical plan: `plans/PLAN-002-remplacement-radzen.md`
> Last updated: 2026-08-10

## Phase 1 - Fondation et publication initiale [in progress]
- [x] Abandonner le fork Radzen au profit d'une implémentation clean-room indépendante.
- [x] Recenser les balises Radzen dans tous les projets sous `C:\Dev`.
- [x] Établir le contrat CSP sans `unsafe-inline` ni `unsafe-eval`.
- [x] Créer la Razor Class Library, les tests et le packaging NuGet `OmniEurope.Blazor`.
- [x] Appliquer la licence EUPL-1.2 et les fichiers de gouvernance.
- [x] Implémenter les pilotes `OmniButton`, `OmniCard`, `OmniStack` et `OmniAlert`.
- [x] Prouver le build Release, les tests unitaires, le scanner CSP et le paquet NuGet.
- [x] Initialiser GitFlow avec `main`, `develop` et les préfixes standards.
- [x] Raccorder `origin` à `https://github.com/OmniEurope/OmniEurope.Blazor.git`.
- [ ] Publier `main` et `develop` après autorisation explicite du contenu public.
- [ ] Gate : dépôt GitHub lisible, CI verte et paquet alpha téléchargeable comme preuve de reprise.

## Phase 2 - Contrats, matrice de couverture et banc d'essai [todo]
- [ ] Étendre l'inventaire aux types C#, services, enums, extensions, CSS, JavaScript et ressources Radzen.
- [ ] Extraire pour chaque composant les paramètres, événements, templates et comportements réellement utilisés.
- [ ] Créer un registre de couverture reliant chaque usage observé à une capacité OmniEurope, un lot et un état.
- [ ] Définir les conventions publiques de noms, paramètres, événements, génériques et valeurs nulles.
- [ ] Définir la compatibilité Blazor Server, WebAssembly, Interactive Auto et MAUI Blazor Hybrid.
- [ ] Stabiliser les design tokens, thèmes clair/sombre, densités, tailles et états visuels en CSS statique.
- [ ] Définir les règles HTML/ARIA, clavier, focus, annonces live et contraste applicables à chaque famille.
- [ ] Ajouter un site catalogue local affichant tous les états et variations de chaque composant.
- [ ] Ajouter un hôte CSP strict qui collecte les violations du navigateur et échoue dès la première.
- [ ] Ajouter les contrôles automatiques de source, rendu, accessibilité, API publique et contenu NuGet.
- [ ] Définir le budget de poids, de rendu et d'allocations pour les composants simples et complexes.
- [ ] Définir la politique SemVer, dépréciation, migration et rupture d'API.
- [ ] Documenter le protocole clean-room par fiche de composant, sans lecture du code Radzen.
- [ ] Gate : matrice complète des 110 composants et banc d'essai vert avant extension de la bibliothèque.

## Phase 3 - Lot fondations, contenu et disposition [todo]
- [ ] Durcir les quatre pilotes existants avec tests clavier, ARIA, thèmes et paramètres supplémentaires observés.
- [ ] Implémenter texte et titres avec HTML sémantique configurable.
- [ ] Implémenter icônes avec SVG propre, libellés accessibles et aucun asset Radzen.
- [ ] Implémenter badges, liens, images et skeletons.
- [ ] Implémenter lignes, colonnes et grille de disposition responsive par classes finies.
- [ ] Implémenter layout, body/main et header sémantiques.
- [ ] Implémenter sidebar et sidebar toggle avec état contrôlé et focus cohérent.
- [ ] Implémenter fieldset avec legend accessible.
- [ ] Implémenter progress bar linéaire et circulaire avec valeurs ARIA.
- [ ] Implémenter thèmes et appearance toggle sans injection de style.
- [ ] Vérifier les états hover, focus, disabled, busy, loading, vide et erreur.
- [ ] Documenter les correspondances de migration pour cette famille.
- [ ] Migrer un premier écran Aetheus représentatif avec coexistence Radzen/OmniEurope.
- [ ] Mesurer le différentiel visuel, le nombre d'usages retirés et les violations CSP.
- [ ] Gate : lot de 15 capacités couvert, catalogue visuel validé et premier écran Aetheus sans Radzen.

## Phase 4 - Lot actions, services et superpositions [todo]
- [ ] Finaliser les contrats d'action communs, commandes asynchrones et prévention du double clic.
- [ ] Implémenter split button et ses items avec navigation clavier.
- [ ] Implémenter toggle button avec états pressé et désactivé.
- [ ] Créer l'hôte de services OmniEurope remplaçant `RadzenComponents`.
- [ ] Créer une couche de portail unique pour les superpositions.
- [ ] Implémenter dialog avec focus trap, fermeture contrôlée et restauration du focus.
- [ ] Implémenter notifications avec régions live et files d'attente.
- [ ] Implémenter tooltip avec déclencheurs clavier, souris et tactile.
- [ ] Implémenter context menu sans gestionnaire HTML inline.
- [ ] Gérer z-index, scroll lock, clic extérieur, Escape et superpositions imbriquées.
- [ ] Ajouter tests unitaires, intégration navigateur et accessibilité de la famille.
- [ ] Migrer dans Aetheus les services dialog, notification, tooltip et context menu.
- [ ] Gate : aucune dépendance aux services Radzen dans les écrans Aetheus migrés et zéro violation CSP.

## Phase 5 - Lot formulaires et validation [todo]
- [ ] Définir une base commune `InputBase<T>` pour identifiants, descriptions, erreurs et états.
- [ ] Implémenter text box et password avec autocomplete et révélation contrôlée.
- [ ] Implémenter text area avec comptage et limites accessibles.
- [ ] Implémenter numeric avec culture, bornes, pas et saisie incomplète.
- [ ] Implémenter checkbox et switch avec liaison nullable lorsque requise.
- [ ] Implémenter label et form field sans dupliquer la sémantique native.
- [ ] Implémenter template form autour de `EditContext`.
- [ ] Implémenter required validator.
- [ ] Implémenter length validator.
- [ ] Implémenter email validator.
- [ ] Implémenter compare validator.
- [ ] Uniformiser messages, validation différée, soumission et focus sur première erreur.
- [ ] Tester cultures, clavier, lecteurs d'écran, serveur, WASM et Hybrid.
- [ ] Migrer les formulaires simples d'Aetheus et supprimer les validateurs Radzen correspondants.
- [ ] Gate : formulaires Aetheus sélectionnés fonctionnellement équivalents et sans balise ni type Radzen couvert.

## Phase 6 - Lot sélecteurs et entrées avancées [todo]
- [ ] Définir le modèle commun d'options, valeurs, texte, groupes, recherche et valeur vide.
- [ ] Implémenter drop-down simple et multiple avec virtualisation si l'usage l'exige.
- [ ] Implémenter autocomplete avec annulation, debounce et annonces de résultats.
- [ ] Implémenter list box et checkbox list.
- [ ] Implémenter radio button list et ses items.
- [ ] Implémenter select bar et ses items.
- [ ] Implémenter date picker avec culture, clavier, bornes et calendrier accessible.
- [ ] Implémenter slider avec orientation, pas, bornes et valeur ARIA.
- [ ] Implémenter color picker sans style inline généré.
- [ ] Implémenter upload avec progression, annulation, validation et reprise selon les usages.
- [ ] Tester gros volumes, valeurs nulles, rechargements asynchrones et formulaires imbriqués.
- [ ] Migrer les sélecteurs Aetheus du plus fréquent au plus complexe.
- [ ] Gate : scénarios Aetheus de saisie et sélection verts sous CSP stricte et navigation clavier complète.

## Phase 7 - Lot navigation structurée [todo]
- [ ] Définir les contrats communs d'item, route, sélection, expansion et autorisation.
- [ ] Implémenter panel menu et panel menu item.
- [ ] Implémenter breadcrumb et breadcrumb item.
- [ ] Implémenter tabs et tabs item avec roving tabindex.
- [ ] Implémenter steps et steps item avec validation de transition.
- [ ] Implémenter profile menu et profile menu item.
- [ ] Intégrer `NavigationManager`, routes actives et navigation annulable.
- [ ] Tester clavier, focus, responsive, historique et rendu préinteractif.
- [ ] Migrer navigation principale, secondaire et parcours guidés d'Aetheus.
- [ ] Gate : navigation Aetheus cible sans Radzen, sans régression de route et conforme aux motifs ARIA.

## Phase 8 - Lot collections, listes et arbres [todo]
- [ ] Définir les contrats génériques de source locale/distante, clé stable, sélection et chargement.
- [ ] Implémenter data list avec templates, états vide/chargement/erreur et virtualisation.
- [ ] Implémenter pager contrôlé et compatible serveur.
- [ ] Implémenter tree, tree item et tree level.
- [ ] Ajouter expansion paresseuse, sélection simple/multiple et navigation clavier d'arbre.
- [ ] Gérer gros volumes, annulation des chargements et conservation d'état.
- [ ] Ajouter tests de performance, accessibilité et rendu déterministe.
- [ ] Migrer listes, pagers et arbres Aetheus.
- [ ] Gate : jeux de données Aetheus réels verts, sans fuite d'état ni type Radzen dans le lot.

## Phase 9 - Lot DataGrid [todo]
- [ ] Extraire exhaustivement les paramètres, événements, templates et extensions DataGrid utilisés par projet.
- [ ] Définir une API de grille par capacités plutôt qu'une copie de l'API Radzen.
- [ ] Implémenter table sémantique, colonnes typées et templates de cellule/en-tête.
- [ ] Implémenter tri simple et multiple stable.
- [ ] Implémenter filtres typés, opérateurs utilisés et composition serveur.
- [ ] Implémenter pagination locale et chargement distant annulable.
- [ ] Implémenter sélection simple/multiple et clés de ligne stables.
- [ ] Implémenter édition en ligne ou formulaire uniquement pour les scénarios observés.
- [ ] Implémenter regroupement, agrégats et expansion seulement lorsqu'ils sont prouvés nécessaires.
- [ ] Implémenter redimensionnement, ordre et visibilité des colonnes sans style inline.
- [ ] Implémenter virtualisation lignes/colonnes selon les mesures.
- [ ] Garantir clavier, focus, annonces de tri et associations en-têtes/cellules.
- [ ] Établir des budgets sur 100, 1 000 et 10 000 lignes avec données réalistes.
- [ ] Migrer les grilles Aetheus par complexité croissante.
- [ ] Gate : toutes les grilles Aetheus couvertes, budgets respectés et zéro `RadzenDataGrid*`.

## Phase 10 - Lot graphiques et jauges [todo]
- [ ] Extraire les combinaisons de séries, axes, formats, événements et volumes réellement utilisées.
- [ ] Définir un moteur SVG propre avec palette, thèmes et attributs CSP sûrs.
- [ ] Implémenter chart, axis title, category axis et value axis.
- [ ] Implémenter legend, grid lines, markers, labels et options de tooltip.
- [ ] Implémenter line series.
- [ ] Implémenter area series et stacked area series.
- [ ] Implémenter bar series, column series et stacked column series.
- [ ] Implémenter pie series et donut series.
- [ ] Implémenter bar options réellement utilisées.
- [ ] Implémenter arc gauge, scale et scale values.
- [ ] Ajouter redimensionnement, données vides, valeurs extrêmes et formats culturels.
- [ ] Fournir description textuelle et tableau de données alternatif accessible.
- [ ] Tester snapshots SVG sémantiques, interactions et performance.
- [ ] Migrer les graphiques Aetheus puis Astraia utilisés comme catalogue étendu.
- [ ] Gate : tous les types de graphiques inventoriés rendus sans style inline et scénarios consommateurs verts.

## Phase 11 - Lot chronologie et planification [todo]
- [ ] Définir modèles temporels, fuseaux, récurrence et intervalles à partir des usages.
- [ ] Implémenter timeline et timeline item.
- [ ] Implémenter scheduler avec source locale/distante et événements contrôlés.
- [ ] Implémenter day view, week view et month view.
- [ ] Gérer culture, fuseaux, heure d'été, chevauchements et navigation temporelle.
- [ ] Garantir clavier, libellés temporels et alternative accessible.
- [ ] Tester changements de fuseau, bornes, volumes et rendu responsive.
- [ ] Migrer les projets utilisant timeline et scheduler.
- [ ] Gate : scénarios temporels observés verts sur dates limites et sans composant Radzen.

## Phase 12 - Lot éditeur HTML [todo]
- [ ] Recenser commandes, formats, collages, pièces jointes et sorties réellement utilisés.
- [ ] Définir le modèle de document, la politique de sanitisation et les formats autorisés.
- [ ] Implémenter l'éditeur sans `execCommand`, `eval` ni gestionnaire inline.
- [ ] Implémenter bold, italic, subscript et superscript.
- [ ] Implémenter indent et outdent.
- [ ] Implémenter undo et redo déterministes.
- [ ] Implémenter separator et custom tool via une API contrôlée.
- [ ] Gérer collage, sélection, IME, clavier, lecture d'écran et contenu malveillant.
- [ ] Ajouter tests de sécurité XSS, sérialisation, round-trip et CSP navigateur.
- [ ] Migrer l'éditeur du projet consommateur puis comparer les documents produits.
- [ ] Gate : contenu existant préservé, payloads XSS bloqués et zéro `RadzenHtmlEditor*`.

## Phase 13 - Sortie complète de Radzen dans Aetheus [todo]
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

## Phase 14 - Migration de tous les autres projets [todo]
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

## Phase 15 - Stabilisation 1.0 et maintenance [todo]
- [ ] Regénérer l'inventaire global et conserver le rapport zéro Radzen comme artefact de release.
- [ ] Exécuter la matrice complète Server, WASM, Auto et Hybrid sur les plateformes disponibles.
- [ ] Exécuter les audits accessibilité automatisés et les parcours clavier manuels instrumentés.
- [ ] Exécuter les tests CSP navigateur avec collecte de rapports sur tout le catalogue.
- [ ] Vérifier les budgets de poids, rendu, allocations et gros volumes.
- [ ] Geler et comparer la surface API publique.
- [ ] Finaliser documentation, exemples, guides de migration et matrice de compatibilité.
- [ ] Vérifier le paquet NuGet, symboles, provenance, licence et reproductibilité.
- [ ] Publier une release candidate consommée par Aetheus et Astraia sans référence projet locale.
- [ ] Corriger les régressions observables sans affaiblir tests, CSP ou accessibilité.
- [ ] Publier `OmniEurope.Blazor` 1.0 sur GitHub et NuGet.
- [ ] Installer une veille de régression empêchant toute réintroduction de Radzen.
- [ ] Gate : release 1.0 reproductible, projets actifs verts et preuve publique de suppression de Radzen.
