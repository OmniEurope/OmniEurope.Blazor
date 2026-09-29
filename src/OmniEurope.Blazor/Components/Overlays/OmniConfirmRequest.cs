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
    /// <summary>The action's label. Null, the default, is the localized "Confirm".</summary>
    public string? ConfirmText { get; init; }

    /// <summary>The cancel button's label. Null, the default, is the localized "Cancel".</summary>
    public string? CancelText { get; init; }

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

    /// <summary>
    /// The dialog's intention (<see cref="OmniDialog.Intent"/>). Null, the default, derives it from
    /// <see cref="ConfirmVariant"/>: <see cref="OmniTone.Warning"/> for a
    /// <see cref="OmniButtonVariant.Danger"/> action, <see cref="OmniTone.Accent"/> otherwise.
    /// <see cref="OmniTone.Neutral"/> draws the dialog without tint or mark.
    /// </summary>
    public OmniTone? Intent { get; init; }

    internal OmniTone EffectiveIntent => Intent
        ?? (ConfirmVariant == OmniButtonVariant.Danger ? OmniTone.Warning : OmniTone.Accent);
}
