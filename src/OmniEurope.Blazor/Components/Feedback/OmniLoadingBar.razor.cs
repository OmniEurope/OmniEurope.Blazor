using System.Globalization;

namespace OmniEurope.Blazor.Components;

public partial class OmniLoadingBar
{
    /// <summary>How the bar reports a load. Sweep by default, which is what a page load looks like.</summary>
    [Parameter]
    public OmniLoadingBarMode Mode { get; set; } = OmniLoadingBarMode.Sweep;

    [Parameter]
    public string Label { get; set; } = string.Empty;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("LoadingLabel") : Label;

    /// <summary>
    /// A load nobody reported a figure for. The sweep can still run on the animation alone, and the
    /// continuous bar has its own filling curve, so neither needs a number to be honest.
    /// </summary>
    private bool Unmeasured => State.Unmeasured;

    private string? ValueNow => State.Loading && !Unmeasured
        ? State.Value.ToString("0.##", CultureInfo.InvariantCulture)
        : null;

    /// <summary>
    /// A reported figure is drawn at a five-percent step, the same buckets the progress bar uses:
    /// the width belongs in the stylesheet, not in an inline style the content policy would reject.
    /// </summary>
    private string IndicatorClass
    {
        get
        {
            var indicator = "omni-loading-bar__indicator";
            if (Unmeasured || _finishing)
            {
                return indicator;
            }

            var bucket = Math.Clamp((int)(Math.Round(State.Value / 5, MidpointRounding.AwayFromZero) * 5), 0, 100);
            return $"{indicator} omni-loading-bar__indicator--{bucket.ToString(CultureInfo.InvariantCulture)}";
        }
    }

    /// <summary>
    /// How long the bar stays once the load is over: the 700 ms it takes to reach the end, which an
    /// instant jump made look like no ending at all, then the 250 ms of its fade. Kept in step with the
    /// stylesheet's omni-loading-bar--done timings.
    /// </summary>
    internal static readonly TimeSpan FinishDuration = TimeSpan.FromMilliseconds(1000);

    private bool _wasLoading;
    private bool _finishing;
    private CancellationTokenSource? _finish;

    /// <summary>
    /// The bar is drawn while a load runs and for the short finish after it. Removing it the moment
    /// the load ended cut a filling bar wherever it had got to, two thirds of the way on a short
    /// load, which read as an interrupted load rather than a finished one.
    /// </summary>
    private bool Visible => State.Loading || _finishing;

    protected override void OnInitialized() => State.Changed += OnStateChanged;

    private void OnStateChanged() => _ = InvokeAsync(HandleStateChangedAsync);

    private async Task HandleStateChangedAsync()
    {
        var loading = State.Loading;
        var justEnded = _wasLoading && !loading;
        _wasLoading = loading;

        if (!justEnded)
        {
            // A load starting during a finish takes the bar back rather than letting it fade.
            if (loading)
            {
                CancelFinish();
            }

            StateHasChanged();
            return;
        }

        CancelFinish();
        _finishing = true;
        var finish = new CancellationTokenSource();
        _finish = finish;
        StateHasChanged();

        try
        {
            await Task.Delay(FinishDuration, finish.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        _finishing = false;
        StateHasChanged();
    }

    private void CancelFinish()
    {
        _finishing = false;
        _finish?.Cancel();
        _finish?.Dispose();
        _finish = null;
    }

    public void Dispose()
    {
        State.Changed -= OnStateChanged;
        CancelFinish();
    }
}
