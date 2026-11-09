using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Feedback;

/// <summary>The Country flags page: a few countries with or without a picture template and their code.</summary>
public partial class CountryFlagsPage
{
    [Inject]
    private IStringLocalizer<FeedbackStrings> Text { get; set; } = default!;

    private bool UseTemplate { get; set; } = true;

    private bool ShowCode { get; set; } = true;
}
