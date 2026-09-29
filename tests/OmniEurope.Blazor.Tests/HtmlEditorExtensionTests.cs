using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniHtmlEditor.Extensions"/>: what an extension adds (toolbar, policy, shortcuts, inline
/// elements, context menu, selection) and the primitives its commands act through. The surface script
/// is bUnit's module double, so these tests cover what .NET sends to it and does with its answers.
/// </summary>
public sealed class HtmlEditorExtensionTests : OmniBunitContext
{
    private const string ModulePath = OmniModules.HtmlEditor;
    private const string FocusModulePath = OmniModules.Focus;

    [Fact]
    public void ArrangeToolbar_ByDefault_AppendsTheCommandsAfterASeparator()
    {
        JSInterop.SetupModule(ModulePath);
        var editor = RenderEditor(new TestExtension { ExtraCommands = [Command("stamp")] }, [OmniHtmlEditorCommands.Bold]);

        Assert.Equal(["bold", "stamp"], ToolbarNames(editor));
        Assert.Single(editor.FindAll(".omni-html-editor__toolbar [role=separator]"));
    }

    [Fact]
    public void Extensions_ArrangeTheToolbarInTheirOrder_AndMayReorderIt()
    {
        JSInterop.SetupModule(ModulePath);
        var first = new TestExtension { ExtraCommands = [Command("first")] };
        var second = new TestExtension { Arrange = toolbar => [Command("save"), .. toolbar.Reverse()] };

        var editor = RenderEditor([first, second], [OmniHtmlEditorCommands.Bold]);

        Assert.Equal(["save", "first", "bold"], ToolbarNames(editor));
    }

    [Fact]
    public void Merge_KeepsWhatEitherPolicyKeeps_AndNullIsTheOtherSide()
    {
        var first = new OmniHtmlSanitizerPolicy { AdditionalCssClasses = ["note"], AdditionalTagAttributes = new Dictionary<string, IReadOnlyList<string>> { ["span"] = ["title"] } };
        var second = new OmniHtmlSanitizerPolicy { AdditionalCssClasses = ["formula"], AllowDataAttributes = true, AdditionalTagAttributes = new Dictionary<string, IReadOnlyList<string>> { ["SPAN"] = ["lang"] } };

        var merged = OmniHtmlSanitizerPolicy.Merge(first, second)!;

        Assert.Equal(["note", "formula"], merged.AdditionalCssClasses);
        Assert.True(merged.AllowDataAttributes);
        Assert.Equal(["title", "lang"], merged.AdditionalTagAttributes["span"]);
        Assert.Same(first, OmniHtmlSanitizerPolicy.Merge(first, null));
        Assert.Same(second, OmniHtmlSanitizerPolicy.Merge(null, second));
        Assert.Null(OmniHtmlSanitizerPolicy.Merge(null, null));
    }

