using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A virtualized tree grid of 5 000 nodes with buttons that open and close every folder.</summary>
public partial class DataGridTreeVirtualExample
{
    private OmniDataGrid<ArchiveNode>? _grid;

    private Task ExpandAsync() => _grid?.ExpandAllTreeRowsAsync() ?? Task.CompletedTask;

    private Task CollapseAsync() => _grid?.CollapseAllTreeRowsAsync() ?? Task.CompletedTask;
}
