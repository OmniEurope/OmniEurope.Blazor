using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>Buttons, badges, alerts and a small form, to see what the theme in force does to each.</summary>
public partial class ThemeComponentsExample
{
    private static IReadOnlyList<OmniOption<string>> CityOptions { get; } =
        [new("Lisbon", "Lisbon"), new("Porto", "Porto"), new("Seville", "Seville")];

    [Inject]
    private OmniOverlayService Overlay { get; set; } = default!;

    private string Name { get; set; } = string.Empty;

    private string City { get; set; } = "Lisbon";

    private bool Accept { get; set; } = true;

    private bool Alerts { get; set; }

    private double Budget { get; set; } = 60;

    /// <summary>The buttons say they were clicked rather than stay mute.</summary>
    private void NotifySample() =>
        Overlay.Notify(
            "This button only illustrates the component: it performs no real action.",
            OmniSeverity.Info,
            title: "Sample button",
            duration: TimeSpan.FromSeconds(3));
}
