using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;
using System.Globalization;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Surface added alongside the grid filter menus: the alert's severity and weight, and the compact
/// multi-select whose summary is the only thing a single-line control can show.
/// </summary>
public sealed class AlertAndMultiSelectTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<string>> Regions =
    [
        new("wal", "Wallonie"),
        new("bru", "Bruxelles"),
        new("vla", "Flandre", Disabled: true)
    ];

    [Fact]
    public void Alert_ProjectsItsSeverityVariantIconAndTitle()
    {
        var alert = Render<OmniAlert>(parameters => parameters
            .Add(component => component.Severity, OmniAlertSeverity.Warning)
            .Add(component => component.Variant, OmniAlertVariant.Filled)
            .Add(component => component.Title, "Espace disque")
            .Add(component => component.Icon, builder => builder.AddMarkupContent(0, "<i class=\"probe-icon\"></i>"))
            .AddChildContent("Le volume est presque plein."));

        var root = alert.Find(".omni-alert");
        Assert.Contains("omni-alert--warning", root.ClassName);
        Assert.Contains("omni-alert--filled", root.ClassName);
        Assert.Equal("Espace disque", alert.Find(".omni-alert__title").TextContent);
        Assert.Single(alert.FindAll(".omni-alert__icon .probe-icon"));
        Assert.Equal("status", root.GetAttribute("role"));
    }

    [Fact]
    public void Alert_DefaultsToOutlineAndOmitsTheTitleAndIconSlots()
    {
        var alert = Render<OmniAlert>(parameters => parameters
            .Add(component => component.Live, true)
            .AddChildContent("Enregistre."));

        var root = alert.Find(".omni-alert");
        Assert.Contains("omni-alert--outline", root.ClassName);
        Assert.Contains("omni-alert--info", root.ClassName);
        Assert.Empty(alert.FindAll(".omni-alert__title"));
        Assert.Empty(alert.FindAll(".omni-alert__icon"));
        Assert.Equal("alert", root.GetAttribute("role"));
    }

    [Fact]
    public void CompactMultiSelect_NamesASingleSelectionAndCountsSeveral()
    {
        IReadOnlyList<string> bound = [];
        var select = RenderCompact(bound, value => bound = value);

        Assert.Equal("Tout", Summary(select));

        select.FindAll(".omni-multi-select-compact__option input")[0].Change(true);
        select.Render(parameters => parameters.Add(component => component.Value, bound));
        Assert.Equal(new[] { "wal" }, bound);
        Assert.Equal("Wallonie", Summary(select));

        select.FindAll(".omni-multi-select-compact__option input")[1].Change(true);
        select.Render(parameters => parameters.Add(component => component.Value, bound));
        Assert.Equal(new[] { "wal", "bru" }, bound);

        // The wording belongs to the culture theory below; what matters here is that two selections
        // stop naming one of them and start counting.
        Assert.Contains("2", Summary(select), StringComparison.Ordinal);
        Assert.DoesNotContain("Wallonie", Summary(select), StringComparison.Ordinal);
    }

    [Fact]
    public void CompactMultiSelect_ShowsItsPlaceholderAndClearsOnDemand()
    {
        IReadOnlyList<string> bound = ["wal"];
        var select = RenderCompact(bound, value => bound = value, "Toutes les regions");

        // The clear control only exists while something is selected, so it cannot be a no-op button.
        select.Find(".omni-multi-select-compact__clear").Click();
        select.Render(parameters => parameters.Add(component => component.Value, bound));

        Assert.Empty(bound);
        Assert.Equal("Toutes les regions", Summary(select));
        Assert.Empty(select.FindAll(".omni-multi-select-compact__clear"));
    }

    [Fact]
    public void CompactMultiSelect_LeavesADisabledOptionUnselectable()
    {
        IReadOnlyList<string> bound = [];
        var select = RenderCompact(bound, value => bound = value);

        Assert.True(select.FindAll(".omni-multi-select-compact__option input")[2].HasAttribute("disabled"));
    }

    [Theory]
    [InlineData("fr-FR", "Tout", "2 sélectionnés", "Tout désélectionner")]
    [InlineData("en-US", "All", "2 selected", "Clear selection")]
    public void CompactMultiSelect_ResolvesItsOwnResourcesInBothCultures(
        string cultureName,
        string empty,
        string counted,
        string clear)
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            IReadOnlyList<string> none = [];
            Assert.Equal(empty, Summary(RenderCompact(none, _ => { })));

            IReadOnlyList<string> two = ["wal", "bru"];
            var select = RenderCompact(two, _ => { });
            Assert.Equal(counted, Summary(select));
            Assert.Equal(clear, select.Find(".omni-multi-select-compact__clear").TextContent.Trim());
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private static string Summary(IRenderedComponent<OmniMultiSelect<string>> select) =>
        select.Find(".omni-multi-select-compact__text").TextContent.Trim();

    private IRenderedComponent<OmniMultiSelect<string>> RenderCompact(
        IReadOnlyList<string> value,
        Action<IReadOnlyList<string>> onChanged,
        string? placeholder = null,
        bool filterable = false,
        Action<string?>? onFilterTextChanged = null)
    {
        var bound = value;
        return Render<OmniMultiSelect<string>>(parameters => parameters
            .Add(component => component.Options, Regions)
            .Add(component => component.Presentation, OmniMultiSelectPresentation.Compact)
            .Add(component => component.Placeholder, placeholder)
            .Add(component => component.Filterable, filterable)
            .Add(component => component.FilterTextChanged,
                EventCallback.Factory.Create(this, onFilterTextChanged ?? (_ => { })))
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => bound)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create(this, onChanged)));
    }

    [Fact]
    public void CompactMultiSelect_NarrowsItsListToWhatWasTypedAndSaysWhenNothingMatches()
    {
        IReadOnlyList<string> bound = [];
        var select = RenderCompact(bound, value => bound = value, filterable: true);

        select.Find(".omni-multi-select-compact__filter").Input("bru");
        Assert.Equal(["Bruxelles"], OptionTexts(select));

        // An empty panel would read as a control whose options never loaded.
        select.Find(".omni-multi-select-compact__filter").Input("zzz");
        Assert.Empty(select.FindAll(".omni-multi-select-compact__option"));
        Assert.Equal("Aucun résultat", select.Find(".omni-multi-select-compact__empty").TextContent.Trim());

        select.Find(".omni-multi-select-compact__filter").Input(string.Empty);
        Assert.Equal(3, select.FindAll(".omni-multi-select-compact__option").Count);
    }

    [Fact]
    public void CompactMultiSelect_StillSelectsTheOptionItNarrowedTo()
    {
        // Filtering renders a subset, and the toggle carries the option rather than its position in
        // the full list: addressing it by index would select whatever sat there before the filter.
        IReadOnlyList<string> bound = [];
        var select = RenderCompact(bound, value => bound = value, filterable: true);

        select.Find(".omni-multi-select-compact__filter").Input("flandre");
        select.Find(".omni-multi-select-compact__option input").Change(true);

        Assert.Equal(["vla"], bound);
    }

    [Fact]
    public void CompactMultiSelect_ReportsWhatIsTypedAndTakesItBack()
    {
        string? reported = null;
        IReadOnlyList<string> bound = [];
        var select = RenderCompact(bound, value => bound = value, filterable: true,
            onFilterTextChanged: text => reported = text);

        select.Find(".omni-multi-select-compact__filter").Input("wal");
        Assert.Equal("wal", reported);

        // Handing the parameter back empties the field, which is how a page clears the search after
        // acting on it.
        select.Render(parameters => parameters.Add(component => component.FilterText, null));
        Assert.True(string.IsNullOrEmpty(select.Find(".omni-multi-select-compact__filter").GetAttribute("value")));
        Assert.Equal(3, select.FindAll(".omni-multi-select-compact__option").Count);
    }

    [Fact]
    public void CompactMultiSelect_DrawsItsOptionsAndItsFooterFromTheTemplatesItIsGiven()
    {
        IReadOnlyList<string> bound = [];
        var select = Render<OmniMultiSelect<string>>(parameters => parameters
            .Add(component => component.Options, Regions)
            .Add(component => component.Presentation, OmniMultiSelectPresentation.Compact)
            .Add(component => component.Value, bound)
            .Add(component => component.ValueExpression, () => bound)
            .Add(component => component.OptionTemplate,
                option => builder => builder.AddMarkupContent(0, $"<i class=\"probe-swatch\">{option.Text}</i>"))
            .Add(component => component.FooterTemplate,
                builder => builder.AddMarkupContent(0, "<button class=\"probe-create\">+ nouveau</button>")));

        Assert.Equal(3, select.FindAll(".omni-multi-select-compact__option .probe-swatch").Count);
        Assert.Single(select.FindAll(".omni-multi-select-compact__footer .probe-create"));

        // The check box survives the template: the option is still selectable.
        Assert.Equal(3, select.FindAll(".omni-multi-select-compact__option input[type=checkbox]").Count);
    }

    [Fact]
    public void MultiSelect_RefusesToFilterAListPresentation()
    {
        // The list presentation is a native multiple select: it has nowhere to put a search field and
        // addresses its options by position, so a silently ignored filter would be the trap.
        IReadOnlyList<string> bound = [];
        var failure = Assert.Throws<InvalidOperationException>(() => Render<OmniMultiSelect<string>>(parameters => parameters
            .Add(component => component.Options, Regions)
            .Add(component => component.Filterable, true)
            .Add(component => component.Value, bound)
            .Add(component => component.ValueExpression, () => bound)));

        Assert.Contains("Compact", failure.Message, StringComparison.Ordinal);
    }

    private static string[] OptionTexts(IRenderedComponent<OmniMultiSelect<string>> select) =>
        [.. select.FindAll(".omni-multi-select-compact__option").Select(option => option.TextContent.Trim())];
}
