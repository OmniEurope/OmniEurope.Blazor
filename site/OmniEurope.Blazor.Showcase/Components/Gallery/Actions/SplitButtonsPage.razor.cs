using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Actions;

/// <summary>The Split buttons page: the settings of one split button, then its states, variants and sizes.</summary>
public partial class SplitButtonsPage
{
    [Inject]
    private IStringLocalizer<ActionsStrings> Text { get; set; } = default!;

    private OmniButtonVariant Variant { get; set; } = OmniButtonVariant.Secondary;

    private OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    private bool Disabled { get; set; }

    private bool Busy { get; set; }

    private bool BusyOnClick { get; set; } = true;

    private bool DeleteDisabled { get; set; } = true;

    /// <summary>The main text is one of the three option values, which are the English words the example shows.</summary>
    private string MainText { get; set; } = "Publish";

    private IReadOnlyList<OmniOption<OmniButtonVariant>> VariantOptions => ButtonVariantOptions.For(Text);

    private IReadOnlyList<OmniOption<OmniControlSize>> SizeOptions =>
    [
        new(OmniControlSize.Small, Text["SplitButtonsSizeSmall"]),
        new(OmniControlSize.Medium, Text["SplitButtonsSizeMedium"]),
        new(OmniControlSize.Large, Text["SplitButtonsSizeLarge"])
    ];

    private IReadOnlyList<OmniOption<string>> MainOptions =>
    [
        new("Publish", Text["SplitButtonsMainPublish"]),
        new("Save", Text["SplitButtonsMainSave"]),
        new("Send", Text["SplitButtonsMainSend"])
    ];
}
