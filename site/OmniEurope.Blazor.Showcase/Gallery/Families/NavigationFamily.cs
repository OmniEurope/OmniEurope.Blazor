using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Navigation family of the gallery: its pages, in menu order.</summary>
internal static class NavigationFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyNavigation", OmniIconName.Path, typeof(NavigationStrings),
    [
        new("tabs", "TabsTitle", "TabsDescription", OmniIconName.Layers,
            ["OmniTabs", "OmniTabsItem", "OmniSettingsSection", "OmniSettingsTile", "OmniSettingsDangerZone", "OmniSettingsDangerAction"],
            [
                "Navigation/TabsExample.razor", "Navigation/TabsExample.razor.cs",
                "Navigation/TabsPlainExample.razor", "Navigation/TabsPlainExample.razor.cs",
                "Navigation/TabsOverflowExample.razor", "Navigation/TabsOverflowExample.razor.cs",
                "Navigation/SettingsTabsExample.razor", "Navigation/SettingsTabsExample.razor.cs",
                "Navigation/SettingsTabsModel.cs"
            ]),
        new("breadcrumbs-links", "BreadcrumbsTitle", "BreadcrumbsDescription", OmniIconName.OpenExternal,
            ["OmniBreadcrumb", "OmniBreadcrumbItem", "OmniLink"],
            [
                "Navigation/BreadcrumbExample.razor", "Navigation/BreadcrumbExample.razor.cs",
                "Navigation/LinksExample.razor", "Navigation/LinksExample.razor.cs",
                "Navigation/BreadcrumbGalleryExample.razor", "Navigation/BreadcrumbGalleryExample.razor.cs"
            ]),
        new("steps", "StepsTitle", "StepsDescription", OmniIconName.NumberedList,
            ["OmniSteps", "OmniStepsItem"],
            [
                "Navigation/StepsExample.razor", "Navigation/StepsExample.razor.cs",
                "Navigation/StepsGalleryExample.razor", "Navigation/StepsGalleryExample.razor.cs"
            ]),
        new("profile-menu", "ProfileMenuTitle", "ProfileMenuDescription", OmniIconName.User,
            ["OmniProfileMenu", "OmniMenuItem"],
            [
                "Navigation/ProfileMenuExample.razor", "Navigation/ProfileMenuExample.razor.cs",
                "Navigation/ProfileMenuGalleryExample.razor", "Navigation/ProfileMenuGalleryExample.razor.cs"
            ]),
        new("app-menu", "AppMenuTitle", "AppMenuDescription", OmniIconName.Sliders,
            ["OmniAppMenu"],
            [
                "Navigation/AppMenuExample.razor", "Navigation/AppMenuExample.razor.cs",
                "Navigation/AppMenuLanguages.cs"
            ])
    ]);
}
