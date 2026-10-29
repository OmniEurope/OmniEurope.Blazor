namespace OmniEurope.Blazor.Components;

/// <summary>
/// One share of the total of an <see cref="OmniProgressBar"/> that stacks several values, such as the
/// words discovered, known and mastered of a lesson.
/// </summary>
/// <param name="Value">
/// The amount this segment holds, in the units of <see cref="OmniProgressBar.Maximum"/>. A negative value
/// counts as zero. Must be finite.
/// </param>
/// <param name="Label">
/// What the segment stands for: its name in the legend, its tooltip and the text announced for the bar.
/// Must not be blank, since a colour alone says nothing to a screen reader.
/// </param>
/// <param name="Tone">The colour intention of the segment; <see cref="OmniTone.Accent"/> by default.</param>
public sealed record OmniProgressSegment(double Value, string Label, OmniTone Tone = OmniTone.Accent);
