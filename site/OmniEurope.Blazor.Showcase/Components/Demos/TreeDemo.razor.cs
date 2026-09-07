namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class TreeDemo
{
    private IReadOnlyList<string> Selected { get; set; } = ["bru"];

    private List<string> Archives { get; } = [];

    /// <summary>
    /// Stands in for a branch fetched on demand: the tree waits on the task before it draws the
    /// level, so a slow source cannot leave a half-open node behind.
    /// </summary>
    private async Task LoadArchivesAsync(CancellationToken cancellationToken)
    {
        if (Archives.Count > 0)
        {
            return;
        }

        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        Archives.AddRange(["2024", "2025", "2026"]);
    }
}
