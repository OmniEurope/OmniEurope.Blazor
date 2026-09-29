using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A filtering <see cref="OmniDropDown{TValue}"/> renders an <see cref="OmniAutocomplete{TValue}"/>:
/// its field must carry the drop-down's id, accessible names and class, so a label can target it and
/// the host's styling still applies.
/// </summary>
public sealed class DropDownFilteringIdTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<int?>> Countries =
    [
        new(1, "Belgique"),
        new(2, "France")
    ];

    [Fact]
    public void FilteringDropDown_ForwardsItsIdAndAccessibleNamesToTheField()
    {
        int? value = null;
        var dropDown = Render<OmniDropDown<int?>>(parameters => parameters
            .Add(component => component.Id, "country")
            .Add(component => component.Filterable, true)
            .Add(component => component.Options, Countries)
            .Add(component => component.Label, "Pays")
            .Add(component => component.AriaDescribedBy, "country-help")
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        var field = dropDown.Find("input#country");
        Assert.Equal("Pays", field.GetAttribute("aria-label"));
        Assert.Contains("country-help", field.GetAttribute("aria-describedby") ?? string.Empty, StringComparison.Ordinal);
        Assert.Single(dropDown.FindAll("#country"));
    }

    [Fact]
    public void FilteringDropDown_KeepsItsClass_OnTheOutermostElement()
    {
        int? value = null;
        var dropDown = Render<OmniDropDown<int?>>(parameters => parameters
            .Add(component => component.Filterable, true)
            .Add(component => component.Class, "probe-width")
            .Add(component => component.Options, Countries)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        var root = dropDown.Find(".omni-autocomplete");
        Assert.Contains("probe-width", root.ClassList);
        Assert.DoesNotContain("probe-width", dropDown.Find("input").ClassList);
    }
}
