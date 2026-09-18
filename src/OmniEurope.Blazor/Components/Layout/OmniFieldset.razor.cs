namespace OmniEurope.Blazor.Components;

public partial class OmniFieldset
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-focus.js";
    private readonly ToggleInterop _toggleInterop;
    private ElementReference _details;
    private IJSObjectReference? _module;
    private DotNetObjectReference<ToggleInterop>? _selfReference;
    private bool _observing;
    private bool _collapsed;
    private bool? _collapsedParameter;

    public OmniFieldset()
    {
        _toggleInterop = new ToggleInterop(HandleToggledAsync);
    }

    [Parameter, EditorRequired]
    public RenderFragment? Legend { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether the group folds away. A section of rarely used settings is worth grouping without
    /// making every reader scroll past it.
    /// </summary>
    [Parameter]
    public bool Collapsible { get; set; }

    /// <summary>
    /// Whether a collapsible group is folded when it first renders. The disclosure is the browser's
    /// own from then on: it opens and closes without a round trip. Without
    /// <see cref="CollapsedChanged"/> this parameter does not track it; with it, it can be bound
    /// both ways.
    /// </summary>
    [Parameter]
    public bool Collapsed { get; set; }

    /// <summary>
    /// Raised with the new folded state when the reader opens or closes a
    /// <see cref="Collapsible"/> group. The native element's <c>toggle</c> event is heard by a
    /// listener the package script adds, no inline handler, and only while this callback has a
    /// delegate: without one the group renders and behaves exactly as before, with no script. A
    /// change the host makes through <see cref="Collapsed"/> is not reported back.
    /// </summary>
    [Parameter]
    public EventCallback<bool> CollapsedChanged { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // The state the disclosure is known to be in: the host's value whenever it changes it, the
        // reader's otherwise, so a toggle the host caused itself is not echoed back to it.
        if (_collapsedParameter != Collapsed)
        {
            _collapsedParameter = Collapsed;
            _collapsed = Collapsed;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var wanted = Collapsible && CollapsedChanged.HasDelegate;
        if (wanted == _observing)
        {
            return;
        }

        if (wanted)
        {
            _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);
            _selfReference ??= DotNetObjectReference.Create(_toggleInterop);
            await _module.InvokeVoidAsync("observeFieldsetToggle", _details, _selfReference);
            _observing = true;
        }
        else if (_module is not null)
        {
            // Either the callback went away or the disclosure did; a removed element resolves to
            // null in the script, which then has nothing to release.
            await _module.InvokeVoidAsync("disposeFieldsetToggle", _details);
            _observing = false;
        }
    }

    private async Task HandleToggledAsync(bool open)
    {
        var collapsed = !open;
        if (collapsed == _collapsed)
        {
            return;
        }

        _collapsed = collapsed;
        await CollapsedChanged.InvokeAsync(collapsed);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                if (_observing)
                {
                    await _module.InvokeVoidAsync("disposeFieldsetToggle", _details);
                }

                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        _selfReference?.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class ToggleInterop(Func<bool, Task> toggled)
    {
        [JSInvokable("OmniFieldset.Toggled")]
        public Task ToggledAsync(bool open) => toggled(open);
    }
}
