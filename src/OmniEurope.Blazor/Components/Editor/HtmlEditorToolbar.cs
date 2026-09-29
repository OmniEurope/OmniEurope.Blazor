namespace OmniEurope.Blazor.Components;

/// <summary>
/// How the commands of an <see cref="OmniHtmlEditor"/> are drawn: their groups, resource keys, icons,
/// pressed and disabled states, the choices of its lists, and the name the surface script knows each
/// built-in action by.
/// </summary>
internal static class HtmlEditorToolbar
{
    /// <summary>The blocks of <see cref="OmniHtmlEditorAction.BlockFormat"/>: the tag and the resource key of its name.</summary>
    internal static readonly (string Value, string Key)[] BlockFormats =
    [
        ("p", "HtmlEditorParagraph"), ("h1", "HtmlEditorHeading1"), ("h2", "HtmlEditorHeading2"),
        ("h3", "HtmlEditorHeading3"), ("h4", "HtmlEditorHeading4")
    ];

    /// <summary>The sizes of <see cref="OmniHtmlEditorAction.FontSize"/>: the value and the resource key of its name.</summary>
    internal static readonly (string Value, string Key)[] FontSizes =
    [
        ("small", "HtmlEditorFontSizeSmall"), ("normal", "HtmlEditorFontSizeNormal"),
        ("large", "HtmlEditorFontSizeLarge"), ("xlarge", "HtmlEditorFontSizeXLarge")
    ];

    /// <summary>
    /// The toolbar cut at its separators into groups of commands, none empty: a separator at either end
    /// or next to another one draws nothing. Each group after the first is drawn with its separator
    /// before it, so a separator always stays with the group it opens and never ends a row of a wrapped
    /// toolbar; the one that would start a row falls outside the toolbar and is clipped.
    /// </summary>
    internal static List<List<OmniHtmlEditorCommand>> Groups(IReadOnlyList<OmniHtmlEditorCommand> commands)
    {
        var groups = new List<List<OmniHtmlEditorCommand>>();
        var current = new List<OmniHtmlEditorCommand>();
        foreach (var command in commands)
        {
            if (command.Action != OmniHtmlEditorAction.Separator)
            {
                current.Add(command);
            }
            else if (current.Count > 0)
            {
                groups.Add(current);
                current = [];
            }
        }

        if (current.Count > 0)
        {
            groups.Add(current);
        }

        return groups;
    }

    /// <summary>The name the surface script knows a built-in action by.</summary>
    internal static string ScriptName(OmniHtmlEditorAction action) => action.ToString().ToLowerInvariant();

    /// <summary>Whether the action works on the table at the caret.</summary>
    internal static bool IsTableAction(OmniHtmlEditorAction action) =>
        action is >= OmniHtmlEditorAction.AddRowAbove and <= OmniHtmlEditorAction.SetCellSpan;

    /// <summary>
    /// Whether the command cannot run now: the editor is locked, the command's own
    /// <see cref="OmniHtmlEditorCommand.Enabled"/> refuses the selection, the history is empty, the action
    /// needs the visual face, or a table action finds no table at the caret.
    /// </summary>
    internal static bool IsDisabled(OmniHtmlEditorCommand command, bool locked, OmniHtmlEditorMode mode,
        OmniHtmlEditorSelection? caret, HtmlEditorFormatState format, bool canUndo, bool canRedo) => locked
        || (command.Enabled is { } enabled && !enabled(mode == OmniHtmlEditorMode.Visual ? caret : null))
        || command.Action switch
    {
        OmniHtmlEditorAction.Undo => !canUndo,
        OmniHtmlEditorAction.Redo => !canRedo,
        OmniHtmlEditorAction.Unlink or OmniHtmlEditorAction.ClearFormatting
            or OmniHtmlEditorAction.ChangeCase or OmniHtmlEditorAction.ShowBlocks => mode == OmniHtmlEditorMode.Source,
        OmniHtmlEditorAction.Cut or OmniHtmlEditorAction.Copy or OmniHtmlEditorAction.Paste
            or OmniHtmlEditorAction.InsertParagraph => mode == OmniHtmlEditorMode.Source,
        _ when IsTableAction(command.Action) => mode == OmniHtmlEditorMode.Source || !format.Marks.Contains("intable"),
        _ => false
    };

