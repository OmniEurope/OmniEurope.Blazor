using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Data;

/// <summary>The Collections page: a board of movable cards and a searchable log, the log's search field and wrapping as settings.</summary>
public partial class CollectionsPage
{
    [Inject]
    private IStringLocalizer<DataStrings> Text { get; set; } = default!;

    private bool ShowSearch { get; set; } = true;

    private bool Wrap { get; set; }
}
