namespace OmniEurope.Blazor.Components;

/// <summary>
/// The part of an <see cref="OmniLayout"/> under its header: a flex row that holds the
/// <see cref="OmniSidebar"/> and the <see cref="OmniMain"/> side by side and takes the remaining height.
/// </summary>
public partial class OmniBody
{
    /// <summary>The sidebar and the main area. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }
}
