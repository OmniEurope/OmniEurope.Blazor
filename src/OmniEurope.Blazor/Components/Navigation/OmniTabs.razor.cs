namespace OmniEurope.Blazor.Components;

public partial class OmniTabs
{
    private ElementReference _root;
    private IJSObjectReference? _module;
    private KeyboardInterop? _keyboardInterop;
    private DotNetObjectReference<KeyboardInterop>? _selfReference;

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public IReadOnlyList<string> Keys { get; set; } = Array.Empty<string>();

    [Parameter]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("TabsLabel")
        : Label;

    private OmniTabsContext Context => new() { Value = Value, SelectAsync = SelectAsync };
    private Task SelectAsync(string key) => ValueChanged.InvokeAsync(key);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/OmniEurope.Blazor/omni-focus.js");
            _keyboardInterop = new KeyboardInterop(SelectAsync);
            _selfReference = DotNetObjectReference.Create(_keyboardInterop);
            await _module.InvokeVoidAsync("configureTabs", _root, _selfReference);
        }
    }

    private Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (Keys.Count == 0 || args.Key is not ("ArrowLeft" or "ArrowRight" or "Home" or "End"))
        {
            return Task.CompletedTask;
        }

        var current = Math.Max(0, Array.IndexOf(Keys.ToArray(), Value));
        var next = args.Key switch
        {
            "Home" => 0,
            "End" => Keys.Count - 1,
            "ArrowLeft" => (current - 1 + Keys.Count) % Keys.Count,
            _ => (current + 1) % Keys.Count
        };
        return ValueChanged.InvokeAsync(Keys[next]);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
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
}
