using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class NoticesDemo
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private bool ShowDone { get; set; } = true;

    private bool ShowRefused { get; set; } = true;

    private bool UnlistOpen { get; set; }

    private bool UnlistAlpha { get; set; }

    private bool RerunOpen { get; set; }

    private bool RerunNotify { get; set; }

    private string? Last { get; set; }

    /// <summary>Closes whichever dialog is open, recording the action taken, or nothing on Cancel.</summary>
    private void Close(string? action)
    {
        UnlistOpen = false;
        RerunOpen = false;
        if (action is not null)
        {
            Last = action;
        }
    }
}
