using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>Renders a lone selectable card, for the form and feedback test classes.</summary>
internal static class SelectableCardRendering
{
    /// <summary>Renders the multiple-choice card "Docker" holding <paramref name="value"/>, each change added to <paramref name="picked"/>.</summary>
    public static IRenderedComponent<OmniSelectableCard> RenderDockerCard(this BunitContext context, bool value, List<bool> picked) =>
        context.Render<OmniSelectableCard>(parameters => parameters
            .Add(component => component.Title, "Docker")
            .Add(component => component.Multiple, true)
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => picked.Add(next)));
}
