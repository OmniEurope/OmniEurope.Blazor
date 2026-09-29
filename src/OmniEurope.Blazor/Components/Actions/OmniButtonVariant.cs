namespace OmniEurope.Blazor.Components;

/// <summary>
/// What a button means, not what it looks like. <see cref="Success"/> and <see cref="Warning"/>
/// exist because a destructive action is not the only one worth colouring: confirming and
/// suspending read as two different risks, and a list where they share one colour makes the
/// reader check the tooltip.
/// </summary>
public enum OmniButtonVariant
{
    /// <summary>The main action of a view, on the accent fill. The default of <see cref="OmniButton"/>.</summary>
    Primary,

    /// <summary>A secondary action, on a plain neutral fill with the page text.</summary>
    Secondary,

    /// <summary>A quiet action with no fill and no relief, in the accent text colour, tinted only on hover and press.</summary>
    Ghost,

    /// <summary>A destructive or irreversible action, on the deep danger fill.</summary>
    Danger,

    // Appended rather than slotted in beside Danger: the values are already published, and
    // renumbering one would silently change what a compiled consumer means.

    /// <summary>An action that confirms or completes, on the bright success fill.</summary>
    Success,

    /// <summary>An action that carries a risk short of destruction, such as suspending, on the deep warning fill.</summary>
    Warning,

    // Information, the fourth severity: an action that opens or explains rather than commits.
    // Appended for the same reason as the two above.

    /// <summary>An action that opens or explains rather than commits, on the bright information fill.</summary>
    Info
}
