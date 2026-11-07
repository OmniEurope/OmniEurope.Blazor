using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The Escape that closes the open list of a field must not also close the dialog around it: the first
/// Escape closes the list, the second closes the dialog. The dialog listens with a Blazor handler, so each
/// list stops the key it consumed with <c>@onkeydown:stopPropagation</c> while it is open. These tests cover
/// that wiring on bUnit's event bubbling; what a real Chromium does with the key (the dialog staying open, the
/// text kept, the focus) is proved by the <c>combo</c> part of the scripts probe.
/// </summary>
public sealed class EscapeInDialogTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<string>> Cities =
    [
        new("bxl", "Bruxelles"),
        new("brg", "Bruges"),
        new("brn", "Brno")
    ];

    private static readonly string[] CityNames = ["Bruxelles", "Bruges", "Brno"];

    private IRenderedComponent<OmniDialog> RenderDialog(List<bool> openChanges, Action<ComponentParameterCollectionBuilder<OmniDialog>> content) =>
        Render<OmniDialog>(parameters =>
        {
            parameters
                .Add(component => component.Open, true)
                .Add(component => component.Title, "Dialog")
                .Add(component => component.OpenChanged, EventCallback.Factory.Create<bool>(this, open => openChanges.Add(open)));
            content(parameters);
        });

    private static void Autocomplete(ComponentParameterCollectionBuilder<OmniDialog> parameters)
    {
        var value = string.Empty;
        parameters.AddChildContent<OmniAutocomplete<string>>(child => child
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Debounce, TimeSpan.Zero)
            .Add(component => component.MinimumLength, 0)
            .Add(component => component.Search, (_, _) => Task.FromResult(Cities)));
    }

    private static void FilterableDropDown(ComponentParameterCollectionBuilder<OmniDialog> parameters)
    {
        var value = string.Empty;
        parameters.AddChildContent<OmniDropDown<string>>(child => child
            .Add(component => component.Filterable, true)
            .Add(component => component.Options, Cities)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
    }

    private static void FilterCombo(ComponentParameterCollectionBuilder<OmniDialog> parameters) =>
        parameters.AddChildContent<OmniDataGridFilterCombo>(child => child
            .Add(component => component.Id, "city")
            .Add(component => component.Value, string.Empty)
            .Add(component => component.Suggestions, CityNames));

    public static TheoryData<string> Lists => ["autocomplete", "drop-down", "filter-combo"];

    [Theory]
    [MemberData(nameof(Lists))]
    public async Task Escape_WithTheListOpen_ClosesTheListAndLeavesTheDialog_ASecondEscapeClosesTheDialog(string list)
    {
        var openChanges = new List<bool>();
        var dialog = RenderDialog(openChanges, list switch
        {
            "autocomplete" => Autocomplete,
            "drop-down" => FilterableDropDown,
            _ => FilterCombo
        });
        var input = dialog.Find("input");
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        dialog.WaitForAssertion(() => Assert.NotEmpty(dialog.FindAll("[role=option]")));
        // No key release is listened to while nothing is held back: a handler per key would cost a call per keystroke.
        Assert.False(dialog.Find("input").HasAttribute("blazor:onkeyup"));

        await dialog.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(dialog.FindAll("[role=option]"));
        Assert.Empty(openChanges);
        Assert.True(dialog.Find("input").HasAttribute("blazor:onkeyup"));

        // The key is released, then pressed again: the list is closed, so this Escape is the dialog's.
        await dialog.Find("input").KeyUpAsync(new KeyboardEventArgs { Key = "Escape" });
        await dialog.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal([false], openChanges);
    }

    [Fact]
    public async Task FilterCombo_AnEscapeHeldDown_KeepsBeingConsumed_AfterTheListClosed_UntilTheKeyIsReleased()
    {
        var openChanges = new List<bool>();
        var dialog = RenderDialog(openChanges, FilterCombo);
        await dialog.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await dialog.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(dialog.FindAll("[role=option]"));

        // The key repeats while held: the list is already closed, yet this Escape is the same press, not the dialog's.
        await dialog.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(dialog.FindAll("[role=option]"));
        Assert.Empty(openChanges);
    }

    [Fact]
    public async Task Escape_InTheCharacterSearchOfTheEditor_ClosesThePanelAndLeavesTheDialog_ASecondEscapeClosesTheDialog()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.HtmlEditor);
        module.Setup<string?>("read", _ => true).SetResult(null);
        var openChanges = new List<bool>();
        var value = "<p>A</p>";
        var dialog = RenderDialog(openChanges, parameters => parameters.AddChildContent<OmniHtmlEditor>(child => child
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, [OmniHtmlEditorCommands.InsertSpecialCharacter])));
        await dialog.Find("[data-command=insert-special-character]").ClickAsync(new MouseEventArgs());
        dialog.Find(".omni-html-editor__char-search").Input("eur");

        await dialog.Find(".omni-html-editor__char-search").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(dialog.FindAll(".omni-html-editor__panel"));
        Assert.Empty(openChanges);

        await dialog.Find("[role=textbox]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal([false], openChanges);
    }

    [Fact]
    public async Task OtherKeysInTheCharacterSearchOfTheEditor_LeaveThePanelOpen()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.HtmlEditor);
        module.Setup<string?>("read", _ => true).SetResult(null);
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, [OmniHtmlEditorCommands.InsertSpecialCharacter]));
        await editor.Find("[data-command=insert-special-character]").ClickAsync(new MouseEventArgs());

        await editor.Find(".omni-html-editor__char-search").KeyDownAsync(new KeyboardEventArgs { Key = "a" });

        Assert.Single(editor.FindAll(".omni-html-editor__panel"));
    }

    [Fact]
    public async Task Escape_WithoutAnOpenList_StillClosesTheDialog()
    {
        var openChanges = new List<bool>();
        var dialog = RenderDialog(openChanges, Autocomplete);

        await dialog.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal([false], openChanges);
    }

    [Fact]
    public async Task OtherKeysOfAnOpenList_StayInsideTheField()
    {
        var openChanges = new List<bool>();
        var dialog = RenderDialog(openChanges, Autocomplete);
        var input = dialog.Find("input");
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        dialog.WaitForAssertion(() => Assert.NotEmpty(dialog.FindAll("[role=option]")));

        await dialog.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await dialog.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.Empty(dialog.FindAll("[role=option]"));
        Assert.Empty(openChanges);
    }
}
