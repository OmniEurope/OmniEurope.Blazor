namespace OmniEurope.Blazor.Tests;

/// <summary>
/// PLAN-009 lot 8: what floats over a dialog is anchored to the window. A picker panel is fixed and
/// placed by script, a dragged dialog moves without becoming the origin of the fixed surfaces it holds,
/// and the open list of a select is dressed where the browser allows it. The behaviour on screen (no
/// scrollbar in the dialog, the panel under its field once the dialog is dragged) is the pickers probe's.
/// </summary>
public sealed class FloatingSurfaceTests
{
    private const string Select = "select.omni-input:not([multiple]):not([size])";

    private static string Script(params string[] path) =>
        File.ReadAllText(Path.Combine([ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", .. path]));

    [Fact]
    public void A_picker_panel_is_fixed_to_the_window_and_waits_out_of_sight_until_placed()
    {
        var panel = ShippedLookTests.Body(".omni-calendar");
        Assert.Equal("fixed", ShippedLookTests.Value(panel, "position"));
        Assert.Equal("var(--omni-picker-x, -200vw)", ShippedLookTests.Value(panel, "left"));
        Assert.Equal("var(--omni-picker-y, 0)", ShippedLookTests.Value(panel, "top"));

        // The script writes the two custom properties on the panel, which leaves the page with them, and
        // follows the field through any scroll.
        var focus = Script("omni-focus.js");
        Assert.Contains("panel.style.setProperty('--omni-picker-x'", focus, StringComparison.Ordinal);
        Assert.Contains("panel.style.setProperty('--omni-picker-y'", focus, StringComparison.Ordinal);
        Assert.Contains("window.addEventListener('scroll', onMove, true)", focus, StringComparison.Ordinal);
        Assert.Contains("window.removeEventListener('scroll', state.onMove, true)", focus, StringComparison.Ordinal);
    }

    [Fact]
    public void A_dragged_dialog_moves_by_its_offsets_never_by_a_transform()
    {
        // A transform would make the dialog the origin of every position: fixed surface it holds.
        Assert.Equal("relative", ShippedLookTests.Value(ShippedLookTests.Body(".omni-dialog--draggable"), "position"));
        var dialog = Script("omni-dialog.js");
        Assert.Contains("dialog.style.left =", dialog, StringComparison.Ordinal);
        Assert.Contains("dialog.style.top =", dialog, StringComparison.Ordinal);
        Assert.DoesNotContain("style.transform", dialog, StringComparison.Ordinal);

        // Positioned, the dialog holds its hidden focus sentinels: pinned to its corner, they add no scroll.
        var sentinel = ShippedLookTests.Body(".omni-dialog > [data-focus-sentinel]");
        Assert.Equal("0", ShippedLookTests.Value(sentinel, "inset-block-start"));
        Assert.Equal("0", ShippedLookTests.Value(sentinel, "inset-inline-start"));
    }

    [Fact]
    public void The_open_list_of_a_select_is_dressed_only_where_the_browser_allows_it()
    {
        var css = ShippedLookTests.Css;
        var start = css.IndexOf("@supports (appearance: base-select) {", StringComparison.Ordinal);
        Assert.True(start >= 0, "No @supports (appearance: base-select) block.");

        // Every rule of the dressed list sits inside the block: a browser without the feature keeps
        // the list the system draws, whole, instead of a half-styled one.
        Assert.DoesNotContain("::picker(select)", css[..start], StringComparison.Ordinal);
        var block = css[start..];
        Assert.Contains($"{Select},", block, StringComparison.Ordinal);
        Assert.Contains($"{Select}::picker(select) {{ appearance: base-select; }}", block, StringComparison.Ordinal);
        Assert.Contains("background: var(--omni-overlay-background, var(--omni-color-surface));", block, StringComparison.Ordinal);
        Assert.Contains($"{Select} option:checked {{ background: var(--omni-color-accent-subtle);", block, StringComparison.Ordinal);
    }
}
