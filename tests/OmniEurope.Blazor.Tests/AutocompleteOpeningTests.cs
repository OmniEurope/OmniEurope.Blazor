using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// With <see cref="OmniAutocomplete{TValue}.ShowToggle"/>, a focus or a click on the field opens the whole
/// list as the chevron does (a click on the field's label focuses it, and clicks it); without it, nothing
/// opens on its own. A native select opens its picker on a click on its label, which the focus module's
/// script does on the click itself: here, only that the drop-down asks for it. The focus and the picker
/// themselves belong to the browser.
/// </summary>
public sealed class AutocompleteOpeningTests : OmniBunitContext
{
    private IRenderedComponent<AutocompleteOpeningTestHost> RenderHost(
        string kind = "autocomplete", bool showToggle = true, bool disabled = false, bool readOnly = false) =>
        Render<AutocompleteOpeningTestHost>(parameters => parameters
            .Add(component => component.Kind, kind)
            .Add(component => component.ShowToggle, showToggle)
            .Add(component => component.Disabled, disabled)
            .Add(component => component.ReadOnly, readOnly));

    private static void AssertFullListOpen(IRenderedComponent<AutocompleteOpeningTestHost> host)
    {
        Assert.Equal("true", host.Find("input").GetAttribute("aria-expanded"));
        Assert.Equal(["Bruxelles", "Bruges", "Brno"], host.FindAll("[role=option]").Select(option => option.TextContent.Trim()));
    }

    private static void AssertClosed(IRenderedComponent<AutocompleteOpeningTestHost> host)
    {
        Assert.Equal("false", host.Find("input").GetAttribute("aria-expanded"));
        Assert.Empty(host.FindAll("[role=option]"));
    }

    [Fact]
    public async Task Focus_WithShowToggle_OpensTheWholeList()
    {
        var host = RenderHost();
        AssertClosed(host);

        await host.Find("input").FocusAsync(new FocusEventArgs());

        AssertFullListOpen(host);
    }

    [Fact]
    public async Task Click_WithShowToggle_OpensTheWholeList()
    {
        var host = RenderHost();

        await host.Find("input").ClickAsync(new MouseEventArgs());

        AssertFullListOpen(host);
    }

    [Fact]
    public async Task TheFocusAndTheClickOfOneGesture_SearchOnce()
    {
        var host = RenderHost();

        await host.Find("input").FocusAsync(new FocusEventArgs());
        await host.Find("input").ClickAsync(new MouseEventArgs());

        AssertFullListOpen(host);
        Assert.Equal(1, host.Instance.SearchCount);
    }

    [Fact]
    public async Task ClickWhileOpen_KeepsTheListOpen_WithoutSearchingAgain()
    {
        var host = RenderHost();
        await host.Find("input").ClickAsync(new MouseEventArgs());
        var searches = host.Instance.SearchCount;
        var options = host.FindAll("[role=option]").Select(option => option.Id).ToList();

        await host.Find("input").ClickAsync(new MouseEventArgs());
        await host.Find("input").FocusAsync(new FocusEventArgs());

        AssertFullListOpen(host);
        Assert.Equal(searches, host.Instance.SearchCount);
        Assert.Equal(options, host.FindAll("[role=option]").Select(option => option.Id));
    }

    [Fact]
    public async Task PickingAnOption_ClosesTheList_AndTheFocusStayingInTheFieldDoesNotReopenIt()
    {
        var host = RenderHost();
        await host.Find("input").FocusAsync(new FocusEventArgs());

        await host.FindAll("[role=option]")[1].ClickAsync(new MouseEventArgs());

        AssertClosed(host);

        // The options keep the mousedown from moving the focus: no focus event follows the pick, and the
        // pick itself searches nothing.
        Assert.Equal(1, host.Instance.SearchCount);
    }

    [Fact]
    public async Task Escape_ClosesTheList_AndItStaysClosedUntilTheNextGesture()
    {
        var host = RenderHost();
        await host.Find("input").FocusAsync(new FocusEventArgs());

        await host.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        AssertClosed(host);
        Assert.Equal(1, host.Instance.SearchCount);

        // A new click is a new gesture: it lists again.
        await host.Find("input").ClickAsync(new MouseEventArgs());
        AssertFullListOpen(host);
    }

