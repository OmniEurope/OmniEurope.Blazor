using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The host of the package's overlays, placed once around the application: it cascades the overlay
/// service and draws the dialog, the notification stack and the portal of the popups. It draws no
/// element of its own, so it takes no id, class, preset or other attribute.
/// </summary>
public partial class OmniComponentsHost
{
    [Inject]
    private IStringLocalizer<AppStrings> StringLocalizer { get; set; } = default!;

    /// <summary>
    /// The overlay service the host draws; null, the default, makes the host create and own its own.
    /// </summary>
    [Parameter]
    public OmniOverlayService? OverlayService { get; set; }

    /// <summary>
    /// How the notification stack behaves: where it sits, whether each card counts down, whether it
    /// can be dismissed by hand, and whether a burst collapses into one pile.
    /// </summary>
    [Parameter]
    public OmniNotificationOptions Notifications { get; set; } = new();

    /// <summary>The application, which the overlay service cascades to.</summary>
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

    /// <summary>Takes the overlay service asked for, or creates one the first time.</summary>
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

    private string Localize(string name) => StringLocalizer[name].Value;

    /// <summary>Stops listening, and disposes the overlay service when the host created it.</summary>
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
