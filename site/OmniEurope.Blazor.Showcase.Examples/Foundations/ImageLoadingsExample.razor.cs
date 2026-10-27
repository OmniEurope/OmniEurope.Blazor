using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>The eager and the lazy loading modes side by side.</summary>
public partial class ImageLoadingsExample
{
    private static readonly OmniImageLoading[] Loadings = Enum.GetValues<OmniImageLoading>();

    private static string HintOf(OmniImageLoading loading) => loading == OmniImageLoading.Eager
        ? "Fetched straight away, even out of sight."
        : "Fetched when it nears the visible area. The default.";
}
