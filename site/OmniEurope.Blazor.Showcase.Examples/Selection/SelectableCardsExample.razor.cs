using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A transport card group whose choice mode, state and descriptions are parameters.</summary>
public partial class SelectableCardsExample
{
    /// <summary>Whether the cards become check boxes and the group binds a list of values.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>Whether every card of the group ignores clicks and keeps its state.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether each card shows its explanation line.</summary>
    [Parameter]
    public bool ShowDescriptions { get; set; } = true;

    private string Transport { get; set; } = SelectableCardsChoices.Train;

    private IReadOnlyList<string> Transports { get; set; } = [SelectableCardsChoices.Train];

    private string SelectionEcho => Multiple
        ? Transports.Count == 0 ? "none" : string.Join(", ", Transports)
        : Transport;
}
