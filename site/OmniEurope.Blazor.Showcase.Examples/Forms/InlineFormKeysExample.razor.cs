namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>An inline editor row saved by Ctrl+Enter and cancelled by Escape.</summary>
public partial class InlineFormKeysExample
{
    private string Title { get; set; } = string.Empty;

    private string Notes { get; set; } = string.Empty;

    private string Status { get; set; } = "Press Ctrl+Enter to save, Escape to cancel.";

    private void Save() => Status = $"Saved \"{Title}\" ({Notes.Length} characters of notes).";

    private void Cancel()
    {
        Title = string.Empty;
        Notes = string.Empty;
        Status = "Cancelled: the row was cleared.";
    }
}
