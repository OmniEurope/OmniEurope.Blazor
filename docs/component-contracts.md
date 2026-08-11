# Contrats observés des composants Radzen

Ce rapport extrait par expressions régulières des paramètres candidats et les emplacements de templates présents dans les fichiers Razor, sans lire le code source de Radzen. Le parseur actuel n'interprète pas Razor : des identifiants C# placés dans une expression d'attribut peuvent être comptés à tort comme des paramètres. Ce document guide l'inventaire, mais ne constitue pas encore un contrat fiable paramètre par paramètre.

## RadzenAlert

`AlertStyle` (485), `AllowClose` (247), `Close` (2), `FailureCount` (1), `Icon` (9), `Shade` (106), `ShowIcon` (83), `Size` (11), `Style` (2), `Title` (8), `Variant` (402), `Visible` (2)

## RadzenAppearanceToggle

Aucun paramètre nommé observé.

## RadzenArcGauge

Aucun paramètre nommé observé.

## RadzenArcGaugeScale

`EndAngle` (4), `Max` (4), `Min` (4), `StartAngle` (4), `Step` (4), `TickPosition` (4)

## RadzenArcGaugeScaleValue

`Fill` (8), `ShowValue` (8), `Value` (8)

## RadzenAreaSeries

`CategoryProperty` (5), `Data` (5), `Fill` (1), `LineType` (2), `Smooth` (4), `Stroke` (1), `Title` (5), `ValueProperty` (5)

## RadzenAutoComplete

`Data` (1), `FilterCaseSensitivity` (1), `FilterDelay` (1), `FilterOperator` (1), `LoadData` (1), `MinLength` (1), `OpenOnFocus` (1), `Placeholder` (1), `TextProperty` (1)

## RadzenAxisTitle

`Text` (13)

## RadzenBadge

`Action` (2), `ArticleCount` (1), `BadgeStyle` (974), `Committee` (2), `Decision` (1), `DeploymentTargetAvailable` (4), `Icon` (29), `IsPill` (70), `LintPassed` (4), `ModelType` (1), `PipelineRunnerAvailable` (8), `PipelineRunnerEnabled` (4), `Port` (2), `Provenance` (4), `Role` (4), `Scope` (2), `Shade` (11), `Source` (1), `Status` (19), `Style` (7), `Text` (976), `Type` (2), `Variant` (5), `Verdict` (2)

## RadzenBarOptions

`Radius` (1)

## RadzenBarSeries

`CategoryProperty` (4), `Data` (4), `Fill` (1), `Title` (3), `ValueProperty` (4)

## RadzenBody

Aucun paramètre nommé observé.

## RadzenBreadCrumb

Aucun paramètre nommé observé.

## RadzenBreadCrumbItem

`Icon` (3), `Path` (19), `Text` (35)

## RadzenButton

`Busy` (18), `BusyText` (42), `ButtonStyle` (1941), `ButtonType` (290), `Click` (2397), `Count` (10), `Disabled` (340), `HasIngestKey` (2), `Icon` (2477), `IconPosition` (2), `IsBusy` (384), `MatchCount` (2), `Mode` (1), `MouseEnter` (22), `PipelineRunnerEnabled` (6), `ResultCount` (2), `SelectedCount` (2), `Size` (1254), `Status` (1), `Style` (37), `Text` (1814), `Title` (38), `Variant` (1211), `Visible` (6)

## RadzenCard

`Id` (2), `Style` (39), `Variant` (15)

## RadzenCategoryAxis

`FormatString` (2), `Formatter` (2), `LabelAutoRotation` (4), `Padding` (2)

## RadzenChart

`Style` (10), `Visible` (2)

## RadzenChartTooltipOptions

`Shared` (4)

## RadzenCheckBox

`Change` (36), `Count` (1), `Disabled` (3), `Name` (29), `ReadOnly` (4), `TabIndex` (2), `TriState` (3), `TValue` (64), `Value` (36), `ValueChanged` (2)

## RadzenCheckBoxList

`Data` (1), `Orientation` (1), `TValue` (1)

## RadzenColorPicker

`Change` (1), `ShowButton` (1), `ShowColors` (1), `ShowHSV` (1), `ShowRGBA` (1), `Value` (1)

## RadzenColumn

`Size` (805), `SizeLG` (89), `SizeMD` (658), `SizeSM` (66), `SizeXL` (6), `SizeXS` (96)

## RadzenColumnSeries

`CategoryProperty` (6), `Data` (6), `Fill` (1), `Title` (6), `ValueProperty` (6)

## RadzenCompareValidator

