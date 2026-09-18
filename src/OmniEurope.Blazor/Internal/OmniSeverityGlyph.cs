namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The glyph of each severity, shared by the alert disc and the notification mark so both say the
/// same thing the same way: the glyph alone, without a circle or a triangle around it (the disc is
/// the frame), drawn centred on (12, 12) of a 24-unit box so it sits in the middle of the disc.
/// </summary>
internal static class OmniSeverityGlyph
{
    internal const string Information = "M12 11v6M12 7h.01";
    internal const string Success = "m6.5 12.5 3.5 3.5 7.5-8";
    internal const string Warning = "M12 7v6M12 17h.01";
    internal const string Danger = "M8 8l8 8M16 8l-8 8";
}
