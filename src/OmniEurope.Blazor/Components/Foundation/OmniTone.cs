namespace OmniEurope.Blazor.Components;

/// <summary>
/// The colour intention of a component: <see cref="OmniBadge"/>, <see cref="OmniProgressBar"/>, the
/// intention of <see cref="OmniDialog"/> and a menu item. <see cref="OmniSeverity"/> stays the severity of
/// a message; <see cref="OmniButtonVariant"/> stays the emphasis of a button.
/// </summary>
public enum OmniTone
{
    /// <summary>No colour intention: the neutral text and surface colours. The default.</summary>
    Neutral,

    /// <summary>The accent colour of the palette.</summary>
    Accent,

    /// <summary>Information.</summary>
    Info,

    /// <summary>A success.</summary>
    Success,

    /// <summary>Something that needs attention.</summary>
    Warning,

    /// <summary>A failure, a refusal or a destructive action.</summary>
    Danger
}
