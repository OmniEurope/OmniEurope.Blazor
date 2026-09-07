namespace OmniEurope.Blazor.Components;

public partial class OmniComponentsHost
{
    [Parameter]
    public OmniOverlayService? OverlayService { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private OmniOverlayService? _service;
    private OmniOverlayService Service => _service ?? throw new InvalidOperationException("The overlay service has not been initialized.");
    private readonly OmniOverlayCoordinator _coordinator = new();
    private bool _ownsService;

    protected override void OnInitialized()
    {
        _coordinator.Changed += HandleChanged;
    }

    protected override void OnParametersSet()
    {
        var requested = OverlayService;
        if (requested is null && _ownsService && _service is not null)
        {
            return;
        }

        if (requested is not null && ReferenceEquals(_service, requested))
        {
            return;
        }

        SwitchService(requested ?? new OmniOverlayService(), requested is null);
    }

    private void SwitchService(OmniOverlayService next, bool ownsNext)
    {
        if (_service is not null)
        {
            _service.Changed -= HandleChanged;
            if (_ownsService)
            {
                _service.Dispose();
            }
        }

        _service = next;
        _ownsService = ownsNext;
        _service.Changed += HandleChanged;
    }

    private Task HandleDialogOpenChanged(bool open)
    {
        if (!open)
        {
            Service.CloseDialog();
        }

        return Task.CompletedTask;
    }

    private void HandleChanged() => _ = InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_service is not null)
        {
            _service.Changed -= HandleChanged;
        }
        _coordinator.Changed -= HandleChanged;
        if (_ownsService && _service is not null)
        {
            _service.Dispose();
        }
    }
}
