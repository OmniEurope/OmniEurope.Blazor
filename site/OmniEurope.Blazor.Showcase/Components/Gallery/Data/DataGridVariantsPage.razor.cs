using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Data;

/// <summary>The Data grid variants page: the grid sized, capped, edited, grouped and filled in many ways.</summary>
public partial class DataGridVariantsPage
{
    [Inject]
    private IStringLocalizer<DataStrings> Text { get; set; } = default!;
}
