using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ValidationDemo
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private ValidationDemoModel Model { get; } = new();

    private bool Submitted { get; set; }

    private bool UnsavedChanges { get; set; }

    private AnnotatedValidationDemoModel Annotated { get; } = new();
}
