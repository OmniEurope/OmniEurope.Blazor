using System.Globalization;
using System.Resources;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The descriptions of the theme, palette, backdrop and font catalogues, read from the package resources
/// (<see cref="AppStrings"/>, 24 languages) in the current UI culture each time they are asked for, so a
/// catalogue entry built once describes itself in the language of whoever reads it. The names stay French
/// and are never translated: they are the keys the preference keeps.
/// </summary>
/// <remarks>
/// The records are values, built once and compared by reference and by value, so they cannot take an
/// injected <c>IStringLocalizer</c>: the resources are read through their <see cref="ResourceManager"/>,
/// the one the package localizer reads too. A host text override
/// (<c>AddOmniEuropeTextOverrides</c>) therefore does not reach these descriptions.
/// </remarks>
internal static class CatalogDescriptions
{
    private static readonly ResourceManager Resources = new(typeof(AppStrings));

    /// <summary>The text of <paramref name="key"/> in the current UI culture, or <paramref name="fallback"/> when the resources do not hold it.</summary>
    internal static string Describe(string key, string fallback) =>
        Resources.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;

    /// <summary>The French text of <paramref name="key"/>, the neutral resource: the value a catalogue record carries as written.</summary>
    internal static string Neutral(string key) =>
        Resources.GetString(key, CultureInfo.InvariantCulture)
        ?? throw new InvalidOperationException($"The package resources have no description {key}.");
}
