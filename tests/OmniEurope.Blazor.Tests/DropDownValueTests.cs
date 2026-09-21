using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class DropDownValueTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<int?>> Types =
        [new OmniOption<int?>(0, "Normal"), new OmniOption<int?>(1, "Build")];

    [Fact]
    public void SelectValue_FollowsAValueResetFromCode()
    {
        int? value = 1;
        var dropDown = Render<OmniDropDown<int?>>(parameters => parameters
            .Add(component => component.Options, Types)
            .Add(component => component.AllowClear, true)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.Equal("1", dropDown.Find("select").GetAttribute("value"));

        // A "Clear filters" button sets the bound value back to null: the select itself must move back
        // to the empty option, not only lose the selected attribute of the former one.
        dropDown.Render(parameters => parameters.Add(component => component.Value, (int?)null));

        Assert.Equal(string.Empty, dropDown.Find("select").GetAttribute("value"));
    }
}
