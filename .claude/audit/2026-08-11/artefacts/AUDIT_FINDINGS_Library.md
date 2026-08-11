# Findings d'audit 360 - Library

> Audit: 2026-08-11
> Les blocs sont ajoutés fichier par fichier. Une absence de finding est consignée explicitement par `RAS`.

<a id="src-omnieurope-blazor-imports-razor"></a>
## `src/OmniEurope.Blazor/_Imports.razor`

RAS

<a id="src-omnieurope-blazor-components-omnialert-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniAlert.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 14 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnialert-razor-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniAlert.razor.cs`

RAS

<a id="src-omnieurope-blazor-components-omniappearancetoggle-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniAppearanceToggle.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 12 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Les libellés visibles et l'aria-label sont codés en dur en français au lieu d'un contrat de localisation - lignes 20-26 - règle STD-I18N et infrastructure absente selon AUDIT_KIT.md - recommandation: Codex peut exposer des ressources par défaut localisables et permettre leur surcharge par l'hôte.

<a id="src-omnieurope-blazor-components-omniarcgauge-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniArcGauge.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 7 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le libellé accessible par défaut `Jauge` est codé en dur - ligne 8 - règle STD-I18N - recommandation: Codex peut résoudre ce texte depuis les ressources de la bibliothèque.

<a id="src-omnieurope-blazor-components-omniarcgaugescale-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniArcGaugeScale.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 8 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omniarcgaugescalevalue-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniArcGaugeScaleValue.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 6 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omniareaseries-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniAreaSeries.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 3 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Moyen] [Architecture] La série projette directement toutes les valeurs sur l'échelle fixe 0-100 via `OmniChartGeometry`, sans domaine partagé avec les axes - lignes 4-8 - source: ARCH-01 dans AUDIT_ARCHITECTURE.md - recommandation: Codex peut faire consommer à la série une projection calculée par un contexte interne de graphique.

<a id="src-omnieurope-blazor-components-omniautocomplete-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniAutocomplete.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 38 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Les annonces, états de sélection et messages de validation sont codés en dur en français - lignes 103-106, 123 et 130 - règle STD-I18N - recommandation: Codex peut les fournir par ressources localisables avec pluralisation.
- [Moyen] [Fiabilité] Les exceptions non liées à l'annulation levées par `Search` ne sont pas capturées; elles interrompent le traitement de l'événement et ne donnent aucun état d'erreur au composant - lignes 96-110 - recommandation: Codex peut exposer un état d'erreur récupérable et observer l'exception.
- [Faible] [Qualité] `SelectAsync` effectue un `await Task.CompletedTask` sans opération asynchrone, ce qui ajoute un faux chemin asynchrone - lignes 118-125 - recommandation: Codex peut retourner directement `Task.CompletedTask` après les mutations synchrones.

<a id="src-omnieurope-blazor-components-omniaxistitle-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniAxisTitle.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 3 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnibadge-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniBadge.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnibaroptions-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniBarOptions.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 3 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnibarseries-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniBarSeries.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 10 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Moyen] [Architecture] La largeur des barres dépend de valeurs bornées arbitrairement à 0-100 et non d'un domaine d'axe partagé - lignes 2-7 - source: ARCH-01 - recommandation: Codex peut utiliser la projection commune du graphique.

<a id="src-omnieurope-blazor-components-omnibody-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniBody.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 7 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnibreadcrumb-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniBreadcrumb.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 7 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le libellé accessible `Fil d'Ariane` est codé en dur - ligne 9 - règle STD-I18N - recommandation: Codex peut le fournir par ressource localisable.

<a id="src-omnieurope-blazor-components-omnibreadcrumbitem-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniBreadcrumbItem.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 14 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Sécurité] `Href` est rendu sans validation de schéma; une valeur non fiable telle que `javascript:` reste exécutable au clic malgré l'encodage HTML - lignes 10 et 16 - revue best-effort, non outillée - recommandation: Codex peut centraliser une validation des URI autorisant seulement les schémas et chemins explicitement supportés.

<a id="src-omnieurope-blazor-components-omnibutton-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniButton.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 18 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnibutton-razor-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniButton.razor.cs`

RAS

<a id="src-omnieurope-blazor-components-omnicard-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniCard.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 15 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnicategoryaxis-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniCategoryAxis.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 10 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnichart-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniChart.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 15 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Architecture] Le composant juxtapose axes et séries via `ChildContent` sans domaine, échelles, catégories ni baseline partagés - lignes 4-18 - source: ARCH-01 dans AUDIT_ARCHITECTURE.md - recommandation: Codex peut introduire un contexte interne calculant une projection immuable tout en conservant l'API déclarative.

