using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Media;

/// <summary>
/// Two video tiles on the same stream, each with a drawing layer, that exchange their strokes through this page (a tile
/// transports nothing), a demonstration stream made in the module of the page itself (<c>wwwroot/media-tile-demo.js</c>, from
/// an animated canvas) or the camera, and the export of the frame with its strokes as a PNG.
/// </summary>
public sealed partial class MediaTileExample : IAsyncDisposable
{
    private const string DemoModule = "./_content/OmniEurope.Blazor.Showcase.Examples/media-tile-demo.js";

    private OmniMediaTile? _first;
    private OmniMediaTile? _second;
    private IJSObjectReference? _module;
    private IJSObjectReference? _demo;
    private IJSObjectReference? _camera;
    private bool _useCamera;
    private bool _disposed;

    /// <summary>Shows the stream; off, the tiles are in their no-stream state (initials on a disc).</summary>
    [Parameter]
    public bool HasStream { get; set; } = true;

    /// <summary>How the picture fills each tile: all of it, with bands, or the whole tile, cut off.</summary>
    [Parameter]
    public OmniMediaFit Fit { get; set; }

    /// <summary>The video plays without sound; a remote peer's stream is not muted.</summary>
    [Parameter]
    public bool Muted { get; set; } = true;

    /// <summary>Shows the full-screen button in the action bar of each tile.</summary>
    [Parameter]
    public bool ShowFullscreen { get; set; } = true;

    /// <summary>Shows the muted-microphone icon.</summary>
    [Parameter]
    public bool MicrophoneMuted { get; set; }

    /// <summary>Shows the screen-sharing icon.</summary>
    [Parameter]
    public bool ScreenSharing { get; set; }

    /// <summary>Shows the camera icon.</summary>
    [Parameter]
    public bool CameraActive { get; set; }

    /// <summary>Shows the drawing layer over the picture of each tile.</summary>
    [Parameter]
    public bool Drawing { get; set; } = true;

    /// <summary>Shows the drawing toolbar above each tile.</summary>
    [Parameter]
    public bool ShowDrawingToolbar { get; set; } = true;

    /// <summary>Shows the strokes of each tile without letting the pointer draw or erase.</summary>
    [Parameter]
    public bool DrawingReadOnly { get; set; }

    /// <summary>What the pointer does on the drawing layers: draw or erase whole strokes.</summary>
    [Parameter]
    public OmniInkTool Tool { get; set; }

    /// <summary>Raised when a toolbar chooses the pen or the eraser.</summary>
    [Parameter]
    public EventCallback<OmniInkTool> ToolChanged { get; set; }

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    private IReadOnlyList<OmniInkStroke> StrokesA { get; set; } = [];

    private IReadOnlyList<OmniInkStroke> StrokesB { get; set; } = [];

    private string? Exported { get; set; }

    private int ExportKilobytes { get; set; }

    private string Message { get; set; } = "The demo stream is an animated canvas captured by captureStream(30): no permission needed.";

    private IJSObjectReference? CurrentStream => HasStream ? (_useCamera ? _camera : _demo) : null;

    /// <summary>Makes the demonstration stream once, in the browser, as an application would make its own.</summary>
    /// <param name="firstRender">True on the first render.</param>
    /// <returns>A task that completes when the stream is made and shown.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _module = await JavaScript.InvokeAsync<IJSObjectReference>("import", DemoModule);
        _demo = await _module.InvokeAsync<IJSObjectReference>("createDemoStream", 640, 480, "Demo stream");
        if (_disposed)
        {
            await StopAsync(_demo);
            return;
        }

        StateHasChanged();
    }

    /// <summary>Stops the streams this page made (the tiles never stop a stream) and releases its module.</summary>
    /// <returns>A task that completes when everything is released.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        try
        {
            await StopAsync(_demo);
            await StopAsync(_camera);
            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the streams with it.
        }
    }

    private void DrawnOnFirst(OmniInkStroke stroke) => StrokesB = [.. StrokesB, stroke];

    private void DrawnOnSecond(OmniInkStroke stroke) => StrokesA = [.. StrokesA, stroke];

    private void RemovedOnFirst(IReadOnlyList<OmniInkStroke> removed) => StrokesB = Without(StrokesB, removed);

    private void RemovedOnSecond(IReadOnlyList<OmniInkStroke> removed) => StrokesA = Without(StrokesA, removed);

    private static IReadOnlyList<OmniInkStroke> Without(IReadOnlyList<OmniInkStroke> strokes, IReadOnlyList<OmniInkStroke> removed)
    {
        var ids = removed.Select(stroke => stroke.Id).ToHashSet();
        return [.. strokes.Where(stroke => !ids.Contains(stroke.Id))];
    }

    private async Task UseCameraAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            _camera = await _module.InvokeAsync<IJSObjectReference>("useCamera");
            _useCamera = true;
            Message = "The camera is on: both tiles show it. Nothing is sent anywhere.";
        }
        catch (JSException exception)
        {
            Message = $"The camera cannot be used here ({FirstLine(exception.Message)}); the demo stream stays.";
        }
    }

    private async Task UseDemoAsync()
    {
        _useCamera = false;
        Message = "The demo stream is back.";
        await StopAsync(_camera);
        _camera = null;
    }

    private Task FullscreenAsync() => _first is null ? Task.CompletedTask : _first.ToggleFullscreenAsync();

    private async Task ExportAsync()
    {
        if (_first is null)
        {
            return;
        }

        try
        {
            var png = await _first.ExportPngAsync();
            Exported = "data:image/png;base64," + Convert.ToBase64String(png);
            ExportKilobytes = Math.Max(1, png.Length / 1024);
            Message = "Exported.";
        }
        catch (InvalidOperationException exception)
        {
            Message = exception.Message;
        }
    }

    private async Task StopAsync(IJSObjectReference? stream)
    {
        if (stream is not null && _module is not null)
        {
            await _module.InvokeVoidAsync("stopStream", stream);
        }
    }

    private static string FirstLine(string text) => text.Split('\n', 2)[0].Trim();
}
