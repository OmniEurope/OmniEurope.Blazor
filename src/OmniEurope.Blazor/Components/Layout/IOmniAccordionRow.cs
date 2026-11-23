namespace OmniEurope.Blazor.Components;

/// <summary>
/// What an <see cref="OmniAccordionContext"/> needs from a registered row to close it when a peer
/// opens in single mode: whether it is open, and the close that reports the change.
/// </summary>
internal interface IOmniAccordionRow
{
    /// <summary>Whether the row's body is shown.</summary>
    bool IsOpen { get; }

    /// <summary>Closes the row because a peer opened, reporting the change to a bound value.</summary>
    Task CloseFromPeerAsync();
}