    /// <summary>The resource key of the name of a built-in action; custom commands share one.</summary>
    internal static string LabelKey(OmniHtmlEditorAction action) => action switch
    {
        OmniHtmlEditorAction.Bold => "HtmlEditorBold",
        OmniHtmlEditorAction.Italic => "HtmlEditorItalic",
        OmniHtmlEditorAction.Underline => "HtmlEditorUnderline",
        OmniHtmlEditorAction.Strikethrough => "HtmlEditorStrikethrough",
        OmniHtmlEditorAction.Subscript => "HtmlEditorSubscript",
        OmniHtmlEditorAction.Superscript => "HtmlEditorSuperscript",
        OmniHtmlEditorAction.InlineCode => "HtmlEditorInlineCode",
        OmniHtmlEditorAction.BlockFormat => "HtmlEditorBlockFormat",
        OmniHtmlEditorAction.FontSize => "HtmlEditorFontSize",
        OmniHtmlEditorAction.BulletList => "HtmlEditorBulletList",
        OmniHtmlEditorAction.NumberedList => "HtmlEditorNumberedList",
        OmniHtmlEditorAction.Indent => "HtmlEditorIndent",
        OmniHtmlEditorAction.Outdent => "HtmlEditorOutdent",
        OmniHtmlEditorAction.Quote => "HtmlEditorQuote",
        OmniHtmlEditorAction.CodeBlock => "HtmlEditorCodeBlock",
        OmniHtmlEditorAction.Link => "HtmlEditorLink",
        OmniHtmlEditorAction.Unlink => "HtmlEditorUnlink",
        OmniHtmlEditorAction.AlignLeft => "HtmlEditorAlignLeft",
        OmniHtmlEditorAction.AlignCenter => "HtmlEditorAlignCenter",
        OmniHtmlEditorAction.AlignRight => "HtmlEditorAlignRight",
        OmniHtmlEditorAction.AlignJustify => "HtmlEditorAlignJustify",
        OmniHtmlEditorAction.InsertTable => "HtmlEditorInsertTable",
        OmniHtmlEditorAction.ClearFormatting => "HtmlEditorClearFormatting",
        OmniHtmlEditorAction.Undo => "HtmlEditorUndo",
        OmniHtmlEditorAction.Redo => "HtmlEditorRedo",
        OmniHtmlEditorAction.ToggleSource => "HtmlEditorSource",
        OmniHtmlEditorAction.Cut => "HtmlEditorCut",
        OmniHtmlEditorAction.Copy => "HtmlEditorCopy",
        OmniHtmlEditorAction.Paste => "HtmlEditorPaste",
        OmniHtmlEditorAction.InsertParagraph => "HtmlEditorInsertParagraph",
        OmniHtmlEditorAction.AddRowAbove => "HtmlEditorAddRowAbove",
        OmniHtmlEditorAction.AddRowBelow => "HtmlEditorAddRowBelow",
        OmniHtmlEditorAction.DeleteRow => "HtmlEditorDeleteRow",
        OmniHtmlEditorAction.AddColumnBefore => "HtmlEditorAddColumnBefore",
        OmniHtmlEditorAction.AddColumnAfter => "HtmlEditorAddColumnAfter",
        OmniHtmlEditorAction.DeleteColumn => "HtmlEditorDeleteColumn",
        OmniHtmlEditorAction.MergeCellRight => "HtmlEditorMergeCellRight",
        OmniHtmlEditorAction.MergeCellDown => "HtmlEditorMergeCellDown",
        OmniHtmlEditorAction.SplitCell => "HtmlEditorSplitCell",
        OmniHtmlEditorAction.SetCellSpan => "HtmlEditorSetCellSpan",
        OmniHtmlEditorAction.ChangeCase => "HtmlEditorChangeCase",
        OmniHtmlEditorAction.InsertSpecialCharacter => "HtmlEditorInsertSpecialCharacter",
        OmniHtmlEditorAction.ImportTable => "HtmlEditorImportTable",
        OmniHtmlEditorAction.ShowBlocks => "HtmlEditorShowBlocks",
        OmniHtmlEditorAction.Highlight => "HtmlEditorHighlight",
        _ => "HtmlEditorCustomCommand"
    };

