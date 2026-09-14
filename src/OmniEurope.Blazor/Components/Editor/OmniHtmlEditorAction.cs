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
    ToggleSource
}
