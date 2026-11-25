using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>The interop module of the copy buttons, set up so that every copy to the clipboard succeeds.</summary>
internal static class ClipboardTestSetup
{
    internal static BunitJSModuleInterop SetupSucceedingCopy(this BunitJSInterop interop)
    {
        var module = interop.SetupModule(OmniModules.Interop);
        module.Setup<bool>("copyText", _ => true).SetResult(true);
        return module;
    }
}
