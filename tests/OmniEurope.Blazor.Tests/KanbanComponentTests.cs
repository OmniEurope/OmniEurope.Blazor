using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniKanban{TItem}"/> in bUnit. The keys pressed on a card reach .NET through
/// <c>omni-kanban.js</c>; here they are sent through the same internal handler the script's bridge
/// calls, so the whole keyboard move (pick up, carry across and along the columns, drop, cancel) and
/// its announcements are covered. The mouse path is driven with bUnit's drag events.
/// </summary>
public sealed class KanbanComponentTests : OmniBunitContext
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-kanban.js";

    private static readonly IReadOnlyList<OmniKanbanColumn> Columns =
    [
        new("todo", "À faire"),
        new("doing", "En cours"),
        new("done", "Terminé")
    ];

    private static readonly IReadOnlyList<Card> Cards =
    [
        new("A", "todo"),
        new("B", "todo"),
        new("C", "doing"),
        new("X", "archived")
    ];

    private readonly List<OmniKanbanMove<Card>> _moves = [];

    [Fact]
    public void Board_IsANamedRegion_WithOneLabelledListPerColumn_AndItsCountHeard()
    {
        var board = RenderBoard();

        var root = board.Find(".omni-kanban");
        Assert.Equal("region", root.GetAttribute("role"));
        Assert.Equal("Tableau de cartes", root.GetAttribute("aria-label"));
        var sections = board.FindAll("section.omni-kanban__column");
        Assert.Equal(3, sections.Count);
        Assert.Equal(["todo", "doing", "done"], sections.Select(section => section.GetAttribute("data-omni-kanban-column")));
        foreach (var section in sections)
        {
            var header = section.GetAttribute("aria-labelledby");
            Assert.NotNull(board.Find($"#{header}"));
            Assert.Equal(header, section.QuerySelector("ul")!.GetAttribute("aria-labelledby"));
        }

        var todo = sections[0];
        Assert.Equal("À faire", todo.QuerySelector(".omni-kanban__title")!.TextContent);
        Assert.Equal("true", todo.QuerySelector(".omni-kanban__count")!.GetAttribute("aria-hidden"));
        Assert.Equal("2", todo.QuerySelector(".omni-kanban__count")!.TextContent);
        Assert.Equal("Cartes : 2", todo.QuerySelector(".omni-kanban__header .omni-visually-hidden")!.TextContent);
        Assert.Null(todo.QuerySelector(".omni-kanban__count")!.GetAttribute("aria-label"));
        Assert.Equal("polite", board.Find("[role=status]").GetAttribute("aria-live"));
        Assert.DoesNotContain("style=", board.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cards_AreFocusable_DescribedByTheInstructions_AndDrawnInTheirColumnInItemOrder()
    {
        var board = RenderBoard();

        Assert.Equal(["A", "B"], CardsOf(board, "todo"));
        Assert.Equal(["C"], CardsOf(board, "doing"));
        Assert.Empty(CardsOf(board, "done"));
        Assert.DoesNotContain("X", board.Markup, StringComparison.Ordinal);

        var card = board.Find("[data-omni-kanban-card='0']");
        Assert.Equal("0", card.GetAttribute("tabindex"));
        Assert.Equal("true", card.GetAttribute("draggable"));
        var help = board.Find($"#{card.GetAttribute("aria-describedby")}");
        Assert.StartsWith("Espace ou Entrée pour saisir la carte", help.TextContent, StringComparison.Ordinal);
        Assert.Equal("Aucune carte", board.Find("[data-omni-kanban-column='done'] .omni-kanban__empty").TextContent);
    }

    [Fact]
    public void TextParameters_AndTheHeaderTemplate_ReplaceTheDefaults()
    {
        var board = RenderBoard(parameters => parameters
            .Add(component => component.Label, "Suivi")
            .Add(component => component.EmptyColumnText, "Rien ici")
            .Add(component => component.Id, "board")
            .Add(component => component.ColumnHeaderTemplate, column => builder => builder.AddContent(0, $"[{column.Key}]")));

        Assert.Equal("Suivi", board.Find(".omni-kanban").GetAttribute("aria-label"));
        Assert.Equal("board", board.Find(".omni-kanban").Id);
        Assert.Equal("board-help", board.Find("[data-omni-kanban-card='0']").GetAttribute("aria-describedby"));
        Assert.Equal("Rien ici", board.Find(".omni-kanban__empty").TextContent);
        Assert.Equal("[doing]", board.Find("[data-omni-kanban-column='doing'] .omni-kanban__header").TextContent);
        Assert.Empty(board.FindAll(".omni-kanban__count"));
    }

    [Fact]
    public void RequiredParameters_AreChecked()
    {
        Assert.Throws<ArgumentNullException>(() => Render<OmniKanban<Card>>(parameters => parameters
            .Add(component => component.Columns, Columns)
            .Add(component => component.Items, Cards)
            .Add(component => component.ColumnOf, card => card.Column)));
        Assert.Throws<ArgumentNullException>(() => Render<OmniKanban<Card>>(parameters => parameters
            .Add(component => component.Columns, Columns)
            .Add(component => component.Items, Cards)
            .Add(component => component.CardTemplate, card => builder => builder.AddContent(0, card.Name))));
    }

    [Fact]
    public async Task Keyboard_PicksACardUp_CarriesItAcrossAndDown_ThenDropsIt()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var board = RenderBoard();

        await KeyAsync(board, 0, " ");
        Assert.True(board.Instance.IsGrabbing);
        Assert.Equal("true", board.Find(".omni-kanban").GetAttribute("data-omni-kanban-grabbed"));
        Assert.Contains("omni-kanban__card--grabbed", board.Find("[data-omni-kanban-card='0']").ClassList);
        Assert.Equal("A saisie, colonne À faire, position 1 sur 2.", board.Instance.Announcement);

        await KeyAsync(board, 0, "ArrowRight");
        Assert.Equal("A : colonne En cours, position 1 sur 2.", board.Instance.Announcement);
        Assert.Equal(["A", "C"], CardsOf(board, "doing"));
        Assert.Equal(["B"], CardsOf(board, "todo"));

        await KeyAsync(board, 0, "ArrowDown");
        Assert.Equal("A : colonne En cours, position 2 sur 2.", board.Instance.Announcement);
        Assert.Equal(["C", "A"], CardsOf(board, "doing"));

        // Past the last place and past the last column, the card stays where it is.
        await KeyAsync(board, 0, "ArrowDown");
        Assert.Equal(["C", "A"], CardsOf(board, "doing"));
        await KeyAsync(board, 0, "ArrowRight");
        await KeyAsync(board, 0, "ArrowRight");
        Assert.Equal(["A"], CardsOf(board, "done"));
        // Back in the previous column, the card keeps the place it had reached in the last one.
        await KeyAsync(board, 0, "ArrowLeft");
        Assert.Equal(["A", "C"], CardsOf(board, "doing"));

        Assert.Empty(_moves);
        await KeyAsync(board, 0, "Enter");

        var move = Assert.Single(_moves);
        Assert.Equal(new OmniKanbanMove<Card>(Cards[0], "todo", "doing", 0), move);
        Assert.False(board.Instance.IsGrabbing);
        Assert.Null(board.Find(".omni-kanban").GetAttribute("data-omni-kanban-grabbed"));
        Assert.Equal("A déposée, colonne En cours, position 1 sur 2.", board.Instance.Announcement);

        // The focus follows the card after every key, the element having moved to another list.
        Assert.All(module.Invocations["focusCard"], invocation => Assert.Equal("0", invocation.Arguments[1]));
        Assert.Equal(8, module.Invocations["focusCard"].Count);
    }

    [Fact]
    public async Task Keyboard_MovesACardWithinItsColumn()
    {
        JSInterop.SetupModule(ModulePath);
        var board = RenderBoard();

        await KeyAsync(board, 1, "Enter");
        await KeyAsync(board, 1, "ArrowUp");
        Assert.Equal(["B", "A"], CardsOf(board, "todo"));
        await KeyAsync(board, 1, " ");

        Assert.Equal(new OmniKanbanMove<Card>(Cards[1], "todo", "todo", 0), Assert.Single(_moves));
    }

    [Fact]
    public async Task Keyboard_Escape_PutsTheCardBack_AndADropWhereItWasReportsNothing()
    {
        JSInterop.SetupModule(ModulePath);
        var board = RenderBoard();

        await KeyAsync(board, 0, " ");
        await KeyAsync(board, 0, "ArrowRight");
        await KeyAsync(board, 0, "Escape");

        Assert.False(board.Instance.IsGrabbing);
        Assert.Equal(["A", "B"], CardsOf(board, "todo"));
        Assert.Equal("Déplacement annulé : A reste dans la colonne À faire.", board.Instance.Announcement);

        await KeyAsync(board, 0, " ");
        await KeyAsync(board, 0, " ");

        Assert.Empty(_moves);
        Assert.Equal("A déposée, colonne À faire, position 1 sur 2.", board.Instance.Announcement);
    }

    [Fact]
    public async Task Keyboard_ArrowsWithoutACardHeld_AndOtherKeys_AreLeftToTheScript()
    {
        JSInterop.SetupModule(ModulePath);
        var board = RenderBoard();

        await KeyAsync(board, 0, "ArrowRight");
        await KeyAsync(board, 0, "Escape");
        await KeyAsync(board, 0, "a");
        await KeyAsync(board, 9, " ");
        await KeyAsync(board, 0, "not-a-number");

        Assert.False(board.Instance.IsGrabbing);
        Assert.Empty(board.Instance.Announcement);
        Assert.Equal(["A", "B"], CardsOf(board, "todo"));
    }

    [Fact]
    public async Task Keyboard_AKeyOnAnotherCard_AbandonsTheMoveInProgress()
    {
        JSInterop.SetupModule(ModulePath);
        var board = RenderBoard();

        await KeyAsync(board, 0, " ");
        await KeyAsync(board, 0, "ArrowRight");
        await KeyAsync(board, 2, "ArrowDown");

        Assert.False(board.Instance.IsGrabbing);
        Assert.Equal(["A", "B"], CardsOf(board, "todo"));
        Assert.Equal(["C"], CardsOf(board, "doing"));
        Assert.Equal("Déplacement annulé : A reste dans la colonne À faire.", board.Instance.Announcement);
        Assert.Empty(_moves);
    }

    [Fact]
    public async Task RepeatedAnnouncements_AreMadeDifferent_SoTheyAreReadAgain()
    {
        JSInterop.SetupModule(ModulePath);
        var board = RenderBoard();
        await KeyAsync(board, 0, " ");
        await KeyAsync(board, 0, "ArrowDown");
        var first = board.Find("[role=status]").TextContent;

        await KeyAsync(board, 0, "ArrowDown");

        var second = board.Find("[role=status]").TextContent;
        Assert.NotEqual(first, second);
        Assert.Equal(first.TrimEnd(' '), second.TrimEnd(' '));
    }

    [Fact]
    public async Task ReadOnly_ShowsTheBoard_WithoutAnyMove()
    {
        JSInterop.SetupModule(ModulePath);
        var board = RenderBoard(parameters => parameters.Add(component => component.ReadOnly, true));

        Assert.Contains("omni-kanban--readonly", board.Find(".omni-kanban").ClassList);
        var card = board.Find("[data-omni-kanban-card='0']");
        Assert.Equal("false", card.GetAttribute("draggable"));
        Assert.Null(card.GetAttribute("aria-describedby"));

        await KeyAsync(board, 0, " ");
        card.DragStart();
        board.Find("[data-omni-kanban-column='done'] ul").DragEnter();
        board.Find("[data-omni-kanban-column='done'] ul").Drop();

        Assert.False(board.Instance.IsGrabbing);
        Assert.Empty(_moves);
    }

    [Fact]
    public async Task ReadOnly_SetWhileACardIsHeld_EndsTheMove()
    {
        JSInterop.SetupModule(ModulePath);
        var board = RenderBoard();
        await KeyAsync(board, 0, " ");

        board.Render(parameters => parameters.Add(component => component.ReadOnly, true));

        Assert.False(board.Instance.IsGrabbing);
        Assert.Equal(["A", "B"], CardsOf(board, "todo"));
    }

    [Fact]
    public void Mouse_DropBeforeACardOfAnotherColumn_ReportsTheMoveAndMarksTheSlot()
    {
        var board = RenderBoard();

        board.Find("[data-omni-kanban-card='0']").DragStart();
        board.Find("[data-omni-kanban-card='2']").DragEnter();

        Assert.Contains("omni-kanban__card--dragging", board.Find("[data-omni-kanban-card='0']").ClassList);
        Assert.Contains("omni-kanban__card--drop-before", board.Find("[data-omni-kanban-card='2']").ClassList);
        Assert.Contains("omni-kanban__column--drop", board.Find("[data-omni-kanban-column='doing']").ClassList);

        board.Find("[data-omni-kanban-column='doing'] ul").Drop();

        Assert.Equal(new OmniKanbanMove<Card>(Cards[0], "todo", "doing", 0), Assert.Single(_moves));
        Assert.Equal("A déposée, colonne En cours, position 1 sur 2.", board.Instance.Announcement);
        Assert.Empty(board.FindAll(".omni-kanban__column--drop"));
        Assert.Empty(board.FindAll(".omni-kanban__card--dragging"));
    }

    [Fact]
    public void Mouse_DropInAnEmptyColumn_PlacesTheCardFirst()
    {
        var board = RenderBoard();

        board.Find("[data-omni-kanban-card='1']").DragStart();
        board.Find("[data-omni-kanban-column='done'] ul").DragEnter();
        board.Find("[data-omni-kanban-column='done'] ul").Drop();

        Assert.Equal(new OmniKanbanMove<Card>(Cards[1], "todo", "done", 0), Assert.Single(_moves));
    }

    [Fact]
    public void Mouse_ADropOnItself_OrADragAbandoned_ReportsNothing()
    {
        var board = RenderBoard();

        board.Find("[data-omni-kanban-card='0']").DragStart();
        board.Find("[data-omni-kanban-card='0']").DragEnter();
        board.Find("[data-omni-kanban-column='todo'] ul").Drop();
        Assert.Empty(_moves);

        board.Find("[data-omni-kanban-card='0']").DragStart();
        board.Find("[data-omni-kanban-card='2']").DragEnter();
        board.Find("[data-omni-kanban-card='0']").DragEnd();
        board.Find("[data-omni-kanban-column='doing'] ul").Drop();

        Assert.Empty(_moves);
        Assert.Empty(board.FindAll(".omni-kanban__card--drop-before"));
    }

    [Fact]
    public void Mouse_ADragEnterWithoutADrag_IsIgnored()
    {
        var board = RenderBoard();

        board.Find("[data-omni-kanban-column='done'] ul").DragEnter();
        board.Find("[data-omni-kanban-card='2']").DragEnter();
        board.Find("[data-omni-kanban-column='done'] ul").Drop();

        Assert.Empty(board.FindAll(".omni-kanban__column--drop"));
        Assert.Empty(_moves);
    }

    [Fact]
    public async Task Script_IsAttachedOnce_WithTheBridge_AndDetachedOnDispose_ReleasingTheBridge()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var board = RenderBoard();

        var attach = Assert.Single(module.Invocations["attach"]);
        var bridge = Assert.IsType<DotNetObjectReference<KanbanInteropBridge>>(attach.Arguments[1]);
        Assert.NotNull(bridge.Value);
        board.Render(parameters => parameters.Add(component => component.Label, "Suivi"));
        Assert.Single(module.Invocations["attach"]);

        await DisposeComponentsAsync();

        Assert.Single(module.Invocations["detach"]);
        Assert.Throws<ObjectDisposedException>(() => bridge.Value);
    }

    [Fact]
    public async Task Bridge_ForwardsTheCardKeysToTheBoard()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var board = RenderBoard();
        var bridge = (DotNetObjectReference<KanbanInteropBridge>)module.Invocations["attach"][0].Arguments[1]!;

        await board.InvokeAsync(() => bridge.Value.OnCardKey("0", " "));

        Assert.True(board.Instance.IsGrabbing);
    }

    [Fact]
    public async Task ItemLabel_NamesTheCardInTheAnnouncements()
    {
        JSInterop.SetupModule(ModulePath);
        var board = RenderBoard(parameters => parameters.Add(component => component.ItemLabel, card => $"Dossier {card.Name}"));

        await KeyAsync(board, 0, " ");

        Assert.Equal("Dossier A saisie, colonne À faire, position 1 sur 2.", board.Instance.Announcement);
    }

    [Fact]
    public async Task ItemsRefreshedWithoutTheHeldCard_EndTheMove()
    {
        JSInterop.SetupModule(ModulePath);
        var board = RenderBoard();
        await KeyAsync(board, 2, " ");

        board.Render(parameters => parameters.Add(component => component.Items, [Cards[0]]));

        Assert.False(board.Instance.IsGrabbing);
        Assert.Equal(["A"], CardsOf(board, "todo"));
    }

    private IRenderedComponent<OmniKanban<Card>> RenderBoard(Action<ComponentParameterCollectionBuilder<OmniKanban<Card>>>? configure = null) =>
        Render<OmniKanban<Card>>(parameters =>
        {
            parameters
                .Add(component => component.Columns, Columns)
                .Add(component => component.Items, Cards)
                .Add(component => component.ColumnOf, card => card.Column)
                .Add(component => component.KeyOf, card => card.Name)
                .Add(component => component.CardTemplate, card => builder => builder.AddContent(0, card.Name))
                .Add(component => component.OnItemMoved, EventCallback.Factory.Create<OmniKanbanMove<Card>>(this, move => _moves.Add(move)));
            configure?.Invoke(parameters);
        });

    private static Task KeyAsync(IRenderedComponent<OmniKanban<Card>> board, int card, string key) =>
        board.InvokeAsync(() => board.Instance.HandleCardKeyAsync(card.ToString(System.Globalization.CultureInfo.InvariantCulture), key));

    private static IEnumerable<string> CardsOf(IRenderedComponent<OmniKanban<Card>> board, string column) =>
        board.FindAll($"[data-omni-kanban-column='{column}'] [data-omni-kanban-card]").Select(card => card.TextContent).ToList();

    public sealed record Card(string Name, string Column)
    {
        public override string ToString() => Name;
    }
}
