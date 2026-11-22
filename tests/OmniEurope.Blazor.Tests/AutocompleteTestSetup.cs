using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The autocomplete and filterable drop-down setups the selection suites share: an empty bound value, a
/// search over a fixed list, and the integer parsing of a free text.
/// </summary>
internal static class AutocompleteTestSetup
{
    /// <summary>An autocomplete bound to <paramref name="value"/> (empty by default), answering <paramref name="search"/>; <paramref name="extra"/> adds the rest.</summary>
    internal static IRenderedComponent<OmniAutocomplete<string>> RenderAutocomplete(
        this BunitContext context,
        Func<string, CancellationToken, Task<IReadOnlyList<OmniOption<string>>>> search,
        string value = "",
        Action<ComponentParameterCollectionBuilder<OmniAutocomplete<string>>>? extra = null)
    {
        return context.Render<OmniAutocomplete<string>>(parameters =>
        {
            parameters
                .Add(component => component.Value, value)
                .Add(component => component.ValueExpression, () => value)
                .Add(component => component.Search, search);
            extra?.Invoke(parameters);
        });
    }

    /// <summary>
    /// A filterable drop-down over <paramref name="options"/>, bound to an empty text; with
    /// <paramref name="changed"/>, the value it raises is bound back and reported.
    /// </summary>
    internal static IRenderedComponent<OmniDropDown<string>> RenderFilterableDropDown(
        this BunitContext context,
        IReadOnlyList<OmniOption<string>> options,
        Action<string>? changed = null,
        Action<ComponentParameterCollectionBuilder<OmniDropDown<string>>>? extra = null)
    {
        var value = string.Empty;
        return context.Render<OmniDropDown<string>>(parameters =>
        {
            parameters
                .Add(component => component.Filterable, true)
                .Add(component => component.Options, options)
                .Add(component => component.Value, value)
                .Add(component => component.ValueExpression, () => value);
            if (changed is not null)
            {
                parameters.Add(component => component.ValueChanged, (string next) =>
                {
                    value = next;
                    changed(next);
                });
            }

            extra?.Invoke(parameters);
        });
    }

    /// <summary>The options, among <paramref name="options"/>, whose text contains the typed text whatever its case.</summary>
    internal static Func<string, CancellationToken, Task<IReadOnlyList<OmniOption<string>>>> SearchIn(IReadOnlyList<OmniOption<string>> options) =>
        (text, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(
            [.. options.Where(option => option.Text.Contains(text, StringComparison.OrdinalIgnoreCase))]);

    /// <summary>A free text taken and parsed as an invariant integer, every raised value recorded, and no debounce.</summary>
    internal static ComponentParameterCollectionBuilder<OmniAutocomplete<int>> ParsingIntegersInto(
        this ComponentParameterCollectionBuilder<OmniAutocomplete<int>> parameters,
        List<int> changes) =>
        parameters
            .Add(component => component.ValueChanged, (int next) => changes.Add(next))
            .Add(component => component.AllowFreeText, true)
            .Add(component => component.ParseFreeText, text => int.Parse(text, System.Globalization.CultureInfo.InvariantCulture))
            .Add(component => component.Debounce, TimeSpan.Zero);
}
