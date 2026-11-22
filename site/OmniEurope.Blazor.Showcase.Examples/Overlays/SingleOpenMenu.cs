namespace OmniEurope.Blazor.Showcase.Examples.Overlays;

/// <summary>Keeps one menu of a list of files open at a time.</summary>
public sealed class SingleOpenMenu
{
    /// <summary>Sample file names: data, not labels.</summary>
    public static IReadOnlyList<string> Files { get; } = ["report-2026.pdf", "budget.xlsx", "team-photo.jpg"];

    /// <summary>The file whose menu is open, if any.</summary>
    public string? OpenFile { get; private set; }

    /// <summary>Follows a menu that opens or closes: opening one replaces the open one.</summary>
    /// <param name="file">The file of the menu.</param>
    /// <param name="open">Whether that menu is now open.</param>
    public void OnChanged(string file, bool open)
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

    /// <summary>Closes the open menu.</summary>
    public void Close() => OpenFile = null;
}
