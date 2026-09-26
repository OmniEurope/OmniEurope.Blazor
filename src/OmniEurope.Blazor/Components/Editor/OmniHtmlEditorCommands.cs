namespace OmniEurope.Blazor.Components;

/// <summary>The built-in commands of <see cref="OmniHtmlEditor"/> and the toolbars made of them.</summary>
public static class OmniHtmlEditorCommands
{
    public static OmniHtmlEditorCommand BlockFormat { get; } = new("block-format", OmniHtmlEditorAction.BlockFormat);
    public static OmniHtmlEditorCommand FontSize { get; } = new("font-size", OmniHtmlEditorAction.FontSize);
    public static OmniHtmlEditorCommand Bold { get; } = new("bold", OmniHtmlEditorAction.Bold);
    public static OmniHtmlEditorCommand Italic { get; } = new("italic", OmniHtmlEditorAction.Italic);
    public static OmniHtmlEditorCommand Underline { get; } = new("underline", OmniHtmlEditorAction.Underline);
    public static OmniHtmlEditorCommand Strikethrough { get; } = new("strikethrough", OmniHtmlEditorAction.Strikethrough);
    public static OmniHtmlEditorCommand Subscript { get; } = new("sub", OmniHtmlEditorAction.Subscript);
    public static OmniHtmlEditorCommand Superscript { get; } = new("sup", OmniHtmlEditorAction.Superscript);
    public static OmniHtmlEditorCommand InlineCode { get; } = new("inline-code", OmniHtmlEditorAction.InlineCode);
    public static OmniHtmlEditorCommand BulletList { get; } = new("bullet-list", OmniHtmlEditorAction.BulletList);
    public static OmniHtmlEditorCommand NumberedList { get; } = new("numbered-list", OmniHtmlEditorAction.NumberedList);
    public static OmniHtmlEditorCommand Indent { get; } = new("indent", OmniHtmlEditorAction.Indent);
    public static OmniHtmlEditorCommand Outdent { get; } = new("outdent", OmniHtmlEditorAction.Outdent);
    public static OmniHtmlEditorCommand Quote { get; } = new("quote", OmniHtmlEditorAction.Quote);
    public static OmniHtmlEditorCommand CodeBlock { get; } = new("code-block", OmniHtmlEditorAction.CodeBlock);
    public static OmniHtmlEditorCommand Link { get; } = new("link", OmniHtmlEditorAction.Link);
    public static OmniHtmlEditorCommand Unlink { get; } = new("unlink", OmniHtmlEditorAction.Unlink);
    public static OmniHtmlEditorCommand AlignLeft { get; } = new("align-left", OmniHtmlEditorAction.AlignLeft);
    public static OmniHtmlEditorCommand AlignCenter { get; } = new("align-center", OmniHtmlEditorAction.AlignCenter);
    public static OmniHtmlEditorCommand AlignRight { get; } = new("align-right", OmniHtmlEditorAction.AlignRight);
    public static OmniHtmlEditorCommand AlignJustify { get; } = new("align-justify", OmniHtmlEditorAction.AlignJustify);
    public static OmniHtmlEditorCommand InsertTable { get; } = new("insert-table", OmniHtmlEditorAction.InsertTable);
    public static OmniHtmlEditorCommand ClearFormatting { get; } = new("clear-formatting", OmniHtmlEditorAction.ClearFormatting);
    public static OmniHtmlEditorCommand Undo { get; } = new("undo", OmniHtmlEditorAction.Undo);
    public static OmniHtmlEditorCommand Redo { get; } = new("redo", OmniHtmlEditorAction.Redo);
    public static OmniHtmlEditorCommand ToggleSource { get; } = new("toggle-source", OmniHtmlEditorAction.ToggleSource);

