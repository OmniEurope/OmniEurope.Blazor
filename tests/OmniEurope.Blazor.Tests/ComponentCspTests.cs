using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class ComponentCspTests : BunitContext
{
    [Fact]
    public void PilotComponents_RenderWithoutInlineStyles()
    {
        var button = Render<OmniButton>(parameters => parameters.AddChildContent("Enregistrer"));
        var card = Render<OmniCard>(parameters => parameters.AddChildContent("Contenu"));
        var stack = Render<OmniStack>(parameters => parameters.AddChildContent("Contenu"));
        var alert = Render<OmniAlert>(parameters => parameters.AddChildContent("Information"));

        AssertMarkupHasNoInlineStyle(button.Markup);
        AssertMarkupHasNoInlineStyle(card.Markup);
        AssertMarkupHasNoInlineStyle(stack.Markup);
        AssertMarkupHasNoInlineStyle(alert.Markup);
    }

    [Fact]
    public void AdditionalStyleAttribute_IsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Render<OmniCard>(parameters => parameters
                .AddChildContent("Contenu")
                .AddUnmatched("style", "color: red")));

        Assert.Contains("Inline style attributes are forbidden", exception.Message);
    }

    [Fact]
    public void StringEventHandlerAttribute_IsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Render<OmniButton>(parameters => parameters
                .AddChildContent("Action")
                .AddUnmatched("onmouseover", "alert(1)")));

        Assert.Contains("Inline event handler", exception.Message);
    }

    private static void AssertMarkupHasNoInlineStyle(string markup) =>
        Assert.False(markup.Contains("style=", StringComparison.OrdinalIgnoreCase), markup);
}
