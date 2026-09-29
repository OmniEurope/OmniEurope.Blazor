using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ChoiceDemo
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private int? Rating { get; set; } = 3;
    private int? ReadonlyRating { get; set; } = 3;

    private IReadOnlyList<OmniOption<string>> Shipping { get; set; } = [];

    private IReadOnlyList<OmniOption<string>> Extras { get; set; } = [];

    private IReadOnlyList<OmniOption<string>> Periods { get; set; } = [];

    private string Method { get; set; } = "standard";

    private IReadOnlyList<string> Selected { get; set; } = ["suivi"];

    private string Period { get; set; } = "mois";

    private bool Pinned { get; set; }

    private bool? Partial { get; set; }

    private string Platform { get; set; } = "linux";

    private bool Runner { get; set; } = true;

    private IReadOnlyList<OmniOption<string>> Features { get; set; } = [];

    private IReadOnlyList<string> AgentFeatures { get; set; } = ["cache"];

    /// <summary>The options keep their keys; only the texts follow the reader's language.</summary>
    protected override void OnInitialized()
    {
        Shipping =
        [
            new("standard", Text["DemoChoiceShippingStandard"]),
            new("express", Text["DemoChoiceShippingExpress"]),
            new("retrait", Text["DemoChoiceShippingPickup"], Disabled: true)
        ];
        Extras =
        [
            new("accuse", Text["DemoChoiceExtraReceipt"]),
            new("copie", Text["DemoChoiceExtraCertifiedCopy"]),
            new("suivi", Text["DemoChoiceExtraTracking"])
        ];
        Periods =
        [
            new("jour", Text["DemoChoicePeriodDay"]),
            new("mois", Text["DemoChoicePeriodMonth"]),
            new("annee", Text["DemoChoicePeriodYear"])
        ];
        Features =
        [
            new("cache", Text["DemoChoiceFeatureCache"]),
            new("artefacts", Text["DemoChoiceFeatureArtifacts"]),
            new("gpu", Text["DemoChoiceFeatureGpu"], Disabled: true)
        ];
    }
}
