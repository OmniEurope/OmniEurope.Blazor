using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The recurring requests of the client applications on the grid (PLAN-001 lot 5): Enter and Escape in a
/// row being edited, a row that keeps its height when it enters edit mode, rows that alternate by default
/// and a header written in the cell font.
/// </summary>
public sealed partial class DataGridEditRowAndDefaultsTests : OmniBunitContext
{
    private sealed record Row(int Id, string Name);

    private static readonly IReadOnlyList<Row> Rows = [new(1, "Un"), new(2, "Deux"), new(3, "Trois")];

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(Action<ComponentParameterCollectionBuilder<OmniDataGrid<Row>>>? configure = null)
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Nom");
            builder.CloseComponent();
        };

        return Render<OmniDataGrid<Row>>(parameters =>
        {
            parameters.Add(grid => grid.Items, Rows).Add(grid => grid.Columns, columns);
            configure?.Invoke(parameters);
        });
    }

    // ---- G-D29-03: Enter saves, Escape cancels ----

    /// <summary>
    /// The keys are read by the grid script (bUnit runs no script): a keydown listener on the document,
    /// in the capture phase so the editor's state is read before any handler of the page changes it, that
    /// presses the row's own save or cancel button (the path of a click: OnRowUpdate, OnRowEditCancel),
    /// commits the value typed in the field first, and leaves both keys to an open list, calendar or
    /// disclosure in the cell and Enter to a multi-line field. The behaviour itself is driven with real
    /// keys by the scripts probe (eng/scripts-probe/keys.mjs, grid step).
    /// </summary>
    [Fact]
    public void The_grid_script_saves_on_Enter_and_cancels_on_Escape_through_the_row_buttons()
    {
        var script = File.ReadAllText(Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "grid", "edit-focus.js"));
        var grid = File.ReadAllText(Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omni-grid.js"));

        Assert.Contains("from './grid/edit-focus.js'", grid, StringComparison.Ordinal);
        Assert.Matches(@"document\.addEventListener\('keydown', event => \{[\s\S]*?\}, \{ capture: true \}\);", script);
        Assert.Contains("omni-data-grid__row--editing", script, StringComparison.Ordinal);
        Assert.Contains("omni-data-grid__action--save", script, StringComparison.Ordinal);
        Assert.Contains("omni-data-grid__action--cancel", script, StringComparison.Ordinal);
        Assert.Contains("new Event('change', { bubbles: true })", script, StringComparison.Ordinal);
        // The editors that keep the keys for themselves.
        Assert.Contains("[aria-expanded=\"true\"]", script, StringComparison.Ordinal);
        Assert.Contains("details[open]", script, StringComparison.Ordinal);
        Assert.Contains("HTMLTextAreaElement", script, StringComparison.Ordinal);
        Assert.Contains("isContentEditable", script, StringComparison.Ordinal);
        Assert.Contains("aria-multiline", script, StringComparison.Ordinal);
    }

    /// <summary>The buttons the keys press: one save and one cancel in the row being edited, none elsewhere.</summary>
    [Fact]
    public async Task The_row_being_edited_holds_the_save_and_cancel_buttons_the_keys_press()
    {
        var updated = new List<int>();
        var cancelled = new List<int>();
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.KeyOf, row => row.Id)
            .Add(component => component.OnRowUpdate, row => updated.Add(row.Id))
            .Add(component => component.OnRowEditCancel, row => cancelled.Add(row.Id))
            .Add(component => component.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniDataGridColumn<Row>>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
                builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.EditTemplate), (RenderFragment<Row>)(row => inner =>
                {
                    inner.OpenElement(0, "input");
                    inner.AddAttribute(1, "value", row.Name);
                    inner.CloseElement();
                }));
                builder.CloseComponent();
            })));

        await grid.InvokeAsync(() => grid.Instance.EditRowAsync(Rows[1]));

        var row = grid.Find("tr.omni-data-grid__row--editing");
        Assert.Equal("1", row.GetAttribute("data-omni-row-index"));
        Assert.Single(grid.FindAll(".omni-data-grid__action--save"));
        Assert.Single(grid.FindAll(".omni-data-grid__action--cancel"));
        grid.Find("tr.omni-data-grid__row--editing > td.omni-data-grid__actions .omni-data-grid__action--save").Click();
        Assert.Equal([2], updated);

        await grid.InvokeAsync(() => grid.Instance.EditRowAsync(Rows[2]));
        grid.Find("tr.omni-data-grid__row--editing > td.omni-data-grid__actions .omni-data-grid__action--cancel").Click();
        Assert.Equal([3], cancelled);
        Assert.Empty(grid.FindAll("tr.omni-data-grid__row--editing"));
    }

    // ---- G-D26-04: the edit row keeps the height of a read row ----

    private const string EditorCell = ".omni-data-grid:not(.omni-data-grid--fixed-row-height) tbody tr.omni-data-grid__row--editing > td[data-omni-col]:not(.omni-data-grid__cell--text)";

    /// <summary>
    /// A read cell is its block padding twice and one line; an editor is one control tall, taller than the
    /// line in every density (28, 24 and 34 px against a 22.4 px line). The cells holding an editor give
    /// up half of the difference on each side, so the row keeps its read height, measured 40 then 45 px on
    /// the showcase before (default density). A control laid on the top of its line box makes that box
    /// exactly the taller of the line and the control: a baseline or middle alignment added the strut's
    /// overhang to it.
    /// </summary>
    [Fact]
    public void A_cell_holding_an_editor_trades_its_padding_for_the_control_so_the_row_keeps_its_height()
    {
        var cell = ShippedLookTests.Body(EditorCell);
        Assert.Equal(
            "max(0px, var(--omni-cell-pad-y) - max(0px, (var(--omni-control-height) - 1lh) / 2))",
            ShippedLookTests.Value(cell, "padding-block"));

        var controls = ShippedLookTests.Body(EditorCell + " :is(input, select, button, .omni-input, .omni-button)");
        Assert.Equal("top", ShippedLookTests.Value(controls, "vertical-align"));

        // A fixed row height keeps its own rule (recette R-017).
        Assert.Equal("0", ShippedLookTests.Value(ShippedLookTests.Body(".omni-data-grid--fixed-row-height tbody tr.omni-data-grid__row--editing td"), "padding-block"));
    }

    // ---- D3: alternating rows by default ----

    [Fact]
    public void Rows_alternate_by_default_and_a_grid_can_still_opt_out()
    {
        var grid = RenderGrid();
        Assert.True(grid.Instance.AllowAlternatingRows);
        Assert.Contains("omni-data-grid--striped", grid.Find(".omni-data-grid").ClassList);
        Assert.Contains("omni-data-grid__row--alternate", grid.Find("tbody tr[data-omni-row-index='1']").ClassList);
        Assert.DoesNotContain("omni-data-grid__row--alternate", grid.Find("tbody tr[data-omni-row-index='0']").ClassList);

        var plain = RenderGrid(parameters => parameters.Add(component => component.AllowAlternatingRows, false));
        Assert.DoesNotContain("omni-data-grid--striped", plain.Find(".omni-data-grid").ClassList);
        Assert.Empty(plain.FindAll(".omni-data-grid__row--alternate"));
    }

    /// <summary>
    /// The floor between two adjacent rows in dark mode (owner decision D3): at least the weakest light
    /// set, 1.064:1 at 4 % (Velours with the Essentiel palette).
    /// </summary>
    private const double StripeFloor = 1.06;

    /// <summary>
    /// An alternate row lays a share of the text colour over the grid frame. At 4 %, the light sets hold
    /// 1.064 to 1.093:1 between adjacent rows, the dark ones only 1.055:1 on a black frame (the Horizon
    /// palette, 2026-10-12). The dark scopes take 5 %, so every theme and palette in dark mode keeps the
    /// two rows at least as far apart as in light mode, while the light share stays 4 %.
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeContrastMatrixTests.Sets), MemberType = typeof(ThemeContrastMatrixTests))]
    public void Adjacent_rows_stay_apart_in_dark_mode(string themeName, string paletteName, OmniAppearance mode)
    {
        if (mode != OmniAppearance.Dark)
        {
            return;
        }

        var theme = OmniThemePresets.All.Single(entry => entry.Name == themeName);
        var palette = OmniThemePalettes.All.Single(entry => entry.Name == paletteName);
        var tokens = theme.With(palette).For(mode);
        var frame = ShowcaseThemeTests.Resolve(tokens, tokens.TryGetValue("--omni-grid-background", out var grid) ? grid
            : tokens.TryGetValue("--omni-card-background", out var card) ? card
            : tokens["--omni-color-surface"]);
        var stripe = ThemeColor.Mix(ShowcaseThemeTests.Resolve(tokens, tokens["--omni-color-text"]), frame, StripeShare(dark: true));

        var contrast = ThemeColor.Contrast(stripe, frame);
        Assert.True(contrast >= StripeFloor, string.Create(CultureInfo.InvariantCulture, $"{themeName} + {paletteName}: alternate row {stripe} on frame {frame}: {contrast:F3}, below {StripeFloor:F2}"));
    }

    [Fact]
    public void The_stripe_share_is_a_scope_token_four_percent_in_light_and_raised_in_dark()
    {
        var alternate = ShippedLookTests.Body(".omni-data-grid--striped .omni-data-grid__row--alternate");
        Assert.Equal("color-mix(in srgb, var(--omni-color-text) var(--omni-grid-stripe-share, 4%), transparent)", ShippedLookTests.Value(alternate, "--omni-row-fill"));
        Assert.Equal(0.04, StripeShare(dark: false));
        Assert.True(StripeShare(dark: true) > 0.04);
    }

    /// <summary>The share of the text colour an alternate row takes, read from the shipped scopes.</summary>
    private static double StripeShare(bool dark)
    {
        var body = ShippedLookTests.BodyWith(dark ? "[data-omni-theme=\"dark\"]" : "[data-omni-theme=\"light\"],\n[data-omni-theme=\"system\"]", "--omni-grid-stripe-share:");
        var value = ShippedLookTests.Value(body, "--omni-grid-stripe-share");
        return double.Parse(Percent().Match(value).Groups["value"].Value, CultureInfo.InvariantCulture) / 100;
    }

    [GeneratedRegex(@"^(?<value>\d+(?:\.\d+)?)%$")]
    private static partial Regex Percent();

    // ---- D4: the header in the cell font ----

    /// <summary>
    /// The header titles took the small size (12 px under 14 px cells). They now follow the cell font
    /// through a token whose default is the font of the grid itself (1em on the header cells); a host
    /// gives it <c>var(--omni-font-size-sm)</c> to restore the small titles. The padding of the header
    /// cells is the one of the body cells, so the header grows with its titles as a row grows with its text.
    /// </summary>
    [Fact]
    public void The_header_is_written_in_the_cell_font_through_its_own_token()
    {
        var head = ShippedLookTests.Body(".omni-data-grid thead > tr > *");
        Assert.Equal("var(--omni-grid-header-font-size, 1em)", ShippedLookTests.Value(head, "font-size"));
        Assert.Equal("var(--omni-font-size-base)", ShippedLookTests.Value(ShippedLookTests.Rules().First(rule => rule.Selector == ".omni-data-grid").Body, "font-size"));
        Assert.Equal("var(--omni-cell-pad-y) var(--omni-space-md)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-data-grid__table :is(th, td)"), "padding"));
    }
}
