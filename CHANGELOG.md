# Journal des modifications

Les changements notables de ce projet seront documentés ici selon le format Keep a Changelog.

## [Non publié]

### Added

- `OmniMultiSelect` en forme `Compact` gagne `Filterable` avec `FilterText` lié dans les deux sens, `OptionTemplate` et `FooterTemplate` : une liste d'étiquettes se cherche, s'affiche avec sa pastille de couleur, et propose de créer celle que la recherche n'a pas trouvée. `Filterable` exige la forme `Compact` et lève sinon, la forme `List` étant un `select multiple` natif qui adresse ses options par position : un filtre y sélectionnerait silencieusement les mauvaises.
- `OmniTabsItem` gagne `TitleContent` et `OnDrop` : un onglet peut afficher un compteur ou un marqueur à la place de son texte, et recevoir un glisser-déposer. Le dépôt est reçu par l'enveloppe de l'onglet, jamais par son bouton, et `OnDrop` est un paramètre parce que le contrat CSP refuse un attribut `on*` transmis par les attributs du composant.
- Nouveau `OmniDateTimePicker` : une date et une heure dans un seul contrôle natif `datetime-local`, lié à `DateTime?` en heure locale, avec bornes `Minimum`/`Maximum` et `ShowSeconds`. Distinct d'`OmniDatePicker` parce que les deux lient des types différents.
- `OmniButton` gagne les variantes `Success` et `Warning` : une action destructrice n'est pas la seule qui mérite une couleur, et confirmer ou suspendre ne se lisent pas comme le même risque.
- `OmniFieldset` gagne `Collapsible` et `Collapsed` : le groupe se replie avec l'élément natif `details`, sans script ni entorse à la CSP stricte.
- `OmniTextBox` gagne `Type` (`Text`, `Email`, `Tel`, `Url` ou `Search`). Le type est ce qui choisit le clavier mobile et le remplissage automatique du navigateur : le champ rendait auparavant `type="text"` quoi que la page demande, un champ e-mail devenant indiscernable d'un champ libre.
- Comportements de coquille paramétrables : `OmniSidebar` gagne `Reveal` (`Push` ou `Overlay`), `Collapse` (`Hidden` ou `Icons`), `Backdrop`, `OpenChanged`, `CloseLabel` et `Header`, un emplacement qui, fourni, fait monter le panneau flottant ouvert jusqu'en haut en portant le coin de l'en-tête ; un panneau flottant se ferme quand l'adresse change. Nouveau `OmniLoadingBar` (`Sweep` ou `Continuous`) piloté par `OmniLoadingState`, sans hauteur propre, qui va au bout puis s'efface à la fin d'un chargement, avec les variables `--omni-loading-bar-color` et `--omni-loading-bar-thickness`. `OmniNotificationOptions` règle la position, la teinte, la croix et le regroupement de la pile. `OmniStack` gagne `Overflow` (`Scroll` ou `Collapse`), `OmniTooltip` gagne `Tracking`, `OmniBadge` gagne `Fill`, `OmniDataGrid` gagne `HeaderWrap`.
- Nouveaux `OmniSettingsSection` et `OmniSettingsTile` : une carte par thème de réglages, chaque réglage dans un encadré avec son icône, son nom, sa description, son contrôle et un emplacement `Details` dessous.
- `OmniTooltip` gagne `Delay` : le temps que le pointeur reste posé avant que l'infobulle paraisse, dessiné par pas de 100 ms entre 0 et 2 s. Sans valeur, la variable `--omni-tooltip-delay` s'applique, 700 ms par défaut, à redéfinir sur `.omni-tooltip` même (posée sur un ancêtre, elle serait masquée) ; le focus clavier montre l'infobulle sans attendre. Figée (`Pinned`), une infobulle suit le pointeur pendant son délai et se pose là où il s'est arrêté.
- `OmniOverlayService.Notify` gagne une surcharge avec `detailsHref` : au-delà de 2 000 caractères, la notification propose « Ouvrir le journal ». La signature existante est conservée telle quelle.

