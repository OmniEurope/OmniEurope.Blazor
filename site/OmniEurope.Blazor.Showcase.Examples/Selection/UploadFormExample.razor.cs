using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>The field, one-file zone and reduced several-file zone forms of the upload component.</summary>
public partial class UploadFormExample
{
    private IReadOnlyList<OmniUploadFile> Manifest { get; set; } = [];

    private IReadOnlyList<OmniUploadFile> Archive { get; set; } = [];

    /// <summary>Four attachments already there, one more than the reduced list shows.</summary>
    private IReadOnlyList<OmniUploadFile> Attachments { get; set; } =
    [
        new("dashboard-screenshot.png", 348_160, "image/png"),
        new("deployment-log.txt", 12_288, "text/plain"),
        new("coverage-report.pdf", 212_992, "application/pdf"),
        new("network-diagram.png", 96_256, "image/png")
    ];
}
