using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A log: numbered lines with their time and severity, warnings and errors tinted, that follows the
/// newest line while lines arrive and lets go as soon as the reader scrolls up, with a button to
/// jump back to the latest. An optional level filter hides the lines under a severity, and a search
/// marks every occurrence of a text and steps from one to the next.
/// </summary>
/// <remarks>
/// <para>
/// Lines come in by <see cref="Lines"/>; the component opens no connection. A host that streams
/// appends to the list it passes, a new list or the same one: an appended tail keeps the measured
/// heights of the lines before it, any other change measures again.
/// </para>
/// <para>
/// Only the lines near the visible part of the viewport are rendered, with the mechanics of the data
/// grid: measured heights, and two spacers sized through a CSS custom property by
/// <c>omni-log-viewer.js</c>, never a <c>style</c> attribute, so the strict CSP holds.
/// </para>
/// </remarks>
public partial class OmniLogViewer : IAsyncDisposable
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-log-viewer.js";
    private const double EstimatedLineHeight = 20d;
    private const int OverscanCount = 12;

    private readonly GridVirtualWindow _window = new();
    private readonly List<int> _visible = [];
    private readonly List<int> _matches = [];
    private ElementReference _viewport;
    private IJSObjectReference? _module;
    private DotNetObjectReference<OmniLogViewer>? _selfReference;
    private bool _attached;
    private bool _disposed;
    private GridVirtualRange _range;
    private double _scrollTop;
    private double _viewportHeight;
    private bool _atBottom = true;
    private string? _appliedHeight;

    private IReadOnlyList<OmniLogLine>? _observedLines;
    private int _observedCount;
    private OmniLogLine? _observedLast;
    private OmniLogLevel? _filteredLevel;
    private string _searchedText = string.Empty;

    private bool _following = true;
    private bool _observedFollow = true;
    private bool? _sentFollowing;
    private int _unseen;
    private OmniLogLevel? _minimumLevel;
    private OmniLogLevel? _observedMinimumLevel;
    private string _search = string.Empty;
    private string? _observedSearch;
    private int _matchCursor = -1;
    private double? _pendingReveal;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The lines, oldest first.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniLogLine> Lines { get; set; } = Array.Empty<OmniLogLine>();

    /// <summary>Accessible name of the log; the localized "log" when empty.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The height of the viewport, as a CSS length (<c>24rem</c>, <c>480px</c>, <c>60vh</c>, <c>100%</c>). Null keeps 24rem.</summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>Whether each line shows its number, counted in the whole log even when a filter hides lines.</summary>
    [Parameter]
    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>Whether a line's <see cref="OmniLogLine.Timestamp"/> is shown before its text.</summary>
    [Parameter]
    public bool ShowTimestamps { get; set; } = true;

    /// <summary>The format of the shown time; <c>HH:mm:ss</c> by default.</summary>
    [Parameter]
    public string TimestampFormat { get; set; } = "HH:mm:ss";

    /// <summary>The time zone the times are shown in; the local zone by default.</summary>
    [Parameter]
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>Whether each line shows the name of its level. Off, warnings and errors are still tinted and named to screen readers.</summary>
    [Parameter]
    public bool ShowLevels { get; set; }

    /// <summary>Whether long lines wrap instead of scrolling sideways.</summary>
    [Parameter]
    public bool WrapLines { get; set; }

    /// <summary>
    /// Whether the viewport follows the newest line. A reader scrolling up turns it off, scrolling back
    /// to the end or the jump button turns it on; <see cref="FollowChanged"/> reports both.
    /// </summary>
    [Parameter]
    public bool Follow { get; set; } = true;

    /// <summary>Raised when following starts or stops.</summary>
    [Parameter]
    public EventCallback<bool> FollowChanged { get; set; }

    /// <summary>Shows the level filter above the log.</summary>
    [Parameter]
    public bool ShowLevelFilter { get; set; }

    /// <summary>The least severe level shown; every line when null.</summary>
    [Parameter]
    public OmniLogLevel? MinimumLevel { get; set; }

    /// <summary>Raised when the level filter changes.</summary>
    [Parameter]
    public EventCallback<OmniLogLevel?> MinimumLevelChanged { get; set; }

    /// <summary>Shows the search box above the log.</summary>
    [Parameter]
    public bool ShowSearch { get; set; }

    /// <summary>The searched text, marked in every line, ignoring case and accents.</summary>
    [Parameter]
    public string? SearchText { get; set; }

    /// <summary>Raised when the searched text changes.</summary>
    [Parameter]
    public EventCallback<string?> SearchTextChanged { get; set; }

    /// <summary>Placeholder and accessible name of the search box; the localized "search the log" when empty.</summary>
    [Parameter]
    public string? SearchPlaceholder { get; set; }

    /// <summary>What an empty log says; the localized "no line yet" when empty.</summary>
    [Parameter]
    public string? EmptyText { get; set; }

    /// <summary>Whether the viewport currently follows the newest line.</summary>
    public bool IsFollowing => _following;

    private bool HasToolbar => ShowLevelFilter || ShowSearch;

    private string CurrentSearch => _search;

    private bool HasSearch => !string.IsNullOrWhiteSpace(_search);

    private OmniLogLevel? CurrentMinimumLevel => _minimumLevel;

    private bool ShowJump => !_following && _visible.Count > 0 && (_unseen > 0 || !_atBottom);

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("LogViewerLabel") : Label;

    private string EffectiveEmptyText => string.IsNullOrWhiteSpace(EmptyText) ? Localize("LogViewerEmpty") : EmptyText;

    private string EffectiveSearchPlaceholder => string.IsNullOrWhiteSpace(SearchPlaceholder) ? Localize("LogViewerSearch") : SearchPlaceholder;

    private string JumpText => _unseen > 0 ? Localize("LogViewerNewLines", _unseen) : Localize("LogViewerJumpToLatest");

    private string MatchSummary => _matches.Count == 0
        ? Localize("LogViewerNoMatch")
        : Localize("LogViewerMatchPosition", _matchCursor < 0 ? 0 : _matchCursor + 1, _matches.Count);

    private IReadOnlyList<OmniOption<OmniLogLevel?>> LevelOptions =>
    [
        .. new[] { OmniLogLevel.Debug, OmniLogLevel.Information, OmniLogLevel.Warning, OmniLogLevel.Error }
            .Select(level => new OmniOption<OmniLogLevel?>(level, Localize("LogViewerFromLevel", LevelName(level))))
    ];

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Lines);
        ArgumentNullException.ThrowIfNull(TimeZone);
        if (Height is not null && !CssLength().IsMatch(Height))
        {
            throw new ArgumentException($"'{Height}' is not a CSS length {nameof(OmniLogViewer)} accepts for {nameof(Height)} (a number followed by px, rem, em, vh or %).", nameof(Height));
        }

        // Bound or not, a parameter is only taken when it changes: a parent that does not bind Follow
        // does not pull the viewport back to the tail every time it renders.
        if (Follow != _observedFollow)
        {
            _observedFollow = Follow;
            SetFollowing(Follow);
        }

        if (MinimumLevel != _observedMinimumLevel)
        {
            _observedMinimumLevel = MinimumLevel;
            _minimumLevel = MinimumLevel;
        }

        if (!string.Equals(SearchText, _observedSearch, StringComparison.Ordinal))
        {
            _observedSearch = SearchText;
            _search = SearchText ?? string.Empty;
        }

        SyncLines();
    }

    /// <summary>
    /// Brings the visible lines up to date with <see cref="Lines"/>, the level filter and the search.
    /// An appended tail is filtered and searched on its own; anything else starts over.
    /// </summary>
    private void SyncLines()
    {
        var appended = ReferenceEquals(_observedLines, Lines) || _observedLines is null || IsAppendOf(Lines);
        var filterChanged = _filteredLevel != _minimumLevel;
        var searchChanged = !string.Equals(_searchedText, _search, StringComparison.Ordinal);
        var previousCount = _observedCount;

        // Lines that arrive while the reader is away from the tail are counted on the jump button.
        if (!_following && _observedLines is not null && !filterChanged)
        {
            for (var index = previousCount; index < Lines.Count; index++)
            {
                if (_minimumLevel is not { } threshold || Lines[index].Level >= threshold)
                {
                    _unseen++;
                }
            }
        }

        if (!appended || filterChanged || Lines.Count < _observedCount)
        {
            _visible.Clear();
            _observedCount = 0;
            _window.Configure(0, EstimatedLineHeight);
            _window.ResetMeasurements();
            _matches.Clear();
            _matchCursor = -1;
            searchChanged = true;
        }

        var firstNewPosition = _visible.Count;
        for (var index = _observedCount; index < Lines.Count; index++)
        {
            if (_minimumLevel is not { } minimum || Lines[index].Level >= minimum)
            {
                _visible.Add(index);
            }
        }

        _observedLines = Lines;
        _observedCount = Lines.Count;
        _observedLast = Lines.Count == 0 ? null : Lines[^1];
        _filteredLevel = _minimumLevel;
        _window.Configure(_visible.Count, EstimatedLineHeight);

        if (searchChanged)
        {
            _searchedText = _search;
            _matches.Clear();
            _matchCursor = -1;
            FindMatches(0);
        }
        else
        {
            FindMatches(firstNewPosition);
        }

        ComputeRange();
    }

    /// <summary>
    /// Whether the new list continues the one seen last: at least as long, and ending the old part
    /// with the same line, so that only its tail is new.
    /// </summary>
    private bool IsAppendOf(IReadOnlyList<OmniLogLine> lines) =>
        lines.Count >= _observedCount
        && (_observedCount == 0 || Equals(lines[_observedCount - 1], _observedLast));

    private void FindMatches(int fromPosition)
    {
        if (!HasSearch)
        {
            return;
        }

        var compare = CultureInfo.CurrentCulture.CompareInfo;
        var needle = _search.Trim();
        for (var position = fromPosition; position < _visible.Count; position++)
        {
            if (compare.IndexOf(Lines[_visible[position]].Text, needle, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0)
            {
                _matches.Add(position);
            }
        }
    }

    private void ComputeRange()
    {
        // Following means the tail is what is visible, whatever the last reported position was.
        var top = _following ? double.MaxValue : _scrollTop;
        _range = _window.Compute(top, _viewportHeight, OverscanCount);
    }

    private void SetFollowing(bool following)
    {
        _following = following;
        if (following)
        {
            _unseen = 0;
        }
    }

    private async Task ChangeFollowingAsync(bool following)
    {
        if (following == _following)
        {
            return;
        }

        SetFollowing(following);
        _observedFollow = following;
        ComputeRange();
        await FollowChanged.InvokeAsync(following);
    }

    private Task ToggleFollowAsync() => ChangeFollowingAsync(!_following);

    /// <summary>Follows the newest line again and scrolls to it.</summary>
    public Task JumpToLatestAsync() => ChangeFollowingAsync(true);

    private async Task SetMinimumLevelAsync(OmniLogLevel? level)
    {
        _minimumLevel = level;
        _observedMinimumLevel = level;
        SyncLines();
        await MinimumLevelChanged.InvokeAsync(level);
    }

    private async Task SetSearchAsync(string? text)
    {
        _search = text ?? string.Empty;
        _observedSearch = text;
        SyncLines();
        await SearchTextChanged.InvokeAsync(text);
    }

    private Task NextMatchAsync() => MoveToMatchAsync(1);

    private Task PreviousMatchAsync() => MoveToMatchAsync(-1);

    /// <summary>Steps to the next or previous match, wrapping around, and scrolls it into view.</summary>
    private async Task MoveToMatchAsync(int step)
    {
        if (_matches.Count == 0)
        {
            return;
        }

        _matchCursor = _matchCursor < 0
            ? (step > 0 ? FirstMatchFromView() : LastMatchBeforeView())
            : ((_matchCursor + step) % _matches.Count + _matches.Count) % _matches.Count;

        var position = _matches[_matchCursor];
        var offset = Math.Max(0d, _window.OffsetOf(position) - (_viewportHeight / 3d));
        _pendingReveal = offset;
        _scrollTop = offset;
        await ChangeFollowingAsync(false);
        ComputeRange();
    }

    private int FirstMatchFromView()
    {
        var first = _window.IndexAt(_scrollTop);
        var found = _matches.FindIndex(position => position >= first);
        return found < 0 ? 0 : found;
    }

    private int LastMatchBeforeView()
    {
        var first = _window.IndexAt(_scrollTop);
        var found = _matches.FindLastIndex(position => position < first);
        return found < 0 ? _matches.Count - 1 : found;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);
        if (!_attached)
        {
            _selfReference ??= DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync("attach", _viewport, _selfReference, _following);
            _attached = true;
            _sentFollowing = _following;
        }

        if (_sentFollowing != _following)
        {
            _sentFollowing = _following;
            await _module.InvokeVoidAsync("setFollowing", _viewport, _following);
        }

        var snapshot = await _module.InvokeAsync<GridViewportSnapshot?>("sync", _viewport, true);
        var moved = ApplySnapshot(snapshot);
        var previous = _range;
        ComputeRange();
        _appliedHeight = Height;
        await _module.InvokeVoidAsync("applyLayout", _viewport, _range.TopSpacer, _range.BottomSpacer, _appliedHeight, null);

        if (_pendingReveal is { } offset)
        {
            _pendingReveal = null;
            await _module.InvokeVoidAsync("reveal", _viewport, offset);
        }
        else if (_following)
        {
            await _module.InvokeAsync<bool>("pin", _viewport);
        }

        if (moved || previous != _range)
        {
            StateHasChanged();
        }
    }

    private bool ApplySnapshot(GridViewportSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return false;
        }

        var moved = false;
        if (Math.Abs(snapshot.ViewportHeight - _viewportHeight) > 0.5d)
        {
            _viewportHeight = snapshot.ViewportHeight;
            moved = true;
        }

        if (!_following && _pendingReveal is null && Math.Abs(snapshot.ScrollTop - _scrollTop) > 0.5d)
        {
            _scrollTop = snapshot.ScrollTop;
            moved = true;
        }

        foreach (var row in snapshot.Rows ?? [])
        {
            moved |= _window.Measure(row.Index, row.Height);
        }

        return moved;
    }

    /// <summary>
    /// Invoked by the log script when the viewport scrolls or resizes. <paramref name="following"/> is
    /// the script's own verdict: it lets go of the tail the moment the reader scrolls up.
    /// </summary>
    [JSInvokable]
    public async Task OnViewportChangedAsync(double scrollTop, double viewportHeight, bool atBottom, bool following)
    {
        if (_disposed)
        {
            return;
        }

        _scrollTop = scrollTop;
        _viewportHeight = viewportHeight;
        var wasAtBottom = _atBottom;
        _atBottom = atBottom;
        _sentFollowing = following;
        var previous = _range;
        var wasFollowing = _following;
        if (following != _following)
        {
            await ChangeFollowingAsync(following);
        }

        ComputeRange();
        if (previous != _range || wasFollowing != _following || wasAtBottom != _atBottom)
        {
            StateHasChanged();
        }
    }

    private string LineClass(OmniLogLine line, int position) => CssClassBuilder.Combine(
    [
        "omni-log-viewer__line",
        line.Level >= OmniLogLevel.Error ? "omni-log-viewer__line--error" : line.Level == OmniLogLevel.Warning ? "omni-log-viewer__line--warning" : null,
        line.Level <= OmniLogLevel.Debug ? "omni-log-viewer__line--quiet" : null,
        _matchCursor >= 0 && _matches[_matchCursor] == position ? "omni-log-viewer__line--current" : null
    ]);

    private string LevelName(OmniLogLevel level) => Localize($"LogLevel{level}");

    private static string MachineTime(DateTimeOffset timestamp) =>
        timestamp.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    private string DisplayTime(DateTimeOffset timestamp) =>
        TimeZoneInfo.ConvertTime(timestamp, TimeZone).ToString(TimestampFormat, CultureInfo.CurrentCulture);

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        try
        {
            if (_attached && _module is not null)
            {
                await _module.InvokeVoidAsync("detach", _viewport);
            }

            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }

        _selfReference?.Dispose();
        GC.SuppressFinalize(this);
    }

    [GeneratedRegex("^\\d+(\\.\\d+)?(px|rem|em|vh|%)$", RegexOptions.CultureInvariant)]
    private static partial Regex CssLength();
}
