using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniMap"/> in bUnit. The tiles, the projection and the gestures are the script's (its node tests cover the
/// arithmetic and the handlers, the showcase probe the real map); here the markup (region, attribution, marker buttons,
/// zoom buttons, lines and their colours), the checks of the parameters, the callbacks and what the component tells its
/// module, read back from bUnit's interop.
/// </summary>
public sealed class MapTests : MapTestContext
{
    private static OmniMapLine Line(string? color = null, OmniTone tone = OmniTone.Accent, double width = 5, string? tooltip = null)
        => new() { Points = [new(50.8466, 4.3528), new(50.8355, 4.3330)], Color = color, Tone = tone, Width = width, Tooltip = tooltip };

    private static OmniMapMarker Marker(string label, OmniMapMarkerKind kind = OmniMapMarkerKind.Point, OmniIconName? icon = null)
        => new() { Point = Brussels, Label = label, Kind = kind, Icon = icon };

    private IReadOnlyList<object?> LastUpdate() => MapModule.Invocations["update"][^1].Arguments;

    // ----- Markup ------------------------------------------------------------------------------------

    [Fact]
    public void Map_IsAFocusableNamedRegion_DescribedByItsKeys_WithoutAnyStyleAttribute()
    {
        var map = RenderMap(parameters => parameters.Add(m => m.Id, "route").Add(m => m.Lines, [Line()]).Add(m => m.Markers, [Marker("Gare")]));

        var root = map.Find("div.omni-map");
        Assert.Equal("region", root.GetAttribute("role"));
        Assert.Equal("0", root.GetAttribute("tabindex"));
        Assert.Equal("Carte", root.GetAttribute("aria-label"));
        Assert.Equal("route-help", root.GetAttribute("aria-describedby"));
        Assert.Equal("Flèches pour déplacer la carte, + et - pour zoomer.", map.Find("#route-help").TextContent);
        Assert.Contains("omni-visually-hidden", map.Find("#route-help").ClassName, StringComparison.Ordinal);
        Assert.Equal("true", map.Find(".omni-map__tiles").GetAttribute("aria-hidden"));
        Assert.DoesNotContain("style=", map.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Label_ClassAndAttributes_GoToTheRegion()
    {
        var map = RenderMap(parameters => parameters.Add(m => m.Label, "Trajet").Add(m => m.Class, "host-map").AddUnmatched("data-x", "1"));

        var root = map.Find("div.omni-map");
        Assert.Equal("Trajet", root.GetAttribute("aria-label"));
        Assert.Contains("host-map", root.ClassName, StringComparison.Ordinal);
        Assert.Equal("1", root.GetAttribute("data-x"));
    }

    [Fact]
    public void Attribution_IsAlwaysShown_ByDefaultALinkToTheOpenStreetMapCopyright()
    {
        var link = RenderMap().Find(".omni-map__attribution a");

        Assert.Equal("https://www.openstreetmap.org/copyright", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Contains("noopener", link.GetAttribute("rel"), StringComparison.Ordinal);
        Assert.Equal("© les contributeurs d'OpenStreetMap", link.TextContent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankAttribution_StillShowsTheDefault(string? attribution)
    {
        var map = RenderMap(parameters => parameters.Add(m => m.Attribution, attribution));

        Assert.Single(map.FindAll(".omni-map__attribution a"));
    }

    [Fact]
    public void AnotherAttribution_IsShownAsText_InTheSameCorner()
    {
        var map = RenderMap(parameters => parameters.Add(m => m.Attribution, "© Mon serveur de tuiles").Add(m => m.TileUrl, "/tiles/{z}/{x}/{y}.png"));

        var corner = map.Find(".omni-map__attribution");
        Assert.Equal("© Mon serveur de tuiles", corner.TextContent.Trim());
        Assert.Empty(corner.QuerySelectorAll("a"));
    }

    [Fact]
    public void Markers_AreRealButtons_NamedByTheirLabel_InTheirOrder()
    {
        var map = RenderMap(parameters => parameters.Add(m => m.Markers, [Marker("Départ", OmniMapMarkerKind.Start), Marker("Arrivée", OmniMapMarkerKind.End)]));

        var buttons = map.FindAll("button.omni-map__marker");
        Assert.Equal(2, buttons.Count);
        Assert.All(buttons, button => Assert.Equal("button", button.GetAttribute("type")));
        Assert.Equal(["Départ", "Arrivée"], buttons.Select(button => button.GetAttribute("aria-label")));
        Assert.Equal(["Départ", "Arrivée"], buttons.Select(button => button.GetAttribute("data-omni-tip")));
        Assert.All(buttons, button => Assert.True(button.HasAttribute("data-omni-map-marker")));
    }

    [Theory]
    [InlineData(OmniMapMarkerKind.Point, "omni-map__marker--point", OmniIconName.MapPin)]
    [InlineData(OmniMapMarkerKind.Start, "omni-map__marker--start", OmniIconName.Play)]
    [InlineData(OmniMapMarkerKind.Waypoint, "omni-map__marker--waypoint", OmniIconName.Circle)]
    [InlineData(OmniMapMarkerKind.End, "omni-map__marker--end", OmniIconName.Flag)]
    public void EachKind_HasItsOwnLookAndIcon(OmniMapMarkerKind kind, string expectedClass, OmniIconName expectedIcon)
    {
        var marker = Marker("Ici", kind);
        var map = RenderMap(parameters => parameters.Add(m => m.Markers, [marker]));

        Assert.Contains(expectedClass, map.Find("button.omni-map__marker").ClassName, StringComparison.Ordinal);
        Assert.Equal(expectedIcon, OmniMap.IconOf(marker));
        Assert.Equal(expectedIcon, map.FindComponent<OmniIcon>().Instance.Name);
    }

    [Fact]
    public void AMarkerIcon_ReplacesTheIconOfItsKind_AndKeepsItsLook()
    {
        var map = RenderMap(parameters => parameters.Add(m => m.Markers, [Marker("Bureau", OmniMapMarkerKind.End, OmniIconName.Buildings)]));

        Assert.Equal(OmniIconName.Buildings, map.FindComponent<OmniIcon>().Instance.Name);
        Assert.Contains("omni-map__marker--end", map.Find("button.omni-map__marker").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void ZoomButtons_AreLocalizedOmniButtons_DisabledAtTheEndsOfTheRange()
    {
        var map = RenderMap(parameters => parameters.Add(m => m.Zoom, 19));

        var buttons = map.FindAll(".omni-map__controls button");
        Assert.Equal(["Zoom avant", "Zoom arrière"], buttons.Select(button => button.GetAttribute("aria-label")));
        Assert.True(buttons[0].HasAttribute("disabled"));
        Assert.False(buttons[1].HasAttribute("disabled"));

        map.Render(parameters => parameters.Add(m => m.Zoom, 2));
        buttons = map.FindAll(".omni-map__controls button");
        Assert.False(buttons[0].HasAttribute("disabled"));
        Assert.True(buttons[1].HasAttribute("disabled"));
    }

    // ----- Lines -------------------------------------------------------------------------------------

    [Fact]
    public void ALine_IsAGroupOfCasingAndStroke_InTheClassOfItsTone_WithItsWidthWrittenWithADot()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        try
        {
            var map = RenderMap(parameters => parameters.Add(m => m.Lines, [Line(tone: OmniTone.Warning, width: 5.5) with { Class = "pluie" }]));

            var group = map.Find("g[data-omni-map-line]");
            Assert.Equal("omni-map__line omni-map__line--warning pluie", group.GetAttribute("class"));
            Assert.Equal("9.5", map.Find("path.omni-map__casing").GetAttribute("stroke-width"));
            var stroke = map.Find("path.omni-map__stroke");
            Assert.Equal("5.5", stroke.GetAttribute("stroke-width"));
            Assert.False(stroke.HasAttribute("stroke"));
            Assert.Equal("true", map.Find("svg.omni-map__lines").GetAttribute("aria-hidden"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("#0a7")]
    [InlineData("#0a7f")]
    [InlineData("#00aa77")]
    [InlineData("#00AA77cc")]
    [InlineData("rgb(0, 170, 119)")]
    [InlineData("rgba(0,170,119,0.5)")]
    [InlineData("rgb(0 170 119 / 50%)")]
    [InlineData("hsl(160, 100%, 33%)")]
    [InlineData("hsla(160deg 100% 33% / .8)")]
    [InlineData("  #123456  ")]
    public void AValidColour_IsWrittenAsTheStrokeAttribute(string color)
    {
        var map = RenderMap(parameters => parameters.Add(m => m.Lines, [Line(color)]));

        Assert.Equal(color.Trim(), map.Find("path.omni-map__stroke").GetAttribute("stroke"));
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("#gggggg")]
    [InlineData("rgb(1, 2)")]
    [InlineData("rgb(1, 2, 3); fill: red")]
    [InlineData("url(#evil)")]
    [InlineData("var(--omni-color-accent)")]
    [InlineData("expression(alert(1))")]
    [InlineData("rgb(1, 2, 3)\" onload=\"x")]
    [InlineData("")]
    public void JunkAsAColour_IsIgnored_AndTheToneApplies(string color)
    {
        var map = RenderMap(parameters => parameters.Add(m => m.Lines, [Line(color, OmniTone.Danger)]));

        Assert.False(map.Find("path.omni-map__stroke").HasAttribute("stroke"));
        Assert.Contains("omni-map__line--danger", map.Find("g[data-omni-map-line]").GetAttribute("class"), StringComparison.Ordinal);
    }

    [Fact]
    public void Tooltips_AreThePackageTip_AndTheAccessibleDescriptionOfTheMap()
    {
        var map = RenderMap(parameters => parameters
            .Add(m => m.Id, "route")
            .Add(m => m.Lines, [Line(tooltip: "Sec"), Line(), Line(tooltip: "Forte pluie")]));

        var groups = map.FindAll("g[data-omni-map-line]");
        Assert.Equal(["Sec", null, "Forte pluie"], groups.Select(group => group.GetAttribute("data-omni-tip")));
        Assert.Equal("route-help route-lines", map.Find("div.omni-map").GetAttribute("aria-describedby"));
        Assert.Equal(["Sec", "Forte pluie"], map.FindAll("#route-lines li").Select(item => item.TextContent));
    }

    // ----- Parameter checks --------------------------------------------------------------------------

    [Fact]
    public void Defaults_AreBrusselsAtZoom13_OnOpenStreetMap()
    {
        var map = new OmniMap();

        Assert.Equal(new OmniGeoPoint(50.8466, 4.3528), map.Center);
        Assert.Equal(13, map.Zoom);
        Assert.Equal(2, map.MinZoom);
        Assert.Equal(19, map.MaxZoom);
        Assert.Equal("https://tile.openstreetmap.org/{z}/{x}/{y}.png", map.TileUrl);
        Assert.Equal(OmniMap.DefaultTileUrl, map.TileUrl);
        Assert.False(map.FitToContent);
        Assert.Empty(map.Lines);
        Assert.Empty(map.Markers);
        Assert.Equal(OmniTone.Accent, new OmniMapLine { Points = [] }.Tone);
        Assert.Equal(5, new OmniMapLine { Points = [] }.Width);
        Assert.Equal(OmniMapMarkerKind.Point, new OmniMapMarker { Point = Brussels, Label = "x" }.Kind);
    }

    [Fact]
    public void FirstRender_AttachesTheScript_AndGivesItTheOptionsTheContentAndTheView()
    {
        RenderMap(parameters => parameters.Add(m => m.Lines, [Line()]).Add(m => m.Markers, [Marker("Gare")]));

        Assert.Single(MapModule.Invocations["attach"]);
        var update = Assert.Single(MapModule.Invocations["update"]).Arguments;
        Assert.Equal(OmniMap.DefaultTileUrl, update[1]);
        Assert.Equal(2, update[2]);
        Assert.Equal(19, update[3]);
        Assert.Equal([50.8466, 4.3528, 50.8355, 4.3330], Assert.Single((double[][])update[4]!));
        Assert.Equal([50.8466, 4.3528], Assert.Single((double[][])update[5]!));
        Assert.Equal([50.8466, 4.3528, 13d], (double[])update[6]!);
        Assert.Equal(false, update[7]);
    }

    [Theory]
    [InlineData(25, 19)]
    [InlineData(-3, 2)]
    [InlineData(10, 10)]
    public void TheZoom_IsKeptInsideTheRange(int zoom, double expected)
    {
        RenderMap(parameters => parameters.Add(m => m.Zoom, zoom));

        Assert.Equal(expected, ((double[])LastUpdate()[6]!)[2]);
    }

    [Fact]
    public void TheRange_IsKeptBetween0And22_AndAMinimumAboveTheMaximumIsRefused()
    {
        RenderMap(parameters => parameters.Add(m => m.MinZoom, -4).Add(m => m.MaxZoom, 30).Add(m => m.Zoom, 30));
        Assert.Equal(0, LastUpdate()[2]);
        Assert.Equal(22, LastUpdate()[3]);
        Assert.Equal(22d, ((double[])LastUpdate()[6]!)[2]);

        var error = Assert.Throws<ArgumentException>(() => RenderMap(parameters => parameters.Add(m => m.MinZoom, 12).Add(m => m.MaxZoom, 8)));
        Assert.Contains("MinZoom", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheCentre_IsBroughtIntoWebMercator_LatitudeClampedAndLongitudeWrapped()
    {
        RenderMap(parameters => parameters.Add(m => m.Center, new OmniGeoPoint(89, 190)));

        var view = (double[])LastUpdate()[6]!;
        Assert.Equal(OmniGeoPoint.MaxLatitude, view[0]);
        Assert.Equal(-170, view[1], 9);
    }

    [Theory]
    [InlineData(double.NaN, 4)]
    [InlineData(50, double.PositiveInfinity)]
    public void ACentreThatIsNotANumber_IsRefused(double latitude, double longitude)
        => Assert.Throws<ArgumentException>(() => RenderMap(parameters => parameters.Add(m => m.Center, new OmniGeoPoint(latitude, longitude))));

    [Fact]
    public void ALineOfOnePoint_AWidthOutOfRange_OrAPointThatIsNotANumber_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => RenderMap(parameters => parameters.Add(m => m.Lines, [new OmniMapLine { Points = [Brussels] }])));
        Assert.Throws<ArgumentException>(() => RenderMap(parameters => parameters.Add(m => m.Lines, [Line(width: 0)])));
        Assert.Throws<ArgumentException>(() => RenderMap(parameters => parameters.Add(m => m.Lines, [Line(width: 41)])));
        Assert.Throws<ArgumentException>(() => RenderMap(parameters => parameters.Add(m => m.Lines, [Line(width: double.NaN)])));
        Assert.Throws<ArgumentException>(() => RenderMap(parameters => parameters.Add(m => m.Lines, [new OmniMapLine { Points = [Brussels, new(double.NaN, 4)] }])));
    }

    [Fact]
    public void AMarkerWithoutALabel_OrWithAPointThatIsNotANumber_IsRefused_AndNullListsToo()
    {
        Assert.Throws<ArgumentException>(() => RenderMap(parameters => parameters.Add(m => m.Markers, [Marker(" ")])));
        Assert.Throws<ArgumentException>(() => RenderMap(parameters => parameters.Add(m => m.Markers, [new OmniMapMarker { Point = new(1, double.NaN), Label = "x" }])));
        Assert.Throws<ArgumentNullException>(() => RenderMap(parameters => parameters.Add(m => m.Lines, null!)));
        Assert.Throws<ArgumentNullException>(() => RenderMap(parameters => parameters.Add(m => m.Markers, null!)));
    }

    [Theory]
    [InlineData("https://tile.example/{z}/{x}/{y}.png")]
    [InlineData("http://localhost:8080/{z}/{y}/{x}")]
    [InlineData("/tiles/{z}/{x}/{y}.png")]
    public void AValidTileTemplate_IsGivenToTheScript(string template)
    {
        RenderMap(parameters => parameters.Add(m => m.TileUrl, template));

        Assert.Equal(template, LastUpdate()[1]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("tile.example/{z}/{x}/{y}.png")]
    [InlineData("//tile.example/{z}/{x}/{y}.png")]
    [InlineData("javascript:alert(1)//{z}/{x}/{y}")]
    [InlineData("https://tile.example/{z}/{x}.png")]
    [InlineData("https://tile.example/{z}/{x}/{y}/{s}.png")]
    [InlineData("https://tile.example/{z}/{x}/{y}.png?k={key")]
    [InlineData("https://tile.example/{z}/{x}/{y} .png")]
    [InlineData("https://tile.example/{z}/{x}/{y}.png\" onerror=\"x")]
    public void AnInvalidTileTemplate_IsRefused(string template)
        => Assert.Throws<ArgumentException>(() => RenderMap(parameters => parameters.Add(m => m.TileUrl, template)));

    // ----- What the script is told -------------------------------------------------------------------

    /// <summary>Renders a map of one default line, and returns the list it was given.</summary>
    private (OmniMapLine[] Lines, IRenderedComponent<OmniMap> Map) RenderOneLine()
    {
        var lines = new[] { Line() };
        return (lines, RenderMap(parameters => parameters.Add(m => m.Lines, lines)));
    }

    [Fact]
    public void ARenderWithNothingChanged_TellsTheScriptNothing()
    {
        var (lines, map) = RenderOneLine();

        map.Render(parameters => parameters.Add(m => m.Lines, lines).Add(m => m.Label, "Autre"));

        Assert.Single(MapModule.Invocations["update"]);
    }

    [Fact]
    public void ANewContent_IsGiven_AndFramedWithFitToContent()
    {
        var map = RenderMap(parameters => parameters.Add(m => m.FitToContent, true).Add(m => m.Lines, [Line()]));
        Assert.Equal(true, LastUpdate()[7]);

        map.Render(parameters => parameters.Add(m => m.FitToContent, true).Add(m => m.Lines, [Line(), Line()]));

        Assert.Equal(2, MapModule.Invocations["update"].Count);
        Assert.Equal(2, ((double[][])LastUpdate()[4]!).Length);
        Assert.Null(LastUpdate()[6]);
        Assert.Equal(true, LastUpdate()[7]);
    }

    [Fact]
    public void ANewContent_WithoutFitToContent_IsGivenWithoutMovingTheView()
    {
        var map = RenderMap(parameters => parameters.Add(m => m.Markers, [Marker("A")]));

        map.Render(parameters => parameters.Add(m => m.Markers, [Marker("A"), Marker("B")]));

        Assert.Equal(2, ((double[][])LastUpdate()[5]!).Length);
        Assert.Null(LastUpdate()[6]);
        Assert.Equal(false, LastUpdate()[7]);
    }

    [Fact]
    public void TurningFitToContentOn_FramesTheContentAlreadyThere()
    {
        var (lines, map) = RenderOneLine();

        map.Render(parameters => parameters.Add(m => m.Lines, lines).Add(m => m.FitToContent, true));

        Assert.Equal(true, LastUpdate()[7]);
        Assert.Null(LastUpdate()[4]);
    }

    [Fact]
    public void ACentreOrZoomTheHostSets_MovesTheView()
    {
        var map = RenderMap();

        map.Render(parameters => parameters.Add(m => m.Center, new OmniGeoPoint(48.8566, 2.3522)).Add(m => m.Zoom, 11));

        Assert.Equal([48.8566, 2.3522, 11d], (double[])LastUpdate()[6]!);
        Assert.Null(LastUpdate()[4]);
    }

    [Fact]
    public async Task TheViewTheReaderMovedTo_IsBound_AndTheEchoOfTheHostMovesNothing()
    {
        var centres = new List<OmniGeoPoint>();
        var zooms = new List<int>();
        var map = RenderMap(parameters => parameters
            .Add(m => m.CenterChanged, centre => centres.Add(centre))
            .Add(m => m.ZoomChanged, zoom => zooms.Add(zoom)));

        await map.InvokeAsync(() => map.Instance.HandleViewChangedAsync(50.9, 4.4, 15));

        Assert.Equal([new OmniGeoPoint(50.9, 4.4)], centres);
        Assert.Equal([15], zooms);
        // The bound host gives the same view back: nothing to tell the script.
        map.Render(parameters => parameters.Add(m => m.Center, new OmniGeoPoint(50.9, 4.4)).Add(m => m.Zoom, 15));
        Assert.Single(MapModule.Invocations["update"]);
        Assert.False(map.FindAll(".omni-map__controls button")[0].HasAttribute("disabled"));
    }

    [Fact]
    public async Task OnlyWhatMoved_IsRaised()
    {
        var centres = 0;
        var zooms = 0;
        var map = RenderMap(parameters => parameters
            .Add(m => m.CenterChanged, _ => centres++)
            .Add(m => m.ZoomChanged, _ => zooms++));

        await map.InvokeAsync(() => map.Instance.HandleViewChangedAsync(50.8466, 4.3528, 13));
        Assert.Equal((0, 0), (centres, zooms));

        await map.InvokeAsync(() => map.Instance.HandleViewChangedAsync(50.8466, 4.3528, 14));
        Assert.Equal((0, 1), (centres, zooms));

        await map.InvokeAsync(() => map.Instance.HandleViewChangedAsync(51, 4.3528, 14));
        Assert.Equal((1, 1), (centres, zooms));
    }

    [Fact]
    public async Task AClickOnTheMap_RaisesOnMapClick_WithItsPlace()
    {
        var clicks = new List<OmniGeoPoint>();
        var map = RenderMap(parameters => parameters.Add(m => m.OnMapClick, point => clicks.Add(point)));

        await map.InvokeAsync(() => map.Instance.HandleMapClickAsync(50.85, 4.36));

        Assert.Equal([new OmniGeoPoint(50.85, 4.36)], clicks);
    }

    [Fact]
    public void AMarkerPressed_RaisesOnMarkerClick_WithThatMarker()
    {
        var pressed = new List<OmniMapMarker>();
        var start = Marker("Départ", OmniMapMarkerKind.Start);
        var end = Marker("Arrivée", OmniMapMarkerKind.End);
        var map = RenderMap(parameters => parameters.Add(m => m.Markers, [start, end]).Add(m => m.OnMarkerClick, marker => pressed.Add(marker)));

        map.FindAll("button.omni-map__marker")[1].Click();

        Assert.Same(end, Assert.Single(pressed));
    }

    [Fact]
    public async Task TheZoomButtons_AskTheScriptToZoomAroundTheCentre()
    {
        var map = RenderMap();

        map.FindAll(".omni-map__controls button")[0].Click();
        map.FindAll(".omni-map__controls button")[1].Click();
        await map.Instance.ZoomInAsync();

        Assert.Equal([1, -1, 1], MapModule.Invocations["zoomBy"].Select(call => (int)call.Arguments[1]!));
    }

    [Fact]
    public async Task Dispose_DetachesTheScript_AndAZoomAfterwardsIsRefused()
    {
        var map = RenderMap();

        await map.Instance.DisposeAsync();

        Assert.Single(MapModule.Invocations["detach"]);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => map.Instance.ZoomOutAsync());
        await map.InvokeAsync(() => map.Instance.HandleMapClickAsync(1, 2));
    }

    [Fact]
    public void TheColourCheck_TakesLiteralColoursOnly()
    {
        Assert.True(MapInput.IsColor("#abc"));
        Assert.True(MapInput.IsColor("rgb(10%, 20%, 30%)"));
        Assert.False(MapInput.IsColor(null));
        Assert.False(MapInput.IsColor("currentColor"));
        Assert.False(MapInput.IsColor(new string('1', 90)));
    }
}
