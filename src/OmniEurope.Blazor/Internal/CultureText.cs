using System.Globalization;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Reads a library text in a given culture: a component with a <c>Culture</c> parameter writes its
/// strings in the same language as its dates, whatever the current UI culture of the thread.
/// </summary>
internal static class CultureText
{
    /// <summary>
    /// Runs <paramref name="localize"/> with <paramref name="culture"/> as the UI culture, then restores
    /// the previous one; with no culture, in the current UI culture.
    /// </summary>
    internal static string In(CultureInfo? culture, Func<string> localize)
    {
        var previous = CultureInfo.CurrentUICulture;
        if (culture is null || Equals(previous, culture))
        {
            return localize();
        }

        CultureInfo.CurrentUICulture = culture;
        try
        {
            return localize();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
