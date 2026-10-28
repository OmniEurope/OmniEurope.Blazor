using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Foundations;

/// <summary>The Badges page: the settings of one badge, then every tone, fill, ink, icon and width.</summary>
public partial class BadgesPage
{
    private static readonly OmniTone[] AllVariants = Enum.GetValues<OmniTone>();

    private static readonly OmniFill[] AllFills = Enum.GetValues<OmniFill>();

    private static readonly OmniBadgeShape[] AllShapes = Enum.GetValues<OmniBadgeShape>();

    [Inject]
    private IStringLocalizer<FoundationsStrings> Text { get; set; } = default!;

    private OmniTone Variant { get; set; } = OmniTone.Accent;

    private OmniFill Fill { get; set; } = OmniFill.Tonal;

    private OmniBadgeShape Shape { get; set; } = OmniBadgeShape.Pill;

    private string BadgeText { get; set; } = string.Empty;

    private bool WithIcon { get; set; }

    private IReadOnlyList<OmniOption<OmniTone>> VariantOptions =>
        [.. AllVariants.Select(variant => new OmniOption<OmniTone>(variant, VariantLabel(variant)))];

    private IReadOnlyList<OmniOption<OmniFill>> FillOptions =>
        [.. AllFills.Select(fill => new OmniOption<OmniFill>(fill, FillLabel(fill)))];

    private IReadOnlyList<OmniOption<OmniBadgeShape>> ShapeOptions =>
        [.. AllShapes.Select(shape => new OmniOption<OmniBadgeShape>(shape, ShapeLabel(shape)))];

    protected override void OnInitialized() => BadgeText = Text["BadgesLabelDefault"];

    private string VariantLabel(OmniTone variant) => variant switch
    {
        OmniTone.Accent => Text["BadgesVariantAccent"],
        OmniTone.Info => Text["BadgesVariantInfo"],
        OmniTone.Success => Text["BadgesVariantSuccess"],
        OmniTone.Warning => Text["BadgesVariantWarning"],
        OmniTone.Danger => Text["BadgesVariantDanger"],
        _ => Text["BadgesVariantNeutral"]
    };

    private string ShapeLabel(OmniBadgeShape shape) => shape switch
    {
        OmniBadgeShape.Rounded => Text["BadgesShapeRounded"],
        OmniBadgeShape.Square => Text["BadgesShapeSquare"],
        _ => Text["BadgesShapePill"]
    };

    private string FillLabel(OmniFill fill) => fill switch
    {
        OmniFill.Solid => Text["BadgesFillSolid"],
        OmniFill.Outline => Text["BadgesFillOutline"],
        _ => Text["BadgesFillTonal"]
    };
}
