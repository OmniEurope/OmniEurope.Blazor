using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// PLAN-007 lot 7: one tone and one fill vocabulary, the behaviour fixes of the feedback and form
/// families, Class on the outermost element, and the selectable card group.
/// </summary>
public sealed class FeedbackFormsCoherenceTests : OmniBunitContext
{
    // ---- progress bar ---------------------------------------------------------------------------

    [Fact]
    public void IndeterminateProgress_WithShowValue_WritesInProgress_NeverAFakePercentage()
    {
        var progress = Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Indeterminate, true)
            .Add(component => component.ShowValue, true));

        var label = progress.Find(".omni-progress__label").TextContent;
        Assert.Equal("En cours", label);
        Assert.DoesNotContain("%", label, StringComparison.Ordinal);
        var root = progress.Find("[role=progressbar]");
        Assert.False(root.HasAttribute("aria-valuenow"));
        Assert.False(root.HasAttribute("aria-valuetext"));
    }

    [Fact]
    public void DeterminateProgress_StillWritesItsPercentage_AndValueTextWins()
    {
        var progress = Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Value, 40)
            .Add(component => component.ShowValue, true));
        Assert.Equal("40 %", progress.Find(".omni-progress__label").TextContent);

        var named = Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Indeterminate, true)
            .Add(component => component.ValueText, "3 fichiers sur 12")
            .Add(component => component.ShowValue, true));
        Assert.Equal("3 fichiers sur 12", named.Find(".omni-progress__label").TextContent);
    }

    [Theory]
    [MemberData(nameof(Tones))]
    public void Progress_EveryTone_HasItsModifier_AndAccentIsTheDefault(OmniTone tone)
    {
        var progress = Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Value, 10)
            .Add(component => component.Tone, tone));

        Assert.Contains($"omni-progress--{tone.ToString().ToLowerInvariant()}", progress.Find(".omni-progress").ClassList);
        Assert.Contains("omni-progress--accent", Render<OmniProgressBar>().Find(".omni-progress").ClassList);
        if (tone != OmniTone.Accent)
        {
            // Accent is the colour of the bar itself; every other tone has its own rule.
            Assert.Contains("color:", ShippedLookTests.Body($".omni-progress--{tone.ToString().ToLowerInvariant()}"), StringComparison.Ordinal);
        }
    }

    public static TheoryData<OmniTone> Tones() => [.. Enum.GetValues<OmniTone>()];

    // ---- badge ----------------------------------------------------------------------------------

    [Theory]
    [InlineData(OmniFill.Tonal, "omni-badge--tonal")]
    [InlineData(OmniFill.Outline, "omni-badge--outline")]
    [InlineData(OmniFill.Solid, "omni-badge--solid")]
    public void Badge_EveryFill_HasItsModifier(OmniFill fill, string expected)
    {
        var badge = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Fill, fill)
            .AddChildContent("Nouveau"));

        Assert.Contains(expected, badge.Find(".omni-badge").ClassList);
    }

    [Fact]
    public void Badge_DefaultsToASolidNeutralBadge_AndDrawsItsTextOnce()
    {
        var badge = Render<OmniBadge>(parameters => parameters.AddChildContent("Nouveau"));

        var root = badge.Find(".omni-badge");
        Assert.Contains("omni-badge--neutral", root.ClassList);
        Assert.Contains("omni-badge--solid", root.ClassList);
        Assert.Equal("Nouveau", root.TextContent.Trim());
    }

    // ---- form field -----------------------------------------------------------------------------

    [Fact]
    public void FormField_TakesItsLabelAsText_OrAsMarkup_WhichWins()
    {
        var text = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "nom")
            .Add(component => component.Label, "Nom")
            .AddChildContent("<input id=\"nom\" />"));
        Assert.Equal("Nom", text.Find("label.omni-label").TextContent.Trim());

        var markup = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "nom")
            .Add(component => component.Label, "Ignoré")
            .Add(component => component.LabelContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<em>Nom</em>")))
            .Add(component => component.EndContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"unit\">kg</span>")))
            .AddChildContent("<input id=\"nom\" />"));
        Assert.Equal("Nom", markup.Find("label.omni-label em").TextContent);
        Assert.DoesNotContain("Ignoré", markup.Markup, StringComparison.Ordinal);
        Assert.NotNull(markup.Find(".omni-form-field__control > .unit"));
    }

    [Fact]
    public void FormField_Error_IsAnnouncedPolitely_FromARegionThatExistsBeforeIt()
    {
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.Id, "poids")
            .Add(component => component.For, "poids-input")
            .Add(component => component.Label, "Poids")
            .AddChildContent("<input id=\"poids-input\" />"));

        var region = field.Find(".omni-form-field > .omni-form-field__message");
        Assert.Equal("polite", region.GetAttribute("aria-live"));
        Assert.Empty(region.Children);

        field.Render(parameters => parameters.Add(component => component.Error, "Trop lourd."));

        region = field.Find(".omni-form-field > .omni-form-field__message");
        var error = region.QuerySelector("#poids-error.omni-form-field__error")!;
        Assert.False(error.HasAttribute("role"));
        Assert.Empty(field.FindAll("[role=alert]"));
        Assert.Equal("Trop lourd.", error.TextContent.Trim());
        Assert.Contains("omni-form-field--invalid", field.Find(".omni-form-field").ClassList);
        // Empty, the region takes no room in the field's grid.
        Assert.Equal("absolute", ShippedLookTests.Value(ShippedLookTests.Body(".omni-form-field__message:empty, .omni-validation-message:empty"), "position"));
    }

    // ---- Class on the outermost element ---------------------------------------------------------

    [Fact]
    public void WrappedControls_PutClassOnTheirWrapper_AndIdWithTheAttributesOnTheControl()
    {
        var value = "x";
        var password = Render<OmniPassword>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Id, "secret")
            .Add(component => component.Class, "hote")
            .AddUnmatched("data-probe", "1"));
        Assert.Contains("hote", password.Find(".omni-password").ClassList);
        Assert.DoesNotContain("hote", password.Find("input").ClassList);
        Assert.Equal("secret", password.Find("input").Id);
        Assert.Equal("1", password.Find("input").GetAttribute("data-probe"));

        var area = Render<OmniTextArea>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Id, "note")
            .Add(component => component.Class, "hote"));
        Assert.Contains("hote", area.Find(".omni-text-area").ClassList);
        Assert.DoesNotContain("hote", area.Find("textarea").ClassList);
        Assert.Equal("note", area.Find("textarea").Id);

        var withIcon = Render<OmniTextBox>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Class, "hote")
            .Add(component => component.Icon, (RenderFragment)(builder => builder.AddMarkupContent(0, "<i></i>"))));
        Assert.Contains("hote", withIcon.Find(".omni-text-box-field").ClassList);
        Assert.DoesNotContain("hote", withIcon.Find("input").ClassList);

        var alone = Render<OmniTextBox>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Class, "hote"));
        Assert.Contains("hote", alone.Find("input").ClassList);
    }

    [Fact]
    public void TextAreaCount_IsWrittenThroughTheResources()
    {
        var value = "abc";
        var area = Render<OmniTextArea>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.MaxLength, 10)
            .Add(component => component.ShowCount, true));

        Assert.Equal("3 / 10", area.Find(".omni-text-area__count").TextContent);
    }

    [Fact]
    public void Numeric_WritesItsDoubleBoundsInTheInvariantCulture()
    {
        decimal value = 1;
        var numeric = Render<OmniNumeric<decimal>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Minimum, 0.5)
            .Add(component => component.Maximum, 1250.75)
            .Add(component => component.Step, 0.25));

        var input = numeric.Find("input");
        Assert.Equal(("0.5", "1250.75", "0.25"), (input.GetAttribute("min"), input.GetAttribute("max"), input.GetAttribute("step")));
    }

    // ---- header ---------------------------------------------------------------------------------

    [Fact]
    public void HeaderLogoWithoutAName_IsNeverANamelessLink()
    {
        var link = Render<OmniHeader>(parameters => parameters
            .Add(component => component.BrandLogo, "logo.svg")
            .Add(component => component.BrandHref, "/")
            .AddChildContent("actions"));
        var linkLogo = link.Find("a.omni-header__brand img");
        Assert.Equal("Accueil", linkLogo.GetAttribute("alt"));
        Assert.False(linkLogo.HasAttribute("aria-hidden"));

        var label = Render<OmniHeader>(parameters => parameters
            .Add(component => component.BrandLogo, "logo.svg")
            .AddChildContent("actions"));
        Assert.Equal("Logo", label.Find("span.omni-header__brand img").GetAttribute("alt"));
    }

    // ---- selectable card group ------------------------------------------------------------------

    private static readonly IReadOnlyList<OmniOption<string>> Systems =
    [
        new("linux", "Linux"),
        new("mac", "macOS", Disabled: true),
        new("windows", "Windows"),
        new("bsd", "BSD")
    ];

    [Fact]
    public void CardGroup_IsANamedRadioGroup_WithOneTabStopThatFollowsTheChoice()
    {
        var value = "windows";
        var group = Render<OmniSelectableCardGroup<string, string>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.Label, "Système")
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => value = next)
            .Add(component => component.ValueExpression, () => value));

        var root = group.Find(".omni-selectable-card-group");
        Assert.Equal("radiogroup", root.GetAttribute("role"));
        Assert.Equal("Système", root.GetAttribute("aria-label"));
        var cards = group.FindAll("button.omni-selectable-card");
        Assert.Equal(["radio", "radio", "radio", "radio"], cards.Select(card => card.GetAttribute("role")));
        Assert.Equal(["false", "false", "true", "false"], cards.Select(card => card.GetAttribute("aria-checked")));
        Assert.Equal(["-1", "-1", "0", "-1"], cards.Select(card => card.GetAttribute("tabindex")));
        Assert.Equal("true", cards[1].GetAttribute("aria-disabled"));

        cards[0].Click();
        Assert.Equal("linux", value);
    }

    [Fact]
    public void CardGroup_ArrowsMoveTheChoiceAmongTheAvailableCards()
    {
        var value = "linux";
        var group = Render<OmniSelectableCardGroup<string, string>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => value = next)
            .Add(component => component.ValueExpression, () => value));

        // macOS is disabled: the arrow skips it.
        group.FindAll("button.omni-selectable-card")[0].KeyDown("ArrowDown");
        Assert.Equal("windows", value);
        group.Render(parameters => parameters.Add(component => component.Value, value));
        Assert.Equal("0", group.FindAll("button.omni-selectable-card")[2].GetAttribute("tabindex"));

        group.FindAll("button.omni-selectable-card")[2].KeyDown("ArrowUp");
        Assert.Equal("linux", value);
        group.FindAll("button.omni-selectable-card")[0].KeyDown("ArrowLeft");
        Assert.Equal("bsd", value);
        group.FindAll("button.omni-selectable-card")[3].KeyDown("Home");
        Assert.Equal("linux", value);
        group.FindAll("button.omni-selectable-card")[0].KeyDown("End");
        Assert.Equal("bsd", value);
    }

    [Fact]
    public void CardGroup_WithoutAChoice_PutsTheFirstAvailableCardInTheTabOrder()
    {
        string? value = null;
        var group = Render<OmniSelectableCardGroup<string, string?>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("mac", "macOS", Disabled: true), new OmniOption<string>("linux", "Linux")])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.Equal(["-1", "0"], group.FindAll("button.omni-selectable-card").Select(card => card.GetAttribute("tabindex")));
    }

    [Fact]
    public void CardGroup_Multiple_IsAGroupOfCheckBoxesBindingTheChosenValuesInCardOrder()
    {
        IReadOnlyList<string> value = ["bsd"];
        var group = Render<OmniSelectableCardGroup<string, IReadOnlyList<string>>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.Multiple, true)
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => value = next)
            .Add(component => component.ValueExpression, () => value));

        Assert.Equal("group", group.Find(".omni-selectable-card-group").GetAttribute("role"));
        var cards = group.FindAll("button.omni-selectable-card");
        Assert.All(cards, card => Assert.Equal("checkbox", card.GetAttribute("role")));
        Assert.All(cards, card => Assert.False(card.HasAttribute("tabindex")));

        cards[0].Click();
        Assert.Equal(["linux", "bsd"], value);
        group.Render(parameters => parameters.Add(component => component.Value, value));
        group.FindAll("button.omni-selectable-card")[3].Click();
        Assert.Equal(["linux"], value);
        group.FindAll("button.omni-selectable-card")[1].Click();
        Assert.Equal(["linux"], value);
    }

    [Fact]
    public void CardGroup_TakesCardsWrittenInside_EachStandingForItsChoice()
    {
        var value = "b";
        var group = Render<OmniSelectableCardGroup<string, string>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => value = next)
            .Add(component => component.ValueExpression, () => value)
            .AddChildContent<OmniSelectableCard>(card => card.Add(c => c.Title, "A").Add(c => c.Choice, "a"))
            .AddChildContent<OmniSelectableCard>(card => card.Add(c => c.Title, "B").Add(c => c.Choice, "b")));

        var cards = group.FindAll("button.omni-selectable-card");
        Assert.Equal(["false", "true"], cards.Select(card => card.GetAttribute("aria-checked")));

        cards[0].Click();
        Assert.Equal("a", value);
        // The cards redraw on their own: their parameters did not change, the group's value did.
        Assert.Equal(["true", "false"], group.FindAll("button.omni-selectable-card").Select(card => card.GetAttribute("aria-checked")));
    }

    [Fact]
    public void CardGroup_RefusesACardOfAnotherTypeAndABindingThatDoesNotMatchMultiple()
    {
        var value = "a";
        Assert.Throws<InvalidOperationException>(() => Render<OmniSelectableCardGroup<string, string>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .AddChildContent<OmniSelectableCard>(card => card.Add(c => c.Title, "Un").Add(c => c.Choice, 1))));

        Assert.Throws<InvalidOperationException>(() => Render<OmniSelectableCardGroup<string, string>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.Multiple, true)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)));
    }

    [Fact]
    public void Card_Alone_BindsABoolean()
    {
        var picked = new List<bool>();
        var card = Render<OmniSelectableCard>(parameters => parameters
            .Add(component => component.Title, "Docker")
            .Add(component => component.Multiple, true)
            .Add(component => component.Value, false)
            .Add(component => component.ValueChanged, value => picked.Add(value)));

        Assert.False(card.Find("button").HasAttribute("tabindex"));
        card.Find("button").Click();
        Assert.Equal([true], picked);
    }
}
