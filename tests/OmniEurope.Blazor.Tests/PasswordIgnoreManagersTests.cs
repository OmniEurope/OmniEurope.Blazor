using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A secret that is not the user's login password (a vault value): a masked text field that password
/// managers leave alone, with the same eye as a password field.
/// </summary>
public sealed class PasswordIgnoreManagersTests : OmniBunitContext
{
    private string _value = "s3cret";

    [Fact]
    public void IgnorePasswordManagers_IsAMaskedTextField_MarkedForManagersToSkip()
    {
        var field = Render<OmniPassword>(parameters => parameters
            .Add(component => component.Value, _value)
            .Add(component => component.ValueExpression, () => _value)
            .Add(component => component.IgnorePasswordManagers, true));

        var input = field.Find("input");
        Assert.Equal("text", input.GetAttribute("type"));
        Assert.Contains("omni-password__input--masked", input.ClassList);
        Assert.Equal("off", input.GetAttribute("autocomplete"));
        Assert.Equal("true", input.GetAttribute("data-lpignore"));
        Assert.Equal("true", input.GetAttribute("data-1p-ignore"));
        Assert.Equal("true", input.GetAttribute("data-bwignore"));
        Assert.Equal("other", input.GetAttribute("data-form-type"));

        field.Find(".omni-password__toggle").Click();

        Assert.DoesNotContain("omni-password__input--masked", field.Find("input").ClassList);
        Assert.Equal("text", field.Find("input").GetAttribute("type"));
    }

    [Fact]
    public void ByDefault_StaysAPasswordField()
    {
        var field = Render<OmniPassword>(parameters => parameters
            .Add(component => component.ValueExpression, () => _value));

        var input = field.Find("input");
        Assert.Equal("password", input.GetAttribute("type"));
        Assert.Equal("current-password", input.GetAttribute("autocomplete"));
        Assert.Null(input.GetAttribute("data-lpignore"));
        Assert.DoesNotContain("omni-password__input--masked", input.ClassList);
    }
}
