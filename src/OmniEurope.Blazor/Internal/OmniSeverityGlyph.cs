using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The glyph of each severity, shared by the alert disc, the notification mark and the intention mark
/// of a dialog so all three say the same thing the same way: the glyph alone, without a circle or a
/// triangle around it (the disc is the frame), drawn centred on (12, 12) of a 24-unit box so it sits
/// in the middle of the disc.
/// </summary>
internal static class OmniSeverityGlyph
{
    internal const string Information = "M12 11v6M12 7h.01";
    internal const string Success = "m6.5 12.5 3.5 3.5 7.5-8";
    internal const string Warning = "M12 7v6M12 17h.01";
    internal const string Danger = "M8 8l8 8M16 8l-8 8";

    /// <summary>The path of the glyph of <paramref name="severity"/>.</summary>
    internal static string For(OmniSeverity severity) => severity switch
    {
        OmniSeverity.Success => Success,
        OmniSeverity.Warning => Warning,
        OmniSeverity.Danger => Danger,
        _ => Information
    };

    /// <summary>
    /// The path of the glyph of a colour intention: the severity glyph of the same name, the
    /// information glyph for <see cref="OmniTone.Accent"/> and <see cref="OmniTone.Neutral"/>.
    /// </summary>
    internal static string For(OmniTone tone) => tone switch
    {
        OmniTone.Success => Success,
        OmniTone.Warning => Warning,
        OmniTone.Danger => Danger,
        _ => Information
    };
}
