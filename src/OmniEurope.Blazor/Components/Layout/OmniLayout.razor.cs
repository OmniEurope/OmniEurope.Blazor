namespace OmniEurope.Blazor.Components;

/// <summary>
/// The shell of an application page: holds the <see cref="OmniHeader"/> and the <see cref="OmniBody"/>,
/// spans the full width, and becomes a column that owns the window when its main area scrolls on its own.
/// </summary>
public partial class OmniLayout
{
    /// <summary>The header and the body of the shell. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }
}