### Changed

- Notifications : le décompte est la teinte pâle du rôle, vidée par la droite sur la vie réelle de la carte et tenue au survol comme au focus, le magasin suspendant l'expiration pendant ce temps ; la barre colorée de gauche disparaît. Un message de plus de 300 caractères élargit la carte, se replie sur quatre lignes derrière « Tout afficher » / « Réduire » et vit quatre secondes plus une par centaine de caractères, trente au plus. Regroupées, les cartes de derrière ne gardent que leur titre. `OmniNotificationMessage` gagne les propriétés `init` `Duration` et `DetailsHref` ; son constructeur et son `Deconstruct` restent ceux de la version publiée.
- Rail d'icônes : les icônes gardent l'alignement du menu ouvert au lieu d'être centrées. Pile repliée : seul le libellé est masqué, l'icône reste, reconnue par `aria-hidden` ou `role="img"`. Pile qui défile : aucune barre de défilement, un chevron de chaque côté qui cache encore des éléments, ôté en butée, comme l'en-tête des onglets ; `OmniStack` enveloppe alors sa rangée, la classe et les attributs de l'hôte restant sur l'enveloppe. En-tête de grille : séparation en dégradé, visible aussi quand l'en-tête collant défile. Badges au demi-rayon. `OmniLink` souligné au survol seulement.
- `OmniButton`, `OmniSplitButton` et `OmniToggleButton` en `Busy` gardent exactement la taille et le contenu du repos : plus de spinner inséré ni d'état `disabled`, un voile de la couleur de surface (`--omni-color-surface`) respire par-dessus en opacité, avec `aria-busy`. Le bouton reste focalisable et ignore les clics ; un `Submit` en cours annule son action par défaut, ce qui empêche un formulaire de repartir sur Entrée. La règle est aussi publiée sous `.btn-busy`, pour qu'un `<button>` de l'application hôte l'obtienne sans CSS propre.
- Infobulles : elles paraissent après 700 ms de pointeur posé au lieu de tout de suite. Barre de chargement : le remplissage suit une seule courbe qui ralentit sans jamais s'arrêter (50 % en 3 s, 77 % en 10 s, 91 % en 30 s) et le balayage, fini, laisse son trait filer jusqu'au bout au lieu de se remplir. Notifications regroupées : la pile est une cascade où chaque carte se pose sous le titre de la précédente et y jette une ombre (`--omni-shadow-stack`, qu'un thème sombre peut foncer), trois titres visibles au plus derrière la carte de devant ; l'icône et la croix se centrent sur le titre ; au survol, la pile s'ouvre dans l'ordre de la cascade, la plus ancienne en tête (en position haute, la carte de devant descend donc sous le pointeur) ; le compteur ferme la zone, erreurs comprises. Un cadre (`OmniCard`, `OmniSettingsTile`) peut devenir plus étroit que son contenu, et `OmniSelectBar` défile au lieu de se couper.
- La feuille `omnieurope.blazor.css` est livrée minifiée : en Release, le build en produit une copie sans commentaires ni espaces superflus (76 604 octets au lieu de 98 136, 12 168 en brotli) qui remplace la source comme ressource statique, dans le paquet NuGet comme dans les hôtes publiés. La source garde ses commentaires, et Debug continue de servir la version lisible. Équivalence vérifiée dans Chromium : mêmes 734 règles et mêmes sélecteurs, les 67 déclarations dont le texte diffère donnent la même valeur calculée. Le lecteur de variables du personnalisateur ne dépend plus des retours à la ligne, sans quoi il n'aurait plus trouvé aucune variable dans la feuille minifiée.
- Budgets CSS : la source est plafonnée à 128 Kio commentaires compris, la feuille livrée à 96 Kio et à 24 Kio en brotli. `eng/Test-Budgets.ps1 -PackagePath` mesure la feuille lue dans le paquet et refuse une copie qui contiendrait encore un commentaire.

## [1.0.0] - 2026-09-06

### Added

- Structure initiale de la Razor Class Library et du paquet NuGet.
- Contrat CSP strict.
- Composants pilotes `OmniButton`, `OmniCard`, `OmniStack` et `OmniAlert`.
- Surface complète de composants, du socle de formulaires au DataGrid, graphiques, scheduler et éditeur HTML.
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
- Couverture des variantes dans la vitrine : chaque valeur de chaque paramètre énuméré des composants publics apparaît désormais dans une démonstration. La grille avancée devient un banc d'essai piloté en direct (lignes, densité, position du paginateur, mode de sélection, mode d'édition, mode de dépliage, position du pied de tableau, mode de filtre), le planificateur gagne un sélecteur de vue, et les démonstrations des boutons, médias, retours, mise en page, coquille, typographie, sélection et grille simple gagnent les variantes qui manquaient.
- Garde `ShowcaseVariantCoverageTests` : une variante publiée mais jamais démontrée fait échouer la compilation. Trois énumérations de filtrage de la grille (`OmniDataGridFilterOperator`, `OmniDataGridLogicalOperator`, `OmniDataGridFilterCaseSensitivity`) sont exclues parce que le visiteur les choisit dans le menu de filtre du composant ; deux tests vérifient que cette exclusion reste justifiée, l'un en exigeant que ces énumérations existent réellement comme paramètres, l'autre en exigeant qu'une démonstration ouvre le mode de filtre avancé.
- Garde `ShowcaseRenderTests.EveryDemo_BindsTheParametersItNames` : un littéral d'énumération C# qui fuit dans le HTML rendu comme valeur d'attribut fait échouer le test, puisqu'il signale un attribut tombé dans le sac des attributs non reconnus au lieu d'être un paramètre du composant.
- Gardes `ShowcaseThemeStateTests` et `ShowcaseCustomizerTests` : rejeu de la persistance du thème, entrée de stockage à moitié écrite, comparaison des tokens déplacés, invalidation de palette, report du mode, puis rendu du personnalisateur, présence de chaque palette et conformité à la politique de sécurité.
- Six chaînes de capacité supplémentaires, françaises et anglaises, pour les démonstrations des boutons, des retours, de la mise en page, de la coquille, des médias et de la grille avancée.

