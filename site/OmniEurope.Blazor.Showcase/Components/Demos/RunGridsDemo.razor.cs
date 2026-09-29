using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class RunGridsDemo
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private IReadOnlyList<PipelineRun> Recent { get; set; } = [];

    private IReadOnlyList<PipelineRun> History { get; set; } = [];

    private IReadOnlyList<PipelineRun> SortedSelection { get; set; } = [];

    private IReadOnlyList<PipelineRun> StripedSelection { get; set; } = [];

    protected override void OnInitialized()
    {
        Recent =
        [
            Run("#2394", "orders-candidate", "RunsTriggerPush", 125, "success"),
            Run("#2399", "orders-deploy-prod", "RunsTriggerManual", 108, "success"),
            Run("#2392", "orders-deploy-prod", "RunsTriggerManual", 271, "warning"),
            Run("#2400", "billing-candidate", "RunsTriggerPush", 192, "danger")
        ];
        History =
        [
            Run("#2400", "billing-candidate", "RunsTriggerPush", 192, "danger"),
            Run("#2399", "orders-deploy-prod", "RunsTriggerManual", 108, "success"),
            Run("#2398", "catalog-build", "RunsTriggerScheduled", 52, "success"),
            Run("#2397", "billing-candidate", "RunsTriggerPush", 164, "info"),
            Run("#2396", "orders-candidate", "RunsTriggerPush", 121, "success"),
            Run("#2395", "orders-deploy-prod", "RunsTriggerManual", 309, "success"),
            Run("#2394", "orders-candidate", "RunsTriggerPush", 125, "success"),
            Run("#2393", "catalog-build", "RunsTriggerScheduled", 49, "success"),
            Run("#2392", "orders-deploy-prod", "RunsTriggerManual", 271, "warning"),
            Run("#2391", "billing-candidate", "RunsTriggerPush", 207, "danger")
        ];
        SortedSelection = [.. Recent.Where(run => run.Run == "#2394")];
        StripedSelection = [.. History.Where(run => run.Run == "#2395")];
    }

    /// <summary>A run whose trigger and status read in the current culture.</summary>
    private PipelineRun Run(string run, string pipeline, string triggerKey, int seconds, string tone) =>
        new(run, pipeline, Text[triggerKey], seconds, Text[$"RunsStatus{char.ToUpperInvariant(tone[0])}{tone[1..]}"], tone);
}
