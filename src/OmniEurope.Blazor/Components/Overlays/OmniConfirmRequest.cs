namespace OmniEurope.Blazor.Components;

/// <summary>
/// A question put to the user by <see cref="OmniOverlayService.ConfirmAsync(OmniConfirmRequest)"/>.
/// The dialog follows the package's button convention: the action first, cancelling after it in the
/// neutral <see cref="OmniButtonVariant.Secondary"/>, both at the end of the footer, each with its icon.
/// </summary>
/// <param name="Title">The dialog title.</param>
/// <param name="Message">The question, shown as the dialog's content.</param>
public sealed record OmniConfirmRequest(string Title, string Message)
{
    /// <summary>The action's label. Empty: the localized "Confirmer".</summary>
    public string ConfirmText { get; init; } = string.Empty;

    /// <summary>The cancel button's label. Empty: the localized "Annuler".</summary>
    public string CancelText { get; init; } = string.Empty;

    /// <summary>
    /// The action's variant: <see cref="OmniButtonVariant.Primary"/> by default,
    /// <see cref="OmniButtonVariant.Danger"/> for a destructive action.
    /// </summary>
    public OmniButtonVariant ConfirmVariant { get; init; } = OmniButtonVariant.Primary;

    /// <summary>
    /// Variant of the cancel button: the neutral <see cref="OmniButtonVariant.Secondary"/> by default,
    /// because cancelling is the safe way out and must not look like the dangerous choice.
    /// </summary>
    public OmniButtonVariant CancelVariant { get; init; } = OmniButtonVariant.Secondary;

    /// <summary>The action's icon; cancelling always carries <see cref="OmniIconName.Close"/>.</summary>
    public OmniIconName ConfirmIcon { get; init; } = OmniIconName.Check;
}
