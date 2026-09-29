using OmniEurope.Blazor.Showcase.Demos;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Pages;

public partial class ComponentGallery
{
    [Parameter]
    public string? DemoKey { get; set; }

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    /// <summary>
    /// The entry on show: the first one on the bare gallery address, <see langword="null"/> when the
    /// address names a key the gallery does not have (the page then renders its not-found content).
    /// </summary>
    private DemoDefinition? Current { get; set; } = DemoCatalog.All[0];

    private string? Source { get; set; }

    protected override void OnParametersSet()
    {
        Current = DemoKey is null ? DemoCatalog.All[0] : DemoCatalog.Find(DemoKey);
        Source = Current is null ? null : DemoSource.Read(Current.Component);
    }
}
