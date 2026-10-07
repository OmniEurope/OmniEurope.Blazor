using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;
using Row = OmniEurope.Blazor.Tests.DataGridExportTestHost.Row;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The header and footer bars of the grid: inside the table's frame, each shown or not, carrying the
/// export buttons at its start or its end, the formats switched one by one, icon buttons named "Export
/// as ...", and the host's content beside them. Without a bar the grid renders as before.
/// </summary>
public sealed class DataGridBarsTests : OmniBunitContext
{
    private static readonly Row[] Rows =
    [
        new(1, "Delta", 1234.5m, new DateOnly(2026, 9, 1), true),
        new(2, "Alpha", 20m, new DateOnly(2026, 9, 2), false)
    ];

    private IRenderedComponent<DataGridExportTestHost> Grid(Action<ComponentParameterCollectionBuilder<DataGridExportTestHost>> parameters) =>
        Render<DataGridExportTestHost>(builder =>
        {
            builder.Add(component => component.Items, Rows);
            parameters(builder);
        });

    private static IReadOnlyList<string?> Labels(AngleSharp.Dom.IElement scope) =>
        scope.QuerySelectorAll(".omni-data-grid__export-button").Select(button => button.GetAttribute("aria-label")).ToArray();

    [Fact]
    public void WithoutBars_TheTableIsNotFramed_AndNothingIsExported()
    {
        var host = Grid(_ => { });

        Assert.Empty(host.FindAll(".omni-data-grid__frame"));
        Assert.Empty(host.FindAll(".omni-data-grid__bar"));
        Assert.Contains(host.Find(".omni-data-grid").Children, child => child.ClassList.Contains("omni-data-grid__viewport"));
    }

    [Fact]
    public void BothBars_ShareTheFrame_AroundTheRows()
    {
        var host = Grid(builder => builder
            .Add(component => component.ShowHeaderBar, true)
            .Add(component => component.ShowFooterBar, true)
            .Add(component => component.HeaderBarExport, OmniDataGridBarExport.None)
            .Add(component => component.FooterBarExport, OmniDataGridBarExport.None));

        var frame = host.Find(".omni-data-grid__frame");
        Assert.Equal(["omni-data-grid__bar--header", "omni-data-grid__viewport", "omni-data-grid__bar--footer"],
            frame.Children.Select(child => child.ClassList.First(name => name is "omni-data-grid__bar--header" or "omni-data-grid__viewport" or "omni-data-grid__bar--footer")));
        // A bar whose export is None carries no button.
        Assert.Empty(host.FindAll(".omni-data-grid__export"));
    }

    [Fact]
    public void AShownBar_CarriesTheButtons_AtItsStart_ByDefault()
    {
        var host = Grid(builder => builder
            .Add(component => component.ShowHeaderBar, true)
            .Add(component => component.ShowFooterBar, true)
            .Add(component => component.HeaderBarContent, (RenderFragment)(content => content.AddContent(0, "en-tête"))));

        foreach (var bar in host.FindAll(".omni-data-grid__bar"))
        {
            Assert.Equal(["Exporter en Markdown", "Exporter en CSV", "Exporter en Excel"], Labels(bar));
            Assert.True(bar.Children[0].ClassList.Contains("omni-data-grid__export"));
        }

        Assert.Equal(2, host.FindAll(".omni-data-grid__bar").Count);
    }

    [Fact]
    public void TheGridItself_PutsTheButtons_AtTheStartOfAShownBar_ByDefault()
    {
        // Rendered without the test host, which always passes a value of its own: only the grid's default
        // decides here.
        var grid = Render<OmniDataGrid<Row>>(builder => builder
            .Add(component => component.Items, Rows)
            .Add(component => component.ShowHeaderBar, true)
            .Add(component => component.ShowFooterBar, true));

        Assert.Equal(OmniDataGridBarExport.Start, grid.Instance.HeaderBarExport);
        Assert.Equal(OmniDataGridBarExport.Start, grid.Instance.FooterBarExport);
        foreach (var bar in grid.FindAll(".omni-data-grid__bar"))
        {
            Assert.NotEmpty(Labels(bar));
            Assert.True(bar.Children[0].ClassList.Contains("omni-data-grid__export"));
        }

        Assert.Equal(2, grid.FindAll(".omni-data-grid__bar").Count);
    }