<a id="src-omnieurope-blazor-components-omnicharttooltipoptions-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniChartTooltipOptions.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 3 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Les descriptions d'infobulle sont codées en dur en français - lignes 1 et 5 - règle STD-I18N - recommandation: Codex peut les fournir par ressources localisables.

<a id="src-omnieurope-blazor-components-omnicharttypes-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniChartTypes.cs`

- [Élevé] [Architecture] `Clamp`, `Xy` et `Points` imposent un domaine global fixe 0-100, ce qui tronque silencieusement les valeurs négatives ou supérieures à 100 et empêche l'alignement avec `OmniValueAxis` - lignes 10-13 - source: ARCH-01 - recommandation: Codex peut projeter les données depuis les domaines calculés du graphique plutôt que les borner.
- [Faible] [Style] Le fichier regroupe plusieurs types utilitaires de géométrie et de formatage au lieu d'un type par fichier - lignes 5-65 - source: `coding-standards.md` - recommandation: Codex peut séparer ces responsabilités lors de la stabilisation de l'API interne.

<a id="src-omnieurope-blazor-components-omnicheckbox-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniCheckBox.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 13 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur en français - ligne 36 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omnicheckboxlist-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniCheckBoxList.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 23 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur en français - ligne 58 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omnicollectiontypes-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniCollectionTypes.cs`

- [Moyen] [Architecture] `OmniTreeContext<TValue>` est public alors qu'il ne sert qu'au transport en cascade interne entre `OmniTree` et ses éléments - lignes 3-7 - source: ARCH-04 - recommandation: Codex peut rendre ce contexte interne avant 1.0 après vérification de la surface de migration.

<a id="src-omnieurope-blazor-components-omnicolorpicker-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniColorPicker.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 19 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur en français - ligne 41 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omnicolumn-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniColumn.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnicolumnseries-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniColumnSeries.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 12 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Moyen] [Architecture] La série borne les hauteurs à 0-100 sans domaine partagé avec l'axe et transforme toute valeur négative en zéro - lignes 5-8 - source: ARCH-01 - recommandation: Codex peut utiliser une projection de coordonnées commune.

<a id="src-omnieurope-blazor-components-omnicomparevalidator-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniCompareValidator.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 10 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation par défaut est codé en dur en français - ligne 17 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.
- [Moyen] [Correctness] L'accesseur de `Other` est compilé seulement à l'initialisation; un changement ultérieur de l'expression paramètre continue donc à comparer l'ancienne valeur - lignes 14 et 19-22 - recommandation: Codex peut recompiler lorsque l'identité de l'expression change.

<a id="src-omnieurope-blazor-components-omnicomponentbase-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniComponentBase.cs`

RAS

<a id="src-omnieurope-blazor-components-omnicomponentshost-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniComponentsHost.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 25 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le nom accessible de la région de notifications est codé en dur - ligne 15 - règle STD-I18N - recommandation: Codex peut l'exposer via les ressources de la bibliothèque.
- [Moyen] [Architecture] Le composant concentre dans une même frontière le contenu applicatif, le dialogue global et toutes les notifications, ce qui couple des cycles de vie indépendants - lignes 4-23 - source: ARCH-02 - recommandation: Codex peut séparer les hôtes internes de dialogue et de notifications tout en conservant un point d'enregistrement public unique.

<a id="src-omnieurope-blazor-components-omnicontextmenu-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniContextMenu.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 18 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le nom accessible `Menu contextuel` est codé en dur - ligne 26 - règle STD-I18N - recommandation: Codex peut le fournir par ressource localisable.
- [Moyen] [Accessibilité] Le conteneur `role="menu"` ne gère ni focus initial, ni déplacement par flèches, ni fermeture par Échap; il ne fournit donc pas le modèle clavier attendu d'un menu contextuel - lignes 4-16 et 24-52 - recommandation: Codex peut implémenter le focus roving et la fermeture clavier avec restauration du focus déclencheur.

<a id="src-omnieurope-blazor-components-omnidatagrid-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniDataGrid.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 145 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Fiabilité] Un chargement distant valide retournant zéro élément laisse `_remoteItems.Count == 0`; `OnParametersSetAsync` relance alors automatiquement la requête à chaque rendu - lignes 233-239 et 366-371 - recommandation: Codex peut mémoriser explicitement qu'une plage a été chargée, indépendamment du nombre d'éléments.
- [Élevé] [Style] Les libellés de tri, filtres, sélection, pagination, chargement et erreurs sont codés en dur en français - lignes 10-142 - règle STD-I18N - recommandation: Codex peut fournir un jeu complet de ressources localisables avec pluralisation.
- [Moyen] [Correctness] En mode local, le pipeline applique la pagination avant certains calculs et interactions de tri/filtrage, ce qui peut produire une page et un total incohérents avec la vue demandée - lignes 245-288 - recommandation: Codex peut formaliser puis appliquer un pipeline unique filtre, tri et pagination.
- [Moyen] [Performance] Le rendu et les opérations de grille répètent des projections et recherches linéaires sur colonnes, clés et valeurs pour chaque cellule, ce qui amplifie le coût sur les grandes collections - lignes 154-224 et 289-448 - recommandation: Codex peut calculer une vue immuable et des index par rendu ou génération de données.
- [Moyen] [Architecture] Le composant cumule en un seul fichier chargement distant, requêtage local, sélection, édition, expansion, redimensionnement et rendu - lignes 1-455 - source: ARCH-03 - recommandation: Codex peut extraire des services internes de projection et d'état sans fragmenter l'API publique.

