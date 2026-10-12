namespace OmniEurope.Blazor.Components;

/// <summary>
/// One action that cannot be undone, as a tile of an <see cref="OmniSettingsDangerZone"/>: its name and
/// what it destroys, and an <see cref="OmniButtonVariant.Danger"/> button. The button asks first, through
/// <see cref="OmniOverlayService.ConfirmAsync(OmniConfirmRequest)"/> (the browser's own question without
/// an <see cref="OmniComponentsHost"/>); only the confirmation runs <see cref="OnConfirm"/>, and the
/// button stays busy until it completes. Cancelling, Escape, the close button and the backdrop do
/// nothing, and the dialog gives the focus back to the button.
/// </summary>
public partial class OmniSettingsDangerAction
{
    private readonly string _generatedId = $"omni-settings-danger-action-{Guid.NewGuid():N}";
    private bool _asking;
    private bool _busy;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The overlay service of the enclosing <see cref="OmniComponentsHost"/>, which draws the question.</summary>
    [CascadingParameter]
    private OmniOverlayService? Overlays { get; set; }

    /// <summary>The name of the action (<c>Delete the project</c>), on the tile and as the dialog's title.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>What the action destroys, under its name; it also describes the button.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>The button's visible text. Null, the default, is the localized "Delete".</summary>
    [Parameter]
    public string? ActionText { get; set; }

    /// <summary>
    /// The icon of the button and of the dialog's action, <see cref="OmniIconName.Delete"/> by default; an
    /// action that deletes nothing (a reset, a transfer) takes its own.
    /// </summary>
    [Parameter]
    public OmniIconName ActionIcon { get; set; } = OmniIconName.Delete;

    /// <summary>The dialog's title. Null, the default, is <see cref="Title"/>.</summary>
    [Parameter]
    public string? ConfirmTitle { get; set; }

    /// <summary>
    /// The question, best naming what is lost (<c>The project "North" and its 42 reports will be deleted.</c>).
    /// Null, the default, is the localized "This action cannot be undone. Do you want to continue?".
    /// </summary>
    [Parameter]
    public string? ConfirmMessage { get; set; }

    /// <summary>The label of the dialog's action. Null, the default, is the button's text.</summary>
    [Parameter]
    public string? ConfirmText { get; set; }

    /// <summary>
    /// Carries the action out once the user confirmed; the button is busy until the returned task
    /// completes, and a second click meanwhile is ignored.
    /// </summary>
    [Parameter]
    public EventCallback OnConfirm { get; set; }

    /// <summary>
    /// Raised with the exception when <see cref="OnConfirm"/> fails, for the host to say so. Without a
    /// handler the exception propagates to the renderer. A cancellation is never reported here.
    /// </summary>
    [Parameter]
    public EventCallback<Exception> OnConfirmError { get; set; }

    /// <summary>Disables the button, for an action the user may not take.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private string BaseId => Id ?? _generatedId;

    private string TitleId => $"{BaseId}-title";

    private string DescriptionId => $"{BaseId}-description";

    private bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    private string DescribedBy => HasDescription ? $"{TitleId} {DescriptionId}" : TitleId;

    private string ActionLabel => LocalizeOr(ActionText, "SettingsDangerActionText");

    // The button itself ignores a click while it is disabled or busy; while the question is open it is
    // neither, so a second click (the browser's own question, a quick double click) asks nothing more.
    private async Task ConfirmAsync()
    {
        if (_asking)
        {
            return;
        }

        bool confirmed;
        _asking = true;
        try
        {
            confirmed = await AskAsync();
        }
        finally
        {
            _asking = false;
        }

        if (!confirmed)
        {
            return;
        }

        _busy = true;
        StateHasChanged();
        try
        {
            await OnConfirm.InvokeAsync();
        }
        catch (Exception exception) when (OnConfirmError.HasDelegate && exception is not OperationCanceledException)
        {
            await OnConfirmError.InvokeAsync(exception);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task<bool> AskAsync()
    {
        var message = LocalizeOr(ConfirmMessage, "SettingsDangerConfirmMessage");
        if (Overlays is null)
        {
            return await JavaScript.InvokeAsync<bool>("confirm", message);
        }

        return await Overlays.ConfirmAsync(new OmniConfirmRequest(string.IsNullOrWhiteSpace(ConfirmTitle) ? Title : ConfirmTitle, message)
        {
            ConfirmText = string.IsNullOrWhiteSpace(ConfirmText) ? ActionLabel : ConfirmText,
            ConfirmVariant = OmniButtonVariant.Danger,
            ConfirmIcon = ActionIcon
        });
    }
}
