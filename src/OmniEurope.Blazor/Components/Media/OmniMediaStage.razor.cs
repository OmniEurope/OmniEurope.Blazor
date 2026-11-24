namespace OmniEurope.Blazor.Components;

/// <summary>
/// A scene of one large main item and a strip of thumbnails, for a video call or a shared screen: the items are
/// whatever <see cref="ItemTemplate"/> draws (a video tile, a picture, a card), the stage only places them.
/// </summary>
/// <remarks>
/// <para>
/// The first item is the main one unless <see cref="MainItem"/> names another. A thumbnail becomes the main item
/// with a click on its picture or, from the keyboard, with its own button (named "Show … large", reachable with Tab,
/// activated with Enter or Space); the change is raised through <see cref="MainItemChanged"/> and announced politely.
/// The button is a sibling of the thumbnail's content, never nested in it, and the interactive controls the content
/// draws (buttons, links, fields) keep their own clicks.
/// </para>
/// <para>
/// Every item is a direct child of one element, under a stable key, whatever its role: the main item is placed by
/// the style sheet, not by the markup. Switching the main item therefore moves the existing elements instead of
/// recreating them, and a <c>&lt;video&gt;</c> inside an item keeps playing. That holds as long as an item keeps its
/// key (see <see cref="KeyOf"/>); two items with the same key are an error of Blazor.
/// </para>
/// <para>
/// The layout follows the width of the stage itself (container queries), not that of the window. Wide (from 40rem):
/// the main item on the left at 16:9 (never taller than 40rem, <c>--omni-media-stage-main-max-height</c>) and the
/// thumbnails in a column on the right, scrolling when they do not fit the height of the main item. Narrow: the main
/// item on top and the thumbnails in a single row below, scrolling sideways. A thumbnail keeps a 16:9 frame; the
/// content of an item fills its frame. With a single item there is no thumbnail strip, and with no item the stage is
/// empty: its element is drawn, with its accessible name, and shows nothing.
/// </para>
/// </remarks>
/// <typeparam name="TItem">The type of the items.</typeparam>
public partial class OmniMediaStage<TItem>
{
    private readonly record struct Entry(TItem Item, object Key);

    private Entry[] _entries = [];
    // By key, never by position: Blazor captures a reference when it inserts the element and keeps it while the same
    // key keeps the element, so a reference held by position would go stale (or empty) when the list grows, shrinks
    // or is reordered.
    private readonly Dictionary<object, ElementReference> _itemElements = [];
    private static readonly object NullKey = new();
    private bool _focusMain;
    private int _mainIndex = -1;
    private TItem? _selected;
    private TItem? _hostMain;
    private bool _parametersSeen;
    private string _announcement = string.Empty;
    private bool _announceToggle;

    /// <summary>
    /// The items, in the order the keyboard visits them (the main item is where it is in this list, drawn large).
    /// Required; a null list is read as an empty one, and an empty stage is drawn empty.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Draws an item, whether it is the main one or a thumbnail; it fills the frame the stage gives it. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// A stable identity for an item, used as its <c>@key</c>, so an item keeps its element (and what runs in it)
    /// when the main item changes or the items are refreshed. Null uses the item itself. Keys must be unique among
    /// the items.
    /// </summary>
    [Parameter]
    public Func<TItem, object>? KeyOf { get; set; }

    /// <summary>
    /// How the button of a thumbnail and the announcement name an item (a participant's name); null, or a blank
    /// result, names it by its position ("Item 3").
    /// </summary>
    [Parameter]
    public Func<TItem, string>? ItemLabel { get; set; }

    /// <summary>
    /// The item shown large. Null, or an item that is not in <see cref="Items"/> (compared by <see cref="KeyOf"/>
    /// when it is set), leaves the first item as the main one. The stage also keeps the choice a click made: a parent
    /// that does not bind this parameter does not take it back, and a parent that sets it to another value moves the
    /// main item.
    /// </summary>
    [Parameter]
    public TItem? MainItem { get; set; }

    /// <summary>Raised with the item a click or the keyboard made the main one, once it is the main one.</summary>
    [Parameter]
    public EventCallback<TItem> MainItemChanged { get; set; }