    [Fact]
    public void ExtensionPolicies_AreMerged_WhereverTheValueIsSanitised()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var value = "<p><span class=\"note\" data-marker=\"1\">N</span><span class=\"formula\">F</span><span class=\"other\">O</span></p>";
        var extension = new TestExtension { Policy = new OmniHtmlSanitizerPolicy { AdditionalCssClasses = ["formula"] } };

        Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Extensions, [new PolicyExtension(new OmniHtmlSanitizerPolicy { AdditionalCssClasses = ["note"], AllowDataAttributes = true }), extension]));

        var mount = Assert.Single(module.Invocations["mount"]);
        var html = (string)mount.Arguments[2]!;
        Assert.Contains("class=\"note\" data-marker=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"formula\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("other", html, StringComparison.Ordinal);
        var options = JsonSerializer.SerializeToElement(mount.Arguments[3]);
        var classes = options.GetProperty("classes").EnumerateArray().Select(item => item.GetString()).ToList();
        Assert.Contains("note", classes);
        Assert.Contains("formula", classes);
        Assert.DoesNotContain("other", classes);
    }

    [Fact]
    public void ANewListOfTheSameExtensions_DoesNotReconfigureTheSurface()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var extension = new TestExtension { Policy = new OmniHtmlSanitizerPolicy { AdditionalCssClasses = ["note"] } };
        var editor = RenderEditor(extension, null);

        editor.Render(parameters => parameters.Add(component => component.Extensions, [extension]));

        Assert.Empty(module.Invocations["configure"]);
    }

    [Fact]
    public async Task TracksSelection_AsksTheSurfaceToReport_AndHandsTheSelectionToTheExtension()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var extension = new TestExtension { Tracks = true };
        var editor = RenderEditor(extension, null);

        var options = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["mount"]).Arguments[3]);
        Assert.True(options.GetProperty("selection").GetBoolean());

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnSelectionChanged(
            "{\"collapsed\":true,\"ancestors\":[{\"tag\":\"td\",\"classes\":[],\"data\":{},\"colspan\":2,\"rowspan\":3}]}"));

        var cell = Assert.Single(extension.Selections).Closest("td")!;
        Assert.Equal(2, cell.ColumnSpan);
        Assert.Equal(3, cell.RowSpan);
    }

    [Fact]
    public void SelectionNode_OutsideACell_SpansOne()
    {
        var node = Assert.Single(OmniHtmlEditorSelection.Parse("{\"ancestors\":[{\"tag\":\"p\",\"classes\":[],\"data\":{},\"colspan\":-4}]}").Ancestors);

        Assert.Equal(1, node.ColumnSpan);
        Assert.Equal(1, node.RowSpan);
    }

    [Theory]
    [InlineData("Ctrl+Shift+M", "ctrl+shift+m")]
    [InlineData("shift+cmd+m", "ctrl+shift+m")]
    [InlineData("Alt+Control+F", "ctrl+alt+f")]
    [InlineData("F9", "f9")]
    public void Shortcut_IsNormalised_ModifiersInAFixedOrder(string keys, string expected) =>
        Assert.Equal(expected, OmniHtmlEditorShortcut.Normalize(keys));

    [Theory]
    [InlineData("Ctrl+Z")]
    [InlineData("Ctrl+Shift+Z")]
    [InlineData("Meta+K")]
    [InlineData("Hyper+M")]
    [InlineData("Ctrl++")]
    public void Shortcut_ReservedMalformedOrUnknown_Throws(string keys) =>
        Assert.Throws<ArgumentException>(() => OmniHtmlEditorShortcut.Normalize(keys));

    [Fact]
    public async Task Shortcut_IsSentToTheSurface_AndRunsItsCommandFromTheBridge()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("read", _ => true).SetResult(null);
        var ran = 0;
        var extension = new TestExtension
        {
            ExtraCommands = [OmniHtmlEditorCommand.Create("stamp", "Tampon", _ => { ran++; return Task.CompletedTask; })],
            Keys = [new OmniHtmlEditorShortcut("Ctrl+Shift+S", "stamp")]
        };
        var editor = RenderEditor(extension, null);

        var options = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["mount"]).Arguments[3]);
        Assert.Equal(["ctrl+shift+s"], options.GetProperty("shortcuts").EnumerateArray().Select(item => item.GetString()));

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnShortcut(0));

        Assert.Equal(1, ran);
    }

    [Fact]
    public void Shortcut_NamingNoCommand_Throws()
    {
        JSInterop.SetupModule(ModulePath);
        var extension = new TestExtension { Keys = [new OmniHtmlEditorShortcut("Ctrl+Shift+S", "missing")] };

        var error = Assert.Throws<InvalidOperationException>(() => RenderEditor(extension, null));

        Assert.Contains("missing", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Shortcut_DeclaredTwice_Throws()
    {
        JSInterop.SetupModule(ModulePath);
        var extension = new TestExtension
        {
            ExtraCommands = [Command("stamp")],
            Keys = [new OmniHtmlEditorShortcut("Ctrl+Shift+S", "stamp"), new OmniHtmlEditorShortcut("shift+ctrl+s", "stamp")]
        };

        Assert.Throws<InvalidOperationException>(() => RenderEditor(extension, null));
    }

    [Fact]
    public async Task InlineElement_IsActivatedWithItsElementAndText_AndReplacedThroughTheSanitiser()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("read", _ => true).SetResult(null);
        module.Setup<string?>("replaceActivated", _ => true).SetResult("<p>Texte <span class=\"note\">Neuve</span></p>");
        OmniHtmlEditorElementContext? activated = null;
        var extension = new TestExtension
        {
            Policy = new OmniHtmlSanitizerPolicy { AdditionalCssClasses = ["note"], AllowDataAttributes = true },
            Inline = [new OmniHtmlEditorInlineElement(".note", async context =>
            {
                activated = context;
                await context.ReplaceAsync("<span class=\"note\" onclick=\"x()\">Neuve</span>");
            })]
        };
        string? bound = "<p>Texte <span class=\"note\" data-marker=\"1\">Ancienne</span></p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, bound)
            .Add(component => component.ValueExpression, () => bound)
            .Add(component => component.ValueChanged, value => bound = value)
            .Add(component => component.Extensions, [extension]));

        var options = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["mount"]).Arguments[3]);
        Assert.Equal([".note"], options.GetProperty("inline").EnumerateArray().Select(item => item.GetString()));

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnElementActivated(
            0, "{\"tag\":\"span\",\"classes\":[\"note\"],\"data\":{\"data-marker\":\"1\"}}", "Ancienne"));

        Assert.NotNull(activated);
        Assert.Equal("1", activated!.Element.DataAttributes["data-marker"]);
        Assert.Equal("Ancienne", activated.Text);
        var replace = Assert.Single(module.Invocations["replaceActivated"]);
        Assert.Equal("<span class=\"note\">Neuve</span>", replace.Arguments[1]);
        Assert.Equal("<p>Texte <span class=\"note\">Neuve</span></p>", bound);
    }

    [Fact]
    public async Task InlineElement_SetText_KeepsTheElement_AndCommitsTheSurfaceResult()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("read", _ => true).SetResult(null);
        module.Setup<string?>("setActivatedText", _ => true).SetResult("<p><span class=\"note\" data-marker=\"1\">Revue</span></p>");
        var extension = new TestExtension
        {
            Policy = new OmniHtmlSanitizerPolicy { AdditionalCssClasses = ["note"], AllowDataAttributes = true },
            Inline = [new OmniHtmlEditorInlineElement(".note", context => context.SetTextAsync("Revue <b>"))]
        };
        string? bound = "<p><span class=\"note\" data-marker=\"1\">Ancienne</span></p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, bound)
            .Add(component => component.ValueExpression, () => bound)
            .Add(component => component.ValueChanged, value => bound = value)
            .Add(component => component.Extensions, [extension]));

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnElementActivated(
            0, "{\"tag\":\"span\",\"classes\":[\"note\"],\"data\":{\"data-marker\":\"1\"}}", "Ancienne"));

        // The text goes to the surface as text: the script sets textContent, never markup.
        Assert.Equal("Revue <b>", Assert.Single(module.Invocations["setActivatedText"]).Arguments[1]);
        Assert.Equal("<p><span class=\"note\" data-marker=\"1\">Revue</span></p>", bound);
    }

    [Fact]
    public async Task Suggestion_AsksTheSurfaceToOffer_AndReturnsTheFirstExtensionsProposal()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var silent = new SuggestingExtension(_ => null);
        var proposing = new SuggestingExtension(text => text.EndsWith("sous", StringComparison.Ordinal) ? " réserve" : null);
        var editor = RenderEditor([silent, proposing], null);

        var options = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["mount"]).Arguments[3]);
        Assert.True(options.GetProperty("suggest").GetBoolean());
        var bridge = new HtmlEditorInteropBridge(editor.Instance);

        Assert.Equal(" réserve", await editor.InvokeAsync(() => bridge.OnSuggestionRequested("Avis favorable sous")));
        Assert.Null(await editor.InvokeAsync(() => bridge.OnSuggestionRequested("Autre chose")));
        Assert.Equal(["Avis favorable sous", "Autre chose"], silent.Asked);
    }

    [Fact]
    public void Suggestion_WithoutAnExtensionThatSuggests_IsNotOffered()
    {
        var module = JSInterop.SetupModule(ModulePath);
        RenderEditor(new TestExtension(), null);

        var options = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["mount"]).Arguments[3]);
        Assert.False(options.TryGetProperty("suggest", out _));
    }

    [Fact]
    public async Task Suggestion_InTheSourceFace_OrDisabled_IsNull()
    {
        var extension = new SuggestingExtension(_ => "suite");
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.Extensions, [extension]));

        Assert.Null(await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnSuggestionRequested("Avis favorable")));
        Assert.Empty(extension.Asked);
    }

    [Fact]
    public async Task InlineElement_WithAnUnknownPosition_OrMalformedElement_DoesNothing()
    {
        JSInterop.SetupModule(ModulePath);
        var activations = 0;
        var extension = new TestExtension { Inline = [new OmniHtmlEditorInlineElement(".note", _ => { activations++; return Task.CompletedTask; })] };
        var editor = RenderEditor(extension, null);
        var bridge = new HtmlEditorInteropBridge(editor.Instance);

        await editor.InvokeAsync(() => bridge.OnElementActivated(3, "{\"tag\":\"span\",\"classes\":[],\"data\":{}}", ""));
        await editor.InvokeAsync(() => bridge.OnElementActivated(0, "{\"classes\":[]}", ""));

        Assert.Equal(0, activations);
    }

    [Fact]
    public async Task ContextMenu_OpensAtThePointer_RunsACommand_AndCloses()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("read", _ => true).SetResult(null);
        module.SetupVoid("restoreMenuSelection", _ => true).SetVoidResult();
        var focus = JSInterop.SetupModule(FocusModulePath);
        focus.SetupVoid("openMenu", _ => true).SetVoidResult();
        focus.SetupVoid("closeMenu", _ => true).SetVoidResult();
        var ran = 0;
        var extension = new TestExtension
        {
            Menu =
            [
                OmniHtmlEditorCommand.Create("edit-note", "Modifier la note", _ => { ran++; return Task.CompletedTask; }),
                OmniHtmlEditorCommands.Separator,
                OmniHtmlEditorCommand.Create("never", "Jamais", _ => Task.CompletedTask) with { Enabled = _ => false }
            ]
        };
        var editor = RenderEditor(extension, null);

        var options = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["mount"]).Arguments[3]);
        Assert.True(options.GetProperty("menu").GetBoolean());
        Assert.Empty(editor.FindAll("[role=menu]"));

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnContextMenu(120, 80, null));

        // The package's one menu engine, opened at the pointer over the surface.
        var open = Assert.Single(focus.Invocations["openMenu"]);
        Assert.Equal(editor.Find("[role=menu]").GetAttribute("id"), open.Arguments[0]);
        Assert.Equal("pointer", open.Arguments[3]);
        Assert.Equal(120d, open.Arguments[4]);
        Assert.Equal(80d, open.Arguments[5]);
        Assert.Contains("omni-menu", editor.Find("[role=menu]").ClassList);
        Assert.Equal(["edit-note", "never"], editor.FindAll("[role=menu] [role=menuitem]").Select(item => item.GetAttribute("data-command")));
        Assert.All(editor.FindAll("[role=menu] [role=menuitem]"), item => Assert.Contains("omni-menu__item", item.ClassList));
        Assert.True(editor.Find("[role=menuitem][data-command=never]").HasAttribute("disabled"));
        Assert.Single(editor.FindAll("[role=menu] [role=separator]"));

        await editor.Find("[role=menuitem][data-command=edit-note]").ClickAsync(new MouseEventArgs());

        Assert.Equal(1, ran);
        Assert.Empty(editor.FindAll("[role=menu]"));
        // The item closes the menu first, the focus back where the menu was asked for.
        var close = Assert.Single(focus.Invocations["closeMenu"]);
        Assert.Equal(true, close.Arguments[1]);
        // The selection the menu opened on is put back before the command acts.
        Assert.Single(module.Invocations["restoreMenuSelection"]);
    }

    [Fact]
    public async Task ContextMenu_Keys_AreThoseOfEveryMenu()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("read", _ => true).SetResult(null);
        var focus = JSInterop.SetupModule(FocusModulePath);
        focus.SetupVoid("openMenu", _ => true).SetVoidResult();
        focus.SetupVoid("closeMenu", _ => true).SetVoidResult();
        focus.SetupVoid("moveMenuFocus", _ => true).SetVoidResult();
        var extension = new TestExtension { Menu = [OmniHtmlEditorCommand.Create("edit-note", "Modifier la note", _ => Task.CompletedTask)] };
        var editor = RenderEditor(extension, null);
        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnContextMenu(10, 10, null));

        await editor.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("ArrowDown", Assert.Single(focus.Invocations["moveMenuFocus"]).Arguments[1]);

        await editor.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(editor.FindAll("[role=menu]"));
        Assert.Equal(true, Assert.Single(focus.Invocations["closeMenu"]).Arguments[1]);
    }

    [Fact]
    public async Task ContextMenu_EnablesItsEntriesForTheSelectionWhereItOpens()
    {
        JSInterop.SetupModule(ModulePath);
        var focus = JSInterop.SetupModule(FocusModulePath);
        focus.SetupVoid("openMenu", _ => true).SetVoidResult();
        var extension = new TestExtension
        {
            Menu = [OmniHtmlEditorCommand.Create("remove-note", "Retirer", _ => Task.CompletedTask) with { Enabled = selection => selection?.ClosestWithClass("note") is not null }]
        };
        var editor = RenderEditor(extension, null);

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnContextMenu(
            5, 5, "{\"collapsed\":false,\"ancestors\":[{\"tag\":\"span\",\"classes\":[\"note\"],\"data\":{}}]}"));

        Assert.False(editor.Find("[role=menuitem][data-command=remove-note]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task ContextMenu_WithoutEntries_LeavesTheBrowsersMenu()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var editor = RenderEditor(new TestExtension(), null);

        var options = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["mount"]).Arguments[3]);
        Assert.False(options.TryGetProperty("menu", out _));

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnContextMenu(10, 10, null));

        Assert.Empty(editor.FindAll("[role=menu]"));
    }

    [Fact]
    public async Task ReplaceClosest_SendsSanitisedHtml_CommitsTheResult_AndSaysWhetherItFoundTheElement()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("read", _ => true).SetResult(null);
        var answers = new Queue<string?>(["<p>x <span class=\"formula\">y</span></p>", null]);
        module.Setup<string?>("replaceClosest", _ => true).SetResult(null);
        var found = new List<bool>();
        var extension = new TestExtension
        {
            Policy = new OmniHtmlSanitizerPolicy { AdditionalCssClasses = ["formula"] },
            ExtraCommands = [OmniHtmlEditorCommand.Create("formula", "Formule", async context =>
                found.Add(await context.ReplaceClosestAsync("span.formula", "<span class=\"formula\"><script>x</script>y</span>")))]
        };
        string? bound = "<p>x <span class=\"formula\">z</span></p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, bound)
            .Add(component => component.ValueExpression, () => bound)
            .Add(component => component.ValueChanged, value => bound = value)
            .Add(component => component.Extensions, [extension]));

        module.Setup<string?>("replaceClosest", _ => true).SetResult(answers.Dequeue());
        await editor.Find("button[data-command=formula]").ClickAsync(new MouseEventArgs());

        var call = Assert.Single(module.Invocations["replaceClosest"]);
        Assert.Equal("span.formula", call.Arguments[1]);
        Assert.Equal("<span class=\"formula\">y</span>", call.Arguments[2]);
        Assert.Equal("<p>x <span class=\"formula\">y</span></p>", bound);

        module.Setup<string?>("replaceClosest", _ => true).SetResult(answers.Dequeue());
        await editor.Find("button[data-command=formula]").ClickAsync(new MouseEventArgs());

        Assert.Equal([true, false], found);
        Assert.Equal("<p>x <span class=\"formula\">y</span></p>", bound);
    }

    [Fact]
    public async Task SelectedText_AndInsertText_GoThroughTheSurface()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("read", _ => true).SetResult(null);
        module.Setup<string?>("selectedText", _ => true).SetResult("deux mots");
        module.Setup<string?>("exec", _ => true).SetResult("<p>DEUX MOTS</p>");
        var extension = new TestExtension
        {
            ExtraCommands = [OmniHtmlEditorCommand.Create("upper", "Majuscules", async context =>
                await context.InsertTextAsync((await context.GetSelectedTextAsync()).ToUpperInvariant()))]
        };
        string? bound = "<p>deux mots</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, bound)
            .Add(component => component.ValueExpression, () => bound)
            .Add(component => component.ValueChanged, value => bound = value)
            .Add(component => component.Extensions, [extension]));

        await editor.Find("button[data-command=upper]").ClickAsync(new MouseEventArgs());

        var exec = Assert.Single(module.Invocations["exec"]);
        Assert.Equal("inserttext", exec.Arguments[1]);
        Assert.Equal("DEUX MOTS", exec.Arguments[2]);
        Assert.Equal("<p>DEUX MOTS</p>", bound);
    }

    [Fact]
    public async Task Primitives_InTheSourceFace_ChangeNothing()
    {
        var found = true;
        string selected = "?";
        var extension = new TestExtension
        {
            ExtraCommands = [OmniHtmlEditorCommand.Create("probe", "Sonde", async context =>
            {
                found = await context.ReplaceClosestAsync("span", "<b>x</b>");
                selected = await context.GetSelectedTextAsync();
                await context.InsertTextAsync("x");
            })]
        };
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.Extensions, [extension]));

        await editor.Find("button[data-command=probe]").ClickAsync(new MouseEventArgs());

        Assert.False(found);
        Assert.Equal(string.Empty, selected);
        Assert.Equal("<p>A</p>", editor.Find("textarea").GetAttribute("value"));
    }

    private IRenderedComponent<OmniHtmlEditor> RenderEditor(OmniHtmlEditorExtension extension, IReadOnlyList<OmniHtmlEditorCommand>? commands) =>
        RenderEditor([extension], commands);

    private IRenderedComponent<OmniHtmlEditor> RenderEditor(IReadOnlyList<OmniHtmlEditorExtension> extensions, IReadOnlyList<OmniHtmlEditorCommand>? commands)
    {
        var value = "<p>A</p>";
        return Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, commands)
            .Add(component => component.Extensions, extensions));
    }

    private static OmniHtmlEditorCommand Command(string name) => OmniHtmlEditorCommand.Create(name, name, _ => Task.CompletedTask);

    private static List<string?> ToolbarNames(IRenderedComponent<OmniHtmlEditor> editor) =>
        editor.FindAll(".omni-html-editor__toolbar [data-command]").Select(element => element.GetAttribute("data-command")).ToList();

    private sealed class SuggestingExtension(Func<string, string?> propose) : OmniHtmlEditorExtension
    {
        public List<string> Asked { get; } = [];

        public override bool SuggestsText => true;

        public override Task<string?> SuggestAsync(string textBeforeCaret)
        {
            Asked.Add(textBeforeCaret);
            return Task.FromResult(propose(textBeforeCaret));
        }
    }

    private sealed class TestExtension : OmniHtmlEditorExtension
    {
        public IReadOnlyList<OmniHtmlEditorCommand> ExtraCommands { get; init; } = [];
        public Func<IReadOnlyList<OmniHtmlEditorCommand>, IReadOnlyList<OmniHtmlEditorCommand>>? Arrange { get; init; }
        public OmniHtmlSanitizerPolicy? Policy { get; init; }
        public IReadOnlyList<OmniHtmlEditorShortcut> Keys { get; init; } = [];
        public IReadOnlyList<OmniHtmlEditorInlineElement> Inline { get; init; } = [];
        public IReadOnlyList<OmniHtmlEditorCommand> Menu { get; init; } = [];
        public bool Tracks { get; init; }
        public List<OmniHtmlEditorSelection> Selections { get; } = [];

        public override IReadOnlyList<OmniHtmlEditorCommand> Commands => ExtraCommands;
        public override OmniHtmlSanitizerPolicy? SanitizerPolicy => Policy;
        public override IReadOnlyList<OmniHtmlEditorShortcut> Shortcuts => Keys;
        public override IReadOnlyList<OmniHtmlEditorInlineElement> InlineElements => Inline;
        public override IReadOnlyList<OmniHtmlEditorCommand> ContextMenu => Menu;
        public override bool TracksSelection => Tracks;

        public override IReadOnlyList<OmniHtmlEditorCommand> ArrangeToolbar(IReadOnlyList<OmniHtmlEditorCommand> toolbar) =>
            Arrange is null ? base.ArrangeToolbar(toolbar) : Arrange(toolbar);

        public override Task OnSelectionChangedAsync(OmniHtmlEditorSelection selection)
        {
            Selections.Add(selection);
            return Task.CompletedTask;
        }
    }
}
