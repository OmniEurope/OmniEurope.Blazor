namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>An inline editor row saved by Ctrl+Enter and cancelled by Escape.</summary>
public partial class InlineFormKeysExample
{
    private string Title { get; set; } = string.Empty;

    private string Notes { get; set; } = string.Empty;

    private decimal? Price { get; set; }

    private string Status { get; set; } = "Press Ctrl+Enter to save, Escape to cancel.";

    private void Save() => Status = $"Saved \"{Title}\" ({Notes.Length} characters of notes, price {Price?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none"}).";

    private void Cancel()
    {
        Title = string.Empty;
        Notes = string.Empty;
        Price = null;
        Status = "Cancelled: the row was cleared.";
    }
}
