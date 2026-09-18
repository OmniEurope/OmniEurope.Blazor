using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ButtonStatesDemo
{
    /// <summary>The variants the mockup compares, the principal action first.</summary>
    private static readonly (OmniButtonVariant Variant, string Key)[] Variants =
    [
        (OmniButtonVariant.Primary, "StatesPrimary"),
        (OmniButtonVariant.Success, "StatesSuccess"),
        (OmniButtonVariant.Secondary, "StatesSecondary"),
        (OmniButtonVariant.Ghost, "StatesGhost")
    ];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;
}
