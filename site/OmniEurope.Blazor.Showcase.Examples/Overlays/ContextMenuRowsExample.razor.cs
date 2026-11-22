namespace OmniEurope.Blazor.Showcase.Examples.Overlays;

/// <summary>A list of files, each row with its own context menu, one open at a time.</summary>
public partial class ContextMenuRowsExample
{
    /// <summary>The file whose menu is open: one menu of the list at a time.</summary>
    private SingleOpenMenu Menus { get; } = new();

    private string? LastAction { get; set; }

    private void Choose(string file, string action)
    {
        LastAction = $"{action}: {file}";
        Menus.Close();
    }
}
