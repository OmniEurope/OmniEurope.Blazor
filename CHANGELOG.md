# Journal des modifications

Les changements notables de ce projet seront documentés ici selon le format Keep a Changelog.

## [Unreleased]

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
- Tests bUnit du cycle de dépliement du menu latéral, des paramètres `Variant`, `Icon` et `Title` d'`OmniAlert`, de la forme compacte d'`OmniMultiSelect` dans les deux cultures, et du contrat asynchrone d'`OmniOverlayService`.
- Garde de convention exigeant `@using Microsoft.AspNetCore.Components.Web` dans chaque `_Imports.razor` des tests et des samples, sans lequel un `@onchange` compile en attribut HTML littéral au lieu d'un gestionnaire d'événement.

### Changed

- `OmniDataGridColumn.Width` devient une longueur CSS et `OmniDataGridColumnWidthChange.Width` une chaîne ; l'énumération `OmniDataGridColumnWidth` est retirée au profit de largeurs réelles.
- `OmniDataGridLoadRequest` expose `Skip` et `Top`, et `OmniDataGridFilter` porte une seconde condition avec son opérateur logique.
- `OmniPager` gagne première et dernière page, numéros de page, sélecteur de taille de page, libellés par bouton et alignement.
- Remplacement de la référence serveur `Microsoft.AspNetCore.App` par le paquet client-compatible `Microsoft.AspNetCore.Components.Web`.
- Francisation des libellés accessibles par défaut d'`OmniAppearanceToggle`, `OmniProgressBar` et `OmniSidebarToggle`.
- `OmniDataGridColumnFilterType.MultiCombo` est retirée : la forme est désormais `MultiSelect` avec le paramètre de colonne `FilterSearchable`, un axe pour la forme du contrôle et une option pour la recherche.
- Précision des limites de preuve du registre de couverture, de la procédure clean-room, des contrôles CSP et de la baseline API après audit et revue adversariale.

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

