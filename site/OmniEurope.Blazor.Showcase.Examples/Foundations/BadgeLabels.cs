using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>The words the badge examples show for a tone, a fill and an ink, instead of the names of the enumerations.</summary>
public static class BadgeLabels
{
    /// <summary>The word for a tone, as the row of a matrix reads it.</summary>
    public static string ToneOf(OmniTone tone) => tone switch
    {
        OmniTone.Accent => "Accent",
        OmniTone.Info => "Information",
        OmniTone.Success => "Success",
        OmniTone.Warning => "Warning",
        OmniTone.Danger => "Danger",
        _ => "Neutral"
    };

    /// <summary>The word for a fill.</summary>
    public static string FillOf(OmniFill fill) => fill switch
    {
        OmniFill.Solid => "Solid",
        OmniFill.Outline => "Outline",
        _ => "Tonal"
    };

    /// <summary>The word for a corner shape.</summary>
    public static string ShapeOf(OmniBadgeShape shape) => shape switch
    {
        OmniBadgeShape.Rounded => "Rounded",
        OmniBadgeShape.Square => "Square",
        _ => "Pill"
    };

    /// <summary>The words for an ink of the solid badge.</summary>
    public static string InkOf(OmniBadgeInk ink) => ink switch
    {
        OmniBadgeInk.WhiteOutlined => "White, outlined",
        OmniBadgeInk.Black => "Black",
        _ => "White"
    };
}
