using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class LoadingDemo
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    [Inject]
    private OmniLoadingState Loading { get; set; } = default!;

    private OmniLoadingBarMode Mode { get; set; } = OmniLoadingBarMode.Sweep;

    /// <summary>
    /// Three seconds of real tracked loading in the mode asked, the time to see the bar run, reach
    /// the end and fade.
    /// </summary>
    private async Task LoadAsync(OmniLoadingBarMode mode)
    {
        Mode = mode;
        using var load = Loading.Track();
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}
