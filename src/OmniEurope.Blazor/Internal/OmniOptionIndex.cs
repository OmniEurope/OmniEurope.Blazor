using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Reads the index of an option, as the native <c>select</c> and the list box post it: a whole number
/// written with digits whatever the culture of the page is, naming an option that exists and is enabled.
/// </summary>
internal static class OmniOptionIndex
{
    internal static bool TryGet<TValue>(IReadOnlyList<OmniOption<TValue>> options, string? text, out OmniOption<TValue> option)
    {
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var index)
            && index < options.Count && !options[index].Disabled)
        {
            option = options[index];
            return true;
        }

        option = null!;
        return false;
    }
}
