using OmniEurope.Blazor.Showcase.Demos;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Pages;

public partial class ComponentGallery
{
    [Parameter]
    public string? DemoKey { get; set; }

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private DemoDefinition Current { get; set; } = DemoCatalog.All[0];

    private string? Source { get; set; }

    protected override void OnParametersSet()
    {
        Current = DemoCatalog.Resolve(DemoKey);
        Source = DemoSource.Read(Current.Component);
    }
}
