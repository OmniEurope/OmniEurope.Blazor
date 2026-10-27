using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>
/// Three sample languages for the app menu. Their flags are small inline images (data addresses), so they
/// show without any request, under the strictest content policy.
/// </summary>
public static class AppMenuLanguages
{
    /// <summary>French, English and German, each with its flag.</summary>
    public static IReadOnlyList<OmniAppMenuLanguage> All { get; } =
    [
        new("fr", "Français", Flag("0 0 3 2",
            "<rect width='1' height='2' fill='#0055a4'/><rect x='1' width='1' height='2' fill='#fff'/><rect x='2' width='1' height='2' fill='#ef4135'/>")),
        new("en", "English", Flag("0 0 60 30",
            "<rect width='60' height='30' fill='#012169'/>"
            + "<path d='M0,0 60,30M60,0 0,30' stroke='#fff' stroke-width='6'/>"
            + "<path d='M0,0 60,30M60,0 0,30' stroke='#c8102e' stroke-width='3'/>"
            + "<path d='M30,0v30M0,15h60' stroke='#fff' stroke-width='10'/>"
            + "<path d='M30,0v30M0,15h60' stroke='#c8102e' stroke-width='6'/>")),
        new("de", "Deutsch", Flag("0 0 5 3",
            "<rect width='5' height='1' fill='#000'/><rect y='1' width='5' height='1' fill='#dd0000'/><rect y='2' width='5' height='1' fill='#ffce00'/>"))
    ];

    private static string Flag(string viewBox, string shapes) =>
        "data:image/svg+xml," + Uri.EscapeDataString($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='{viewBox}'>{shapes}</svg>");
}
