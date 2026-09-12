using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class StackTests : OmniBunitContext
{
    [Fact]
    public void Scroll_WrapsTheRowBetweenTwoHiddenChevronsThatTheOverflowScriptOwns()
    {
        var stack = Render<OmniStack>(parameters => parameters
            .Add(component => component.Orientation, OmniStackOrientation.Horizontal)
            .Add(component => component.Overflow, OmniStackOverflow.Scroll)
            .Add(component => component.Class, "host")
            .AddUnmatched("role", "group")
            .AddChildContent("<button>Un</button><button>Deux</button>"));

        // The host's class and attributes stay on the outer element, the stack classes on the row.
        var wrapper = stack.Find(".omni-stack-scroll");
        Assert.Contains("host", wrapper.ClassList);
        Assert.Equal("group", wrapper.GetAttribute("role"));
        var row = wrapper.QuerySelector(":scope > .omni-stack-scroll__viewport")!;
        Assert.Contains("omni-stack--overflow-scroll", row.ClassList);
        Assert.Equal(2, row.QuerySelectorAll("button").Length);

        // The chevrons are decorative mouse affordances: hidden until the script sees an overflow,
        // out of the tab order and of the accessibility tree.
        var chevrons = wrapper.QuerySelectorAll(":scope > .omni-stack-scroll__button");
        Assert.Equal(2, chevrons.Length);
        Assert.All(chevrons, chevron =>
        {
            Assert.True(chevron.HasAttribute("hidden"));
            Assert.Equal("-1", chevron.GetAttribute("tabindex"));
            Assert.Equal("true", chevron.GetAttribute("aria-hidden"));
        });
        JSInterop.VerifyInvoke("configureScrollOverflow");
        Assert.DoesNotContain("style=", stack.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Scroll_ReleasesTheOverflowScriptWhenDisposed()
    {
        Render<OmniStack>(parameters => parameters
            .Add(component => component.Orientation, OmniStackOrientation.Horizontal)
            .Add(component => component.Overflow, OmniStackOverflow.Scroll)
            .AddChildContent("<button>Un</button>"));

        await DisposeComponentsAsync();

        // The observers and listeners live in the browser: without this call they would outlive the row.
        JSInterop.VerifyInvoke("disposeScrollOverflow");
    }

    [Theory]
    [InlineData(OmniStackOverflow.None)]
    [InlineData(OmniStackOverflow.Collapse)]
    public void OtherModes_KeepASingleElementAndNoScript(OmniStackOverflow overflow)
    {
        var stack = Render<OmniStack>(parameters => parameters
            .Add(component => component.Orientation, OmniStackOrientation.Horizontal)
            .Add(component => component.Overflow, overflow)
            .AddChildContent("Contenu"));

        Assert.Empty(stack.FindAll(".omni-stack-scroll"));
        Assert.Single(stack.FindAll(".omni-stack"));
        Assert.Empty(JSInterop.Invocations);
    }
}
