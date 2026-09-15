namespace OmniEurope.Blazor.Components;

/// <summary>What a line of a unified diff hunk says.</summary>
public enum OmniDiffLineKind
{
    /// <summary>Unchanged, shown for context (a leading space).</summary>
    Context,

    /// <summary>Added by the change (a leading <c>+</c>).</summary>
    Added,

    /// <summary>Removed by the change (a leading <c>-</c>).</summary>
    Removed,

    /// <summary>A remark about the line before, such as <c>\ No newline at end of file</c>.</summary>
    Note
}
