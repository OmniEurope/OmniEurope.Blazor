namespace OmniEurope.Blazor.Components;

/// <summary>
/// A horizontal flex row of <see cref="OmniColumn"/> children on a twelve-part grid; the column widths
/// take the gap into account, so twelve twelfths still fit on one line.
/// </summary>
public partial class OmniRow
{
    /// <summary>The columns of the row. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The space between the columns; <see cref="OmniSpacing.Medium"/> by default.</summary>
    [Parameter]
    public OmniSpacing Gap { get; set; } = OmniSpacing.Medium;

    /// <summary>How the columns sit vertically; <see cref="OmniAlignment.Stretch"/> (same height) by default.</summary>
    [Parameter]
    public OmniAlignment Align { get; set; } = OmniAlignment.Stretch;

    /// <summary>How the columns are spread along the row; <see cref="OmniJustification.Start"/> by default.</summary>
    [Parameter]
    public OmniJustification Justify { get; set; } = OmniJustification.Start;

    /// <summary>Whether columns that do not fit move to a new line. On by default.</summary>
    [Parameter]
    public bool Wrap { get; set; } = true;
}
