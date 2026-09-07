namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class SelectionDemo
{
    private static readonly IReadOnlyList<OmniOption<string>> Countries =
    [
        new("be", "Belgique"),
        new("fr", "France"),
        new("lu", "Luxembourg"),
        new("nl", "Pays-Bas")
    ];

    private static readonly IReadOnlyList<OmniOption<string>> Cities =
    [
        new("bru", "Bruxelles"),
        new("par", "Paris"),
        new("lux", "Luxembourg"),
        new("ams", "Amsterdam")
    ];

    private static readonly IReadOnlyList<OmniOption<string>> Tags =
    [
        new("urgent", "Urgent"),
        new("interne", "Interne"),
        new("archive", "Archivé")
    ];

    private string Country { get; set; } = "be";

    private string City { get; set; } = "bru";

    private IReadOnlyList<string> SelectedTags { get; set; } = ["urgent"];

    private IReadOnlyList<string> CompactTags { get; set; } = ["urgent", "interne"];
}
