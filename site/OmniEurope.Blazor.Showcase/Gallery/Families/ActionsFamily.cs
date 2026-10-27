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
                "Actions/BusyButtonsExample.razor", "Actions/BusyButtonsExample.razor.cs",
                "Actions/CopyButtonsExample.razor", "Actions/CopyButtonsExample.razor.cs",
                "Actions/ButtonSample.cs"
            ])
    ]);
}
