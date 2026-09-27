using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The caption buttons of a borderless desktop window, placed last in an <see cref="OmniHeader"/>: minimize to
/// the tray, minimize, maximize or restore, and close. Each button is square and fills the header's height, the
/// buttons touch, and close takes the top-right corner and turns red on hover like a system caption. A button
/// is drawn only when its callback is set. <see cref="Actions"/> holds extra header buttons (a theme toggle),
/// drawn square at the same height before the caption buttons, with a small gap.
/// </summary>
public partial class OmniWindowControls
{
    [Parameter]
    public EventCallback OnMinimizeToTray { get; set; }

    [Parameter]
    public EventCallback OnMinimize { get; set; }

    [Parameter]
    public EventCallback OnMaximizeRestore { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>Whether the window is maximized: the middle button then offers to restore it.</summary>
    [Parameter]
    public bool IsMaximized { get; set; }

    [Parameter]
    public RenderFragment? Actions { get; set; }

    [Parameter]
    public string AriaLabel { get; set; } = string.Empty;

    [Parameter]
    public string MinimizeToTrayLabel { get; set; } = string.Empty;

    [Parameter]
    public string MinimizeLabel { get; set; } = string.Empty;

    [Parameter]
    public string MaximizeLabel { get; set; } = string.Empty;

    [Parameter]
    public string RestoreLabel { get; set; } = string.Empty;

    [Parameter]
    public string CloseLabel { get; set; } = string.Empty;

    private string EffectiveLabel => Or(AriaLabel, "WindowControlsLabel");
    private string EffectiveMinimizeToTrayLabel => Or(MinimizeToTrayLabel, "WindowMinimizeToTray");
    private string EffectiveMinimizeLabel => Or(MinimizeLabel, "WindowMinimize");
    private string EffectiveMaximizeLabel => IsMaximized ? Or(RestoreLabel, "WindowRestore") : Or(MaximizeLabel, "WindowMaximize");
    private string EffectiveCloseLabel => Or(CloseLabel, "WindowClose");

    private string Or(string value, string key) => string.IsNullOrWhiteSpace(value) ? Localize(key) : value;
}
