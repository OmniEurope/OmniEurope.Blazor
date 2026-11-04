using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Editor;

/// <summary>The structured document page: a service order edited in place, whose change set the host shows live.</summary>
public partial class StructuredDocumentPage
{
    [Inject]
    private IStringLocalizer<EditorStrings> Text { get; set; } = default!;

    private bool ReadOnly { get; set; }

    private bool AllowDrawings { get; set; } = true;
}
