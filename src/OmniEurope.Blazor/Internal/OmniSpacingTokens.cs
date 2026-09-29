using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The suffix a spacing takes in a modifier class, the same as its token in the stylesheet
/// (<c>--omni-space-xs</c> to <c>--omni-space-xl</c>): <c>omni-stack--gap-sm</c> reads
/// <c>var(--omni-space-sm)</c>.
/// </summary>
internal static class OmniSpacingTokens
{
    /// <summary>The token suffix of <paramref name="spacing"/>: <c>none</c>, <c>xs</c>, <c>sm</c>, <c>md</c>, <c>lg</c> or <c>xl</c>.</summary>
    internal static string Suffix(OmniSpacing spacing) => spacing switch
    {
        OmniSpacing.None => "none",
        OmniSpacing.XSmall => "xs",
        OmniSpacing.Small => "sm",
        OmniSpacing.Large => "lg",
        OmniSpacing.XLarge => "xl",
        _ => "md"
    };
}
