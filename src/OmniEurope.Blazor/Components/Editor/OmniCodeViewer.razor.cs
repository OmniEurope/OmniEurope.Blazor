using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A read-only view of a file or a definition: numbered lines, a wrap toggle, a copy button, actions of
/// the host in the header, links drawn from patterns, and an optional side panel beside the code.
/// </summary>
/// <remarks>
/// <para>
/// It complements <see cref="OmniCodeEditor"/> rather than repeating it. The editor shows line numbers
/// and colours only with Monaco, which needs <c>style-src 'unsafe-inline'</c>; its plain text engine is a
/// text area with neither. The viewer is plain markup, without script beyond the copy, so it keeps its
/// line numbers and links under the strict policy. For colouring, use the editor with
/// <see cref="OmniCodeEditor.ReadOnly"/> where Monaco is allowed.
/// </para>
/// <para>
/// Links reuse <see cref="OmniCodeEditorLink"/>: each pattern is applied to each line, here by the .NET
/// regular expression engine (with a one-second timeout), and its first group, or the whole match,
/// becomes a button that raises <see cref="LinkActivated"/>. Every line is rendered: meant for a file or
/// a definition, not for a log of thousands of lines, which <see cref="OmniLogViewer"/> virtualises.
/// </para>
/// </remarks>
public partial class OmniCodeViewer : IAsyncDisposable
{
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

    private readonly string _generatedId = $"omni-code-viewer-{Guid.NewGuid():N}";
    private List<string> _lines = [];
    private string? _splitCode;
    private IReadOnlyList<(OmniCodeEditorLink Link, Regex Pattern)> _patterns = [];
    private IReadOnlyList<OmniCodeEditorLink>? _compiledLinks;
    private HashSet<int> _highlighted = [];
    private OmniClipboardCopy? _clipboard;
    private bool _wrap;
    private bool _observedWrap;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The code, shown as written.</summary>
    [Parameter, EditorRequired]
    public string? Code { get; set; }

    /// <summary>The title of the view and the accessible name of the code: a file name, a definition name.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The language, shown as a small label beside the title.</summary>
    [Parameter]
    public string? Language { get; set; }

    /// <summary>Whether each line shows its number.</summary>
    [Parameter]
    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>
    /// Number of the first line shown, 1 by default: an excerpt of a file (the lines around a finding)
    /// keeps the numbers the lines have in the file. <see cref="HighlightedLines"/> and the line numbers
    /// reported by <see cref="LinkActivated"/> count the same way. Below 1, the count starts at 1.
    /// </summary>
    [Parameter]
    public int FirstLineNumber { get; set; } = 1;

    /// <summary>Whether long lines wrap. Taken when it changes; the toggle changes it too.</summary>
    [Parameter]
    public bool Wrap { get; set; }

    /// <summary>Raised when the wrap toggle is pressed.</summary>
    [Parameter]
    public EventCallback<bool> WrapChanged { get; set; }

    /// <summary>Whether the header shows the wrap toggle.</summary>
    [Parameter]
    public bool ShowWrapToggle { get; set; } = true;

    /// <summary>Whether the header shows the copy button.</summary>
    [Parameter]
    public bool ShowCopy { get; set; } = true;

    /// <summary>Numbers of the lines to mark, counted from <see cref="FirstLineNumber"/>: the lines of an error, a search hit.</summary>
    [Parameter]
    public IReadOnlyList<int>? HighlightedLines { get; set; }

    /// <summary>Patterns whose matches become links, reported through <see cref="LinkActivated"/>.</summary>
    [Parameter]
    public IReadOnlyList<OmniCodeEditorLink> Links { get; set; } = Array.Empty<OmniCodeEditorLink>();

    /// <summary>Raised when a link is followed.</summary>
    [Parameter]
    public EventCallback<OmniCodeEditorLinkEventArgs> LinkActivated { get; set; }

