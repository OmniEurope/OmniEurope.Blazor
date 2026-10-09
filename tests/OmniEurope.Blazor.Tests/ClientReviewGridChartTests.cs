using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A client application's review of 2026-10-09 (recette R1-4 to R1-8): the editor a row takes the focus
/// in, Enter typed in an editor, a fixed row under a font of its own, the page header in a stack and the
/// data labels at the edges of the plot.
/// </summary>
public sealed class ClientReviewGridChartTests : OmniBunitContext
{
    private const string GridModule = "./_content/OmniEurope.Blazor/omni-grid.js";

    private sealed record Row(int Id, string Name);

    private static readonly IReadOnlyList<Row> Rows = [new(1, "Un"), new(2, "Deux")];

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(Action<ComponentParameterCollectionBuilder<OmniDataGrid<Row>>>? configure = null)
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Key), "nom");
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)(row => row.Name));
            builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Title), "Nom");
            builder.AddComponentParameter(4, nameof(OmniDataGridColumn<Row>.EditTemplate), (RenderFragment<Row>)(row => inner =>
            {
                inner.OpenElement(0, "input");
                inner.AddAttribute(1, "class", "editor");
                inner.AddAttribute(2, "value", row.Name);
                inner.CloseElement();
            }));
            builder.CloseComponent();
        };

        return Render<OmniDataGrid<Row>>(parameters =>
        {
            parameters.Add(grid => grid.Items, Rows).Add(grid => grid.Columns, columns).Add(grid => grid.KeyOf, row => row.Id);
            configure?.Invoke(parameters);
        });
    }

    [Fact]
    public async Task A_row_put_in_edit_mode_asks_its_editor_to_take_the_focus_once()
    {
        var module = JSInterop.SetupModule(GridModule);
        var focus = module.Setup<bool>("focusEditor", _ => true);
        focus.SetResult(true);
        var grid = RenderGrid();

        await grid.InvokeAsync(() => grid.Instance.EditRowAsync(Rows[1]));

        var marked = Assert.Single(grid.FindAll("tr[data-omni-edit-focus]"));
        Assert.Equal("1", marked.GetAttribute("data-omni-row-index"));
        grid.WaitForAssertion(() => Assert.Single(module.Invocations, invocation => invocation.Identifier == "focusEditor"));

        // Spent: a later render (a sort, a refresh) does not take the focus back from the user.
        grid.Render();
        Assert.Single(module.Invocations, invocation => invocation.Identifier == "focusEditor");
        Assert.Empty(grid.FindAll("tr[data-omni-edit-focus]"));
    }

    [Fact]
    public async Task The_focus_request_waits_for_a_render_that_put_the_marked_row_in_the_page()
    {
        var module = JSInterop.SetupModule(GridModule);
        var focus = module.Setup<bool>("focusEditor", _ => true);
        focus.SetResult(false);
        var grid = RenderGrid();

        await grid.InvokeAsync(() => grid.Instance.EditRowAsync(Rows[0]));
        grid.Render();

        // The script found no marked row yet: the request held and was asked again.
        grid.WaitForAssertion(() => Assert.True(module.Invocations.Count(invocation => invocation.Identifier == "focusEditor") >= 2));
        Assert.Single(grid.FindAll("tr[data-omni-edit-focus]"));
    }

    [Fact]
    public async Task Enter_typed_in_an_editor_is_not_a_click_on_its_row()
    {
        var clicks = new List<int>();
        var grid = RenderGrid(parameters => parameters.Add(g => g.OnRowClick, args => clicks.Add(args.Item.Id)));

        // The control: Enter on a row that is not being edited activates it.
        grid.FindAll("tbody tr[data-omni-row-index]")[1].KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal([2], clicks);

        await grid.InvokeAsync(() => grid.Instance.EditRowAsync(Rows[0]));
        // The editing cell stops the key before the row: bubbling from the editor finds no handler at all,
        // where the row's own would have been reached.
        Assert.Throws<MissingEventHandlerException>(() =>
            grid.Find("tr[data-omni-row-index='0'] input.editor").KeyDown(new KeyboardEventArgs { Key = "Enter" }));
        Assert.Throws<MissingEventHandlerException>(() =>
            grid.Find("tr[data-omni-row-index='0'] input.editor").KeyDown(new KeyboardEventArgs { Key = " " }));

        Assert.Equal([2], clicks);
    }

    [Fact]
    public void A_fixed_row_gives_its_cells_a_line_of_their_own_and_a_padding_that_fits_the_row()
    {
        // Under a serif face line-height: normal made a 40px row 41.2px, its 2px rule 38px out of 36.
        var body = ShippedLookTests.BodyWith(".omni-data-grid--fixed-row-height tbody tr:not(.omni-data-grid__spacer) td", "line-height");
        Assert.Equal("1.25rem", ShippedLookTests.Value(body, "line-height"));
        Assert.Equal(
            "max(0px, min(var(--omni-cell-pad-y), (var(--omni-row-height) - 1.25rem - var(--omni-border-width)) / 2))",
            ShippedLookTests.Value(body, "padding-block"));
    }

    [Fact]
    public void A_page_header_in_a_stack_leaves_the_spacing_to_the_stack()
    {
        Assert.Equal("0", ShippedLookTests.Value(ShippedLookTests.Body(".omni-stack > .omni-page-header"), "margin-block-end"));
        // Outside a stack it keeps its own margin, as before.
        Assert.Equal("var(--omni-space-lg)", ShippedLookTests.Value(ShippedLookTests.BodyWith(".omni-page-header", "margin-block-end"), "margin-block-end"));
    }

    [Fact]
    public void The_first_and_last_data_labels_are_anchored_inside_the_plot()
    {
        var points = new[] { new OmniChartPoint(0, 10, "123 456,78 €"), new OmniChartPoint(1, 20, "2"), new OmniChartPoint(2, 30, "345 678,90 €") };
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Soldes")
            .AddChildContent(builder =>
            {
                Child<OmniLineSeries>(builder, 0, ("Data", points), ("Title", "Solde"));
                Child<OmniSeriesDataLabels>(builder, 10, ("Data", points));
            }));

        chart.WaitForAssertion(() =>
        {
            var labels = chart.FindAll(".omni-chart__labels text");
            Assert.Equal(3, labels.Count);
            // Centred on the first point it would cross the left edge by half its width: it starts there.
            Assert.Null(labels[0].GetAttribute("text-anchor"));
            Assert.Equal("middle", labels[1].GetAttribute("text-anchor"));
            Assert.Equal("end", labels[2].GetAttribute("text-anchor"));
            var start = double.Parse(labels[0].GetAttribute("x")!, System.Globalization.CultureInfo.InvariantCulture);
            var end = double.Parse(labels[2].GetAttribute("x")!, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(start > 0 && end <= 100, $"labels at {start} and {end}");
        });
    }

    [Fact]
    public void A_label_that_fits_stays_centred_on_its_point()
    {
        var points = new[] { new OmniChartPoint(0, 10), new OmniChartPoint(1, 20), new OmniChartPoint(2, 30) };
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Courts")
            .AddChildContent(builder =>
            {
                Child<OmniColumnSeries>(builder, 0, ("Data", points), ("Title", "Ventes"));
                Child<OmniSeriesDataLabels>(builder, 10, ("Data", points));
            }));

        chart.WaitForAssertion(() =>
            Assert.All(chart.FindAll(".omni-chart__labels text"), label => Assert.Equal("middle", label.GetAttribute("text-anchor"))));
    }

    private static void Child<TComponent>(RenderTreeBuilder builder, int sequence, params (string Name, object Value)[] parameters)
        where TComponent : IComponent
    {
        builder.OpenComponent<TComponent>(sequence);
        for (var index = 0; index < parameters.Length; index++)
        {
            builder.AddAttribute(sequence + index + 1, parameters[index].Name, parameters[index].Value);
        }

        builder.CloseComponent();
    }
}
