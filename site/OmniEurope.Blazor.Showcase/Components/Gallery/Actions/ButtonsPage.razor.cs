using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Actions;

/// <summary>The Buttons page: the settings of one button, then every variant, size and state.</summary>
public partial class ButtonsPage
{
    [Inject]
    private IStringLocalizer<ActionsStrings> Text { get; set; } = default!;

    private OmniButtonVariant Variant { get; set; } = OmniButtonVariant.Primary;

    private OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    private OmniButtonType ButtonType { get; set; } = OmniButtonType.Button;

    private bool IconOnly { get; set; }

    private bool Disabled { get; set; }

    private bool Busy { get; set; }

    private bool BusyOnClick { get; set; } = true;

    private IReadOnlyList<OmniOption<OmniButtonVariant>> VariantOptions =>
    [
        new(OmniButtonVariant.Primary, Text["ButtonsVariantPrimary"]),
        new(OmniButtonVariant.Secondary, Text["ButtonsVariantSecondary"]),
        new(OmniButtonVariant.Ghost, Text["ButtonsVariantGhost"]),
        new(OmniButtonVariant.Success, Text["ButtonsVariantSuccess"]),
        new(OmniButtonVariant.Warning, Text["ButtonsVariantWarning"]),
        new(OmniButtonVariant.Info, Text["ButtonsVariantInfo"]),
        new(OmniButtonVariant.Danger, Text["ButtonsVariantDanger"]),
        new(OmniButtonVariant.GhostDanger, Text["ButtonsVariantGhostDanger"])
    ];

    private IReadOnlyList<OmniOption<OmniControlSize>> SizeOptions =>
    [
        new(OmniControlSize.Small, Text["ButtonsSizeSmall"]),
        new(OmniControlSize.Medium, Text["ButtonsSizeMedium"]),
        new(OmniControlSize.Large, Text["ButtonsSizeLarge"])
    ];

    private IReadOnlyList<OmniOption<OmniButtonType>> TypeOptions =>
    [
        new(OmniButtonType.Button, Text["ButtonsTypeButton"]),
        new(OmniButtonType.Submit, Text["ButtonsTypeSubmit"]),
        new(OmniButtonType.Reset, Text["ButtonsTypeReset"])
    ];

    private IReadOnlyList<OmniOption<bool>> ContentOptions =>
    [
        new(false, Text["ButtonsContentIconAndLabel"]),
        new(true, Text["ButtonsContentIconOnly"])
    ];
}
