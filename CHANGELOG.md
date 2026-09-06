# Journal des modifications

Les changements notables de ce projet seront documentés ici selon le format Keep a Changelog.

## [1.0.0] - 2026-09-06

### Added

- Structure initiale de la Razor Class Library et du paquet NuGet.
- Contrat CSP strict et procédure de développement clean-room.
- Composants pilotes `OmniButton`, `OmniCard`, `OmniStack` et `OmniAlert`.
- Inventaires générés des usages Radzen observés dans l'instantané local des projets consommateurs.
- Cibles Razor pour les 110 balises inventoriées, du socle de formulaires au DataGrid, graphiques, scheduler et éditeur HTML.
- Catalogue Interactive Server avec en-tête CSP strict, collecteur de rapports et matrice de documentation publique.
- Gardes CI pour le scan source CSP, la baseline API actuelle, les budgets, le registre de présence des cibles et le contenu NuGet.
- Sondes de compilation et publication WebAssembly et Interactive Auto, test HTTP du prérendu et des assets Auto, et compilation MAUI Blazor Hybrid.
- Virtualisation réelle de `OmniDataGrid` : défilement continu sur la totalité des lignes, index de décalages Fenwick avec mesure réelle des lignes, chargement distant par blocs à cache borné, `ScrollToIndexAsync` et hauteur de tableau paramétrable en longueur CSS via `Height`.
- Surface `OmniDataGrid` alignée sur les paramètres observés des projets consommateurs : colonnes par `Property` et `FormatString`, largeurs et colonnes gelées, opérateurs de filtre étendus avec modes `Simple`, `SimpleWithMenu` et `Advanced`, regroupements par clé avec panneau, `RowRender`, `EditMode`, `ExpandMode`, pagination complète avec numéros de page et tailles de page, `GridLines`, `Density` et mode responsive.
- Guide de famille `docs/data-components.md` pour la grille, la liste, la pagination et l'arbre.
- `OmniAlert` gagne `Variant` (`Outline` ou `Filled`) et un emplacement `Icon` rendu avant le titre.
- `OmniMultiSelect<TValue>` gagne `Presentation` (`List` ou `Compact`) et `Placeholder` : la forme compacte tient sur une ligne, résume la sélection et ouvre sa liste à la demande.
- `OmniPanelMenu` gagne `DisplayStyle` (`IconAndText` ou `Icon`), `OmniPanelMenuItem` et `OmniTabsItem` gagnent un emplacement `Icon`, et le contexte de groupe du menu latéral déplie la branche portant la page courante.
- `OmniOverlayService.OpenDialogAsync` ouvre un dialogue et attend son résultat ; `CloseDialog(object?)` répond à l'appelant, toute autre fermeture répond `null`.
- `OmniDataGrid<TItem>` gagne `AlwaysShowPager` et `ShowEditColumn`, et `OmniDataGridColumn<TItem>` gagne `FilterSearchable`.
- Quatre chaînes de ressources françaises et anglaises pour la sélection multiple compacte et la bascule du menu latéral : `MultiSelectEmpty`, `MultiSelectSelected`, `MultiSelectClear` et `PanelMenuToggle`.
- `OmniIcon` rend désormais les tracés Phosphor Icons `2.0.8` (poids `regular`, MIT) et gagne `Glyph`, qui accepte n'importe quel autre contour sans que le paquet embarque de catalogue. Nouveau type public `OmniIconGlyph`, qui valide la donnée de tracé et porte la grille du jeu d'origine. Surcoût mesuré sur une publication WebAssembly identique : +1392 octets brotli.
- `eng/vendored-assets.json` déclare les ressources tierces recopiées hors NuGet ; `Generate-Sbom.ps1` les verse dans `NOTICE.md`, dans le SBOM CycloneDX et dans les textes de licence conservés, et `Test-Sbom.ps1` les vérifie.
- Tests bUnit du cycle de dépliement du menu latéral, des paramètres `Variant`, `Icon` et `Title` d'`OmniAlert`, de la forme compacte d'`OmniMultiSelect` dans les deux cultures, et du contrat asynchrone d'`OmniOverlayService`.
- Garde de convention exigeant `@using Microsoft.AspNetCore.Components.Web` dans chaque `_Imports.razor` des tests et des samples, sans lequel un `@onchange` compile en attribut HTML littéral au lieu d'un gestionnaire d'événement.
- Site vitrine `site/OmniEurope.Blazor.Showcase`, une application WebAssembly statique à quatre sections : présentation, documentation, explorateur de composants et personnalisateur de thème. Elle se publie sans serveur applicatif, sous la même politique de sécurité stricte que la bibliothèque.
- Explorateur de composants couvrant les 109 composants publiés en 23 familles, avec leurs variantes. Chaque démonstration affiche le fichier réellement compilé et exécuté, relu depuis les ressources de l'assembly, jamais une transcription.
- Personnalisateur de thème : les 66 variables sont éditables et lues dans la feuille de style livrée plutôt que redéclarées, appliquées en direct par le CSSOM, exportables en CSS, et conservées dans le navigateur. Catalogue de 25 palettes transposées de Bootswatch (licence MIT), chacune déclinée en clair et en sombre, la moitié dérivée par ce projet et étiquetée comme telle.
- Gardes de la vitrine : couverture exhaustive des composants, rendu effectif de chaque démonstration, conformité de chaque démonstration à la politique de sécurité, et contraste WCAG vérifié sur les 50 palettes-modes.

