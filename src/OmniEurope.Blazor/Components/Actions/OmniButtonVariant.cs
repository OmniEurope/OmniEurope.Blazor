namespace OmniEurope.Blazor.Components;

/// <summary>
/// What a button means, not what it looks like. <see cref="Success"/> and <see cref="Warning"/>
/// exist because a destructive action is not the only one worth colouring: confirming and
/// suspending read as two different risks, and a list where they share one colour makes the
/// reader check the tooltip.
/// </summary>
public enum OmniButtonVariant
{
    Primary,
    Secondary,
    Ghost,
    Danger,

    // Appended rather than slotted in beside Danger: the values are already published, and
    // renumbering one would silently change what a compiled consumer means.
    Success,
    Warning
}