<a id="src-omnieurope-blazor-components-omnidatagridcolumn-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniDataGridColumn.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 4 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Moyen] [Fiabilité] La définition de colonne n'est enregistrée qu'à `OnInitialized`; toute modification ultérieure de `Title`, `Value`, templates, visibilité ou largeur reste invisible à la grille - lignes 47-65 - recommandation: Codex peut réenregistrer sur les changements de paramètres avec une identité de clé stable.

<a id="src-omnieurope-blazor-components-omnidatagridtypes-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniDataGridTypes.cs`

- [Moyen] [Architecture] `OmniDataGridColumnDefinition<TItem>` et `OmniDataGridContext<TItem>` sont publics alors que leurs usages observés sont internes au couple grille/colonne - lignes 48-69 - source: ARCH-04 - recommandation: Codex peut les rendre internes avant 1.0 après vérification de compatibilité.
- [Faible] [Style] Le fichier regroupe enums, contrats de requête/résultat et contextes de rendu au lieu d'un type par fichier - lignes 5-69 - source: `coding-standards.md` - recommandation: Codex peut séparer ces types selon leur responsabilité.

<a id="src-omnieurope-blazor-components-omnidatalist-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniDataList.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 44 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Fiabilité] Un chargement réussi retournant une liste vide remplit de nouveau la condition `_items.Count == 0` et peut relancer la source à chaque cycle de paramètres; un changement de délégué `Load` avec une liste non vide n'est au contraire jamais rechargé - lignes 75-91 et 112-137 - recommandation: Codex peut suivre la clé ou génération de chargement et distinguer résultat vide de jamais chargé.
- [Élevé] [Style] Les états chargement, erreur, réessai et collection vide sont codés en dur en français - lignes 8-27 et 72-73 - règle STD-I18N - recommandation: Codex peut les fournir par fragments ou ressources localisables.

<a id="src-omnieurope-blazor-components-omnidatepicker-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniDatePicker.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 16 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur en français - ligne 57 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omnidayview-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniDayView.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 13 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le nom accessible `Vue journalière` est codé en dur - ligne 17 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omnidialog-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniDialog.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 29 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Les sentinelles de focus et le libellé de fermeture sont codés en dur en français - lignes 14, 24 et 43 - règle STD-I18N - recommandation: Codex peut les fournir par ressources localisables.
- [Moyen] [Accessibilité] Le piège de focus ne mémorise ni ne restaure l'élément déclencheur et ses sentinelles redirigent vers des cibles fixes plutôt que la liste réelle des éléments focusables - lignes 13-24 et 62-72 - recommandation: Codex peut capturer le focus d'origine, calculer les cibles focusables et le restaurer à la fermeture.

<a id="src-omnieurope-blazor-components-omnidonutseries-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniDonutSeries.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnidropdown-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniDropDown.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 36 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le placeholder et le message de validation sont codés en dur en français - lignes 44 et 88 - règle STD-I18N - recommandation: Codex peut les résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omniemailvalidator-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniEmailValidator.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 8 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur en français - ligne 12 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omnifieldset-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniFieldset.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 11 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omniformfield-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniFormField.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 18 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnifoundationtypes-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniFoundationTypes.cs`

- [Faible] [Style] Le fichier regroupe onze enums publics sans responsabilité unique de fichier - lignes 3-109 - standard un type par fichier et dette de taxonomie ARCH-05 - recommandation: Codex peut répartir les types par famille sans changer leur namespace public.

<a id="src-omnieurope-blazor-components-omnigrid-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniGrid.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnigridlines-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniGridLines.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Faible] [Fiabilité] `Count` n'est pas validé; une valeur nulle ou négative produit silencieusement une grille vide au lieu d'un contrat explicite - ligne 9 - recommandation: Codex peut imposer une borne positive dans `OnParametersSet`.

