namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ButtonsDemo
{
    private int Count { get; set; }

    private int Sent { get; set; }

    private bool Saving { get; set; }

    private bool Sending { get; set; }

    private bool Pinned { get; set; }

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
