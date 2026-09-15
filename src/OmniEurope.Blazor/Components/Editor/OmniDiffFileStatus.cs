namespace OmniEurope.Blazor.Components;

/// <summary>What a change did to a file of a unified diff.</summary>
public enum OmniDiffFileStatus
{
    /// <summary>The file was changed in place.</summary>
    Modified,

    /// <summary>The file is new (<c>new file mode</c>, or an old side of <c>/dev/null</c>).</summary>
    Added,

    /// <summary>The file was removed (<c>deleted file mode</c>, or a new side of <c>/dev/null</c>).</summary>
    Deleted,

    /// <summary>The file was moved (<c>rename from</c> and <c>rename to</c>), with or without changes.</summary>
    Renamed
}
