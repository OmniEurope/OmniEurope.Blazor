using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>The files the upload forms start with.</summary>
public static class UploadSamples
{
    /// <summary>Four attachments already there, one more than the reduced list shows.</summary>
    /// <returns>A fresh list, so each form keeps its own.</returns>
    public static IReadOnlyList<OmniUploadFile> Attachments() =>
    [
        new("dashboard-screenshot.png", 348_160, "image/png"),
        new("deployment-log.txt", 12_288, "text/plain"),
        new("coverage-report.pdf", 212_992, "application/pdf"),
        new("network-diagram.png", 96_256, "image/png")
    ];
}
