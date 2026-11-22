using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>The field, one-file zone and reduced several-file zone forms of the upload component.</summary>
public partial class UploadFormExample
{
    private IReadOnlyList<OmniUploadFile> Manifest { get; set; } = [];

    private IReadOnlyList<OmniUploadFile> Archive { get; set; } = [];

    /// <summary>Four attachments already there, one more than the reduced list shows.</summary>
    private IReadOnlyList<OmniUploadFile> Attachments { get; set; } = UploadSamples.Attachments();
}
