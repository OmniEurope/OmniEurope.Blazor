using System.Text.RegularExpressions;
using Bunit;
using OmniEurope.Blazor.Showcase.Demos;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Renders every demonstration of the gallery.
/// </summary>
/// <remarks>
/// Compiling a demo proves only that its parameter names exist on some type; it does not prove the
/// component accepts them. A wrong parameter, a missing required one or a bad cascade only surfaces
/// when the thing is actually rendered, which is what this does, once per gallery entry.
/// </remarks>
public sealed partial class ShowcaseRenderTests : OmniBunitContext
{
    public static TheoryData<string> DemoKeys
    {
        get
        {
            var keys = new TheoryData<string>();
            foreach (var demo in DemoCatalog.All)
            {
                keys.Add(demo.Key);
            }

            return keys;
        }
    }

    [Theory]
    [MemberData(nameof(DemoKeys))]
    public void EveryDemo_RendersWithoutThrowing(string key)
    {
        var demo = DemoCatalog.Resolve(key);

        var rendered = Render(builder =>
        {
            builder.OpenComponent(0, demo.Component);
            builder.CloseComponent();
        });

        Assert.False(string.IsNullOrWhiteSpace(rendered.Markup), $"{key} rendered nothing.");
    }

    [Theory]
    [MemberData(nameof(DemoKeys))]
    public void EveryDemo_StaysWithinTheContentSecurityPolicy(string key)
    {
        var demo = DemoCatalog.Resolve(key);

        var rendered = Render(builder =>
        {
            builder.OpenComponent(0, demo.Component);
            builder.CloseComponent();
        });

        Assert.DoesNotContain("style=", rendered.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<style", rendered.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", rendered.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(DemoKeys))]
    public void EveryDemo_BindsTheParametersItNames(string key)
    {
        var demo = DemoCatalog.Resolve(key);

        var rendered = Render(builder =>
        {
            builder.OpenComponent(0, demo.Component);
            builder.CloseComponent();
        });

        // A C# enum literal reaching the browser as an attribute value means the attribute was not a
        // parameter of the component: it fell through to the unmatched-attribute bag, so the setting
        // it advertises does nothing. The gallery would then show working code that does not work.
        var leaked = LeakedEnumLiteral().Match(rendered.Markup);
        Assert.False(leaked.Success, $"{key} passes {leaked.Value} to a component that has no such parameter.");
    }

    [GeneratedRegex("[a-z-]+=\"Omni[A-Za-z]+\\.[A-Za-z]+\"")]
    private static partial Regex LeakedEnumLiteral();
}
