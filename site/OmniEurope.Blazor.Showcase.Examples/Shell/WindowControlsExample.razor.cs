using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Shell;

/// <summary>The caption buttons of a borderless window, each switched on or off by a parameter.</summary>
public partial class WindowControlsExample
{
    /// <summary>Whether the minimize-to-tray button is drawn.</summary>
    [Parameter]
    public bool ShowTray { get; set; }

    /// <summary>Whether the minimize button is drawn.</summary>
    [Parameter]
    public bool ShowMinimize { get; set; } = true;

    /// <summary>Whether the maximize or restore button is drawn.</summary>
    [Parameter]
    public bool ShowMaximize { get; set; } = true;

    /// <summary>Whether the close button is drawn.</summary>
    [Parameter]
    public bool ShowClose { get; set; } = true;

    /// <summary>Whether an extra action sits before the caption buttons.</summary>
    [Parameter]
    public bool ShowActions { get; set; }

    private bool Maximized { get; set; }

    private string LastAction { get; set; } = string.Empty;

    // A button is drawn only when its callback has a delegate: a switched-off button gets the empty callback.
    private EventCallback TrayCallback => Callback(ShowTray, () => LastAction = "Minimize to tray");

    private EventCallback MinimizeCallback => Callback(ShowMinimize, () => LastAction = "Minimize");

    private EventCallback MaximizeCallback => Callback(ShowMaximize, ToggleMaximized);

    private EventCallback CloseCallback => Callback(ShowClose, () => LastAction = "Close");

    private EventCallback Callback(bool shown, Action action) => shown ? EventCallback.Factory.Create(this, action) : default;

    /// <summary>The example window only records the state: the maximize button's icon follows Maximized.</summary>
    private void ToggleMaximized()
    {
        Maximized = !Maximized;
        LastAction = Maximized ? "Maximized" : "Restored";
    }
}
