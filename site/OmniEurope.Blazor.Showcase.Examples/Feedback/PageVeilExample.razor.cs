using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>The page veil waiting for a moment over the content or the whole window, at the opacity chosen, with the logo given as markup, as an image address, animated by the application, or not at all.</summary>
public partial class PageVeilExample
{
    private enum VeilLogo
    {
        None,
        Markup,
        Address,
        Animated
    }

    private bool Waiting { get; set; }

    private VeilLogo Current { get; set; }

    private OmniPageVeilScope Scope { get; set; }

    private double Opacity { get; set; } = 85;

    /// <summary>
    /// A wait of the given length with the logo asked: the veil exists for exactly that long, and shows
    /// only if the wait passes the 100 ms of its delay. The flag is cleared in every case, or an
    /// interruption would leave the page veiled.
    /// </summary>
    private async Task WaitAsync(VeilLogo logo, TimeSpan duration, OmniPageVeilScope scope = OmniPageVeilScope.Content)
    {
        Current = logo;
        Scope = scope;
        Waiting = true;
        try
        {
            await Task.Delay(duration);
        }
        finally
        {
            Waiting = false;
        }
    }
}
