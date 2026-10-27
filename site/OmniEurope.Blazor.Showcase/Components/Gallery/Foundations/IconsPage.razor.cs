using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Foundations;

/// <summary>The Icons page: one icon with its size and accessible label, the glyph path and the whole set.</summary>
public partial class IconsPage
{
    private static readonly OmniControlSize[] AllSizes = Enum.GetValues<OmniControlSize>();

    // The enumeration names are the data shown, not interface text.
    private static readonly IReadOnlyList<OmniOption<OmniIconName>> IconOptions =
        [.. Enum.GetValues<OmniIconName>().Select(name => new OmniOption<OmniIconName>(name, name.ToString()))];

    [Inject]
    private IStringLocalizer<FoundationsStrings> Text { get; set; } = default!;

    private OmniControlSize Size { get; set; } = OmniControlSize.Large;

    private OmniIconName SelectedIcon { get; set; } = OmniIconName.Star;

    private string LabelText { get; set; } = string.Empty;

    private IReadOnlyList<OmniOption<OmniControlSize>> SizeOptions =>
        [.. AllSizes.Select(size => new OmniOption<OmniControlSize>(size, SizeLabel(size)))];

    protected override void OnInitialized() => LabelText = Text["IconsLabelDefault"];

    private string SizeLabel(OmniControlSize size) => size switch
    {
        OmniControlSize.Small => Text["IconsSizeSmall"],
        OmniControlSize.Medium => Text["IconsSizeMedium"],
        _ => Text["IconsSizeLarge"]
    };
}
