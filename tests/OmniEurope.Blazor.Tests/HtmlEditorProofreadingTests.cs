using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniHtmlEditorExtension.Proofreader"/> (PLAN-012): the blocks the surface script sends are checked by
/// every proofreader and come back as rows the script underlines; a right-click on a flagged passage opens the menu of
/// its corrections and actions. The surface script is bUnit's module double, so these tests cover what .NET does with
/// what the script sends and what it asks of the script in return.
/// </summary>
public sealed class HtmlEditorProofreadingTests : OmniBunitContext
{
    private const string ModulePath = OmniModules.HtmlEditor;
    private const string FocusModulePath = OmniModules.Focus;

    // The passage "speling" (5, 7) of "Some speling here." in English, flagged by the first proofreader.
    private const string Flagged = "{\"p\":0,\"text\":\"Some speling here.\",\"lang\":\"en\",\"s\":5,\"l\":7,\"k\":0,\"m\":null}";

    [Fact]
    public void Proofreading_IsTurnedOnInTheScript_OnlyWhenAnExtensionBringsAProofreader()
    {
        var module = JSInterop.SetupModule(ModulePath);
        RenderEditor(new ProofreadingExtension(new TestProofreader()));
        var options = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["mount"]).Arguments[3]);
        Assert.True(options.GetProperty("proofread").GetBoolean());

        var other = JSInterop.SetupModule(ModulePath);
        RenderEditor(new ProofreadingExtension(null));
        var none = JsonSerializer.SerializeToElement(other.Invocations["mount"][^1].Arguments[3]);
        Assert.False(none.TryGetProperty("proofread", out _));
    }

    [Fact]
    public async Task TheBlocks_AreCheckedByEveryProofreader_AndComeBackAsRows()
    {
        JSInterop.SetupModule(ModulePath);
        var spelling = new TestProofreader
        {
            Check = texts =>
            [
                new(0, 5, 7),
                new(1, 0, 3, OmniHtmlEditorProofreadingKind.Grammar, "Repeated word"),
                // Outside the texts given: dropped before it reaches the script.
                new(2, 0, 1),
                new(0, 1, 0)
            ]
        };
        var failing = new TestProofreader { Check = _ => throw new InvalidOperationException("engine down") };
        var editor = RenderEditor(new ProofreadingExtension(failing), new ProofreadingExtension(spelling));

        var json = await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance)
            .OnProofreadRequested(["Some speling here.", "the the end"], ["en", null]));

        Assert.Equal("[[1,0,5,7,0,null],[1,1,0,3,1,\"Repeated word\"]]", json);
        var checkedTexts = Assert.Single(spelling.Checked);
        Assert.Equal([new OmniHtmlEditorProofreadingText("Some speling here.", "en"), new OmniHtmlEditorProofreadingText("the the end", null)], checkedTexts);
    }

    // Nothing checked is not "no issue": the script keeps no answer for these blocks and asks again later, so a
    // block is underlined once the editor is unlocked or the proofreader is back.
    [Fact]
    public async Task ALockedEditor_OrProofreadersThatAllFail_AnswerNull_SoNothingIsKeptAsClean()
    {
        JSInterop.SetupModule(ModulePath);
        var proofreader = new TestProofreader { Check = _ => [new(0, 5, 7)] };
        var value = "<p>A</p>";
        var locked = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Extensions, [new ProofreadingExtension(proofreader)]));

        Assert.Null(await locked.InvokeAsync(() => new HtmlEditorInteropBridge(locked.Instance).OnProofreadRequested(["Some speling"], [null])));
        Assert.Empty(proofreader.Checked);

        var failing = RenderEditor(new ProofreadingExtension(new TestProofreader { Check = _ => throw new HttpRequestException("offline") }));
        Assert.Null(await failing.InvokeAsync(() => new HtmlEditorInteropBridge(failing.Instance).OnProofreadRequested(["Some speling"], [null])));

        // A proofreader that answers "nothing to flag" is an answer: the blocks are kept as clean.
        var clean = RenderEditor(new ProofreadingExtension(new TestProofreader()));
        Assert.Equal("[]", await clean.InvokeAsync(() => new HtmlEditorInteropBridge(clean.Instance).OnProofreadRequested(["Fine"], [null])));
    }

    [Fact]
    public async Task ClosingTheMenu_OrRemovingTheEditor_CancelsTheCorrectionsStillAskedFor()
    {
        JSInterop.SetupModule(ModulePath);
        SetUpMenu();
        var proofreader = new TestProofreader { Pending = true };
        var editor = RenderEditor(new ProofreadingExtension(proofreader));
        var bridge = new HtmlEditorInteropBridge(editor.Instance);

        var opening = editor.InvokeAsync(() => bridge.OnContextMenu(40, 20, null, Flagged));
        var first = Assert.Single(proofreader.SuggestTokens);
        Assert.False(first.IsCancellationRequested);
        await editor.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(first.IsCancellationRequested);
        await opening;

        _ = editor.InvokeAsync(() => bridge.OnContextMenu(40, 20, null, Flagged));
        // The second opening is not awaited (its corrections never come): wait until it asked for them.
        editor.WaitForAssertion(() => Assert.Equal(2, proofreader.SuggestTokens.Count));
        var second = proofreader.SuggestTokens[1];
        Assert.False(second.IsCancellationRequested);
        await DisposeComponentsAsync();
        Assert.True(second.IsCancellationRequested);
    }

    [Fact]
    public async Task TheSourceFace_ChecksNothing()
    {
        JSInterop.SetupModule(ModulePath);
        var proofreader = new TestProofreader();
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.Extensions, [new ProofreadingExtension(proofreader)]));

        Assert.Null(await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnProofreadRequested(["x"], [null])));
        Assert.Empty(proofreader.Checked);
    }

    [Fact]
    public async Task AFlaggedPassage_OpensItsMenu_WithFiveCorrectionsAtMost_AndTheProofreadersActions()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.SetupVoid("proofreadReplace", _ => true).SetVoidResult();
        var focus = SetUpMenu();
        var proofreader = new TestProofreader
        {
            Suggest = (_, _) => ["spelling", "spieling", "", "spelling", "spewing", "speeling", "sapling", "spilling"],
            IgnoreAll = true,
            AddToDictionary = true
        };
        var editor = RenderEditor(new ProofreadingExtension(proofreader));

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnContextMenu(40, 20, null, Flagged));

        // The proofreader was asked about the passage alone, with its block and language.
        var (text, issue) = Assert.Single(proofreader.Suggested);
        Assert.Equal(new OmniHtmlEditorProofreadingText("Some speling here.", "en"), text);
        Assert.Equal(new OmniHtmlEditorProofreadingIssue(0, 5, 7), issue);
        Assert.Equal(["spelling", "spieling", "spewing", "speeling", "sapling"],
            editor.FindAll("[role=menuitem][data-proofreading=suggestion]").Select(item => item.TextContent.Trim()));
        Assert.Equal(["suggestion", "suggestion", "suggestion", "suggestion", "suggestion", "ignore", "ignore-all", "add"],
            editor.FindAll("[role=menuitem][data-proofreading]").Select(item => item.GetAttribute("data-proofreading")));
        // Placed again once the corrections came, so the grown menu stays in the window, at the same point.
        Assert.Equal(2, focus.Invocations["openMenu"].Count);
        Assert.Equal(40d, focus.Invocations["openMenu"][1].Arguments[4]);

        await editor.Find("[role=menuitem][data-proofreading=suggestion]").ClickAsync(new MouseEventArgs());

        Assert.Equal("spelling", Assert.Single(module.Invocations["proofreadReplace"]).Arguments[1]);
        Assert.Empty(editor.FindAll("[role=menu]"));
    }

    [Fact]
    public async Task IgnoreAll_AndAddToDictionary_CallTheProofreader_ThenCheckEveryBlockAgain()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.SetupVoid("proofreadRecheck", _ => true).SetVoidResult();
        SetUpMenu();
        var proofreader = new TestProofreader { IgnoreAll = true, AddToDictionary = true };
        var editor = RenderEditor(new ProofreadingExtension(proofreader));
        var bridge = new HtmlEditorInteropBridge(editor.Instance);

        await editor.InvokeAsync(() => bridge.OnContextMenu(40, 20, null, Flagged));
        await editor.Find("[role=menuitem][data-proofreading=ignore-all]").ClickAsync(new MouseEventArgs());
        await editor.InvokeAsync(() => bridge.OnContextMenu(40, 20, null, Flagged));
        await editor.Find("[role=menuitem][data-proofreading=add]").ClickAsync(new MouseEventArgs());

        Assert.Equal([("ignore", "speling", "en"), ("add", "speling", "en")], proofreader.Recorded);
        Assert.Equal(2, module.Invocations["proofreadRecheck"].Count);
    }

    [Fact]
    public async Task Ignore_LeavesThePassageToTheScript_AndAMenuWithoutCorrectionsSaysSo()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.SetupVoid("proofreadIgnore", _ => true).SetVoidResult();
        SetUpMenu();
        var proofreader = new TestProofreader();
        var editor = RenderEditor(new ProofreadingExtension(proofreader));

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnContextMenu(40, 20, null,
            Flagged.Replace("\"m\":null", "\"m\":\"Unknown word\"", StringComparison.Ordinal)));

        Assert.Equal("Unknown word", editor.Find("[role=menuitem][data-proofreading=message]").TextContent.Trim());
        Assert.True(editor.Find("[role=menuitem][data-proofreading=none]").HasAttribute("disabled"));
        // A proofreader that keeps no word list offers neither.
        Assert.Empty(editor.FindAll("[data-proofreading=ignore-all], [data-proofreading=add]"));

        await editor.Find("[role=menuitem][data-proofreading=ignore]").ClickAsync(new MouseEventArgs());

        Assert.Single(module.Invocations["proofreadIgnore"]);
        Assert.Empty(proofreader.Recorded);
    }

    [Fact]
    public async Task AMalformedPassage_OrNoProofreader_OpensNoMenu()
    {
        JSInterop.SetupModule(ModulePath);
        SetUpMenu();
        var editor = RenderEditor(new ProofreadingExtension(new TestProofreader()));
        var bridge = new HtmlEditorInteropBridge(editor.Instance);

        await editor.InvokeAsync(() => bridge.OnContextMenu(40, 20, null, "{\"p\":0,\"text\":\"abc\",\"s\":2,\"l\":5,\"k\":0}"));
        await editor.InvokeAsync(() => bridge.OnContextMenu(40, 20, null, "not json"));
        await editor.InvokeAsync(() => bridge.OnContextMenu(40, 20, null, Flagged.Replace("\"p\":0", "\"p\":4", StringComparison.Ordinal)));
        await editor.InvokeAsync(() => bridge.OnContextMenu(40, 20, null, Flagged.Replace("\"p\":0", "\"p\":-1", StringComparison.Ordinal)));
        await editor.InvokeAsync(() => bridge.OnContextMenu(40, 20, null, Flagged.Replace("\"l\":7", "\"l\":2147483647", StringComparison.Ordinal)));

        Assert.Empty(editor.FindAll("[role=menu]"));
    }

    [Fact]
    public async Task AFlaggedPassage_ComesBeforeTheExtensionsEntries()
    {
        JSInterop.SetupModule(ModulePath);
        SetUpMenu();
        var editor = RenderEditor(new ProofreadingExtension(new TestProofreader { Suggest = (_, _) => ["spelling"] })
        {
            Menu = [OmniHtmlEditorCommand.Create("edit-note", "Modifier la note", _ => Task.CompletedTask)]
        });

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnContextMenu(40, 20, null, Flagged));

        var rows = editor.FindAll("[role=menu] [role=menuitem]");
        Assert.Equal("suggestion", rows[0].GetAttribute("data-proofreading"));
        Assert.Equal("edit-note", rows[^1].GetAttribute("data-command"));
        Assert.Equal(2, editor.FindAll("[role=menu] [role=separator]").Count);
    }

    // A host with its own proofreader turns the browser's spell checker off, so a word is not underlined twice; the
    // browser's stays on by default.
    [Theory]
    [InlineData(null, "true")]
    [InlineData("false", "false")]
    public void TheBrowsersSpellChecker_IsOnByDefault_AndTheHostMayTurnItOff(string? hostValue, string expected)
    {
        JSInterop.SetupModule(ModulePath);
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters =>
        {
            parameters.Add(component => component.Value, value).Add(component => component.ValueExpression, () => value);
            if (hostValue is not null)
            {
                parameters.AddUnmatched("spellcheck", hostValue);
            }
        });

        Assert.Equal(expected, editor.Find(".omni-html-editor__surface").GetAttribute("spellcheck"));
    }

    private BunitJSModuleInterop SetUpMenu()
    {
        var focus = JSInterop.SetupModule(FocusModulePath);
        focus.SetupVoid("openMenu", _ => true).SetVoidResult();
        focus.SetupVoid("closeMenu", _ => true).SetVoidResult();
        return focus;
    }

    private IRenderedComponent<OmniHtmlEditor> RenderEditor(params OmniHtmlEditorExtension[] extensions)
    {
        var value = "<p>Some speling here.</p>";
        return Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Extensions, extensions));
    }

    private sealed class ProofreadingExtension(OmniHtmlEditorProofreader? proofreader) : OmniHtmlEditorExtension
    {
        public IReadOnlyList<OmniHtmlEditorCommand> Menu { get; init; } = [];

        public override OmniHtmlEditorProofreader? Proofreader => proofreader;

        public override IReadOnlyList<OmniHtmlEditorCommand> ContextMenu => Menu;
    }

    private sealed class TestProofreader : OmniHtmlEditorProofreader
    {
        public Func<IReadOnlyList<OmniHtmlEditorProofreadingText>, IReadOnlyList<OmniHtmlEditorProofreadingIssue>> Check { get; init; } = _ => [];

        public Func<OmniHtmlEditorProofreadingText, OmniHtmlEditorProofreadingIssue, IReadOnlyList<string>> Suggest { get; init; } = (_, _) => [];

        public bool IgnoreAll { get; init; }

        public bool AddToDictionary { get; init; }

        // The corrections never come: the menu stays loading until it is cancelled.
        public bool Pending { get; init; }

        public List<CancellationToken> SuggestTokens { get; } = [];

        public List<IReadOnlyList<OmniHtmlEditorProofreadingText>> Checked { get; } = [];

        public List<(OmniHtmlEditorProofreadingText Text, OmniHtmlEditorProofreadingIssue Issue)> Suggested { get; } = [];

        public List<(string Action, string Passage, string? Language)> Recorded { get; } = [];

        public override bool CanIgnoreAll => IgnoreAll;

        public override bool CanAddToDictionary => AddToDictionary;

        public override Task<IReadOnlyList<OmniHtmlEditorProofreadingIssue>> CheckAsync(
            IReadOnlyList<OmniHtmlEditorProofreadingText> texts, CancellationToken cancellationToken)
        {
            Checked.Add(texts);
            return Task.FromResult(Check(texts));
        }

        public override Task<IReadOnlyList<string>> SuggestAsync(
            OmniHtmlEditorProofreadingText text, OmniHtmlEditorProofreadingIssue issue, CancellationToken cancellationToken)
        {
            Suggested.Add((text, issue));
            SuggestTokens.Add(cancellationToken);
            return Pending ? Task.Delay(Timeout.Infinite, cancellationToken).ContinueWith<IReadOnlyList<string>>(_ => [], TaskScheduler.Default) : Task.FromResult(Suggest(text, issue));
        }

        public override Task IgnoreAllAsync(string passage, string? language)
        {
            Recorded.Add(("ignore", passage, language));
            return Task.CompletedTask;
        }

        public override Task AddToDictionaryAsync(string passage, string? language)
        {
            Recorded.Add(("add", passage, language));
            return Task.CompletedTask;
        }
    }
}
