using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// An <see cref="OmniMap"/> disposed while its script is still loading must leave nothing behind: the late module is
/// released at once, the script is never attached nor given anything, and the disposal does not hang.
/// </summary>
public sealed class MapScriptLoadTests : OmniBunitContext
{
    [Fact]
    public async Task AMapDisposedWhileItsScriptLoads_NeverAttachesIt_AndReleasesTheLateModule()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var map = Render<OmniMap>();

        var disposal = map.Instance.DisposeAsync().AsTask();
        runtime.PendingImport.SetResult(runtime.Module);
        await disposal;

        Assert.Equal(1, runtime.Imports);
        Assert.Empty(runtime.Module.Calls);
        Assert.Equal(1, runtime.Module.Disposals);
    }
}
