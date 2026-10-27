using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>The body of the preview card, short or long.</summary>
public partial class CardsBody
{
    /// <summary>Whether the body holds three paragraphs instead of one.</summary>
    [Parameter]
    public bool Long { get; set; }
}