    /// <summary>The accessible name of the stage; the localized "Media stage" when empty.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Takes the items and the main item. The main item is the one <see cref="MainItem"/> names, taken again only
    /// when the parent gives it a new value; if it is not among the items, the first one.
    /// </summary>
    /// <exception cref="ArgumentNullException"><see cref="ItemTemplate"/> is null.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(ItemTemplate);

        var items = Items ?? [];
        _entries = new Entry[items.Count];
        for (var index = 0; index < _entries.Length; index++)
        {
            var item = items[index];
            _entries[index] = new Entry(item, KeyOf is null ? item! : KeyOf(item));
        }

        // An element whose key left the list is gone with it; its reference is dropped.
        if (_itemElements.Count > 0)
        {
            var keys = _entries.Select(entry => ReferenceKey(entry.Key)).ToHashSet();
            foreach (var key in _itemElements.Keys.ToList())
            {
                if (!keys.Contains(key))
                {
                    _itemElements.Remove(key);
                }
            }
        }

        // The host's value is taken when it changes; a choice the stage made stays until the host says otherwise.
        if (!_parametersSeen || !EqualityComparer<TItem>.Default.Equals(MainItem, _hostMain))
        {
            _selected = MainItem;
            _hostMain = MainItem;
            _parametersSeen = true;
        }

        _mainIndex = _entries.Length == 0 ? -1 : Math.Max(0, IndexOf(_selected));
    }

    private int IndexOf(TItem? item)
    {
        if (item is null)
        {
            return -1;
        }

        for (var index = 0; index < _entries.Length; index++)
        {
            var same = KeyOf is null
                ? EqualityComparer<TItem>.Default.Equals(_entries[index].Item, item)
                : Equals(_entries[index].Key, KeyOf(item));
            if (same)
            {
                return index;
            }
        }

        return -1;
    }

    // A null item (or a null key from KeyOf) cannot index a dictionary; it gets a key of its own.
    private static object ReferenceKey(object? key) => key ?? NullKey;

    private string NameOf(int index)
    {
        var name = ItemLabel?.Invoke(_entries[index].Item);
        return string.IsNullOrWhiteSpace(name) ? Localize("MediaStageItem", index + 1) : name;
    }

    /// <summary>
    /// Makes the item at <paramref name="index"/> the main one. A choice that arrives for the item already shown large, or for a
    /// position the list no longer holds (a second click on a thumbnail the browser had not yet seen replaced), changes nothing.
    /// </summary>
    /// <param name="index">The position of the chosen item in <see cref="Items"/>.</param>
    internal async Task SelectAsync(int index)
    {
        if (index == _mainIndex || index < 0 || index >= _entries.Length)
        {
            return;
        }

        var item = _entries[index].Item;
        _selected = item;
        _mainIndex = index;
        _focusMain = true;
        Announce(Localize("MediaStageShownLarge", NameOf(index)));
        await MainItemChanged.InvokeAsync(item);
    }

    /// <summary>
    /// Gives the focus to the new main item once it is drawn: the button that was activated has just left its
    /// thumbnail (the main item has none), and a keyboard user would otherwise lose their place. A lost circuit is
    /// ignored.
    /// </summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>The task of the focus.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focusMain)
        {
            return;
        }

        _focusMain = false;
        // A reference not captured yet (the element is not drawn) has no context: there is nothing to focus.
        if (_mainIndex >= 0 && _mainIndex < _entries.Length
            && _itemElements.TryGetValue(ReferenceKey(_entries[_mainIndex].Key), out var element)
            && element.Context is not null)
        {
            try
            {
                await element.FocusAsync();
            }
            catch (Exception exception) when (exception is JSDisconnectedException or TaskCanceledException)
            {
                // The circuit is gone: there is nothing to focus.
            }
        }
    }

    /// <summary>
    /// Sets the live region. A repeated message gets a trailing no-break space in turn, so it is a new text and is
    /// read again.
    /// </summary>
    private void Announce(string message)
    {
        _announceToggle = !_announceToggle;
        _announcement = _announceToggle ? message : message + ' ';
    }
}
