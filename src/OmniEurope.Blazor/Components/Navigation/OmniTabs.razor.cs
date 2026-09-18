namespace OmniEurope.Blazor.Components;

public partial class OmniTabs
{
    private readonly List<string> _registeredKeys = [];
    private ElementReference _root;
    private ElementReference _strip;
    private IJSObjectReference? _module;
    private KeyboardInterop? _keyboardInterop;
    private DotNetObjectReference<KeyboardInterop>? _selfReference;
    private string? _selectedValue;

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public int SelectedIndex { get; set; }

    [Parameter]
    public EventCallback<int> SelectedIndexChanged { get; set; }

    [Parameter]
    public EventCallback<int> Change { get; set; }

    [Parameter]
    public IReadOnlyList<string> Keys { get; set; } = Array.Empty<string>();

    [Parameter]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? Tabs { get; set; }

    /// <summary>Builds every panel immediately while keeping inactive panels hidden.</summary>
    [Parameter]
    public bool RenderAllPanels { get; set; }

    private RenderFragment? EffectiveContent => ChildContent ?? Tabs;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("TabsLabel")
        : Label;

    private string? EffectiveValue => Value
        ?? (SelectedIndexChanged.HasDelegate && SelectedIndex >= 0 && SelectedIndex < _registeredKeys.Count
            ? _registeredKeys[SelectedIndex]
            : _selectedValue ?? (SelectedIndex >= 0 && SelectedIndex < _registeredKeys.Count
                ? _registeredKeys[SelectedIndex]
                : null));

    private IReadOnlyList<string> EffectiveKeys => Keys.Count > 0 ? Keys : _registeredKeys;

    private OmniTabsContext TabContext => new() { Value = EffectiveValue, SelectAsync = SelectAsync, RegisterKey = RegisterKey, Phase = OmniTabsPhase.Tab };

    private OmniTabsContext PanelContext => new() { Value = EffectiveValue, SelectAsync = SelectAsync, RegisterKey = RegisterKey, Phase = OmniTabsPhase.Panel, RenderAllPanels = RenderAllPanels };

    private string RegisterKey(string key)
    {
        if (!_registeredKeys.Contains(key, StringComparer.Ordinal))
        {
            _registeredKeys.Add(key);
        }
        return key;
    }

    private async Task SelectAsync(string key)
    {
        _selectedValue = key;
        StateHasChanged();
        await ValueChanged.InvokeAsync(key);
        var index = _registeredKeys.IndexOf(key);
        if (index >= 0)
        {
            await SelectedIndexChanged.InvokeAsync(index);
            await Change.InvokeAsync(index);
        }
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
    }

    private Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (EffectiveKeys.Count == 0 || args.Key is not ("ArrowLeft" or "ArrowRight" or "Home" or "End"))
        {
            return Task.CompletedTask;
        }

        var current = Math.Max(0, Array.IndexOf(EffectiveKeys.ToArray(), EffectiveValue));
        var next = args.Key switch
        {
            "Home" => 0,
            "End" => EffectiveKeys.Count - 1,
            "ArrowLeft" => (current - 1 + EffectiveKeys.Count) % EffectiveKeys.Count,
            _ => (current + 1) % EffectiveKeys.Count
        };
        return SelectAsync(EffectiveKeys[next]);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
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
