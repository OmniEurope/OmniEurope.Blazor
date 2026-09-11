namespace OmniEurope.Blazor.Components;

/// <summary>
/// The properties of the node selected in an <see cref="OmniMindMap"/>: its text, colour, font
/// size, bold and italic, and a fixed width and height (0 sizes the box to the text).
/// </summary>
/// <remarks>
/// Place it in the map's <see cref="OmniMindMap.PanelContent"/>. It shows nothing while no single
/// node is selected. Every edit is a change of the map, raised through
/// <see cref="OmniMindMap.DocumentChanged"/> and undone with the rest of the history. When the
/// reader asks to rename a node and the host handles no <see cref="OmniMindMap.NodeRenameRequested"/>,
/// the focus moves to the text field of this panel.
/// </remarks>
public partial class OmniMindMapNodeProperties : IDisposable
{
    internal const int MinFontSize = 8;
    internal const int MaxFontSize = 48;
    internal const int MaxWidth = 500;
    internal const int MaxHeight = 300;

    private readonly string _generatedId = $"omni-mindmap-properties-{Guid.NewGuid():N}";
    private OmniMindMap? _subscribed;
    private IReadOnlyList<OmniOption<string>> _colorOptions = [];
    private string? _colorCulture;

    [CascadingParameter]
    private OmniMindMap? Owner { get; set; }

    private OmniMindMap Map => Owner
        ?? throw new InvalidOperationException($"{nameof(OmniMindMapNodeProperties)} must be placed inside the PanelContent of an {nameof(OmniMindMap)}.");

    private string BaseId => Id ?? _generatedId;

    private string TitleId => $"{BaseId}-title";

    private string LabelId => $"{BaseId}-label";

    private string ColorId => $"{BaseId}-color";

    private string FontSizeId => $"{BaseId}-font-size";

    private string WidthId => $"{BaseId}-width";

    private string HeightId => $"{BaseId}-height";

    private string HintId => $"{BaseId}-hint";

    /// <summary>The colour choices, rebuilt only when the UI culture changes.</summary>
    private IReadOnlyList<OmniOption<string>> ColorOptions
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            if (!string.Equals(_colorCulture, culture, StringComparison.Ordinal))
            {
                _colorCulture = culture;
                _colorOptions = [.. OmniMindMapGroups.Palette.Select(group => new OmniOption<string>(group, Map.GroupName(group)))];
            }

            return _colorOptions;
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var map = Map;
        if (!ReferenceEquals(_subscribed, map))
        {
            Unsubscribe();
            _subscribed = map;
            map.StateChanged += OnMapStateChanged;
            map.RenameRequested += FocusLabelAsync;
        }
    }

    public void Dispose()
    {
        Unsubscribe();
        GC.SuppressFinalize(this);
    }

    private static string GroupOf(OmniMindMapNode node) => OmniMindMapGroups.Resolve(node.Group);

    /// <summary>The colour of the selected node, as the drop-down binds it.</summary>
    private string SelectedGroup => Map.SelectedNode is { } node ? GroupOf(node) : OmniMindMapGroups.Root;

    private void Unsubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.StateChanged -= OnMapStateChanged;
            _subscribed.RenameRequested -= FocusLabelAsync;
            _subscribed = null;
        }
    }

    private void OnMapStateChanged() => _ = InvokeAsync(StateHasChanged);

    private Task FocusLabelAsync() => Map.FocusElementAsync(LabelId);

    private Task EditAsync(Func<OmniMindMapNode, OmniMindMapNode> change) =>
        Map.SelectedNode is { } node
            ? Map.DispatchAsync(() => Map.UpdateNodeAsync(change(node)))
            : Task.CompletedTask;

    private Task SetLabelAsync(string? value) => EditAsync(node => node with { Label = value ?? string.Empty });

    private Task SetGroupAsync(string? group) =>
        group is not null && OmniMindMapGroups.Palette.Contains(group, StringComparer.Ordinal)
            ? EditAsync(node => node with { Group = group })
            : Task.CompletedTask;

    private Task SetFontSizeAsync(int value) => EditAsync(node => node with { FontSize = Math.Clamp(value, MinFontSize, MaxFontSize) });

    private Task SetBoldAsync(bool value) => EditAsync(node => node with { Bold = value });

    private Task SetItalicAsync(bool value) => EditAsync(node => node with { Italic = value });

    private Task SetWidthAsync(int value) => EditAsync(node => node with { Width = Math.Clamp(value, 0, MaxWidth) });

    private Task SetHeightAsync(int value) => EditAsync(node => node with { Height = Math.Clamp(value, 0, MaxHeight) });
}
