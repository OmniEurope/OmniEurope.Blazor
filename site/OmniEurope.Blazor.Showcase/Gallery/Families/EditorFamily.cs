using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Editor family of the gallery: its pages, in menu order.</summary>
internal static class EditorFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyEditor", OmniIconName.BracketsCurly, typeof(EditorStrings),
    [
        new("html-editor", "HtmlEditorTitle", "HtmlEditorDescription", OmniIconName.Edit,
            ["OmniHtmlEditor", "OmniCharacterPalette", "OmniMailTemplateEditor"],
            [
                "Editor/HtmlEditorExample.razor", "Editor/HtmlEditorExample.razor.cs",
                "Editor/HtmlEditorCharactersExample.razor", "Editor/HtmlEditorCharactersExample.razor.cs",
                "Editor/HtmlEditorRenderedExample.razor", "Editor/HtmlEditorRenderedExample.razor.cs",
                "Editor/HtmlEditorBoundExample.razor", "Editor/HtmlEditorBoundExample.razor.cs",
                "Editor/HtmlEditorGalleryExample.razor", "Editor/HtmlEditorGalleryExample.razor.cs",
                "Editor/HtmlEditorHostCommandsExample.razor", "Editor/HtmlEditorHostCommandsExample.razor.cs",
                "Editor/HtmlEditorAnnotatedExample.razor", "Editor/HtmlEditorAnnotatedExample.razor.cs",
                "Editor/HtmlEditorExtensionExample.razor", "Editor/HtmlEditorExtensionExample.razor.cs",
                "Editor/MailTemplateExample.razor", "Editor/MailTemplateExample.razor.cs",
                "Editor/HtmlEditorIconsExample.razor",
                "Editor/HtmlEditorToolbarChoice.cs", "Editor/HtmlEditorSamples.cs", "Editor/SampleProofreader.cs"
            ]),
        // The word processor is the same component as the editor, on a sheet: this page shows that form.
        new("document-editor", "DocumentEditorTitle", "DocumentEditorDescription", OmniIconName.Document,
            ["OmniHtmlEditor"],
            [
                "Editor/DocumentEditorExample.razor", "Editor/DocumentEditorExample.razor.cs"
            ]),
        new("code-editor", "CodeEditorTitle", "CodeEditorDescription", OmniIconName.BracketsCurly,
            ["OmniCodeEditor"],
            [
                "Editor/CodeEditorExample.razor", "Editor/CodeEditorExample.razor.cs",
                "Editor/CodeEditorBoundExample.razor", "Editor/CodeEditorBoundExample.razor.cs",
                "Editor/CodeSamples.cs"
            ]),
        new("code-reading", "CodeReadingTitle", "CodeReadingDescription", OmniIconName.BracketsCurly,
            ["OmniCodeBlock", "OmniCodeViewer", "OmniUnifiedDiff", "OmniDiffViewer"],
            [
                "Editor/CodeBlockExample.razor", "Editor/CodeBlockExample.razor.cs",
                "Editor/CodeBlockAnsiExample.razor", "Editor/CodeBlockAnsiExample.razor.cs",
                "Editor/CodeViewerExample.razor", "Editor/CodeViewerExample.razor.cs",
                "Editor/CodeViewerExcerptExample.razor", "Editor/CodeViewerExcerptExample.razor.cs",
                "Editor/UnifiedDiffExample.razor", "Editor/UnifiedDiffExample.razor.cs",
                "Editor/DiffViewerExample.razor", "Editor/DiffViewerExample.razor.cs",
                "Editor/DiffViewerNavigationExample.razor", "Editor/DiffViewerNavigationExample.razor.cs",
                "Editor/CodeReadingSamples.cs"
            ])
    ]);
}
