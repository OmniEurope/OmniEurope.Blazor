using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The tooltip's width and its "Show more" preview, the square clear action of every filter editor,
/// the heading scale a page title follows, the compact scale row of the appearance settings, and the
/// grid viewport that keeps its absolutely placed content inside its own scroll.
/// </summary>
public sealed class TooltipLengthAndFilterClearTests : OmniBunitContext
{
    private sealed record Row(int Number, string Name);

    private static readonly string LongText = string.Join(' ', Enumerable.Repeat("Une phrase de description assez longue.", 10));

    // ---- tooltip ----

    [Fact]
    public void Short_tooltip_shows_its_whole_text_without_an_action()
    {
        var tooltip = Render<OmniTooltip>(parameters => parameters.Add(component => component.Text, "Court."));

        Assert.Equal("Court.", tooltip.Find(".omni-tooltip__content").TextContent.Trim());
        Assert.Empty(tooltip.FindAll(".omni-tooltip__more"));
        Assert.DoesNotContain("omni-tooltip--expandable", tooltip.Find(".omni-tooltip").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Long_tooltip_opens_on_a_preview_and_unfolds_the_full_text()
    {
        var tooltip = Render<OmniTooltip>(parameters => parameters.Add(component => component.Text, LongText));

        var preview = tooltip.Find(".omni-tooltip__text").TextContent;
        Assert.EndsWith("…", preview, StringComparison.Ordinal);
        Assert.True(preview.Length <= OmniTooltip.DefaultCompactLength + 1);
        Assert.StartsWith(preview[..^1].TrimEnd(), LongText, StringComparison.Ordinal);
        var more = tooltip.Find(".omni-tooltip__more");
        Assert.Equal("Afficher plus", more.TextContent);
        Assert.Equal("false", more.GetAttribute("aria-expanded"));

        // The accessible description is the full text from the start, not the preview.
        var describedBy = tooltip.Find(".omni-tooltip__trigger").GetAttribute("aria-describedby")!;
        Assert.Equal(LongText, tooltip.Find($"#{describedBy}").TextContent);

        tooltip.Find(".omni-tooltip__more").Click();

        Assert.Equal(LongText, tooltip.Find(".omni-tooltip__text").TextContent);
        Assert.Equal("Afficher moins", tooltip.Find(".omni-tooltip__more").TextContent);
        Assert.Contains("omni-tooltip--expanded", tooltip.Find(".omni-tooltip").ClassName, StringComparison.Ordinal);

        tooltip.Find(".omni-tooltip").TriggerEvent("onmouseleave", new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.EndsWith("…", tooltip.Find(".omni-tooltip__text").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Compact_length_zero_never_shortens_the_text()
    {
        var tooltip = Render<OmniTooltip>(parameters => parameters
            .Add(component => component.Text, LongText)
            .Add(component => component.CompactLength, 0));

        Assert.Equal(LongText, tooltip.Find(".omni-tooltip__content").TextContent.Trim());
        Assert.Empty(tooltip.FindAll(".omni-tooltip__more"));
    }

    [Theory]
    [InlineData(OmniTooltipWidth.Standard, null)]
    [InlineData(OmniTooltipWidth.Narrow, "omni-tooltip--narrow")]
    [InlineData(OmniTooltipWidth.Wide, "omni-tooltip--wide")]
    public void Max_width_is_drawn_from_a_class(OmniTooltipWidth width, string? expected)
    {
        var tooltip = Render<OmniTooltip>(parameters => parameters
            .Add(component => component.Text, "Texte.")
            .Add(component => component.MaxWidth, width));

        var classes = tooltip.Find(".omni-tooltip").ClassList;
        Assert.Equal(expected is not null, expected is not null && classes.Contains(expected));
        Assert.Equal(width == OmniTooltipWidth.Standard, !classes.Any(name => name is "omni-tooltip--narrow" or "omni-tooltip--wide"));
        Assert.Equal("18rem", ShippedLookTests.Value(ShippedLookTests.Body(".omni-tooltip__content"), "max-width"));
    }

    // ---- filter clear actions ----

    [Theory]
    [InlineData(OmniDataGridFilterMode.SimpleWithMenu, OmniDataGridColumnFilterType.MultiSelect)]
    [InlineData(OmniDataGridFilterMode.Advanced, OmniDataGridColumnFilterType.Text)]
    [InlineData(OmniDataGridFilterMode.SimpleWithMenu, OmniDataGridColumnFilterType.Text)]
    public void Every_filter_editor_clears_with_the_same_square_icon_button(OmniDataGridFilterMode mode, OmniDataGridColumnFilterType type)
    {
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, new[] { new Row(1, "Alpha"), new Row(2, "Beta") })
            .Add(component => component.AllowFiltering, true)
            .Add(component => component.FilterMode, mode)
            .Add(component => component.ShowHeaderFilterMenu, true)
            .Add(component => component.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniDataGridColumn<Row>>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Key), "name");
                builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Nom");
                builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)(row => row.Name));
                builder.AddComponentParameter(4, nameof(OmniDataGridColumn<Row>.Filterable), true);
                builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Row>.FilterType), type);
                builder.CloseComponent();
            })));

        var clears = grid.FindAll(".omni-data-grid__filter-clear");
        Assert.NotEmpty(clears);
        Assert.All(clears, clear =>
        {
            Assert.Contains("omni-data-grid__filter-clear--icon", clear.ClassName, StringComparison.Ordinal);
            Assert.DoesNotContain("omni-button--small", clear.ClassName, StringComparison.Ordinal);
            Assert.Equal("Effacer", clear.GetAttribute("aria-label"));
            Assert.Equal(string.Empty, clear.TextContent.Trim());
        });
        Assert.All(grid.FindAll(".omni-data-grid__filter-apply"), apply => Assert.DoesNotContain("omni-button--small", apply.ClassName, StringComparison.Ordinal));
    }

    // ---- headings ----

    [Fact]
    public void Heading_scale_decreases_and_a_page_title_takes_the_h1_size()
    {
        var root = ShippedLookTests.Body(":root");
        var sizes = Enumerable.Range(1, 6)
            .Select(level => double.Parse(ShippedLookTests.Value(root, $"--omni-font-size-h{level}").Replace("rem", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture))
            .ToArray();

        Assert.Equal(new[] { 2.0, 1.5, 1.25, 1.125, 1.0, 0.875 }, sizes);
        Assert.DoesNotContain("font-size", ShippedLookTests.Body(".omni-page-header .omni-page-header__title"), StringComparison.Ordinal);

        var header = Render<OmniPageHeader>(parameters => parameters.Add(component => component.Title, "Titre"));
        var title = header.Find(".omni-page-header__title");
        Assert.Equal("H1", title.TagName);
        Assert.Contains("omni-heading--h1", title.ClassName, StringComparison.Ordinal);
    }

    // ---- appearance settings ----

    [Fact]
    public void Compact_scale_row_reads_as_a_short_label_a_summary_and_a_green_edit_action()
    {
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.Compact, true)
            .Add(component => component.TextSizeLevel, 6)
            .Add(component => component.DensityLevel, 4)
            .Add(component => component.ControlSizeLevel, 7)
            .Add(component => component.TextSizeLevelChanged, _ => { })
            .Add(component => component.DensityLevelChanged, _ => { })
            .Add(component => component.ControlSizeLevelChanged, _ => { }));

        var row = settings.Find(".omni-appearance-settings__row--scale");
        Assert.Equal("Tailles", row.QuerySelector(".omni-appearance-settings__label")!.TextContent.Trim());
        Assert.Equal("Texte 6 · Densité 4 · Contrôles 7", row.QuerySelector(".omni-appearance-settings__summary")!.TextContent);
        Assert.Contains("omni-button--success", row.QuerySelector("button")!.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Full_scale_row_keeps_its_title_and_the_green_edit_action()
    {
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.TextSizeLevelChanged, _ => { })
            .Add(component => component.DensityLevelChanged, _ => { }));

        var row = settings.Find(".omni-appearance-settings__row--scale");
        Assert.Equal("Taille du texte / Densité", row.QuerySelector(".omni-appearance-settings__label")!.TextContent.Trim());
        Assert.Empty(row.QuerySelectorAll(".omni-appearance-settings__summary"));
        Assert.Contains("omni-button--success", row.QuerySelector("button")!.ClassName, StringComparison.Ordinal);
    }

    // ---- autocomplete option icon ----

    [Fact]
    public async Task Autocomplete_draws_the_option_icon_before_the_highlighted_text()
    {
        var value = string.Empty;
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.DebounceMilliseconds, 0)
            .Add(component => component.Search, (_, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>([new("FR", "France"), new("BE", "Belgique")]))
            .Add(component => component.OptionIcon, option => builder => builder.AddMarkupContent(0, $"<i class=\"flag\">{option.Value}</i>")));

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "fr" });

        var first = autocomplete.FindAll(".omni-autocomplete__option")[0];
        var icon = first.QuerySelector(".omni-autocomplete__option-icon")!;
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
        Assert.Equal("FR", icon.TextContent);
        Assert.Same(icon, first.FirstElementChild);
        Assert.NotNull(first.QuerySelector("mark.omni-autocomplete__match"));
    }

    // ---- grid viewport ----

    [Fact]
    public void Grid_viewport_is_the_containing_block_of_its_absolutely_placed_content()
    {
        Assert.Equal("relative", ShippedLookTests.Value(ShippedLookTests.Body(".omni-data-grid__viewport"), "position"));
    }
}
