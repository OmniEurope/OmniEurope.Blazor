namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The address of every JavaScript module the components import through <c>IJSRuntime</c>, written
/// once. Each is a file of the library's <c>wwwroot</c>, served to the host under
/// <c>_content/OmniEurope.Blazor/</c>; a test checks that every constant names a file that exists.
/// </summary>
internal static class OmniModules
{
    private const string Root = "./_content/OmniEurope.Blazor/";

    /// <summary>The general helpers: first invalid field, text selection, document metadata, history, clipboard, boot splash, scroll watching.</summary>
    internal const string Interop = Root + "omniInterop.js";

    /// <summary>The code editor and the diff viewer.</summary>
    internal const string CodeEditor = Root + "omni-code-editor.js";

    /// <summary>The dialog surface: attaching, detaching and freezing its scale.</summary>
    internal const string Dialog = Root + "omni-dialog.js";

    /// <summary>File download, for the HTML editor and the Markdown export button.</summary>
    internal const string DocumentEditor = Root + "omni-document-editor.js";

    /// <summary>Focus management, roving focus, outside-click and menu dismissal.</summary>
    internal const string Focus = Root + "omni-focus.js";

    /// <summary>The data grid and the data list: layout, frozen columns, resizing, auto-fit, filter menus, fill.</summary>
    internal const string Grid = Root + "omni-grid.js";

    /// <summary>The HTML editor surface.</summary>
    internal const string HtmlEditor = Root + "omni-html-editor.js";

    /// <summary>The drag data of the tree items.</summary>
    internal const string Tree = Root + "omni-tree.js";

    /// <summary>The Kanban board surface and card focus.</summary>
    internal const string Kanban = Root + "omni-kanban.js";

    /// <summary>The log viewer: following the tail, pinning and revealing lines.</summary>
    internal const string LogViewer = Root + "omni-log-viewer.js";

    /// <summary>The mind map canvas: gestures, label measurement and focus.</summary>
    internal const string MindMap = Root + "omni-mindmap.js";

    /// <summary>The page header: the sideways scroll of a title longer than its line.</summary>
    internal const string PageHeader = Root + "omni-page-header.js";

    /// <summary>The scheduler surface.</summary>
    internal const string Scheduler = Root + "omni-scheduler.js";

    /// <summary>The spreadsheet surface: editor focus and cell reveal.</summary>
    internal const string Spreadsheet = Root + "omni-spreadsheet.js";

    /// <summary>The theme scope: applying and clearing its tokens.</summary>
    internal const string Theme = Root + "omni-theme.js";

    /// <summary>Trou noir's field: the black hole drawn by a WebGL shader on the scope's canvas.</summary>
    internal const string BlackHole = Root + "omni-black-hole.js";

    /// <summary>Tooltips, including the ones installed for native <c>title</c> attributes.</summary>
    internal const string Tooltip = Root + "omni-tooltip.js";
}
