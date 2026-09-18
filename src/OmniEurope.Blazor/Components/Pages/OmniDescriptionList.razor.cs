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
    /// value.
    /// </summary>
    [Parameter]
    public int Columns { get; set; } = 1;

    private string ColumnsClass => $"omni-description-list--columns-{Math.Clamp(Columns, 1, MaximumColumns).ToString(CultureInfo.InvariantCulture)}";
}
