using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ShellDemo
{
    [Inject] private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;
    private bool? SplashFound { get; set; }
    /// <summary>Resource keys of the paragraphs that make the reading column scroll.</summary>
    private static readonly string[] ScrollParagraphs =
    [
        "DemoShellScroll1",
        "DemoShellScroll2",
        "DemoShellScroll3",
        "DemoShellScroll4",
        "DemoShellScroll5"
    ];

    private bool SidebarOpen { get; set; } = true;
    private bool RightSidebarOpen { get; set; } = true;

    private OmniAppearance Appearance { get; set; } = OmniAppearance.Light;

    private void ToggleAppearance() =>
        Appearance = Appearance == OmniAppearance.Dark ? OmniAppearance.Light : OmniAppearance.Dark;

    private bool RailOpen { get; set; }
}
