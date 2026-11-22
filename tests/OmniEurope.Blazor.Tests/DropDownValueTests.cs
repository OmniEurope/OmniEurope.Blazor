using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class DropDownValueTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<int?>> Types =
        [new OmniOption<int?>(0, "Normal"), new OmniOption<int?>(1, "Build")];

    // The types with an empty option, bound to Build, each value raised kept in <paramref name="raised"/> when given.
    private IRenderedComponent<OmniDropDown<int?>> RenderEmptiableAtBuild(List<int?>? raised = null)
    {
        int? value = 1;
        return Render<OmniDropDown<int?>>(parameters =>
        {
            parameters
                .Add(component => component.Options, Types)
                .Add(component => component.AllowEmpty, true)
                .Add(component => component.Value, value);
            if (raised is not null)
            {
                parameters.Add(component => component.ValueChanged, changed => raised.Add(changed));
            }

            parameters.Add(component => component.ValueExpression, () => value);
        });
    }

    [Fact]
    public void SelectValue_FollowsAValueResetFromCode()
    {
        var dropDown = RenderEmptiableAtBuild();

        Assert.Equal("1", dropDown.Find("select").GetAttribute("value"));

        // A "Clear filters" button sets the bound value back to null: the select itself must move back
        // to the empty option, not only lose the selected attribute of the former one.
        dropDown.Render(parameters => parameters.Add(component => component.Value, (int?)null));

        Assert.Equal(string.Empty, dropDown.Find("select").GetAttribute("value"));
    }

    [Fact]
    public void AllowEmpty_TheEmptyOptionClearsTheValue_ThroughValueChanged()
    {
        var raised = new List<int?>();
        var dropDown = RenderEmptiableAtBuild(raised);

        dropDown.Find("select").Change(string.Empty);

        Assert.Equal([(int?)null], raised);
    }
}
