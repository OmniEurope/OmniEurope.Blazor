using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Every key of the spreadsheet from the cell B2 of a four by three sheet: the moves of the grid with and
/// without Ctrl, the keys that neither move nor edit, the arrows that commit a typed entry but move the
/// caret of an edit, Shift with Enter and Tab, and the keys of the formula bar.
/// </summary>
public sealed class SpreadsheetKeyboardTests : OmniBunitContext
{
    private IRenderedComponent<SpreadsheetTestHost> RenderAtB2()
    {
        var host = Render<SpreadsheetTestHost>();
        host.Find("#sheet-r1-c1").Click();
        return host;
    }

    private static void Press(IRenderedComponent<SpreadsheetTestHost> host, string selector, string key, bool shift = false, bool control = false, bool alt = false, bool meta = false) =>
        host.Find(selector).KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift, CtrlKey = control, AltKey = alt, MetaKey = meta });

    private static string Address(IRenderedComponent<SpreadsheetTestHost> host) => host.Find(".omni-spreadsheet__address").TextContent;

    [Theory]
    [InlineData("ArrowUp", false, "B1")]
    [InlineData("ArrowUp", true, "B1")]
    [InlineData("ArrowLeft", false, "A2")]
    [InlineData("ArrowLeft", true, "A2")]
    [InlineData("ArrowRight", false, "C2")]
    [InlineData("ArrowRight", true, "C2")]
    [InlineData("ArrowDown", false, "B3")]
    [InlineData("Home", false, "A2")]
    [InlineData("Home", true, "A1")]
    [InlineData("End", false, "C2")]
    [InlineData("PageUp", false, "B1")]
    [InlineData("PageDown", false, "B4")]
    public void GridKey_MovesFromB2(string key, bool control, string expected)
    {
        var host = RenderAtB2();

        Press(host, ".omni-spreadsheet__viewport", key, control: control);

        Assert.Equal(expected, Address(host));
    }

    [Theory]
    [InlineData("ArrowDown", false, true, false)]
    [InlineData("ArrowDown", false, false, true)]
    [InlineData("Enter", true, false, false)]
    [InlineData("Delete", true, false, false)]
    [InlineData("Backspace", true, false, false)]
    [InlineData("v", true, false, false)]
    [InlineData("Shift", false, false, false)]
    public void GridKey_ThatNeitherMovesNorEdits_LeavesTheSheetAlone(string key, bool control, bool alt, bool meta)
    {
        var host = RenderAtB2();
        var original = host.Instance.Data;

        Press(host, ".omni-spreadsheet__viewport", key, control: control, alt: alt, meta: meta);

        Assert.Equal("B2", Address(host));
        Assert.Same(original, host.Instance.Data);
        Assert.Empty(host.FindAll(".omni-spreadsheet__editor"));
    }

    [Theory]
    [InlineData("ArrowUp", "B1")]
    [InlineData("ArrowDown", "B3")]
    [InlineData("ArrowLeft", "A2")]
    [InlineData("ArrowRight", "C2")]
    public void ArrowOnATypedEntry_CommitsAndMoves(string key, string expected)
    {
        var host = RenderAtB2();
        Press(host, ".omni-spreadsheet__viewport", "7");

        Press(host, ".omni-spreadsheet__editor", key);

        Assert.Equal("7", host.Instance.Data.GetInput("B2"));
        Assert.Equal(expected, Address(host));
        Assert.Empty(host.FindAll(".omni-spreadsheet__editor"));
    }

    [Theory]
    [InlineData("ArrowDown")]
    [InlineData("Home")]
    [InlineData("a")]
    public void KeyInAnEdit_MovesTheCaretOnly(string key)
    {
        var host = RenderAtB2();
        var original = host.Instance.Data;
        Press(host, ".omni-spreadsheet__viewport", "F2");

        Press(host, ".omni-spreadsheet__editor", key);

        Assert.Same(original, host.Instance.Data);
        Assert.Equal("B2", Address(host));
        Assert.Equal("edit", host.Find(".omni-spreadsheet__editor").GetAttribute("data-omni-sheet-mode"));
    }

    [Theory]
    [InlineData("Enter", "B1")]
    [InlineData("Tab", "A2")]
    public void ShiftWithEnterOrTab_CommitsAndGoesBack(string key, string expected)
    {
        var host = RenderAtB2();
        Press(host, ".omni-spreadsheet__viewport", "F2");
        host.Find(".omni-spreadsheet__editor").Input("12");

        Press(host, ".omni-spreadsheet__editor", key, shift: true);

        Assert.Equal("12", host.Instance.Data.GetInput("B2"));
        Assert.Equal(expected, Address(host));
    }

    [Fact]
    public void FormulaBar_ShiftEnterGoesUp_EscapeCancels_AndOtherKeysWait()
    {
        var host = RenderAtB2();
        var bar = host.Find(".omni-spreadsheet__formula-input");
        bar.Focus();
        host.Find(".omni-spreadsheet__formula-input").Input("5");

        Press(host, ".omni-spreadsheet__formula-input", "a");
        Assert.Equal("5", host.Find("#sheet-r1-c1").TextContent.Trim());
        Press(host, ".omni-spreadsheet__formula-input", "Escape");
        Assert.Equal("800", host.Find("#sheet-r1-c1").TextContent.Trim());

        host.Find(".omni-spreadsheet__formula-input").Focus();
        host.Find(".omni-spreadsheet__formula-input").Input("6");
        Press(host, ".omni-spreadsheet__formula-input", "Enter", shift: true);
        Assert.Equal("6", host.Instance.Data.GetInput("B2"));
        Assert.Equal("B1", Address(host));
    }

    [Fact]
    public void FormulaBarKey_WithoutAnEditInTheBar_DoesNothing()
    {
        var host = RenderAtB2();
        var original = host.Instance.Data;

        Press(host, ".omni-spreadsheet__formula-input", "Enter");

        Assert.Same(original, host.Instance.Data);
        Assert.Equal("B2", Address(host));
    }
}
