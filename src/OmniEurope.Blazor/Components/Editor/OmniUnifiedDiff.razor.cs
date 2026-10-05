namespace OmniEurope.Blazor.Components;

/// <summary>
/// A unified diff, drawn without Monaco: one foldable section per file with its path, what happened
/// to it and its added and removed counts, then its hunks with the old and new line numbers side by
/// side and added and removed lines tinted.
/// </summary>
/// <remarks>
/// Give it the text of a diff in <see cref="Diff"/>, read by <see cref="OmniUnifiedDiffParser"/>, or
/// files already read in <see cref="Files"/>. Every line of an unfolded file is rendered: for a large
/// change, <see cref="ExpandedByDefault"/> off keeps the files folded until the reader opens one.
/// </remarks>
public partial class OmniUnifiedDiff
{
    private readonly string _generatedId = $"omni-unified-diff-{Guid.NewGuid():N}";
    private readonly Dictionary<int, bool> _toggled = [];
    private IReadOnlyList<OmniDiffFile> _files = [];
    private string? _parsedDiff;
    private IReadOnlyList<OmniDiffFile>? _observedFiles;

    /// <summary>The text of a unified diff. Ignored when <see cref="Files"/> is given.</summary>
    [Parameter]
    public string? Diff { get; set; }

    /// <summary>Files already read, from <see cref="OmniUnifiedDiffParser.Parse"/> or built by the host.</summary>
    [Parameter]
    public IReadOnlyList<OmniDiffFile>? Files { get; set; }

    /// <summary>Whether each file can be folded by its header.</summary>
    [Parameter]
    public bool Collapsible { get; set; } = true;

    /// <summary>
    /// Whether the files start unfolded; on by default. Turn it off for a change too large to read at
    /// once. Without <see cref="Collapsible"/> the files are always unfolded.
    /// </summary>
    [Parameter]
    public bool ExpandedByDefault { get; set; } = true;

    /// <summary>Whether long lines wrap instead of scrolling sideways.</summary>
    [Parameter]
    public bool Wrap { get; set; }

    /// <summary>Actions of the host at the end of each file header: open the file, view it whole.</summary>
    [Parameter]
    public RenderFragment<OmniDiffFile>? FileActionsTemplate { get; set; }

    /// <summary>What an empty diff says; the localized "no change" when empty.</summary>
    [Parameter]
    public string? EmptyText { get; set; }

    /// <summary>The files shown.</summary>
    public IReadOnlyList<OmniDiffFile> ShownFiles => _files;

    private string EffectiveEmptyText => string.IsNullOrWhiteSpace(EmptyText) ? Localize("UnifiedDiffEmpty") : EmptyText;

    /// <summary>
    /// Takes a new <see cref="Files"/> list as given, or else parses <see cref="Diff"/> again when its
    /// text changed; either way the folds the reader toggled are reset.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Files is not null)
        {
            if (!ReferenceEquals(Files, _observedFiles))
            {
                _observedFiles = Files;
                _parsedDiff = null;
                _files = Files;
                _toggled.Clear();
            }

            return;
        }

        if (_observedFiles is not null || !string.Equals(Diff, _parsedDiff, StringComparison.Ordinal))
        {
            _observedFiles = null;
            _parsedDiff = Diff;
            _files = OmniUnifiedDiffParser.Parse(Diff);
            _toggled.Clear();
        }
    }

    private bool IsCollapsed(int index) => Collapsible && (_toggled.TryGetValue(index, out var toggled) ? toggled : !ExpandedByDefault);

    private void Toggle(int index) => _toggled[index] = !IsCollapsed(index);

    private string HeaderId(int index) => $"{Id ?? _generatedId}-file-{index}";

    private string BodyId(int index) => $"{Id ?? _generatedId}-body-{index}";

    private string PathText(OmniDiffFile file) => file.Status == OmniDiffFileStatus.Renamed && file.OldPath is not null && file.NewPath is not null
        ? Localize("UnifiedDiffRenamedPath", file.OldPath, file.NewPath)
        : file.Path ?? Localize("UnifiedDiffUntitled");

    // Only a file that is not merely modified wears a badge: added, deleted, or else renamed.
    private static OmniTone StatusTone(OmniDiffFileStatus status) => status switch
    {
        OmniDiffFileStatus.Added => OmniTone.Success,
        OmniDiffFileStatus.Deleted => OmniTone.Danger,
        _ => OmniTone.Accent
    };

    private string StatusText(OmniDiffFileStatus status) => Localize($"UnifiedDiffStatus{status}");

    private static string LineClass(OmniDiffLineKind kind) => kind switch
    {
        OmniDiffLineKind.Added => "omni-unified-diff__line omni-unified-diff__line--added",
        OmniDiffLineKind.Removed => "omni-unified-diff__line omni-unified-diff__line--removed",
        OmniDiffLineKind.Note => "omni-unified-diff__line omni-unified-diff__line--note",
        _ => "omni-unified-diff__line"
    };
}
