using System.Globalization;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Showcase.Localization;

/// <summary>
/// Switches the showcase between its languages for the application menu of the header: it saves the
/// language code in the browser, then reloads the page, which starts in that culture (<c>Program.cs</c>).
/// </summary>
/// <param name="js">The browser, where the choice is saved.</param>
/// <param name="navigation">Reloads the page in the new culture.</param>
public sealed class ShowcaseLanguageSwitcher(IJSRuntime js, NavigationManager navigation)
{
    /// <summary>One entry per language, its own name as text, its ISO 639-1 code as value.</summary>
    public static IReadOnlyList<OmniAppMenuLanguage> Languages { get; } =
        [.. ShowcaseLanguages.All.Select(language => new OmniAppMenuLanguage(language.Code, language.Endonym))];

    /// <summary>The code of the language the page runs in, read from its UI culture.</summary>
    public static string Current => ShowcaseLanguages.Resolve(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName).Code;

    /// <summary>
    /// Saves the chosen language and reloads. When the browser refuses to store it (blocked site data),
    /// a reload would come back in the same language, so nothing happens and the menu keeps showing the
    /// current one.
    /// </summary>
    /// <param name="code">The ISO 639-1 code picked; an unknown code is the default language.</param>
    /// <returns>True when the page was sent to reload in the new language.</returns>
    public async Task<bool> SwitchAsync(string code)
    {
        var language = ShowcaseLanguages.Resolve(code);
        if (language.Code == Current)
        {
            return false;
        }

        if (!await js.InvokeAsync<bool>("omniShowcaseCulture.save", ShowcaseLanguages.StorageKey, language.Code).ConfigureAwait(true))
        {
            return false;
        }

        navigation.NavigateTo(navigation.Uri, forceLoad: true);
        return true;
    }
}
