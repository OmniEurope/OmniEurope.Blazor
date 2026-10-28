using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Data;

/// <summary>The run grids page: a sorted grid with a tinted column and a striped grid with a sticky header.</summary>
public partial class DataGridRunsPage
{
    [Inject]
    private IStringLocalizer<DataStrings> Text { get; set; } = default!;
}
