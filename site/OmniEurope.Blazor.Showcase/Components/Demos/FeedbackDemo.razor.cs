namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class FeedbackDemo
{
    private static readonly OmniStatusMap<string> DemoStatuses = new()
    {
        { "ready", OmniBadgeVariant.Success, "Prêt", OmniIconName.CheckCircle },
        { "failed", OmniBadgeVariant.Danger, "Échec", OmniIconName.Error }
    };

    private static readonly IReadOnlyList<OmniStatusStripItem> RecentRuns =
    [
        new() { Status = "success", Label = "Exécution 41 réussie" },
        new() { Status = "warning", Label = "Exécution 42 avec avertissement" },
        new() { Status = "failure", Label = "Exécution 43 échouée" }
    ];

    private static readonly IReadOnlyDictionary<string, OmniBadgeVariant> RunTones =
        new Dictionary<string, OmniBadgeVariant>
        {
            ["success"] = OmniBadgeVariant.Success,
            ["warning"] = OmniBadgeVariant.Warning,
            ["failure"] = OmniBadgeVariant.Danger
        };

    [Inject] private OmniLoadingState Loading { get; set; } = null!;

    /// <summary>Trois secondes de chargement, le temps de voir le remplissage ralentir puis finir.</summary>
    private async Task SimulateLoadAsync()
    {
        using var load = Loading.Track();
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}
