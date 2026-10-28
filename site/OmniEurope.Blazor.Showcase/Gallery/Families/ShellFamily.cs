using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Shell family of the gallery: its pages, in menu order.</summary>
internal static class ShellFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyShell", OmniIconName.Sidebar, typeof(ShellStrings),
    [
        new("sidebar", "SidebarTitle", "SidebarDescription", OmniIconName.Sidebar,
            ["OmniSidebar", "OmniSidebarToggle", "OmniPanelMenu", "OmniPanelMenuItem"],
            [
                "Shell/SidebarExample.razor", "Shell/SidebarExample.razor.cs"
            ]),
        new("window-controls", "WindowControlsTitle", "WindowControlsDescription", OmniIconName.Monitor,
            ["OmniWindowControls"],
            [
                "Shell/WindowControlsExample.razor", "Shell/WindowControlsExample.razor.cs"
            ]),
        new("loading-bar", "LoadingBarTitle", "LoadingBarDescription", OmniIconName.Hourglass,
            ["OmniLoadingBar"],
            [
                "Shell/LoadingBarExample.razor", "Shell/LoadingBarExample.razor.cs"
            ]),
        new("notifications", "NotificationsTitle", "NotificationsDescription", OmniIconName.Bell,
            ["OmniNotification", "OmniComponentsHost"],
            [
                "Shell/NotificationsExample.razor", "Shell/NotificationsExample.razor.cs",
                "Shell/NotificationCardsExample.razor", "Shell/NotificationCardsExample.razor.cs"
            ]),
        new("tooltips", "TooltipsTitle", "TooltipsDescription", OmniIconName.Chat,
            ["OmniTooltip", "OmniTitleTooltips"],
            [
                "Shell/TooltipsExample.razor", "Shell/TooltipsExample.razor.cs",
                "Shell/TooltipVariantsExample.razor", "Shell/TooltipVariantsExample.razor.cs",
                "Shell/TitleTooltipExample.razor", "Shell/TitleTooltipExample.razor.cs"
            ])
    ]);
}
