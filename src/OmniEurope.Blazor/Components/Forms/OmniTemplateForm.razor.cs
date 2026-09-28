namespace OmniEurope.Blazor.Components;

public partial class OmniTemplateForm<TModel>
where TModel : class
{
    private EditContext? _resolvedEditContext;
    private EditContext? _trackedEditContext;
    private bool _dirty;
    private bool _submitting;
    private TModel? _lastModel;
    private ElementReference _formRoot;
    private IJSObjectReference? _module;

    [Parameter]
    public TModel? Model { get; set; }

    [Parameter]
    public EditContext? EditContext { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment<EditContext> ChildContent { get; set; } = default!;

    [Parameter]
    public EventCallback<EditContext> OnValidSubmit { get; set; }

    [Parameter]
    public EventCallback<EditContext> OnInvalidSubmit { get; set; }

    [Parameter]
    public string? FormName { get; set; }

    [Parameter]
    public bool FocusOnFirstInvalid { get; set; } = true;

    /// <summary>
    /// Whether leaving the page once a field has changed asks first (<see cref="OmniUnsavedChangesGuard"/>):
    /// a navigation inside the application waits for a confirmation, and closing or reloading the tab
    /// raises the browser's question. On by default. A valid submit counts as saved: the question stops
    /// until a field changes again, and a navigation the submit handler starts is never held.
    /// </summary>
    [Parameter]
    public bool GuardUnsavedChanges { get; set; } = true;

    protected override void OnParametersSet()
    {
        if ((Model is null) == (EditContext is null))
        {
            throw new InvalidOperationException("OmniTemplateForm requires exactly one of Model or EditContext.");
        }

        if (EditContext is not null)
        {
            _resolvedEditContext = EditContext;
            _lastModel = null;
        }
        else if (!ReferenceEquals(Model, _lastModel))
        {
            _lastModel = Model;
            _resolvedEditContext = new EditContext(Model!);
        }

        Track(_resolvedEditContext);
    }

    // A new edit context is a new form: it starts clean, and only its own changes count.
    private void Track(EditContext? context)
    {
        if (ReferenceEquals(context, _trackedEditContext))
        {
            return;
        }

        if (_trackedEditContext is not null)
        {
            _trackedEditContext.OnFieldChanged -= HandleFieldChanged;
        }

        _trackedEditContext = context;
        _dirty = false;
        if (context is not null)
        {
            context.OnFieldChanged += HandleFieldChanged;
        }
    }

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        if (!_dirty)
        {
            _dirty = true;
            StateHasChanged();
        }
    }

    private async Task HandleValidSubmitAsync(EditContext context)
    {
        // The guard reads what it was last rendered with: render it released before the handler runs,
        // so a save that navigates away is never held by its own form.
        _submitting = true;
        StateHasChanged();
        await Task.Yield();
        try
        {
            await OnValidSubmit.InvokeAsync(context);
            _dirty = false;
        }
        finally
        {
            _submitting = false;
        }
    }

    private async Task HandleInvalidSubmitAsync(EditContext context)
    {
        await OnInvalidSubmit.InvokeAsync(context);
        if (FocusOnFirstInvalid)
        {
            try
            {
                _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/OmniEurope.Blazor/omniInterop.js");
                await _module.InvokeVoidAsync("focusFirstInvalid", _formRoot);
            }
            catch (Exception exception) when (exception is JSException or JSDisconnectedException or TaskCanceledException)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogDebug(Logger, exception, "Unable to focus the first invalid form control.");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        Track(null);
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); } catch (JSDisconnectedException) { }
        }
    }
}
