namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class FeedbackDemo
{
    [Inject] private OmniLoadingState Loading { get; set; } = null!;

    /// <summary>Trois secondes de chargement, le temps de voir le remplissage ralentir puis finir.</summary>
    private async Task SimulateLoadAsync()
    {
        using var load = Loading.Track();
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}