`Component` (1), `Text` (1), `Value` (1)

## RadzenComponents

Aucun paramètre nommé observé.

## RadzenContextMenu

Aucun paramètre nommé observé.

## RadzenDataGrid

`AllowAltering` (2), `AllowAlternatingRows` (5), `AllowColumnResize` (20), `AllowFiltering` (200), `AllowGrouping` (11), `AllowPaging` (149), `AllowRowSelectOnRowClick` (6), `AllowSorting` (252), `AllowVirtualization` (7), `ColumnResized` (1), `ColumnWidth` (3), `Count` (49), `Data` (297), `Density` (206), `EditMode` (14), `EmptyText` (91), `ExpandChildItemAriaLabel` (3), `ExpandMode` (6), `FilterCaseSensitivity` (4), `FilterMode` (183), `FilterPopupRenderMode` (10), `FirstPageAriaLabel` (24), `FirstPageTitle` (24), `GridLines` (6), `Groups` (8), `IsLoading` (75), `KeyProperty` (3), `LastPageAriaLabel` (24), `LastPageTitle` (24), `LoadData` (48), `NextPageAriaLabel` (24), `NextPageTitle` (24), `PageAriaLabelFormat` (24), `PagerHorizontalAlign` (24), `PagerPosition` (2), `PageSize` (132), `PageSizeOptions` (10), `PageSizeText` (24), `PageTitleFormat` (24), `PagingSummaryFormat` (24), `PrevPageAriaLabel` (24), `PrevPageTitle` (24), `Responsive` (87), `RowClick` (55), `RowCollapse` (1), `RowDoubleClick` (2), `RowExpand` (1), `RowRender` (12), `RowSelect` (4), `RowUpdate` (8), `SelectionMode` (1), `ShowExpandAll` (1), `ShowExpandColumn` (1), `ShowGroupPanel` (10), `ShowPagingSummary` (2), `Style` (19), `TItem` (293), `VirtualizationOverscanCount` (4)

## RadzenDataGridColumn

`Context` (4), `CssClass` (28), `Filterable` (164), `FormatString` (195), `Frozen` (3), `HeaderCssClass` (28), `MinWidth` (110), `Property` (1458), `Resizable` (11), `Sortable` (269), `SortOrder` (17), `SortProperty` (7), `TextAlign` (241), `TItem` (1469), `Title` (1884), `Visible` (18), `Width` (1329)

## RadzenDataList

`AllowPaging` (43), `Count` (2), `Data` (70), `LoadData` (2), `PageSize` (4), `Style` (1), `TItem` (70), `WrapItems` (51)

## RadzenDatePicker

`AllowClear` (6), `Change` (17), `DateFormat` (31), `Name` (13), `Placeholder` (9), `ShowCalendarWeek` (3), `ShowTime` (22), `Style` (11), `TimeOnly` (2), `TValue` (32), `Value` (7), `ValueChanged` (6)

## RadzenDayView

`Text` (1)

## RadzenDialog

Aucun paramètre nommé observé.

## RadzenDonutSeries

`CategoryProperty` (1), `Data` (1), `Title` (1), `ValueProperty` (1)

## RadzenDropDown

`AllowClear` (136), `AllowFiltering` (54), `AllowVirtualization` (8), `Change` (177), `Chips` (1), `ClearSearchAfterSelection` (12), `Count` (2), `Data` (443), `Disabled` (14), `FilterCaseSensitivity` (35), `FilterDelay` (15), `FilterOperator` (15), `FilterPlaceholder` (14), `ItemRender` (4), `LoadData` (9), `Multiple` (1), `Name` (35), `Placeholder` (172), `SearchText` (1), `Size` (4), `Style` (78), `TextProperty` (324), `TValue` (313), `Value` (60), `ValueChanged` (12), `ValueProperty` (323)

## RadzenEmailValidator

`Component` (5), `Text` (5)

## RadzenFieldset

`AllowCollapse` (14), `CollapseChanged` (2), `Collapsed` (12), `Text` (45)

## RadzenFormField

`AllowFloatingLabel` (14), `Component` (4), `Id` (2), `Style` (241), `Text` (1162), `Variant` (393)

## RadzenGridLines

`Visible` (15)

## RadzenHeader

Aucun paramètre nommé observé.

## RadzenHtmlEditor

`Execute` (1), `Style` (4), `Value` (1), `ValueChanged` (1)

## RadzenHtmlEditorBold

Aucun paramètre nommé observé.

## RadzenHtmlEditorCustomTool

`CommandName` (28), `Icon` (28), `Title` (28)

