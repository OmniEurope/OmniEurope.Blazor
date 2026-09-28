namespace OmniEurope.Blazor.Components;

/// <summary>
/// The severity of a message, shared by <see cref="OmniAlert"/>, <see cref="OmniNotification"/> and
/// <see cref="OmniOverlayService.Notify(string, OmniSeverity, string?, TimeSpan?)"/>. The member names
/// are those of the intentions of <see cref="OmniButtonVariant"/> and <see cref="OmniBadgeVariant"/>.
/// </summary>
public enum OmniSeverity
{
    /// <summary>Neutral information, the default.</summary>
    Info,

    /// <summary>An operation that succeeded.</summary>
    Success,

    /// <summary>Something that needs attention without having failed.</summary>
    Warning,

    /// <summary>A failure or a refusal. A notification of this severity is announced assertively.</summary>
    Danger
}
