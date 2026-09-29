namespace OmniEurope.Blazor.Components;

/// <summary>The built-in commands of <see cref="OmniHtmlEditor"/> and the toolbars made of them.</summary>
public static class OmniHtmlEditorCommands
{
    /// <summary>The built-in <see cref="OmniHtmlEditorAction.BlockFormat"/> command, named <c>block-format</c>.</summary>
    public static OmniHtmlEditorCommand BlockFormat { get; } = new("block-format", OmniHtmlEditorAction.BlockFormat);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.FontSize"/> command, named <c>font-size</c>.</summary>
    public static OmniHtmlEditorCommand FontSize { get; } = new("font-size", OmniHtmlEditorAction.FontSize);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Bold"/> command, named <c>bold</c>.</summary>
    public static OmniHtmlEditorCommand Bold { get; } = new("bold", OmniHtmlEditorAction.Bold);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Italic"/> command, named <c>italic</c>.</summary>
    public static OmniHtmlEditorCommand Italic { get; } = new("italic", OmniHtmlEditorAction.Italic);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Underline"/> command, named <c>underline</c>.</summary>
    public static OmniHtmlEditorCommand Underline { get; } = new("underline", OmniHtmlEditorAction.Underline);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Strikethrough"/> command, named <c>strikethrough</c>.</summary>
    public static OmniHtmlEditorCommand Strikethrough { get; } = new("strikethrough", OmniHtmlEditorAction.Strikethrough);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Subscript"/> command, named <c>sub</c>.</summary>
    public static OmniHtmlEditorCommand Subscript { get; } = new("sub", OmniHtmlEditorAction.Subscript);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Superscript"/> command, named <c>sup</c>.</summary>
    public static OmniHtmlEditorCommand Superscript { get; } = new("sup", OmniHtmlEditorAction.Superscript);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.InlineCode"/> command, named <c>inline-code</c>.</summary>
    public static OmniHtmlEditorCommand InlineCode { get; } = new("inline-code", OmniHtmlEditorAction.InlineCode);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.BulletList"/> command, named <c>bullet-list</c>.</summary>
    public static OmniHtmlEditorCommand BulletList { get; } = new("bullet-list", OmniHtmlEditorAction.BulletList);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.NumberedList"/> command, named <c>numbered-list</c>.</summary>
    public static OmniHtmlEditorCommand NumberedList { get; } = new("numbered-list", OmniHtmlEditorAction.NumberedList);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Indent"/> command, named <c>indent</c>.</summary>
    public static OmniHtmlEditorCommand Indent { get; } = new("indent", OmniHtmlEditorAction.Indent);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Outdent"/> command, named <c>outdent</c>.</summary>
    public static OmniHtmlEditorCommand Outdent { get; } = new("outdent", OmniHtmlEditorAction.Outdent);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Quote"/> command, named <c>quote</c>.</summary>
    public static OmniHtmlEditorCommand Quote { get; } = new("quote", OmniHtmlEditorAction.Quote);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.CodeBlock"/> command, named <c>code-block</c>.</summary>
    public static OmniHtmlEditorCommand CodeBlock { get; } = new("code-block", OmniHtmlEditorAction.CodeBlock);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Link"/> command, named <c>link</c>.</summary>
    public static OmniHtmlEditorCommand Link { get; } = new("link", OmniHtmlEditorAction.Link);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Unlink"/> command, named <c>unlink</c>.</summary>
    public static OmniHtmlEditorCommand Unlink { get; } = new("unlink", OmniHtmlEditorAction.Unlink);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.AlignLeft"/> command, named <c>align-left</c>.</summary>
    public static OmniHtmlEditorCommand AlignLeft { get; } = new("align-left", OmniHtmlEditorAction.AlignLeft);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.AlignCenter"/> command, named <c>align-center</c>.</summary>
    public static OmniHtmlEditorCommand AlignCenter { get; } = new("align-center", OmniHtmlEditorAction.AlignCenter);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.AlignRight"/> command, named <c>align-right</c>.</summary>
    public static OmniHtmlEditorCommand AlignRight { get; } = new("align-right", OmniHtmlEditorAction.AlignRight);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.AlignJustify"/> command, named <c>align-justify</c>.</summary>
    public static OmniHtmlEditorCommand AlignJustify { get; } = new("align-justify", OmniHtmlEditorAction.AlignJustify);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.InsertTable"/> command, named <c>insert-table</c>.</summary>
    public static OmniHtmlEditorCommand InsertTable { get; } = new("insert-table", OmniHtmlEditorAction.InsertTable);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.ClearFormatting"/> command, named <c>clear-formatting</c>.</summary>
    public static OmniHtmlEditorCommand ClearFormatting { get; } = new("clear-formatting", OmniHtmlEditorAction.ClearFormatting);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Undo"/> command, named <c>undo</c>.</summary>
    public static OmniHtmlEditorCommand Undo { get; } = new("undo", OmniHtmlEditorAction.Undo);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Redo"/> command, named <c>redo</c>.</summary>
    public static OmniHtmlEditorCommand Redo { get; } = new("redo", OmniHtmlEditorAction.Redo);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.ToggleSource"/> command, named <c>toggle-source</c>.</summary>
    public static OmniHtmlEditorCommand ToggleSource { get; } = new("toggle-source", OmniHtmlEditorAction.ToggleSource);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Cut"/> command, named <c>cut</c>.</summary>
    public static OmniHtmlEditorCommand Cut { get; } = new("cut", OmniHtmlEditorAction.Cut);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Copy"/> command, named <c>copy</c>.</summary>
    public static OmniHtmlEditorCommand Copy { get; } = new("copy", OmniHtmlEditorAction.Copy);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Paste"/> command, named <c>paste</c>.</summary>
    public static OmniHtmlEditorCommand Paste { get; } = new("paste", OmniHtmlEditorAction.Paste);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.InsertParagraph"/> command, named <c>insert-paragraph</c>.</summary>
    public static OmniHtmlEditorCommand InsertParagraph { get; } = new("insert-paragraph", OmniHtmlEditorAction.InsertParagraph);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.AddRowAbove"/> command, named <c>add-row-above</c>.</summary>
    public static OmniHtmlEditorCommand AddRowAbove { get; } = new("add-row-above", OmniHtmlEditorAction.AddRowAbove);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.AddRowBelow"/> command, named <c>add-row-below</c>.</summary>
    public static OmniHtmlEditorCommand AddRowBelow { get; } = new("add-row-below", OmniHtmlEditorAction.AddRowBelow);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.DeleteRow"/> command, named <c>delete-row</c>.</summary>
    public static OmniHtmlEditorCommand DeleteRow { get; } = new("delete-row", OmniHtmlEditorAction.DeleteRow);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.AddColumnBefore"/> command, named <c>add-column-before</c>.</summary>
    public static OmniHtmlEditorCommand AddColumnBefore { get; } = new("add-column-before", OmniHtmlEditorAction.AddColumnBefore);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.AddColumnAfter"/> command, named <c>add-column-after</c>.</summary>
    public static OmniHtmlEditorCommand AddColumnAfter { get; } = new("add-column-after", OmniHtmlEditorAction.AddColumnAfter);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.DeleteColumn"/> command, named <c>delete-column</c>.</summary>
    public static OmniHtmlEditorCommand DeleteColumn { get; } = new("delete-column", OmniHtmlEditorAction.DeleteColumn);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.MergeCellRight"/> command, named <c>merge-cell-right</c>.</summary>
    public static OmniHtmlEditorCommand MergeCellRight { get; } = new("merge-cell-right", OmniHtmlEditorAction.MergeCellRight);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.MergeCellDown"/> command, named <c>merge-cell-down</c>.</summary>
    public static OmniHtmlEditorCommand MergeCellDown { get; } = new("merge-cell-down", OmniHtmlEditorAction.MergeCellDown);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.SplitCell"/> command, named <c>split-cell</c>.</summary>
    public static OmniHtmlEditorCommand SplitCell { get; } = new("split-cell", OmniHtmlEditorAction.SplitCell);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.ChangeCase"/> command, named <c>change-case</c>.</summary>
    public static OmniHtmlEditorCommand ChangeCase { get; } = new("change-case", OmniHtmlEditorAction.ChangeCase);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.InsertSpecialCharacter"/> command, named <c>insert-special-character</c>.</summary>
    public static OmniHtmlEditorCommand InsertSpecialCharacter { get; } = new("insert-special-character", OmniHtmlEditorAction.InsertSpecialCharacter);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.ImportTable"/> command, named <c>import-table</c>.</summary>
    public static OmniHtmlEditorCommand ImportTable { get; } = new("import-table", OmniHtmlEditorAction.ImportTable);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.ShowBlocks"/> command, named <c>show-blocks</c>.</summary>
    public static OmniHtmlEditorCommand ShowBlocks { get; } = new("show-blocks", OmniHtmlEditorAction.ShowBlocks);

    /// <summary>The built-in <see cref="OmniHtmlEditorAction.Highlight"/> command, named <c>highlight</c>.</summary>
    public static OmniHtmlEditorCommand Highlight { get; } = new("highlight", OmniHtmlEditorAction.Highlight);

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
    /// The toolbar of a light word processor (an <see cref="OmniHtmlEditor"/> with <c>Sheet</c> and
    /// <c>ShowStatusBar</c>): styles and sizes, inline formatting, lists, alignment, a table, clear
    /// formatting and history.
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
