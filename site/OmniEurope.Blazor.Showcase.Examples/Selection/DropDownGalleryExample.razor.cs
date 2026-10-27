using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>Drop-downs bound to an enumeration, an integer and a string, with an empty entry and disabled.</summary>
public partial class DropDownGalleryExample
{
    private static readonly IReadOnlyList<OmniOption<string>> CityOptions =
    [
        new("bru", "Brussels"),
        new("gand", "Ghent"),
        new("liege", "Liège"),
        new("namur", "Namur")
    ];

    private static readonly IReadOnlyList<OmniOption<OmniControlSize>> SizeOptions =
    [
        new(OmniControlSize.Small, "Small"),
        new(OmniControlSize.Medium, "Medium"),
        new(OmniControlSize.Large, "Large")
    ];

    private static readonly IReadOnlyList<OmniOption<int>> SeatOptions =
    [
        new(2, "2 seats"),
        new(4, "4 seats"),
        new(6, "6 seats"),
        new(8, "8 seats")
    ];

    private OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    private int Seats { get; set; } = 4;

    private string? City { get; set; }

    private string FrozenCity { get; set; } = "liege";
}
