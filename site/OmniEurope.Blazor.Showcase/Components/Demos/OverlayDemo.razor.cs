using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// What is left of the old overlays demo once dialogs, context menus, the overflow menu and the popover
/// moved to their gallery pages: the split button, the account menu, inline notifications and the
/// notification stack, which belong to later pages.
/// </summary>
public partial class OverlayDemo : IDisposable
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private string? Last { get; set; }

    private OmniOverlayService Overlays { get; } = new();

    /// <summary>The start of the night job, written the way the reader's culture writes a time.</summary>
    private static string NightJobStart => new TimeOnly(23, 0).ToString("t", CultureInfo.CurrentCulture);

    private void NotifyShort() => Overlays.Notify(Text["DemoOverlayNoticeQuota"], OmniSeverity.Warning);

    private void NotifyLong() => Overlays.Notify(Text["DemoOverlayNoticeLong"], OmniSeverity.Success, Text["DemoOverlayNoticeDoneTitle"]);

    public void Dispose()
    {
        Overlays.Dispose();
        GC.SuppressFinalize(this);
    }
}
