using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <c>ColumnSizing</c>: the grid sizes its columns to their content. The measuring and the sharing out of the
/// room run in omni-grid.js and are proven in a browser (the <c>requests</c> part of the Scripts probe) and by
/// the node tests of the sharing out; these tests pin what .NET decides and hands the script: the mode, which
/// widths count, which columns grow, wrap or are cut, which texts are measured, and that the default sizing is
/// untouched.
/// </summary>
public sealed class DataGridColumnSizingTests : OmniBunitContext
{
    private static readonly Dictionary<string, object> Content = Sizing(OmniDataGridColumnSizing.Content);

    private static readonly Dictionary<string, object> Declared = Sizing(OmniDataGridColumnSizing.Declared);

    private BunitJSModuleInterop? _module;

    // One module for the whole test: the script is loose, every call is recorded.
    private BunitJSModuleInterop Module
    {
        get
        {
            if (_module is null)
            {
                _module = JSInterop.SetupModule(OmniModules.Grid);
                _module.Mode = JSRuntimeMode.Loose;
            }

            return _module;
        }
    }

    private static Dictionary<string, object> Sizing(OmniDataGridColumnSizing sizing) =>
        new() { [nameof(OmniDataGrid<DataGridColumnSizingTestHost.Site>.ColumnSizing)] = sizing };

    private IRenderedComponent<DataGridColumnSizingTestHost> RenderSites(
        IDictionary<string, object>? grid = null,
        Action<ComponentParameterCollectionBuilder<DataGridColumnSizingTestHost>>? extra = null)
    {
        _ = Module;
        return Render<DataGridColumnSizingTestHost>(parameters =>
        {
            parameters.Add(host => host.GridAttributes, grid);
            extra?.Invoke(parameters);
        });
    }

    private static OmniDataGrid<DataGridColumnSizingTestHost.Site> GridOf(IRenderedComponent<DataGridColumnSizingTestHost> host) =>
        host.FindComponent<OmniDataGrid<DataGridColumnSizingTestHost.Site>>().Instance;

    [Fact]
    public void TheDefaultSizing_IsDeclared_AndMarksNothing()
    {
        var host = RenderSites(extra: parameters => parameters
            .Add(site => site.GrowName, true)
            .Add(site => site.WrapProject, true));

        Assert.Equal(OmniDataGridColumnSizing.Declared, GridOf(host).ColumnSizing);
        Assert.DoesNotContain("omni-data-grid--sizing-content", host.Find(".omni-data-grid").ClassName, StringComparison.Ordinal);
        Assert.False(host.Find("table").HasAttribute("data-omni-sizing"));
        // Grow and Wrap belong to content sizing: the default sizing carries no trace of them.
        Assert.Empty(host.FindAll("col[data-omni-grow], col[data-omni-wrap]"));
        Assert.Empty(host.FindAll("[class*='omni-data-grid__column--fit'], [class*='omni-data-grid__column--wrap']"));
    }

    [Fact]
    public void TheDefaultSizing_KeepsTheDeclaredWidths_AndTheirMinimum()
    {
        var host = RenderSites();

        Assert.Equal("9rem", host.Find("col[data-omni-col='Status']").GetAttribute("data-omni-width"));
        Assert.Equal("10rem", host.Find("col[data-omni-col='Since']").GetAttribute("data-omni-width"));
        Assert.Equal("calc(9rem + 9rem + 8rem + 8rem + 10rem)", Module.Invocations["applyColumns"][^1].Arguments[2]);
        Assert.Empty(Module.Invocations["setContentTexts"]);
    }

    [Fact]
    public void Content_MarksTheGridAndItsTable()
    {
        var host = RenderSites(Content);

        Assert.Equal(OmniDataGridColumnSizing.Content, GridOf(host).ColumnSizing);
        Assert.Contains("omni-data-grid--sizing-content", host.Find(".omni-data-grid").ClassName, StringComparison.Ordinal);
        Assert.Equal("content", host.Find("table").GetAttribute("data-omni-sizing"));
    }