<a id="src-omnieurope-blazor-components-omniheader-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniHeader.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omniheading-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniHeading.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 25 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnihtmleditor-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniHtmlEditor.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 35 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Les libellés de toolbar, commandes, aperçu et label par défaut sont codés en dur en français - lignes 5-14, 31 et 39 - règle STD-I18N - recommandation: Codex peut les fournir par ressources localisables.
- [Moyen] [Authenticité] Les commandes gras, italique, indice et exposant transforment la totalité de la chaîne HTML au lieu de la sélection de l'utilisateur; la toolbar présente ainsi un comportement d'éditeur riche qu'elle n'implémente pas - lignes 54-61 - recommandation: Codex peut manipuler explicitement la sélection via interop ou réduire les libellés à la transformation réellement fournie.

<a id="src-omnieurope-blazor-components-omnihtmleditortypes-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniHtmlEditorTypes.cs`

RAS

<a id="src-omnieurope-blazor-components-omniicon-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniIcon.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 44 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omniimage-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniImage.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 13 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omniinputbase-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniInputBase.cs`

RAS

<a id="src-omnieurope-blazor-components-omnilabel-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniLabel.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 14 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnilayout-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniLayout.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnilegend-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniLegend.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le nom accessible `Légende` est codé en dur - ligne 10 - règle STD-I18N - recommandation: Codex peut le fournir par ressource localisable.

<a id="src-omnieurope-blazor-components-omnilengthvalidator-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniLengthValidator.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 8 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur en français - ligne 16 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omnilineseries-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniLineSeries.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 3 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Moyen] [Architecture] Les points sont bornés sur une échelle fixe 0-100 indépendante des axes - lignes 4-8 - source: ARCH-01 - recommandation: Codex peut consommer la projection commune du graphique.

<a id="src-omnieurope-blazor-components-omnilink-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniLink.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 13 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Sécurité] `Href` est rendu sans validation de schéma; une valeur non fiable `javascript:` reste exécutable au clic - lignes 5 et 15 - revue best-effort, non outillée - recommandation: Codex peut refuser les schémas dangereux et centraliser une politique URI partagée.

<a id="src-omnieurope-blazor-components-omnilistbox-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniListBox.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 19 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur en français - ligne 52 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omnimain-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniMain.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 11 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnimarkers-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniMarkers.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 8 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Moyen] [Architecture] Les marqueurs bornent silencieusement X et Y à 0-100 sans domaine partagé avec les axes - lignes 2-5 - source: ARCH-01 - recommandation: Codex peut consommer la projection commune du graphique.

<a id="src-omnieurope-blazor-components-omnimonthview-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniMonthView.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 17 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le nom accessible `Vue mensuelle` est codé en dur - ligne 21 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.
- [Moyen] [Correctness] La vue mensuelle rend uniquement les jours 1 à N sans cellules de décalage pour le jour de semaine du premier du mois; les colonnes ne correspondent donc pas au calendrier attendu - lignes 3-13 - recommandation: Codex peut générer une matrice de semaines alignée sur le premier jour configuré par la culture.

<a id="src-omnieurope-blazor-components-omnimultiselect-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniMultiSelect.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 20 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omninavigationtypes-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniNavigationTypes.cs`

- [Moyen] [Architecture] `OmniTabsContext` et `OmniStepsContext` sont publics alors que leurs usages sont limités à la composition en cascade interne - lignes 5-15 - source: ARCH-04 - recommandation: Codex peut les rendre internes avant 1.0.
- [Faible] [Style] Le fichier regroupe deux contextes de navigation sans relation d'héritage ni responsabilité commune autre que leur transport en cascade - lignes 5-15 - source: `coding-standards.md` - recommandation: Codex peut placer chaque type dans son propre fichier.

<a id="src-omnieurope-blazor-components-omninotification-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniNotification.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 21 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] L'aria-label de fermeture est codé en dur en français - ligne 17 - règle STD-I18N - recommandation: Codex peut le fournir par ressource localisable.

<a id="src-omnieurope-blazor-components-omninullablecheckbox-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniNullableCheckBox.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 20 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur en français - ligne 68 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omninullableswitch-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniNullableSwitch.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 21 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] La description de l'état indéterminé et le message de validation sont codés en dur en français - lignes 29 et 70 - règle STD-I18N - recommandation: Codex peut les résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omninumeric-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniNumeric.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 19 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur et incorpore le nom technique du champ - ligne 53 - règle STD-I18N - recommandation: Codex peut utiliser une ressource paramétrée par le nom d'affichage du champ.

<a id="src-omnieurope-blazor-components-omnioverlaytypes-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniOverlayTypes.cs`

