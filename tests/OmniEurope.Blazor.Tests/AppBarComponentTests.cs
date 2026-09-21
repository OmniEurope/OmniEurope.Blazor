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

    // ---- OmniProfileMenu: avatar trigger and header ----

    [Fact]
    public void ProfileMenu_WithoutSummary_DrawsTheAvatar_NamedByTheLabel()
    {
        var menu = Render<OmniProfileMenu>(parameters => parameters
            .Add(component => component.Label, "Compte de Sony Tumen"));

        Assert.Contains("omni-profile-menu--avatar", menu.Find("details").ClassName, StringComparison.Ordinal);
        var summary = menu.Find("summary.omni-profile-menu__summary");
        Assert.Equal("Compte de Sony Tumen", summary.GetAttribute("aria-label"));
        var avatar = summary.QuerySelector(":scope > .omni-disc.omni-profile-menu__avatar")!;
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

        var avatar = menu.Find("summary > .omni-profile-menu__avatar");
        Assert.Equal("ST", avatar.QuerySelector(".omni-profile-menu__initials")!.TextContent);
        Assert.Null(avatar.QuerySelector("svg"));
    }

    [Fact]
    public void ProfileMenu_WithASummary_KeepsItAndDrawsNoAvatar()
    {
        var menu = Render<OmniProfileMenu>(parameters => parameters
            .Add(component => component.Summary, (RenderFragment)(builder => builder.AddContent(0, "Camille")))
            .Add(component => component.Initials, "CA"));

        Assert.DoesNotContain("omni-profile-menu--avatar", menu.Find("details").ClassName, StringComparison.Ordinal);
        Assert.Equal("Camille", menu.Find("summary").TextContent);
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
                builder.AddContent(1, "Sony Tumen");
                builder.CloseElement();
                builder.OpenElement(2, "span");
                builder.AddContent(3, "Administrateur");
                builder.CloseElement();
            }))
            .AddChildContent<OmniProfileMenuItem>(item => item.AddChildContent("Profil")));

        var panel = menu.Find("details > .omni-profile-menu__panel");
        var header = panel.QuerySelector(":scope > .omni-profile-menu__header")!;
        var list = panel.QuerySelector(":scope > .omni-profile-menu__items")!;
        Assert.Equal("menu", list.GetAttribute("role"));
        Assert.Null(header.GetAttribute("role"));
        Assert.Null(list.QuerySelector(".omni-profile-menu__header"));
        Assert.Single(list.QuerySelectorAll("[role=menuitem]"));

        var avatar = header.QuerySelector(":scope > .omni-disc.omni-disc--lg.omni-profile-menu__avatar")!;
        Assert.Equal("true", avatar.GetAttribute("aria-hidden"));
        Assert.Equal("ST", avatar.TextContent);
        Assert.Equal("Sony Tumen", header.QuerySelector(".omni-profile-menu__identity > strong")!.TextContent);
    }

    [Fact]
    public void ProfileMenuHeader_IsAbsentByDefault_AndTheListStaysTheFloatingSurface()
    {
        var menu = Render<OmniProfileMenu>(parameters => parameters
            .Add(component => component.Summary, (RenderFragment)(builder => builder.AddContent(0, "AB"))));

        Assert.Empty(menu.FindAll(".omni-profile-menu__panel"));
        Assert.Empty(menu.FindAll(".omni-profile-menu__header"));
        Assert.NotNull(menu.Find("details > .omni-profile-menu__items[role=menu]"));
    }

    [Fact]
    public void ProfileMenu_AvatarKeepsA44PixelTarget_TheFocusRing_AndTheInitialsInThePageText()
    {
        var summary = ShippedLookTests.Body(".omni-profile-menu--avatar > .omni-profile-menu__summary");
        Assert.Equal("var(--omni-radius-circle)", ShippedLookTests.Value(summary, "border-radius"));
        Assert.Equal("relative", ShippedLookTests.Value(summary, "position"));
        Assert.Equal(
            "min(0px, calc((var(--omni-icon-box) * 0.9 - 2.75rem) / 2))",
            ShippedLookTests.Value(ShippedLookTests.Body(".omni-profile-menu--avatar > .omni-profile-menu__summary::before"), "inset"));
        Assert.Equal("var(--omni-focus-ring)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-profile-menu--avatar > .omni-profile-menu__summary:focus-visible"), "box-shadow"));
        Assert.Equal("none", ShippedLookTests.Value(ShippedLookTests.Body(".omni-profile-menu--avatar > .omni-profile-menu__summary::-webkit-details-marker"), "display"));

        // The initials are text on the palette grey: the pair text on neutral-fill of the contrast matrix.
        Assert.Equal("var(--omni-color-text)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-profile-menu__initials"), "color"));
        Assert.Equal("var(--omni-color-neutral-fill)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-disc"), "background"));
        Assert.Contains(ThemeContrastMatrixTests.Pairs, pair => pair is ("--omni-color-text", "--omni-color-neutral-fill", _));
    }

    [Fact]
    public void ProfileMenuPanel_TakesTheFloatingSurface_AndTheListInsideShedsIt()
    {
        var panel = ShippedLookTests.Body(".omni-profile-menu__panel");
        Assert.Equal("var(--omni-overlay-background, var(--omni-color-surface))", ShippedLookTests.Value(panel, "background"));
        Assert.Equal("var(--omni-overlay-shadow, var(--omni-shadow-md))", ShippedLookTests.Value(panel, "box-shadow"));
        Assert.Equal("absolute", ShippedLookTests.Value(panel, "position"));

        var inner = ShippedLookTests.Body(".omni-profile-menu__panel > .omni-profile-menu__items");
        Assert.Equal("static", ShippedLookTests.Value(inner, "position"));
        Assert.Equal("none", ShippedLookTests.Value(inner, "box-shadow"));
        Assert.Equal("0", ShippedLookTests.Value(inner, "border"));
    }

    // ---- OmniProfileMenuItem.Icon, Description ----

    private static readonly RenderFragment SettingsIcon = builder =>
    {
        builder.OpenComponent<OmniIcon>(0);
        builder.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.Settings);
        builder.CloseComponent();
    };

    [Fact]
    public void ProfileMenuItem_IconAndDescription_DrawADiscAndAMutedLine()
    {
        var item = Render<OmniProfileMenuItem>(parameters => parameters
            .Add(component => component.Icon, SettingsIcon)
            .Add(component => component.Description, "Thème, langue, notifications")
            .AddChildContent("Paramètres"));

        var button = item.Find("button[role=menuitem]");
        Assert.Contains("omni-profile-menu__item--rich", button.ClassName, StringComparison.Ordinal);
        var disc = button.QuerySelector(":scope > .omni-disc.omni-profile-menu__item-icon")!;
        Assert.Equal("true", disc.GetAttribute("aria-hidden"));
        Assert.NotNull(disc.QuerySelector("svg.omni-icon"));
        var text = button.QuerySelector(":scope > .omni-profile-menu__item-text")!;
        Assert.Equal("Thème, langue, notifications", text.QuerySelector("small.omni-profile-menu__item-description")!.TextContent);
        Assert.StartsWith("Paramètres", text.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileMenuItem_DescriptionAlone_DrawsNoDisc_AndALinkItemTakesItToo()
    {
        var item = Render<OmniProfileMenuItem>(parameters => parameters
            .Add(component => component.Href, "/profil")
            .Add(component => component.Description, "Nom et photo")
            .AddChildContent("Profil"));

        var link = item.Find("a[role=menuitem]");
        Assert.Empty(link.QuerySelectorAll(".omni-disc"));
        Assert.Equal("Nom et photo", link.QuerySelector(".omni-profile-menu__item-description")!.TextContent);
    }

    [Fact]
    public void ProfileMenuItem_WithoutIconOrDescription_RendersItsContentAlone()
    {
        var item = Render<OmniProfileMenuItem>(parameters => parameters
            .Add(component => component.Description, "  ")
            .AddChildContent("<span class=\"own\">Profil</span>"));

        var button = item.Find("button");
        Assert.DoesNotContain("omni-profile-menu__item--rich", button.ClassName, StringComparison.Ordinal);
        Assert.Single(button.Children);
        Assert.Equal("own", button.Children[0].ClassName);
    }

    [Fact]
    public void ProfileMenuItem_DescriptionIsMutedText()
    {
        Assert.Equal("var(--omni-color-text-muted)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-profile-menu__item-description"), "color"));
        Assert.Equal("flex", ShippedLookTests.Value(ShippedLookTests.Body(".omni-profile-menu__item--rich"), "display"));
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
            .AddUnmatched("aria-describedby", "strategy-help"));

        var fieldset = list.Find("fieldset");
        Assert.Equal("true", fieldset.GetAttribute("aria-invalid"));
        Assert.Equal("strategy-help strategy-error", fieldset.GetAttribute("aria-describedby"));
        Assert.Contains("omni-choice-list--invalid", fieldset.ClassName, StringComparison.Ordinal);

        var error = fieldset.QuerySelector(":scope > #strategy-error.omni-form-field__error")!;
        Assert.Equal("alert", error.GetAttribute("role"));
        Assert.Equal("true", error.QuerySelector("svg.omni-form-field__error-icon")!.GetAttribute("aria-hidden"));
        Assert.Equal("Choisissez une stratégie.", error.TextContent.Trim());
        Assert.Same(fieldset.LastElementChild, error);
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
    public void RadioListError_IsAbsentByDefault_AndTheConsumersAttributesPassThrough()
    {
        var value = "a";
        var list = Render<OmniRadioButtonList<string>>(parameters => parameters
            .Add(component => component.Id, "strategy")
            .Add(component => component.Options, [new OmniOption<string>("a", "Progressif")])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .AddUnmatched("aria-describedby", "own-error")
            .AddUnmatched("aria-invalid", "true"));

        var fieldset = list.Find("fieldset");
        Assert.Empty(list.FindAll(".omni-form-field__error"));
        Assert.Equal("own-error", fieldset.GetAttribute("aria-describedby"));
        Assert.Equal("true", fieldset.GetAttribute("aria-invalid"));
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
        Assert.Equal("var(--omni-color-danger)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-choice-list--invalid .omni-radio"), "border-color"));
    }
}
