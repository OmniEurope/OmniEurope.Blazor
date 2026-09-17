namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class FormDemo
{
    private FormDemoModel Model { get; } = new();

    private static readonly IReadOnlyList<OmniDynamicField> DynamicFields =
    [
        new("name", "Nom") { Required = true },
        new("amount", "Montant", OmniDynamicFieldKind.Number) { Minimum = 0, Step = 0.5m },
        new("comment", "Commentaire", OmniDynamicFieldKind.MultilineText),
        new("active", "Actif", OmniDynamicFieldKind.Boolean)
    ];

    private IReadOnlyDictionary<string, string> DynamicValues { get; set; } =
        new Dictionary<string, string> { ["name"] = "Camille", ["amount"] = "2.5" };
}
