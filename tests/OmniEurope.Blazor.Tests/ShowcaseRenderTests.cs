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
public sealed class ShowcaseRenderTests : OmniBunitContext
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
}