### Changed

- `OmniDataGridColumn.Width` devient une longueur CSS et `OmniDataGridColumnWidthChange.Width` une chaîne ; l'énumération `OmniDataGridColumnWidth` est retirée au profit de largeurs réelles.
- `OmniDataGridLoadRequest` expose `Skip` et `Top`, et `OmniDataGridFilter` porte une seconde condition avec son opérateur logique.
- `OmniPager` gagne première et dernière page, numéros de page, sélecteur de taille de page, libellés par bouton et alignement.
- Remplacement de la référence serveur `Microsoft.AspNetCore.App` par le paquet client-compatible `Microsoft.AspNetCore.Components.Web`.
- Francisation des libellés accessibles par défaut d'`OmniAppearanceToggle`, `OmniProgressBar` et `OmniSidebarToggle`.
- `OmniDataGridColumnFilterType.MultiCombo` est retirée : la forme est désormais `MultiSelect` avec le paramètre de colonne `FilterSearchable`, un axe pour la forme du contrôle et une option pour la recherche.
- Précision des limites de preuve du registre de couverture, de la procédure clean-room, des contrôles CSP et de la baseline API après audit et revue adversariale.
- Variabilisation complète de la feuille de style : 66 variables dans `:root` contre 25, aucune couleur, taille de police ou arrondi ne restant codé en dur. Nouvelles familles pour le texte sur fond plein, les fonds teintés par sévérité, les ombres, l'échelle typographique, l'échelle d'arrondis et la palette de graphiques.
- `--omni-font-family` porte la pile de polices système et est appliquée par `.omni-theme-scope` ; le paquet n'embarque et ne charge aucune police web.
- Le rôle `tablist` et la navigation clavier des onglets portent désormais sur `.omni-tabs__viewport`, la bande qui défile, et non plus sur `.omni-tabs`.

### Fixed

- Rétablissement de la compilation de la solution : localisation statique dans `OmniUpload`, culture explicite dans `OmniScheduler`, références xUnit et conversions de groupes de méthodes dans l'outillage `eng`.
- Les tests bUnit enregistrent désormais les services de la bibliothèque comme un hôte réel, ce qui rétablit la résolution du localizer par les composants.
- `Localize` utilise l'indexeur sans arguments quand il n'y en a pas, au lieu de formater une liste vide.
- Rechargement effectif de `OmniDataGrid` lorsque le délégué `Load` change.
- Annonce accessible des erreurs de validation, focus automatique sur le premier contrôle invalide et prise en charge des entrées autonomes hors `EditForm`.
- Assainissement de la valeur initiale de l'éditeur HTML et protection contre les résultats asynchrones obsolètes dans Autocomplete, DataList, DataGrid et Scheduler.
- Coûts répétés supprimés dans DropDown et les séries Pie/Donut, sans changer leurs contrats publics.
- Correction de l'exemple `OmniSkeleton` du catalogue pour utiliser le paramètre public `LineCount`.
- Remontée de l'état actif à travers les groupes imbriqués d'`OmniPanelMenuItem` : une feuille rapporte la route qu'elle satisfait et un groupe rapporte la page tenue par ses propres enfants, puis re-rapporte à son parent, si bien qu'un menu de trois niveaux ou plus déplie la branche courante même quand le groupe intermédiaire ne porte aucun `Href`.
- Fin de l'attente infinie d'`OmniOverlayService.OpenDialogAsync` lorsque la même instance d'`OmniDialogRequest` est rouverte avant sa fermeture : l'appelant déplacé reçoit `null`, conformément au contrat documenté de la fermeture non explicite.
- Rétablissement des sigils Razor dans l'exemple `FilterTemplate` de `docs/data-components.md`, qui ne compilait pas tel quel chez un consommateur.

