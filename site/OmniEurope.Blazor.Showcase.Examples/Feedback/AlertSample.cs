using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>One alert severity with the title, the message and the icon that suit it.</summary>
/// <param name="Severity">The severity of the alert.</param>
/// <param name="Title">The bold line above the message.</param>
/// <param name="Message">The message.</param>
/// <param name="Icon">The icon of the severity.</param>
public sealed record AlertSample(OmniSeverity Severity, string Title, string Message, OmniIconName Icon)
{
    /// <summary>A message long enough to wrap over several lines next to the icon.</summary>
    public const string LongMessage =
        "The nightly import stopped after three thousand rows because one file carried an unknown column. "
        + "The rows already read were kept, the rest will be retried tomorrow night, and the faulty file was moved "
        + "to the quarantine folder so that it can be checked before the next attempt.";

    /// <summary>The four severities, each with a sample that suits its meaning.</summary>
    public static IReadOnlyList<AlertSample> All { get; } =
    [
        new(OmniSeverity.Info, "Information", "The export runs every night at two o'clock.", OmniIconName.Info),
        new(OmniSeverity.Success, "Saved", "Your changes have been saved.", OmniIconName.CheckCircle),
        new(OmniSeverity.Warning, "Careful", "This setting will apply to every page.", OmniIconName.Warning),
        new(OmniSeverity.Danger, "Failure", "The connection to the service failed.", OmniIconName.Error)
    ];

    /// <summary>The sample of a severity, the first one for a value the list does not hold.</summary>
    public static AlertSample Of(OmniSeverity severity) =>
        All.FirstOrDefault(sample => sample.Severity == severity) ?? All[0];
}
