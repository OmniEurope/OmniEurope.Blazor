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
        new("mise-en-page", typeof(LayoutDemo), "DemoLayoutTitle", "DemoLayoutSummary",
            ["DemoLayoutCapability5"]),
        new("coquille", typeof(ShellDemo), "DemoShellTitle", "DemoShellSummary",
            ["DemoShellCapability1", "DemoShellCapability2", "DemoShellCapability3", "DemoShellCapability4"]),
        new("coquille-application", typeof(AppShellDemo), "DemoAppShellTitle", "DemoAppShellSummary",
            ["DemoAppShellCapability1", "DemoAppShellCapability2", "DemoAppShellCapability3"]),
        new("surfaces", typeof(SurfacesDemo), "DemoSurfacesTitle", "DemoSurfacesSummary",
            ["DemoSurfacesCapability1", "DemoSurfacesCapability2", "DemoSurfacesCapability3"]),
        new("formulaires", typeof(FormDemo), "DemoFormTitle", "DemoFormSummary",
            ["DemoFormCapability3"]),
        new("formulaire-vertical", typeof(VerticalFormDemo), "DemoVerticalFormTitle", "DemoVerticalFormSummary",
            ["DemoVerticalFormCapability2", "DemoVerticalFormCapability3"]),
        new("reglages-onglets", typeof(SettingsTabsDemo), "DemoSettingsTabsTitle", "DemoSettingsTabsSummary",
            ["DemoSettingsTabsCapability1", "DemoSettingsTabsCapability2"]),
        new("saisie-avancee", typeof(InputsDemo), "DemoInputsTitle", "DemoInputsSummary",
            ["DemoInputsCapability3", "DemoInputsCapability4"]),
        new("choix", typeof(ChoiceDemo), "DemoChoiceTitle", "DemoChoiceSummary",
            ["DemoChoiceCapability1", "DemoChoiceCapability2", "DemoChoiceCapability3", "DemoChoiceCapability4"]),
        new("editeur", typeof(EditorDemo), "DemoEditorTitle", "DemoEditorSummary",
            ["DemoEditorCapability1", "DemoEditorCapability2", "DemoEditorCapability3"]),
        new("navigation", typeof(NavigationDemo), "DemoNavigationTitle", "DemoNavigationSummary",
            ["DemoNavigationCapability1", "DemoNavigationCapability2", "DemoNavigationCapability3"]),
        new("pages", typeof(PagesDemo), "DemoPagesTitle", "DemoPagesSummary",
            ["DemoPagesCapability1", "DemoPagesCapability2", "DemoPagesCapability3", "DemoPagesCapability4"]),
        new("parcours", typeof(FlowsDemo), "DemoFlowsTitle", "DemoFlowsSummary",
            ["DemoFlowsCapability1", "DemoFlowsCapability2", "DemoFlowsCapability3"]),
        new("superpositions", typeof(OverlayDemo), "DemoOverlayTitle", "DemoOverlaySummary",
            ["DemoOverlayCapability1", "DemoOverlayCapability2", "DemoOverlayCapability3"]),
        new("retours", typeof(FeedbackDemo), "DemoFeedbackTitle", "DemoFeedbackSummary",
            ["DemoFeedbackCapability1", "DemoFeedbackCapability2", "DemoFeedbackCapability3", "DemoFeedbackCapability4"]),
        new("chargement", typeof(LoadingDemo), "DemoLoadingTitle", "DemoLoadingSummary",
            ["DemoLoadingCapability1", "DemoLoadingCapability2", "DemoLoadingCapability3"]),
        new("grille", typeof(DataGridDemo), "DemoGridTitle", "DemoGridSummary",
            ["DemoGridCapability1", "DemoGridCapability2", "DemoGridCapability3"]),
        new("grilles-maquette", typeof(RunGridsDemo), "DemoRunGridsTitle", "DemoRunGridsSummary",
            ["DemoRunGridsCapability1", "DemoRunGridsCapability2", "DemoRunGridsCapability3"]),
        new("grille-avancee", typeof(DataGridAdvancedDemo), "DemoGridAdvancedTitle", "DemoGridAdvancedSummary",
            ["DemoGridAdvancedCapability1", "DemoGridAdvancedCapability2", "DemoGridAdvancedCapability3", "DemoGridAdvancedCapability4"]),
        new("listes", typeof(DataListDemo), "DemoDataListTitle", "DemoDataListSummary",
            ["DemoDataListCapability1", "DemoDataListCapability2", "DemoDataListCapability3"]),
        new("arborescence", typeof(TreeDemo), "DemoTreeTitle", "DemoTreeSummary",
            ["DemoTreeCapability1", "DemoTreeCapability2", "DemoTreeCapability3"]),
        new("tableur", typeof(SpreadsheetDemo), "DemoSpreadsheetTitle", "DemoSpreadsheetSummary",
            ["DemoSpreadsheetCapability1", "DemoSpreadsheetCapability2", "DemoSpreadsheetCapability3"]),
        new("agenda", typeof(SchedulerDemo), "DemoSchedulerTitle", "DemoSchedulerSummary",
            ["DemoSchedulerCapability1", "DemoSchedulerCapability2", "DemoSchedulerCapability3"]),
        new("planification", typeof(PlanningDemo), "DemoPlanningTitle", "DemoPlanningSummary",
            ["DemoPlanningCapability1", "DemoPlanningCapability2", "DemoPlanningCapability3"]),
        new("graphiques", typeof(ChartDemo), "DemoChartTitle", "DemoChartSummary",
            ["DemoChartCapability1", "DemoChartCapability2"]),
        new("graphiques-avances", typeof(ChartsExtendedDemo), "DemoChartsExtendedTitle", "DemoChartsExtendedSummary",
            ["DemoChartsExtendedCapability1", "DemoChartsExtendedCapability2", "DemoChartsExtendedCapability3"]),
        new("diagramme", typeof(DiagramDemo), "DemoDiagramTitle", "DemoDiagramSummary",
            ["DemoDiagramCapability1", "DemoDiagramCapability2", "DemoDiagramCapability3", "DemoDiagramCapability4"])
    ];

    /// <summary>The entry with the given key, or <see langword="null"/> when no entry has it.</summary>
    /// <remarks>
    /// An unknown key is not quietly mapped to another entry: an address the gallery does not know
    /// must read as "not found", not show the first demo under a wrong address.
    /// </remarks>
    public static DemoDefinition? Find(string? key) =>
        All.FirstOrDefault(demo => string.Equals(demo.Key, key, StringComparison.Ordinal));

    /// <summary>The entry with the given key, for a key the caller knows to exist.</summary>
    /// <exception cref="ArgumentException">No entry has <paramref name="key"/>.</exception>
    public static DemoDefinition Resolve(string key) =>
        Find(key) ?? throw new ArgumentException($"The gallery has no entry '{key}'.", nameof(key));
}
