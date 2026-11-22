using Bunit;
using OmniEurope.Blazor.Components;
using Card = OmniEurope.Blazor.Tests.KanbanComponentTests.Card;

namespace OmniEurope.Blazor.Tests;

/// <summary>Renders a kanban board of test cards, for the kanban test classes.</summary>
internal static class KanbanRendering
{
    /// <summary>The three columns of the test boards: to do, in progress, done.</summary>
    public static readonly IReadOnlyList<OmniKanbanColumn> Columns = [new("todo", "À faire"), new("doing", "En cours"), new("done", "Terminé")];

    /// <summary>
    /// Renders a board of <paramref name="cards"/> over <see cref="Columns"/>, each card in the column it names and
    /// drawn as its name, then the parameters <paramref name="configure"/> adds.
    /// </summary>
    public static IRenderedComponent<OmniKanban<Card>> RenderKanban(
        this BunitContext context,
        IReadOnlyList<Card> cards,
        Action<ComponentParameterCollectionBuilder<OmniKanban<Card>>> configure) =>
        context.Render<OmniKanban<Card>>(parameters =>
        {
            parameters
                .Add(component => component.Columns, Columns)
                .Add(component => component.Items, cards)
                .Add(component => component.ColumnOf, card => card.Column)
                .Add(component => component.KeyOf, card => card.Name)
                .Add(component => component.CardTemplate, card => builder => builder.AddContent(0, card.Name));
            configure(parameters);
        });
}
