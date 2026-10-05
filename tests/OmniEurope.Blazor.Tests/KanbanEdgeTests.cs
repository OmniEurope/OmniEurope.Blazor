using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using Card = OmniEurope.Blazor.Tests.KanbanComponentTests.Card;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniKanban at its edges: no items, a card outside every column, a column removed under the card
/// being carried, cards without a label nor a key, a drag that survives an unrelated render, and the
/// script that arrives late, on a lost circuit, or after the board went.
/// </summary>
public sealed class KanbanEdgeTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniKanbanColumn> Columns = [new("todo", "À faire"), new("doing", "En cours"), new("done", "Terminé")];

    private static readonly IReadOnlyList<Card> Cards = [new("A", "todo"), new("C", "doing"), new("X", "archived")];

    private readonly List<OmniKanbanMove<Card>> _moves = [];

    private IRenderedComponent<OmniKanban<Card>> RenderBoard(Action<ComponentParameterCollectionBuilder<OmniKanban<Card>>>? configure = null) =>
        Render<OmniKanban<Card>>(parameters =>
        {
            parameters
                .Add(component => component.Columns, Columns)
                .Add(component => component.Items, Cards)
                .Add(component => component.ColumnOf, card => card.Column)
                .Add(component => component.KeyOf, card => card.Name)
                .Add(component => component.CardTemplate, card => builder => builder.AddContent(0, card.Name))
                .Add(component => component.OnItemMove, EventCallback.Factory.Create<OmniKanbanMove<Card>>(this, move => _moves.Add(move)));
            configure?.Invoke(parameters);
        });

    private static Task KeyAsync<T>(IRenderedComponent<OmniKanban<T>> board, int card, string key) =>
        board.InvokeAsync(() => board.Instance.HandleCardKeyAsync(card.ToString(System.Globalization.CultureInfo.InvariantCulture), key));

    [Fact]
    public void NullItems_DrawEmptyColumns()
    {
        var board = RenderBoard();

        board.Render(parameters => parameters.Add(component => component.Items, null));
        Assert.Empty(board.FindAll("[data-omni-kanban-card]"));
        Assert.Equal(3, board.FindAll("section.omni-kanban__column").Count);
    }

    [Fact]
    public async Task CardOutsideEveryColumn_CannotBePickedUp()
    {
        JSInterop.SetupModule(Internal.OmniModules.Kanban);
        var board = RenderBoard();

        await KeyAsync(board, 2, " ");

        Assert.False(board.Instance.IsGrabbing);
    }

    [Fact]
    public async Task ColumnRemovedUnderTheCarriedCard_IsNamedByItsKey_AndTheArrowsStartFromTheFirstColumn()
    {
        JSInterop.SetupModule(Internal.OmniModules.Kanban);
        var board = RenderBoard();
        await KeyAsync(board, 1, " ");

        board.Render(parameters => parameters.Add(component => component.Columns, [Columns[0], Columns[2]]));
        await KeyAsync(board, 1, "ArrowUp");
        Assert.Equal("C : colonne doing, position 1 sur 1.", board.Instance.Announcement);

        await KeyAsync(board, 1, "ArrowRight");
        Assert.Equal("C : colonne Terminé, position 1 sur 1.", board.Instance.Announcement);
    }

    [Fact]
    public async Task CardsWithoutLabelNorKey_AreNamedByTheirTextOrNothing()
    {
        JSInterop.SetupModule(Internal.OmniModules.Kanban);
        var moves = new List<OmniKanbanMove<string?>>();
        var board = Render<OmniKanban<string?>>(parameters => parameters
            .Add(component => component.Columns, Columns)
            .Add(component => component.Items, [null, "B"])
            .Add(component => component.ColumnOf, _ => "todo")
            .Add(component => component.KeyOf, _ => null!)
            .Add(component => component.ItemLabel, _ => null!)
            .Add(component => component.CardTemplate, card => builder => builder.AddContent(0, card))
            .Add(component => component.OnItemMove, EventCallback.Factory.Create<OmniKanbanMove<string?>>(this, move => moves.Add(move))));

        await KeyAsync(board, 1, " ");
        Assert.Equal("B saisie, colonne À faire, position 2 sur 2.", board.Instance.Announcement);
        await KeyAsync(board, 1, "Escape");

        await KeyAsync(board, 0, " ");
        // A refresh finds the carried card again by its key: here the card itself, null read as empty.
        board.Render(parameters => parameters.Add(component => component.Items, [null, "B"]));
        Assert.True(board.Instance.IsGrabbing);
        Assert.Equal(" saisie, colonne À faire, position 1 sur 2.", board.Instance.Announcement);
        await KeyAsync(board, 0, "ArrowDown");
        await KeyAsync(board, 0, "Enter");
        Assert.Equal(new OmniKanbanMove<string?>(null, "todo", "todo", 1), Assert.Single(moves));
    }

    [Fact]
    public async Task BoardWithoutKeys_FindsTheCarriedCardByItself()
    {
        JSInterop.SetupModule(Internal.OmniModules.Kanban);
        var board = Render<OmniKanban<Card>>(parameters => parameters
            .Add(component => component.Columns, Columns)
            .Add(component => component.Items, Cards)
            .Add(component => component.ColumnOf, card => card.Column)
            .Add(component => component.CardTemplate, card => builder => builder.AddContent(0, card.Name))
            .Add(component => component.OnItemMove, EventCallback.Factory.Create<OmniKanbanMove<Card>>(this, move => _moves.Add(move))));
        await KeyAsync(board, 1, " ");

        board.Render(parameters => parameters.Add(component => component.Items, [Cards[1], Cards[0]]));

        Assert.True(board.Instance.IsGrabbing);
        Assert.Contains("omni-kanban__card--grabbed", board.Find("[data-omni-kanban-card='0']").ClassList);
    }

    [Fact]
    public void DragKept_ByARenderWithTheSameItems()
    {
        var board = RenderBoard();
        board.Find("[data-omni-kanban-card='0']").DragStart();
        board.Find("[data-omni-kanban-column='doing'] ul").DragEnter();

        board.Render(parameters => parameters.Add(component => component.Label, "Suivi"));
        board.Find("[data-omni-kanban-column='doing'] ul").Drop();

        Assert.Equal(new OmniKanbanMove<Card>(Cards[0], "todo", "doing", 1), Assert.Single(_moves));
    }

    [Fact]
    public async Task BoardGoneWhileItsScriptLoads_ReleasesItOnArrival_AndARenderAfterItDoesNothing()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var board = RenderBoard();

        await board.Instance.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);
        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        board.Render();

        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task LostCircuit_WhileAttachingOrDetaching_IsIgnored()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["attach"] = new JSDisconnectedException("perdu");
        runtime.Module.CallFailures["detach"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var board = RenderBoard();
        Assert.Equal(["attach"], runtime.Module.Calls);

        await board.Instance.DisposeAsync();

        Assert.Equal(["attach", "detach"], runtime.Module.Calls);
        Assert.False(runtime.Module.Disposal.Task.IsCompleted);
    }
}
