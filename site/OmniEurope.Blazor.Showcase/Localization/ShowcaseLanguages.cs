using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Localization;

/// <summary>
/// One language the showcase is offered in.
/// </summary>
/// <param name="Code">ISO 639-1 code of the language, the value kept in the browser.</param>
/// <param name="Culture">Specific culture of a member state speaking the language: it picks the
/// satellite resources and formats numbers and dates.</param>
/// <param name="Endonym">The language's name in the language itself, as the selector lists it.</param>
public sealed record ShowcaseLanguage(string Code, string Culture, string Endonym);

/// <summary>
/// The 24 official languages of the European Union the showcase is offered in, and the culture the
/// page runs in.
/// </summary>
/// <remarks>
/// A WebAssembly page keeps the culture it started with: the choice is read from the browser before
/// the host runs (<c>Program.cs</c>), and changing it saves the new code and reloads the page.
/// </remarks>
public static class ShowcaseLanguages
{
    /// <summary>The browser storage key holding the chosen language code.</summary>
    public const string StorageKey = "omnieurope.showcase.culture";

    /// <summary>
    /// Every language, in the EU protocol order: alphabetical by each language's own name
    /// (български, español, čeština, ... suomi, svenska), the order of the institutions' language menus.
    /// </summary>
    public static IReadOnlyList<ShowcaseLanguage> All { get; } =
    [
        new("bg", "bg-BG", "Български"),
        new("es", "es-ES", "Español"),
        new("cs", "cs-CZ", "Čeština"),
        new("da", "da-DK", "Dansk"),
        new("de", "de-DE", "Deutsch"),
        new("et", "et-EE", "Eesti"),
        new("el", "el-GR", "Ελληνικά"),
        new("en", "en-IE", "English"),
        new("fr", "fr-FR", "Français"),
        new("ga", "ga-IE", "Gaeilge"),
        new("hr", "hr-HR", "Hrvatski"),
        new("it", "it-IT", "Italiano"),
        new("lv", "lv-LV", "Latviešu"),
        new("lt", "lt-LT", "Lietuvių"),
        new("hu", "hu-HU", "Magyar"),
        new("mt", "mt-MT", "Malti"),
        new("nl", "nl-NL", "Nederlands"),
        new("pl", "pl-PL", "Polski"),
        new("pt", "pt-PT", "Português"),
        new("ro", "ro-RO", "Română"),
        new("sk", "sk-SK", "Slovenčina"),
        new("sl", "sl-SI", "Slovenščina"),
        new("fi", "fi-FI", "Suomi"),
        new("sv", "sv-SE", "Svenska")
    ];

    /// <summary>French, the language the showcase is written in and the one used when nothing is saved.</summary>
    public static ShowcaseLanguage Default { get; } = All.Single(language => language.Code == "fr");

    /// <summary>
    /// The language of a saved code, compared without regard to case; <see cref="Default"/> when the
    /// code is missing or names no language of the list.
    /// </summary>
    public static ShowcaseLanguage Resolve(string? code) =>
        All.FirstOrDefault(language => string.Equals(language.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? Default;

    /// <summary>
    /// The culture that formats numbers and dates for a language: the language's own culture, with the
    /// euro as currency sign. The showcase's sample amounts are euros whatever the language; without the
    /// override a {0:C0} amount would show "Kč", "zł" or "kr" in front of the same figure.
    /// </summary>
    public static CultureInfo FormattingCultureOf(ShowcaseLanguage language)
    {
        ArgumentNullException.ThrowIfNull(language);
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(language.Culture).Clone();
        culture.NumberFormat.CurrencySymbol = "€";
        return culture;
    }

    /// <summary>
    /// Makes a language the culture of the page: the default culture of every thread (formatting,
    /// through <see cref="FormattingCultureOf"/>) and their default UI culture (resources), plus the
    /// calling thread's own.
    /// </summary>
    public static void Apply(ShowcaseLanguage language)
    {
        ArgumentNullException.ThrowIfNull(language);
        var formatting = FormattingCultureOf(language);
        var resources = CultureInfo.GetCultureInfo(language.Culture);
        CultureInfo.DefaultThreadCurrentCulture = formatting;
        CultureInfo.DefaultThreadCurrentUICulture = resources;
        CultureInfo.CurrentCulture = formatting;
        CultureInfo.CurrentUICulture = resources;
    }
}
