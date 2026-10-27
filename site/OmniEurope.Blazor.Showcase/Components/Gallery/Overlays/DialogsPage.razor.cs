using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Overlays;

/// <summary>The Dialogs page: the settings of one dialog, then dialogs opened from code, their variants, sizes and intentions.</summary>
public partial class DialogsPage
{
    [Inject]
    private IStringLocalizer<OverlaysStrings> Text { get; set; } = default!;

    private bool Dismissible { get; set; } = true;

    private bool CloseOnBackdrop { get; set; } = true;

    private bool ShowFooter { get; set; } = true;

    private OmniDialogSize Size { get; set; } = OmniDialogSize.Medium;

    private OmniTone Intent { get; set; } = OmniTone.Neutral;

    private bool Draggable { get; set; }

    private OmniDialogInitialFocus InitialFocus { get; set; } = OmniDialogInitialFocus.CloseButton;

    private IReadOnlyList<OmniOption<OmniDialogInitialFocus>> InitialFocusOptions =>
    [
        new(OmniDialogInitialFocus.CloseButton, Text["DialogsFocusClose"]),
        new(OmniDialogInitialFocus.Panel, Text["DialogsFocusPanel"]),
        new(OmniDialogInitialFocus.FirstFocusable, Text["DialogsFocusField"])
    ];

    private IReadOnlyList<OmniOption<OmniDialogSize>> SizeOptions =>
    [
        new(OmniDialogSize.Small, Text["DialogsSizeSmall"]),
        new(OmniDialogSize.Medium, Text["DialogsSizeMedium"]),
        new(OmniDialogSize.Large, Text["DialogsSizeLarge"]),
        new(OmniDialogSize.ExtraLarge, Text["DialogsSizeExtraLarge"]),
        new(OmniDialogSize.FullWidth, Text["DialogsSizeFullWidth"])
    ];

    private IReadOnlyList<OmniOption<OmniTone>> IntentOptions =>
    [
        new(OmniTone.Neutral, Text["DialogsIntentNeutral"]),
        new(OmniTone.Accent, Text["DialogsIntentAccent"]),
        new(OmniTone.Info, Text["DialogsIntentInfo"]),
        new(OmniTone.Success, Text["DialogsIntentSuccess"]),
        new(OmniTone.Warning, Text["DialogsIntentWarning"]),
        new(OmniTone.Danger, Text["DialogsIntentDanger"])
    ];
}