    [Fact]
    public void TheFormerExportBar_StillShows_WhenTheShownBarCarriesNoButton()
    {
        var host = Grid(builder => builder
            .Add(component => component.ShowHeaderBar, true)
            .Add(component => component.HeaderBarExport, OmniDataGridBarExport.None)
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));

        Assert.Empty(host.Find(".omni-data-grid__bar--header").QuerySelectorAll(".omni-data-grid__export"));
        Assert.Equal(["Exporter en CSV"], Labels(host.Find(".omni-data-grid__bar--footer")));
    }

    [Fact]
    public void EveryFormat_IsOnByDefault_EachAnIconButton()
    {
        var host = Grid(builder => builder
            .Add(component => component.ShowFooterBar, true)
            .Add(component => component.FooterBarExport, OmniDataGridBarExport.Start));

        var footer = host.Find(".omni-data-grid__bar--footer");
        // PDF needs a renderer of the host: no button here, as before.
        Assert.Equal(["Exporter en Markdown", "Exporter en CSV", "Exporter en Excel"], Labels(footer));
        foreach (var button in footer.QuerySelectorAll(".omni-data-grid__export-button"))
        {
            Assert.Equal(button.GetAttribute("aria-label"), button.GetAttribute("title"));
            Assert.Equal(string.Empty, button.TextContent.Trim());
            Assert.NotNull(button.QuerySelector(".omni-icon"));
        }

        Assert.DoesNotContain("Tout exporter", host.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TheGridItself_ShowsIconsAlone_ByDefault()
    {
        // Rendered without the test host, so only the grid's default decides.
        var grid = Render<OmniDataGrid<Row>>(builder => builder
            .Add(component => component.Items, Rows)
            .Add(component => component.ShowFooterBar, true));

        Assert.False(grid.Instance.ShowExportButtonText);
        Assert.All(grid.FindAll(".omni-data-grid__export-button"), button => Assert.Equal(string.Empty, button.TextContent.Trim()));
    }

    [Fact]
    public void ShowExportButtonText_WritesTheFormatBesideTheIcon_AndKeepsTheNameAndTitle()
    {
        var host = Grid(builder => builder
            .Add(component => component.ShowFooterBar, true)
            .Add(component => component.ShowExportButtonText, true));

        var buttons = host.Find(".omni-data-grid__bar--footer").QuerySelectorAll(".omni-data-grid__export-button");
        Assert.Equal(["Markdown", "CSV", "Excel"], buttons.Select(button => button.TextContent.Trim()));
        Assert.Equal(["Exporter en Markdown", "Exporter en CSV", "Exporter en Excel"], buttons.Select(button => button.GetAttribute("aria-label")));
        foreach (var button in buttons)
        {
            Assert.Equal(button.GetAttribute("aria-label"), button.GetAttribute("title"));
            // WCAG 2.5.3: the accessible name contains the visible text.
            Assert.Contains(button.TextContent.Trim(), button.GetAttribute("aria-label"), StringComparison.Ordinal);
            Assert.NotNull(button.QuerySelector(".omni-icon"));
        }
    }

    [Fact]
    public void TheFormats_AreSwitchedOneByOne()
    {
        var host = Grid(builder => builder
            .Add(component => component.ShowFooterBar, true)
            .Add(component => component.FooterBarExport, OmniDataGridBarExport.End)
            .Add(component => component.ExportCsv, false)
            .Add(component => component.ExportExcel, false)
            .Add(component => component.ExportPdf, false));

        Assert.Equal(["Exporter en Markdown"], Labels(host.Find(".omni-data-grid__bar--footer")));
    }

    [Fact]
    public void NoFormatLeft_LeavesTheBar_WithoutButtons()
    {
        var host = Grid(builder => builder
            .Add(component => component.ShowFooterBar, true)
            .Add(component => component.FooterBarExport, OmniDataGridBarExport.End)
            .Add(component => component.ExportMarkdown, false)
            .Add(component => component.ExportCsv, false)
            .Add(component => component.ExportExcel, false));

        Assert.NotNull(host.Find(".omni-data-grid__bar--footer"));
        Assert.Empty(host.FindAll(".omni-data-grid__export"));
    }

    [Theory]
    [InlineData(OmniDataGridBarExport.Start, true)]
    [InlineData(OmniDataGridBarExport.End, false)]
    public void TheButtons_SitAtTheChosenSide_OfTheHostContent(OmniDataGridBarExport side, bool first)
    {
        var host = Grid(builder => builder
            .Add(component => component.ShowHeaderBar, true)
            .Add(component => component.HeaderBarExport, side)
            .Add(component => component.HeaderBarContent, (RenderFragment)(content => content.AddMarkupContent(0, "<span class=\"host-note\">3 dossiers</span>"))));

        var children = host.Find(".omni-data-grid__bar--header").Children.ToList();
        var buttons = children.FindIndex(child => child.ClassList.Contains("omni-data-grid__export"));
        var content = children.FindIndex(child => child.ClassList.Contains("omni-data-grid__bar-content"));
        Assert.Equal(first, buttons < content);
        Assert.Equal("3 dossiers", children[content].QuerySelector(".host-note")!.TextContent);
    }

    [Fact]
    public void AHeaderBarForTheHostAlone_HasNoButton_AndNoFooter()
    {
        var host = Grid(builder => builder
            .Add(component => component.ShowHeaderBar, true)
            .Add(component => component.HeaderBarExport, OmniDataGridBarExport.None)
            .Add(component => component.FooterBarExport, OmniDataGridBarExport.End)
            .Add(component => component.HeaderBarContent, (RenderFragment)(content => content.AddContent(0, "en-tête"))));

        Assert.Equal("en-tête", host.Find(".omni-data-grid__bar--header .omni-data-grid__bar-content").TextContent);
        Assert.Empty(host.FindAll(".omni-data-grid__export"));
        Assert.Empty(host.FindAll(".omni-data-grid__bar--footer"));
    }

    [Fact]
    public void AHiddenBar_CarriesNothing_EvenWhenAskedToExport()
    {
        var host = Grid(builder => builder
            .Add(component => component.HeaderBarExport, OmniDataGridBarExport.Start)
            .Add(component => component.FooterBarContent, (RenderFragment)(content => content.AddContent(0, "pied"))));

        Assert.Empty(host.FindAll(".omni-data-grid__bar"));
        Assert.DoesNotContain("pied", host.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EachBar_CarriesItsOwnButtons()
    {
        var host = Grid(builder => builder
            .Add(component => component.ShowHeaderBar, true)
            .Add(component => component.ShowFooterBar, true)
            .Add(component => component.HeaderBarExport, OmniDataGridBarExport.End)
            .Add(component => component.FooterBarExport, OmniDataGridBarExport.None));

        Assert.Single(host.Find(".omni-data-grid__bar--header").QuerySelectorAll(".omni-data-grid__export"));
        Assert.Empty(host.Find(".omni-data-grid__bar--footer").QuerySelectorAll(".omni-data-grid__export"));
    }

    [Theory]
    [InlineData(OmniDataGridPosition.Bottom, false, true)]
    [InlineData(OmniDataGridPosition.Top, true, false)]
    [InlineData(OmniDataGridPosition.TopAndBottom, true, true)]
    public void TheFormerExportBar_IsShownInTheFrame_WhereItsPositionSaid(OmniDataGridPosition position, bool header, bool footer)
    {
        var host = Grid(builder => builder
            .Add(component => component.Position, position)
            .Add(component => component.Formats, [OmniTableExportFormat.Csv, OmniTableExportFormat.Markdown]));

        Assert.Equal(header, host.FindAll(".omni-data-grid__frame > .omni-data-grid__bar--header").Count == 1);
        Assert.Equal(footer, host.FindAll(".omni-data-grid__frame > .omni-data-grid__bar--footer").Count == 1);
        // The former list keeps its order and replaces the four switches; the buttons sit at the start.
        foreach (var bar in host.FindAll(".omni-data-grid__bar"))
        {
            Assert.Equal(["Exporter en CSV", "Exporter en Markdown"], Labels(bar));
            Assert.True(bar.Children[0].ClassList.Contains("omni-data-grid__export"));
        }
    }

    [Fact]
    public void TheFormerList_RestrictsTheFormats_OfTheNewBars()
    {
        var host = Grid(builder => builder
            .Add(component => component.ShowFooterBar, true)
            .Add(component => component.FooterBarExport, OmniDataGridBarExport.End)
            .Add(component => component.Formats, [OmniTableExportFormat.Excel]));

        Assert.Equal(["Exporter en Excel"], Labels(host.Find(".omni-data-grid__bar--footer")));
        Assert.Empty(host.FindAll(".omni-data-grid__bar--header"));
    }
}
