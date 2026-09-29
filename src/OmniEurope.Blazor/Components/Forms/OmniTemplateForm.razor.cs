namespace OmniEurope.Blazor.Components;

/// <summary>
/// A form over a model or an edit context: validation, focus on the first invalid field after an invalid
/// submit and, on request, a guard against leaving with unsaved changes. The <c>form</c> element carries
/// <see cref="OmniComponentBase.Id"/>, <see cref="OmniComponentBase.Class"/> and the additional
/// attributes.
/// </summary>
/// <typeparam name="TModel">The type of the edited model.</typeparam>
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

    /// <summary>The edited model; give it or <see cref="EditContext"/>, not both.</summary>
    [Parameter]
    public TModel? Model { get; set; }

    /// <summary>The edit context of the form; give it or <see cref="Model"/>, not both.</summary>
    [Parameter]
    public EditContext? EditContext { get; set; }

    /// <summary>The fields, given the edit context.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<EditContext> ChildContent { get; set; } = default!;

    /// <summary>Raised by a submit that passes validation.</summary>
    [Parameter]
    public EventCallback<EditContext> OnValidSubmit { get; set; }

    /// <summary>Raised by a submit that fails validation, before the focus moves to the first invalid field.</summary>
    [Parameter]
    public EventCallback<EditContext> OnInvalidSubmit { get; set; }

    /// <summary>The name of the form for static server rendering (<see cref="EditForm.FormName"/>).</summary>
    [Parameter]
    public string? FormName { get; set; }

    /// <summary>Moves the focus to the first invalid field after an invalid submit; on by default.</summary>
    [Parameter]
    public bool FocusOnFirstInvalid { get; set; } = true;

    /// <summary>
    /// Whether leaving the page once a field has changed asks first (<see cref="OmniUnsavedChangesGuard"/>):
    /// a navigation inside the application waits for a confirmation, and closing or reloading the tab
    /// raises the browser's question. Off by default, opt-in: a page that already guards its changes
    /// would otherwise ask twice. A valid submit counts as saved: the question stops until a field
    /// changes again, and a navigation the submit handler starts is never held.
    /// </summary>
    [Parameter]
    public bool GuardUnsavedChanges { get; set; }

    /// <summary>Refuses zero or two sources, and starts tracking the changes of a new edit context.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
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
                _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.Interop);
                await _module.InvokeVoidAsync("focusFirstInvalid", _formRoot);
            }
            catch (Exception exception) when (exception is JSException or JSDisconnectedException or TaskCanceledException)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogDebug(Logger, exception, "Unable to focus the first invalid form control.");
            }
        }
    }

    /// <summary>Stops tracking the edit context and releases the focus script.</summary>
    public async ValueTask DisposeAsync()
    {
        Track(null);
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); } catch (JSDisconnectedException) { }
        }
    }
}
