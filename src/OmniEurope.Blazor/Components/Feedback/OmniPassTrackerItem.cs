namespace OmniEurope.Blazor.Components;

/// <summary>One element of an <see cref="OmniPassTracker"/>: its name and how far its passes have gone.</summary>
/// <param name="Text">
/// What the element is called under its marks: "Word 1". Must not be blank: it is also the name announced
/// for the element.
/// </param>
/// <param name="Passes">How many passes the element goes through, one mark each. At least one.</param>
/// <param name="Completed">How many of those passes are finished, from zero to <paramref name="Passes"/>.</param>
/// <param name="InProgress">
/// Whether the pass after the finished ones is under way. A finished element has no such pass left, so
/// true is refused when <paramref name="Completed"/> equals <paramref name="Passes"/>.
/// </param>
public sealed record OmniPassTrackerItem(string Text, int Passes, int Completed = 0, bool InProgress = false);
