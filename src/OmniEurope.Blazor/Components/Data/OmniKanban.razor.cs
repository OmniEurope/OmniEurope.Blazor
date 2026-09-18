namespace OmniEurope.Blazor.Components;

/// <summary>
/// A board of columns holding cards, where a card is moved from one column, or one place, to another
/// with the mouse or entirely from the keyboard.
/// </summary>
/// <remarks>
/// <para>
/// The board never changes <see cref="Items"/>: a move is reported through <see cref="OnItemMoved"/>
/// and the host applies it (and saves it), the card staying where it was otherwise. A card is placed in
/// the column whose key <see cref="ColumnOf"/> returns, in the order of <see cref="Items"/>.
/// </para>
/// <para>
/// Keyboard: a card takes the focus. Space or Enter picks it up, the arrow keys carry it up and down its
/// column and across the columns, Space or Enter drops it and Escape puts it back; every step is
/// announced. While no card is held, the arrow keys move the focus between cards. Keys pressed on a
/// control inside a card are left to that control.
/// </para>
/// </remarks>
/// <typeparam name="TItem">The type of the cards.</typeparam>
public partial class OmniKanban<TItem>
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-kanban.js";

    private readonly string _generatedId = $"omni-kanban-{Guid.NewGuid():N}";
    private ElementReference _root;
    private IJSObjectReference? _module;
    private DotNetObjectReference<KanbanInteropBridge>? _bridge;
    private IReadOnlyList<TItem> _rendered = [];
    private string _announcement = string.Empty;
    private bool _announceToggle;
    private string? _focusCard;
    private bool _disposed;

    // Keyboard move in progress: the card, where it came from, and where it would land.
    private int _grabbed = -1;
    private string _grabFrom = string.Empty;
    private int _grabFromIndex;
    private string _grabTo = string.Empty;
    private int _grabToIndex;

    // Mouse drag in progress.
    private int _dragged = -1;
    private string? _dropColumn;
    private int _dropIndex;
    private int _dropBefore = -1;

    /// <summary>The columns, in the order they are drawn.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniKanbanColumn> Columns { get; set; } = [];

    /// <summary>The cards, in the order each column lists them.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>The key of the column a card belongs to; a card whose key names no column is not drawn.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, string> ColumnOf { get; set; } = default!;

    /// <summary>Draws a card.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<TItem>? CardTemplate { get; set; }

    /// <summary>
    /// A stable identity for a card, so a card keeps its element when the items are refreshed. Null
    /// uses the item itself.
    /// </summary>
    [Parameter]
    public Func<TItem, object>? KeyOf { get; set; }

    /// <summary>How the announcements name a card; null uses the text of the item.</summary>
    [Parameter]
    public Func<TItem, string>? ItemLabel { get; set; }

    /// <summary>Replaces the header of each column, by default its title and its number of cards.</summary>
    [Parameter]
    public RenderFragment<OmniKanbanColumn>? ColumnHeaderTemplate { get; set; }

    /// <summary>Shows the board without letting a card move.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Raised when a card is dropped somewhere else than where it was.</summary>
    [Parameter]
    public EventCallback<OmniKanbanMove<TItem>> OnItemMoved { get; set; }

    /// <summary>Accessible name of the board; the localized "card board" when empty.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>What an empty column shows; a localized default when empty.</summary>
    [Parameter]
    public string? EmptyColumnText { get; set; }

    internal string Announcement => _announcement.TrimEnd('\u00A0');

    internal bool IsGrabbing => _grabbed >= 0;

    private string BaseId => Id ?? _generatedId;

    private string HelpId => $"{BaseId}-help";

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("KanbanLabel") : Label;

    private string EffectiveEmptyColumnText => string.IsNullOrWhiteSpace(EmptyColumnText) ? Localize("KanbanEmptyColumn") : EmptyColumnText;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(ColumnOf);
        ArgumentNullException.ThrowIfNull(CardTemplate);
        _rendered = Items ?? [];

        // A refresh that removed the card being carried, or a board turned read-only, ends the move.
        if (_grabbed >= _rendered.Count || (ReadOnly && _grabbed >= 0))
        {
            _grabbed = -1;
        }

        if (_dragged >= _rendered.Count || ReadOnly)
        {
            ClearDrag();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (firstRender)
            {
                _module = await JavaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);
                _bridge = DotNetObjectReference.Create(new KanbanInteropBridge(HandleCardKeyAsync));
                await _module.InvokeVoidAsync("attach", _root, _bridge);
            }

            if (_focusCard is { } card && _module is not null)
            {
                _focusCard = null;
                await _module.InvokeVoidAsync("focusCard", _root, card);
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the board with it.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("detach", _root);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        _bridge?.Dispose();
        GC.SuppressFinalize(this);
    }

    // ----- Keyboard ------------------------------------------------------------------------

    /// <summary>A key pressed on a card itself, forwarded by the script: Space, Enter, Escape or an arrow.</summary>
    internal Task HandleCardKeyAsync(string card, string key) => InvokeAsync(async () =>
    {
        if (_disposed || ReadOnly
            || !int.TryParse(card, NumberStyles.None, CultureInfo.InvariantCulture, out var index)
            || index >= _rendered.Count)
        {
            return;
        }

        if (_grabbed >= 0 && _grabbed != index)
        {
            // The focus left the card being carried: that move is abandoned before anything else.
            CancelGrab();
        }

        switch (key)
        {
            case " " or "Enter" when _grabbed < 0:
                Grab(index);
                break;
            case " " or "Enter":
                await DropGrabbedAsync();
                break;
            case "Escape" when _grabbed >= 0:
                CancelGrab();
                break;
            case "ArrowUp" when _grabbed >= 0:
                MoveWithinColumn(-1);
                break;
            case "ArrowDown" when _grabbed >= 0:
                MoveWithinColumn(1);
                break;
            case "ArrowLeft" when _grabbed >= 0:
                MoveAcrossColumns(-1);
                break;
            case "ArrowRight" when _grabbed >= 0:
                MoveAcrossColumns(1);
                break;
            default:
                return;
        }

        _focusCard = card;
        StateHasChanged();
    });

    private void Grab(int index)
    {
        var column = ColumnOf(_rendered[index]);
        if (!Columns.Any(candidate => string.Equals(candidate.Key, column, StringComparison.Ordinal)))
        {
            return;
        }

        _grabbed = index;
        _grabFrom = column;
        _grabFromIndex = PlaceWithout(column, index, index);
        _grabTo = column;
        _grabToIndex = _grabFromIndex;
        Announce(Localize("KanbanGrabbed", LabelOf(_rendered[index]), TitleOf(column), _grabToIndex + 1, CountWith(column)));
    }

    private void MoveWithinColumn(int delta)
    {
        var others = Others(_grabTo, _grabbed).Count;
        _grabToIndex = Math.Clamp(_grabToIndex + delta, 0, others);
        AnnouncePosition();
    }

    private void MoveAcrossColumns(int delta)
    {
        var current = IndexOfColumn(_grabTo);
        var target = Math.Clamp(current + delta, 0, Columns.Count - 1);
        if (target != current)
        {
            _grabTo = Columns[target].Key;
            _grabToIndex = Math.Min(_grabToIndex, Others(_grabTo, _grabbed).Count);
        }

        AnnouncePosition();
    }

    private async Task DropGrabbedAsync()
    {
        var index = _grabbed;
        var (from, fromIndex, to, toIndex) = (_grabFrom, _grabFromIndex, _grabTo, _grabToIndex);
        _grabbed = -1;
        var item = _rendered[index];
        Announce(Localize("KanbanDropped", LabelOf(item), TitleOf(to), toIndex + 1, CountWith(to, index)));
        if (!string.Equals(from, to, StringComparison.Ordinal) || fromIndex != toIndex)
        {
            await OnItemMoved.InvokeAsync(new OmniKanbanMove<TItem>(item, from, to, toIndex));
        }
    }

    private void CancelGrab()
    {
        if (_grabbed < 0)
        {
            return;
        }

        var item = _rendered[_grabbed];
        _grabbed = -1;
        Announce(Localize("KanbanCancelled", LabelOf(item), TitleOf(_grabFrom)));
    }

    private void AnnouncePosition() =>
        Announce(Localize("KanbanMoved", LabelOf(_rendered[_grabbed]), TitleOf(_grabTo), _grabToIndex + 1, CountWith(_grabTo, _grabbed)));

    // ----- Mouse ---------------------------------------------------------------------------

    private void StartDrag(int index)
    {
        if (ReadOnly || _grabbed >= 0)
        {
            return;
        }

        _dragged = index;
    }

    private void EndDrag() => ClearDrag();

    private void DragEnterColumn(string column)
    {
        if (_dragged < 0)
        {
            return;
        }

        _dropColumn = column;
        _dropIndex = Others(column, _dragged).Count;
        _dropBefore = -1;
    }

    private void DragEnterCard(string column, int index)
    {
        if (_dragged < 0)
        {
            return;
        }

        _dropColumn = column;
        if (index == _dragged)
        {
            // Over itself: the card would land where it is.
            _dropIndex = PlaceWithout(column, _dragged, _dragged);
            _dropBefore = -1;
            return;
        }

        _dropIndex = PlaceWithout(column, index, _dragged);
        _dropBefore = index;
    }

    private async Task DropDraggedAsync()
    {
        var index = _dragged;
        var column = _dropColumn;
        var place = _dropIndex;
        ClearDrag();
        if (index < 0 || column is null || index >= _rendered.Count)
        {
            return;
        }

        var item = _rendered[index];
        var from = ColumnOf(item);
        var fromIndex = PlaceWithout(from, index, index);
        Announce(Localize("KanbanDropped", LabelOf(item), TitleOf(column), place + 1, CountWith(column, index)));
        if (!string.Equals(from, column, StringComparison.Ordinal) || fromIndex != place)
        {
            await OnItemMoved.InvokeAsync(new OmniKanbanMove<TItem>(item, from, column, place));
        }
    }

    private void ClearDrag()
    {
        _dragged = -1;
        _dropColumn = null;
        _dropBefore = -1;
    }

    // ----- Layout --------------------------------------------------------------------------

    /// <summary>
    /// The cards a column draws, each with its index in <see cref="Items"/>: the card being carried
    /// from the keyboard is drawn where it would land rather than where it was.
    /// </summary>
    private List<(TItem Item, int Index)> Entries(string column)
    {
        var entries = new List<(TItem Item, int Index)>();
        for (var index = 0; index < _rendered.Count; index++)
        {
            if (index != _grabbed && string.Equals(ColumnOf(_rendered[index]), column, StringComparison.Ordinal))
            {
                entries.Add((_rendered[index], index));
            }
        }

        if (_grabbed >= 0 && string.Equals(_grabTo, column, StringComparison.Ordinal))
        {
            entries.Insert(Math.Clamp(_grabToIndex, 0, entries.Count), (_rendered[_grabbed], _grabbed));
        }

        return entries;
    }

    /// <summary>The indexes of the cards of a column, the excluded one left out.</summary>
    private List<int> Others(string column, int excluded)
    {
        var others = new List<int>();
        for (var index = 0; index < _rendered.Count; index++)
        {
            if (index != excluded && string.Equals(ColumnOf(_rendered[index]), column, StringComparison.Ordinal))
            {
                others.Add(index);
            }
        }

        return others;
    }

    /// <summary>
    /// The place of a card among the cards of its column once the excluded one is taken out: where the
    /// card itself sits, or where a card dropped before it lands.
    /// </summary>
    private int PlaceWithout(string column, int card, int excluded)
    {
        var others = Others(column, excluded);
        var place = others.IndexOf(card);
        return place >= 0 ? place : others.Count;
    }

    private int CountWith(string column, int? carried = null) =>
        Others(column, carried ?? _grabbed).Count + 1;

    private int IndexOfColumn(string key)
    {
        for (var index = 0; index < Columns.Count; index++)
        {
            if (string.Equals(Columns[index].Key, key, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return 0;
    }

    private string TitleOf(string key) =>
        Columns.FirstOrDefault(column => string.Equals(column.Key, key, StringComparison.Ordinal))?.Title ?? key;

    private string LabelOf(TItem item) => ItemLabel?.Invoke(item) ?? item?.ToString() ?? string.Empty;

    private object ItemKey(TItem item) => KeyOf?.Invoke(item) ?? (object?)item ?? string.Empty;

    private string HeaderId(OmniKanbanColumn column) => $"{BaseId}-column-{IndexOfColumn(column.Key).ToString(CultureInfo.InvariantCulture)}";

    private string ColumnCss(OmniKanbanColumn column) => CssClassBuilder.Combine(
    [
        "omni-kanban__column",
        _dragged >= 0 && string.Equals(_dropColumn, column.Key, StringComparison.Ordinal) ? "omni-kanban__column--drop" : null
    ]);

    private string CardCss(int index) => CssClassBuilder.Combine(
    [
        "omni-kanban__card",
        index == _grabbed ? "omni-kanban__card--grabbed" : null,
        index == _dragged ? "omni-kanban__card--dragging" : null,
        _dragged >= 0 && index == _dropBefore ? "omni-kanban__card--drop-before" : null
    ]);

    /// <summary>
    /// Sets the live region. A repeated message gets a trailing no-break space in turn, so it is a new
    /// text and read again.
    /// </summary>
    private void Announce(string message)
    {
        _announceToggle = !_announceToggle;
        _announcement = _announceToggle ? message : message + '\u00A0';
    }
}
