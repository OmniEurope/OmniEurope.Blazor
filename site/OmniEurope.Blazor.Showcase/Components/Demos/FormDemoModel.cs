namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// The state the form demo edits. Kept out of the demo file so the source shown in the gallery
/// stays the markup a reader would copy.
/// </summary>
public sealed class FormDemoModel
{
    /// <summary>The slider value.</summary>
    public double Budget { get; set; } = 40;
}
