using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The small parts of the editors read on their own: the format state and selection the surface script
/// sends, the delimited table reader, sanitizer policies merged, extension sets, the toolbar's rules,
/// the defaults of an extension and of a proofreader, the character palette and the secret code block.
/// </summary>
public sealed class EditorHelpersTests : OmniBunitContext
{
    [Theory]
    [InlineData(null)]
    [InlineData("b|h1|left")]
    public void FormatState_ThatIsNotFourParts_IsEmpty(string? state) =>
        Assert.Same(HtmlEditorFormatState.Empty, HtmlEditorFormatState.Parse(state));

    [Fact]
    public void FormatState_EmptyParts_TakeTheDefaults()
    {
        var state = HtmlEditorFormatState.Parse("b i|||");

        Assert.Equal(["b", "i"], state.Marks.Order(StringComparer.Ordinal));
        Assert.Equal(("p", "left", "normal"), (state.Block, state.Align, state.Size));
    }

    [Fact]
    public void Selection_ReadsClassesAndSpansOnlyWhenWellFormed()
    {
        var selection = OmniHtmlEditorSelection.Parse("""
            {"collapsed":false,"ancestors":[
              {"tag":"td","classes":"plain","colspan":"2","rowspan":0},
              {"tag":"td","classes":["a",1],"colspan":3,"rowspan":2},
              {"tag":"td","colspan":1e12}
            ]}
            """);

        Assert.Empty(selection.Ancestors[0].CssClasses);
        Assert.Equal((1, 1), (selection.Ancestors[0].ColumnSpan, selection.Ancestors[0].RowSpan));
        Assert.Equal(["a"], selection.Ancestors[1].CssClasses);
        Assert.Equal((3, 2), (selection.Ancestors[1].ColumnSpan, selection.Ancestors[1].RowSpan));
        Assert.Equal(1, selection.Ancestors[2].ColumnSpan);
    }

    [Fact]
    public void TableFile_ReadsBothLineEnds_AndShapesNothingIntoNothing()
    {
        var rows = HtmlEditorTableFile.ReadDelimited("a,b\r\nc,d\re,f\r", ',');

        Assert.Equal([["a", "b"], ["c", "d"], ["e", "f"]], rows.Select(row => row.ToArray()));
        Assert.Empty(HtmlEditorTableFile.Shape([[" "], []]));
        Assert.Equal("<table><tbody></tbody></table>", HtmlEditorTableFile.ToHtml([], header: true));
    }

    [Fact]
    public void SanitizerPolicies_MergedKeepWhatTheSecondAllows()
    {
        var open = new OmniHtmlSanitizerPolicy { AllowAnyClass = true, AllowDataAttributes = true, AllowImageDataUris = true };
        var merged = OmniHtmlSanitizerPolicy.Merge(new OmniHtmlSanitizerPolicy(), open)!;
        var reversed = OmniHtmlSanitizerPolicy.Merge(open, new OmniHtmlSanitizerPolicy())!;

        Assert.True(merged.AllowAnyClass && merged.AllowDataAttributes && merged.AllowImageDataUris);
        Assert.True(reversed.AllowAnyClass && reversed.AllowDataAttributes && reversed.AllowImageDataUris);
    }

    [Fact]
    public void ExtensionSet_OfNoExtension_IsTheEmptySet()
    {
        Assert.Same(HtmlEditorExtensionSet.For(null), HtmlEditorExtensionSet.For([]));
    }

    [Theory]
    [InlineData(OmniHtmlEditorAction.Bold, false, false)]
    [InlineData(OmniHtmlEditorAction.Bold, true, true)]
    [InlineData(OmniHtmlEditorAction.Separator, true, false)]
    [InlineData(OmniHtmlEditorAction.BlockFormat, true, false)]
    [InlineData(OmniHtmlEditorAction.FontSize, true, false)]
    [InlineData(OmniHtmlEditorAction.ChangeCase, true, false)]
    public void Toolbar_PutsInItsMenuOnlyPlacedCommandsThatAreNotLists(OmniHtmlEditorAction action, bool overflow, bool inMenu) =>
        Assert.Equal(inMenu, HtmlEditorToolbar.InMenu(new OmniHtmlEditorCommand("c", action) { Overflow = overflow }));

    [Theory]
    [InlineData(OmniHtmlEditorAction.SetCellSpan, "HtmlEditorSetCellSpan")]
    [InlineData(OmniHtmlEditorAction.Highlight, "HtmlEditorHighlight")]
    [InlineData(OmniHtmlEditorAction.Custom, "HtmlEditorCustomCommand")]
    public void Toolbar_NamesEveryAction(OmniHtmlEditorAction action, string key) =>
        Assert.Equal(key, HtmlEditorToolbar.LabelKey(action));

