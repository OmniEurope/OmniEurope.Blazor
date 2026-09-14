namespace OmniEurope.Blazor.Components;

/// <summary>One task of an <see cref="OmniGantt"/>: a bar from its first day to its last.</summary>
public sealed record OmniGanttTask
{
    /// <summary>Identifies the task, for <see cref="DependsOn"/> and for the host's own lookups.</summary>
    public required string Id { get; init; }

    public required string Title { get; init; }

    /// <summary>First day of the task.</summary>
    public required DateOnly Start { get; init; }

    /// <summary>Last day of the task, included: a task on a single day has <see cref="Start"/> as its end.</summary>
    public required DateOnly End { get; init; }

    /// <summary>Share done, from 0 to 1, drawn as the filled part of the bar.</summary>
    public double Progress { get; init; }

    /// <summary>
    /// The group the task is listed under, groups following the order they first appear in. Null
    /// lists the task before every group.
    /// </summary>
    public string? Group { get; init; }

    /// <summary>
    /// Tasks that must finish before this one starts, by <see cref="Id"/>. Each is drawn as an arrow
    /// from the end of that task to the start of this one; an unknown identifier is ignored.
    /// </summary>
    public IReadOnlyList<string> DependsOn { get; init; } = [];

    /// <summary>Rank in the palette of eight chart colours, the same as a chart series.</summary>
    public int ColorIndex { get; init; }
}
