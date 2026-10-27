using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>A small, ordinary screen to repaint: the content every theme scope of the examples holds.</summary>
public partial class ThemeSample
{
    [Inject]
    private OmniOverlayService Overlay { get; set; } = default!;

    private string Name { get; set; } = string.Empty;

    private bool Notify { get; set; } = true;

    /// <summary>The buttons say they were clicked rather than stay mute.</summary>
    private void NotifySample() =>
        Overlay.Notify(
            "This button only illustrates the component: it performs no real action.",
            OmniSeverity.Info,
            title: "Sample button",
            duration: TimeSpan.FromSeconds(3));
}
