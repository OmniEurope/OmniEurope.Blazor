using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>The content of the header area of the preview card.</summary>
public partial class CardsHeader
{
    /// <summary>What the header holds.</summary>
    [Parameter]
    public CardHeaderKind Kind { get; set; } = CardHeaderKind.TitleAndBadge;
}
