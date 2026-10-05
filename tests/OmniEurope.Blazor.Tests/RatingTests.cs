using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniRating"/> picks one of N as a radio group: native radio buttons give the checked
/// state, the single tab stop and the arrow keys; each star is announced "n sur N" in a group named by
/// <see cref="OmniRating.Label"/>, the localized "Note" by default.
/// </summary>
public sealed class RatingTests : OmniBunitContext
{
    [Fact]
    public void Selecting_a_star_fills_every_preceding_star_and_can_decrease_the_rating()
    {
        int? value = 1;
        var rating = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, selected => value = selected));
        rating.FindAll("input[type=radio]")[3].Change(true);
        Assert.Equal(4, value);
        Assert.True(rating.FindAll("input[type=radio]")[3].HasAttribute("checked"));
        var filled = OmniEurope.Blazor.Internal.PhosphorIconGlyphs.For(OmniIconName.StarFilled).PathData;
        Assert.Equal(4, rating.FindAll(".omni-rating__star path").Count(path => path.GetAttribute("d") == filled));
        rating.FindAll("input[type=radio]")[1].Change(true);
        Assert.Equal(2, value);
        Assert.Equal(5, rating.FindAll("input[type=radio]").Count);
    }

    [Fact]
    public void Editable_rating_is_a_named_radio_group_of_native_radios_sharing_one_name()
    {
        int? value = 3;
        var rating = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        var group = rating.Find(".omni-rating");
        Assert.Equal("radiogroup", group.GetAttribute("role"));
        Assert.Equal("Note", group.GetAttribute("aria-label"));
        var radios = rating.FindAll("input[type=radio]");
        Assert.Single(radios.Select(radio => radio.GetAttribute("name")).Distinct());
        Assert.Equal(["1 sur 5", "2 sur 5", "3 sur 5", "4 sur 5", "5 sur 5"], radios.Select(radio => radio.GetAttribute("aria-label")));
        Assert.Equal([false, false, true, false, false], radios.Select(radio => radio.HasAttribute("checked")));
        // One tab stop and the arrows come from the native radios: no roving tabindex to keep in step.
        Assert.All(radios, radio => Assert.Null(radio.GetAttribute("tabindex")));
        Assert.Empty(rating.FindAll("button"));
        Assert.Empty(rating.FindAll("[aria-pressed]"));
    }

    [Fact]
    public void Label_names_the_group()
    {
        int? value = null;
        var rating = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Label, "Qualité"));

        Assert.Equal("Qualité", rating.Find("[role=radiogroup]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Disabled_wins_over_ReadOnly()
    {
        int? value = 3;
        var rating = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Disabled, true));
        Assert.Equal(5, rating.FindAll("input[type=radio]:disabled").Count);
        Assert.Contains("omni-rating--disabled", rating.Find(".omni-rating").ClassList);
    }

    [Fact]
    public void ReadOnly_rating_is_one_announced_value_outside_the_tab_order_that_cannot_change()
    {
        int? value = 3;
        var rating = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, selected => value = selected)
            .Add(component => component.Label, "Note")
            .Add(component => component.ReadOnly, true));

        var root = rating.Find(".omni-rating");
        Assert.Equal("img", root.GetAttribute("role"));
        Assert.Null(root.GetAttribute("tabindex"));
        Assert.Equal("Note : 3 sur 5", root.GetAttribute("aria-label"));
        Assert.Contains("omni-rating--readonly", root.ClassList);
        Assert.DoesNotContain("omni-rating--disabled", root.ClassList);
        Assert.Empty(rating.FindAll("input"));
        var filled = OmniEurope.Blazor.Internal.PhosphorIconGlyphs.For(OmniIconName.StarFilled).PathData;
        Assert.Equal(3, rating.FindAll(".omni-rating__star path").Count(path => path.GetAttribute("d") == filled));
        Assert.Equal(5, rating.FindAll(".omni-rating__star").Count);
    }

    [Fact]
    public void ReadOnly_rating_without_label_announces_the_localized_default()
    {
        int? value = null;
        var rating = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ReadOnly, true));

        Assert.Equal("Note : 0 sur 5", rating.Find(".omni-rating").GetAttribute("aria-label"));
    }

    [Fact]
    public void Disabled_rating_keeps_disabled_radios_out_of_the_tab_order()
    {
        int? value = 2;
        var rating = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Label, "Note")
            .Add(component => component.Disabled, true));

        var root = rating.Find(".omni-rating");
        Assert.Equal("radiogroup", root.GetAttribute("role"));
        Assert.Equal("true", root.GetAttribute("aria-disabled"));
        Assert.Null(root.GetAttribute("tabindex"));
        Assert.Contains("omni-rating--disabled", root.ClassList);
        Assert.Equal(5, rating.FindAll("input[type=radio]:disabled").Count);
    }

    [Fact]
    public void Change_OnADisabledOrReadOnlyRating_IsRefused()
    {
        int? value = 2;
        var disabled = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, selected => value = selected)
            .Add(component => component.Disabled, true));

        // A change event for a star of a disabled rating (a click raced with the switch to disabled).
        disabled.FindAll("input[type=radio]")[4].Change(true);
        Assert.Equal(2, value);

        // Disabled and read-only together draw the disabled radios: read-only refuses the change too.
        var both = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, selected => value = selected)
            .Add(component => component.Disabled, true)
            .Add(component => component.ReadOnly, true));
        both.FindAll("input[type=radio]")[4].Change(true);
        Assert.Equal(2, value);
    }

    [Theory]
    [InlineData("", true, null)]
    [InlineData(null, true, null)]
    [InlineData("0", true, 0)]
    [InlineData("5", true, 5)]
    [InlineData("6", false, 6)]
    [InlineData("-1", false, -1)]
    [InlineData("trois", false, null)]
    public void Text_IsARatingFromZeroToTheMaximum_OrEmpty(string? text, bool valid, int? expected)
    {
        var rating = Render<ParsingRating>(parameters => parameters
            .Add(component => component.Value, null)
            .Add(component => component.ValueExpression, () => _none));

        Assert.Equal(valid, rating.Instance.Parse(text, out var result, out var message));
        Assert.Equal(expected, result);
        Assert.Equal(valid ? string.Empty : "La valeur saisie n'est pas valide.", message);
    }

    private readonly int? _none = null;

    /// <summary>Opens the text parsing of the rating, which no markup of the rating reaches.</summary>
    public sealed class ParsingRating : OmniRating
    {
        public bool Parse(string? text, out int? result, out string message) => TryParseValueFromString(text, out result, out message);
    }
}
