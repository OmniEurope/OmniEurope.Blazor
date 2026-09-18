namespace OmniEurope.Blazor.Components;

/// <summary>The state of the application's live connection, as <see cref="OmniConnectionOverlay"/> shows it.</summary>
public enum OmniConnectionState
{
    /// <summary>Connected: the overlay is not rendered.</summary>
    Connected,

    /// <summary>Lost, and an attempt is running or scheduled: the countdown shows when the next one starts.</summary>
    Reconnecting,

    /// <summary>The automatic attempts are over: only a manual attempt remains.</summary>
    Failed,

    /// <summary>The server refused to resume the session: reloading the page is the way back.</summary>
    Rejected
}