    [Fact]
    public async Task TheChevron_OpensTheList_AndTheFocusItGivesTheFieldDoesNotSearchAgain()
    {
        var host = RenderHost();
        await host.Find(".omni-autocomplete__toggle").ClickAsync(new MouseEventArgs());
        AssertFullListOpen(host);
        await host.Find("input").FocusAsync(new FocusEventArgs());

        AssertFullListOpen(host);
        Assert.Equal(1, host.Instance.SearchCount);
    }

    [Fact]
    public async Task Typing_StillFiltersTheList()
    {
        var host = RenderHost();
        await host.Find("input").FocusAsync(new FocusEventArgs());

        await host.Find("input").InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "brn" });

        Assert.Equal(["Brno"], host.FindAll("[role=option]").Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public async Task LeavingTheField_ClosesTheList()
    {
        var host = RenderHost();
        await host.Find("input").FocusAsync(new FocusEventArgs());

        await host.Find("input").BlurAsync(new FocusEventArgs());

        AssertClosed(host);
    }

    [Fact]
    public async Task WithoutShowToggle_FocusAndClickOpenNothing_AndNoHandlerIsRegistered()
    {
        var host = RenderHost(showToggle: false);

        Assert.DoesNotContain("blazor:onfocus", host.Find("input").OuterHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("blazor:onclick", host.Find("input").OuterHtml, StringComparison.Ordinal);
        // bUnit refuses to raise an event nothing listens to: a free-text field costs no round trip on focus.
        await Assert.ThrowsAsync<MissingEventHandlerException>(() => host.Find("input").FocusAsync(new FocusEventArgs()));
        await Assert.ThrowsAsync<MissingEventHandlerException>(() => host.Find("input").ClickAsync(new MouseEventArgs()));

        AssertClosed(host);
        Assert.Equal(0, host.Instance.SearchCount);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task WithShowToggle_ADisabledOrReadOnlyField_OpensNothing(bool disabled, bool readOnly)
    {
        var host = RenderHost(disabled: disabled, readOnly: readOnly);

        await host.Find("input").FocusAsync(new FocusEventArgs());
        await host.Find("input").ClickAsync(new MouseEventArgs());

        AssertClosed(host);
        Assert.Equal(0, host.Instance.SearchCount);
    }

    [Fact]
    public async Task FilterableDropDown_OpensTheWholeListOnFocusAndOnClick()
    {
        var host = RenderHost("filterable");

        await host.Find("input").FocusAsync(new FocusEventArgs());
        AssertFullListOpen(host);

        await host.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        AssertClosed(host);

        await host.Find("input").ClickAsync(new MouseEventArgs());
        AssertFullListOpen(host);
    }

    [Theory]
    [InlineData("autocomplete")]
    [InlineData("filterable")]
    public void TheLabelOfAFormField_NamesTheFieldInput_SoItsClickFocusesIt(string kind)
    {
        var host = RenderHost(kind);

        var label = host.Find("label.omni-label");
        var input = host.Find("input");
        Assert.Equal("city", label.GetAttribute("for"));
        Assert.Equal("city", input.Id);
        // The label is not wrapped in anything that would swallow the click (the help mark sits outside it).
        Assert.Empty(label.QuerySelectorAll("button, a"));
    }

    [Fact]
    public void ANativeDropDown_AsksTheFocusModuleToOpenItsPickerOnALabelClick()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        module.SetupVoid("enableLabelPicker");

        var host = RenderHost("native");

        Assert.Equal("city", host.Find("label.omni-label").GetAttribute("for"));
        Assert.Equal("city", host.Find("select").Id);
        Assert.Single(module.Invocations["enableLabelPicker"]);
    }

    [Theory]
    [InlineData("filterable", false)]
    [InlineData("native", true)]
    public void OnlyANativeEditableSelect_AsksForTheLabelPicker(string kind, bool readOnly)
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        module.SetupVoid("enableLabelPicker");

        RenderHost(kind, readOnly: readOnly);

        Assert.Empty(module.Invocations["enableLabelPicker"]);
    }
}