    [Fact]
    public void Toolbar_DrawsTheHighlighter_AndATableActionWaitsForATable()
    {
        Assert.Equal(OmniIconName.Highlighter, HtmlEditorToolbar.IconOf(new OmniHtmlEditorCommand("h", OmniHtmlEditorAction.Highlight)));
        var addRow = new OmniHtmlEditorCommand("r", OmniHtmlEditorAction.AddRowAbove);
        var inTable = HtmlEditorFormatState.Parse("intable|p|left|normal");

        Assert.True(HtmlEditorToolbar.IsDisabled(addRow, false, OmniHtmlEditorMode.Visual, null, HtmlEditorFormatState.Empty, true, true));
        Assert.False(HtmlEditorToolbar.IsDisabled(addRow, false, OmniHtmlEditorMode.Visual, null, inTable, true, true));
        Assert.True(HtmlEditorToolbar.IsDisabled(addRow, false, OmniHtmlEditorMode.Source, null, inTable, true, true));
        // A command's own rule reads no caret in the source face.
        OmniHtmlEditorSelection? read = OmniHtmlEditorSelection.Parse(null);
        var stamp = new OmniHtmlEditorCommand("s", OmniHtmlEditorAction.Custom) { Enabled = caret => (read = caret) is null };
        Assert.False(HtmlEditorToolbar.IsDisabled(stamp, false, OmniHtmlEditorMode.Source, OmniHtmlEditorSelection.Parse(null), inTable, true, true));
        Assert.Null(read);
    }

    private sealed class PlainExtension : OmniHtmlEditorExtension;

    private sealed class PlainProofreader : OmniHtmlEditorProofreader
    {
        public override Task<IReadOnlyList<OmniHtmlEditorProofreadingIssue>> CheckAsync(
            IReadOnlyList<OmniHtmlEditorProofreadingText> texts, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OmniHtmlEditorProofreadingIssue>>([]);
    }

    [Fact]
    public async Task ExtensionAndProofreaderDefaults_DoNothingAndOfferNothing()
    {
        var extension = new PlainExtension();
        var proofreader = new PlainProofreader();

        await extension.OnSelectionChangedAsync(OmniHtmlEditorSelection.Parse(null));
        Assert.Null(await extension.SuggestAsync("Bonj"));
        Assert.Empty(await proofreader.SuggestAsync(null!, null!, Xunit.TestContext.Current.CancellationToken));
        Assert.False(proofreader.CanIgnoreAll);
        Assert.False(proofreader.CanAddToDictionary);
        await proofreader.IgnoreAllAsync("teh", "en");
        await proofreader.AddToDictionaryAsync("teh", "en");
    }

    [Fact]
    public void CharacterPalette_OffersTheCaseToggleOnlyForLettersThatHaveOne()
    {
        var symbols = Render<OmniCharacterPalette>(parameters => parameters.Add(component => component.Characters, ["", "€", "→"]));
        var letters = Render<OmniCharacterPalette>(parameters => parameters.Add(component => component.Characters, ["", "é"]));

        Assert.Empty(symbols.FindAll(".omni-character-palette__case"));
        Assert.Single(letters.FindAll(".omni-character-palette__case"));
        Assert.Equal(2, symbols.FindAll(".omni-character-palette__character").Count);
    }

    [Theory]
    [InlineData("", 4, "")]
    [InlineData("abcdefghij", 0, "••••••••")]
    [InlineData("abcdefghij", 4, "••••••••")]
    [InlineData("abcdefghijkl", 4, "abcd••••••••ijkl")]
    public void CodeBlock_Mask_KeepsBothEndsOnlyWhenTheyStayApart(string value, int visible, string expected) =>
        Assert.Equal(expected, OmniCodeBlock.Mask(value, visible));

    [Fact]
    public void SecretCodeBlock_RevealsAndHidesItsValue_WithoutACopyButton()
    {
        var block = Render<OmniCodeBlock>(parameters => parameters
            .Add(component => component.Code, "sk-abcdefghijkl")
            .Add(component => component.Secret, true)
            .Add(component => component.ShowCopy, false));
        Assert.Empty(block.FindAll(".omni-code-block__copy"));
        Assert.Contains("omni-code-block__masked", block.Find("code").ClassList);

        block.Find(".omni-code-block__reveal").Click();
        Assert.True(block.Instance.IsRevealed);
        Assert.Equal("sk-abcdefghijkl", block.Find("code").TextContent);
        Assert.Equal("true", block.Find(".omni-code-block__reveal").GetAttribute("aria-pressed"));

        block.Find(".omni-code-block__reveal").Click();
        Assert.False(block.Instance.IsRevealed);
    }

    [Fact]
    public async Task CodeBlock_CopiedTwice_KeepsOneClipboard()
    {
        var module = JSInterop.SetupModule(OmniModules.Interop);
        module.Setup<bool>("copyText", _ => true).SetResult(true);
        var block = Render<OmniCodeBlock>(parameters => parameters.Add(component => component.Code, "dotnet test"));

        Assert.True(await block.InvokeAsync(block.Instance.CopyAsync));
        Assert.True(await block.InvokeAsync(block.Instance.CopyAsync));

        Assert.Equal(2, module.Invocations["copyText"].Count);
    }

    [Fact]
    public void CodeBlock_RefusesANegativeVisibleCount() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniCodeBlock>(parameters => parameters
            .Add(component => component.Code, "x")
            .Add(component => component.VisibleCharacters, -1)));
}
