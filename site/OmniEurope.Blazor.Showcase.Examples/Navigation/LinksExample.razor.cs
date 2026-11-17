using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>A list of links that open in the same tab or in a new one.</summary>
public partial class LinksExample
{
    private static readonly IReadOnlyList<SampleLink> Links =
    [
        new("Tabs page", "components/tabs"),
        new("Steps page", "components/steps"),
        new("Profile menu page", "components/profile-menu")
    ];

    /// <summary>Whether the links open in a new tab.</summary>
    [Parameter]
    public bool NewTab { get; set; }

    private static readonly IReadOnlyList<SampleCase> Cases = [new("C-104"), new("C-117")];

    private string Activity { get; set; } = "Click a row, or the action link of a row.";

    private void OnRowClick(OmniDataGridRowMouseEventArgs<SampleCase> args) => Activity = $"Row {args.Item.Number} clicked.";

    private void Open(SampleCase item) => Activity = $"Action link of {item.Number} run, the row was not clicked.";

    private sealed record SampleLink(string Text, string Href);

    private sealed record SampleCase(string Number);
}
