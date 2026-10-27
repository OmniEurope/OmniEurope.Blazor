using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Overlays family of the gallery: its pages, in menu order.</summary>
internal static class OverlaysFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyOverlays", OmniIconName.PictureInPicture, typeof(OverlaysStrings),
    [
        new("dialogs", "DialogsTitle", "DialogsDescription", OmniIconName.PictureInPicture,
            ["OmniDialog", "OmniComponentsHost", "OmniOverlayService"],
            [
                "Overlays/DialogsExample.razor", "Overlays/DialogsExample.razor.cs",
                "Overlays/ServiceDialogsExample.razor", "Overlays/ServiceDialogsExample.razor.cs",
                "Overlays/DialogVariantsExample.razor", "Overlays/DialogVariantsExample.razor.cs",
                "Overlays/DialogSizesExample.razor", "Overlays/DialogSizesExample.razor.cs",
                "Overlays/DialogIntentsExample.razor", "Overlays/DialogIntentsExample.razor.cs"
            ]),
        new("context-menus", "ContextMenusTitle", "ContextMenusDescription", OmniIconName.Menu,
            ["OmniContextMenu", "OmniMenuItem"],
            [
                "Overlays/ContextMenusExample.razor", "Overlays/ContextMenusExample.razor.cs",
                "Overlays/ContextMenuRowsExample.razor", "Overlays/ContextMenuRowsExample.razor.cs"
            ]),
        new("overflow-menu", "OverflowMenuTitle", "OverflowMenuDescription", OmniIconName.Menu,
            ["OmniOverflowMenu", "OmniMenuItem"],
            [
                "Overlays/OverflowMenuExample.razor", "Overlays/OverflowMenuExample.razor.cs"
            ]),
        new("popover", "PopoverTitle", "PopoverDescription", OmniIconName.Chat,
            ["OmniPopover"],
            [
                "Overlays/PopoverExample.razor", "Overlays/PopoverExample.razor.cs"
            ])
    ]);
}
