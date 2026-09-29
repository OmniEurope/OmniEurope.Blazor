using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The caption buttons of a borderless desktop window: drawn only for the callbacks the host wires, named for
/// assistive technology, the middle one offering to restore a maximized window, close marked for its red hover,
/// and the stylesheet making them square and as tall as the header.
/// </summary>
public sealed class WindowControlsTests : OmniBunitContext
{
    [Fact]
    public void WindowControls_DrawOnlyTheWiredButtons_InCaptionOrder()
    {
        var closed = 0;
        var controls = Render<OmniWindowControls>(parameters => parameters
            .Add(component => component.OnMinimize, () => { })
            .Add(component => component.OnMaximizeRestore, () => { })
            .Add(component => component.OnClose, () => closed++));

        var buttons = controls.FindAll(".omni-window-controls__buttons button");
        Assert.Equal(["Réduire", "Agrandir", "Fermer"], buttons.Select(button => button.GetAttribute("aria-label")));
        Assert.Contains("omni-window-controls__button--close", buttons[^1].ClassList);

        buttons[^1].Click();
        Assert.Equal(1, closed);
    }

    [Fact]
    public void WindowControls_OfferToRestoreAMaximizedWindow_AndHoldExtraActionsBeforeTheCaption()
    {
        var controls = Render<OmniWindowControls>(parameters => parameters
            .Add(component => component.OnMaximizeRestore, () => { })
            .Add(component => component.IsMaximized, true)
            .Add(component => component.Actions, (RenderFragment)(builder => builder.AddMarkupContent(0, "<button class=\"theme\">t</button>"))));

        Assert.Equal("Restaurer", controls.Find(".omni-window-controls__buttons button").GetAttribute("aria-label"));
        Assert.NotNull(controls.Find(".omni-window-controls__actions .theme"));
        Assert.Equal("group", controls.Find(".omni-window-controls").GetAttribute("role"));
    }

    [Fact]
    public void WindowControls_AreSquareAndTakeTheHeaderHeight()
    {
        var button = ShippedLookTests.Body(".omni-window-controls__button, .omni-window-controls__actions .omni-button");
        Assert.Equal("1", ShippedLookTests.Value(button, "aspect-ratio"));
        Assert.Equal("100%", ShippedLookTests.Value(button, "block-size"));
        Assert.Equal("calc(-1 * var(--omni-window-controls-pad-y))", ShippedLookTests.Value(ShippedLookTests.Body(".omni-window-controls"), "margin-block"));
    }
}

/// <summary>OmniMain.AutoHideScrollbar: the page scrollbar shows only while the page moves.</summary>
public sealed class MainAutoHideScrollbarTests : OmniBunitContext
{
    private const string InteropPath = Internal.OmniModules.Interop;

    [Fact]
    public void AutoHideScrollbar_MarksTheScrollingMain_AndAsksTheScriptToWatchIt()
    {
        var module = JSInterop.SetupModule(InteropPath);
        module.SetupVoid("watchScrolling", _ => true).SetVoidResult();

        var main = Render<OmniMain>(parameters => parameters
            .Add(component => component.Scrollable, true)
            .Add(component => component.AutoHideScrollbar, true)
            .AddChildContent("<p>page</p>"));

        Assert.Contains("omni-main--scrollbar-autohide", main.Find("main").ClassList);
        main.WaitForAssertion(() => Assert.Equal("omni-main--scrolling", Assert.Single(module.Invocations["watchScrolling"]).Arguments[1]));
        Assert.Equal("transparent transparent", ShippedLookTests.Value(ShippedLookTests.Body(".omni-main--scrollbar-autohide"), "scrollbar-color"));
    }

    [Fact]
    public void AutoHideScrollbar_IsOffByDefault()
    {
        var main = Render<OmniMain>(parameters => parameters
            .Add(component => component.Scrollable, true)
            .AddChildContent("<p>page</p>"));

        Assert.DoesNotContain("omni-main--scrollbar-autohide", main.Find("main").ClassList);
    }
}
