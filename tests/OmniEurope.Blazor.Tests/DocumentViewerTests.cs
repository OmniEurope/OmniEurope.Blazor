using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniDocumentViewer: the three sources of a page and their validation, the HTML sanitised again, the fields of an HTML
/// page (highlight, focus, tooltip, label lookup, click and Enter), the zoom and its limits, the fit width, the toolbar and
/// the way the component drives its script.
/// </summary>
public sealed class DocumentViewerTests : OmniBunitContext
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==");

    private readonly BunitJSModuleInterop _script;

    public DocumentViewerTests()
    {
        _script = JSInterop.SetupModule(OmniModules.DocumentViewer);
    }

    private static OmniDocumentPage Html(string html, int number = 1) => new(number, Html: html);

    private IRenderedComponent<OmniDocumentViewer> RenderViewer(
        IReadOnlyList<OmniDocumentPage> pages,
        Action<ComponentParameterCollectionBuilder<OmniDocumentViewer>>? extra = null) =>
        Render<OmniDocumentViewer>(parameters =>
        {
            parameters.Add(viewer => viewer.Pages, pages);
            extra?.Invoke(parameters);
        });

    private static IElement Field(IRenderedComponent<OmniDocumentViewer> viewer, string tag = "name") =>
        viewer.Find($"[data-oe-field='{tag}']");

    /// <summary>The bridge the script calls, as it was given to <c>attach</c>.</summary>
    private DocumentViewerInteropBridge Bridge() =>
        ((DotNetObjectReference<DocumentViewerInteropBridge>)_script.Invocations["attach"][0].Arguments[1]!).Value;

    private Task Report(IRenderedComponent<OmniDocumentViewer> viewer, double zoom, int page) =>
        viewer.InvokeAsync(() => Bridge().OnViewChangedAsync(zoom, page));

    [Fact]
    public void ImageUrl_IsDrawnAsAnImage_NamedByItsPage()
    {
        var viewer = RenderViewer([new(1, ImageUrl: "  /pages/1.png  "), new(2, ImageUrl: "https://files.example/2.png")]);

        var pages = viewer.FindAll(".omni-document-viewer__page");
        Assert.Equal(2, pages.Count);
        Assert.All(pages, page => Assert.Contains("omni-document-viewer__page--image", page.ClassList));
        Assert.Equal("group", pages[0].GetAttribute("role"));
        Assert.Equal("Page 1 sur 2", pages[0].GetAttribute("aria-label"));
        var image = pages[0].QuerySelector("img")!;
        Assert.Equal("/pages/1.png", image.GetAttribute("src"));
        Assert.Equal("Page 1 sur 2", image.GetAttribute("alt"));
        Assert.Equal("https://files.example/2.png", pages[1].QuerySelector("img")!.GetAttribute("src"));
        Assert.Equal("Page 2 sur 2", pages[1].GetAttribute("aria-label"));
    }

    [Fact]
    public void PngBytes_AreDrawnAsADataAddress()
    {
        var viewer = RenderViewer([new(1, Image: Png)]);

        Assert.Equal("data:image/png;base64," + Convert.ToBase64String(Png), viewer.Find("img").GetAttribute("src"));
    }

    [Fact]
    public void Html_IsDrawnInsideThePage_AndAnEmptyHtmlIsAnEmptyPage()
    {
        var viewer = RenderViewer([Html("<h1>Contract</h1><p>Between <b>us</b>.</p>"), Html(string.Empty, 2)]);

        var pages = viewer.FindAll(".omni-document-viewer__page--html");
        Assert.Equal(2, pages.Count);
        Assert.Equal("Contract", pages[0].QuerySelector(".omni-document-viewer__content h1")!.TextContent);
        Assert.Equal("us", pages[0].QuerySelector("p b")!.TextContent);
        Assert.Equal(string.Empty, pages[1].QuerySelector(".omni-document-viewer__content")!.TextContent);
    }

    [Fact]
    public void Html_IsSanitisedAgain_ScriptsHandlersAndScriptAddressesNeverReachThePage()
    {
        var viewer = RenderViewer([Html(
            "<p onclick=\"evil()\">Hello <b>there</b></p><script>alert(1)</script>" +
            "<a href=\"javascript:alert(1)\" onmouseover=\"evil()\">bad</a><a href=\"https://example.test/\">good</a>" +
            "<img src=\"x\" onerror=\"evil()\"><iframe src=\"https://evil.test/\"></iframe><style>p{color:red}</style><p style=\"color:red\">styled</p>")]);

        var markup = viewer.Find(".omni-document-viewer__content").InnerHtml;
        Assert.DoesNotContain("<script", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert(1)", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("onclick", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onmouseover", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<style", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", markup, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(viewer.FindAll(".omni-document-viewer__content img"));
        Assert.Equal("https://example.test/", viewer.Find(".omni-document-viewer__content a[href]").GetAttribute("href"));
        Assert.Contains("Hello", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Text_IsNeverReadAsMarkup()
    {
        var viewer = RenderViewer([Html("<p>1 &lt; 2 &amp;&amp; &lt;b&gt;bold&lt;/b&gt;</p>")]);

        Assert.Equal("1 < 2 && <b>bold</b>", viewer.Find(".omni-document-viewer__content p").TextContent);
        Assert.Empty(viewer.FindAll(".omni-document-viewer__content b"));
    }

    [Fact]
    public void SanitizerPolicy_WidensTheAllowList_ButNeverPastWhatNoPolicyCanAllow()
    {
        var pages = new[] { Html("<p class=\"note\">Seal <img src=\"/seal.png\" alt=\"seal\"></p>") };
        Assert.Empty(RenderViewer(pages).FindAll(".omni-document-viewer__content img"));

        var policy = new OmniHtmlSanitizerPolicy
        {
            AdditionalTags = ["img"],
            AdditionalTagAttributes = new Dictionary<string, IReadOnlyList<string>> { ["img"] = ["src", "alt"] },
            AdditionalCssClasses = ["note"]
        };
        var viewer = RenderViewer(pages, parameters => parameters.Add(component => component.SanitizerPolicy, policy));
        Assert.Equal("/seal.png", viewer.Find(".omni-document-viewer__content img").GetAttribute("src"));
        Assert.Contains("note", viewer.Find(".omni-document-viewer__content p").ClassList);

        var forbidden = new OmniHtmlSanitizerPolicy { AdditionalTags = ["script"] };
        Assert.Throws<ArgumentException>(() => RenderViewer(pages, parameters => parameters.Add(component => component.SanitizerPolicy, forbidden)));
    }

    [Fact]
    public void ChangingTheSanitizerPolicy_ReadsThePagesAgain()
    {
        var pages = new[] { Html("<p>Seal <img src=\"/seal.png\"></p>") };
        var viewer = RenderViewer(pages);
        Assert.Empty(viewer.FindAll(".omni-document-viewer__content img"));

        viewer.Render(parameters => parameters.Add(component => component.SanitizerPolicy, new OmniHtmlSanitizerPolicy
        {
            AdditionalTags = ["img"],
            AdditionalTagAttributes = new Dictionary<string, IReadOnlyList<string>> { ["img"] = ["src"] }
        }));

        Assert.Single(viewer.FindAll(".omni-document-viewer__content img"));
    }

    [Fact]
    public void PageSizes_AreHandedToTheScript_InPoints()
    {
        var viewer = RenderViewer([new(1, Html: "<p>A</p>", WidthPt: 595.3, HeightPt: 841.9), new(2, ImageUrl: "/b.png")]);

        var pages = viewer.FindAll(".omni-document-viewer__page");
        Assert.Equal("595.3", pages[0].GetAttribute("data-width-pt"));
        Assert.Equal("841.9", pages[0].GetAttribute("data-height-pt"));
        Assert.False(pages[1].HasAttribute("data-width-pt"));
        Assert.False(pages[1].HasAttribute("data-height-pt"));
    }

    [Fact]
    public void NoPage_SaysSo_WithoutAnIndicator()
    {
        var viewer = RenderViewer([]);

        Assert.Equal("Aucune page à afficher.", viewer.Find(".omni-document-viewer__empty").TextContent);
        Assert.Empty(viewer.FindAll(".omni-document-viewer__page"));
        Assert.Empty(viewer.FindAll(".omni-document-viewer__indicator"));
    }

    [Theory]
    [InlineData(1, null, null, null, "exactly one of")]
    [InlineData(1, "", null, null, "exactly one of")]
    [InlineData(1, "   ", null, null, "exactly one of")]
    [InlineData(1, "/a.png", null, "<p></p>", "exactly one of")]
    [InlineData(0, "/a.png", null, null, "below 1")]
    [InlineData(-3, null, null, "<p></p>", "below 1")]
    public void InvalidPage_ThrowsAClearException(int number, string? url, byte[]? image, string? html, string expected)
    {
        var exception = Assert.Throws<ArgumentException>(() => RenderViewer([new(number, url, image, html)]));

        Assert.Contains(expected, exception.Message, StringComparison.Ordinal);
        Assert.Equal("Pages", exception.ParamName);
    }

    [Fact]
    public void InvalidPage_BytesAndAddressTogether_OrNotAPngOrASizeBelowZero_Throw()
    {
        Assert.Contains("exactly one", Assert.Throws<ArgumentException>(() => RenderViewer([new(1, ImageUrl: "/a.png", Image: Png)])).Message, StringComparison.Ordinal);
        Assert.Contains("not a PNG", Assert.Throws<ArgumentException>(() => RenderViewer([new(1, Image: [1, 2, 3])])).Message, StringComparison.Ordinal);
        Assert.Contains("not a PNG", Assert.Throws<ArgumentException>(() => RenderViewer([new(1, Image: [])])).Message, StringComparison.Ordinal);
        Assert.Contains("WidthPt", Assert.Throws<ArgumentException>(() => RenderViewer([new(1, Html: "", WidthPt: 0)])).Message, StringComparison.Ordinal);
        Assert.Contains("WidthPt", Assert.Throws<ArgumentException>(() => RenderViewer([new(1, Html: "", WidthPt: double.NaN)])).Message, StringComparison.Ordinal);
        Assert.Contains("HeightPt", Assert.Throws<ArgumentException>(() => RenderViewer([new(1, Html: "", HeightPt: -1)])).Message, StringComparison.Ordinal);
        Assert.Contains("HeightPt", Assert.Throws<ArgumentException>(() => RenderViewer([new(1, Html: "", HeightPt: double.PositiveInfinity)])).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NullPages_AndANullPage_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => RenderViewer(null!));
        Assert.Contains("null page", Assert.Throws<ArgumentException>(() => RenderViewer([null!])).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Fields_AreHighlighted_Focusable_AndTippedWithTheirLabel()
    {
        var viewer = RenderViewer(
            [Html("<p>Dear <span data-oe-field=\"name\">Anne</span>, <span data-oe-field=\"date\">today</span> and <span data-oe-field=\"other\">x</span>.</p>")],
            parameters => parameters.Add(component => component.FieldLabels, new Dictionary<string, string> { ["name"] = "Customer name", ["other"] = "  " }));

        var name = Field(viewer);
        Assert.Contains("omni-document-viewer__field", name.ClassList);
        Assert.Equal("0", name.GetAttribute("tabindex"));
        Assert.Equal("Customer name", name.GetAttribute("data-omni-tip"));
        Assert.Equal("Customer name", name.GetAttribute("aria-description"));
        Assert.Equal("Anne", name.TextContent);
        // No label for the tag, or a blank one: the tag itself.
        Assert.Equal("date", Field(viewer, "date").GetAttribute("data-omni-tip"));
        Assert.Equal("other", Field(viewer, "other").GetAttribute("data-omni-tip"));
        Assert.Equal(3, viewer.FindAll(".omni-document-viewer__field").Count);
    }

    [Fact]
    public void Fields_WithoutLabels_ShowTheirTag()
    {
        var viewer = RenderViewer([Html("<span data-oe-field=\"city\">Lyon</span>")]);

        Assert.Equal("city", Field(viewer, "city").GetAttribute("data-omni-tip"));
    }

    [Fact]
    public void AnElementOfTheDocument_ThatIsNotAField_IsLeftAlone_AndABlankTagIsNotAField()
    {
        var viewer = RenderViewer([Html("<p>Plain</p><span data-oe-field=\"\">blank</span><span data-oe-field=\"  \">spaces</span>")]);

        Assert.Empty(viewer.FindAll(".omni-document-viewer__field"));
        Assert.False(viewer.Find(".omni-document-viewer__content p").HasAttribute("tabindex"));
        Assert.Empty(viewer.FindAll("[data-omni-tip]"));
    }

    [Fact]
    public void AFieldKeepsTheClassesOfTheDocument_AndGivesUpItsOwnTabindexAndTip()
    {
        var policy = new OmniHtmlSanitizerPolicy { AdditionalCssClasses = ["note"], AdditionalAttributes = ["tabindex", "title"] };
        var viewer = RenderViewer(
            [Html("<span class=\"note\" tabindex=\"-1\" title=\"mine\" data-oe-field=\"name\">Anne</span>")],
            parameters => parameters.Add(component => component.SanitizerPolicy, policy));

        var name = Field(viewer);
        Assert.Equal("note omni-document-viewer__field", name.GetAttribute("class"));
        Assert.Equal("0", name.GetAttribute("tabindex"));
        Assert.Equal("mine", name.GetAttribute("title"));
        Assert.Single(name.Attributes, attribute => attribute.Name == "tabindex");
    }

    [Fact]
    public void FieldsInATable_AreKept()
    {
        var viewer = RenderViewer([Html("<table><tbody><tr><td data-oe-field=\"total\">12</td><td>EUR</td></tr></tbody></table>")]);

        Assert.Equal("12", viewer.Find("td[data-oe-field]").TextContent);
        Assert.Contains("omni-document-viewer__field", viewer.Find("td[data-oe-field]").ClassList);
    }

    [Fact]
    public void ImagePages_HaveNoFields()
    {
        var viewer = RenderViewer([new(1, ImageUrl: "/a.png")]);

        Assert.Empty(viewer.FindAll("[data-oe-field]"));
        Assert.Empty(viewer.FindAll(".omni-document-viewer__field"));
    }

    [Fact]
    public void HighlightFields_False_DrawsAPlainDocument_AndRaisesNothing()
    {
        var clicked = new List<string>();
        var viewer = RenderViewer(
            [Html("<span data-oe-field=\"name\">Anne</span>")],
            parameters => parameters
                .Add(component => component.HighlightFields, false)
                .Add(component => component.OnFieldClick, tag => clicked.Add(tag)));

        var name = Field(viewer);
        Assert.False(name.ClassList.Contains("omni-document-viewer__field"));
        Assert.False(name.HasAttribute("tabindex"));
        Assert.False(name.HasAttribute("data-omni-tip"));
        // Nothing listens on the element: bUnit refuses the event, which is the point.
        Assert.Throws<MissingEventHandlerException>(() => name.Click());
        Assert.Throws<MissingEventHandlerException>(() => name.KeyDown(new KeyboardEventArgs { Key = "Enter" }));
        Assert.Empty(clicked);
    }

    [Fact]
    public void Click_AndEnter_OnAField_RaiseItsTag_AndNothingElseDoes()
    {
        var raised = new List<string>();
        var viewer = RenderViewer(
            [Html("<p>Hi <span data-oe-field=\"name\">Anne</span> <span data-oe-field=\"date\">today</span></p>")],
            parameters => parameters.Add(component => component.OnFieldClick, tag => raised.Add(tag)));

        Field(viewer).Click();
        Field(viewer, "date").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Field(viewer, "date").KeyDown(new KeyboardEventArgs { Key = "a" });
        Field(viewer, "date").KeyDown(new KeyboardEventArgs { Key = " " });
        Assert.Throws<MissingEventHandlerException>(() => viewer.Find(".omni-document-viewer__content p").Click());

        Assert.Equal(["name", "date"], raised);
    }

    [Fact]
    public void ANestedField_RaisesOnlyItself()
    {
        var raised = new List<string>();
        var viewer = RenderViewer(
            [Html("<span data-oe-field=\"outer\">a <span data-oe-field=\"inner\">b</span></span>")],
            parameters => parameters.Add(component => component.OnFieldClick, tag => raised.Add(tag)));

        Field(viewer, "inner").Click();
        Field(viewer, "inner").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(["inner", "inner"], raised);
    }

    [Fact]
    public void ChangingTheLabels_RedrawsTheTips()
    {
        var viewer = RenderViewer([Html("<span data-oe-field=\"name\">Anne</span>")]);
        Assert.Equal("name", Field(viewer).GetAttribute("data-omni-tip"));

        viewer.Render(parameters => parameters.Add(component => component.FieldLabels, new Dictionary<string, string> { ["name"] = "Name" }));

        Assert.Equal("Name", Field(viewer).GetAttribute("data-omni-tip"));
    }

    [Fact]
    public void Zoom_StartsAtTheGivenValue_AndFollowsTheButtonsInSteps()
    {
        var zooms = new List<double>();
        var viewer = RenderViewer([Html("<p>A</p>")], parameters => parameters
            .Add(component => component.Fit, OmniDocumentFit.None)
            .Add(component => component.Zoom, 1.0)
            .Add(component => component.ZoomChanged, zoom => zooms.Add(zoom)));
        Assert.StartsWith("100", viewer.Find(".omni-document-viewer__zoom").TextContent, StringComparison.Ordinal);

        viewer.Find(".omni-document-viewer__zoom-in").Click();
        viewer.Find(".omni-document-viewer__zoom-in").Click();
        viewer.Find(".omni-document-viewer__zoom-out").Click();

        Assert.Equal([1.25, 1.5, 1.25], zooms);
        Assert.StartsWith("125", viewer.Find(".omni-document-viewer__zoom").TextContent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(99, 4)]
    [InlineData(4, 4)]
    [InlineData(0.01, 0.25)]
    [InlineData(-2, 0.25)]
    [InlineData(double.NaN, 1)]
    [InlineData(double.PositiveInfinity, 4)]
    public void Zoom_IsClampedToTheSupportedRange(double given, double shown)
    {
        var viewer = RenderViewer([Html("<p>A</p>")], parameters => parameters
            .Add(component => component.Fit, OmniDocumentFit.None)
            .Add(component => component.Zoom, given));

        Assert.StartsWith(((int)(shown * 100)).ToString(System.Globalization.CultureInfo.InvariantCulture), viewer.Find(".omni-document-viewer__zoom").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ZoomButtons_AreDisabledAtTheLimits()
    {
        var atMaximum = RenderViewer([Html("<p>A</p>")], parameters => parameters.Add(component => component.Fit, OmniDocumentFit.None).Add(component => component.Zoom, 4.0));
        Assert.True(atMaximum.Find(".omni-document-viewer__zoom-in").HasAttribute("disabled"));
        Assert.False(atMaximum.Find(".omni-document-viewer__zoom-out").HasAttribute("disabled"));

        var atMinimum = RenderViewer([Html("<p>A</p>")], parameters => parameters.Add(component => component.Fit, OmniDocumentFit.None).Add(component => component.Zoom, 0.25));
        Assert.True(atMinimum.Find(".omni-document-viewer__zoom-out").HasAttribute("disabled"));
        Assert.False(atMinimum.Find(".omni-document-viewer__zoom-in").HasAttribute("disabled"));
    }

    [Fact]
    public void Zoom_StepsLandOnQuarters_FromAnyStartAndStopAtTheLimits()
    {
        var zooms = new List<double>();
        var viewer = RenderViewer([Html("<p>A</p>")], parameters => parameters
            .Add(component => component.Fit, OmniDocumentFit.None)
            .Add(component => component.Zoom, 0.87)
            .Add(component => component.ZoomChanged, zoom => zooms.Add(zoom)));

        viewer.Find(".omni-document-viewer__zoom-in").Click();
        viewer.Render(parameters => parameters.Add(component => component.Zoom, 3.9));
        viewer.Find(".omni-document-viewer__zoom-in").Click();
        viewer.Render(parameters => parameters.Add(component => component.Zoom, 0.3));
        viewer.Find(".omni-document-viewer__zoom-out").Click();

        Assert.Equal([1.0, 4.0, 0.25], zooms);
    }

    [Fact]
    public void ZoomingByTheToolbar_EndsTheFitWidth_AndFitWidthStartsItAgain()
    {
        var fits = new List<OmniDocumentFit>();
        var viewer = RenderViewer([Html("<p>A</p>")], parameters => parameters.Add(component => component.FitChanged, fit => fits.Add(fit)));
        Assert.Equal("true", viewer.Find(".omni-document-viewer__fit").GetAttribute("aria-pressed"));

        viewer.Find(".omni-document-viewer__zoom-in").Click();
        Assert.Equal("false", viewer.Find(".omni-document-viewer__fit").GetAttribute("aria-pressed"));
        viewer.Find(".omni-document-viewer__zoom-in").Click();
        viewer.Find(".omni-document-viewer__fit").Click();
        viewer.Find(".omni-document-viewer__fit").Click();

        Assert.Equal([OmniDocumentFit.None, OmniDocumentFit.Width], fits);
        Assert.Equal("true", viewer.Find(".omni-document-viewer__fit").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void AZoomTheHostChanges_IsTaken_AndEndsTheFit_ButTheSameZoomDoesNot()
    {
        var viewer = RenderViewer([Html("<p>A</p>")]);
        Assert.Equal("true", viewer.Find(".omni-document-viewer__fit").GetAttribute("aria-pressed"));

        viewer.Render(parameters => parameters.Add(component => component.Zoom, 1.0));
        Assert.Equal("true", viewer.Find(".omni-document-viewer__fit").GetAttribute("aria-pressed"));

        viewer.Render(parameters => parameters.Add(component => component.Zoom, 2.0));
        Assert.Equal("false", viewer.Find(".omni-document-viewer__fit").GetAttribute("aria-pressed"));
        Assert.StartsWith("200", viewer.Find(".omni-document-viewer__zoom").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheFittedZoomGivenBack_ByABoundHost_DoesNotEndTheFit()
    {
        var viewer = RenderViewer([Html("<p>A</p>")]);
        await Report(viewer, 0.87, 1);

        viewer.Render(parameters => parameters.Add(component => component.Zoom, 0.87));

        Assert.Equal("true", viewer.Find(".omni-document-viewer__fit").GetAttribute("aria-pressed"));
        Assert.StartsWith("87", viewer.Find(".omni-document-viewer__zoom").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void AZoomThatIsNotANumber_DoesNotHideTheNextOne()
    {
        var viewer = RenderViewer([Html("<p>A</p>")], parameters => parameters.Add(component => component.Fit, OmniDocumentFit.None));

        viewer.Render(parameters => parameters.Add(component => component.Zoom, double.NaN));
        Assert.StartsWith("100", viewer.Find(".omni-document-viewer__zoom").TextContent, StringComparison.Ordinal);
        viewer.Render(parameters => parameters.Add(component => component.Zoom, double.NaN));
        viewer.Render(parameters => parameters.Add(component => component.Zoom, 2.0));

        Assert.StartsWith("200", viewer.Find(".omni-document-viewer__zoom").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void AFitTheHostChanges_IsTaken_AfterTheZoom()
    {
        var viewer = RenderViewer([Html("<p>A</p>")], parameters => parameters.Add(component => component.Fit, OmniDocumentFit.None));
        Assert.Equal("false", viewer.Find(".omni-document-viewer__fit").GetAttribute("aria-pressed"));

        viewer.Render(parameters => parameters.Add(component => component.Fit, OmniDocumentFit.Width));
        Assert.Equal("true", viewer.Find(".omni-document-viewer__fit").GetAttribute("aria-pressed"));

        viewer.Render(parameters => parameters.Add(component => component.Zoom, 2.0).Add(component => component.Fit, OmniDocumentFit.None));
        Assert.Equal("false", viewer.Find(".omni-document-viewer__fit").GetAttribute("aria-pressed"));
        viewer.Render(parameters => parameters.Add(component => component.Zoom, 3.0).Add(component => component.Fit, OmniDocumentFit.Width));
        Assert.Equal("true", viewer.Find(".omni-document-viewer__fit").GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task TheFittedZoom_ReportedByTheScript_IsShown_AndRaised()
    {
        var zooms = new List<double>();
        var viewer = RenderViewer([Html("<p>A</p>")], parameters => parameters.Add(component => component.ZoomChanged, zoom => zooms.Add(zoom)));

        await Report(viewer, 0.87, 1);
        await Report(viewer, 0.87, 1);
        await Report(viewer, 99, 1);

        Assert.Equal([0.87, 4.0], zooms);
        Assert.StartsWith("400", viewer.Find(".omni-document-viewer__zoom").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AFittedZoomInFlight_CannotUndoAZoomTheReaderJustChose()
    {
        var viewer = RenderViewer([Html("<p>A</p>")]);
        viewer.Find(".omni-document-viewer__zoom-in").Click();
        var chosen = viewer.Find(".omni-document-viewer__zoom").TextContent;

        await Report(viewer, 0.5, 1);

        Assert.Equal(chosen, viewer.Find(".omni-document-viewer__zoom").TextContent);
    }

    [Fact]
    public async Task Indicator_FollowsThePageInView_AndIgnoresAPageOutsideTheList()
    {
        var viewer = RenderViewer([Html("<p>1</p>", 1), Html("<p>2</p>", 2), Html("<p>3</p>", 7)]);
        Assert.Equal("Page 1 / 3", viewer.Find(".omni-document-viewer__indicator").TextContent);

        await Report(viewer, 1, 3);
        Assert.Equal("Page 7 / 3", viewer.Find(".omni-document-viewer__indicator").TextContent);
        await Report(viewer, 1, 0);
        await Report(viewer, 1, 9);
        Assert.Equal("Page 7 / 3", viewer.Find(".omni-document-viewer__indicator").TextContent);
    }

    [Fact]
    public async Task ReplacingThePages_KeepsThePageInViewInsideTheNewList()
    {
        var viewer = RenderViewer([Html("<p>1</p>"), Html("<p>2</p>", 2), Html("<p>3</p>", 3)]);
        await Report(viewer, 1, 3);

        viewer.Render(parameters => parameters.Add(component => component.Pages, [Html("<p>only</p>")]));
        Assert.Equal("Page 1 / 1", viewer.Find(".omni-document-viewer__indicator").TextContent);

        viewer.Render(parameters => parameters.Add(component => component.Pages, []));
        Assert.Empty(viewer.FindAll(".omni-document-viewer__indicator"));
        viewer.Render(parameters => parameters.Add(component => component.Pages, [Html("<p>back</p>")]));
        Assert.Equal("Page 1 / 1", viewer.Find(".omni-document-viewer__indicator").TextContent);
    }

    [Fact]
    public void Toolbar_NamesItsButtons_AndCanBeHidden()
    {
        var viewer = RenderViewer([Html("<p>A</p>")]);

        var toolbar = viewer.Find("[role=toolbar]");
        Assert.Equal("Barre d'outils du document", toolbar.GetAttribute("aria-label"));
        Assert.Equal("Zoom arrière", viewer.Find(".omni-document-viewer__zoom-out").GetAttribute("aria-label"));
        Assert.Equal("Zoom avant", viewer.Find(".omni-document-viewer__zoom-in").GetAttribute("aria-label"));
        Assert.Equal("Ajuster à la largeur", viewer.Find(".omni-document-viewer__fit").GetAttribute("aria-label"));
        Assert.Equal(viewer.Find(".omni-document-viewer__viewport").Id, toolbar.GetAttribute("aria-controls"));

        var bare = RenderViewer([Html("<p>A</p>")], parameters => parameters.Add(component => component.ShowToolbar, false));
        Assert.Empty(bare.FindAll("[role=toolbar]"));
        Assert.Single(bare.FindAll(".omni-document-viewer__page"));
    }

    [Fact]
    public void Viewport_IsARegionWithAFocusStop_AndTakesIdLabelClassAndAttributes()
    {
        var viewer = RenderViewer([Html("<p>A</p>")], parameters => parameters
            .Add(component => component.Id, "contract")
            .Add(component => component.Class, "mine")
            .Add(component => component.Label, "Contract")
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object> { ["data-case"] = "42" }));

        var viewport = viewer.Find(".omni-document-viewer__viewport");
        Assert.Equal("contract", viewport.Id);
        Assert.Equal("region", viewport.GetAttribute("role"));
        Assert.Equal("0", viewport.GetAttribute("tabindex"));
        Assert.Equal("Contract", viewport.GetAttribute("aria-label"));
        Assert.Equal("42", viewport.GetAttribute("data-case"));
        Assert.Contains("mine", viewer.Find(".omni-document-viewer").ClassList);

        var unnamed = RenderViewer([Html("<p>A</p>")]);
        Assert.Equal("Document", unnamed.Find(".omni-document-viewer__viewport").GetAttribute("aria-label"));
        Assert.StartsWith("omni-document-viewer-", unnamed.Find(".omni-document-viewer__viewport").Id, StringComparison.Ordinal);
    }

    [Fact]
    public void ItRefusesAStyleAttribute_OfTheHost()
    {
        Assert.Throws<InvalidOperationException>(() => RenderViewer([Html("<p>A</p>")], parameters => parameters
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object> { ["style"] = "color:red" })));
    }

    [Fact]
    public void Script_IsAttachedOnce_WithTheZoomTheFitAndTheLimits_AndTheTooltipsAreOn()
    {
        var viewer = RenderViewer([Html("<p>A</p>")], parameters => parameters.Add(component => component.Zoom, 1.5));

        var attach = Assert.Single(_script.Invocations["attach"]);
        Assert.IsType<DotNetObjectReference<DocumentViewerInteropBridge>>(attach.Arguments[1]);
        Assert.Equal(1.5, attach.Arguments[2]);
        Assert.Equal(true, attach.Arguments[3]);
        Assert.Equal(0.25, attach.Arguments[4]);
        Assert.Equal(4.0, attach.Arguments[5]);
        viewer.Render();
        Assert.Single(_script.Invocations["attach"]);
        Assert.Empty(_script.Invocations["update"]);
    }

    [Fact]
    public void Script_IsToldWhatChanged_AndOnlyThen()
    {
        var viewer = RenderViewer([Html("<p>A</p>")], parameters => parameters.Add(component => component.Fit, OmniDocumentFit.None));

        viewer.Render();
        Assert.Empty(_script.Invocations["update"]);

        viewer.Find(".omni-document-viewer__zoom-in").Click();
        var zoom = Assert.Single(_script.Invocations["update"]);
        Assert.Equal(1.25, zoom.Arguments[1]);
        Assert.Equal(false, zoom.Arguments[2]);

        viewer.Find(".omni-document-viewer__fit").Click();
        Assert.Equal(true, _script.Invocations["update"][^1].Arguments[2]);

        viewer.Render(parameters => parameters.Add(component => component.Pages, [Html("<p>B</p>")]));
        Assert.Equal(3, _script.Invocations["update"].Count);
    }

    [Fact]
    public async Task Dispose_DetachesTheScript_AndReleasesTheModule()
    {
        var runtime = new ManualJSRuntime();
        Services.AddSingleton<IJSRuntime>(runtime);
        var viewer = RenderViewer([Html("<p>A</p>")]);
        await viewer.WaitForAssertionAsync(() => Assert.Contains("attach", runtime.Module.Calls));

        await viewer.Instance.DisposeAsync();

        Assert.Contains("detach", runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task AReportAfterDisposal_IsIgnored()
    {
        var viewer = RenderViewer([Html("<p>1</p>"), Html("<p>2</p>", 2)]);
        var bridge = Bridge();

        await viewer.Instance.DisposeAsync();
        await bridge.OnViewChangedAsync(0.5, 2);

        Assert.Equal("Page 1 / 2", viewer.Find(".omni-document-viewer__indicator").TextContent);
    }

    [Fact]
    public async Task Dispose_BeforeTheScriptArrives_ReleasesItWithoutAttaching()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var viewer = RenderViewer([Html("<p>A</p>")]);

        await viewer.Instance.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);

        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        Assert.DoesNotContain("attach", runtime.Module.Calls);
    }

    [Fact]
    public async Task Dispose_WithoutAnyRender_HasNothingToRelease()
    {
        var viewer = new OmniDocumentViewer();

        await viewer.DisposeAsync();
    }

    [Fact]
    public async Task ALostCircuit_OnImport_IsQuiet()
    {
        Services.AddSingleton<IJSRuntime>(new ManualJSRuntime { ImportFailure = new JSDisconnectedException("perdu") });
        var viewer = RenderViewer([Html("<p>A</p>")]);

        await viewer.Instance.DisposeAsync();

        Assert.Single(viewer.FindAll(".omni-document-viewer__page"));
    }

    [Fact]
    public async Task ALostCircuit_OnDetach_IsQuiet()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["detach"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var viewer = RenderViewer([Html("<p>A</p>")]);
        await viewer.WaitForAssertionAsync(() => Assert.Contains("attach", runtime.Module.Calls));

        await viewer.Instance.DisposeAsync();

        Assert.Contains("detach", runtime.Module.Calls);
    }

    [Fact]
    public async Task ALostCircuit_OnAttach_IsQuiet()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["attach"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);

        var viewer = RenderViewer([Html("<p>A</p>")]);

        await viewer.WaitForAssertionAsync(() => Assert.Contains("attach", runtime.Module.Calls));
        Assert.Single(viewer.FindAll(".omni-document-viewer__page"));
    }
}
