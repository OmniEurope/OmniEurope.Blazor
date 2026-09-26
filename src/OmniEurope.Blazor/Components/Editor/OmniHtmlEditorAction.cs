namespace OmniEurope.Blazor.Components;

/// <summary>What a toolbar command of <see cref="OmniHtmlEditor"/> does.</summary>
/// <remarks>
/// Every value except <see cref="Custom"/> and <see cref="Separator"/> is implemented by the editor
/// itself, in both modes where it has a meaning: in <see cref="OmniHtmlEditorMode.Source"/> the
/// formatting actions wrap the selected text in the matching tags, and <see cref="Unlink"/> and
/// <see cref="ClearFormatting"/>, which have no textual equivalent, are disabled.
/// </remarks>
public enum OmniHtmlEditorAction
{
    /// <summary>Runs <see cref="OmniHtmlEditorCommand.Execute"/>.</summary>
    Custom,

    /// <summary>A visual separator between two groups of commands.</summary>
    Separator,

    Bold,
    Italic,
    Underline,
    Strikethrough,
    Subscript,
    Superscript,

    /// <summary>Inline code (<c>code</c>) around the selection.</summary>
    InlineCode,

    /// <summary>A list that turns the current block into a paragraph or a heading of level 1 to 4.</summary>
    BlockFormat,

    /// <summary>A list of four text sizes, carried by classes rather than inline styles.</summary>
    FontSize,

    BulletList,
    NumberedList,
    Indent,
    Outdent,
    Quote,
    CodeBlock,

    /// <summary>Opens the link field of the editor, then links the selection to the given address.</summary>
    Link,

    Unlink,
    AlignLeft,
    AlignCenter,
    AlignRight,
    AlignJustify,

    /// <summary>Inserts a table of three rows and three columns at the caret.</summary>
    InsertTable,

    /// <summary>Removes inline formatting, sizes and alignment from the selection.</summary>
    ClearFormatting,

    Undo,
    Redo,

    /// <summary>Switches between <see cref="OmniHtmlEditorMode.Visual"/> and <see cref="OmniHtmlEditorMode.Source"/>.</summary>
    ToggleSource,

    // The actions below act on the visual face only; in the source face they are disabled.

    /// <summary>Cuts the selection to the clipboard.</summary>
    Cut,

    /// <summary>Copies the selection to the clipboard.</summary>
    Copy,

    /// <summary>
    /// Pastes the clipboard at the caret, through the same sanitiser as Ctrl+V. The browser may ask
    /// the user for permission to read the clipboard; a refusal pastes nothing.
    /// </summary>
    Paste,

    /// <summary>Splits the block at the caret into two paragraphs, as Enter does.</summary>
    InsertParagraph,

    /// <summary>Inserts a row above the row of the cell at the caret. Enabled only in a table cell, as every table action.</summary>
    AddRowAbove,

    /// <summary>Inserts a row below the row of the cell at the caret.</summary>
    AddRowBelow,

    /// <summary>Deletes the row of the cell at the caret; the last row takes the table with it.</summary>
    DeleteRow,

    /// <summary>Inserts a column before the column of the cell at the caret.</summary>
    AddColumnBefore,

    /// <summary>Inserts a column after the column of the cell at the caret.</summary>
    AddColumnAfter,

    /// <summary>Deletes the column of the cell at the caret; the last column takes the table with it.</summary>
    DeleteColumn,

    /// <summary>Merges the cell at the caret with the cell to its right, when both span the same rows.</summary>
    MergeCellRight,

    /// <summary>Merges the cell at the caret with the cell below it, when both span the same columns.</summary>
    MergeCellDown,

    /// <summary>Splits a merged cell at the caret back into single cells.</summary>
    SplitCell,

    /// <summary>
    /// Gives the cell at the caret the row and column spans of the argument, <c>rowsxcolumns</c>
    /// (<c>2x3</c>), absorbing the cells it then covers; without argument, splits it to <c>1x1</c>.
    /// Nothing changes when a covered cell reaches outside the new area.
    /// </summary>
    SetCellSpan
}
