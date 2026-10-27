using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>List boxes and check box lists bound to an integer, to enumeration values and to strings, enabled or not.</summary>
public partial class ListsGalleryExample
{
    private static readonly IReadOnlyList<OmniOption<string>> CityOptions =
    [
        new("ams", "Amsterdam"),
        new("bru", "Brussels"),
        new("gand", "Ghent"),
        new("liege", "Liège"),
        new("lux", "Luxembourg"),
        new("par", "Paris")
    ];

    private static readonly IReadOnlyList<OmniOption<int>> YearOptions =
    [
        new(2022, "2022"),
        new(2023, "2023"),
        new(2024, "2024"),
        new(2025, "2025"),
        new(2026, "2026"),
        new(2027, "2027")
    ];

    private static readonly IReadOnlyList<OmniOption<OmniTone>> ToneOptions =
    [
        new(OmniTone.Neutral, "Neutral"),
        new(OmniTone.Accent, "Accent"),
        new(OmniTone.Success, "Success"),
        new(OmniTone.Warning, "Warning"),
        new(OmniTone.Danger, "Danger")
    ];

    /// <summary>Whether the certified copy stays visible but inert.</summary>
    [Parameter]
    public bool DisableOption { get; set; } = true;

    private int Year { get; set; } = 2026;

    private string FrozenCity { get; set; } = "gand";

    private IReadOnlyList<OmniTone> Tones { get; set; } = [OmniTone.Accent, OmniTone.Success];

    private IReadOnlyList<string> FrozenExtras { get; set; } = ["receipt", "copy"];

    private IReadOnlyList<OmniOption<string>> ExtraOptions =>
    [
        new("receipt", "Acknowledgement of receipt"),
        new("copy", "Certified copy", Disabled: DisableOption),
        new("tracking", "Email tracking"),
        new("insurance", "Insurance")
    ];

    private static string Join<T>(IReadOnlyList<T> values) => values.Count == 0 ? "(none)" : string.Join(", ", values);
}
