# Findings d'audit - OmniEurope.Blazor

Date : 2026-08-11  
Mode : Full  
Périmètre : 282 fichiers du module `src/OmniEurope.Blazor`  
Verdict du module : **11 findings actionnables** - 0 Critique, 2 Élevé, 7 Moyen, 2 Faible. **INFO : 0**.

## Méthode et preuves transversales

Les 282 fichiers du registre ont été lus intégralement, un par un. Le contrôle a couvert qualité, conventions, architecture, sécurité best-effort, performance, fiabilité, couverture et authenticité. Les preuves préalables indiquent un build sans avertissement, 181/181 tests réussis, une couverture lignes de 86,64 % et branches de 67,03 %. CRAP et complexité ne sont pas quantifiables, car aucun outil de complexité exploitable n'était disponible. Semgrep a été omis et gitleaks absent : les conclusions de sécurité restent donc **best-effort, non outillées** sur ces axes.

Aucune référence, dépendance, mention de copyright ou marqueur textuel Radzen n'a été trouvé dans le module. La structure observée reste une implémentation clean-room propre au projet. Cette preuve locale constate l'absence d'indice de copie dans le dépôt audité; elle ne prétend pas comparer avec un corpus externe non fourni.

## Findings actionnables

<a id="oe-blazor-001"></a>
### OE-BLAZOR-001 - [Élevé] [Internationalisation] Le libellé par défaut des onglets est codé en français

**Preuves :** `Components/Navigation/OmniTabs.razor.cs:20` initialise `Label` à `"Onglets"`. Contrairement aux autres libellés par défaut, ce texte ne passe pas par `OmniStrings`; les fichiers resx ne possèdent pas de clé équivalente.

**Impact :** un hôte anglais obtient toujours un nom accessible français pour le groupe d'onglets, ce qui viole le contrat de localisation et la règle STD-I18N.

**Remédiation :** rendre le paramètre vide par défaut, calculer un `EffectiveLabel` depuis une nouvelle ressource fr/en et tester les deux cultures ainsi que le remplacement explicite.

<a id="oe-blazor-002"></a>
### OE-BLAZOR-002 - [Élevé] [Internationalisation] Le message de validation du multi-select est codé en français

**Preuves :** `Components/Selection/OmniMultiSelect.razor.cs:37-41` retourne toujours `"La sélection multiple n'est pas valide."` sans passer par les ressources.

**Impact :** toute validation déclenchée dans une culture non française expose un message incorrect et contourne la stratégie de localisation de la RCL.

**Remédiation :** ajouter une clé fr/en, utiliser `OmniStrings.Get` et couvrir le chemin de validation dans au moins deux cultures.

<a id="oe-blazor-003"></a>
### OE-BLAZOR-003 - [Moyen] [Fiabilité / Contrat] Disabled ne désactive que la zone source de l'éditeur HTML

**Preuves :** `Components/Editor/OmniHtmlEditor.razor:7-18` laisse actifs les boutons natifs et personnalisés; seule la textarea reçoit `disabled="@Disabled"` à la ligne 26. Les handlers `WrapSelectionAsync`, `ApplyAsync`, `UndoAsync` et `RedoAsync` dans le code-behind ne vérifient pas `Disabled`.

**Impact :** un éditeur annoncé désactivé peut encore modifier sa valeur par la barre d'outils, y compris par une transformation personnalisée.

**Remédiation :** propager l'état désactivé à tous les boutons et garder un guard côté handlers; ajouter un test prouvant qu'aucune commande ni callback ne modifie la valeur.

<a id="oe-blazor-004"></a>
### OE-BLAZOR-004 - [Moyen] [Fiabilité / Culture] La plage distante de la vue semaine ne suit pas le premier jour culturel

**Preuves :** `Components/Scheduling/OmniScheduler.razor.cs:47-56` calcule toujours la semaine à partir de dimanche via `DayOfWeek`. `Components/Scheduling/OmniWeekView.razor.cs:12` affiche au contraire une semaine alignée sur `Culture.DateTimeFormat.FirstDayOfWeek`.

**Impact :** en `fr-FR`, le chargement distant demande dimanche à dimanche tandis que l'écran montre lundi à dimanche; il inclut un jour invisible et peut omettre le dimanche visible de fin de plage.

