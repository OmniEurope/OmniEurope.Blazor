namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>One run of a pipeline, as the two run grids list it.</summary>
/// <param name="Run">The run number.</param>
/// <param name="Pipeline">The pipeline it ran.</param>
/// <param name="Trigger">What started it.</param>
/// <param name="Seconds">How long it took, a number the grid aligns at the end.</param>
/// <param name="Status">How it ended.</param>
/// <param name="Tone">The intention of that status: success, info, warning or danger.</param>
public sealed record PipelineRun(string Run, string Pipeline, string Trigger, int Seconds, string Status, string Tone);

/// <summary>One run of the compact run grid: a run, its pipeline, its duration and its status.</summary>
/// <param name="Run">The run number.</param>
/// <param name="Pipeline">The pipeline it ran.</param>
/// <param name="Seconds">How long it took, a number the grid aligns at the end.</param>
/// <param name="Status">How it ended.</param>
/// <param name="Tone">The intention of that status: success, info, warning or danger.</param>
public sealed record RunRow(string Run, string Pipeline, int Seconds, string Status, string Tone);

/// <summary>The sample runs of the run grids.</summary>
public static class RunData
{
    /// <summary>The four runs of the sorted grid.</summary>
    public static IReadOnlyList<PipelineRun> Recent { get; } =
    [
        Run("#2394", "orders-candidate", "Push", 125, "success"),
        Run("#2399", "orders-deploy-prod", "Manual", 108, "success"),
        Run("#2392", "orders-deploy-prod", "Manual", 271, "warning"),
        Run("#2400", "billing-candidate", "Push", 192, "danger")
    ];

    /// <summary>The ten runs of the striped grid.</summary>
    public static IReadOnlyList<PipelineRun> History { get; } =
    [
        Run("#2400", "billing-candidate", "Push", 192, "danger"),
        Run("#2399", "orders-deploy-prod", "Manual", 108, "success"),
        Run("#2398", "catalog-build", "Scheduled", 52, "success"),
        Run("#2397", "billing-candidate", "Push", 164, "info"),
        Run("#2396", "orders-candidate", "Push", 121, "success"),
        Run("#2395", "orders-deploy-prod", "Manual", 309, "success"),
        Run("#2394", "orders-candidate", "Push", 125, "success"),
        Run("#2393", "catalog-build", "Scheduled", 49, "success"),
        Run("#2392", "orders-deploy-prod", "Manual", 271, "warning"),
        Run("#2391", "billing-candidate", "Push", 207, "danger")
    ];

    /// <summary>The eight runs of the compact grid.</summary>
    public static IReadOnlyList<RunRow> Compact { get; } =
    [
        CompactRun("#2400", "billing-candidate", 192, "Failed", "danger"),
        CompactRun("#2399", "orders-deploy-prod", 108, "Success", "success"),
        CompactRun("#2398", "catalog-build", 52, "Success", "success"),
        CompactRun("#2397", "billing-candidate", 164, "Running", "info"),
        CompactRun("#2396", "orders-candidate", 121, "Success", "success"),
        CompactRun("#2395", "orders-deploy-prod", 309, "Pending", "warning"),
        CompactRun("#2394", "orders-candidate", 125, "Success", "success"),
        CompactRun("#2393", "catalog-build", 47, "Cancelled", "warning")
    ];

    private static PipelineRun Run(string run, string pipeline, string trigger, int seconds, string tone) =>
        new(run, pipeline, trigger, seconds, StatusOf(tone), tone);

    private static RunRow CompactRun(string run, string pipeline, int seconds, string status, string tone) =>
        new(run, pipeline, seconds, status, tone);

    private static string StatusOf(string tone) => tone switch
    {
        "success" => "Success",
        "warning" => "Cancelled",
        "danger" => "Failed",
        _ => "Running"
    };
}
