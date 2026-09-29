using System.Globalization;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The steps of a run on one time axis: a row per step with its name, a track spanning the whole run
/// and a bar where the step actually ran, its duration on the right, and the steps that never started
/// named below. Where a list says in which order steps came, this says where the run spent its time
/// and which steps really overlapped.
/// </summary>
/// <remarks>
/// A step still running reaches the current time of the component clock (the host's registered
/// <see cref="TimeProvider"/>, the system clock otherwise). The clock is read each time the
/// parameters are set, which a parent re-render does: a host showing a live run re-renders it on its
/// own refresh; the timeline starts no timer of its own.
/// </remarks>
public partial class OmniStepTimeline
{
    /// <summary>The steps, in the order the rows show them.</summary>
    [Parameter] public IReadOnlyList<OmniStepTimelineStep> Steps { get; set; } = Array.Empty<OmniStepTimelineStep>();

    /// <summary>Accessible name of the section; null (the default) takes the localized "Run steps".</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Columns of values to the right of the durations, aligned from row to row; a column no drawn
    /// step has a value for is left out. Empty by default, which draws the rows as before.
    /// </summary>
    [Parameter] public IReadOnlyList<OmniStepTimelineColumn> Columns { get; set; } = Array.Empty<OmniStepTimelineColumn>();

    internal StepTimelineLayout Layout { get; private set; } = StepTimelineLayout.Build([], DateTimeOffset.MinValue);

    internal IReadOnlyList<OmniStepTimelineColumn> VisibleColumns { get; private set; } = Array.Empty<OmniStepTimelineColumn>();

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("StepTimelineLabel") : Label;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Layout = StepTimelineLayout.Build(Steps, Clock.GetUtcNow());
        VisibleColumns = [.. (Columns ?? []).Where(column => Layout.Bars.Any(bar => !string.IsNullOrEmpty(column.Value(bar.Step))))];
    }

    private string DescribeProgress(StepTimelineProgress progress) => progress.Overrun > 0
        ? Localize("StepTimelineOverrun")
        : Localize("StepTimelineExpected", Math.Round(progress.Fill * 100).ToString(CultureInfo.CurrentCulture));

    private static string RowCss(OmniStepTimelineStep step) => CssClassBuilder.Combine(
    [
        "omni-step-timeline__row",
        "omni-step-timeline__step",
        $"omni-step-timeline__step--{step.Status.ToString().ToLowerInvariant()}",
        step.Secondary ? "omni-step-timeline__step--secondary" : null
    ]);

    private string Describe(StepTimelineBar bar) =>
        Localize("StepTimelineStepDescription", Localize($"StepTimelineStatus{bar.Step.Status}"), StepTimelineLayout.Format(bar.Offset));

    private static string Percent(double value) => value.ToString("0.###", CultureInfo.InvariantCulture) + "%";
}
