using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Shell;

/// <summary>A loading bar under a header, with buttons that start loads of different lengths through the loading state.</summary>
public partial class LoadingBarExample : IDisposable
{
    /// <summary>
    /// The lengths of the demonstration loads. Several, because a real load does not announce its own:
    /// three seconds show only the start of the curve of the filling bar, ten show it slowing down.
    /// </summary>
    private static readonly int[] DemonstrationSeconds = [3, 5, 10];

    /// <summary>A sweep across the bar, or a fill that slows down without finishing while the load lasts.</summary>
    [Parameter]
    public OmniLoadingBarMode Mode { get; set; } = OmniLoadingBarMode.Sweep;

    /// <summary>
    /// A class set on the bar itself, where the colour is redefined: <c>--omni-loading-bar-color</c> is
    /// declared on the bar, so a rule of the host's stylesheet must target the bar to change it.
    /// </summary>
    [Parameter]
    public string? BarClass { get; set; }

    [Inject]
    private OmniLoadingState Loading { get; set; } = default!;

    protected override void OnInitialized() => Loading.Changed += OnLoadingChanged;

    /// <summary>
    /// Runs a load for the time asked. The tracking is disposed in every case, or an interruption would
    /// leave the bar on for good.
    /// </summary>
    private async Task LoadAsync(int seconds)
    {
        using var tracked = Loading.Track();
        await Task.Delay(TimeSpan.FromSeconds(seconds));
    }

    private void OnLoadingChanged() => _ = InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose()
    {
        Loading.Changed -= OnLoadingChanged;
        GC.SuppressFinalize(this);
    }
}
