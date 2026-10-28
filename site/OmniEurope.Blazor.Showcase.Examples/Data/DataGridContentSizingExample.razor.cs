using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>Grids whose columns are sized to their content, loaded in different ways.</summary>
public partial class DataGridContentSizingExample
{
    // The rows of the grid that loads them itself (Load), a moment after its first render, as a server
    // would: the columns are sized once, on these rows, never on the loading row before them.
    private static async Task<OmniDataGridResult<SiteRow>> LoadSitesAsync(OmniDataGridLoadRequest request)
    {
        await Task.Delay(400, request.CancellationToken);
        return new OmniDataGridResult<SiteRow>(SiteData.Sites, SiteData.Sites.Count);
    }
}
