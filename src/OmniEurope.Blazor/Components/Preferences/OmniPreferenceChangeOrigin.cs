namespace OmniEurope.Blazor.Components;

/// <summary>Where the change reported by <see cref="OmniPreferences.Changed"/> comes from.</summary>
public enum OmniPreferenceChangeOrigin
{
    /// <summary>A write of this circuit, through <see cref="OmniPreferences.SetAsync{T}"/> or <see cref="OmniPreferences.RemoveAsync"/>.</summary>
    Local,

    /// <summary>A later value read from the host's <see cref="IOmniPreferenceStore"/>, written on another device or another session.</summary>
    Store,

    /// <summary>Another tab of the same browser, which wrote the shared browser cache.</summary>
    OtherTab,

    /// <summary>The value of the user who just signed in, read again by <see cref="OmniPreferences.ResetUserAsync"/>; null when that user has none.</summary>
    UserChanged
}