- [Élevé] [Style] Le libellé de fermeture par défaut du contrat de dialogue est codé en dur en français - ligne 13 - règle STD-I18N - recommandation: Codex peut le résoudre à la frontière de rendu depuis les ressources.
- [Moyen] [Architecture] `OmniOverlayService` ne conserve qu'un dialogue courant mais accumule les notifications sans borne ni politique d'expiration; le service mélange deux modèles de durée de vie incompatibles - lignes 20-55 - source: ARCH-02 - recommandation: Codex peut séparer les stores internes et définir capacité, remplacement et expiration.
- [Faible] [Style] Le fichier regroupe contrats de dialogue, notification, enum et service mutable au lieu d'un type par fichier - lignes 5-55 - source: `coding-standards.md` - recommandation: Codex peut séparer les contrats et leur service.

<a id="src-omnieurope-blazor-components-omnipager-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniPager.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le libellé de navigation, les aria-labels et le statut de page sont codés en dur en français - lignes 4-6 et 20 - règle STD-I18N - recommandation: Codex peut utiliser des ressources avec paramètres numériques.

<a id="src-omnieurope-blazor-components-omnipanelmenu-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniPanelMenu.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 7 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le nom accessible `Navigation` est codé en dur - ligne 9 - règle STD-I18N - recommandation: Codex peut le fournir par ressource localisable.

<a id="src-omnieurope-blazor-components-omnipanelmenuitem-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniPanelMenuItem.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 27 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Sécurité] `Href` est rendu et transmis à `NavigateTo` sans validation de schéma; une valeur non fiable peut utiliser un schéma actif - lignes 17, 32 et 60-65 - revue best-effort, non outillée - recommandation: Codex peut appliquer la politique URI sûre commune.
- [Moyen] [Correctness] La détection de l'élément actif compare des chaînes d'URI brutes et ne normalise pas correctement chemin, query string et fragment, ce qui produit des états actifs erronés - lignes 45-58 - recommandation: Codex peut comparer des URI normalisées selon la règle de correspondance configurée.

<a id="src-omnieurope-blazor-components-omnipassword-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniPassword.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 30 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Les libellés et textes afficher/masquer sont codés en dur en français - lignes 49-58 - règle STD-I18N - recommandation: Codex peut les fournir par ressources localisables.

<a id="src-omnieurope-blazor-components-omnipieseries-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniPieSeries.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omniprofilemenu-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniProfileMenu.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 8 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le nom accessible `Menu du profil` est codé en dur - ligne 10 - règle STD-I18N - recommandation: Codex peut le fournir par ressource localisable.

<a id="src-omnieurope-blazor-components-omniprofilemenuitem-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniProfileMenuItem.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 12 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Sécurité] Le lien `Href` est rendu sans politique de schémas autorisés - lignes 5 et 14 - revue best-effort, non outillée - recommandation: Codex peut réutiliser la validation URI commune avant rendu.

<a id="src-omnieurope-blazor-components-omniprogressbar-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniProgressBar.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 31 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le libellé accessible `Progression` et le format de pourcentage visible sont codés en dur hors infrastructure de localisation - lignes 42 et 65 - règle STD-I18N - recommandation: Codex peut utiliser des ressources et la culture fournie par l'hôte.

<a id="src-omnieurope-blazor-components-omniradiobuttonlist-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniRadioButtonList.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 20 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur en français - ligne 47 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omniradiobuttonlistitem-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniRadioButtonListItem.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 14 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnirequiredvalidator-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniRequiredValidator.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur en français - ligne 11 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omnirow-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniRow.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnischeduler-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniScheduler.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 41 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Correctness] `Range()` convertit `local.Date` en `DateTimeOffset` par conversion implicite selon le fuseau de la machine, pas selon le paramètre `TimeZone`; les requêtes distantes couvrent donc de mauvaises bornes hors fuseau local et autour des transitions DST - lignes 71-79 et 90-96 - recommandation: Codex peut construire chaque borne avec l'offset du fuseau ciblé puis couvrir les fuseaux non locaux et les transitions.
- [Élevé] [Fiabilité] Le chargement n'est pas associé à une clé de plage et le résultat vide n'établit aucun état durable; changements de date, vue, fuseau ou délégué peuvent ainsi conserver ou relancer un état inadéquat - lignes 81-103 - recommandation: Codex peut suivre une génération de requête comprenant tous les paramètres de plage et distinguer jamais chargé de chargé vide.
- [Élevé] [Style] Les commandes de navigation, noms de vues et états de chargement ou d'erreur sont codés en dur en français - lignes 6-22 et 127-131 - règle STD-I18N - recommandation: Codex peut les fournir par ressources localisables avec la culture du calendrier.
- [Moyen] [Correctness] Le paramètre `Date` et l'action Aujourd'hui reposent sur `DateTimeOffset.Now`, ce qui rend le composant non déterministe et difficile à tester aux changements de jour ou de fuseau - lignes 50 et 124 - recommandation: Codex peut injecter une abstraction d'horloge interne ou accepter explicitement la date courante.

<a id="src-omnieurope-blazor-components-omnischedulertypes-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniSchedulerTypes.cs`

