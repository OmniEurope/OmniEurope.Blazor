using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>A form built from a list of field descriptions: text, number, multi-line text and a flag.</summary>
public partial class DynamicFormExample
{
    private IReadOnlyList<OmniDynamicField> Fields { get; } =
    [
        new("name", "Name") { Required = true },
        new("amount", "Amount", OmniDynamicFieldKind.Number) { Minimum = 0, Step = 0.5m },
        new("comment", "Comment", OmniDynamicFieldKind.MultilineText),
        new("active", "Active", OmniDynamicFieldKind.Boolean)
    ];

    private IReadOnlyDictionary<string, string> Values { get; set; } =
        new Dictionary<string, string> { ["name"] = "Camille", ["amount"] = "2.5" };
}
