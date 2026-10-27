using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>Radio lists bound to an integer, an enumeration and a string, the last one disabled.</summary>
public partial class RadioButtonsGalleryExample
{
    private static readonly IReadOnlyList<OmniOption<string>> ShippingOptions =
    [
        new("standard", "Standard"),
        new("express", "Express"),
        new("pickup", "Agency pickup")
    ];

    private static readonly IReadOnlyList<OmniOption<int>> PassengerOptions =
    [
        new(1, "1"),
        new(2, "2"),
        new(3, "3"),
        new(4, "4")
    ];

    private static readonly IReadOnlyList<OmniOption<OmniSeverity>> SeverityOptions =
    [
        new(OmniSeverity.Info, "Information"),
        new(OmniSeverity.Success, "Success"),
        new(OmniSeverity.Warning, "Warning"),
        new(OmniSeverity.Danger, "Danger")
    ];

    private int Passengers { get; set; } = 2;

    private OmniSeverity Severity { get; set; } = OmniSeverity.Warning;

    private string FrozenShipping { get; set; } = "express";
}
