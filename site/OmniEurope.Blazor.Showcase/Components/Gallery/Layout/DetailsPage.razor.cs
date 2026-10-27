using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Layout;

/// <summary>The Details page: a detail page in each of its states, a header with a pinned action, and the empty states.</summary>
public partial class DetailsPage
{
    [Inject]
    private IStringLocalizer<LayoutStrings> Text { get; set; } = default!;

    private OmniDetailState State { get; set; } = OmniDetailState.Found;

    private OmniButtonVariant BackVariant { get; set; } = OmniButtonVariant.Primary;

    private bool Framed { get; set; }

    private IReadOnlyList<OmniOption<OmniDetailState>> StateOptions =>
    [
        new(OmniDetailState.Loading, Text["DetailsStateLoading"]),
        new(OmniDetailState.Found, Text["DetailsStateFound"]),
        new(OmniDetailState.NotFound, Text["DetailsStateMissing"])
    ];

    private IReadOnlyList<OmniOption<OmniButtonVariant>> BackVariantOptions =>
    [
        new(OmniButtonVariant.Primary, Text["DetailsBackPrimary"]),
        new(OmniButtonVariant.Ghost, Text["DetailsBackGhost"])
    ];
}
