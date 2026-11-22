using System.Globalization;
using Microsoft.Extensions.Localization;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A localizer over a fixed dictionary: a missing name is returned as its own text and marked not found.
/// With <paramref name="formatCulture"/>, the arguments are formatted into the text in that culture; without
/// it, they are ignored.
/// </summary>
internal sealed class DictionaryStringLocalizer(IReadOnlyDictionary<string, string> values, CultureInfo? formatCulture = null) : IStringLocalizer
{
    public LocalizedString this[string name] =>
        values.TryGetValue(name, out var value) ? new(name, value) : new(name, name, resourceNotFound: true);

    public LocalizedString this[string name, params object[] arguments] =>
        formatCulture is null
            ? this[name]
            : new(name, string.Format(formatCulture, this[name].Value, arguments), this[name].ResourceNotFound);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}
