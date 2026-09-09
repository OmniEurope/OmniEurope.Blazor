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
            if (Unmeasured)
            {
                return indicator;
            }

            var bucket = Math.Clamp((int)(Math.Round(State.Value / 5, MidpointRounding.AwayFromZero) * 5), 0, 100);
            return $"{indicator} omni-loading-bar__indicator--{bucket.ToString(CultureInfo.InvariantCulture)}";
        }
    }

    protected override void OnInitialized() => State.Changed += OnStateChanged;

    private void OnStateChanged() => _ = InvokeAsync(StateHasChanged);

    public void Dispose() => State.Changed -= OnStateChanged;
}