    /// <summary>The icon of a command: its own, or the one of its built-in action, or none.</summary>
    internal static OmniIconName? IconOf(OmniHtmlEditorCommand command) => command.Icon ?? command.Action switch
    {
        OmniHtmlEditorAction.Bold => OmniIconName.TextB,
        OmniHtmlEditorAction.Italic => OmniIconName.TextItalic,
        OmniHtmlEditorAction.Underline => OmniIconName.TextUnderline,
        OmniHtmlEditorAction.Strikethrough => OmniIconName.TextStrikethrough,
        OmniHtmlEditorAction.Subscript => OmniIconName.TextSubscript,
        OmniHtmlEditorAction.Superscript => OmniIconName.TextSuperscript,
        OmniHtmlEditorAction.InlineCode => OmniIconName.Code,
        OmniHtmlEditorAction.BulletList => OmniIconName.ListBullets,
        OmniHtmlEditorAction.NumberedList => OmniIconName.NumberedList,
        OmniHtmlEditorAction.Indent => OmniIconName.TextIndent,
        OmniHtmlEditorAction.Outdent => OmniIconName.TextOutdent,
        OmniHtmlEditorAction.Quote => OmniIconName.Quotes,
        OmniHtmlEditorAction.CodeBlock => OmniIconName.CodeBlock,
        OmniHtmlEditorAction.Link => OmniIconName.Link,
        OmniHtmlEditorAction.Unlink => OmniIconName.LinkBreak,
        OmniHtmlEditorAction.AlignLeft => OmniIconName.TextAlignLeft,
        OmniHtmlEditorAction.AlignCenter => OmniIconName.TextAlignCenter,
        OmniHtmlEditorAction.AlignRight => OmniIconName.TextAlignRight,
        OmniHtmlEditorAction.AlignJustify => OmniIconName.TextAlignJustify,
        OmniHtmlEditorAction.InsertTable => OmniIconName.Table,
        OmniHtmlEditorAction.ClearFormatting => OmniIconName.Eraser,
        OmniHtmlEditorAction.Undo => OmniIconName.ArrowUUpLeft,
        OmniHtmlEditorAction.Redo => OmniIconName.ArrowUUpRight,
        OmniHtmlEditorAction.ToggleSource => OmniIconName.FileHtml,
        OmniHtmlEditorAction.Copy => OmniIconName.Copy,
        OmniHtmlEditorAction.InsertSpecialCharacter => OmniIconName.Smiley,
        OmniHtmlEditorAction.ImportTable => OmniIconName.Upload,
        OmniHtmlEditorAction.ShowBlocks => OmniIconName.Rows,
        OmniHtmlEditorAction.Highlight => OmniIconName.Highlighter,
        _ => null
    };

    /// <summary>The pressed state of a toggle, from the formatting at the caret; null for a plain action.</summary>
    internal static string? PressedOf(OmniHtmlEditorCommand command, OmniHtmlEditorMode mode,
        OmniHtmlEditorSelection? caret, HtmlEditorFormatState format, bool showBlocks)
    {
        if (command.Pressed is { } pressed)
        {
            return pressed(mode == OmniHtmlEditorMode.Visual ? caret : null) ? "true" : "false";
        }

        if (command.Action == OmniHtmlEditorAction.ToggleSource)
        {
            return mode == OmniHtmlEditorMode.Source ? "true" : "false";
        }

        if (command.Action == OmniHtmlEditorAction.ShowBlocks)
        {
            return showBlocks ? "true" : "false";
        }

        if (mode != OmniHtmlEditorMode.Visual)
        {
            return null;
        }

        return command.Action switch
        {
            OmniHtmlEditorAction.AlignLeft => Pressed(format.Align == "left"),
            OmniHtmlEditorAction.AlignCenter => Pressed(format.Align == "center"),
            OmniHtmlEditorAction.AlignRight => Pressed(format.Align == "right"),
            OmniHtmlEditorAction.AlignJustify => Pressed(format.Align == "justify"),
            OmniHtmlEditorAction.Bold or OmniHtmlEditorAction.Italic or OmniHtmlEditorAction.Underline
                or OmniHtmlEditorAction.Strikethrough or OmniHtmlEditorAction.Subscript or OmniHtmlEditorAction.Superscript
                or OmniHtmlEditorAction.InlineCode or OmniHtmlEditorAction.BulletList or OmniHtmlEditorAction.NumberedList
                or OmniHtmlEditorAction.Quote or OmniHtmlEditorAction.CodeBlock or OmniHtmlEditorAction.Link
                or OmniHtmlEditorAction.Highlight
                => Pressed(format.Marks.Contains(ScriptName(command.Action))),
            _ => null
        };

        static string Pressed(bool value) => value ? "true" : "false";
    }
}