### Changed

- `OmniDataGridColumn.Width` devient une longueur CSS et `OmniDataGridColumnWidthChange.Width` une chaîne ; l'énumération `OmniDataGridColumnWidth` est retirée au profit de largeurs réelles.
- `OmniDataGridLoadRequest` expose `Skip` et `Top`, et `OmniDataGridFilter` porte une seconde condition avec son opérateur logique.
- `OmniPager` gagne première et dernière page, numéros de page, sélecteur de taille de page, libellés par bouton et alignement.
- Remplacement de la référence serveur `Microsoft.AspNetCore.App` par le paquet client-compatible `Microsoft.AspNetCore.Components.Web`.
- Francisation des libellés accessibles par défaut d'`OmniAppearanceToggle`, `OmniProgressBar` et `OmniSidebarToggle`.
- `OmniDataGridColumnFilterType.MultiCombo` est retirée : la forme est désormais `MultiSelect` avec le paramètre de colonne `FilterSearchable`, un axe pour la forme du contrôle et une option pour la recherche.
- Précision des limites de preuve des contrôles CSP et de la baseline API après audit et revue adversariale.
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
- La démonstration de la grille avancée passait `Lines` alors que le paramètre s'appelle `GridLines` : l'attribut tombait silencieusement dans le sac des attributs non reconnus et ne réglait rien. Le réglage est désormais lié à `GridLines` et la nouvelle garde de rendu empêche la même fuite ailleurs.