**Remédiation :** partager un calcul culturel unique entre plage de chargement et rendu, puis tester explicitement `fr-FR` et une culture commençant le dimanche.

<a id="oe-blazor-005"></a>
### OE-BLAZOR-005 - [Moyen] [Cycle de vie] OmniComponentsHost ignore un changement de service après l'initialisation

**Preuves :** `Components/Overlays/OmniComponentsHost.razor.cs:15-21` choisit `OverlayService`, calcule l'ownership et s'abonne uniquement dans `OnInitialized`. Aucun `OnParametersSet` ne désabonne l'ancien service ni ne branche le nouveau.

**Impact :** si le paramètre est remplacé, l'hôte continue d'afficher et d'écouter l'ancien service; le nouveau service n'est pas rendu et l'ancien abonnement persiste jusqu'à la destruction.

**Remédiation :** réconcilier le service observé à chaque changement de paramètres, avec désabonnement, ownership exact et tests de remplacement puis disposition.

<a id="oe-blazor-006"></a>
### OE-BLAZOR-006 - [Moyen] [Performance] La projection des graphiques rescane toutes les séries pour chaque point

**Preuves :** `Components/Charts/OmniChartContext.cs:76-91` projette chaque point, puis `ProjectCoordinates` aux lignes 127-129 redemande `XDomain` et `ValueDomain`. Ces propriétés reconstruisent et rescannent toutes les données aux lignes 136-176.

**Impact :** le rendu évolue quadratiquement avec le nombre de points dans les parcours usuels et multiplie allocations et agrégations, ce qui peut bloquer le thread UI sur des séries volumineuses.

**Remédiation :** calculer les domaines une fois par snapshot/version de contexte et les réutiliser pendant toute la projection; ajouter un test de complexité ou un budget mesuré sur une série significative.

<a id="oe-blazor-007"></a>
### OE-BLAZOR-007 - [Moyen] [Maintenabilité] Cinq fichiers déclarent plusieurs types top-level

**Preuves :** `OmniButtonTypes.cs` contient trois enums; `OmniChartContext.cs` un enum et une classe; `GridProjection.cs` un record et une classe; `OmniOverlayCoordinator.cs` un enum, un record et une classe; `OmniOverlayStores.cs` deux classes. Les types imbriqués privés ne sont pas concernés.

**Impact :** l'organisation viole la règle canonique d'un type top-level par fichier et rend ownership, navigation et diffs moins prévisibles.

**Remédiation :** répartir les types top-level dans des fichiers nommés d'après eux, sans modifier namespaces ni surface publique.

<a id="oe-blazor-008"></a>
### OE-BLAZOR-008 - [Moyen] [Accessibilité / Mobile] Des contrôles interactifs restent sous la cible tactile de 44 px

**Preuves :** `wwwroot/omnieurope.blazor.css:58-59` fixe les boutons Small et Medium à 32 et 40 px; le mode compact fixe le contrôle à 32 px aux lignes 21-26. Les select-bars, toggles, split-buttons et pager restent à 40 px aux lignes 579, 612-616 et 699, et les outils HTML à 36 px ligne 790.

**Impact :** les cibles par défaut de plusieurs composants publics sont inférieures au minimum mobile documenté de 44 x 44 px, augmentant les erreurs tactiles.

**Remédiation :** conserver les densités visuelles via padding et typographie, mais garantir une zone interactive minimale de 44 x 44 px, puis mesurer les sélecteurs interactifs dans les smokes responsive.

<a id="oe-blazor-009"></a>
### OE-BLAZOR-009 - [Moyen] [Fiabilité] La suppression dynamique d'une colonne laisse filtres et tris orphelins

**Preuves :** `Components/Data/OmniDataGrid.razor.cs:133-139` retire seulement la définition de colonne. Les dictionnaires `_filters`, `_columnWidths` et la liste `_sorts` ne sont pas nettoyés. Au rechargement, les lignes 239-241 utilisent `_columns.First` pour chaque filtre restant.

**Impact :** après retrait conditionnel d'une colonne filtrée, le prochain rechargement distant peut lever `InvalidOperationException`; un tri orphelin peut aussi être envoyé au backend.

**Remédiation :** purger ou ignorer atomiquement l'état associé à la clé désinscrite et couvrir la disparition d'une colonne filtrée/triée en mode local et distant.

