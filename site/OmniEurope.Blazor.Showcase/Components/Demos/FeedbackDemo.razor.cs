namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class FeedbackDemo
{
    private static readonly OmniStatusMap<string> DemoStatuses = new()
    {
        { "ready", OmniTone.Success, "Prêt", OmniIconName.CheckCircle },
        { "failed", OmniTone.Danger, "Échec", OmniIconName.Error }
    };

    private static readonly IReadOnlyList<OmniStatusStripItem> RecentRuns =
    [
        new() { Status = "success", Label = "Exécution 41 réussie" },
        new() { Status = "warning", Label = "Exécution 42 avec avertissement" },
        new() { Status = "failure", Label = "Exécution 43 échouée" }
    ];

    // The strip reads only the tone of each status: each run keeps its own label.
    private static readonly OmniStatusMap<string> RunTones = new()
    {
        { "success", OmniTone.Success, "Réussie" },
        { "warning", OmniTone.Warning, "Avertissement" },
        { "failure", OmniTone.Danger, "Échec" }
    };

    [Inject] private OmniLoadingState Loading { get; set; } = null!;

    /// <summary>Trois secondes de chargement, le temps de voir le remplissage ralentir puis finir.</summary>
    private async Task SimulateLoadAsync()
    {
        using var load = Loading.Track();
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}
