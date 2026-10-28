using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Data;

/// <summary>The advanced data grid page: selection, grouping, editing, frozen columns and the filter controls.</summary>
public partial class DataGridAdvancedPage
{
    [Inject]
    private IStringLocalizer<DataStrings> Text { get; set; } = default!;
}
