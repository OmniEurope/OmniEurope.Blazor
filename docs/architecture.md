# Architecture

`OmniEurope.Blazor` reste volontairement une seule Razor Class Library tant que des frontières supplémentaires ne sont pas justifiées par des consommateurs réels.

## Structure

- `src/OmniEurope.Blazor/Components` : point d'import commun et familles `Actions`, `Charts`, `Data`, `Diagram`, `Editor`, `Feedback`, `Forms`, `Foundation`, `Layout`, `Navigation`, `Overlays`, `Pages`, `Preferences` (services des préférences de l'utilisateur, [preferences.md](preferences.md)), `Scheduling`, `Selection` et `Theming` ; l'espace de noms public reste `OmniEurope.Blazor.Components` indépendamment du dossier ;
- `src/OmniEurope.Blazor/Internal` : utilitaires non exposés (moteurs, catalogues de thèmes et de palettes, gardes CSP) ;
- `src/OmniEurope.Blazor/Localization` et `src/OmniEurope.Blazor/Resources` : localiseur de remplacement des textes et ressources `AppStrings` des 24 langues ([localization.md](localization.md)) ;
- `src/OmniEurope.Blazor/Styles` : parties ordonnées de la feuille de style, assemblées par le build en une seule `omnieurope.blazor.css`, servie minifiée en Release (donc dans le paquet) et lisible en Debug (`eng/OmniEurope.Stylesheet.targets`) ;
- `src/OmniEurope.Blazor/wwwroot` : polices servies et modules JavaScript chargés à la demande ;
- `src/OmniEurope.Blazor.Analyzers` : l'analyseur `OE0001`, livré dans le paquet sous `analyzers/dotnet/cs` ([analyzers.md](analyzers.md)) ;
- `tests/OmniEurope.Blazor.Tests` : contrat de rendu, garde-fous CSP, gardes d'architecture, de conventions et de couverture de la vitrine ;
- `tests/OmniEurope.Blazor.Analyzers.Tests` : tests de `OE0001` sur la sortie réelle du générateur Razor ;
- `samples/` : hôte Server de référence (`OmniEurope.Blazor.Catalog`) et hôtes de fumée WebAssembly, Interactive Auto (serveur et client) et MAUI Hybrid, ce dernier hors de `OmniEurope.Blazor.slnx` ([compatibility.md](compatibility.md)) ;
- `site/OmniEurope.Blazor.Showcase` : vitrine, front WebAssembly (page produit, documentation, galerie, personnalisateur de thème), avec ses projets `site/OmniEurope.Blazor.Showcase.Back` (API ASP.NET Core, EF Core, PostgreSQL), `.Shared` (contrats) et `.Examples` (exemples de la galerie) ;
- `docs` : contrats, conventions, guides de famille et plans numérotés (`docs/plans`) ;
- `eng` : scripts de vérification et de génération, décrits dans [testing.md](testing.md) (portée) et [reproducibility.md](reproducibility.md) (verrous et paquet), analyseurs de conventions du dépôt `GEN001` à `GEN008` (`eng/OmniEurope.Analyzers`, tests dans `eng/OmniEurope.Analyzers.Tests`) et extracteur de la baseline d'API publique (`eng/OmniEurope.PublicApiGuard`) ;
- `artifacts/packages` : paquets locaux, ignorés par Git.

Les composants complexes conservent une façade déclarative publique et délèguent leurs mécanismes à des moteurs internes : projection/chargement pour la grille, projection/domaines pour les graphiques et coordination ordonnée pour les superpositions. Les contextes en cascade de grille, onglets, étapes et arbre ne font pas partie de l'API publique.

## Direction des dépendances

Les hôtes de démonstration et les tests dépendent de la RCL; la RCL ne dépend d'aucun hôte. Les composants publics peuvent dépendre des moteurs `Internal`. Deux sortes de classes y vivent. Un moteur pur (projection, domaines de graphique, lecture de tableau importé, coordination des superpositions) travaille sur des données et des fonctions explicites et ne dépend d'aucun composant public concret. Un adaptateur de composant porte une responsabilité extraite d'un composant (état, édition, export, colonnes, clavier, interop) et reçoit ce composant à sa construction : c'est le cas des collaborateurs de `OmniDataGrid<TItem>` (`Grid*`) et de `OmniMindMap` (`MindMap*`). Cette dépendance est assumée (décision du 2026-10-12, audit ciblé ARCH-001) : le découpage sert la taille et la lisibilité des fichiers, pas l'inversion des dépendances. Un calcul qui n'a besoin ni du rendu ni de l'interop est extrait en moteur pur quand un test isolé le demande, pas par principe. La composition entre enfants et conteneurs passe par des contextes internes, tandis que les consommateurs ne voient que les paramètres et services publics documentés.

La taxonomie canonique est celle des 16 dossiers de `Components`. Toute vue agrégée dans la roadmap, le catalogue ou les registres de couverture doit conserver ces noms, quitte à présenter ensuite des regroupements secondaires explicitement étiquetés.

