namespace OmniEurope.Blazor.Components;

/// <summary>What the trigger of an <see cref="OmniAppMenu"/> shows of the signed-in user.</summary>
public enum OmniAppMenuUserDisplay
{
    /// <summary>The user's full name, cut with an ellipsis past 12rem and hidden under 40rem. The default.</summary>
    Name,

    /// <summary>The user's initials (<see cref="OmniAppMenu.Initials"/>, else taken from the name) and the chevron; the full name stays in the accessible name, the tooltip and the open menu.</summary>
    Initials,

    /// <summary>The user icon alone, square like any icon-only button; the full name stays in the accessible name, the tooltip and the open menu.</summary>
    Icon
}
