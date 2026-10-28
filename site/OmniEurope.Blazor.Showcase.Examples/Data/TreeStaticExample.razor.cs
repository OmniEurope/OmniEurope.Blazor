using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A hand-written tree whose selection mode, opening and disabled items are parameters.</summary>
public partial class TreeStaticExample
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.Ordinal)
    {
        ["documents"] = "Documents",
        ["invoices"] = "Invoices",
        ["contracts"] = "Contracts",
        ["certificates"] = "Certificates",
        ["photos"] = "Photos",
        ["holidays"] = "Holidays",
        ["family"] = "Family",
        ["archives"] = "Archives",
        ["invoices-2025"] = "2025",
        ["invoices-2026"] = "2026"
    };

    /// <summary>Whether several items can be selected; off, selecting an item releases the previous one.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>Whether the branches start open (the Expanded parameter of each branch).</summary>
    [Parameter]
    public bool StartExpanded { get; set; } = true;

    /// <summary>Whether Archives and Certificates stay visible but can no longer be selected.</summary>
    [Parameter]
    public bool DisableItems { get; set; } = true;

    private IReadOnlyList<string> Selection { get; set; } = ["contracts"];

    private string SelectionText => Selection.Count == 0
        ? "(none)"
        : string.Join(", ", Selection.Select(value => Names.GetValueOrDefault(value, value)));

    private static string Join(IReadOnlyList<string> values) =>
        values.Count == 0 ? "(none)" : string.Join(", ", values);
}