## RadzenHtmlEditorIndent

Aucun paramètre nommé observé.

## RadzenHtmlEditorItalic

Aucun paramètre nommé observé.

## RadzenHtmlEditorOutdent

Aucun paramètre nommé observé.

## RadzenHtmlEditorRedo

Aucun paramètre nommé observé.

## RadzenHtmlEditorSeparator

Aucun paramètre nommé observé.

## RadzenHtmlEditorSubscript

Aucun paramètre nommé observé.

## RadzenHtmlEditorSuperscript

Aucun paramètre nommé observé.

## RadzenHtmlEditorUndo

Aucun paramètre nommé observé.

## RadzenIcon

`ButtonStyle` (2), `DeploymentTargetAvailable` (2), `Icon` (1057), `IconStyle` (3), `IsCorrect` (2), `MouseEnter` (1), `PipelineRunnerAvailable` (4), `PipelineRunnerEnabled` (4), `Status` (1), `Style` (81), `Type` (9), `Variant` (2)

## RadzenImage

`Path` (1)

## RadzenLabel

`Component` (85), `Style` (1), `Text` (184)

## RadzenLayout

Aucun paramètre nommé observé.

## RadzenLegend

`Position` (21), `Visible` (6)

## RadzenLengthValidator

`Component` (3), `Min` (3), `Text` (3)

## RadzenLineSeries

`CategoryProperty` (35), `Data` (35), `LineType` (5), `Smooth` (32), `Stroke` (2), `StrokeWidth` (2), `Title` (35), `ValueProperty` (35)

## RadzenLink

`Click` (1), `Icon` (3), `Path` (104), `Style` (2), `Target` (8), `Text` (79)

## RadzenListBox

`Change` (1), `Data` (3), `Style` (1), `TextProperty` (2), `TValue` (2), `ValueProperty` (2)

## RadzenMarkers

`Fill` (1), `MarkerType` (14), `Size` (2), `Stroke` (1), `StrokeWidth` (1), `Visible` (3)

## RadzenMonthView

`Text` (1)

## RadzenNotification

Aucun paramètre nommé observé.

## RadzenNumeric

`AriaLabel` (2), `Change` (20), `Disabled` (1), `Format` (13), `Immediate` (15), `InputAttributes` (2), `Max` (129), `Min` (148), `Name` (26), `Placeholder` (5), `ShowUpDown` (10), `Step` (17), `Style` (45), `TextAlign` (2), `TValue` (128), `Value` (15), `ValueChanged` (2)

## RadzenPager

`Count` (4), `CurrentPage` (1), `PageChanged` (4), `PageSize` (4)

## RadzenPanelMenu

`Click` (1), `DisplayStyle` (19), `ShowArrow` (17)

## RadzenPanelMenuItem

`Click` (6), `Expanded` (8), `ExpandedChanged` (2), `Icon` (350), `Path` (329), `Selected` (6), `Style` (10), `Text` (351)

## RadzenPassword

`Change` (1), `Disabled` (1), `Immediate` (2), `Name` (39), `Placeholder` (2), `Style` (11), `Value` (2)

## RadzenPieSeries

`CategoryProperty` (7), `Data` (7), `Title` (7), `ValueProperty` (7)

## RadzenProfileMenu

`Click` (1)

## RadzenProfileMenuItem

`Icon` (4), `Path` (3), `Text` (4), `Value` (1)

## RadzenProgressBar

`CompletionPercent` (1), `Max` (40), `Mode` (23), `ProgressBarStyle` (10), `ShowValue` (65), `Style` (9), `Value` (60), `Visible` (3)

## RadzenProgressBarCircular

`CompletionPercent` (1), `Max` (2), `Mode` (297), `ProgressBarStyle` (2), `ShowValue` (270), `Size` (65), `Value` (6)

## RadzenRadioButtonList

`Change` (1), `Orientation` (1), `TValue` (2), `Value` (1)

## RadzenRadioButtonListItem

`Text` (4), `TValue` (1), `Value` (4)

## RadzenRequiredValidator

`Component` (64), `Popup` (6), `Text` (64)

## RadzenRow

`AlignItems` (97), `Gap` (267), `JustifyContent` (32), `RowGap` (4), `Style` (1)

## RadzenScheduler

`AppointmentMove` (1), `AppointmentRender` (3), `AppointmentSelect` (3), `Data` (3), `Date` (1), `EndProperty` (3), `LoadData` (1), `SelectedIndex` (1), `SlotRender` (2), `SlotSelect` (2), `StartProperty` (3), `Style` (2), `TextProperty` (3), `TItem` (3), `TodayText` (1)

