using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A virtualized, grouped grid that fills the height left in its frame.</summary>
public partial class DataGridFillExample
{
    private IReadOnlyList<OmniDataGridGroup> _grouping = [new(nameof(CaseRow.Country))];
}
