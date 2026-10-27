using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>A single badge: its tone, its fill, its label and an optional icon are parameters.</summary>
public partial class BadgeExample
{
    /// <summary>What the badge means.</summary>
    [Parameter]
    public OmniTone Tone { get; set; } = OmniTone.Accent;

    /// <summary>How the tone is drawn: tinted, outlined or solid.</summary>
    [Parameter]
    public OmniFill Fill { get; set; } = OmniFill.Tonal;

    /// <summary>The word or number the badge carries.</summary>
    [Parameter]
    public string Label { get; set; } = "New";

    /// <summary>Whether an icon is placed before the label.</summary>
    [Parameter]
    public bool WithIcon { get; set; }

    private string DisplayedLabel => string.IsNullOrWhiteSpace(Label) ? "New" : Label;
}
