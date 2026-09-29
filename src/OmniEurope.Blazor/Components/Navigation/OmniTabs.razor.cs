namespace OmniEurope.Blazor.Components;

/// <summary>
/// Tabs: a strip of <see cref="OmniTabsItem"/> tabs over the panel of the selected one. The strip
/// scrolls sideways when it does not fit; the Left and Right arrows, Home and End move between the
/// tabs that are not disabled, in the order the items are written.
/// </summary>
public partial class OmniTabs
{
    private readonly List<string> _registeredKeys = [];
    private readonly HashSet<string> _disabledKeys = new(StringComparer.Ordinal);
    private readonly string _generatedId = $"omni-tabs-{Guid.NewGuid():N}";
    private ElementReference _root;
    private ElementReference _strip;
    private ElementReference _container;
    private string? _wheelScopeAttached;
    private IJSObjectReference? _module;
    private KeyboardInterop? _keyboardInterop;
    private DotNetObjectReference<KeyboardInterop>? _selfReference;
    private string? _selectedValue;

    /// <summary>
    /// The key of the selected tab (<see cref="OmniTabsItem.Key"/>). Null, the default, selects the
    /// first tab; left unbound, the tabs keep the reader's choice themselves.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Raised with the key of the tab the reader selects, for <c>@bind-Value</c>.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Accessible name of the strip (<c>role="tablist"</c>). Null, the default, is the localized "Tabs".</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The tabs, <see cref="OmniTabsItem"/> in the order they are shown.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Builds every panel immediately while keeping inactive panels hidden.</summary>
    [Parameter]
    public bool RenderAllPanels { get; set; }

    /// <summary>
    /// Makes the selected panel the vertical scroll container: the tab strip, and whatever sits above
    /// the tabs, stay in place while the panel content scrolls under them. The tabs take the full height
    /// of their parent, which must therefore be sized (a flex item with a zero minimum, or a fixed height).
    /// Off by default: the panel then grows with its content and the page scrolls.
    /// </summary>
    [Parameter]
    public bool ScrollablePanels { get; set; }

    /// <summary>
    /// With <see cref="ScrollablePanels"/>, a CSS selector naming an ancestor (the page's content
    /// area, margins included) over which a vertical wheel turn scrolls the selected panel, and not
    /// only over the panel itself. The panel keeps its own wheel, so does any area under the pointer
    /// that can still scroll that way, and a panel that cannot scroll further lets the event go (to a
    /// grid's own <see cref="OmniDataGrid{TItem}.WheelScrollScope"/>, for one). Unset by default.
    /// </summary>
    [Parameter]
    public string? WheelScrollScope { get; set; }

    private string EffectiveLabel => LocalizeOr(Label, "TabsLabel");

    private string? EffectiveValue => Value ?? _selectedValue ?? (_registeredKeys.Count > 0 ? _registeredKeys[0] : null);

    /// <summary>The keys the arrows move through: the registered items, in their order, less the disabled ones.</summary>
    private List<string> NavigableKeys => _registeredKeys.Where(key => !_disabledKeys.Contains(key)).ToList();

    private OmniTabsContext TabContext => new() { Value = EffectiveValue, SelectAsync = SelectAsync, RegisterKey = RegisterKey, Phase = OmniTabsPhase.Tab, IdPrefix = _generatedId };

    private OmniTabsContext PanelContext => new() { Value = EffectiveValue, SelectAsync = SelectAsync, RegisterKey = RegisterKey, Phase = OmniTabsPhase.Panel, RenderAllPanels = RenderAllPanels, IdPrefix = _generatedId };

    private string RegisterKey(string key, bool disabled)
    {
        if (!_registeredKeys.Contains(key, StringComparer.Ordinal))
        {
            _registeredKeys.Add(key);
        }

        if (disabled)
        {
            _disabledKeys.Add(key);
        }
        else
        {
            _disabledKeys.Remove(key);
        }

        return key;
    }

    private async Task SelectAsync(string key)
    {
        _selectedValue = key;
        StateHasChanged();
        await ValueChanged.InvokeAsync(key);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/OmniEurope.Blazor/omni-focus.js");
            _keyboardInterop = new KeyboardInterop(SelectAsync);
            _selfReference = DotNetObjectReference.Create(_keyboardInterop);
            await _module.InvokeVoidAsync("configureTabs", _root, _selfReference);
            await _module.InvokeVoidAsync("configureTabsOverflow", _strip);
            if (Value is null && _registeredKeys.Count > 0)
            {
                StateHasChanged();
            }
        }

        var scope = ScrollablePanels && !string.IsNullOrWhiteSpace(WheelScrollScope) ? WheelScrollScope.Trim() : null;
        if (_module is not null && !string.Equals(scope, _wheelScopeAttached, StringComparison.Ordinal))
        {
            await _module.InvokeVoidAsync(scope is null ? "detachTabsWheelScope" : "attachTabsWheelScope", _container, scope);
            _wheelScopeAttached = scope;
        }
    }

    private Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        var keys = NavigableKeys;
        if (keys.Count == 0 || args.Key is not ("ArrowLeft" or "ArrowRight" or "Home" or "End"))
        {
            return Task.CompletedTask;
        }

        var current = Math.Max(0, keys.IndexOf(EffectiveValue ?? string.Empty));
        var next = args.Key switch
        {
            "Home" => 0,
            "End" => keys.Count - 1,
            "ArrowLeft" => (current - 1 + keys.Count) % keys.Count,
            _ => (current + 1) % keys.Count
        };
        return SelectAsync(keys[next]);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("detachTabsWheelScope", _container, null);
                await _module.InvokeVoidAsync("disposeTabsOverflow", _strip);
                await _module.InvokeVoidAsync("disposeTabs", _root);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
        _selfReference?.Dispose();
    }

    private sealed class KeyboardInterop(Func<string, Task> selectAsync)
    {
        [JSInvokable("OmniTabs.SelectFromKeyboard")]
        public Task SelectFromKeyboardAsync(string key) => selectAsync(key);
    }

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }
}