    [Fact]
    public void Content_HidesTheRowsUntilItsWidthsAreApplied_ThenShowsThemAnywayAfterASecond()
    {
        var styles = StylesheetSource.Read();
        const string Unsized = ".omni-data-grid__table[data-omni-sizing=\"content\"]:not([data-omni-sized])";

        // The equal widths of the first paint are never seen (the script marks the table data-omni-sized),
        // the room stays kept, and a table no script sizes still shows. The probe steps it in a browser.
        Assert.Contains(Unsized + " > :is(tbody, tfoot) { animation: omni-grid-reveal 0s 1s both, omni-grid-reveal 0s 5s both; visibility: hidden; }", styles, StringComparison.Ordinal);
        // While the first rows load, the second waits (five at most) and the headers and the load bar show.
        Assert.Contains(".omni-data-grid[aria-busy=\"true\"] " + Unsized + " > :is(tbody, tfoot) { animation-play-state: paused, running; }", styles, StringComparison.Ordinal);
        Assert.Contains(Unsized + " .omni-data-grid__progress { visibility: visible; }", styles, StringComparison.Ordinal);
        Assert.Contains("@keyframes omni-grid-reveal { to { visibility: visible; } }", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_KeepsTheHeadersVisibleOnceMeasured_AndTheViewportStillWhileUnsized()
    {
        var styles = StylesheetSource.Read();
        const string Unsized = ".omni-data-grid__table[data-omni-sizing=\"content\"]:not([data-omni-sized])";

        // Only the rows and the footer are hidden: the header row is hidden before its own first measure
        // (data-omni-headed) and not after, with the same one second fallback when no script runs.
        Assert.Contains(Unsized + ":not([data-omni-headed]) > thead { animation: omni-grid-reveal 0s 1s both; visibility: hidden; }", styles, StringComparison.Ordinal);
        Assert.DoesNotContain(Unsized + " { animation", styles, StringComparison.Ordinal);
        // The viewport does not scroll while the table is unsized, and unclips on the same timeline.
        Assert.Contains(".omni-data-grid__viewport:has(> " + Unsized.TrimStart() + ") { animation: omni-grid-unclip 0s 1s both, omni-grid-unclip 0s 5s both; overflow: hidden; }", styles, StringComparison.Ordinal);
        Assert.Contains("@keyframes omni-grid-unclip { to { overflow: auto; } }", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_IgnoresTheDeclaredWidth()
    {
        var host = RenderSites(Content);

        // The columns that declared a width (status 9rem, number 8rem, date 10rem) carry none: the content decides.
        Assert.All(host.FindAll("col[data-omni-col]"), col => Assert.False(col.HasAttribute("data-omni-width"), col.OuterHtml));
        var specs = JsonSerializer.Serialize(Module.Invocations["applyColumns"][^1].Arguments[1]);
        Assert.Contains("\"key\":\"Status\",\"width\":null", specs, StringComparison.Ordinal);
        Assert.Contains("\"key\":\"Visitors\",\"width\":null", specs, StringComparison.Ordinal);
        Assert.Contains("\"key\":\"Since\",\"width\":null", specs, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_KeepsMinWidthAsAFloor_AndCountsNoAutoColumnFloor()
    {
        RenderSites(Content);

        var applied = Module.Invocations["applyColumns"][^1];
        var specs = JsonSerializer.Serialize(applied.Arguments[1]);
        Assert.Contains("\"key\":\"Name\",\"width\":null,\"minWidth\":\"9rem\"", specs, StringComparison.Ordinal);
        Assert.Contains("\"key\":\"Project\",\"width\":null,\"minWidth\":\"8rem\"", specs, StringComparison.Ordinal);
        // The table may shrink to what the content needs: a column with no minimum counts for nothing here,
        // not for the 8rem floor of an auto column, which would stop it from shrinking to its content.
        Assert.Equal("calc(9rem + 0px + 0px + 8rem + 0px)", applied.Arguments[2]);
    }

    [Fact]
    public async Task Content_AWidthTheReaderChoseStillWins()
    {
        var host = RenderSites(Content);
        Assert.Contains("Visitors", LastTexts().Keys);

        await host.InvokeAsync(() => GridOf(host).OnColumnResizedAsync("Visitors", 123d));

        Assert.Equal("123px", host.Find("col[data-omni-col='Visitors']").GetAttribute("data-omni-width"));
        // A column the reader sized is no longer measured: its texts are not handed to the script any more.
        Assert.DoesNotContain("Visitors", LastTexts().Keys);
    }

    [Fact]
    public void Content_MarksTheColumnsThatGrowOrWrap()
    {
        var host = RenderSites(Content, parameters => parameters
            .Add(site => site.GrowName, true)
            .Add(site => site.WrapProject, true));

        Assert.Equal("true", host.Find("col[data-omni-col='Name']").GetAttribute("data-omni-grow"));
        Assert.False(host.Find("col[data-omni-col='Status']").HasAttribute("data-omni-grow"));
        Assert.Equal("true", host.Find("col[data-omni-col='Project']").GetAttribute("data-omni-wrap"));
        // A column measured to its content keeps its header and cells on one line; a wrapping one does not.
        Assert.Contains("omni-data-grid__column--fit", host.Find("th[data-omni-col='Status']").ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-data-grid__column--fit", host.Find("td[data-omni-col='Visitors']").ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-data-grid__column--wrap", host.Find("th[data-omni-col='Project']").ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-data-grid__column--wrap", host.Find("td[data-omni-col='Project']").ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("omni-data-grid__column--fit", host.Find("th[data-omni-col='Project']").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_ATruncateColumnKeepsTheEllipsisOfTheDefaultSizing()
    {
        var host = RenderSites(Content, parameters => parameters.Add(site => site.TruncateProject, true));

        Assert.Equal("true", host.Find("col[data-omni-col='Project']").GetAttribute("data-omni-truncate"));
        var header = host.Find("th[data-omni-col='Project']").ClassName;
        Assert.DoesNotContain("omni-data-grid__column--fit", header, StringComparison.Ordinal);
        Assert.DoesNotContain("omni-data-grid__column--wrap", header, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSizingChangedAtRunTime_FollowsInTheMarkup()
    {
        var host = RenderSites(Declared);
        Assert.False(host.Find("table").HasAttribute("data-omni-sizing"));
        Assert.Equal("9rem", host.Find("col[data-omni-col='Status']").GetAttribute("data-omni-width"));

        host.Render(parameters => parameters.Add(site => site.GridAttributes, Content));
        Assert.Equal("content", host.Find("table").GetAttribute("data-omni-sizing"));
        Assert.False(host.Find("col[data-omni-col='Status']").HasAttribute("data-omni-width"));

        host.Render(parameters => parameters.Add(site => site.GridAttributes, Declared));
        Assert.False(host.Find("table").HasAttribute("data-omni-sizing"));
        Assert.Equal("9rem", host.Find("col[data-omni-col='Status']").GetAttribute("data-omni-width"));
    }

    [Fact]
    public void ThePresetOfAHost_SetsContentSizingForEveryGrid()
    {
        Services.AddOmniEuropePreset(
            typeof(OmniDataGrid<>),
            "content",
            new Dictionary<string, object?> { ["ColumnSizing"] = OmniDataGridColumnSizing.Content },
            isDefault: true);

        var host = RenderSites();

        Assert.Equal(OmniDataGridColumnSizing.Content, GridOf(host).ColumnSizing);
        Assert.Equal("content", host.Find("table").GetAttribute("data-omni-sizing"));
        Assert.False(host.Find("col[data-omni-col='Status']").HasAttribute("data-omni-width"));
    }

    [Fact]
    public void ThePresetOfAHost_GivesWayToAnExplicitParameter()
    {
        Services.AddOmniEuropePreset(
            typeof(OmniDataGrid<>),
            "content",
            new Dictionary<string, object?> { ["ColumnSizing"] = OmniDataGridColumnSizing.Content },
            isDefault: true);

        var host = RenderSites(Declared);

        Assert.Equal(OmniDataGridColumnSizing.Declared, GridOf(host).ColumnSizing);
        Assert.False(host.Find("table").HasAttribute("data-omni-sizing"));
        Assert.Equal("9rem", host.Find("col[data-omni-col='Status']").GetAttribute("data-omni-width"));
    }

    [Fact]
    public void ThePresetOfAHost_IsLeftByAGridThatNamesNone()
    {
        Services.AddOmniEuropePreset(
            typeof(OmniDataGrid<>),
            "content",
            new Dictionary<string, object?> { ["ColumnSizing"] = OmniDataGridColumnSizing.Content },
            isDefault: true);

        var host = RenderSites(new Dictionary<string, object> { [nameof(OmniDataGrid<DataGridColumnSizingTestHost.Site>.PresetName)] = OmniPresetRegistry.None });

        Assert.Equal(OmniDataGridColumnSizing.Declared, GridOf(host).ColumnSizing);
        Assert.False(host.Find("table").HasAttribute("data-omni-sizing"));
    }

    [Fact]
    public void Content_HandsTheScriptTheTextsOfEveryLoadedRow_NotOnlyThePage()
    {
        var host = RenderSites(Content);

        var texts = LastTexts();
        // Two rows a page: the long name of the third row is not on screen, and still measured.
        Assert.DoesNotContain("Documentation technique", host.Markup, StringComparison.Ordinal);
        Assert.Contains("Documentation technique de la plateforme d'intégration", texts["Name"]);
        Assert.Contains("Intégration continue", texts["Project"]);
        Assert.Contains("12", texts["Visitors"]);
        Assert.Equal(5, texts["Name"].Length);
        // A templated cell is measured on the page: .NET does not know what a template shows.
        Assert.DoesNotContain("Status", texts.Keys);
    }

    [Fact]
    public void Content_AWrappingColumnHasNoTextsToMeasure()
    {
        RenderSites(Content, parameters => parameters.Add(site => site.WrapProject, true));

        Assert.DoesNotContain("Project", LastTexts().Keys);
        Assert.Contains("Name", LastTexts().Keys);
    }

    [Fact]
    public void Content_ACutColumnHasNoTextsToMeasure()
    {
        RenderSites(Content, parameters => parameters.Add(site => site.TruncateProject, true));

        Assert.DoesNotContain("Project", LastTexts().Keys);
        Assert.Contains("Name", LastTexts().Keys);
    }

    [Fact]
    public void Content_TheTextsAreHandedOverOnceAndAgainWhenTheRowsChange()
    {
        var host = RenderSites(Content);
        Assert.Single(Module.Invocations["setContentTexts"]);

        // A page, then a render for nothing: the same rows, the script keeps what it measured.
        host.Find(".omni-pager button[aria-label='Page suivante']").Click();
        host.Render();
        Assert.Single(Module.Invocations["setContentTexts"]);

        host.Render(parameters => parameters.Add(site => site.Sites, [.. DataGridColumnSizingTestHost.DefaultSites, new("Wiki", "En ligne", 2, "Savoirs", new DateOnly(2021, 1, 1))]));
        Assert.Equal(2, Module.Invocations["setContentTexts"].Count);
        Assert.Contains("Wiki", LastTexts()["Name"]);
    }

    [Fact]
    public void Content_ComingBackFromTheDefaultSizing_HandsTheTextsOverAgain()
    {
        var host = RenderSites(Content);

        host.Render(parameters => parameters.Add(site => site.GridAttributes, Declared));
        Assert.Single(Module.Invocations["setContentTexts"]);
        host.Render(parameters => parameters.Add(site => site.GridAttributes, Content));

        Assert.Equal(2, Module.Invocations["setContentTexts"].Count);
    }

    private Dictionary<string, string[]> LastTexts()
    {
        var call = Module.Invocations["setContentTexts"][^1];
        var texts = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string[]>>(call.Arguments[1]);
        return texts.ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
