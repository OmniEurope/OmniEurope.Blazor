using System.Globalization;
using System.Runtime.CompilerServices;

namespace OmniEurope.Blazor.Tests;

internal static class TestCulture
{
    // The suite asserts French-formatted output, so without an explicit culture the results would
    // depend on the machine locale: green in France, red on an English CI runner. Cross-culture
    // behaviour stays covered by LocalizationTests, which switches culture on its own.
    [ModuleInitializer]
    internal static void Pin()
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
