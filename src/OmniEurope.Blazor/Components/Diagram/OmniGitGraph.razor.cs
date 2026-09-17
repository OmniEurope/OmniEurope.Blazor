namespace OmniEurope.Blazor.Components;

/// <summary>
/// A commit history drawn as a graph: one row per commit, newest first, its lanes computed from the
/// parent identifiers alone and drawn beside the host's own row (subject, author, date).
/// </summary>
/// <remarks>
/// Each row carries its own small drawing, so the graph stays aligned with the text whatever the row
/// height. A parent that is not among <see cref="Items"/>, older than the page shown, keeps its lane
/// running to the bottom. Lanes take the eight chart colours in turn.
/// </remarks>
/// <typeparam name="TItem">The type of the commits.</typeparam>
public partial class OmniGitGraph<TItem>
{
    /// <summary>The commits, newest first, each one before its parents.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>The identifier of a commit, the one its children name among their parents.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, string> IdOf { get; set; } = default!;

    /// <summary>The identifiers of a commit's parents, the first parent first.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, IReadOnlyList<string>> ParentsOf { get; set; } = default!;

    /// <summary>Draws the text of a row beside the graph.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<TItem>? RowTemplate { get; set; }

    /// <summary>Accessible name of the list; the localized "commit history" when empty.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>What is written when there is no commit; a localized default when empty.</summary>
    [Parameter]
    public string? EmptyText { get; set; }

    internal GitGraphLayout Layout { get; private set; } = GitGraphLayout.Build([], []);

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("GitGraphLabel") : Label;

    private string EffectiveEmptyText => string.IsNullOrWhiteSpace(EmptyText) ? Localize("GitGraphEmpty") : EmptyText;

    private string ViewBox =>
        $"0 0 {MindMapGeometry.Format(Layout.LaneCount * GitGraphLayout.LaneWidth)} {MindMapGeometry.Format(GitGraphLayout.RowHeight)}";

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(IdOf);
        ArgumentNullException.ThrowIfNull(ParentsOf);
        Layout = GitGraphLayout.Build(
            [.. Items.Select(IdOf)],
            [.. Items.Select(item => ParentsOf(item) ?? [])]);
    }

    private static int LaneTone(int lane) => lane % 8;

    private static string NodeCss(GitGraphRow row) => CssClassBuilder.Combine(
    [
        "omni-git-graph__node",
        $"omni-git-graph__lane--{LaneTone(row.Lane)}",
        row.IsMerge ? "omni-git-graph__node--merge" : null
    ]);
}
