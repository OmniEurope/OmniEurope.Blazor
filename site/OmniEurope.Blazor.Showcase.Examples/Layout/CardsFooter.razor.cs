using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>The content of the footer area of the preview card.</summary>
public partial class CardsFooter
{
    /// <summary>What the footer holds.</summary>
    [Parameter]
    public CardFooterKind Kind { get; set; } = CardFooterKind.Actions;

    /// <summary>Raised with the name of the button that was clicked.</summary>
    [Parameter]
    public EventCallback<string> OnAction { get; set; }
}
