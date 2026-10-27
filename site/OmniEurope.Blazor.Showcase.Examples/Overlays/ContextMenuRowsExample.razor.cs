namespace OmniEurope.Blazor.Showcase.Examples.Overlays;

/// <summary>A list of files, each row with its own context menu, one open at a time.</summary>
public partial class ContextMenuRowsExample
{
    /// <summary>Sample file names: data, not labels.</summary>
    private static readonly string[] Files = ["report-2026.pdf", "budget.xlsx", "team-photo.jpg"];

    /// <summary>The file whose menu is open: one menu of the list at a time.</summary>
    private string? OpenFile { get; set; }

    private string? LastAction { get; set; }

    private void OnMenuChanged(string file, bool open)
    {
        if (open)
        {
            OpenFile = file;
        }
        else if (OpenFile == file)
        {
            OpenFile = null;
        }
    }

    private void Choose(string file, string action)
    {
        LastAction = $"{action}: {file}";
        OpenFile = null;
    }
}
