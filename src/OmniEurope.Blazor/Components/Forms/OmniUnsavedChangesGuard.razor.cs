using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Warns before the user leaves a page whose changes are not saved: a navigation inside the
/// application waits for a confirmation, and leaving the application (closing the tab, reloading)
/// raises the browser's own question. <see cref="OmniTemplateForm{TModel}"/> places one by default;
/// place one yourself for changes a form does not see (a list edited in place, a drawing).
/// </summary>
public partial class OmniUnsavedChangesGuard
{
    [Inject]
    private IStringLocalizer<AppStrings> StringLocalizer { get; set; } = default!;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The overlay service of the enclosing <see cref="OmniComponentsHost"/>, which draws the question.</summary>
    [CascadingParameter]
    private OmniOverlayService? Overlays { get; set; }

    /// <summary>Whether there are changes to lose. False lets every navigation through at once.</summary>
    [Parameter]
    public bool HasChanges { get; set; }

    /// <summary>Title of the question; the localized "Unsaved changes" when empty.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The question; the localized "Leave the page without saving your changes?" when empty.</summary>
    [Parameter]
    public string? Message { get; set; }

    /// <summary>Label of the button that leaves; the localized "Leave without saving" when empty.</summary>
    [Parameter]
    public string? LeaveText { get; set; }

    /// <summary>Label of the button that stays; the localized "Stay on the page" when empty.</summary>
    [Parameter]
    public string? StayText { get; set; }

    private string Text(string? value, string key) => string.IsNullOrWhiteSpace(value) ? StringLocalizer[key].Value : value;

    private async Task ConfirmLeaveAsync(LocationChangingContext context)
    {
        if (!HasChanges)
        {
            return;
        }

        var message = Text(Message, "UnsavedChangesMessage");
        bool leave;
        if (Overlays is not null)
        {
            leave = await Overlays.ConfirmAsync(new OmniConfirmRequest(Text(Title, "UnsavedChangesTitle"), message)
            {
                ConfirmText = Text(LeaveText, "UnsavedChangesLeave"),
                CancelText = Text(StayText, "UnsavedChangesStay"),
                ConfirmVariant = OmniButtonVariant.Danger,
                ConfirmIcon = OmniIconName.SignOut
            });
        }
        else
        {
            leave = await JavaScript.InvokeAsync<bool>("confirm", context.CancellationToken, message);
        }

        if (!leave)
        {
            context.PreventNavigation();
        }
    }
}
