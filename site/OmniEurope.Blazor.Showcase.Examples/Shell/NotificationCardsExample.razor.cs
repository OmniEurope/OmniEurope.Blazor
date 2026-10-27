using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Shell;

/// <summary>The notification card placed by hand: one per role, and one offering an action.</summary>
public partial class NotificationCardsExample
{
    private const string ActionTitle = "Entry archived";

    private static readonly (OmniSeverity Severity, string Title, string Message)[] Roles =
    [
        (OmniSeverity.Success, "Success", "A notification of this role, to compare the tints."),
        (OmniSeverity.Warning, "Warning", "A notification of this role, to compare the tints."),
        (OmniSeverity.Danger, "Error", "A notification of this role, to compare the tints."),
        (OmniSeverity.Info, "Information", "A notification of this role, to compare the tints.")
    ];

    /// <summary>Whether each card carries a cross.</summary>
    [Parameter]
    public bool Dismissible { get; set; } = true;

    private HashSet<string> Hidden { get; } = [];

    private int ActionCount { get; set; }
}
