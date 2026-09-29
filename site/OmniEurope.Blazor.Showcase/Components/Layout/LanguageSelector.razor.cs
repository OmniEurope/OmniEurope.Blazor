using System.Globalization;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Showcase.Localization;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Layout;

/// <summary>
/// Header control that switches the showcase to another of the 24 official EU languages: it saves the
/// language code in the browser, then reloads the page, which starts in that culture (<c>Program.cs</c>).
/// </summary>
public partial class LanguageSelector
{
    /// <summary>One option per language, its own name as text, its ISO 639-1 code as value.</summary>
    private static readonly IReadOnlyList<OmniOption<string>> Options =
        [.. ShowcaseLanguages.All.Select(language => new OmniOption<string>(language.Code, language.Endonym))];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>The code of the language the page runs in, read from its UI culture.</summary>
    private static string Current => ShowcaseLanguages.Resolve(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName).Code;

    private string Selected { get; set; } = Current;

    /// <summary>
    /// Saves the chosen language and reloads. When the browser refuses to store it (blocked site data),
    /// a reload would come back in the same language, so the selector returns to the current one instead.
    /// </summary>
    private async Task SwitchAsync()
    {
        var language = ShowcaseLanguages.Resolve(Selected);
        if (language.Code == Current)
        {
            return;
        }

        if (await Js.InvokeAsync<bool>("omniShowcaseCulture.save", ShowcaseLanguages.StorageKey, language.Code).ConfigureAwait(true))
        {
            Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
        }
        else
        {
            Selected = Current;
        }
    }
}
