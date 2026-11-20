using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The paths of <see cref="OmniMap"/> the main map tests do not reach: a longitude west of the date line, the key of a line
/// or a marker, a render after the disposal, a lost circuit at each script call, a change of the tile options, a null entry
/// in a list, and the bridge the script calls. Each test reads what the path does (markup, event, call to the script).
/// </summary>
public sealed class MapEdgeTests : OmniBunitContext
{
    private static readonly OmniGeoPoint Brussels = new(50.8466, 4.3528);
    private readonly BunitJSModuleInterop _module;

    public MapEdgeTests() => _module = JSInterop.SetupModule(OmniModules.Map);

    private IRenderedComponent<OmniMap> RenderMap(Action<ComponentParameterCollectionBuilder<OmniMap>>? configure = null)
        => Render<OmniMap>(parameters => configure?.Invoke(parameters));

    private static OmniMapLine Line(double width = 5, string? tooltip = null, object? key = null)
        => new() { Points = [new(50.8466, 4.3528), new(50.8355, 4.3330)], Width = width, Tooltip = tooltip, Key = key };

    private static OmniMapMarker Marker(string label, object? key = null)
        => new() { Point = Brussels, Label = label, Key = key };

    private IReadOnlyList<object?> LastUpdate() => _module.Invocations["update"][^1].Arguments;

    [Theory]
    [InlineData(-190, 170)]
    [InlineData(-540, -180)]
    [InlineData(190, -170)]
    [InlineData(-180, -180)]
    [InlineData(180, 180)]
    [InlineData(0, 0)]
    public void ALongitude_IsBroughtBackBetweenMinus180And180_OnBothSides(double longitude, double expected)
    {
        var point = new OmniGeoPoint(10, longitude).ToMercatorRange();

        Assert.Equal(expected, point.Longitude, 9);
        Assert.Equal(10, point.Latitude);
    }

    [Fact]
    public void ACentreWestOfTheDateLine_IsGivenToTheScriptBroughtBackEast()
    {
        RenderMap(parameters => parameters.Add(m => m.Center, new OmniGeoPoint(-89, -190)));

        var view = (double[])LastUpdate()[6]!;
        Assert.Equal(-OmniGeoPoint.MaxLatitude, view[0]);
        Assert.Equal(170, view[1], 9);
    }

    [Fact]
    public void ALineOrMarkerKey_IsAccepted_AndTheListReplacedInAnotherOrderIsDrawnAndWiredInThatOrder()
    {
        var first = Marker("A", "a");
        var second = Marker("B", "b");
        var wide = Line(width: 9, tooltip: "Large", key: "wide");
        var thin = Line(width: 3, key: "thin");
        var pressed = new List<OmniMapMarker>();
        var map = RenderMap(parameters => parameters
            .Add(m => m.Markers, [first, second])
            .Add(m => m.Lines, [wide, thin])
            .Add(m => m.OnMarkerClick, marker => pressed.Add(marker)));
        Assert.Equal(["Large", null], map.FindAll("g[data-omni-map-line]").Select(group => group.GetAttribute("data-omni-tip")));

        map.Render(parameters => parameters.Add(m => m.Markers, [second, first]).Add(m => m.Lines, [thin, wide]));

        var buttons = map.FindAll("button.omni-map__marker");
        Assert.Equal(["B", "A"], buttons.Select(button => button.GetAttribute("aria-label")));
        var groups = map.FindAll("g[data-omni-map-line]");
        Assert.Equal([null, "Large"], groups.Select(group => group.GetAttribute("data-omni-tip")));
        Assert.Equal(["3", "9"], groups.Select(group => group.QuerySelector("path.omni-map__stroke")!.GetAttribute("stroke-width")));
        buttons[0].Click();
        Assert.Same(second, Assert.Single(pressed));
    }

    [Fact]
    public void ALineWithATooltipOfOnlySpaces_HasNoTip_AndNoLineDescription()
    {
        var map = RenderMap(parameters => parameters.Add(m => m.Id, "route").Add(m => m.Lines, [Line(tooltip: "   ")]));

        Assert.False(map.Find("g[data-omni-map-line]").HasAttribute("data-omni-tip"));
        Assert.Equal("route-help", map.Find("div.omni-map").GetAttribute("aria-describedby"));
    }

    [Fact]
    public async Task AMapDisposed_IgnoresALaterRender_AndTellsTheScriptNothing()
    {
        var map = RenderMap(parameters => parameters.Add(m => m.Markers, [Marker("A")]));
        await map.Instance.DisposeAsync();

        map.Render(parameters => parameters.Add(m => m.Markers, [Marker("A"), Marker("B")]));

        Assert.Single(_module.Invocations["attach"]);
        Assert.Single(_module.Invocations["update"]);
        Assert.Equal(2, map.FindAll("button.omni-map__marker").Count);
    }

    [Fact]
    public void ALostCircuit_WhileAttaching_IsIgnored_AndTheMapStaysRendered()
    {
        _module.SetupVoid("attach", _ => true).SetException(new JSDisconnectedException("circuit perdu"));

        var map = RenderMap(parameters => parameters.Add(m => m.Markers, [Marker("Gare")]));

        Assert.Single(_module.Invocations["attach"]);
        Assert.Empty(_module.Invocations["update"]);
        Assert.Single(map.FindAll("button.omni-map__marker"));
    }