- [INFO] [Sur-ingénierie] `RecurrenceRule` expose dès l'alpha un contrat de syntaxe, validation, fuseau et versionnement sans aucun lecteur observé - ligne 16 - source: AUDIT_ARCHITECTURE.md - alternative plus simple: retirer ce champ avant 1.0 et l'ajouter avec le premier moteur de récurrence; notification consultative non actionnable.
- [Faible] [Style] Le fichier regroupe l'enum de vue et le record de rendez-vous au lieu d'un type par fichier - lignes 3-16 - source: `coding-standards.md` - recommandation: Codex peut séparer ces types lors de la stabilisation du contrat public.

<a id="src-omnieurope-blazor-components-omniselectbar-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniSelectBar.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 20 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur en français - ligne 43 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omniselectbaritem-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniSelectBarItem.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 12 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omniselectiontypes-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniSelectionTypes.cs`

- [Faible] [Style] Le fichier regroupe le contrat d'option générique et la requête d'upload, responsabilités sans cohésion directe - lignes 5-24 - standard un type par fichier - recommandation: Codex peut les séparer par famille.

<a id="src-omnieurope-blazor-components-omniseriesdatalabels-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniSeriesDataLabels.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 8 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnisidebar-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniSidebar.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 12 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le nom accessible `Navigation` est codé en dur - ligne 23 - règle STD-I18N - recommandation: Codex peut le fournir par ressource localisable.

<a id="src-omnieurope-blazor-components-omnisidebartoggle-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniSidebarToggle.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 21 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le nom accessible du contrôle est codé en dur en français - ligne 32 - règle STD-I18N - recommandation: Codex peut le fournir par ressource localisable.

<a id="src-omnieurope-blazor-components-omniskeleton-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniSkeleton.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 18 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnislider-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniSlider.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 26 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur en français - ligne 65 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.
- [Moyen] [Fiabilité] Aucune validation de paramètres n'empêche `Maximum < Minimum`, `Step <= 0` ou une valeur initiale hors bornes; le navigateur et le parseur peuvent alors diverger silencieusement - lignes 28-34 et 54-66 - recommandation: Codex peut valider les invariants lors du cycle de paramètres et borner seulement selon un contrat explicite.

<a id="src-omnieurope-blazor-components-omnisplitbutton-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniSplitButton.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 22 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le libellé accessible `Autres actions` est codé en dur - ligne 29 - règle STD-I18N - recommandation: Codex peut le fournir par ressource localisable.
- [Moyen] [Accessibilité] Le menu s'ouvre sans transférer le focus vers un élément et le gestionnaire clavier ne fournit ni navigation par flèches, ni Home/End, ni restauration de focus - lignes 3-18 et 45-57 - recommandation: Codex peut implémenter le modèle clavier WAI-ARIA menu complet.

<a id="src-omnieurope-blazor-components-omnisplitbuttonitem-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniSplitButtonItem.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 11 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnistack-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniStack.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnistack-razor-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniStack.razor.cs`

- [Faible] [Style] Le fichier nommé comme code-behind ne contient pas la classe partielle du composant mais quatre enums publics - lignes 3-33 - conventions code-behind et un type par fichier - recommandation: Codex peut déplacer chaque enum dans un fichier de contrat nommé explicitement.

<a id="src-omnieurope-blazor-components-omnistackedareaseries-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniStackedAreaSeries.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 3 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Architecture] La variante dite empilée reprend exactement la baseline zéro d'une aire simple et ne reçoit aucun cumul des séries précédentes - lignes 1-8 - source: ARCH-01 - recommandation: Codex peut rendre l'aire depuis les baselines positives et négatives du contexte de graphique.

<a id="src-omnieurope-blazor-components-omnistackedcolumnseries-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniStackedColumnSeries.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 11 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Architecture] La variante dite empilée dessine chaque valeur depuis zéro et n'enregistre aucun cumul entre séries - lignes 1-15 - source: ARCH-01 - recommandation: Codex peut consommer les baselines empilées calculées par le contexte de graphique.

<a id="src-omnieurope-blazor-components-omnisteps-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniSteps.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 9 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le nom accessible `Étapes` est codé en dur - ligne 20 - règle STD-I18N - recommandation: Codex peut le fournir par ressource localisable.

<a id="src-omnieurope-blazor-components-omnistepsitem-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniStepsItem.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 11 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Moyen] [Fiabilité] Le panneau `tabpanel` n'est pas relié à son onglet par `aria-labelledby`, et l'onglet ne gère pas un tabindex roving ni les flèches - lignes 3-8 - recommandation: Codex peut compléter le patron ARIA d'étapes ou employer une sémantique de liste ordonnée si les panneaux ne sont pas des tabs.

