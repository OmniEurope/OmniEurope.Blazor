using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ButtonsDemo
{
    [Inject] private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private int Count { get; set; }

    private int Sent { get; set; }

    private bool Saving { get; set; }

    private bool Sending { get; set; }

    private bool Pinned { get; set; }

    private const string CopySample = "# Error detail\n\n- code: `E-42`\n- when: 2026-10-23T09:30:00Z\n";

    private int Copies { get; set; }

    private Task<string> BuildCopySampleAsync() => Task.FromResult($"{CopySample}- computed at the click: {DateTimeOffset.UtcNow:O}\n");

    private void OnCopied(bool accepted)
    {
        if (accepted)
        {
            Copies++;
        }
    }

    private async Task SaveAsync()
    {
        Saving = true;
        await Task.Delay(2000);
        Saving = false;
    }

    private async Task SendAsync()
    {
        Sent++;
        Sending = true;
        await Task.Delay(2000);
        Sending = false;
    }
}
