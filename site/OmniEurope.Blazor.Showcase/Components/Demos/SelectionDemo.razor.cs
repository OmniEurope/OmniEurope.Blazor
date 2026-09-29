using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class SelectionDemo
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private IReadOnlyList<OmniOption<string>> Countries { get; set; } = [];

    private static readonly IReadOnlyList<OmniOption<string>> Cities =
    [
        new("bru", "Bruxelles"),
        new("par", "Paris"),
        new("lux", "Luxembourg"),
        new("ams", "Amsterdam")
    ];

    private IReadOnlyList<OmniOption<string>> Tags { get; set; } = [];

    private string Country { get; set; } = "be";

    private string City { get; set; } = "bru";

    private IReadOnlyList<string> SelectedTags { get; set; } = ["urgent"];

    private IReadOnlyList<string> CompactTags { get; set; } = ["urgent", "interne"];

    // A tag the search did not find can be created from the panel's footer, which is why the field
    // is bound: the list alone never says what was typed.
    private List<OmniOption<string>> EditableTags { get; } = [];

    private static readonly IReadOnlyList<string> Swatches =
        ["#c2410c", "#1d4ed8", "#15803d", "#7c3aed", "#b91c1c"];

    private IReadOnlyList<string> FilterableTags { get; set; } = [];

    private string? TagSearch { get; set; }

    private bool CanCreateSearchedTag =>
        !string.IsNullOrWhiteSpace(TagSearch)
        && !EditableTags.Any(tag => string.Equals(tag.Text, TagSearch.Trim(), StringComparison.CurrentCultureIgnoreCase));

    /// <summary>Countries and tags keep their keys; only their texts follow the reader's language.</summary>
    protected override void OnInitialized()
    {
        Countries =
        [
            new("be", Text["DemoSelectionBelgium"]),
            new("fr", Text["DemoSelectionFrance"]),
            new("lu", Text["DemoSelectionLuxembourg"]),
            new("nl", Text["DemoSelectionNetherlands"])
        ];
        Tags =
        [
            new("urgent", Text["DemoSelectionTagUrgent"]),
            new("interne", Text["DemoSelectionTagInternal"]),
            new("archive", Text["DemoSelectionTagArchived"])
        ];
        EditableTags.AddRange(Tags);
    }

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