<a id="src-omnieurope-blazor-components-omniswitch-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniSwitch.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 20 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le message de validation est codé en dur en français - ligne 46 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-components-omnitabs-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniTabs.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 7 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Moyen] [Fiabilité] Aucun élément ne porte `role="tablist"`; le clavier change la valeur sans déplacer le focus, sans ignorer les clés désactivées et sans empêcher le défilement de page - lignes 3-5 et 23-38 - recommandation: Codex peut implémenter intégralement le patron ARIA tabs avec focus roving.

<a id="src-omnieurope-blazor-components-omnitabsitem-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniTabsItem.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 8 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnitemplateform-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniTemplateForm.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 17 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Moyen] [Fiabilité] L'échec d'import ou d'appel JS lors d'une soumission invalide n'est pas contenu; une simple indisponibilité JS peut transformer une erreur de validation en exception de circuit - lignes 63-70 - recommandation: Codex peut traiter les exceptions d'interop attendues et conserver la validation fonctionnelle sans déplacement de focus.

<a id="src-omnieurope-blazor-components-omnitext-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniText.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 22 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnitextarea-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniTextArea.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 21 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnitextbox-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniTextBox.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 16 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnithemescope-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniThemeScope.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 11 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnitimeline-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniTimeline.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 7 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le nom accessible `Chronologie` est codé en dur - ligne 8 - règle STD-I18N - recommandation: Codex peut le fournir par ressource localisable.

<a id="src-omnieurope-blazor-components-omnitimelineitem-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniTimelineItem.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 11 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnitogglebutton-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniToggleButton.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 12 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omnitooltip-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniTooltip.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 8 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Moyen] [Fiabilité] Le wrapper de déclencheur est focalisable par défaut; lorsqu'il enveloppe déjà un lien ou bouton, il ajoute un arrêt tabulation artificiel et peut produire une structure de focus ambiguë - lignes 3-5 et 13 - recommandation: Codex peut ne rendre le wrapper focalisable que pour du contenu non interactif ou laisser l'hôte fournir explicitement la stratégie.

<a id="src-omnieurope-blazor-components-omnitree-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniTree.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 8 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le nom accessible `Arbre` est codé en dur - ligne 22 - règle STD-I18N - recommandation: Codex peut le fournir par ressource localisable.

<a id="src-omnieurope-blazor-components-omnitreeitem-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniTreeItem.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 35 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Les commandes développer/réduire et les états de chargement sont codés en dur en français - lignes 17, 25 et 29 - règle STD-I18N - recommandation: Codex peut les fournir par ressources localisables.
- [Moyen] [Fiabilité] `_expanded` est copié depuis `Expanded` uniquement à l'initialisation et les exceptions de chargement sont avalées sans observation; les changements contrôlés et diagnostics de l'hôte sont perdus - lignes 52-55 et 72-95 - recommandation: Codex peut synchroniser les paramètres par génération et exposer une callback d'erreur observable.

<a id="src-omnieurope-blazor-components-omnitreelevel-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniTreeLevel.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 5 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.

<a id="src-omnieurope-blazor-components-omniupload-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniUpload.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 40 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Les aria-labels, boutons, messages d'état, tailles et erreurs sont codés en dur en français - lignes 13, 23, 27, 31, 86-114, 133-149 et 173-177 - règle STD-I18N - recommandation: Codex peut utiliser des ressources avec pluralisation et formatage culturel.
- [Élevé] [Sécurité] L'erreur d'upload affiche directement `exception.Message`, ce qui peut divulguer des détails d'implémentation, chemins ou réponses de service à l'utilisateur - lignes 147-150 - revue best-effort, non outillée - recommandation: Codex peut journaliser le détail côté hôte et afficher un message public stable et localisable.
- [Moyen] [Sécurité] Les limites de taille, nombre et type MIME sont vérifiées seulement à partir des métadonnées fournies par le client; elles ne constituent pas une frontière de sécurité pour le contenu reçu - lignes 56-62 et 82-110 - recommandation: Codex peut documenter ce contrat et fournir des hooks de validation serveur par flux et signature de contenu.

<a id="src-omnieurope-blazor-components-omnivalidatorbase-cs"></a>
## `src/OmniEurope.Blazor/Components/OmniValidatorBase.cs`

- [Moyen] [Fiabilité] Le gestionnaire `async void` ne capture que l'annulation; une exception de l'accesseur ou de `Validate` s'échappe vers le contexte de synchronisation et peut interrompre le circuit - lignes 65-87 - recommandation: Codex peut contenir et observer les exceptions attendues tout en laissant les erreurs de programmation remonter par un chemin testable.
- [Moyen] [Correctness] `For`, `CurrentEditContext`, l'accesseur compilé et les abonnements sont figés à l'initialisation; un remplacement de modèle, d'expression ou de contexte laisse le validateur attaché à l'ancien champ - lignes 14-41 et 91-99 - recommandation: Codex peut détecter ces changements, se désabonner puis reconstruire le champ et le magasin de messages.

