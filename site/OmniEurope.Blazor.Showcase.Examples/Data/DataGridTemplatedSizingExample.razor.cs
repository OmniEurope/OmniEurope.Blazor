namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A grid whose rows arrive after its first render, every column drawn by a template.</summary>
public partial class DataGridTemplatedSizingExample
{
    // The rows of the grid whose every column has a template: empty until the button loads them, so the
    // columns, first sized to their headers, must be measured again on the cells that arrive.
    private IReadOnlyList<SiteRow> TemplatedSites { get; set; } = [];

    private bool TemplatedSitesLoading { get; set; }

    // A load as an application does it: the grid busy, the rows arriving a moment later.
    private async Task LoadTemplatedSitesAsync()
    {
        TemplatedSitesLoading = true;
        await Task.Delay(300);
        TemplatedSites = SiteData.Sites;
        TemplatedSitesLoading = false;
    }
}
