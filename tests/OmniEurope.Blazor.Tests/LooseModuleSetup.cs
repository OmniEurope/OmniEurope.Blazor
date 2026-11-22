using Bunit;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Script modules planted in loose mode, so every call the test does not set up answers with a default,
/// and the preferences module with an empty store, as a first visit finds it.
/// </summary>
internal static class LooseModuleSetup
{
    internal static BunitJSModuleInterop SetupLooseModule(this BunitJSInterop interop, string modulePath)
    {
        var module = interop.SetupModule(modulePath);
        module.Mode = JSRuntimeMode.Loose;
        return module;
    }

    internal static BunitJSModuleInterop SetupEmptyPreferencesModule(this BunitJSInterop interop)
    {
        var module = interop.SetupLooseModule(OmniModules.Preferences);
        module.Setup<string[][]>("entries", _ => true).SetResult([]);
        return module;
    }
}
