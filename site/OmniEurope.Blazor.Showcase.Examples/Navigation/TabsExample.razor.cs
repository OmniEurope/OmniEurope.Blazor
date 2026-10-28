using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>Four tabs whose icons, counters, disabled tab and selected key are parameters.</summary>
public partial class TabsExample
{
    /// <summary>The key of the Summary tab.</summary>
    public const string SummaryKey = "summary";

    /// <summary>The key of the Documents tab.</summary>
    public const string DocumentsKey = "documents";

    /// <summary>The key of the History tab.</summary>
    public const string HistoryKey = "history";

    /// <summary>The key of the Archive tab.</summary>
    public const string ArchiveKey = "archive";

    private const int HistoryCount = 12;

    /// <summary>The key of the selected tab; bind it to drive the strip from the host.</summary>
    [Parameter]
    public string Value { get; set; } = SummaryKey;

    /// <summary>Raised with the key of the tab chosen.</summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Whether each tab shows an icon before its title.</summary>
    [Parameter]
    public bool ShowIcons { get; set; } = true;

    /// <summary>Whether the Documents and History titles carry a counter badge.</summary>
    [Parameter]
    public bool ShowCounters { get; set; } = true;

    /// <summary>Whether the Archive tab is disabled.</summary>
    [Parameter]
    public bool DisableArchive { get; set; } = true;

    /// <summary>The counter of the Documents tab.</summary>
    [Parameter]
    public int DocumentCount { get; set; } = 14;

    /// <summary>The icon of a tab, or null when icons are off: an empty fragment would still draw the slot.</summary>
    private RenderFragment? IconOf(OmniIconName name) => ShowIcons
        ? builder =>
        {
            builder.OpenComponent<OmniIcon>(0);
            builder.AddComponentParameter(1, nameof(OmniIcon.Name), name);
            builder.CloseComponent();
        }
        : null;

    private async Task SelectAsync(string? key)
    {
        Value = key ?? Value;
        await ValueChanged.InvokeAsync(Value);
    }
}
