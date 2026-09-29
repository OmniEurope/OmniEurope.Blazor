using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class FeedbackDemo
{
    [Inject] private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    [Inject] private OmniLoadingState Loading { get; set; } = null!;

    // The map resolves its labels as resource keys through the showcase localizer.
    private OmniStatusMap<string> DemoStatuses { get; set; } = null!;

    private IReadOnlyList<OmniStatusStripItem> RecentRuns { get; set; } = [];

    // The strip reads only the tone of each status: each run keeps its own label.
    private OmniStatusMap<string> RunTones { get; set; } = null!;

    protected override void OnInitialized()
    {
        DemoStatuses = new OmniStatusMap<string> { Localizer = Text }
            .Add("ready", OmniTone.Success, "DemoFeedbackReady", OmniIconName.CheckCircle)
            .Add("failed", OmniTone.Danger, "DemoFeedbackFailed", OmniIconName.Error);
        RecentRuns =
        [
            new() { Status = "success", Label = Text["DemoFeedbackRunSucceeded", 41] },
            new() { Status = "warning", Label = Text["DemoFeedbackRunWarning", 42] },
            new() { Status = "failure", Label = Text["DemoFeedbackRunFailed", 43] }
        ];
        RunTones = new OmniStatusMap<string> { Localizer = Text }
            .Add("success", OmniTone.Success, "DemoFeedbackSucceeded")
            .Add("warning", OmniTone.Warning, "DemoFeedbackWarning")
            .Add("failure", OmniTone.Danger, "DemoFeedbackFailed");
    }

    /// <summary>Trois secondes de chargement, le temps de voir le remplissage ralentir puis finir.</summary>
    private async Task SimulateLoadAsync()
    {
        using var load = Loading.Track();
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}
