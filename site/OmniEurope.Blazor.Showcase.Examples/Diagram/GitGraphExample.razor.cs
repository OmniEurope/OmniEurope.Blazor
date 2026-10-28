namespace OmniEurope.Blazor.Showcase.Examples.Diagram;

/// <summary>A commit graph with a branch opened from the main line and merged back into it.</summary>
public partial class GitGraphExample
{
    private static readonly IReadOnlyList<string> Commits = ["m5", "f4", "c3", "f2", "c1"];

    private static string CommitId(string commit) => commit;

    private static IReadOnlyList<string> CommitParents(string commit) => commit switch
    {
        "m5" => ["c3", "f4"],
        "f4" => ["f2"],
        "c3" => ["c1"],
        "f2" => ["c1"],
        _ => []
    };
}
