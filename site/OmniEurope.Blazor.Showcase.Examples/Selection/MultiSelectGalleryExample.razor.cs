using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>The always-open multiple list, the compact form with and without a template, and a disabled one.</summary>
public partial class MultiSelectGalleryExample
{
    /// <summary>Colours of the swatches, chosen to stay distinct in both themes.</summary>
    private static readonly IReadOnlyList<string> Swatches =
        ["#c2410c", "#1d4ed8", "#15803d", "#7c3aed", "#b91c1c", "#0e7490"];

    private static readonly IReadOnlyList<OmniOption<string>> TagOptions =
    [
        new("urgent", "Urgent"),
        new("internal", "Internal"),
        new("client", "Customer"),
        new("invoice", "Invoice"),
        new("archived", "Archived")
    ];

    // The tags created from the footer of the panel are typed data, not texts of the example.
    private readonly List<OmniOption<string>> _createdTags = [];

    private IReadOnlyList<string> ListTags { get; set; } = ["urgent"];

    private IReadOnlyList<string> CompactTags { get; set; } = ["urgent", "internal"];

    private IReadOnlyList<string> TemplateTags { get; set; } = [];

    private IReadOnlyList<string> FrozenTags { get; set; } = ["archived"];

    private string? TagSearch { get; set; }

    private IReadOnlyList<OmniOption<string>> EditableTagOptions => [.. TagOptions, .. _createdTags];

    private bool CanCreateSearchedTag =>
        !string.IsNullOrWhiteSpace(TagSearch)
        && !EditableTagOptions.Any(tag => string.Equals(tag.Text, TagSearch.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private static string Join(IReadOnlyList<string> values) => values.Count == 0 ? "(none)" : string.Join(", ", values);

    /// <summary>A colour that depends only on the value, so a tag keeps its swatch from one visit to the next.</summary>
    private static string SwatchOf(string value) => Swatches[value.Sum(letter => letter) % Swatches.Count];

    private void CreateSearchedTag()
    {
        if (!CanCreateSearchedTag)
        {
            return;
        }

        var name = TagSearch!.Trim();
        var value = name.ToLowerInvariant();
        _createdTags.Add(new OmniOption<string>(value, name));
        TemplateTags = [.. TemplateTags, value];
        TagSearch = null;
    }
}
