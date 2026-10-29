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
    /// The action's icon as markup (in general an <see cref="OmniIcon"/>, decorative), for a host that draws
    /// its own glyph. Takes the place of <see cref="ConfirmIcon"/> when set; null, the default, keeps it.
    /// </summary>
    public RenderFragment? ConfirmIconContent { get; init; }

    /// <summary>
    /// The dialog's intention (<see cref="OmniDialog.Intent"/>). Null, the default, derives it from
    /// <see cref="ConfirmVariant"/>: <see cref="OmniTone.Warning"/> for a
    /// <see cref="OmniButtonVariant.Danger"/> action, <see cref="OmniTone.Accent"/> otherwise.
    /// <see cref="OmniTone.Neutral"/> draws the dialog without tint or mark.
    /// </summary>
    public OmniTone? Intent { get; init; }

    /// <summary>
    /// What the user has to do before confirming, shown under <see cref="Message"/> (typing the current
    /// password, ticking a box). The content is the host's: the dialog reads nothing of it, stores nothing
    /// of it and logs nothing of it. A request with details puts the focus on the first field of the
    /// dialog when it opens (<see cref="OmniDialogInitialFocus.FirstFocusable"/>). Null, the default, shows
    /// the message alone.
    /// </summary>
    public RenderFragment? Details { get; init; }

    /// <summary>
    /// Whether the action can be chosen now, read each time the dialog draws: while it returns false the
    /// action button is disabled and the question is never answered true. Null, the default, always
    /// allows it. The dialog redraws when the host calls <see cref="Refresh"/>.
    /// </summary>
    public Func<bool>? CanConfirm { get; init; }

    private Action? _refreshed;

    internal event Action? Refreshed
    {
        add => _refreshed += value;
        remove => _refreshed -= value;
    }

    /// <summary>
    /// Tells the dialog that what <see cref="Details"/> and <see cref="CanConfirm"/> read has changed: it
    /// redraws the details and the action button. Does nothing when the dialog is not open.
    /// </summary>
    public void Refresh() => _refreshed?.Invoke();

    internal bool IsConfirmAllowed => CanConfirm?.Invoke() ?? true;

    internal OmniTone EffectiveIntent => Intent
        ?? (ConfirmVariant == OmniButtonVariant.Danger ? OmniTone.Warning : OmniTone.Accent);
}
