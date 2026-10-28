using OmniEurope.Blazor.Showcase.Examples.Editor;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Editor;

/// <summary>The WYSIWYG editor page: the toolbar a page picks, trims and extends, the HTML it binds, and the extensions a host brings.</summary>
public partial class HtmlEditorPage
{
    [Inject]
    private IStringLocalizer<EditorStrings> Text { get; set; } = default!;

    private string Body { get; set; } = HtmlEditorSamples.Letter;

    private OmniHtmlEditorMode Mode { get; set; } = OmniHtmlEditorMode.Visual;

    private HtmlEditorToolbarChoice Toolbar { get; set; } = HtmlEditorToolbarChoice.Complete;

    private bool LimitToolbarRows { get; set; } = true;

    private bool ToolbarLabels { get; set; } = true;

    private bool ShowCommand { get; set; } = true;

    private bool ShowCustomTools { get; set; }

    private bool EnableBold { get; set; } = true;

    private bool EnableItalic { get; set; } = true;

    private bool EnableSubscript { get; set; } = true;

    private bool EnableSuperscript { get; set; } = true;

    private bool EnableIndent { get; set; } = true;

    private bool EnableOutdent { get; set; } = true;

    private bool ShowPreview { get; set; } = true;

    private bool Disabled { get; set; }

    private double RowsValue { get; set; } = 10;

    private int Rows => (int)RowsValue;

    /// <summary>The commands the switches take off the toolbar, whatever the toolbar chosen.</summary>
    private IReadOnlyCollection<OmniHtmlEditorAction> HiddenActions
    {
        get
        {
            List<OmniHtmlEditorAction> hidden = [];
            AddWhen(!EnableBold, OmniHtmlEditorAction.Bold);
            AddWhen(!EnableItalic, OmniHtmlEditorAction.Italic);
            AddWhen(!EnableSubscript, OmniHtmlEditorAction.Subscript);
            AddWhen(!EnableSuperscript, OmniHtmlEditorAction.Superscript);
            AddWhen(!EnableIndent, OmniHtmlEditorAction.Indent);
            AddWhen(!EnableOutdent, OmniHtmlEditorAction.Outdent);
            return hidden;

            void AddWhen(bool condition, OmniHtmlEditorAction action)
            {
                if (condition)
                {
                    hidden.Add(action);
                }
            }
        }
    }

    private IReadOnlyList<OmniOption<OmniHtmlEditorMode>> ModeOptions =>
    [
        new(OmniHtmlEditorMode.Visual, Text["HtmlEditorModeVisual"]),
        new(OmniHtmlEditorMode.Source, Text["HtmlEditorModeSource"])
    ];

    private IReadOnlyList<OmniOption<HtmlEditorToolbarChoice>> ToolbarOptions =>
    [
        new(HtmlEditorToolbarChoice.Complete, Text["HtmlEditorToolbarComplete"]),
        new(HtmlEditorToolbarChoice.Essential, Text["HtmlEditorToolbarEssential"]),
        new(HtmlEditorToolbarChoice.Reordered, Text["HtmlEditorToolbarReordered"])
    ];
}
