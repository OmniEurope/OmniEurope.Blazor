using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A shell whose side menu pushes the content and whose appearance the header button toggles.</summary>
public partial class SidebarShellExample
{
    private bool SidebarOpen { get; set; } = true;

    private OmniAppearance Appearance { get; set; } = OmniAppearance.Light;

    private void ToggleAppearance() =>
        Appearance = Appearance == OmniAppearance.Dark ? OmniAppearance.Light : OmniAppearance.Dark;
}
