using OmniEurope.Blazor.Showcase.Examples.Navigation;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Navigation;

/// <summary>The Tabs page: the settings of one strip, then the plain, overflowing and settings-page shapes.</summary>
public partial class TabsPage
{
    [Inject]
    private IStringLocalizer<NavigationStrings> Text { get; set; } = default!;

    private string SelectedTab { get; set; } = TabsExample.SummaryKey;

    private bool ShowIcons { get; set; } = true;

    private bool ShowCounters { get; set; } = true;

    private bool DisableArchive { get; set; } = true;

    private double DocumentCountValue { get; set; } = 14;

    private int DocumentCount => (int)DocumentCountValue;

    // The bar drives the same value as the strip. The disabled tab stays choosable here, to show that it
    // is selected by code even when a click on it is refused.
    private IReadOnlyList<OmniOption<string>> TabOptions =>
    [
        new(TabsExample.SummaryKey, Text["TabsTabSummary"]),
        new(TabsExample.DocumentsKey, Text["TabsTabDocuments"]),
        new(TabsExample.HistoryKey, Text["TabsTabHistory"]),
        new(TabsExample.ArchiveKey, Text["TabsTabArchive"])
    ];
}
