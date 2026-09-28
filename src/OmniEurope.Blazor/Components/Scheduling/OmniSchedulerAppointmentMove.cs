namespace OmniEurope.Blazor.Components;

/// <summary>An appointment of <see cref="OmniScheduler"/> dropped somewhere else, with where it now starts and ends.</summary>
/// <param name="Appointment">The appointment as the host gave it, unchanged.</param>
/// <param name="Start">Its new start, with the offset of the scheduler's time zone on that day.</param>
/// <param name="End">Its new end: the new start plus the appointment's duration.</param>
public sealed record OmniSchedulerAppointmentMove(
    OmniSchedulerAppointment Appointment,
    DateTimeOffset Start,
    DateTimeOffset End);
