namespace OmniEurope.Blazor.Tests;

/// <summary>Runs a piece of a test under a given language, then puts the thread's cultures back.</summary>
internal static class CultureSwitch
{
    /// <summary>Reads <paramref name="read"/> with both the culture and the UI culture set to <paramref name="language"/>.</summary>
    public static T InCulture<T>(string language, Func<T> read)
    {
        using var scope = CultureScope.Use(language);
        return read();
    }
}
