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

    /// <summary>Bold text; in the source face, <c>strong</c> around the selection.</summary>
    Bold,

    /// <summary>Italic text; in the source face, <c>em</c> around the selection.</summary>
    Italic,

    /// <summary>Underlined text (<c>u</c>).</summary>
    Underline,

    /// <summary>Struck-through text (<c>s</c>).</summary>
    Strikethrough,

    /// <summary>Subscript text (<c>sub</c>).</summary>
    Subscript,

    /// <summary>Superscript text (<c>sup</c>).</summary>
    Superscript,

    /// <summary>Inline code (<c>code</c>) around the selection.</summary>
    InlineCode,

    /// <summary>A list that turns the current block into a paragraph or a heading of level 1 to 4.</summary>
    BlockFormat,

    /// <summary>A list of four text sizes, carried by classes rather than inline styles.</summary>
    FontSize,

    /// <summary>Turns the selection into a bulleted list (<c>ul</c>).</summary>
    BulletList,

    /// <summary>Turns the selection into a numbered list (<c>ol</c>).</summary>
    NumberedList,

    /// <summary>Nests a list item one level deeper; outside a list, puts the block in a quote (<c>blockquote</c>).</summary>
    Indent,

    /// <summary>Moves a list item one level up; outside a list, takes the block out of its quote.</summary>
    Outdent,

    /// <summary>Puts the block in a quote (<c>blockquote</c>), or takes it out of the quote it is in.</summary>
    Quote,

    /// <summary>Turns the block into a code block (<c>pre</c>), or a code block back into a paragraph.</summary>
    CodeBlock,

    /// <summary>Opens the link field of the editor, then links the selection to the given address.</summary>
    Link,

    /// <summary>Removes the link at the caret, or the links of the selection, keeping their text.</summary>
    Unlink,

    /// <summary>Aligns the selected blocks to the start, removing any alignment class.</summary>
    AlignLeft,

    /// <summary>Centres the selected blocks (class <c>omni-align-center</c>).</summary>
    AlignCenter,

    /// <summary>Aligns the selected blocks to the end (a class, never an inline style).</summary>
    AlignRight,

    /// <summary>Justifies the selected blocks (a class, never an inline style).</summary>
    AlignJustify,

    /// <summary>Inserts a table of three rows and three columns at the caret.</summary>
    InsertTable,

    /// <summary>Removes inline formatting, sizes and alignment from the selection.</summary>
    ClearFormatting,

    /// <summary>Restores the value before the last change, from the editor's own history.</summary>
    Undo,

    /// <summary>Applies again the change the last <see cref="Undo"/> took back.</summary>
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
    SetCellSpan,

    /// <summary>
    /// Rewrites the selected text in capitals (<c>upper</c>), small letters (<c>lower</c>) or with a capital
    /// at the start of each word (<c>title</c>); shown as a list of the three. The text only changes case,
    /// its formatting stays.
    /// </summary>
    ChangeCase,

    /// <summary>Opens a panel of special characters by category, with a search; the chosen one is typed at the caret.</summary>
    InsertSpecialCharacter,

    /// <summary>
    /// Opens a panel that reads a table file (CSV and TSV, plus the formats of the extensions'
    /// <see cref="OmniHtmlEditorExtension.TableReaders"/>), shows a preview and inserts it at the caret,
    /// its first row as a header or not. At most 100 rows and 50 columns are kept.
    /// </summary>
    ImportTable,

    /// <summary>A toggle that outlines every block of the document (paragraphs, lists, quotes, tables), to see its structure.</summary>
    ShowBlocks,

    /// <summary>Highlights the selected text (a <c>mark</c> element); with the caret in a highlight, removes it.</summary>
    Highlight
}