<a id="src-omnieurope-blazor-components-omnivalueaxis-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniValueAxis.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 11 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Architecture] L'axe affiche ses propres bornes mais ne les communique pas aux séries, lesquelles continuent à projeter sur 0-100; un axe différent décrit donc une géométrie contradictoire - lignes 3-16 - source: ARCH-01 - recommandation: Codex peut enregistrer l'axe dans le contexte de graphique et partager la même projection.

<a id="src-omnieurope-blazor-components-omniweekview-razor"></a>
## `src/OmniEurope.Blazor/Components/OmniWeekView.razor`

- [Moyen] [Style] Le composant conserve sa logique dans un bloc `@code` au lieu du code-behind `.razor.cs` exigé par les standards - ligne 17 - source: `coding-standards.md`, contrôle granulaire GEN004 indisponible - recommandation: Codex peut extraire paramètres et logique dans un code-behind sans modifier l'API ni le rendu.
- [Élevé] [Style] Le nom accessible `Vue hebdomadaire` est codé en dur - ligne 21 - règle STD-I18N - recommandation: Codex peut le résoudre depuis les ressources.

<a id="src-omnieurope-blazor-internal-cspattributeguard-cs"></a>
## `src/OmniEurope.Blazor/Internal/CspAttributeGuard.cs`

- [Élevé] [Sécurité] La garde ne bloque un attribut `on*` que lorsque sa valeur est exactement une `string`; une valeur splattée d'un autre type peut encore être sérialisée comme gestionnaire inline et contourner le contrat CSP - lignes 20-24 - revue best-effort, non outillée - recommandation: Codex peut refuser tout attribut `on*` non reconnu comme callback Blazor sûr, avec tests de valeurs `MarkupString` et objets.

<a id="src-omnieurope-blazor-internal-cssclassbuilder-cs"></a>
## `src/OmniEurope.Blazor/Internal/CssClassBuilder.cs`

RAS

<a id="src-omnieurope-blazor-internal-omnihtmlsanitizer-cs"></a>
## `src/OmniEurope.Blazor/Internal/OmniHtmlSanitizer.cs`

- [Élevé] [Sécurité] La frontière qui alimente ensuite `MarkupString` assainit du HTML par expressions régulières plutôt que par un parseur conforme au tokenizer du navigateur; les balises malformées et mutations DOM ne bénéficient donc d'aucune garantie structurelle contre le XSS - lignes 21-23 et 67-77, usage dans `OmniHtmlEditor.razor:31` - SAST non fiable, revue best-effort non outillée - recommandation: Codex peut employer un parseur HTML maintenu avec allowlist de noeuds/attributs/URI et ajouter un corpus adversarial mXSS.
- [INFO] [Proportionnalité] Les méthodes partielles `GeneratedRegex` sont proportionnées ici: elles évitent la compilation répétée de motifs utilisés sur une frontière chaude et restent confinées à l'implémentation interne - lignes 67-77 - notification consultative, aucune action demandée.

<a id="src-omnieurope-blazor-omnieurope-blazor-csproj"></a>
## `src/OmniEurope.Blazor/OmniEurope.Blazor.csproj`

RAS

<a id="src-omnieurope-blazor-packages-lock-json"></a>
## `src/OmniEurope.Blazor/packages.lock.json`

RAS

<a id="src-omnieurope-blazor-wwwroot-omnieurope-blazor-css"></a>
## `src/OmniEurope.Blazor/wwwroot/omnieurope.blazor.css`

- [Moyen] [Style] Plusieurs cibles interactives restent à 2rem ou moins, notamment fermeture de dialogue/notification et contrôles d'arbre, sous le minimum tactile de 44 par 44 px du standard mobile - lignes 646-647 et 709-711 - recommandation: Codex peut porter la zone interactive minimale à 2.75rem sans agrandir nécessairement le glyphe.
- [Faible] [Qualité] `.omni-visually-hidden` est défini deux fois avec le même objectif, ce qui crée une source de divergence lors des évolutions - lignes 91-101 et 726-736 - recommandation: Codex peut conserver une seule définition utilitaire canonique.

<a id="src-omnieurope-blazor-wwwroot-omniinterop-js"></a>
## `src/OmniEurope.Blazor/wwwroot/omniInterop.js`

- [Faible] [Fiabilité] Le scroll après focus utilise toujours une animation `smooth` et ignore `prefers-reduced-motion`, contrairement aux animations CSS déjà neutralisées - lignes 4-5 - recommandation: Codex peut sélectionner `auto` lorsque la préférence de réduction des mouvements est active.
