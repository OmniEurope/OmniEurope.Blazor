namespace OmniEurope.Blazor.Components;

/// <summary>
/// How the page-level loading bar reports a load.
/// </summary>
public enum OmniLoadingBarMode
{
    /// <summary>
    /// One sweep from one edge to the other. Honest when the work reports its own progress, and a
    /// reasonable stand-in when it does not, since the sweep ends when the work does.
    /// </summary>
    Sweep,

    /// <summary>
    /// A bar that keeps filling for as long as the load lasts, then snaps to full and goes. Suits
    /// work whose length is unknown, which would otherwise need a sweep timed on nothing.
    /// </summary>
    Continuous
}
