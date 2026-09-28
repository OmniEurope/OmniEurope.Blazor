namespace OmniEurope.Blazor.Components;

/// <summary>Which rules separate the cells of an <see cref="OmniDataGrid{TItem}"/>.</summary>
public enum OmniDataGridLines
{
    /// <summary>A rule under each row, the default.</summary>
    Horizontal,

    /// <summary>No rule at all.</summary>
    None,

    /// <summary>A rule between columns only.</summary>
    Vertical,

    /// <summary>Rules under rows and between columns.</summary>
    Both
}
