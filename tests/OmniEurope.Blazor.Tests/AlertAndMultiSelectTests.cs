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
        string? placeholder = null)
    {
        var bound = value;
        return Render<OmniMultiSelect<string>>(parameters => parameters
            .Add(component => component.Options, Regions)
            .Add(component => component.Presentation, OmniMultiSelectPresentation.Compact)
            .Add(component => component.Placeholder, placeholder)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => bound)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create(this, onChanged)));
    }
}