<a id="oe-blazor-010"></a>
### OE-BLAZOR-010 - [Faible] [Couverture] Plusieurs composants publics et chemins d'interaction restent entièrement non exercés

**Preuves :** le Cobertura courant indique 0 % lignes pour les composants publics `OmniBarOptions`, `OmniBarSeries`, `OmniColumnSeries`, `OmniStackedAreaSeries` et `OmniDataGridFilter`. Il indique aussi 0 % pour `OmniSplitButtonItem.HandleClickAsync`, `OmniOverlayCoordinator.CloseTopAsync` et `OmniDialog.FocusBoundaryAsync`.

**Impact :** malgré 181 tests réussis et 86,64 % de couverture lignes globale, des variantes publiques et des chemins clavier/overlay peuvent régresser sans signal.

**Remédiation :** ajouter des tests comportementaux ciblés sur ces composants et chemins, sans viser un pourcentage global artificiel.

<a id="oe-blazor-011"></a>
### OE-BLAZOR-011 - [Faible] [Validation] OmniDatePicker accepte des bornes contradictoires

**Preuves :** `Components/Forms/OmniDatePicker.razor.cs:5-9` expose `Minimum` et `Maximum`, puis les lignes 35-37 rejettent toute date hors bornes, mais aucun contrôle ne refuse `Minimum > Maximum`.

**Impact :** une configuration invalide rend toute valeur non vide impossible et produit un contrôle natif aux attributs contradictoires, sans diagnostic de configuration.

**Remédiation :** valider l'ordre des bornes dans `OnParametersSet` et ajouter un test de rejet explicite, comme pour les autres composants numériques.

## Proportionnalité et sur-ingénierie

`PROPORTIONALITY: NONE` - Les composants, contextes internes, stores et interop JavaScript restent globalement proportionnés à une RCL UI clean-room. Les remédiations proposées sont locales et ne nécessitent ni nouvelle couche, ni nouvelle dépendance, ni copie ou retour vers Radzen.

## Contrôles fichier par fichier

<a id="src-omnieurope-blazor-imports-razor"></a>
### `src/OmniEurope.Blazor/_Imports.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-imports-razor"></a>
### `src/OmniEurope.Blazor/Components/_Imports.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-actions-omniappearancetoggle-razor"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniAppearanceToggle.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-actions-omniappearancetoggle-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniAppearanceToggle.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-actions-omnibutton-razor"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniButton.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-actions-omnibutton-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniButton.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-actions-omnibuttontypes-cs"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniButtonTypes.cs`

