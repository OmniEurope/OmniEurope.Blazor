using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniSelectableCardGroup at its edges: keys that move nothing, a group whose cards are all disabled, the
/// arrows from a disabled card, a card standing for null, and cards that leave the group.
/// </summary>
public sealed class SelectableCardGroupTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<string>> Systems =
    [
        new("linux", "Linux"),
        new("mac", "macOS", Disabled: true),
        new("windows", "Windows"),
    ];

    private IRenderedComponent<OmniSelectableCardGroup<string, string?>> RenderRadio(List<string?> changes, string? value = "windows", bool disabled = false) =>
        Render<OmniSelectableCardGroup<string, string?>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.Disabled, disabled)
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => changes.Add(next))
            .Add(component => component.ValueExpression, () => _bound));

    private readonly string? _bound = null;

    private static IReadOnlyList<AngleSharp.Dom.IElement> Cards<T>(IRenderedComponent<T> group) where T : Microsoft.AspNetCore.Components.IComponent =>
        group.FindAll("button.omni-selectable-card");

    [Fact]
    public void OtherKeys_MoveNothing()
    {
        var changes = new List<string?>();
        var group = RenderRadio(changes);

        Cards(group)[2].KeyDown("Tab");
        Cards(group)[2].KeyDown("a");

        Assert.Empty(changes);
    }

    [Fact]
    public void GroupWithEveryCardDisabled_MovesNothing_AndPicksNothing()
    {
        var changes = new List<string?>();
        var group = RenderRadio(changes, disabled: true);

        Cards(group)[0].KeyDown("ArrowDown");
        Cards(group)[0].Click();

        Assert.Empty(changes);
        // The first card stays the tab stop, so the group can still be reached and read out.
        Assert.Equal("0", Cards(group)[0].GetAttribute("tabindex"));
    }

    [Theory]
    [InlineData("ArrowRight", "linux")]
    [InlineData("ArrowLeft", "windows")]
    public void ArrowFromADisabledCard_StartsFromTheEnds(string key, string expected)
    {
        var changes = new List<string?>();
        var group = RenderRadio(changes, value: null);

        Cards(group)[1].KeyDown(key);

        Assert.Equal([expected], changes);
    }

    [Fact]
    public void CardStandingForNull_IsChosenByANullValue_AndPicksNull()
    {
        var changes = new List<string?>();
        var group = Render<OmniSelectableCardGroup<string?, string?>>(parameters => parameters
            .Add(component => component.Value, null)
            .Add(component => component.ValueChanged, next => changes.Add(next))
            .Add(component => component.ValueExpression, () => _bound)
            .AddChildContent<OmniSelectableCard>(card => card.Add(c => c.Title, "Aucun").Add(c => c.Choice, null))
            .AddChildContent<OmniSelectableCard>(card => card.Add(c => c.Title, "Un").Add(c => c.Choice, "un")));

        Assert.Equal(["true", "false"], Cards(group).Select(card => card.GetAttribute("aria-checked")));

        Cards(group)[1].Click();
        Cards(group)[0].Click();
        Assert.Equal(["un", null], changes);
    }

    [Fact]
    public void MultipleGroupWithoutAValue_ChoosesNothing()
    {
        IReadOnlyList<string>? value = null;
        var group = Render<OmniSelectableCardGroup<string, IReadOnlyList<string>?>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.Multiple, true)
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => value = next)
            .Add(component => component.ValueExpression, () => value));

        Assert.All(Cards(group), card => Assert.Equal("false", card.GetAttribute("aria-checked")));
        Cards(group)[2].Click();
        Assert.Equal(["windows"], value);
    }

    [Fact]
    public void CardsRemovedFromTheGroup_LeaveIt()
    {
        var changes = new List<string?>();
        var group = RenderRadio(changes, value: "linux");

        group.Render(parameters => parameters.Add(component => component.Options, [Systems[2]]));
        Cards(group)[0].KeyDown("ArrowDown");

        // Only Windows is left: the arrow cannot reach a card that went.
        Assert.Equal(["windows"], changes);
    }

    [Fact]
    public void TextParsing_IsRefused()
    {
        var group = Render<ParsingGroup>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.ValueExpression, () => _bound));

        Assert.False(group.Instance.Parse("linux", out var result, out var message));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(message));
    }

    /// <summary>Opens the text parsing of the group, which none of its cards goes through.</summary>
    public sealed class ParsingGroup : OmniSelectableCardGroup<string, string?>
    {
        public bool Parse(string? text, out string? result, out string message) => TryParseValueFromString(text, out result, out message);
    }

    [Fact]
    public void CardWhoseChoiceTurnedToAnotherType_IsNeitherChosenNorPicked()
    {
        var changes = new List<string?>();
        object choice = "a";
        var group = Render<OmniSelectableCardGroup<string, string?>>(parameters => parameters
            .Add(component => component.Value, "a")
            .Add(component => component.ValueChanged, next => changes.Add(next))
            .Add(component => component.ValueExpression, () => _bound)
            .AddChildContent<OmniSelectableCard>(card => card.Add(c => c.Title, "A").Add(c => c.Choice, choice)));
        var card = group.FindComponent<OmniSelectableCard>();

        card.Render(parameters => parameters.Add(c => c.Choice, 1));
        Assert.Equal("false", Cards(group)[0].GetAttribute("aria-checked"));
        Cards(group)[0].Click();

        Assert.Empty(changes);
    }

    [Fact]
    public void NullChoice_InAGroupOfValues_IsRefusedWithItsName()
    {
        int? value = null;
        var error = Assert.Throws<InvalidOperationException>(() => Render<OmniSelectableCardGroup<int, int?>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .AddChildContent<OmniSelectableCard>(card => card.Add(c => c.Title, "Aucun").Add(c => c.Choice, null))));
        Assert.Contains("a null", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NullValue_OfAGroupOfValues_ChoosesNothing()
    {
        int? value = null;
        var group = Render<OmniSelectableCardGroup<int, int?>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<int>(1, "Un"), new OmniOption<int>(2, "Deux")])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.All(Cards(group), card => Assert.Equal("false", card.GetAttribute("aria-checked")));
    }

    [Fact]
    public void CollectionBoundWithoutMultiple_IsRefused()
    {
        IReadOnlyList<string> value = [];
        var error = Assert.Throws<InvalidOperationException>(() => Render<OmniSelectableCardGroup<string, IReadOnlyList<string>>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)));
        Assert.Contains("Multiple", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ArrowsOnACheckBoxCardOrACardAlone_MoveNothing_AndAnIconIsDrawnBesideTheTitle()
    {
        IReadOnlyList<string> value = [];
        var multiple = Render<OmniSelectableCardGroup<string, IReadOnlyList<string>>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.Multiple, true)
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => value = next)
            .Add(component => component.ValueExpression, () => value));
        Cards(multiple)[0].KeyDown("ArrowDown");
        Assert.Empty(value);

        var picked = new List<bool>();
        var alone = Render<OmniSelectableCard>(parameters => parameters
            .Add(c => c.Title, "Seule")
            .Add(c => c.ValueChanged, next => picked.Add(next))
            .Add(c => c.Icon, builder => builder.AddContent(0, "★")));
        alone.Find("button").KeyDown("ArrowDown");
        alone.Render(parameters => parameters.Add(c => c.Title, "Seule encore"));
        Assert.Empty(picked);
        Assert.Equal("★", alone.Find(".omni-selectable-card__icon").TextContent);
    }
}
