namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ChoiceDemo
{
    private static readonly IReadOnlyList<OmniOption<string>> Shipping =
    [
        new("standard", "Standard"),
        new("express", "Express"),
        new("retrait", "Retrait en agence", Disabled: true)
    ];

    private static readonly IReadOnlyList<OmniOption<string>> Extras =
    [
        new("accuse", "Accusé de réception"),
        new("copie", "Copie certifiée"),
        new("suivi", "Suivi par courriel")
    ];

    private static readonly IReadOnlyList<OmniOption<string>> Periods =
    [
        new("jour", "Jour"),
        new("mois", "Mois"),
        new("annee", "Année")
    ];

    private string Method { get; set; } = "standard";

    private IReadOnlyList<string> Selected { get; set; } = ["suivi"];

    private string Period { get; set; } = "mois";

    private bool Pinned { get; set; }

    private bool? Partial { get; set; }
}
