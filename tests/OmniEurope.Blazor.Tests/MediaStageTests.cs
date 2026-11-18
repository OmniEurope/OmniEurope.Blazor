using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniMediaStage{TItem}"/> in bUnit: which item is the main one, how a click or the keyboard changes it, and the
/// markup guarantee the stage exists for (every item a direct child of one element, under a stable key, so the browser
/// keeps the elements, and what plays in them, when the main item changes). The placement itself is the style sheet's and is
/// proved in a real browser by the <c>mediastage</c> part of the scripts probe.
/// </summary>
public sealed class MediaStageTests : OmniBunitContext
{
    private static readonly string[] Names = ["Ada", "Grace", "Linus", "Margaret"];

    private readonly List<string> _raised = [];

    // Each item is a span, the stand-in for a video; the "Mute" button is the interactive content a real tile has.
    private static RenderFragment<string> Template { get; } = item => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "probe");
        builder.AddContent(2, item);
        builder.OpenElement(3, "button");
        builder.AddAttribute(4, "type", "button");
        builder.AddAttribute(5, "class", "probe-mute");
        builder.AddContent(6, "Mute");
        builder.CloseElement();
        builder.CloseElement();
    };

    private IRenderedComponent<OmniMediaStage<string>> RenderStage(IReadOnlyList<string>? items = null, Action<ComponentParameterCollectionBuilder<OmniMediaStage<string>>>? configure = null, RenderFragment<string>? template = null)
        => Render<OmniMediaStage<string>>(parameters =>
        {
            parameters.Add(stage => stage.Items, items ?? Names);
            parameters.Add(stage => stage.ItemTemplate, template ?? Template);
            parameters.Add(stage => stage.MainItemChanged, EventCallback.Factory.Create<string>(this, item => _raised.Add(item)));
            configure?.Invoke(parameters);
        });

    private static IReadOnlyList<IElement> ItemElements(IRenderedComponent<OmniMediaStage<string>> stage) => stage.FindAll(".omni-media-stage__item");

    private static string MainName(IRenderedComponent<OmniMediaStage<string>> stage) =>
        stage.Find(".omni-media-stage__item--main .probe").TextContent.Replace("Mute", string.Empty, StringComparison.Ordinal).Trim();

    private static IElement ItemOf(IRenderedComponent<OmniMediaStage<string>> stage, string name) =>
        ItemElements(stage).Single(element => element.QuerySelector(".probe")!.FirstChild!.TextContent == name);

    private static IElement SelectOf(IRenderedComponent<OmniMediaStage<string>> stage, string name) =>
        ItemOf(stage, name).QuerySelector("button.omni-media-stage__select")!;

    // ----- Which item is the main one --------------------------------------------------------------------

    [Fact]
    public void TheFirstItemIsTheMainOne_AndEveryOtherItemHasAButtonToMakeItLarge()
    {
        var stage = RenderStage();

        Assert.Equal(Names.Length, ItemElements(stage).Count);
        Assert.Equal("Ada", MainName(stage));
        Assert.Single(stage.FindAll(".omni-media-stage__item--main"));
        Assert.Equal(Names.Length - 1, stage.FindAll("button.omni-media-stage__select").Count);
        Assert.Empty(stage.Find(".omni-media-stage__item--main").QuerySelectorAll(".omni-media-stage__select"));
    }

    [Fact]
    public void TheItemsStayInTheirOwnOrder_Whatever_TheMainOne()
    {
        var stage = RenderStage(configure: parameters => parameters.Add(component => component.MainItem, "Linus"));

        Assert.Equal("Linus", MainName(stage));
        Assert.Equal(Names, ItemElements(stage).Select(element => element.QuerySelector(".probe")!.FirstChild!.TextContent).ToArray());
    }

    [Fact]
    public void MainItem_NamesTheMainItem_AndFollowsTheParentWhenItChanges()
    {
        var stage = RenderStage(configure: parameters => parameters.Add(component => component.MainItem, "Grace"));
        Assert.Equal("Grace", MainName(stage));

        stage.Render(parameters => parameters.Add(component => component.MainItem, "Margaret"));
        Assert.Equal("Margaret", MainName(stage));
        Assert.Empty(_raised);
    }

    [Fact]
    public void AMainItemThatIsNotInTheItems_LeavesTheFirstOne()
    {
        var stage = RenderStage(configure: parameters => parameters.Add(component => component.MainItem, "Nobody"));

        Assert.Equal("Ada", MainName(stage));
        Assert.Single(stage.FindAll(".omni-media-stage__item--main"));
    }

    [Fact]
    public void WithAKeyOf_TheMainItemIsFoundByItsKey_NotByItsReference()
    {
        var items = new[] { new Person(1, "Ada"), new Person(2, "Grace"), new Person(3, "Linus") };
        var stage = Render<OmniMediaStage<Person>>(parameters => parameters
            .Add(component => component.Items, items)
            .Add(component => component.KeyOf, person => person.Id)
            .Add(component => component.MainItem, new Person(3, "A copy of Linus"))
            .Add(component => component.ItemTemplate, person => builder => builder.AddContent(0, person.Name)));

        Assert.Equal("Linus", stage.Find(".omni-media-stage__item--main").TextContent);
    }

    // ----- Changing the main item ------------------------------------------------------------------------

    [Fact]
    public async Task ClickingAThumbnailButton_MakesItTheMainItem_AndRaisesMainItemChanged()
    {
        var stage = RenderStage();

        await stage.InvokeAsync(() => SelectOf(stage, "Linus").Click());

        Assert.Equal("Linus", MainName(stage));
        Assert.Equal(["Linus"], _raised);
        Assert.Empty(ItemOf(stage, "Linus").QuerySelectorAll(".omni-media-stage__select"));
        Assert.Single(ItemOf(stage, "Ada").QuerySelectorAll(".omni-media-stage__select"));
        Assert.Equal(Names.Length - 1, stage.FindAll("button.omni-media-stage__select").Count);
    }

    [Fact]
    public async Task TheChange_IsAnnouncedPolitely_WithTheNameOfTheItem()
    {
        var stage = RenderStage(configure: parameters => parameters.Add(component => component.ItemLabel, name => $"{name} Lovelace"));

        var live = stage.Find("[role=status]");
        Assert.Equal("polite", live.GetAttribute("aria-live"));
        Assert.Equal(string.Empty, live.TextContent.Trim());

        await stage.InvokeAsync(() => SelectOf(stage, "Grace").Click());

        Assert.Equal("Grace Lovelace est affiché en grand", stage.Find("[role=status]").TextContent.Trim(' ', ' '));
    }

    [Fact]
    public async Task APickedMainItem_IsKept_WhenTheParentRendersAgainWithTheSameMainItem()
    {
        var stage = RenderStage(configure: parameters => parameters.Add(component => component.MainItem, "Ada"));
        await stage.InvokeAsync(() => SelectOf(stage, "Margaret").Click());

        stage.Render(parameters => parameters.Add(component => component.MainItem, "Ada").Add(component => component.Label, "Call"));

        Assert.Equal("Margaret", MainName(stage));
    }

    [Fact]
    public async Task TheMainItemBinds_InBothDirections()
    {
        string? main = "Grace";
        var stage = Render<OmniMediaStage<string>>(parameters => parameters
            .Add(component => component.Items, Names)
            .Add(component => component.ItemTemplate, Template)
            .Bind(component => component.MainItem, main, value => main = value));

        Assert.Equal("Grace", MainName(stage));
        await stage.InvokeAsync(() => SelectOf(stage, "Ada").Click());

        Assert.Equal("Ada", main);
        Assert.Equal("Ada", MainName(stage));
    }

    // ----- Keyboard ---------------------------------------------------------------------------------------

    [Fact]
    public void EachThumbnailHasARealButton_Named_InTheTabOrder_AndNeverNestedInAnotherControl()
    {
        var stage = RenderStage(configure: parameters => parameters.Add(component => component.ItemLabel, name => name));

        foreach (var name in Names.Skip(1))
        {
            var button = SelectOf(stage, name);
            Assert.Equal("button", button.GetAttribute("type"));
            Assert.Equal($"Afficher {name} en grand", button.GetAttribute("aria-label"));
            Assert.Null(button.GetAttribute("tabindex"));
            Assert.Null(button.GetAttribute("disabled"));
            Assert.Equal(0, button.QuerySelectorAll("button, a, input, select, textarea").Length);
            // A sibling of the content, which holds the interactive controls of the item: never one inside the other.
            Assert.Null(button.ParentElement!.QuerySelector(".omni-media-stage__content button.omni-media-stage__select"));
            Assert.Null(button.Closest(".omni-media-stage__content"));
        }

        // The controls of the content are all still there, one per item, outside the selecting buttons.
        Assert.Equal(Names.Length, stage.FindAll(".omni-media-stage__content button.probe-mute").Count);
    }

    [Fact]
    public async Task AfterAKeyboardChoice_TheFocusMovesToTheNewMainItem_WhoseButtonHasJustGone()
    {
        var stage = RenderStage();
        Assert.Equal("-1", ItemOf(stage, "Ada").GetAttribute("tabindex"));
        Assert.Null(ItemOf(stage, "Grace").GetAttribute("tabindex"));

        await stage.InvokeAsync(() => SelectOf(stage, "Grace").Click());

        JSInterop.VerifyFocusAsyncInvoke();
        Assert.Equal("-1", ItemOf(stage, "Grace").GetAttribute("tabindex"));
        Assert.Null(ItemOf(stage, "Ada").GetAttribute("tabindex"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    public async Task AChoiceOfTheMainItemOrOfAPositionTheListDoesNotHold_ChangesAndRaisesNothing(int index)
    {
        var stage = RenderStage();

        await stage.InvokeAsync(() => stage.Instance.SelectAsync(index));

        Assert.Equal("Ada", MainName(stage));
        Assert.Empty(_raised);
        Assert.Empty(stage.Find("[role=status]").TextContent);
        Assert.Empty(JSInterop.Invocations);
    }

    public static TheoryData<Exception> LostCircuitFailures() =>
    [
        new Microsoft.JSInterop.JSDisconnectedException("circuit perdu"),
        new TaskCanceledException("circuit perdu"),
    ];

    [Theory]
    [MemberData(nameof(LostCircuitFailures))]
    public async Task AFocusThatTheLostCircuitRefuses_DoesNotFailTheChoice(Exception failure)
    {
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetException(failure);
        var stage = RenderStage();

        await stage.InvokeAsync(() => SelectOf(stage, "Grace").Click());

        Assert.Equal("Grace", MainName(stage));
        Assert.Equal(["Grace"], _raised);
        Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
    }
    [Fact]
    public void WithoutAnItemLabel_TheButtonNamesTheItemByItsPosition()
    {
        var stage = RenderStage();

        Assert.Equal("Afficher Élément 3 en grand", SelectOf(stage, "Linus").GetAttribute("aria-label"));
    }

    // ----- Keys: the elements are kept ---------------------------------------------------------------------
    // bUnit rebuilds its own copy of the DOM at every render, so the identity of an element cannot be read from it. What is
    // observable is the component the item draws (a <video> host in a real tile): Blazor keeps it when it keeps the element
    // that carries it, and disposes it, then creates a new one, when it recreates that element.

    private static RenderFragment<string> ProbeTemplate(List<string> log) => item => builder =>
    {
        builder.OpenComponent<LifetimeProbe>(0);
        builder.AddAttribute(1, nameof(LifetimeProbe.Name), item);
        builder.AddAttribute(2, nameof(LifetimeProbe.Log), log);
        builder.CloseComponent();
    };

    [Fact]
    public async Task SwitchingTheMainItem_KeepsEveryItemsComponent_NoneIsDisposedOrCreatedAgain()
    {
        var log = new List<string>();
        var stage = RenderStage(template: ProbeTemplate(log));
        Assert.Equal(["init:Ada", "init:Grace", "init:Linus", "init:Margaret"], log);

        await stage.InvokeAsync(() => SelectOf(stage, "Margaret").Click());
        await stage.InvokeAsync(() => SelectOf(stage, "Ada").Click());
        await stage.InvokeAsync(() => SelectOf(stage, "Grace").Click());

        Assert.Equal("Grace", MainName(stage));
        Assert.Equal(["init:Ada", "init:Grace", "init:Linus", "init:Margaret"], log);
        // All the items are children of one element and stay in the order of the items: the role is a class, not a place.
        Assert.Equal(Names, ItemElements(stage).Select(element => element.QuerySelector(".probe")!.TextContent).ToArray());
        Assert.Single(ItemElements(stage).Select(element => element.ParentElement).Distinct());
    }

    [Fact]
    public void AnItemKeepsItsComponent_WhenTheHostReordersTheItems_AsLongAsItKeepsItsKey()
    {
        var log = new List<string>();
        var people = new[] { new Person(1, "Ada"), new Person(2, "Grace"), new Person(3, "Linus") };
        var stage = Render<OmniMediaStage<Person>>(parameters => parameters
            .Add(component => component.Items, people)
            .Add(component => component.KeyOf, person => person.Id)
            .Add(component => component.ItemTemplate, person => builder =>
            {
                builder.OpenComponent<LifetimeProbe>(0);
                builder.AddAttribute(1, nameof(LifetimeProbe.Name), person.Name);
                builder.AddAttribute(2, nameof(LifetimeProbe.Log), log);
                builder.CloseComponent();
            }));
        Assert.Equal(["init:Ada", "init:Grace", "init:Linus"], log);

        stage.Render(parameters => parameters
            .Add(component => component.Items, new[] { people[2], people[1], people[0] })
            .Add(component => component.KeyOf, person => person.Id));

        Assert.Equal(["init:Ada", "init:Grace", "init:Linus"], log);
        Assert.Equal(["Linus", "Grace", "Ada"], stage.FindAll(".omni-media-stage__item").Select(element => element.TextContent.Trim()).ToArray());
    }

    // ----- Few items ---------------------------------------------------------------------------------------

    [Fact]
    public void AloneItem_IsTheMainOne_WithNoThumbnailStrip_AndNoButton()
    {
        var stage = RenderStage(["Ada"]);

        Assert.Equal("Ada", MainName(stage));
        Assert.Contains("omni-media-stage--single", stage.Find(".omni-media-stage").ClassName, StringComparison.Ordinal);
        Assert.Empty(stage.FindAll(".omni-media-stage__select"));
        Assert.Single(ItemElements(stage));
    }

    [Fact]
    public void AStageWithoutItems_IsEmpty_ButNamed_AndRaisesNothing()
    {
        var stage = RenderStage([]);

        var root = stage.Find(".omni-media-stage");
        Assert.Contains("omni-media-stage--empty", root.ClassName, StringComparison.Ordinal);
        Assert.Equal("group", root.GetAttribute("role"));
        Assert.Equal("Scène média", root.GetAttribute("aria-label"));
        Assert.Empty(stage.FindAll(".omni-media-stage__item"));
        Assert.Empty(stage.FindAll("button"));
        Assert.Empty(_raised);
    }

    [Fact]
    public void ANullListOfItems_IsReadAsAnEmptyOne()
    {
        var stage = Render<OmniMediaStage<string>>(parameters => parameters
            .Add(component => component.Items, null!)
            .Add(component => component.ItemTemplate, Template));

        Assert.Empty(stage.FindAll(".omni-media-stage__item"));
    }

    [Fact]
    public void ARemovedMainItem_LeavesTheFirstOneAsTheMainOne()
    {
        var stage = RenderStage(configure: parameters => parameters.Add(component => component.MainItem, "Margaret"));

        stage.Render(parameters => parameters.Add(component => component.Items, ["Ada", "Grace"]).Add(component => component.MainItem, "Margaret"));

        Assert.Equal("Ada", MainName(stage));
    }

    // ----- Markup ------------------------------------------------------------------------------------------

    [Fact]
    public void Label_NamesTheStage_AndTheClassAndTheAdditionalAttributesLandOnTheRootElement()
    {
        var stage = RenderStage(configure: parameters => parameters
            .Add(component => component.Label, "Standup")
            .Add(component => component.Id, "call")
            .Add(component => component.Class, "mine")
            .AddUnmatched("data-probe", "yes"));

        var root = stage.Find("#call");
        Assert.Equal("Standup", root.GetAttribute("aria-label"));
        Assert.Contains("mine", root.ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-media-stage", root.ClassName, StringComparison.Ordinal);
        Assert.Equal("yes", root.GetAttribute("data-probe"));
    }

    [Fact]
    public void TheMarkup_HasNoInlineStyle()
    {
        var stage = RenderStage();

        Assert.DoesNotContain("style=", stage.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithoutAnItemTemplate_TheStageRefusesToRender()
    {
        Assert.Throws<ArgumentNullException>(() => Render<OmniMediaStage<string>>(parameters => parameters.Add(component => component.Items, Names)));
    }

    private sealed record Person(int Id, string Name);

    /// <summary>Stands for the component of a real tile that owns a video: it logs its creation and its disposal.</summary>
    private sealed class LifetimeProbe : ComponentBase, IDisposable
    {
        [Parameter]
        public string Name { get; set; } = string.Empty;

        private string _born = string.Empty;

        [Parameter]
        public List<string> Log { get; set; } = [];

        public void Dispose() => Log.Add($"dispose:{Name}");

        protected override void OnInitialized()
        {
            // Like a stream attached once to a video: what it shows is what it was created with, whatever it is given later.
            _born = Name;
            Log.Add($"init:{Name}");
        }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "probe");
            builder.AddContent(2, _born);
            builder.CloseElement();
        }
    }
}