Finding(s) : [OE-BLAZOR-007](#oe-blazor-007).

<a id="src-omnieurope-blazor-components-actions-omniradiobuttonlist-razor"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniRadioButtonList.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-actions-omniradiobuttonlist-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniRadioButtonList.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-actions-omniradiobuttonlistitem-razor"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniRadioButtonListItem.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-actions-omniradiobuttonlistitem-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniRadioButtonListItem.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-actions-omnisplitbutton-razor"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniSplitButton.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-actions-omnisplitbutton-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniSplitButton.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-actions-omnisplitbuttonitem-razor"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniSplitButtonItem.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-actions-omnisplitbuttonitem-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniSplitButtonItem.razor.cs`

Finding(s) : [OE-BLAZOR-010](#oe-blazor-010).

<a id="src-omnieurope-blazor-components-actions-omnitogglebutton-razor"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniToggleButton.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-actions-omnitogglebutton-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Actions/OmniToggleButton.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omniarcgauge-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniArcGauge.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omniarcgauge-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniArcGauge.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omniarcgaugescale-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniArcGaugeScale.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omniarcgaugescale-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniArcGaugeScale.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omniarcgaugescalevalue-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniArcGaugeScaleValue.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omniarcgaugescalevalue-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniArcGaugeScaleValue.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omniareaseries-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniAreaSeries.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omniareaseries-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniAreaSeries.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omniaxistitle-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniAxisTitle.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omniaxistitle-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniAxisTitle.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnibaroptions-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniBarOptions.razor`

Finding(s) : [OE-BLAZOR-010](#oe-blazor-010).

<a id="src-omnieurope-blazor-components-charts-omnibaroptions-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniBarOptions.razor.cs`

Finding(s) : [OE-BLAZOR-010](#oe-blazor-010).

<a id="src-omnieurope-blazor-components-charts-omnibarseries-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniBarSeries.razor`

Finding(s) : [OE-BLAZOR-010](#oe-blazor-010).

<a id="src-omnieurope-blazor-components-charts-omnibarseries-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniBarSeries.razor.cs`

Finding(s) : [OE-BLAZOR-010](#oe-blazor-010).

<a id="src-omnieurope-blazor-components-charts-omnicategoryaxis-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniCategoryAxis.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnicategoryaxis-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniCategoryAxis.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnichart-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniChart.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnichart-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniChart.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnichartcontext-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniChartContext.cs`

Finding(s) : [OE-BLAZOR-006](#oe-blazor-006), [OE-BLAZOR-007](#oe-blazor-007).

<a id="src-omnieurope-blazor-components-charts-omnichartgeometry-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniChartGeometry.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnichartpoint-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniChartPoint.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnichartslice-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniChartSlice.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnicharttooltipoptions-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniChartTooltipOptions.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnicharttooltipoptions-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniChartTooltipOptions.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnicolumnseries-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniColumnSeries.razor`

Finding(s) : [OE-BLAZOR-010](#oe-blazor-010).

<a id="src-omnieurope-blazor-components-charts-omnicolumnseries-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniColumnSeries.razor.cs`

Finding(s) : [OE-BLAZOR-010](#oe-blazor-010).

<a id="src-omnieurope-blazor-components-charts-omnidonutseries-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniDonutSeries.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnidonutseries-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniDonutSeries.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnigridlines-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniGridLines.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnigridlines-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniGridLines.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnilegend-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniLegend.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnilegend-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniLegend.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnilineseries-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniLineSeries.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnilineseries-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniLineSeries.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnimarkers-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniMarkers.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnimarkers-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniMarkers.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnipieseries-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniPieSeries.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnipieseries-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniPieSeries.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omniseriesdatalabels-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniSeriesDataLabels.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omniseriesdatalabels-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniSeriesDataLabels.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnistackedareaseries-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniStackedAreaSeries.razor`

Finding(s) : [OE-BLAZOR-010](#oe-blazor-010).

<a id="src-omnieurope-blazor-components-charts-omnistackedareaseries-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniStackedAreaSeries.razor.cs`

Finding(s) : [OE-BLAZOR-010](#oe-blazor-010).

<a id="src-omnieurope-blazor-components-charts-omnistackedcolumnseries-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniStackedColumnSeries.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnistackedcolumnseries-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniStackedColumnSeries.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnivalueaxis-razor"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniValueAxis.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-charts-omnivalueaxis-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Charts/OmniValueAxis.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnicollectiontypes-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniCollectionTypes.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatagrid-razor"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGrid.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatagrid-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGrid.razor.cs`

Finding(s) : [OE-BLAZOR-009](#oe-blazor-009).

<a id="src-omnieurope-blazor-components-data-omnidatagridcolumn-razor"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGridColumn.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatagridcolumn-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGridColumn.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatagridcolumndefinition-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGridColumnDefinition.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatagridcolumnwidth-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGridColumnWidth.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatagridcolumnwidthchange-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGridColumnWidthChange.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatagridcontext-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGridContext.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatagridfilter-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGridFilter.cs`

Finding(s) : [OE-BLAZOR-010](#oe-blazor-010).

<a id="src-omnieurope-blazor-components-data-omnidatagridfilteroperator-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGridFilterOperator.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatagridloadrequest-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGridLoadRequest.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatagridresult-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGridResult.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatagridselectionmode-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGridSelectionMode.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatagridsort-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataGridSort.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatalist-razor"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataList.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnidatalist-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniDataList.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnipager-razor"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniPager.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnipager-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniPager.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnitree-razor"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniTree.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnitree-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniTree.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnitreeitem-razor"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniTreeItem.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnitreeitem-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniTreeItem.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnitreelevel-razor"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniTreeLevel.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-data-omnitreelevel-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Data/OmniTreeLevel.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-editor-omnihtmleditor-razor"></a>
### `src/OmniEurope.Blazor/Components/Editor/OmniHtmlEditor.razor`

Finding(s) : [OE-BLAZOR-003](#oe-blazor-003).

<a id="src-omnieurope-blazor-components-editor-omnihtmleditor-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Editor/OmniHtmlEditor.razor.cs`

Finding(s) : [OE-BLAZOR-003](#oe-blazor-003).

<a id="src-omnieurope-blazor-components-editor-omnihtmleditortypes-cs"></a>
### `src/OmniEurope.Blazor/Components/Editor/OmniHtmlEditorTypes.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omnialert-razor"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniAlert.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omnialert-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniAlert.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omnialerttypes-cs"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniAlertTypes.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omnibadge-razor"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniBadge.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omnibadge-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniBadge.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omniicon-razor"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniIcon.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omniicon-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniIcon.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omniimage-razor"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniImage.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omniimage-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniImage.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omniprogressbar-razor"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniProgressBar.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omniprogressbar-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniProgressBar.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omniskeleton-razor"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniSkeleton.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omniskeleton-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniSkeleton.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omnitext-razor"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniText.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-feedback-omnitext-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Feedback/OmniText.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnicheckbox-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniCheckBox.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnicheckbox-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniCheckBox.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnicolorpicker-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniColorPicker.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnicolorpicker-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniColorPicker.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnicomparevalidator-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniCompareValidator.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnicomparevalidator-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniCompareValidator.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnidatepicker-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniDatePicker.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnidatepicker-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniDatePicker.razor.cs`

Finding(s) : [OE-BLAZOR-011](#oe-blazor-011).

<a id="src-omnieurope-blazor-components-forms-omniemailvalidator-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniEmailValidator.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omniemailvalidator-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniEmailValidator.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omniformfield-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniFormField.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omniformfield-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniFormField.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omniinputbase-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniInputBase.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnilabel-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniLabel.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnilabel-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniLabel.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnilengthvalidator-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniLengthValidator.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnilengthvalidator-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniLengthValidator.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omninullablecheckbox-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniNullableCheckBox.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omninullablecheckbox-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniNullableCheckBox.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omninullableswitch-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniNullableSwitch.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omninullableswitch-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniNullableSwitch.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omninumeric-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniNumeric.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omninumeric-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniNumeric.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnipassword-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniPassword.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnipassword-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniPassword.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnirequiredvalidator-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniRequiredValidator.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnirequiredvalidator-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniRequiredValidator.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omniswitch-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniSwitch.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omniswitch-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniSwitch.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnitemplateform-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniTemplateForm.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnitemplateform-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniTemplateForm.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnitextarea-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniTextArea.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnitextarea-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniTextArea.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnitextbox-razor"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniTextBox.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnitextbox-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniTextBox.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-forms-omnivalidatorbase-cs"></a>
### `src/OmniEurope.Blazor/Components/Forms/OmniValidatorBase.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omniappearance-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniAppearance.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omnibadgevariant-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniBadgeVariant.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omnicomponentbase-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniComponentBase.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omnidensity-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniDensity.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omniheadinglevel-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniHeadingLevel.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omniiconname-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniIconName.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omniimagefit-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniImageFit.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omniimageloading-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniImageLoading.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omnilayoutwidth-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniLayoutWidth.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omniprogressshape-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniProgressShape.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omniprogressvariant-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniProgressVariant.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omnisidebarposition-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniSidebarPosition.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omniskeletonshape-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniSkeletonShape.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omnitextelement-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniTextElement.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-foundation-omnitexttone-cs"></a>
### `src/OmniEurope.Blazor/Components/Foundation/OmniTextTone.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnialignment-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniAlignment.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnibody-razor"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniBody.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnibody-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniBody.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnicard-razor"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniCard.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnicard-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniCard.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnicolumn-razor"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniColumn.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnicolumn-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniColumn.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnifieldset-razor"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniFieldset.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnifieldset-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniFieldset.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnigrid-razor"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniGrid.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnigrid-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniGrid.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omniheader-razor"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniHeader.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omniheader-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniHeader.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omniheading-razor"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniHeading.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omniheading-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniHeading.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnijustification-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniJustification.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnilayout-razor"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniLayout.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnilayout-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniLayout.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnimain-razor"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniMain.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnimain-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniMain.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnirow-razor"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniRow.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnirow-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniRow.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnispacing-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniSpacing.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnistack-razor"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniStack.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnistack-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniStack.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnistackorientation-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniStackOrientation.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnithemescope-razor"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniThemeScope.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-layout-omnithemescope-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Layout/OmniThemeScope.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnibreadcrumb-razor"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniBreadcrumb.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnibreadcrumb-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniBreadcrumb.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnibreadcrumbitem-razor"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniBreadcrumbItem.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnibreadcrumbitem-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniBreadcrumbItem.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnilink-razor"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniLink.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnilink-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniLink.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnipanelmenu-razor"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniPanelMenu.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnipanelmenu-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniPanelMenu.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnipanelmenuitem-razor"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniPanelMenuItem.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnipanelmenuitem-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniPanelMenuItem.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omniprofilemenu-razor"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniProfileMenu.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omniprofilemenu-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniProfileMenu.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omniprofilemenuitem-razor"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniProfileMenuItem.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omniprofilemenuitem-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniProfileMenuItem.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnisidebar-razor"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniSidebar.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnisidebar-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniSidebar.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnisidebartoggle-razor"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniSidebarToggle.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnisidebartoggle-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniSidebarToggle.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnisteps-razor"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniSteps.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnisteps-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniSteps.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnistepscontext-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniStepsContext.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnistepsitem-razor"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniStepsItem.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnistepsitem-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniStepsItem.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnitabs-razor"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniTabs.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnitabs-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniTabs.razor.cs`

Finding(s) : [OE-BLAZOR-001](#oe-blazor-001).

<a id="src-omnieurope-blazor-components-navigation-omnitabscontext-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniTabsContext.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnitabsitem-razor"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniTabsItem.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-navigation-omnitabsitem-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Navigation/OmniTabsItem.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-overlays-omnicomponentshost-razor"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniComponentsHost.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-overlays-omnicomponentshost-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniComponentsHost.razor.cs`

Finding(s) : [OE-BLAZOR-005](#oe-blazor-005).

<a id="src-omnieurope-blazor-components-overlays-omnicontextmenu-razor"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniContextMenu.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-overlays-omnicontextmenu-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniContextMenu.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-overlays-omnidialog-razor"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniDialog.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-overlays-omnidialog-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniDialog.razor.cs`

Finding(s) : [OE-BLAZOR-010](#oe-blazor-010).

<a id="src-omnieurope-blazor-components-overlays-omnidialogrequest-cs"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniDialogRequest.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-overlays-omninotification-razor"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniNotification.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-overlays-omninotification-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniNotification.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-overlays-omninotificationmessage-cs"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniNotificationMessage.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-overlays-omninotificationseverity-cs"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniNotificationSeverity.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-overlays-omnioverlayservice-cs"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniOverlayService.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-overlays-omnitooltip-razor"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniTooltip.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-overlays-omnitooltip-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Overlays/OmniTooltip.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-scheduling-omnidayview-razor"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniDayView.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-scheduling-omnidayview-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniDayView.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-scheduling-omnimonthview-razor"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniMonthView.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-scheduling-omnimonthview-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniMonthView.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-scheduling-omnischeduler-razor"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniScheduler.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-scheduling-omnischeduler-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniScheduler.razor.cs`

Finding(s) : [OE-BLAZOR-004](#oe-blazor-004).

<a id="src-omnieurope-blazor-components-scheduling-omnischedulerappointment-cs"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniSchedulerAppointment.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-scheduling-omnischedulerview-cs"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniSchedulerView.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-scheduling-omnitimeline-razor"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniTimeline.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-scheduling-omnitimeline-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniTimeline.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-scheduling-omnitimelineitem-razor"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniTimelineItem.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-scheduling-omnitimelineitem-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniTimelineItem.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-scheduling-omniweekview-razor"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniWeekView.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-scheduling-omniweekview-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Scheduling/OmniWeekView.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omniautocomplete-razor"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniAutocomplete.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omniautocomplete-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniAutocomplete.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omnicheckboxlist-razor"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniCheckBoxList.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omnicheckboxlist-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniCheckBoxList.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omnidropdown-razor"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniDropDown.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omnidropdown-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniDropDown.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omnilistbox-razor"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniListBox.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omnilistbox-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniListBox.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omnimultiselect-razor"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniMultiSelect.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omnimultiselect-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniMultiSelect.razor.cs`

Finding(s) : [OE-BLAZOR-002](#oe-blazor-002).

<a id="src-omnieurope-blazor-components-selection-omnioption-cs"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniOption.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omniselectbar-razor"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniSelectBar.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omniselectbar-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniSelectBar.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omniselectbaritem-razor"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniSelectBarItem.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omniselectbaritem-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniSelectBarItem.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omnislider-razor"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniSlider.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omnislider-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniSlider.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omniupload-razor"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniUpload.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omniupload-razor-cs"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniUpload.razor.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-components-selection-omniuploadrequest-cs"></a>
### `src/OmniEurope.Blazor/Components/Selection/OmniUploadRequest.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-globalusings-cs"></a>
### `src/OmniEurope.Blazor/GlobalUsings.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-internal-cspattributeguard-cs"></a>
### `src/OmniEurope.Blazor/Internal/CspAttributeGuard.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-internal-cssclassbuilder-cs"></a>
### `src/OmniEurope.Blazor/Internal/CssClassBuilder.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-internal-gridprojection-cs"></a>
### `src/OmniEurope.Blazor/Internal/GridProjection.cs`

Finding(s) : [OE-BLAZOR-007](#oe-blazor-007).

<a id="src-omnieurope-blazor-internal-gridremotestate-cs"></a>
### `src/OmniEurope.Blazor/Internal/GridRemoteState.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-internal-omnihtmlsanitizer-cs"></a>
### `src/OmniEurope.Blazor/Internal/OmniHtmlSanitizer.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-internal-omnioverlaycoordinator-cs"></a>
### `src/OmniEurope.Blazor/Internal/OmniOverlayCoordinator.cs`

Finding(s) : [OE-BLAZOR-007](#oe-blazor-007), [OE-BLAZOR-010](#oe-blazor-010).

<a id="src-omnieurope-blazor-internal-omnioverlayhosts-cs"></a>
### `src/OmniEurope.Blazor/Internal/OmniOverlayHosts.cs`

RAS additionnel dans ce passage. Référence inter-passes : [ARCH-R-02](AUDIT_ARCHITECTURE.md#arch-r-02---le-renderer-de-superpositions-inverse-la-frontiere-components---internal).

<a id="src-omnieurope-blazor-internal-omnioverlaystores-cs"></a>
### `src/OmniEurope.Blazor/Internal/OmniOverlayStores.cs`

Finding(s) : [OE-BLAZOR-007](#oe-blazor-007).

<a id="src-omnieurope-blazor-internal-omnistrings-cs"></a>
### `src/OmniEurope.Blazor/Internal/OmniStrings.cs`

RAS additionnel dans ce passage. Référence inter-passes : [ARCH-R-01](AUDIT_ARCHITECTURE.md#arch-r-01---la-localisation-de-rendu-contourne-labstraction-imposee).

<a id="src-omnieurope-blazor-internal-omniuripolicy-cs"></a>
### `src/OmniEurope.Blazor/Internal/OmniUriPolicy.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-omnieurope-blazor-csproj"></a>
### `src/OmniEurope.Blazor/OmniEurope.Blazor.csproj`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-omnieuropeblazorservicecollectionextensions-cs"></a>
### `src/OmniEurope.Blazor/OmniEuropeBlazorServiceCollectionExtensions.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-packages-lock-json"></a>
### `src/OmniEurope.Blazor/packages.lock.json`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-properties-assemblyinfo-cs"></a>
### `src/OmniEurope.Blazor/Properties/AssemblyInfo.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-resources-appstrings-cs"></a>
### `src/OmniEurope.Blazor/Resources/AppStrings.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-resources-appstrings-en-resx"></a>
### `src/OmniEurope.Blazor/Resources/AppStrings.en.resx`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-resources-appstrings-resx"></a>
### `src/OmniEurope.Blazor/Resources/AppStrings.resx`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-wwwroot-omni-focus-js"></a>
### `src/OmniEurope.Blazor/wwwroot/omni-focus.js`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="src-omnieurope-blazor-wwwroot-omnieurope-blazor-css"></a>
### `src/OmniEurope.Blazor/wwwroot/omnieurope.blazor.css`

Finding(s) : [OE-BLAZOR-008](#oe-blazor-008).

<a id="src-omnieurope-blazor-wwwroot-omniinterop-js"></a>
### `src/OmniEurope.Blazor/wwwroot/omniInterop.js`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

## Totaux

- Critique : 0
- Élevé : 2
- Moyen : 7
- Faible : 2
- INFO, consultatif et exclu du verdict : 0
- Fichiers audités : 282/282

