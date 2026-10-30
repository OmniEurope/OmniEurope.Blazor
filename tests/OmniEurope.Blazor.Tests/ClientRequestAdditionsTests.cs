using Bunit;
using OmniEurope.Blazor.Components;
using static OmniEurope.Blazor.Tests.ShippedLookTests;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The additions asked by a client application: <see cref="OmniSelectableCard.Tone"/>,
/// <see cref="OmniButton.Inline"/> and <see cref="OmniCheckBoxList{TValue}.Columns"/>. The markup is rendered;
/// the look is read from the stylesheet and measured in the browser probe.
/// </summary>
public sealed class ClientRequestAdditionsTests : OmniBunitContext
{
    [Fact]
    public void Card_WithoutTone_KeepsItsClasses()
    {
        var card = Render<OmniSelectableCard>(parameters => parameters.Add(component => component.Title, "Plain"));

        var classes = card.Find("button").ClassList;
        Assert.Contains("omni-selectable-card", classes);
        Assert.DoesNotContain(classes, name => name.StartsWith("omni-selectable-card--", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(OmniTone.Neutral, "neutral")]
    [InlineData(OmniTone.Accent, "accent")]
    [InlineData(OmniTone.Info, "info")]
    [InlineData(OmniTone.Success, "success")]
    [InlineData(OmniTone.Warning, "warning")]
    [InlineData(OmniTone.Danger, "danger")]
    public void Card_WithTone_RendersItsToneClass(OmniTone tone, string name)
    {
        var card = Render<OmniSelectableCard>(parameters => parameters
            .Add(component => component.Title, "Toned")
            .Add(component => component.Tone, tone));

        var classes = card.Find("button").ClassList;
        Assert.Contains("omni-selectable-card--toned", classes);
        Assert.Contains($"omni-selectable-card--{name}", classes);
        Assert.Null(card.Find("button").GetAttribute("style"));
    }

    [Fact]
    public void Card_Tone_KeepsTheChoiceAnnounced()
    {
        var card = Render<OmniSelectableCard>(parameters => parameters
            .Add(component => component.Title, "Toned")
            .Add(component => component.Tone, OmniTone.Success)
            .Add(component => component.Value, true));

        Assert.Equal("true", card.Find("button").GetAttribute("aria-checked"));
        Assert.Contains("omni-selectable-card--selected", card.Find("button").ClassList);
    }

    [Theory]
    [InlineData("neutral")]
    [InlineData("accent")]
    [InlineData("info")]
    [InlineData("success")]
    [InlineData("warning")]
    [InlineData("danger")]
    public void Card_EveryToneNamesItsColours(string tone)
    {
        var rule = Body($".omni-selectable-card--{tone}");

        foreach (var token in new[] { "tone", "tint", "fill", "ink" })
        {
            Assert.Contains($"--omni-selectable-card-{token}:", rule, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Button_Inline_RendersItsModifierAfterTheVariantAndStaysAButton()
    {
        var clicks = 0;
        var button = Render<OmniButton>(parameters => parameters
            .Add(component => component.Inline, true)
            .Add(component => component.OnClick, () => clicks++)
            .AddChildContent("word"));

        var element = button.Find("button");
        Assert.Contains("omni-button--inline", element.ClassList);
        Assert.Equal("button", element.GetAttribute("type"));
        element.Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Button_NotInline_HasNoInlineModifier()
    {
        var button = Render<OmniButton>(parameters => parameters.AddChildContent("word"));

        Assert.DoesNotContain("omni-button--inline", button.Find("button").ClassList);
    }

    [Fact]
    public void Button_InlineLook_HasNoBodyAndFollowsTheSentence()
    {
        var look = Body(".omni-button--inline");

        Assert.Contains("color: inherit", look, StringComparison.Ordinal);
        Assert.Contains("font: inherit", look, StringComparison.Ordinal);
        Assert.Contains("min-height: 0", look, StringComparison.Ordinal);
        Assert.Contains("padding: 0", look, StringComparison.Ordinal);
        Assert.Contains("underline dotted", look, StringComparison.Ordinal);
        Assert.Contains("display: inline", Body(".omni-button--inline > .omni-button__content"), StringComparison.Ordinal);
    }

    private readonly IReadOnlyList<string> _chosen = [];

    private IRenderedComponent<OmniCheckBoxList<string>> RenderList(int? columns) =>
        Render<OmniCheckBoxList<string>>(parameters => parameters
            .Add(component => component.Options, [new("a", "A"), new("b", "B")])
            .Add(component => component.ValueExpression, () => _chosen)
            .Add(component => component.Columns, columns));

    [Fact]
    public void List_WithoutColumns_KeepsItsMarkup()
    {
        var list = RenderList(null);

        Assert.DoesNotContain(list.Find("fieldset").ClassList, name => name.StartsWith("omni-choice-list--columns", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void List_WithColumns_HasAStyleForTheNumber(int columns)
    {
        var classes = RenderList(columns).Find("fieldset").ClassList;

        Assert.Contains("omni-choice-list--columns", classes);
        Assert.Contains($"omni-choice-list--columns-{columns}", classes);
        Assert.Contains($"columns: 5rem {columns}", Body($".omni-choice-list--columns-{columns}"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void List_RefusesAColumnCountTheStylesheetDoesNotDraw(int columns) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderList(columns));
}

/// <summary><see cref="OmniPassTracker"/>: where each element of a series stands among its passes.</summary>
public sealed class PassTrackerTests : OmniBunitContext
{
    private static readonly OmniPassTrackerItem[] Words =
    [
        new("Word 1", 3, 2, InProgress: true),
        new("Word 2", 3, 1),
        new("Word 3", 3)
    ];

    private IRenderedComponent<OmniPassTracker> RenderWords(int? current = null) =>
        Render<OmniPassTracker>(parameters => parameters
            .Add(component => component.Items, Words)
            .Add(component => component.Label, "Lesson")
            .Add(component => component.CurrentIndex, current));

    [Fact]
    public void Marks_ShowTheFinishedPassesAndThePassUnderWay()
    {
        var tracker = RenderWords();

        var items = tracker.FindAll(".omni-pass-tracker__item");
        Assert.Equal(3, items.Count);
        Assert.Equal(3, items[0].QuerySelectorAll(".omni-pass-tracker__mark").Length);
        Assert.Equal(2, items[0].QuerySelectorAll(".omni-pass-tracker__mark--done").Length);
        Assert.Single(items[0].QuerySelectorAll(".omni-pass-tracker__mark--active"));
        Assert.Single(items[1].QuerySelectorAll(".omni-pass-tracker__mark--done"));
        Assert.Empty(items[1].QuerySelectorAll(".omni-pass-tracker__mark--active"));
        Assert.Empty(items[2].QuerySelectorAll(".omni-pass-tracker__mark--done"));
    }

    [Fact]
    public void ThePassUnderWay_FollowsTheFinishedOnes()
    {
        var marks = RenderWords().FindAll(".omni-pass-tracker__item")[0].QuerySelectorAll(".omni-pass-tracker__mark");

        Assert.Contains("omni-pass-tracker__mark--active", marks[2].ClassList);
    }

    [Fact]
    public void List_IsNamedAndOrdered()
    {
        var list = RenderWords().Find("ol");

        Assert.Equal("Lesson", list.GetAttribute("aria-label"));
        Assert.Contains("omni-pass-tracker", list.ClassList);
    }

    [Fact]
    public void CurrentElement_IsMarkedForEveryone()
    {
        var items = RenderWords(1).FindAll(".omni-pass-tracker__item");

        Assert.Equal("step", items[1].GetAttribute("aria-current"));
        Assert.Contains("omni-pass-tracker__item--current", items[1].ClassList);
        Assert.Null(items[0].GetAttribute("aria-current"));
    }

    [Fact]
    public void ScreenReaders_HearOneSentencePerElementInsteadOfTheMarks()
    {
        var item = RenderWords().FindAll(".omni-pass-tracker__item")[0];

        Assert.Equal("true", item.QuerySelector(".omni-pass-tracker__marks")!.GetAttribute("aria-hidden"));
        Assert.Equal("true", item.QuerySelector(".omni-pass-tracker__text")!.GetAttribute("aria-hidden"));
        var sentence = item.QuerySelector(".omni-visually-hidden")!.TextContent;
        Assert.Contains("Word 1", sentence, StringComparison.Ordinal);
        Assert.Contains("2", sentence, StringComparison.Ordinal);
        Assert.Contains("3", sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void AnElementUnderWay_SaysSoInItsSentence()
    {
        var items = RenderWords().FindAll(".omni-pass-tracker__item");

        Assert.NotEqual(
            items[0].QuerySelector(".omni-visually-hidden")!.TextContent.Replace("Word 1", string.Empty, StringComparison.Ordinal).Length,
            items[1].QuerySelector(".omni-visually-hidden")!.TextContent.Replace("Word 2", string.Empty, StringComparison.Ordinal).Length);
    }

    [Fact]
    public void NoInlineStyleIsEmitted() => Assert.DoesNotContain("style=", RenderWords(0).Markup, StringComparison.Ordinal);

    public static TheoryData<OmniPassTrackerItem> Refused => new()
    {
        new OmniPassTrackerItem(" ", 3),
        new OmniPassTrackerItem("No pass", 0),
        new OmniPassTrackerItem("Negative", 3, -1),
        new OmniPassTrackerItem("Too many", 3, 4),
        new OmniPassTrackerItem("Finished yet under way", 3, 3, true)
    };

    [Theory]
    [MemberData(nameof(Refused))]
    public void AnImpossibleElement_IsRefused(OmniPassTrackerItem item) =>
        Assert.ThrowsAny<ArgumentException>(() => Render<OmniPassTracker>(parameters => parameters.Add(component => component.Items, [item])));

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void ACurrentElementOutOfTheList_IsRefused(int current) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderWords(current));
}
