namespace OmniEurope.Blazor.Components;

public partial class OmniTemplateForm<TModel>
where TModel : class
{
    private EditContext? _resolvedEditContext;
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
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); } catch (JSDisconnectedException) { }
        }
    }
}
