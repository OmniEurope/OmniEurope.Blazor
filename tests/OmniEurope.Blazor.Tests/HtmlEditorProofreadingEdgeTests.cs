using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The proofreading menu when things go wrong: a proofreader that fails to suggest or to record, a passage the
/// script describes with nulls or odd types.
/// </summary>
public sealed class HtmlEditorProofreadingEdgeTests : OmniBunitContext
{
    private const string Flagged = "{\"p\":0,\"text\":\"Some speling here.\",\"lang\":\"en\",\"s\":5,\"l\":7,\"k\":0,\"m\":null}";

    private readonly BunitJSModuleInterop _module;

    public HtmlEditorProofreadingEdgeTests()
    {
        _module = JSInterop.SetupModule(OmniModules.HtmlEditor);
        _module.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule(OmniModules.Focus).Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<OmniHtmlEditor> RenderEditor(OmniHtmlEditorProofreader proofreader)
    {
        var value = "<p>Some speling here.</p>";
        return Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Extensions, [new ProofreadingExtension(proofreader)]));
    }

    [Fact]
    public async Task ProofreaderThatFailsToSuggest_LeavesTheMenuWithItsOtherEntries()
    {
        var editor = RenderEditor(new FailingProofreader());

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnContextMenu(40, 20, null, Flagged));

        Assert.Empty(editor.FindAll("[data-proofreading=suggestion]"));
        Assert.NotEmpty(editor.FindAll("[data-proofreading=ignore-all]"));
    }

    [Theory]
    [InlineData("ignore-all")]
    [InlineData("add")]
    public async Task ProofreaderThatFailsToRecord_LeavesThePassageUnderlined_AndChecksAgain(string action)
    {
        _module.SetupVoid("proofreadRecheck", _ => true).SetVoidResult();
        var editor = RenderEditor(new FailingProofreader());
        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnContextMenu(40, 20, null, Flagged));

        await editor.Find($"[role=menuitem][data-proofreading={action}]").ClickAsync(new MouseEventArgs());

        Assert.Single(_module.Invocations["proofreadRecheck"]);
    }

    [Fact]
    public async Task ProofreaderWithdrawnWhileItsMenuIsOpen_TakesItsEntriesAway()
    {
        var editor = RenderEditor(new FailingProofreader());
        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnContextMenu(40, 20, null, Flagged));
        Assert.NotEmpty(editor.FindAll("[role=menuitem][data-proofreading=ignore-all]"));

        editor.Render(parameters => parameters.Add(component => component.Extensions, Array.Empty<OmniHtmlEditorExtension>()));

        // The passage no longer has a proofreader: its entries go, nothing is left to record it.
        Assert.Empty(editor.FindAll("[role=menuitem][data-proofreading=ignore-all]"));
        Assert.Empty(editor.FindAll("[role=menuitem][data-proofreading=add]"));
    }

    [Fact]
    public async Task PassageWithNullTextOrOddTypes_IsReadWithoutItsLanguageAndMessage()
    {
        var editor = RenderEditor(new FailingProofreader());
        var bridge = new HtmlEditorInteropBridge(editor.Instance);

        await editor.InvokeAsync(() => bridge.OnContextMenu(40, 20, null, "{\"p\":0,\"text\":null,\"s\":0,\"l\":1,\"k\":0}"));
        Assert.Empty(editor.FindAll("[role=menu]"));

        await editor.InvokeAsync(() => bridge.OnContextMenu(40, 20, null, "{\"p\":0,\"text\":\"Some speling\",\"lang\":7,\"s\":5,\"l\":7,\"k\":0,\"m\":3}"));
        Assert.NotEmpty(editor.FindAll("[role=menu]"));
        Assert.Empty(editor.FindAll("[data-proofreading=message]"));
    }

    private sealed class ProofreadingExtension(OmniHtmlEditorProofreader proofreader) : OmniHtmlEditorExtension
    {
        public override OmniHtmlEditorProofreader? Proofreader => proofreader;
    }

    /// <summary>Flags nothing itself, and fails at everything else it is asked.</summary>
    private sealed class FailingProofreader : OmniHtmlEditorProofreader
    {
        public override bool CanIgnoreAll => true;

        public override bool CanAddToDictionary => true;

        public override Task<IReadOnlyList<OmniHtmlEditorProofreadingIssue>> CheckAsync(
            IReadOnlyList<OmniHtmlEditorProofreadingText> texts, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OmniHtmlEditorProofreadingIssue>>([]);

        public override Task<IReadOnlyList<string>> SuggestAsync(
            OmniHtmlEditorProofreadingText text, OmniHtmlEditorProofreadingIssue issue, CancellationToken cancellationToken)
            => throw new InvalidOperationException("dictionnaire absent");

        public override Task IgnoreAllAsync(string passage, string? language) => throw new IOException("lecture seule");

        public override Task AddToDictionaryAsync(string passage, string? language) => throw new IOException("lecture seule");
    }
}