    /// <summary>Actions of the host at the end of the header: open, close, download.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>A panel beside the code: what the file defines, the errors it holds. Stacked under the code on a narrow screen.</summary>
    [Parameter]
    public RenderFragment? SidePanel { get; set; }

    /// <summary>Accessible name of the side panel.</summary>
    [Parameter]
    public string? SidePanelLabel { get; set; }

    /// <summary>What an empty code says; the localized "nothing to show" when empty.</summary>
    [Parameter]
    public string? EmptyText { get; set; }

    /// <summary>How long the copy button shows its outcome.</summary>
    [Parameter]
    public TimeSpan CopiedFeedbackDuration { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Raised after a copy, with whether the clipboard accepted it.</summary>
    [Parameter]
    public EventCallback<bool> OnCopy { get; set; }

    private string TitleId => $"{Id ?? _generatedId}-title";

    private string EffectiveTitle => string.IsNullOrWhiteSpace(Title) ? Localize("CodeViewerLabel") : Title;

    private string EffectiveEmptyText => string.IsNullOrWhiteSpace(EmptyText) ? Localize("CodeViewerEmpty") : EmptyText;

    private OmniClipboardCopy Clipboard => _clipboard ??= new OmniClipboardCopy(JavaScript, () => InvokeAsync(StateHasChanged));

    private bool? CopyResult => _clipboard?.Result;

    private string CopyLabel => Localize(OmniClipboardCopy.LabelKey(CopyResult));

    /// <summary>The header shared with <see cref="OmniCodeBlock"/>: the wrap toggle before the copy button, the host actions after it.</summary>
    private RenderFragment Header => builder =>
    {
        builder.OpenComponent<CodeHeader>(0);
        builder.AddComponentParameter(1, nameof(CodeHeader.Block), "omni-code-viewer");
        builder.AddComponentParameter(2, nameof(CodeHeader.TitleId), TitleId);
        builder.AddComponentParameter(3, nameof(CodeHeader.Title), EffectiveTitle);
        builder.AddComponentParameter(4, nameof(CodeHeader.Language), Language);
        builder.AddComponentParameter(5, nameof(CodeHeader.ShowCopy), ShowCopy);
        builder.AddComponentParameter(6, nameof(CodeHeader.CopyText), true);
        builder.AddComponentParameter(7, nameof(CodeHeader.CopyResult), CopyResult);
        builder.AddComponentParameter(8, nameof(CodeHeader.CopyLabel), CopyLabel);
        builder.AddComponentParameter(9, nameof(CodeHeader.OnCopy), EventCallback.Factory.Create<MouseEventArgs>(this, CopyAsync));
        if (ShowWrapToggle)
        {
            builder.AddComponentParameter(10, nameof(CodeHeader.LeadingActions), WrapToggle);
        }

        builder.AddComponentParameter(11, nameof(CodeHeader.TrailingActions), Actions);
        builder.CloseComponent();
    };

    private RenderFragment WrapToggle => builder =>
    {
        builder.OpenComponent<OmniButton>(0);
        builder.AddComponentParameter(1, nameof(OmniButton.Class), "omni-code-viewer__wrap");
        builder.AddComponentParameter(2, nameof(OmniButton.Variant), _wrap ? OmniButtonVariant.Secondary : OmniButtonVariant.Ghost);
        builder.AddComponentParameter(3, nameof(OmniButton.Size), OmniControlSize.Small);
        builder.AddComponentParameter(4, "aria-pressed", _wrap ? "true" : "false");
        builder.AddComponentParameter(5, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, ToggleWrap));
        builder.AddComponentParameter(6, nameof(OmniButton.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<OmniIcon>(0);
            content.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.TextAlignLeft);
            content.AddComponentParameter(2, nameof(OmniIcon.Size), OmniControlSize.Small);
            content.CloseComponent();
            content.OpenElement(3, "span");
            content.AddContent(4, Localize("CodeViewerWrap"));
            content.CloseElement();
        }));
        builder.CloseComponent();
    };

    private string CopyAnnouncement => OmniClipboardCopy.AnnouncementKey(CopyResult) is { } key ? Localize(key) : string.Empty;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Links);
        if (Wrap != _observedWrap)
        {
            _observedWrap = Wrap;
            _wrap = Wrap;
        }

        if (!string.Equals(_splitCode, Code, StringComparison.Ordinal))
        {
            _splitCode = Code;
            _lines = [.. (Code ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')];
            if (_lines.Count > 1 && _lines[^1].Length == 0)
            {
                // A final newline ends the last line; it does not open an empty one.
                _lines.RemoveAt(_lines.Count - 1);
            }
        }

        if (!ReferenceEquals(_compiledLinks, Links))
        {
            _compiledLinks = Links;
            _patterns = [.. Links.Select(link => (link, new Regex(link.Pattern, RegexOptions.CultureInvariant, PatternTimeout)))];
        }

        _highlighted = HighlightedLines is null ? [] : [.. HighlightedLines];
    }

    private string LineClass(int number) => _highlighted.Contains(number)
        ? "omni-code-viewer__line omni-code-viewer__line--highlighted"
        : "omni-code-viewer__line";

    /// <summary>
    /// Cuts a line around the links its patterns find: the first group of each match, or the whole
    /// match, the earliest match winning where two overlap.
    /// </summary>
    private IEnumerable<CodeSegment> Segments(string line, int number)
    {
        if (_patterns.Count == 0 || line.Length == 0)
        {
            return [new CodeSegment(line, null)];
        }

        var spans = new List<(int Start, int Length, CodeLink Link)>();
        foreach (var (link, pattern) in _patterns)
        {
            try
            {
                foreach (Match match in pattern.Matches(line))
                {
                    var target = match.Groups.Count > 1 && match.Groups[1].Success ? match.Groups[1] : (Group)match;
                    if (target.Length > 0)
                    {
                        spans.Add((target.Index, target.Length, new CodeLink(link.Name, target.Value, number, link.Tooltip ?? Localize("CodeViewerLinkHint"))));
                    }
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // A pattern too slow for one line leaves that line without its links, not the page frozen.
            }
        }

        if (spans.Count == 0)
        {
            return [new CodeSegment(line, null)];
        }

        var segments = new List<CodeSegment>();
        var cursor = 0;
        foreach (var span in spans.OrderBy(span => span.Start))
        {
            if (span.Start < cursor)
            {
                continue;
            }

            if (span.Start > cursor)
            {
                segments.Add(new CodeSegment(line[cursor..span.Start], null));
            }

            segments.Add(new CodeSegment(line.Substring(span.Start, span.Length), span.Link));
            cursor = span.Start + span.Length;
        }

        if (cursor < line.Length)
        {
            segments.Add(new CodeSegment(line[cursor..], null));
        }

        return segments;
    }

    private Task ActivateLinkAsync(CodeLink link) =>
        LinkActivated.InvokeAsync(new OmniCodeEditorLinkEventArgs(link.Name, link.Target, link.LineNumber));

    private async Task ToggleWrap()
    {
        _wrap = !_wrap;
        _observedWrap = _wrap;
        await WrapChanged.InvokeAsync(_wrap);
    }

    /// <summary>Copies <see cref="Code"/> to the clipboard; true when the clipboard accepted it.</summary>
    public async Task<bool> CopyAsync()
    {
        var copied = await Clipboard.CopyAsync(Code ?? string.Empty, CopiedFeedbackDuration, Clock);
        StateHasChanged();
        await OnCopy.InvokeAsync(copied);
        return copied;
    }

    public async ValueTask DisposeAsync()
    {
        if (_clipboard is not null)
        {
            await _clipboard.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    private sealed record CodeLink(string Name, string Target, int LineNumber, string Tooltip);

    private readonly record struct CodeSegment(string Text, CodeLink? Link);
}
