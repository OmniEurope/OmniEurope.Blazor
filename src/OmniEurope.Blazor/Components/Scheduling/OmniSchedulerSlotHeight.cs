namespace OmniEurope.Blazor.Components;

/// <summary>
/// The height of one slot of the time grid of an <see cref="OmniScheduler"/>; an appointment drawn on its
/// duration follows it.
/// </summary>
public enum OmniSchedulerSlotHeight
{
    /// <summary>One control height and a small gap per slot: room for the time and the title of an appointment of one slot. The default.</summary>
    Standard,

    /// <summary>Half of <see cref="Standard"/>, to see a long day on a large screen. A block shows its title alone under two slots, its time and one line of title under four, as a standard one does at the same height.</summary>
    Compact
}
