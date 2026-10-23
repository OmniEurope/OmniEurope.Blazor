using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniPackageTooltips"/>: the module is imported and the tooltips turned on once, turned off and
/// released on disposal, never wired after the owner left, and a lost circuit is ignored at every step.
/// </summary>
public sealed class PackageTooltipsTests
{
    [Fact]
    public async Task Install_ImportsTheModuleAndTurnsTheTooltipsOnOnce()
    {
        var runtime = new ManualJSRuntime();
        var tooltips = new OmniPackageTooltips(runtime);

        await tooltips.InstallAsync();
        await tooltips.InstallAsync();

        Assert.Equal(["installPackageTooltips"], runtime.Module.Calls);
    }

    [Fact]
    public async Task Dispose_AfterInstall_TurnsTheTooltipsOffAndReleasesTheModule()
    {
        var runtime = new ManualJSRuntime();
        var tooltips = new OmniPackageTooltips(runtime);
        await tooltips.InstallAsync();

        await tooltips.DisposeAsync();

        Assert.Equal(["installPackageTooltips", "uninstallPackageTooltips"], runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task Dispose_WithoutInstall_TouchesNothing()
    {
        var runtime = new ManualJSRuntime();
        var tooltips = new OmniPackageTooltips(runtime);

        await tooltips.DisposeAsync();

        Assert.Empty(runtime.Module.Calls);
        Assert.False(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task Install_AfterDispose_ImportsNothing()
    {
        var runtime = new ManualJSRuntime { ImportFailure = new InvalidOperationException("no import expected") };
        var tooltips = new OmniPackageTooltips(runtime);
        await tooltips.DisposeAsync();

        await tooltips.InstallAsync();

        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task Dispose_DuringTheImport_ReleasesTheLateModuleAndTurnsNothingOn()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        var tooltips = new OmniPackageTooltips(runtime);

        var installing = tooltips.InstallAsync();
        await tooltips.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);
        await installing;

        Assert.Empty(runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task Install_OnALostCircuit_IsIgnored_AndDisposalStaysQuiet()
    {
        var runtime = new ManualJSRuntime { ImportFailure = new JSDisconnectedException("perdu") };
        var tooltips = new OmniPackageTooltips(runtime);

        await tooltips.InstallAsync();
        await tooltips.DisposeAsync();

        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task Install_CallLostAfterTheImport_StillReleasesTheModuleOnDisposal()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["installPackageTooltips"] = new JSDisconnectedException("perdu");
        var tooltips = new OmniPackageTooltips(runtime);

        await tooltips.InstallAsync();
        await tooltips.DisposeAsync();

        Assert.Equal(["installPackageTooltips"], runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task Dispose_OnALostCircuit_IsIgnored()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["uninstallPackageTooltips"] = new JSDisconnectedException("perdu");
        var tooltips = new OmniPackageTooltips(runtime);
        await tooltips.InstallAsync();

        await tooltips.DisposeAsync();

        Assert.Equal(["installPackageTooltips", "uninstallPackageTooltips"], runtime.Module.Calls);
    }

    [Fact]
    public async Task Dispose_WhenTheModuleReleaseIsLost_IsIgnored()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        var tooltips = new OmniPackageTooltips(runtime);
        await tooltips.InstallAsync();

        await tooltips.DisposeAsync();

        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }
}
