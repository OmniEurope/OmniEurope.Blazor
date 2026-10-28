using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The caption buttons of a borderless desktop window, placed last in an <see cref="OmniHeader"/>: minimize to
/// the tray, minimize, maximize or restore, and close. Each button is 2.875rem wide, as a system caption, and fills the header's height; the
/// buttons touch, and close takes the top-right corner and turns red on hover like a system caption. A button
/// is drawn only when its callback is set. <see cref="ActionsContent"/> holds extra header buttons (a theme toggle),
/// drawn at the same width and height before the caption buttons, with a small gap.
/// </summary>
public partial class OmniWindowControls
{
    /// <summary>Raised when the minimize-to-tray button is clicked; the button is drawn only when this is set.</summary>
    [Parameter]
    public EventCallback OnMinimizeToTray { get; set; }

    /// <summary>Raised when the minimize button is clicked; the button is drawn only when this is set.</summary>
    [Parameter]
    public EventCallback OnMinimize { get; set; }

    /// <summary>
    /// Raised when the maximize or restore button is clicked; the host flips the window and
    /// <see cref="Maximized"/>. The button is drawn only when this is set.
    /// </summary>
    [Parameter]
    public EventCallback OnMaximizeRestore { get; set; }

    /// <summary>Raised when the close button is clicked; the button is drawn only when this is set.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>Whether the window is maximized: the middle button then offers to restore it.</summary>
    [Parameter]
    public bool Maximized { get; set; }

    /// <summary>Extra header buttons, such as a theme toggle, drawn at the caption width before the caption buttons; none by default.</summary>
    [Parameter]
    public RenderFragment? ActionsContent { get; set; }

    /// <summary>Accessible name of the group of buttons. Null, the default, is the localized "Window controls".</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Accessible name and tooltip of the minimize-to-tray button. Null, the default, is the localized text.</summary>
    [Parameter]
    public string? MinimizeToTrayLabel { get; set; }

    /// <summary>Accessible name and tooltip of the minimize button. Null, the default, is the localized text.</summary>
    [Parameter]
    public string? MinimizeLabel { get; set; }

    /// <summary>Accessible name and tooltip of the maximize button. Null, the default, is the localized text.</summary>
    [Parameter]
    public string? MaximizeLabel { get; set; }

    /// <summary>Accessible name and tooltip of the restore button, shown while <see cref="Maximized"/>. Null, the default, is the localized text.</summary>
    [Parameter]
    public string? RestoreLabel { get; set; }

    /// <summary>Accessible name and tooltip of the close button. Null, the default, is the localized text.</summary>
    [Parameter]
    public string? CloseLabel { get; set; }

    private string EffectiveLabel => LocalizeOr(Label, "WindowControlsLabel");
    private string EffectiveMinimizeToTrayLabel => LocalizeOr(MinimizeToTrayLabel, "WindowMinimizeToTray");
    private string EffectiveMinimizeLabel => LocalizeOr(MinimizeLabel, "WindowMinimize");
    private string EffectiveMaximizeLabel => Maximized ? LocalizeOr(RestoreLabel, "WindowRestore") : LocalizeOr(MaximizeLabel, "WindowMaximize");
    private string EffectiveCloseLabel => LocalizeOr(CloseLabel, "WindowClose");
}
