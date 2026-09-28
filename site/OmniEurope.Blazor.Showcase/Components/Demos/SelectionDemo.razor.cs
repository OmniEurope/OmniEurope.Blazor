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

    // A tag the search did not find can be created from the panel's footer, which is why the field
    // is bound: the list alone never says what was typed.
    private List<OmniOption<string>> EditableTags { get; } =
    [
        new("urgent", "Urgent"),
        new("interne", "Interne"),
        new("archive", "Archivé")
    ];

    private static readonly IReadOnlyList<string> Swatches =
        ["#c2410c", "#1d4ed8", "#15803d", "#7c3aed", "#b91c1c"];

    private IReadOnlyList<string> FilterableTags { get; set; } = [];

    private string? TagSearch { get; set; }

    private bool CanCreateSearchedTag =>
        !string.IsNullOrWhiteSpace(TagSearch)
        && !EditableTags.Any(tag => string.Equals(tag.Text, TagSearch.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private string SwatchOf(string value) =>
        Swatches[Math.Abs(value.GetHashCode(StringComparison.Ordinal)) % Swatches.Count];

    private void CreateSearchedTag()
    {
        if (!CanCreateSearchedTag)
        {
            return;
        }

        var name = TagSearch!.Trim();
        var created = new OmniOption<string>(name.ToLowerInvariant(), name);
        EditableTags.Add(created);
        FilterableTags = [.. FilterableTags, created.Value];
        TagSearch = null;
    }
}
