using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The fact tile (<see cref="OmniFactTile"/> and <see cref="OmniFactTileRow"/>): its structure (the title and its
/// alignment, three cells per row in a fixed order, a link value), the whole-tile link or button it shares with
/// <see cref="OmniStatTile"/>, its place in an <see cref="OmniTileGrid"/>, the small badge, the cut label and value read
/// whole in the package tooltip, and the shipped rules that line the columns up. The markup and the rules are proved
/// here; the measured alignment and the heights are proved by the showcase probe in a real browser.
/// </summary>
public sealed class FactTileTests : OmniBunitContext
{
    private static RenderFragment Text(string text) => builder => builder.AddContent(0, text);

    private IRenderedComponent<OmniFactTile> RenderTile(Action<ComponentParameterCollectionBuilder<OmniFactTile>>? configure = null) =>
        Render<OmniFactTile>(parameters =>
        {
            parameters.Add(component => component.Title, "Source");
            configure?.Invoke(parameters);
            parameters
                .AddChildContent<OmniFactTileRow>(row => row
                    .Add(component => component.Term, "Repository")
                    .Add(component => component.Icon, Text("R"))
                    .Add(component => component.ChildContent, Text("omni-europe-blazor")))
                .AddChildContent<OmniFactTileRow>(row => row
                    .Add(component => component.Term, "Branch")
                    .Add(component => component.ChildContent, Text("release/1.9.0")));
        });

    // ---- Structure ------------------------------------------------------------------------------

    [Fact]
    public void APlainTile_IsAGroupNamedByItsTitle_AndTheTitleIsCenteredByDefault()
    {
        var tile = RenderTile();

        var root = tile.Find(".omni-fact-tile");
        Assert.Equal("DIV", root.TagName);
        Assert.Equal("group", root.GetAttribute("role"));
        var title = tile.Find(".omni-fact-tile__title-text");
        Assert.Equal("Source", title.TextContent);
        Assert.Equal(title.Id, root.GetAttribute("aria-labelledby"));
        Assert.Equal(OmniAlignment.Center, tile.Instance.TitleAlign);
        Assert.Contains("omni-fact-tile--title-center", root.ClassList);
        Assert.DoesNotContain("omni-fact-tile--action", root.ClassList);
        Assert.False(root.HasAttribute("style"));
    }

    [Theory]
    [InlineData(OmniAlignment.Start, "omni-fact-tile--title-start")]
    [InlineData(OmniAlignment.Center, "omni-fact-tile--title-center")]
    [InlineData(OmniAlignment.End, "omni-fact-tile--title-end")]
    [InlineData(OmniAlignment.Stretch, "omni-fact-tile--title-start")]
    public void TheTitleAlignment_IsOneClassOfTheTile_AndStretchReadsAsStart(OmniAlignment align, string expected)
    {
        var tile = RenderTile(parameters => parameters.Add(component => component.TitleAlign, align));

        var classes = tile.Find(".omni-fact-tile").ClassList.Where(name => name.StartsWith("omni-fact-tile--title-", StringComparison.Ordinal)).ToArray();
        Assert.Equal([expected], classes);
    }

    [Fact]
    public void TheTitleIcon_IsDecorative_AndAbsentWhenNotGiven()
    {
        var without = RenderTile();
        var with = RenderTile(parameters => parameters.Add(component => component.Icon, Text("I")));

        Assert.Empty(without.FindAll(".omni-fact-tile__title-icon"));
        var icon = with.Find(".omni-fact-tile__title-icon");
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
        Assert.Equal("I", icon.TextContent);
    }

    [Fact]
    public void EveryRow_HasThreeCellsInOrder_IconLabelValue_EvenWithoutAnIcon()
    {
        var tile = RenderTile();

        var rows = tile.FindAll(".omni-fact-tile__row");
        Assert.Equal(2, rows.Count);
        foreach (var row in rows)
        {
            Assert.Equal(
                ["omni-fact-tile__icon", "omni-fact-tile__label", "omni-fact-tile__value"],
                row.Children.Select(cell => cell.ClassName!).ToArray());
            Assert.Equal("true", row.Children[0].GetAttribute("aria-hidden"));
        }

        Assert.Equal("R", rows[0].Children[0].TextContent);
        Assert.Equal("Repository", rows[0].Children[1].TextContent);
        Assert.Equal("omni-europe-blazor", rows[0].Children[2].TextContent);
        // No icon given: the cell is there, empty, so the label keeps its column.
        Assert.Equal(string.Empty, rows[1].Children[0].TextContent);
        Assert.Equal("Branch", rows[1].Children[1].TextContent);
        Assert.Equal("release/1.9.0", rows[1].Children[2].TextContent);
    }

    [Fact]
    public void ARowWithAnHref_HasALinkValue_AndARowWithoutOneHasNone()
    {
        var tile = Render<OmniFactTile>(parameters => parameters
            .Add(component => component.Title, "Source")
            .AddChildContent<OmniFactTileRow>(row => row
                .Add(component => component.Term, "Repository")
                .Add(component => component.Href, "/depots/blazor")
                .Add(component => component.ChildContent, Text("omni-europe-blazor")))
            .AddChildContent<OmniFactTileRow>(row => row
                .Add(component => component.Term, "Commit")
                .Add(component => component.ChildContent, Text("9f3c2a1"))));

        var rows = tile.FindAll(".omni-fact-tile__row");
        var link = rows[0].QuerySelector(".omni-fact-tile__value a.omni-link")!;
        Assert.Equal("/depots/blazor", link.GetAttribute("href"));
        Assert.Equal("omni-europe-blazor", link.TextContent);
        Assert.Empty(rows[1].QuerySelectorAll("a"));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//elsewhere.example/x")]
    public void ARowLink_RefusesAnUnsafeHref(string href)
    {
        Assert.Throws<InvalidOperationException>(() => Render<OmniFactTile>(parameters => parameters
            .Add(component => component.Title, "Source")
            .AddChildContent<OmniFactTileRow>(row => row
                .Add(component => component.Term, "Repository")
                .Add(component => component.Href, href)
                .Add(component => component.ChildContent, Text("x")))));
    }

    [Fact]
    public void AValue_KeepsItsWholeTextInThePage_ForTheTooltipAndForReaders()
    {
        const string Long = "release/1.9.0-a-branch-name-much-too-long-for-its-column-so-the-ellipsis-cuts-it";
        var tile = Render<OmniFactTile>(parameters => parameters
            .Add(component => component.Title, "Source")
            .AddChildContent<OmniFactTileRow>(row => row
                .Add(component => component.Term, "A label wider than its column")
                .Add(component => component.ChildContent, Text(Long))));

        // The cut is drawn by the stylesheet; the text is whole in the cell, which the tooltip script reads.
        Assert.Equal(Long, tile.Find(".omni-fact-tile__value").TextContent.Trim());
        Assert.Equal("A label wider than its column", tile.Find(".omni-fact-tile__label").TextContent);
    }

    [Fact]
    public void ABadgeInAValue_IsRenderedAsAnOrdinaryBadge_TheSmallSizeComingFromTheValueCell()
    {
        var tile = Render<OmniFactTile>(parameters => parameters
            .Add(component => component.Title, "Run")
            .AddChildContent<OmniFactTileRow>(row => row
                .Add(component => component.Term, "Status")
                .AddChildContent<OmniBadge>(badge => badge.Add(component => component.Tone, OmniTone.Success).AddChildContent("Réussi"))));

        var badge = tile.Find(".omni-fact-tile__value > .omni-badge");
        Assert.Equal("Réussi", badge.TextContent.Trim());
        Assert.False(badge.HasAttribute("style"));
    }

    // ---- The whole tile as a link or a button ---------------------------------------------------

    [Fact]
    public void ATileWithHref_IsOneLink_NamedByItsContent_DrawnLikeTheStatTile()
    {
        var tile = RenderTile(parameters => parameters.Add(component => component.Href, "/runs/1"));

        var root = tile.Find(".omni-fact-tile");
        Assert.Equal("A", root.TagName);
        Assert.Equal("/runs/1", root.GetAttribute("href"));
        Assert.Contains("omni-fact-tile--action", root.ClassList);
        Assert.Null(root.GetAttribute("role"));
        Assert.Null(root.GetAttribute("aria-label"));
        Assert.Empty(tile.FindAll("button"));
        Assert.Contains("Source", root.TextContent, StringComparison.Ordinal);
        Assert.Contains("Repository", root.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("<div", root.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void ATileWithOnClick_IsOneButton_AndClicksTheCallback()
    {
        var clicks = 0;
        var tile = RenderTile(parameters => parameters.Add(component => component.OnClick, () => clicks++));

        var root = tile.Find(".omni-fact-tile");
        Assert.Equal("BUTTON", root.TagName);
        Assert.Equal("button", root.GetAttribute("type"));
        Assert.Contains("omni-fact-tile--action", root.ClassList);
        Assert.Empty(tile.FindAll("a"));
        Assert.DoesNotContain("<div", root.InnerHtml, StringComparison.Ordinal);

        root.Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void ATile_WithHrefAndOnClick_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => RenderTile(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.OnClick, () => { })));

        Assert.Equal("Href", exception.ParamName);
        Assert.Contains("OmniFactTile", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//elsewhere.example/x")]
    public void ATile_RefusesAnUnsafeHref(string href)
    {
        Assert.Throws<InvalidOperationException>(() => RenderTile(parameters => parameters.Add(component => component.Href, href)));
    }

    [Fact]
    public void ALinkRow_InAClickableTile_Throws_BecauseALinkCannotSitInALinkOrAButton()
    {
        void RenderWith(Action<ComponentParameterCollectionBuilder<OmniFactTile>> configure) =>
            Render<OmniFactTile>(parameters =>
            {
                parameters.Add(component => component.Title, "Source");
                configure(parameters);
                parameters.AddChildContent<OmniFactTileRow>(row => row
                    .Add(component => component.Term, "Repository")
                    .Add(component => component.Href, "/depots")
                    .Add(component => component.ChildContent, Text("x")));
            });

        Assert.Throws<InvalidOperationException>(() => RenderWith(parameters => parameters.Add(component => component.Href, "/runs/1")));
        Assert.Throws<InvalidOperationException>(() => RenderWith(parameters => parameters.Add(component => component.OnClick, () => { })));
        // A plain tile takes it.
        RenderWith(_ => { });
    }

    // ---- The tile rule --------------------------------------------------------------------------

    [Fact]
    public void ADoubleWidthTile_CarriesTheDoubleClass_OnItsRoot_InEveryMode()
    {
        var plain = RenderTile(parameters => parameters.Add(component => component.DoubleWidth, true));
        var link = RenderTile(parameters => parameters.Add(component => component.DoubleWidth, true).Add(component => component.Href, "/a"));
        var button = RenderTile(parameters => parameters.Add(component => component.DoubleWidth, true).Add(component => component.OnClick, () => { }));
        var single = RenderTile();

        Assert.Contains("omni-tile--double", plain.Find(".omni-fact-tile").ClassList);
        Assert.Contains("omni-tile--double", link.Find(".omni-fact-tile").ClassList);
        Assert.Contains("omni-tile--double", button.Find(".omni-fact-tile").ClassList);
        Assert.DoesNotContain("omni-tile--double", single.Find(".omni-fact-tile").ClassList);
    }

    [Fact]
    public void ATileGrid_KeepsTheFactTilesAsItsDirectChildren_SoTheTileRuleReachesThem()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters
            .AddChildContent<OmniFactTile>(tile => tile.Add(component => component.Title, "A"))
            .AddChildContent<OmniFactTile>(tile => tile.Add(component => component.Title, "B").Add(component => component.Href, "/b"))
            .AddChildContent<OmniFactTile>(tile => tile.Add(component => component.Title, "C").Add(component => component.OnClick, () => { })));

        var children = grid.Find(".omni-tile-grid").Children;
        Assert.Equal(["DIV", "A", "BUTTON"], children.Select(child => child.TagName).ToArray());
        Assert.All(children, child => Assert.Contains("omni-fact-tile", child.ClassList));
    }

    [Fact]
    public void TheTileGridRule_ReachesTheFactTile_AndItsRowsKeepOneThreeColumnGrid()
    {
        var reach = ShippedLookTests.Body(".omni-tile-grid > :is(.omni-card, .omni-selectable-card, .omni-stat-tile, .omni-fact-tile, .omni-nav-tile)");
        Assert.Equal("100%", ShippedLookTests.Value(reach, "block-size"));
        Assert.Equal("0", ShippedLookTests.Value(reach, "min-inline-size"));
        Assert.Equal("0", ShippedLookTests.Value(reach, "min-block-size"));

        // The columns: a fixed icon track, a fixed label track, the rest for the value, shared by the rows block
        // and, as a subgrid, by every row; fixed in rem so they also match from one tile to the next.
        var rows = ShippedLookTests.Body(".omni-fact-tile__rows");
        Assert.Equal("var(--omni-fact-tile-columns)", ShippedLookTests.Value(rows, "grid-template-columns"));
        Assert.Contains("--omni-fact-tile-columns: var(--omni-fact-tile-icon-width, 1.25rem) var(--omni-fact-tile-label-width, 6.5rem) minmax(0, 1fr)", rows, StringComparison.Ordinal);
        Assert.Equal("grid", ShippedLookTests.Value(rows, "display"));
        Assert.Equal("auto", ShippedLookTests.Value(rows, "overflow-y"));
        var row = ShippedLookTests.Body(".omni-fact-tile__row");
        Assert.Contains("grid-template-columns: subgrid", row, StringComparison.Ordinal);
        Assert.Equal("1 / -1", ShippedLookTests.Value(row, "grid-column"));
        // A clickable tile clips its rows: a scrolling region inside a link or a button is a nested focus stop.
        Assert.Equal("hidden", ShippedLookTests.Value(ShippedLookTests.Body(".omni-fact-tile--action > .omni-fact-tile__rows"), "overflow"));
    }

    [Fact]
    public void TheLabelAndTheValue_AreCutByAnEllipsis_OnOneLine()
    {
        foreach (var selector in new[] { ".omni-fact-tile__label", ".omni-fact-tile__value" })
        {
            var body = ShippedLookTests.Body(selector);
            Assert.Equal("hidden", ShippedLookTests.Value(body, "overflow"));
            Assert.Equal("ellipsis", ShippedLookTests.Value(body, "text-overflow"));
            Assert.Equal("nowrap", ShippedLookTests.Value(body, "white-space"));
            Assert.Equal("0", ShippedLookTests.Value(body, "min-inline-size"));
        }
    }

    [Fact]
    public void ABadgeInAValue_IsDrawnSmall_ByTheValueCell()
    {
        var body = ShippedLookTests.Body(".omni-fact-tile__value .omni-badge");
        Assert.Equal("1.125rem", ShippedLookTests.Value(body, "--omni-badge-height"));
        Assert.Equal("var(--omni-font-size-xs)", ShippedLookTests.Value(body, "font-size"));
        Assert.Equal("100%", ShippedLookTests.Value(body, "max-inline-size"));
    }

    [Fact]
    public void AClickableFactTile_ShipsTheHoverAndTheFocusRingOfTheClickableTiles()
    {
        Assert.Contains("var(--omni-focus-ring)", ShippedLookTests.Body(".omni-fact-tile--action:focus-visible"), StringComparison.Ordinal);
        Assert.Contains("color-mix", ShippedLookTests.Body(".omni-fact-tile--action:hover"), StringComparison.Ordinal);
        Assert.Equal("pointer", ShippedLookTests.Value(ShippedLookTests.Body(".omni-fact-tile--action"), "cursor"));
        // The value clips what it holds, so a link in it takes its ring inside.
        Assert.Contains("var(--omni-focus-ring-inset)", ShippedLookTests.Body(".omni-fact-tile__value :focus-visible"), StringComparison.Ordinal);
    }

    // ---- The tooltip ----------------------------------------------------------------------------

    [Fact]
    public async Task ATile_TurnsThePackageTooltipsOnOnce_AndOffWhenDisposed()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Tooltip);

        var tile = RenderTile();
        tile.WaitForAssertion(() => Assert.Single(module.Invocations["installPackageTooltips"]));
        tile.Render(parameters => parameters.Add(component => component.Title, "Source again"));
        Assert.Single(module.Invocations["installPackageTooltips"]);
        Assert.Empty(module.Invocations["uninstallPackageTooltips"]);

        await tile.Instance.DisposeAsync();
        Assert.Single(module.Invocations["uninstallPackageTooltips"]);
    }

    [Fact]
    public void TheTooltipScript_ReadsTheCutLabelAndValueOfAFactTile()
    {
        var script = File.ReadAllText(Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omni-tooltip.js"));

        var selectors = script.Split('\n').Single(line => line.StartsWith("const CUT_CELL = ", StringComparison.Ordinal));
        Assert.Contains(".omni-fact-tile__label", selectors, StringComparison.Ordinal);
        Assert.Contains(".omni-fact-tile__value", selectors, StringComparison.Ordinal);
        Assert.Contains(".omni-data-grid__cell--text", selectors, StringComparison.Ordinal);
    }
}