    [Fact]
    public async Task ALostCircuit_WhileDetaching_IsIgnored_AndTheMapIsDisposed()
    {
        var map = RenderMap();
        _module.SetupVoid("detach", _ => true).SetException(new JSDisconnectedException("circuit perdu"));

        await map.Instance.DisposeAsync();

        Assert.Single(_module.Invocations["detach"]);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => map.Instance.ZoomInAsync());
    }

    [Fact]
    public async Task ALostCircuit_WhileZooming_IsIgnored()
    {
        var map = RenderMap();
        _module.SetupVoid("zoomBy", _ => true).SetException(new JSDisconnectedException("circuit perdu"));

        await map.Instance.ZoomInAsync();
        await map.Instance.ZoomOutAsync();

        Assert.Equal([1, -1], _module.Invocations["zoomBy"].Select(call => (int)call.Arguments[1]!));
    }

    [Fact]
    public void ANewMaximumZoomBelowTheView_ClampsItAndMovesTheScriptsView()
    {
        var map = RenderMap();

        map.Render(parameters => parameters.Add(m => m.MaxZoom, 10));

        Assert.Equal(2, _module.Invocations["update"].Count);
        Assert.Equal(OmniMap.DefaultTileUrl, LastUpdate()[1]);
        Assert.Equal(2, LastUpdate()[2]);
        Assert.Equal(10, LastUpdate()[3]);
        Assert.Null(LastUpdate()[4]);
        Assert.Null(LastUpdate()[5]);
        Assert.Equal([50.8466, 4.3528, 10d], (double[])LastUpdate()[6]!);
        Assert.True(map.FindAll(".omni-map__controls button")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void ANewTileUrl_IsGivenToTheScript_WithoutMovingTheView()
    {
        var map = RenderMap();

        map.Render(parameters => parameters.Add(m => m.TileUrl, "/tiles/{z}/{x}/{y}.png"));

        Assert.Equal(2, _module.Invocations["update"].Count);
        Assert.Equal("/tiles/{z}/{x}/{y}.png", LastUpdate()[1]);
        Assert.Null(LastUpdate()[4]);
        Assert.Null(LastUpdate()[6]);
        Assert.False(map.FindAll(".omni-map__controls button")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void ANullLineOrMarker_OrALineWithoutPoints_IsRefused_NamingItsPosition()
    {
        var line = Assert.Throws<ArgumentException>(() => RenderMap(parameters => parameters.Add(m => m.Lines, [Line(), null!])));
        var marker = Assert.Throws<ArgumentException>(() => RenderMap(parameters => parameters.Add(m => m.Markers, [Marker("A"), null!])));
        var points = Assert.Throws<ArgumentException>(() => RenderMap(parameters => parameters.Add(m => m.Lines, [new OmniMapLine { Points = null! }])));

        Assert.Contains("line 1 is null", line.Message, StringComparison.Ordinal);
        Assert.Equal("Lines", line.ParamName);
        Assert.Contains("marker 1 is null", marker.Message, StringComparison.Ordinal);
        Assert.Equal("Markers", marker.ParamName);
        Assert.Contains("line 0 must have two points at least", points.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheBridgeGivenToTheScript_ForwardsTheViewAndTheClick_ToTheHost()
    {
        var centres = new List<OmniGeoPoint>();
        var zooms = new List<int>();
        var clicks = new List<OmniGeoPoint>();
        var map = RenderMap(parameters => parameters
            .Add(m => m.CenterChanged, centre => centres.Add(centre))
            .Add(m => m.ZoomChanged, zoom => zooms.Add(zoom))
            .Add(m => m.OnMapClick, point => clicks.Add(point)));
        var bridge = ((DotNetObjectReference<MapInteropBridge>)_module.Invocations["attach"][0].Arguments[1]!).Value;

        await map.InvokeAsync(() => bridge.OnViewChanged(51, 5, 16));
        await map.InvokeAsync(() => bridge.OnMapClick(50.1, 4.2));

        Assert.Equal([new OmniGeoPoint(51, 5)], centres);
        Assert.Equal([16], zooms);
        Assert.Equal([new OmniGeoPoint(50.1, 4.2)], clicks);
    }

    [Fact]
    public void TheBridge_CallsItsTwoHandlers_WithTheValuesItReceives_AndReturnsTheirTask()
    {
        (double Latitude, double Longitude, int Zoom)? view = null;
        (double Latitude, double Longitude)? click = null;
        var viewDone = new TaskCompletionSource();
        var clickDone = new TaskCompletionSource();
        var bridge = new MapInteropBridge(
            (latitude, longitude, zoom) =>
            {
                view = (latitude, longitude, zoom);
                return viewDone.Task;
            },
            (latitude, longitude) =>
            {
                click = (latitude, longitude);
                return clickDone.Task;
            });

        var viewTask = bridge.OnViewChanged(1.5, 2.5, 7);
        var clickTask = bridge.OnMapClick(3.5, 4.5);

        Assert.Equal((1.5, 2.5, 7), view);
        Assert.Equal((3.5, 4.5), click);
        Assert.Same(viewDone.Task, viewTask);
        Assert.Same(clickDone.Task, clickTask);
    }
}
