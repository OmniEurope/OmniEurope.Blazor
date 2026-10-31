namespace OmniEurope.Blazor.Components;

/// <summary>Two appointments that share some time, the one that starts first in <see cref="First"/>.</summary>
/// <param name="First">The appointment that starts first.</param>
/// <param name="Second">The appointment that starts inside it.</param>
public sealed record OmniSchedulerOverlap(OmniSchedulerAppointment First, OmniSchedulerAppointment Second)
{
    /// <summary>
    /// Finds every pair of appointments that share some time, with the rule the time grid of
    /// <see cref="OmniScheduler"/> draws: one ending when the other starts does not overlap it. The host
    /// calls it on the appointments it gives, to flag a conflict its own way.
    /// </summary>
    /// <param name="appointments">The appointments, in any order and any time zone.</param>
    /// <returns>The pairs, in the order their second appointment starts.</returns>
    public static IReadOnlyList<OmniSchedulerOverlap> Find(IEnumerable<OmniSchedulerAppointment> appointments)
    {
        ArgumentNullException.ThrowIfNull(appointments);

        var overlaps = new List<OmniSchedulerOverlap>();
        var open = new List<OmniSchedulerAppointment>();
        foreach (var appointment in appointments.OrderBy(item => item.Start).ThenBy(item => item.End))
        {
            open.RemoveAll(item => item.End <= appointment.Start);
            overlaps.AddRange(open.Where(item => item.Start < appointment.End).Select(item => new OmniSchedulerOverlap(item, appointment)));
            open.Add(appointment);
        }

        return overlaps;
    }
}
