using OmniEurope.Blazor.Showcase.Components.Demos;

namespace OmniEurope.Blazor.Showcase.Demos;

/// <summary>
/// The families the gallery presents, in the order it presents them.
/// </summary>
/// <remarks>
/// The catalogue covers every component the library publishes; a test fails the build when one of
/// them has no entry here, so the gallery cannot quietly fall behind the library.
/// </remarks>
public static class DemoCatalog
{
    /// <summary>Every entry of the gallery.</summary>
    public static IReadOnlyList<DemoDefinition> All { get; } =
    [
        new("boutons", typeof(ButtonsDemo), "DemoButtonsTitle", "DemoButtonsSummary",
            ["DemoButtonsCapability1", "DemoButtonsCapability2", "DemoButtonsCapability3", "DemoButtonsCapability4"]),
        new("badges", typeof(BadgesDemo), "DemoBadgesTitle", "DemoBadgesSummary",
            ["DemoBadgesCapability1", "DemoBadgesCapability2"]),
        new("typographie", typeof(TypographyDemo), "DemoTypographyTitle", "DemoTypographySummary",
            ["DemoTypographyCapability1", "DemoTypographyCapability2", "DemoTypographyCapability3"]),
        new("mise-en-page", typeof(LayoutDemo), "DemoLayoutTitle", "DemoLayoutSummary",
            ["DemoLayoutCapability1", "DemoLayoutCapability2", "DemoLayoutCapability3", "DemoLayoutCapability4"]),
        new("coquille", typeof(ShellDemo), "DemoShellTitle", "DemoShellSummary",
            ["DemoShellCapability1", "DemoShellCapability2", "DemoShellCapability3", "DemoShellCapability4"]),
        new("medias", typeof(MediaDemo), "DemoMediaTitle", "DemoMediaSummary",
            ["DemoMediaCapability1", "DemoMediaCapability2", "DemoMediaCapability3", "DemoMediaCapability4"]),
        new("alertes", typeof(AlertsDemo), "DemoAlertsTitle", "DemoAlertsSummary",
            ["DemoAlertsCapability1", "DemoAlertsCapability2", "DemoAlertsCapability3"]),
        new("formulaires", typeof(FormDemo), "DemoFormTitle", "DemoFormSummary",
            ["DemoFormCapability1", "DemoFormCapability2", "DemoFormCapability3"]),
        new("saisie-avancee", typeof(InputsDemo), "DemoInputsTitle", "DemoInputsSummary",
            ["DemoInputsCapability1", "DemoInputsCapability2", "DemoInputsCapability3"]),
        new("selection", typeof(SelectionDemo), "DemoSelectionTitle", "DemoSelectionSummary",
            ["DemoSelectionCapability1", "DemoSelectionCapability2", "DemoSelectionCapability3"]),
        new("choix", typeof(ChoiceDemo), "DemoChoiceTitle", "DemoChoiceSummary",
            ["DemoChoiceCapability1", "DemoChoiceCapability2", "DemoChoiceCapability3"]),
        new("validation", typeof(ValidationDemo), "DemoValidationTitle", "DemoValidationSummary",
            ["DemoValidationCapability1", "DemoValidationCapability2", "DemoValidationCapability3"]),
        new("editeur", typeof(EditorDemo), "DemoEditorTitle", "DemoEditorSummary",
            ["DemoEditorCapability1", "DemoEditorCapability2", "DemoEditorCapability3"]),
        new("navigation", typeof(NavigationDemo), "DemoNavigationTitle", "DemoNavigationSummary",
            ["DemoNavigationCapability1", "DemoNavigationCapability2", "DemoNavigationCapability3"]),
        new("superpositions", typeof(OverlayDemo), "DemoOverlayTitle", "DemoOverlaySummary",
            ["DemoOverlayCapability1", "DemoOverlayCapability2", "DemoOverlayCapability3"]),
        new("retours", typeof(FeedbackDemo), "DemoFeedbackTitle", "DemoFeedbackSummary",
            ["DemoFeedbackCapability1", "DemoFeedbackCapability2", "DemoFeedbackCapability3", "DemoFeedbackCapability4"]),
        new("grille", typeof(DataGridDemo), "DemoGridTitle", "DemoGridSummary",
            ["DemoGridCapability1", "DemoGridCapability2", "DemoGridCapability3"]),
        new("grille-avancee", typeof(DataGridAdvancedDemo), "DemoGridAdvancedTitle", "DemoGridAdvancedSummary",
            ["DemoGridAdvancedCapability1", "DemoGridAdvancedCapability2", "DemoGridAdvancedCapability3", "DemoGridAdvancedCapability4"]),
        new("listes", typeof(DataListDemo), "DemoDataListTitle", "DemoDataListSummary",
            ["DemoDataListCapability1", "DemoDataListCapability2", "DemoDataListCapability3"]),
        new("arborescence", typeof(TreeDemo), "DemoTreeTitle", "DemoTreeSummary",
            ["DemoTreeCapability1", "DemoTreeCapability2", "DemoTreeCapability3"]),
        new("agenda", typeof(SchedulerDemo), "DemoSchedulerTitle", "DemoSchedulerSummary",
            ["DemoSchedulerCapability1", "DemoSchedulerCapability2", "DemoSchedulerCapability3"]),
        new("graphiques", typeof(ChartDemo), "DemoChartTitle", "DemoChartSummary",
            ["DemoChartCapability1", "DemoChartCapability2"]),
        new("graphiques-avances", typeof(ChartsExtendedDemo), "DemoChartsExtendedTitle", "DemoChartsExtendedSummary",
            ["DemoChartsExtendedCapability1", "DemoChartsExtendedCapability2", "DemoChartsExtendedCapability3"]),
        new("diagramme", typeof(DiagramDemo), "DemoDiagramTitle", "DemoDiagramSummary",
            ["DemoDiagramCapability1", "DemoDiagramCapability2", "DemoDiagramCapability3", "DemoDiagramCapability4"])
    ];

    /// <summary>The entry with the given key, or the first entry when the key is unknown.</summary>
    public static DemoDefinition Resolve(string? key) =>
        All.FirstOrDefault(demo => string.Equals(demo.Key, key, StringComparison.Ordinal)) ?? All[0];
}
