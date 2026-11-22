using System.Globalization;

namespace OmniEurope.Blazor.Tests;

/// <summary>A culture set for the current thread until the scope is disposed, then the previous one restored.</summary>
internal static class CultureScope
{
    /// <summary>Sets both the culture and the interface culture to <paramref name="name"/>.</summary>
    internal static IDisposable Use(string name)
    {
        var previous = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
        return new Restore(previous.CurrentCulture, previous.CurrentUICulture);
    }

    private sealed class Restore(CultureInfo culture, CultureInfo uiCulture) : IDisposable
    {
        public void Dispose() => (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, uiCulture);
    }
}
