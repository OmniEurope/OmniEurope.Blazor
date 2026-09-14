using System.Globalization;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The steps of a run on one time axis: a row per step with its name, a track spanning the whole run
/// and a bar where the step actually ran, its duration on the right, and the steps that never started
/// named below. Where a list says in which order steps came, this says where the run spent its time
/// and which steps really overlapped.
/// </summary>
public partial class OmniStepTimeline
{
    /// <summary>The steps, in the order the rows show them.</summary>
    [Parameter] public IReadOnlyList<OmniStepTimelineStep> Steps { get; set; } = Array.Empty<OmniStepTimelineStep>();

    /// <summary>
    /// The right edge of a step still running. Null reads the clock at each render; a host that
    /// refreshes a live run, or a test, passes its own.
    /// </summary>
    [Parameter] public DateTimeOffset? Now { get; set; }

    /// <summary>Accessible name of the section; the localized StepTimelineLabel by default.</summary>
    [Parameter] public string Label { get; set; } = string.Empty;

    internal StepTimelineLayout Layout { get; private set; } = StepTimelineLayout.Build([], DateTimeOffset.MinValue);

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("StepTimelineLabel") : Label;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Layout = StepTimelineLayout.Build(Steps, Now ?? DateTimeOffset.UtcNow);
    }

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
