namespace OmniEurope.Blazor.Components;

/// <summary>
/// The essentials of an entity as label and value pairs, in a <c>dl</c> element: a server's address
/// and system, a project's owner and creation date. Holds <see cref="OmniDescriptionItem"/>.
/// </summary>
public partial class OmniDescriptionList
{
    internal const int MaximumColumns = 4;

    /// <summary>The items, <see cref="OmniDescriptionItem"/>.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// How many columns the items flow into, from 1 to 4; one column on a narrow screen whatever the
    /// value, and one item per row in a narrow responsive list (<see cref="Responsive"/>).
    /// </summary>
    [Parameter]
    public int Columns { get; set; } = 1;

    /// <summary>
    /// Whether each label goes above its value, both at the start, when the list itself is narrower than
    /// 30rem (a side panel, a narrow card), whatever the screen width; wider, each label sits beside its
    /// value, both centred on the same line. On by default. The list then sizes against its container,
    /// not its content: turn it off for a list that must take the width of its content (in a
    /// shrink-to-fit parent).
    /// </summary>
    [Parameter]
    public bool Responsive { get; set; } = true;

    private string ColumnsClass => $"omni-description-list--columns-{Math.Clamp(Columns, 1, MaximumColumns).ToString(CultureInfo.InvariantCulture)}";

    private string? ResponsiveClass => Responsive ? "omni-description-list--responsive" : null;
}
