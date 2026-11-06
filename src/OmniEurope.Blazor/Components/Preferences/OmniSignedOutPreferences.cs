namespace OmniEurope.Blazor.Components;

/// <summary>What <see cref="OmniPreferences"/> does while the host's <see cref="IOmniPreferenceStore"/> names no signed-in user (<see cref="OmniPreferences.NoUserScope"/>).</summary>
public enum OmniSignedOutPreferences
{
    /// <summary>Every preference reads as its default and a value written meanwhile stays in the circuit's memory only, never in the browser cache nor in the store.</summary>
    Defaults,

    /// <summary>
    /// The visitor's preferences live in the browser alone, in a cache of their own that the store never sees.
    /// On sign-in, each preference the visitor changed goes to the account unless the account already holds one (which wins);
    /// the account's values are also kept in that cache, so the browser keeps them after sign-out.
    /// </summary>
    Browser
}
