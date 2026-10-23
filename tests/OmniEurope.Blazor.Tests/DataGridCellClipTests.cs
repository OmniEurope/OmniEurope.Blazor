using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A text cell keeps to one line and ends in an ellipsis instead of spilling over the next column;
/// a templated cell stays unclipped so its badges, buttons and edit inputs are not cut.
/// </summary>
public sealed class DataGridCellClipTests : OmniBunitContext
{
    private const string TextCell = "omni-data-grid__cell--text";

    [Fact]
    public void TextCells_CarryTheClipClass_TemplatedCellsDoNot()
    {
        var text = Render<DataGridLoopColumnsTestHost>(parameters => parameters.Add(component => component.CaptureValue, true));
        var templated = Render<DataGridLoopColumnsTestHost>();

        Assert.All(text.FindAll("tbody td[data-omni-col]"), cell => Assert.Contains(TextCell, cell.ClassList));
        Assert.All(templated.FindAll("tbody td[data-omni-col]"), cell => Assert.DoesNotContain(TextCell, cell.ClassList));
    }

    [Fact]
    public void ARowInEdit_UnclipsTheCellsThatShowAnEditInput()
    {
        var grid = Render<DataGridSurfaceTestHost>(parameters => parameters
            .Add(component => component.EditMode, OmniDataGridRowMode.Single));

        grid.FindAll(".omni-data-grid__actions button")[0].Click();

        var editing = grid.Find(".surface-edit").Closest("td")!;
        Assert.DoesNotContain(TextCell, editing.ClassList);
        // The row's other column has no edit template: it still shows its text, clipped.
        Assert.Contains(TextCell, editing.ParentElement!.QuerySelector("td[data-omni-col='Total']")!.ClassList);
        // The header and the footer keep their own rules (HeaderWrap).
        Assert.Empty(grid.FindAll($"thead .{TextCell}, tfoot .{TextCell}"));
    }

    [Fact]
    public async Task EveryGrid_TurnsOnTheTooltipOfCutCells_AndTurnsItOffWhenItGoes()
    {
        // The script shows the whole value of a cell only while it is cut (measured in the browser,
        // omni-tooltip.js); the grid's part is to turn it on once and off when it is disposed.
        var grid = Render<DataGridLoopColumnsTestHost>(parameters => parameters.Add(component => component.CaptureValue, true));

        Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "installPackageTooltips");
        grid.Render();
        Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "installPackageTooltips");

        await DisposeComponentsAsync();
        Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "uninstallPackageTooltips");
    }

    [Fact]
    public void Stylesheet_ClipsTextCellsWithAnEllipsis()
    {
        var body = ShippedLookTests.Body(".omni-data-grid tbody td.omni-data-grid__cell--text");

        Assert.Equal("hidden", ShippedLookTests.Value(body, "overflow"));
        Assert.Equal("ellipsis", ShippedLookTests.Value(body, "text-overflow"));
        Assert.Equal("nowrap", ShippedLookTests.Value(body, "white-space"));
    }

    private sealed record TitleRow(string Reference);

    private IRenderedComponent<OmniDataGrid<TitleRow>> RenderNarrowTitle(Action<ComponentParameterCollectionBuilder<OmniDataGrid<TitleRow>>>? configure = null) =>
        Render<OmniDataGrid<TitleRow>>(parameters =>
        {
            parameters.Add(grid => grid.Items, [new TitleRow("A-1")])
                .Add(grid => grid.Columns, (Microsoft.AspNetCore.Components.RenderFragment)(builder =>
                {
                    builder.OpenComponent<OmniDataGridColumn<TitleRow>>(0);
                    builder.AddComponentParameter(1, nameof(OmniDataGridColumn<TitleRow>.Property), nameof(TitleRow.Reference));
                    builder.AddComponentParameter(2, nameof(OmniDataGridColumn<TitleRow>.Title), "Responsabilité");
                    builder.AddComponentParameter(3, nameof(OmniDataGridColumn<TitleRow>.Width), "4rem");
                    builder.CloseComponent();
                }));
            configure?.Invoke(parameters);
        });

    [Fact]
    public void ANarrowDeclaredWidth_DoesNotTurnTheTitleIntoATruncatedOne_UnlessAskedTo()
    {
        // The browser half (the title's scrollWidth never exceeds its clientWidth) is the boards probe; here the
        // inputs that decide it: the declared width is only the preferred one (fitted by the script, which
        // reads data-omni-width), and truncation is opt-in on the grid or on the column.
        var wrapped = RenderNarrowTitle();

        Assert.DoesNotContain("omni-data-grid--header-truncate", wrapped.Find(".omni-data-grid").ClassList);
        var col = wrapped.Find("col[data-omni-col='Reference']");
        Assert.Equal("4rem", col.GetAttribute("data-omni-width"));
        Assert.Null(col.GetAttribute("data-omni-truncate"));

        var truncated = RenderNarrowTitle(parameters => parameters.Add(grid => grid.HeaderWrap, OmniDataGridHeaderWrap.Truncate));
        Assert.Contains("omni-data-grid--header-truncate", truncated.Find(".omni-data-grid").ClassList);
    }

    [Fact]
    public void Stylesheet_ClipsNoTemplatedCellAndLetsResponsiveCardsWrap()
    {
        // Only the text-cell class clips: no rule hides the overflow of every body cell, which would
        // cut the focus rings, badges and popups of templated cells.
        Assert.DoesNotContain(
            ShippedLookTests.Rules(),
            rule => rule.Selector.StartsWith(".omni-data-grid", StringComparison.Ordinal)
                && rule.Selector.EndsWith("tbody td", StringComparison.Ordinal)
                && rule.Body.Contains("overflow", StringComparison.Ordinal));

        var card = ShippedLookTests.Body(".omni-data-grid--responsive .omni-data-grid__table td.omni-data-grid__cell--text");
        Assert.Equal("normal", ShippedLookTests.Value(card, "white-space"));
        Assert.Equal("anywhere", ShippedLookTests.Value(card, "overflow-wrap"));
    }
}
