namespace OmniEurope.Blazor.Components;

public enum OmniBadgeVariant
{
    Neutral,
    Accent,
    Success,
    Warning,
    Danger,

    // Appended rather than slotted in beside the other severities: the values are already published,
    // and renumbering one would silently change what a compiled consumer means.
    Info
}
