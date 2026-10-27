using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>Status badges at two widths and status strips in dots and in segments.</summary>
public partial class StatusExample
{
    private OmniStatusMap<string> BadgeMap { get; } = new OmniStatusMap<string>()
        .Add("ready", OmniTone.Success, "Ready", OmniIconName.CheckCircle)
        .Add("failed", OmniTone.Danger, "Failed", OmniIconName.Error);

    private OmniStatusMap<string> RunMap { get; } = new OmniStatusMap<string>()
        .Add("success", OmniTone.Success, "Succeeded")
        .Add("warning", OmniTone.Warning, "Warning")
        .Add("failure", OmniTone.Danger, "Failed");

    private IReadOnlyList<OmniStatusStripItem> Runs { get; } =
    [
        new() { Status = "success", Label = "Run 41 succeeded" },
        new() { Status = "warning", Label = "Run 42 finished with a warning" },
        new() { Status = "failure", Label = "Run 43 failed" }
    ];
}
