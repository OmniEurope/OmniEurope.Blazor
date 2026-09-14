using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>One bar of an <see cref="OmniStepTimeline"/>, as percentages of the run.</summary>
internal sealed record StepTimelineBar(OmniStepTimelineStep Step, double OffsetPercent, double WidthPercent, TimeSpan Offset, TimeSpan Duration);

/// <summary>The bars of a run, the span they are measured against, and the steps that never started.</summary>
internal sealed record StepTimelineLayout(
    IReadOnlyList<StepTimelineBar> Bars,
    TimeSpan Total,
    IReadOnlyList<OmniStepTimelineStep> NeverStarted)
{
    /// <summary>
    /// A bar narrower than this vanishes, so a step that took a sliver of the run is drawn at this
    /// width and reads as brief, which is true, rather than as absent, which is not.
    /// </summary>
    internal const double MinimumWidthPercent = 0.6;

    /// <summary>
    /// Places every started step on one axis that runs from the earliest start to the latest end, a
    /// step still running ending at <paramref name="now"/>. Pure, so every rule is tested without a
    /// render.
    /// </summary>
    internal static StepTimelineLayout Build(IReadOnlyList<OmniStepTimelineStep> steps, DateTimeOffset now)
    {
        var started = steps.Where(step => step.StartedAt.HasValue).ToArray();
        var neverStarted = steps.Where(step => !step.StartedAt.HasValue).ToArray();
        if (started.Length == 0)
        {
            return new StepTimelineLayout([], TimeSpan.Zero, neverStarted);
        }

        var start = started.Min(step => step.StartedAt!.Value);
        var end = started.Select(step => EndOf(step, now)).Append(start).Max();
        var total = end - start;
        var bars = new List<StepTimelineBar>(started.Length);
        foreach (var step in started)
        {
            var offset = step.StartedAt!.Value - start;
            // Clocks that disagree can put an end before its own start: the step stays a brief bar
            // rather than a negative width that draws nothing.
            var duration = EndOf(step, now) - step.StartedAt.Value;
            if (duration < TimeSpan.Zero)
            {
                duration = TimeSpan.Zero;
            }

            double left;
            double width;
            if (total <= TimeSpan.Zero)
            {
                // Every step started at the very instant the run did: no scale to divide by.
                (left, width) = (0, 100);
            }
            else
            {
                left = Math.Clamp(offset / total * 100, 0, 100);
                width = Math.Max(duration / total * 100, MinimumWidthPercent);
                if (left + width > 100)
                {
                    left = Math.Max(0, 100 - width);
                }
            }

            bars.Add(new StepTimelineBar(step, left, width, offset, duration));
        }

        return new StepTimelineLayout(bars, total, neverStarted);
    }

    /// <summary>A duration the way a run log reads: 42s, 3m05, 1h20.</summary>
    internal static string Format(TimeSpan span) =>
        span.TotalHours >= 1
            ? $"{(int)span.TotalHours}h{span.Minutes:D2}"
            : span.TotalMinutes >= 1
                ? $"{(int)span.TotalMinutes}m{span.Seconds:D2}"
                : $"{Math.Max(0, (int)span.TotalSeconds)}s";

    private static DateTimeOffset EndOf(OmniStepTimelineStep step, DateTimeOffset now) =>
        step.CompletedAt ?? (step.Status == OmniStepTimelineStatus.Running ? now : step.StartedAt!.Value);
}
