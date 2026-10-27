using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Feedback;

/// <summary>The Progress page: the settings of one gauge, a simulation, then every colour, step and status.</summary>
public partial class ProgressPage : IDisposable
{
    /// <summary>One step every 80 ms: the whole run lasts eight seconds, long enough to follow the animation.</summary>
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(80);

    private CancellationTokenSource? _run;

    [Inject]
    private IStringLocalizer<FeedbackStrings> Text { get; set; } = default!;

    private OmniProgressShape Shape { get; set; } = OmniProgressShape.Linear;

    private OmniTone Tone { get; set; } = OmniTone.Accent;

    private double Value { get; set; } = 40;

    private bool Indeterminate { get; set; }

    private bool ShowValue { get; set; } = true;

    private bool CustomValueText { get; set; }

    private string Label { get; set; } = string.Empty;

    private bool Running => _run is not null;

    private IReadOnlyList<OmniOption<OmniProgressShape>> ShapeOptions =>
    [
        new(OmniProgressShape.Linear, Text["ProgressShapeLinear"]),
        new(OmniProgressShape.Circular, Text["ProgressShapeCircular"])
    ];

    private IReadOnlyList<OmniOption<OmniTone>> ToneOptions =>
    [
        new(OmniTone.Accent, Text["ProgressToneAccent"]),
        new(OmniTone.Neutral, Text["ProgressToneNeutral"]),
        new(OmniTone.Info, Text["ProgressToneInfo"]),
        new(OmniTone.Success, Text["ProgressToneSuccess"]),
        new(OmniTone.Warning, Text["ProgressToneWarning"]),
        new(OmniTone.Danger, Text["ProgressToneDanger"])
    ];

    /// <summary>Runs the value from zero to one hundred; stopping, restarting or leaving the page cuts the loop.</summary>
    private async Task RunAsync()
    {
        Stop();
        var run = new CancellationTokenSource();
        _run = run;
        Indeterminate = false;
        Value = 0;
        try
        {
            while (Value < 100)
            {
                await Task.Delay(TickInterval, run.Token);
                Value = Math.Min(100, Value + 1);
                StateHasChanged();
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            if (ReferenceEquals(_run, run))
            {
                _run = null;
            }

            run.Dispose();
        }
    }

    private void Stop()
    {
        _run?.Cancel();
        _run = null;
    }

    /// <inheritdoc />
    public void Dispose() => Stop();
}
