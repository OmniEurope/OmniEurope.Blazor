using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary><see cref="OmniFileDownloader"/>: the module receives the name, the type and the content; bad arguments are refused before any script runs.</summary>
public sealed class FileDownloaderTests : OmniBunitContext
{
    private BunitJSModuleInterop Module() => JSInterop.SetupModule(Internal.OmniModules.DocumentEditor);

    private OmniFileDownloader Downloader() => Services.GetRequiredService<OmniFileDownloader>();

    [Fact]
    public async Task Bytes_ReachTheDownloadFunction_WithNameTypeAndContent()
    {
        var module = Module();
        module.SetupVoid("download", _ => true).SetVoidResult();
        byte[] content = [1, 2, 3];

        await Downloader().DownloadAsync(content, " report.bin ", "application/x-test", Xunit.TestContext.Current.CancellationToken);

        var call = Assert.Single(module.Invocations["download"]);
        Assert.Equal("report.bin", call.Arguments[0]);
        Assert.Equal("application/x-test", call.Arguments[1]);
        Assert.Equal(content, Assert.IsType<byte[]>(call.Arguments[2]));
    }

    [Fact]
    public async Task TheContentType_DefaultsToOctetStream()
    {
        var module = Module();
        module.SetupVoid("download", _ => true).SetVoidResult();

        await Downloader().DownloadAsync([7], "a.bin", cancellationToken: Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("application/octet-stream", Assert.Single(module.Invocations["download"]).Arguments[1]);
    }

    [Fact]
    public async Task AStream_IsHandedAsAStreamReference_AndLeftOpen()
    {
        var module = Module();
        module.SetupVoid("downloadStream", _ => true).SetVoidResult();
        await using var stream = new MemoryStream([1, 2, 3, 4]);

        await Downloader().DownloadAsync(stream, "big.bin", "application/x-test", Xunit.TestContext.Current.CancellationToken);

        var call = Assert.Single(module.Invocations["downloadStream"]);
        Assert.Equal("big.bin", call.Arguments[0]);
        Assert.Equal("application/x-test", call.Arguments[1]);
        Assert.IsType<DotNetStreamReference>(call.Arguments[2]);
        Assert.True(stream.CanRead);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task AnEmptyName_IsRefused_BeforeTheModuleLoads(string? name)
    {
        var module = Module();

        await Assert.ThrowsAnyAsync<ArgumentException>(async () => await Downloader().DownloadAsync([1], name!, "text/plain", Xunit.TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<ArgumentException>(async () => await Downloader().DownloadAsync(new MemoryStream(), name!, "text/plain", Xunit.TestContext.Current.CancellationToken));

        Assert.Empty(module.Invocations);
    }

    [Fact]
    public async Task ANullContent_OrAnEmptyType_IsRefused()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await Downloader().DownloadAsync((byte[])null!, "a.txt", "text/plain", Xunit.TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await Downloader().DownloadAsync((Stream)null!, "a.txt", "text/plain", Xunit.TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<ArgumentException>(async () => await Downloader().DownloadAsync([1], "a.txt", " ", Xunit.TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AnUnreadableStream_IsRefused()
    {
        var stream = new MemoryStream();
        await stream.DisposeAsync();

        await Assert.ThrowsAsync<ArgumentException>(async () => await Downloader().DownloadAsync(stream, "a.txt", "text/plain", Xunit.TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TheDownloads_ShareOneModule()
    {
        var module = Module();
        module.SetupVoid("download", _ => true).SetVoidResult();

        await Downloader().DownloadAsync([1], "a.bin", cancellationToken: Xunit.TestContext.Current.CancellationToken);
        await Downloader().DownloadAsync([2], "b.bin", cancellationToken: Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(2, module.Invocations["download"].Count);
        Assert.Single(JSInterop.Invocations["import"]);
    }
}
