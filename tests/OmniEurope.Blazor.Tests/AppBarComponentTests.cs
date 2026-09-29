using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The app bar pieces the showcase used to draw with its own stylesheet (PLAN-004 lot 9), now in the
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
            .Add(component => component.Label, "Notifications, 3 non lues")
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
        // 30 px at the drawn control size, scaled with the control like the field's side padding.
        Assert.Equal("calc(1.875rem * var(--omni-control-scale, 1))", ShippedLookTests.Value(ShippedLookTests.Body(".omni-input.omni-text-box--icon"), "padding-inline-start"));
    }

    // ---- OmniIcon tooltip ----

    [Fact]
    public void IconTitleAttribute_BecomesTheSvgTitle_SoTheTooltipShows()
    {
        // A title attribute on an svg shows no tooltip; a <title> child does (recette R-077).
        var icon = Render<OmniIcon>(parameters => parameters
            .Add(component => component.Name, OmniIconName.WifiHigh)
            .AddUnmatched("title", "Connected to backend"));

        Assert.Equal("Connected to backend", icon.Find("svg > title").TextContent);
    }

    [Fact]
    public void IconWithoutTitleOrName_HasNoSvgTitle()
    {
        var icon = Render<OmniIcon>(parameters => parameters.Add(component => component.Name, OmniIconName.WifiHigh));

        Assert.Empty(icon.FindAll("svg > title"));
    }

    // ---- OmniTextBox.DebounceMilliseconds ----

    [Fact]
    public async Task TextBoxDebounce_RaisesOnlyTheLastValue_AfterThePause()
    {
        var raised = new List<string>();
        var value = string.Empty;
        var box = Render<OmniTextBox>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, (string next) => raised.Add(next))
            .Add(component => component.DebounceMilliseconds, 150));

        var input = box.Find("input");
        var first = input.InputAsync(new ChangeEventArgs { Value = "a" });
        var second = input.InputAsync(new ChangeEventArgs { Value = "ab" });
        var third = input.InputAsync(new ChangeEventArgs { Value = "abc" });

        Assert.Empty(raised);
        await Task.WhenAll(first, second, third);
        Assert.Equal(["abc"], raised);
    }

    [Fact]
    public async Task TextBoxDebounce_IsOffByDefault_EveryKeystrokeRaises()
    {
        var raised = new List<string>();
        var value = string.Empty;
        var box = Render<OmniTextBox>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, (string next) => raised.Add(next)));

        await box.Find("input").InputAsync(new ChangeEventArgs { Value = "a" });
        await box.Find("input").InputAsync(new ChangeEventArgs { Value = "ab" });

        Assert.Equal(["a", "ab"], raised);
    }

    // ---- OmniHeader.Brand, BrandLogo ----

    [Fact]
    public void HeaderBrand_ShowsADecorativeLogoAndTheNameAsText()
    {
        var header = Render<OmniHeader>(parameters => parameters
            .Add(component => component.Brand, "Boutique")
            .Add(component => component.BrandLogo, "logo.svg")
            .AddChildContent("<button type=\"button\" class=\"omni-sidebar-toggle\">menu</button>"));

        var root = header.Find("header");
        Assert.Contains("omni-header--branded", root.ClassName, StringComparison.Ordinal);
        var logo = header.Find(".omni-header__brand > img.omni-header__logo-image");
        Assert.Equal("logo.svg", logo.GetAttribute("src"));
        Assert.Equal(string.Empty, logo.GetAttribute("alt"));
        Assert.Equal("true", logo.GetAttribute("aria-hidden"));
        Assert.Equal("Boutique", header.Find(".omni-header__brand > .omni-header__brand-name").TextContent);
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
    public void HeaderBrand_TheToggleStaysAtTheEdge()
    {
        Assert.Equal("-1", ShippedLookTests.Value(ShippedLookTests.Body(".omni-header--branded > .omni-sidebar-toggle"), "order"));
    }

    // ---- OmniProfileMenu: avatar trigger and header ----

    [Fact]
    public void ProfileMenu_WithoutSummary_DrawsTheAvatar_NamedByTheLabel()
    {
        var menu = Render<OmniProfileMenu>(parameters => parameters
            .Add(component => component.Label, "Compte de Camille Martin"));

        Assert.Contains("omni-profile-menu--avatar", menu.Find(".omni-profile-menu").ClassName, StringComparison.Ordinal);
        var trigger = menu.Find("button.omni-profile-menu__trigger");
        Assert.Equal("Compte de Camille Martin", trigger.GetAttribute("aria-label"));
        Assert.Equal("menu", trigger.GetAttribute("aria-haspopup"));
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        var avatar = trigger.QuerySelector(":scope > .omni-disc.omni-profile-menu__avatar")!;
        Assert.Equal("true", avatar.GetAttribute("aria-hidden"));
        Assert.NotNull(avatar.QuerySelector("svg.omni-icon"));
        Assert.Null(avatar.QuerySelector(".omni-profile-menu__initials"));
    }

    [Fact]
    public void ProfileMenu_Initials_ReplaceTheUserGlyph()
    {
        var menu = Render<OmniProfileMenu>(parameters => parameters
            .Add(component => component.Initials, " ST ")
            .Add(component => component.Label, "Compte"));

        var avatar = menu.Find(".omni-profile-menu__trigger > .omni-profile-menu__avatar");
        Assert.Equal("ST", avatar.QuerySelector(".omni-profile-menu__initials")!.TextContent);
        Assert.Null(avatar.QuerySelector("svg"));
    }

    [Fact]
    public void ProfileMenu_WithASummary_KeepsItAndDrawsNoAvatar()
    {
        var menu = Render<OmniProfileMenu>(parameters => parameters
            .Add(component => component.Summary, (RenderFragment)(builder => builder.AddContent(0, "Camille")))
            .Add(component => component.Initials, "CA"));

        Assert.DoesNotContain("omni-profile-menu--avatar", menu.Find(".omni-profile-menu").ClassName, StringComparison.Ordinal);
        Assert.Equal("Camille", menu.Find(".omni-profile-menu__trigger").TextContent.Trim());
        Assert.Empty(menu.FindAll(".omni-profile-menu__avatar"));
    }

    [Fact]
    public void ProfileMenuHeader_SitsOutsideTheMenuRole_BesideALargeAvatar()
    {
        var menu = Render<OmniProfileMenu>(parameters => parameters
            .Add(component => component.Initials, "ST")
            .Add(component => component.Header, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "strong");
                builder.AddContent(1, "Camille Martin");
                builder.CloseElement();
                builder.OpenElement(2, "span");
                builder.AddContent(3, "Administrateur");
                builder.CloseElement();
            }))
            .AddChildContent<OmniMenuItem>(item => item.AddChildContent("Profil")));

        menu.Find(".omni-profile-menu__trigger").Click();

        var panel = menu.Find(".omni-profile-menu__popup--header[data-omni-menu-surface]");
        Assert.Contains("omni-menu", panel.ClassList);
        var header = panel.QuerySelector(":scope > .omni-profile-menu__header")!;
        var list = panel.QuerySelector(":scope > .omni-profile-menu__items")!;
        Assert.Equal("menu", list.GetAttribute("role"));
        Assert.Null(header.GetAttribute("role"));
        Assert.Null(list.QuerySelector(".omni-profile-menu__header"));
        Assert.Single(list.QuerySelectorAll("[role=menuitem]"));

        var avatar = header.QuerySelector(":scope > .omni-disc.omni-disc--large.omni-profile-menu__avatar")!;
        Assert.Equal("true", avatar.GetAttribute("aria-hidden"));
        Assert.Equal("ST", avatar.TextContent);
        Assert.Equal("Camille Martin", header.QuerySelector(".omni-profile-menu__identity > strong")!.TextContent);
    }

    [Fact]
    public void ProfileMenuHeader_IsAbsentByDefault_AndTheListIsTheFloatingSurface()
    {
        var menu = Render<OmniProfileMenu>(parameters => parameters
            .Add(component => component.Summary, (RenderFragment)(builder => builder.AddContent(0, "AB")))
            .AddChildContent<OmniMenuItem>(item => item.AddChildContent("Profil")));

        Assert.Empty(menu.FindAll("[role=menu]"));
        menu.Find(".omni-profile-menu__trigger").Click();

        Assert.Empty(menu.FindAll(".omni-profile-menu__popup--header"));
        Assert.Empty(menu.FindAll(".omni-profile-menu__header"));
        Assert.NotNull(menu.Find(".omni-menu.omni-profile-menu__popup[role=menu]"));
    }

    [Fact]
    public void ProfileMenu_AvatarKeepsA44PixelTarget_TheFocusRing_AndTheInitialsInThePageText()
    {
        var trigger = ShippedLookTests.Body(".omni-profile-menu--avatar > .omni-profile-menu__trigger");
        Assert.Equal("var(--omni-radius-circle)", ShippedLookTests.Value(trigger, "border-radius"));
        Assert.Equal("relative", ShippedLookTests.Value(trigger, "position"));
        Assert.Equal(
            "min(0px, calc((var(--omni-icon-box) * 0.9 - 2.75rem) / 2))",
            ShippedLookTests.Value(ShippedLookTests.Body(".omni-profile-menu--avatar > .omni-profile-menu__trigger::before"), "inset"));
        Assert.Equal("var(--omni-focus-ring)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-profile-menu__trigger:focus-visible"), "box-shadow"));

        // The initials are text on the palette grey: the pair text on neutral-fill of the contrast matrix.
        Assert.Equal("var(--omni-color-text)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-profile-menu__initials"), "color"));
        Assert.Equal("var(--omni-color-neutral-fill)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-disc"), "background"));
        Assert.Contains(ThemeContrastMatrixTests.Pairs, pair => pair is ("--omni-color-text", "--omni-color-neutral-fill", _));
    }

    [Fact]
    public void ProfileMenuPopup_IsTheSharedMenuSurface_AndItsItemIconsSitInADisc()
    {
        var surface = DialogIntentAndOverflowMenuTests.BodyWith(".omni-menu", "backdrop-filter");
        Assert.Equal("var(--omni-overlay-background, var(--omni-color-surface))", ShippedLookTests.Value(surface, "background"));
        Assert.Equal("var(--omni-overlay-shadow, var(--omni-shadow-md))", ShippedLookTests.Value(surface, "box-shadow"));
        Assert.Equal("fixed", ShippedLookTests.Value(surface, "position"));

        var disc = ShippedLookTests.Body(".omni-profile-menu__popup .omni-menu__icon:not(:empty)");
        Assert.Equal("var(--omni-color-neutral-fill)", ShippedLookTests.Value(disc, "background"));
        Assert.Equal("var(--omni-radius-circle)", ShippedLookTests.Value(disc, "border-radius"));
        Assert.Equal("none", ShippedLookTests.Value(ShippedLookTests.Body(".omni-profile-menu__popup .omni-menu__icon:empty"), "display"));
    }

    // ---- OmniMenuItem in a profile menu ----

    private static readonly RenderFragment SettingsIcon = builder =>
    {
        builder.OpenComponent<OmniIcon>(0);
        builder.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.Settings);
        builder.CloseComponent();
    };

    [Fact]
    public void MenuItem_Icon_IsDecorativeBeforeTheLabel()
    {
        var item = Render<OmniMenuItem>(parameters => parameters
            .Add(component => component.Icon, SettingsIcon)
            .AddChildContent("Paramètres"));

        var button = item.Find("button[role=menuitem]");
        Assert.Equal("-1", button.GetAttribute("tabindex"));
        var icon = button.QuerySelector(":scope > .omni-menu__icon")!;
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
        Assert.NotNull(icon.QuerySelector("svg.omni-icon"));
        Assert.Equal("Paramètres", button.QuerySelector(":scope > .omni-menu__label")!.TextContent);
    }

    [Fact]
    public void MenuItem_LinkItemTakesTheIconToo_AndADisabledOneIsAButton()
    {
        var item = Render<OmniMenuItem>(parameters => parameters
            .Add(component => component.Href, "/profil")
            .Add(component => component.Icon, SettingsIcon)
            .AddChildContent("Profil"));

        var link = item.Find("a[role=menuitem]");
        Assert.Equal("/profil", link.GetAttribute("href"));
        Assert.Equal("-1", link.GetAttribute("tabindex"));
        Assert.NotNull(link.QuerySelector(":scope > .omni-menu__icon svg"));
        Assert.Equal("Profil", link.QuerySelector(".omni-menu__label")!.TextContent);

        item.Render(parameters => parameters.Add(component => component.Disabled, true));
        Assert.Empty(item.FindAll("a"));
        Assert.True(item.Find("button[role=menuitem]").HasAttribute("disabled"));
    }

    [Theory]
    [InlineData(OmniTone.Neutral, null)]
    [InlineData(OmniTone.Danger, "omni-menu__item--danger")]
    [InlineData(OmniTone.Accent, "omni-menu__item--accent")]
    public void MenuItem_Tone_ColoursTheItem(OmniTone tone, string? expectedClass)
    {
        var item = Render<OmniMenuItem>(parameters => parameters
            .Add(component => component.Tone, tone)
            .AddChildContent("Supprimer"));

        var modifiers = item.Find(".omni-menu__item").ClassList.Where(name => name.StartsWith("omni-menu__item--", StringComparison.Ordinal)).ToList();
        string[] expected = expectedClass is null ? [] : [expectedClass];
        Assert.Equal(expected, modifiers);
        Assert.Equal("var(--omni-color-danger)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-menu__item--danger"), "color"));
    }

    // ---- OmniRadioButtonList.Error ----

    [Fact]
    public void RadioListError_MarksTheGroupInvalid_AndDescribesItAfterTheConsumersOwnDescription()
    {
        var value = "a";
        var list = Render<OmniRadioButtonList<string>>(parameters => parameters
            .Add(component => component.Id, "strategy")
            .Add(component => component.Label, "Stratégie")
            .Add(component => component.Options, [new OmniOption<string>("a", "Progressif"), new OmniOption<string>("b", "Bleu vert")])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Error, "Choisissez une stratégie.")
            .Add(component => component.AriaDescribedBy, "strategy-help"));

        var fieldset = list.Find("fieldset");
        Assert.Equal("true", fieldset.GetAttribute("aria-invalid"));
        Assert.Equal("strategy-help strategy-error", fieldset.GetAttribute("aria-describedby"));
        Assert.Contains("omni-choice-list--invalid", fieldset.ClassName, StringComparison.Ordinal);

        // The error line sits in the polite live region of the field, last in the group: a field error is
        // not a blocking one, so it is not role="alert".
        var region = fieldset.LastElementChild!;
        Assert.Contains("omni-form-field__message", region.ClassList);
        Assert.Equal("polite", region.GetAttribute("aria-live"));
        var error = region.QuerySelector(":scope > #strategy-error.omni-form-field__error")!;
        Assert.False(error.HasAttribute("role"));
        Assert.Equal("true", error.QuerySelector("svg.omni-form-field__error-icon")!.GetAttribute("aria-hidden"));
        Assert.Equal("Choisissez une stratégie.", error.TextContent.Trim());
    }

    [Fact]
    public void RadioListError_WithoutAnId_IsNamedAfterTheGroup()
    {
        string? value = null;
        var list = Render<OmniRadioButtonList<string?>>(parameters => parameters
            .Add(component => component.Name, "delivery")
            .Add(component => component.Options, [new OmniOption<string?>("a", "Standard")])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Error, "Obligatoire"));

        Assert.Equal("delivery-error", list.Find("fieldset").GetAttribute("aria-describedby"));
        Assert.NotNull(list.Find("#delivery-error"));
    }

    [Fact]
    public void RadioListError_IsAbsentByDefault_AndTheConsumersDescriptionPassesThrough()
    {
        // The description is the AriaDescribedBy parameter, shared with OmniCheckBoxList; the invalid
        // state comes from Error or from the form's validation, no longer from a raw attribute.
        var value = "a";
        var list = Render<OmniRadioButtonList<string>>(parameters => parameters
            .Add(component => component.Id, "strategy")
            .Add(component => component.Options, [new OmniOption<string>("a", "Progressif")])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.AriaDescribedBy, "own-error"));

        var fieldset = list.Find("fieldset");
        Assert.Empty(list.FindAll(".omni-form-field__error"));
        Assert.Equal("own-error", fieldset.GetAttribute("aria-describedby"));
        Assert.False(fieldset.HasAttribute("aria-invalid"));
        Assert.DoesNotContain("omni-choice-list--invalid", fieldset.ClassName, StringComparison.Ordinal);

        var plain = Render<OmniRadioButtonList<string>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("a", "Progressif")])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
        Assert.False(plain.Find("fieldset").HasAttribute("aria-describedby"));
        Assert.False(plain.Find("fieldset").HasAttribute("aria-invalid"));
    }

    [Fact]
    public void RadioListError_GivesEveryRadioTheDangerBorder()
    {
        Assert.Equal("var(--omni-color-danger)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-choice-list--invalid :is(.omni-radio, .omni-checkbox)"), "border-color"));
    }
}