    public static OmniHtmlEditorCommand Cut { get; } = new("cut", OmniHtmlEditorAction.Cut);
    public static OmniHtmlEditorCommand Copy { get; } = new("copy", OmniHtmlEditorAction.Copy);
    public static OmniHtmlEditorCommand Paste { get; } = new("paste", OmniHtmlEditorAction.Paste);
    public static OmniHtmlEditorCommand InsertParagraph { get; } = new("insert-paragraph", OmniHtmlEditorAction.InsertParagraph);
    public static OmniHtmlEditorCommand AddRowAbove { get; } = new("add-row-above", OmniHtmlEditorAction.AddRowAbove);
    public static OmniHtmlEditorCommand AddRowBelow { get; } = new("add-row-below", OmniHtmlEditorAction.AddRowBelow);
    public static OmniHtmlEditorCommand DeleteRow { get; } = new("delete-row", OmniHtmlEditorAction.DeleteRow);
    public static OmniHtmlEditorCommand AddColumnBefore { get; } = new("add-column-before", OmniHtmlEditorAction.AddColumnBefore);
    public static OmniHtmlEditorCommand AddColumnAfter { get; } = new("add-column-after", OmniHtmlEditorAction.AddColumnAfter);
    public static OmniHtmlEditorCommand DeleteColumn { get; } = new("delete-column", OmniHtmlEditorAction.DeleteColumn);
    public static OmniHtmlEditorCommand MergeCellRight { get; } = new("merge-cell-right", OmniHtmlEditorAction.MergeCellRight);
    public static OmniHtmlEditorCommand MergeCellDown { get; } = new("merge-cell-down", OmniHtmlEditorAction.MergeCellDown);
    public static OmniHtmlEditorCommand SplitCell { get; } = new("split-cell", OmniHtmlEditorAction.SplitCell);

    /// <summary>A separator. Separators carry no state, so the same instance can appear several times.</summary>
    public static OmniHtmlEditorCommand Separator { get; } = new("separator", OmniHtmlEditorAction.Separator);

    /// <summary>
    /// The toolbar of <see cref="OmniHtmlEditor"/> when <see cref="OmniHtmlEditor.Commands"/> is not set:
    /// block format, inline formatting, lists, quote and code, link, alignment, clear formatting, history
    /// and the source switch.
    /// </summary>
    public static IReadOnlyList<OmniHtmlEditorCommand> Default { get; } =
    [
        BlockFormat, Separator,
        Bold, Italic, Underline, Strikethrough, Subscript, Superscript, InlineCode, Separator,
        BulletList, NumberedList, Outdent, Indent, Separator,
        Quote, CodeBlock, Link, Unlink, Separator,
        AlignLeft, AlignCenter, AlignRight, AlignJustify, Separator,
        ClearFormatting, Separator,
        Undo, Redo, Separator,
        ToggleSource
    ];

    /// <summary>
    /// The toolbar of <see cref="OmniDocumentEditor"/>: styles and sizes, inline formatting, lists,
    /// alignment, a table, clear formatting and history.
    /// </summary>
    public static IReadOnlyList<OmniHtmlEditorCommand> Document { get; } =
    [
        BlockFormat, FontSize, Separator,
        Bold, Italic, Underline, Strikethrough, Separator,
        BulletList, NumberedList, Outdent, Indent, Separator,
        AlignLeft, AlignCenter, AlignRight, AlignJustify, Separator,
        InsertTable, Link, ClearFormatting, Separator,
        Undo, Redo
    ];

    /// <summary>
    /// The table commands, to append to a toolbar: insert a table, add and delete rows and
    /// columns, merge and split cells. Every one but the first acts on the cell at the caret.
    /// </summary>
    public static IReadOnlyList<OmniHtmlEditorCommand> Table { get; } =
    [
        InsertTable, Separator,
        AddRowAbove, AddRowBelow, DeleteRow, Separator,
        AddColumnBefore, AddColumnAfter, DeleteColumn, Separator,
        MergeCellRight, MergeCellDown, SplitCell
    ];

    /// <summary>The clipboard commands: cut, copy and paste.</summary>
    public static IReadOnlyList<OmniHtmlEditorCommand> Clipboard { get; } = [Cut, Copy, Paste];
}
