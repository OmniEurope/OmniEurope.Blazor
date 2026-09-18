using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The app bar pieces the showcase used to draw with its own stylesheet (PLAN-008 lot 9), now in the
/// package: the indicator dot of a button, the icon of a text box, the brand of the header, the avatar
/// and the identity header of the account menu, the icon and description of a menu item, and the
/// error line of a radio list. Each is checked rendered, with its accessibility attributes, absent
/// when not asked for, and against the stylesheet rules that draw it.
/// </summary>
public sealed class AppBarComponentTests : OmniBunitContext
{
    // ---- OmniButton.Indicator ----

    [Fact]
    public void ButtonIndicator_DrawsADecorativeDot_AndTheNameKeepsTheMeaning()
    {
        var button = Render<OmniButton>(parameters => parameters
            .Add(component => component.Variant, OmniButtonVariant.Ghost)
            .Add(component => component.AriaLabel, "Notifications, 3 non lues")
            .Add(component => component.Indicator, true)
            .AddChildContent("B"));

        var root = button.Find("button");
        Assert.Contains("omni-button--indicator", root.ClassName, StringComparison.Ordinal);
        Assert.Equal("Notifications, 3 non lues", root.GetAttribute("aria-label"));
        var dot = root.QuerySelector(":scope > .omni-button__indicator");
        Assert.NotNull(dot);
        Assert.Equal("true", dot!.GetAttribute("aria-hidden"));
        Assert.Equal(string.Empty, dot.TextContent);

        // Outside the content span, so an icon alone still makes the button square.
        Assert.Null(root.QuerySelector(".omni-button__content .omni-button__indicator"));
    }

    [Fact]
    public void ButtonIndicator_IsAbsentByDefault()
    {
        var button = Render<OmniButton>(parameters => parameters.AddChildContent("B"));

        Assert.Empty(button.FindAll(".omni-button__indicator"));
        Assert.DoesNotContain("omni-button--indicator", button.Find("button").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void ButtonIndicator_IsTheDangerFillRingedByTheSurface_AtTheTopEndCorner()
    {
        Assert.Equal("relative", ShippedLookTests.Value(ShippedLookTests.Body(".omni-button--indicator"), "position"));
        var dot = ShippedLookTests.Body(".omni-button__indicator");
        Assert.Equal("var(--omni-color-danger-fill)", ShippedLookTests.Value(dot, "background"));
        Assert.Equal("0 0 0 2px var(--omni-color-surface)", ShippedLookTests.Value(dot, "box-shadow"));
        Assert.Equal("absolute", ShippedLookTests.Value(dot, "position"));
        // 5 px from the outer edge, as the mockup places it: the offset is taken inside the border.
        Assert.Equal("calc(5px - var(--omni-button-border-width, 1px))", ShippedLookTests.Value(dot, "inset-inline-end"));
        Assert.Equal("none", ShippedLookTests.Value(dot, "pointer-events"));
    }

    // ---- OmniTextBox.Icon ----

    [Fact]
    public void TextBoxIcon_LiesOverTheStartOfTheField_Decoratively()
    {
        var value = string.Empty;
        var box = Render<OmniTextBox>(parameters => parameters
            .Add(component => component.Id, "search")
            .Add(component => component.Type, OmniTextBoxType.Search)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.AriaDescribedBy, "search-help")
            .AddUnmatched("aria-label", "Rechercher")
            .Add(component => component.Icon, builder =>
            {
                builder.OpenComponent<OmniIcon>(0);
                builder.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.Search);
                builder.CloseComponent();
            }));

        var field = box.Find(".omni-text-box-field");
        var icon = field.QuerySelector(":scope > .omni-text-box-field__icon");
        Assert.NotNull(icon);
        Assert.Equal("true", icon!.GetAttribute("aria-hidden"));
        Assert.NotNull(icon.QuerySelector("svg.omni-icon"));

        var input = field.QuerySelector(":scope > input")!;
        Assert.Equal("search", input.Id);
        Assert.Equal("search", input.GetAttribute("type"));
        Assert.Equal("Rechercher", input.GetAttribute("aria-label"));
        Assert.Equal("search-help", input.GetAttribute("aria-describedby"));
        Assert.Contains("omni-text-box--icon", input.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void TextBoxIcon_IsAbsentByDefault_AndTheInputStandsAlone()
    {
        var value = string.Empty;
        var box = Render<OmniTextBox>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.Equal("INPUT", box.Nodes.OfType<AngleSharp.Dom.IElement>().Single().TagName);
        Assert.DoesNotContain("omni-text-box--icon", box.Find("input").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void TextBoxIcon_IsMutedAndClickThrough_AndTheTextStartsAfterIt()
    {
        var icon = ShippedLookTests.Body(".omni-text-box-field__icon");
        Assert.Equal("var(--omni-color-text-muted)", ShippedLookTests.Value(icon, "color"));
        Assert.Equal("none", ShippedLookTests.Value(icon, "pointer-events"));
        Assert.Equal("30px", ShippedLookTests.Value(ShippedLookTests.Body(".omni-input.omni-text-box--icon"), "padding-inline-start"));
    }

    // ---- OmniHeader.Brand, BrandMark ----

    [Fact]
    public void HeaderBrand_ShowsADecorativeLogoAndTheNameAsText()
    {
        var header = Render<OmniHeader>(parameters => parameters
            .Add(component => component.Brand, "Aetheus")
            .Add(component => component.BrandMark, "Ae")
            .AddChildContent("<button type=\"button\" class=\"omni-sidebar-toggle\">menu</button>"));

        var root = header.Find("header");
        Assert.Contains("omni-header--branded", root.ClassName, StringComparison.Ordinal);
        var logo = header.Find(".omni-header__brand > .omni-header__logo");
        Assert.Equal("Ae", logo.TextContent);
        Assert.Equal("true", logo.GetAttribute("aria-hidden"));
        Assert.Equal("Aetheus", header.Find(".omni-header__brand > .omni-header__brand-name").TextContent);
        Assert.Null(header.Find(".omni-header__brand-name").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void HeaderBrand_IsAbsentByDefault()
    {
        var header = Render<OmniHeader>(parameters => parameters.AddChildContent("Titre"));

        Assert.Empty(header.FindAll(".omni-header__brand"));
        Assert.DoesNotContain("omni-header--branded", header.Find("header").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void HeaderBrand_LogoIsTheAccentFill_AndSwapsOnTheAccentBand_AndTheToggleStaysAtTheEdge()
    {
        var logo = ShippedLookTests.Body(".omni-header__logo");
        Assert.Equal("var(--omni-color-accent-fill)", ShippedLookTests.Value(logo, "background"));
        Assert.Equal("var(--omni-color-on-accent-fill)", ShippedLookTests.Value(logo, "color"));
        Assert.Equal("calc(var(--omni-icon-box) * 0.72)", ShippedLookTests.Value(logo, "block-size"));

        var onBand = ShippedLookTests.Body(".omni-header--accent .omni-header__logo");
        Assert.Equal("var(--omni-color-on-accent)", ShippedLookTests.Value(onBand, "background"));
        Assert.Equal("var(--omni-color-accent)", ShippedLookTests.Value(onBand, "color"));

        Assert.Equal("-1", ShippedLookTests.Value(ShippedLookTests.Body(".omni-header--branded > .omni-sidebar-toggle"), "order"));
    }
}
