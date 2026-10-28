namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>Two run grids: a sorted one with a tinted column and a striped one with a sticky header.</summary>
public partial class DataGridRunsExample
{
    private IReadOnlyList<PipelineRun> _sortedSelection = [.. RunData.Recent.Where(run => run.Run == "#2394")];

    private IReadOnlyList<PipelineRun> _stripedSelection = [.. RunData.History.Where(run => run.Run == "#2395")];
}
