using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Actions family of the gallery: its pages, in menu order.</summary>
internal static class ActionsFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyActions", OmniIconName.Play, typeof(ActionsStrings),
    [
        new("buttons", "ButtonsTitle", "ButtonsDescription", OmniIconName.Play,
            ["OmniButton", "OmniCopyButton", "OmniSplitButton", "OmniToggleButton"],
            [
                "Actions/ButtonsExample.razor", "Actions/ButtonsExample.razor.cs",
                "Actions/ButtonSizesExample.razor", "Actions/ButtonSizesExample.razor.cs",
                "Actions/ButtonStatesExample.razor", "Actions/ButtonStatesExample.razor.cs",
                "Actions/ButtonVariantColumnExample.razor", "Actions/ButtonVariantColumnExample.razor.cs",
                "Actions/BusyButtonsExample.razor", "Actions/BusyButtonsExample.razor.cs",
                "Actions/CopyButtonsExample.razor", "Actions/CopyButtonsExample.razor.cs",
                "Actions/ButtonSample.cs"
            ]),
        new("split-buttons", "SplitButtonsTitle", "SplitButtonsDescription", OmniIconName.ChevronDown,
            ["OmniSplitButton", "OmniMenuItem"],
            [
                "Actions/SplitButtonExample.razor", "Actions/SplitButtonExample.razor.cs",
                "Actions/SplitButtonStatesExample.razor", "Actions/SplitButtonStatesExample.razor.cs",
                "Actions/SplitButtonVariantsExample.razor", "Actions/SplitButtonVariantsExample.razor.cs"
            ]),
        new("toggle-buttons", "ToggleButtonsTitle", "ToggleButtonsDescription", OmniIconName.Swap,
            ["OmniToggleButton"],
            [
                "Actions/ToggleButtonExample.razor", "Actions/ToggleButtonExample.razor.cs",
                "Actions/ToggleToolbarExample.razor", "Actions/ToggleToolbarExample.razor.cs",
                "Actions/ToggleStatesExample.razor", "Actions/ToggleStatesExample.razor.cs",
                "Actions/ToggleSizesExample.razor", "Actions/ToggleSizesExample.razor.cs"
            ])
    ]);
}
