using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class FormDemo
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private FormDemoModel Model { get; } = new();

    private IReadOnlyList<OmniDynamicField> DynamicFields { get; set; } = [];

    private IReadOnlyDictionary<string, string> DynamicValues { get; set; } =
        new Dictionary<string, string> { ["name"] = "Camille", ["amount"] = "2.5" };

    /// <summary>The fields keep their keys; only their labels follow the reader's language.</summary>
    protected override void OnInitialized() => DynamicFields =
    [
        new("name", Text["DemoFormDynamicName"]) { Required = true },
        new("amount", Text["DemoFormDynamicAmount"], OmniDynamicFieldKind.Number) { Minimum = 0, Step = 0.5m },
        new("comment", Text["DemoFormDynamicComment"], OmniDynamicFieldKind.MultilineText),
        new("active", Text["DemoFormDynamicActive"], OmniDynamicFieldKind.Boolean)
    ];
}