## RadzenSelectBar

`Change` (3), `Data` (1), `Size` (8), `TextProperty` (1), `TValue` (8), `Value` (4), `ValueChanged` (2), `ValueProperty` (1)

## RadzenSelectBarItem

`Icon` (7), `Text` (18), `Value` (21)

## RadzenSeriesDataLabels

`Visible` (13)

## RadzenSidebar

`Expanded` (18), `Responsive` (31), `Style` (12)

## RadzenSidebarToggle

`Click` (23)

## RadzenSkeleton

`Shape` (1), `Style` (21)

## RadzenSlider

`Change` (4), `Disabled` (1), `Max` (6), `Min` (6), `Step` (1), `TValue` (6), `Value` (2), `ValueChanged` (1)

## RadzenSplitButton

`BusyText` (4), `ButtonStyle` (4), `Click` (23), `Disabled` (10), `Icon` (23), `IsBusy` (4), `Size` (18), `Text` (20), `Variant` (4)

## RadzenSplitButtonItem

`ButtonStyle` (12), `Icon` (65), `Text` (65), `Value` (65), `Variant` (12)

## RadzenStack

`AlignItems` (1392), `Gap` (2855), `JustifyContent` (531), `Orientation` (1860), `Style` (75), `Wrap` (417)

## RadzenStackedAreaSeries

`CategoryProperty` (2), `Data` (2), `Smooth` (2), `Title` (2), `ValueProperty` (2)

## RadzenStackedColumnSeries

`CategoryProperty` (2), `Data` (2), `Fill` (2), `Title` (2), `ValueProperty` (2)

## RadzenSteps

`Change` (6), `ShowStepsButtons` (5), `Style` (1)

## RadzenStepsItem

`Disabled` (6), `Icon` (7), `Text` (11)

## RadzenSwitch

`Change` (16), `Disabled` (8), `Style` (1), `TabIndex` (2), `TValue` (3), `Value` (17)

## RadzenTabs

`Change` (12), `RenderMode` (2), `SelectedIndex` (9), `Style` (1), `TabPosition` (2)

## RadzenTabsItem

`Disabled` (1), `Icon` (183), `IconColor` (9), `Text` (295)

## RadzenTemplateForm

`Data` (150), `InvalidSubmit` (1), `OnValidSubmit` (2), `Submit` (149), `TItem` (151)

## RadzenText

`Status` (3), `Style` (223), `TagName` (48), `Text` (9), `TextAlign` (17), `TextStyle` (3564)

## RadzenTextArea

`Change` (4), `Disabled` (1), `Immediate` (1), `MaxLength` (32), `Name` (23), `Placeholder` (23), `ReadOnly` (7), `Rows` (164), `Style` (32), `Value` (12), `ValueChanged` (4)

## RadzenTextBox

`AutoComplete` (3), `AutoFocus` (4), `Change` (82), `Disabled` (18), `Immediate` (29), `InputAttributes` (2), `MaxLength` (129), `Name` (177), `Placeholder` (278), `ReadOnly` (29), `Style` (93), `Value` (73), `ValueChanged` (7)

## RadzenTheme

`Theme` (2)

## RadzenTimeline

`AlignItems` (5), `LinePosition` (5), `Orientation` (5)

## RadzenTimelineItem

`PointStyle` (7)

## RadzenToggleButton

`ButtonStyle` (2), `Click` (1), `Icon` (3), `Text` (2), `ToggleIcon` (1), `Value` (2), `ValueChanged` (2), `Variant` (1)

## RadzenTooltip

Aucun paramètre nommé observé.

## RadzenTree

`Change` (4), `Data` (3), `Expand` (1), `Style` (1)

## RadzenTreeItem

`Expanded` (3), `HasChildren` (2), `Id` (2), `Selected` (2), `Text` (6), `Value` (2)

## RadzenTreeLevel

`ChildrenProperty` (3), `HasChildren` (3), `TextProperty` (3)

## RadzenUpload

`Accept` (1), `ChooseText` (1), `Complete` (1), `Error` (1), `Icon` (1), `Style` (1), `Url` (1)

## RadzenValueAxis

`FormatString` (3), `Formatter` (6), `Max` (8), `Min` (17), `Step` (8)

## RadzenWeekView

`EndTime` (1), `StartTime` (1), `Text` (1)

## Templates observés

`ChildContent` (205), `EditTemplate` (61), `EmptyTemplate` (66), `FooterTemplate` (36), `HeaderTemplate` (10), `LoadingTemplate` (3), `Template` (1132)
