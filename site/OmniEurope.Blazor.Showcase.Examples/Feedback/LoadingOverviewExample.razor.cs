using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>The waiting states together: a loading bar following a tracked load, busy buttons, indeterminate and known progress, and a skeleton.</summary>
public partial class LoadingOverviewExample : IDisposable
{
    [Inject]
    private OmniLoadingState Loading { get; set; } = default!;

    private OmniLoadingBarMode Mode { get; set; } = OmniLoadingBarMode.Sweep;

    /// <inheritdoc />
    protected override void OnInitialized() => Loading.Changed += OnLoadingChanged;

    /// <summary>
    /// Three seconds of real tracked loading in the mode asked, the time to see the bar run, reach
    /// the end and fade. The tracking is disposed in every case, or an interruption would leave the bar on.
    /// </summary>
    private async Task LoadAsync(OmniLoadingBarMode mode)
    {
        Mode = mode;
        using var tracked = Loading.Track();
        await Task.Delay(TimeSpan.FromSeconds(3));
    }

    private void OnLoadingChanged() => _ = InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose()
    {
        Loading.Changed -= OnLoadingChanged;
        GC.SuppressFinalize(this);
    }
}
