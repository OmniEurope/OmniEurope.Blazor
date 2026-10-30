namespace OmniEurope.Blazor.Components;

/// <summary>
/// Where a series of elements stands when each goes through several passes: five words of a lesson, three
/// passes each, shown as five small groups of marks rather than one count of fifteen steps. It only
/// reports: nothing in it can be clicked. <see cref="OmniSteps"/> is a navigation through steps and
/// <see cref="OmniProgressBar"/> holds a single total, stacked or not.
/// </summary>
public partial class OmniPassTracker
{
    /// <summary>The elements, in order. Each one is checked when the list is set.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniPassTrackerItem> Items { get; set; } = [];

    /// <summary>
    /// The position in <see cref="Items"/> of the element being worked on: its name stands out and it
    /// carries <c>aria-current="step"</c>. Null, the default, marks none, as when the series is over.
    /// </summary>
    [Parameter]
    public int? CurrentIndex { get; set; }

    /// <summary>The accessible name of the list. Null or blank uses the localized "Progress".</summary>
    [Parameter]
    public string? Label { get; set; }

    private string EffectiveLabel => LocalizeOr(Label, "ProgressLabel");

    /// <summary>Refuses a list the marks could not draw truthfully.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        foreach (var item in Items)
        {
            Check(item);
        }

        if (CurrentIndex is { } index && (index < 0 || index >= Items.Count))
        {
            throw new ArgumentOutOfRangeException(nameof(CurrentIndex), index, "The current element is not in the list.");
        }
    }

    private static void Check(OmniPassTrackerItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Text))
        {
            throw new ArgumentException("An element needs a name: the marks alone say nothing to a screen reader.", nameof(Items));
        }

        if (item.Passes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Items), item.Passes, $"'{item.Text}' has no pass.");
        }

        if (item.Completed < 0 || item.Completed > item.Passes)
        {
            throw new ArgumentOutOfRangeException(nameof(Items), item.Completed, $"'{item.Text}' has {item.Passes} passes.");
        }

        if (item.InProgress && item.Completed == item.Passes)
        {
            throw new ArgumentException($"'{item.Text}' has finished its passes: none can be under way.", nameof(Items));
        }
    }

    private static string ItemClass(bool current) =>
        current ? "omni-pass-tracker__item omni-pass-tracker__item--current" : "omni-pass-tracker__item";

    private static string MarkClass(OmniPassTrackerItem item, int pass)
    {
        if (pass < item.Completed)
        {
            return "omni-pass-tracker__mark omni-pass-tracker__mark--done";
        }

        return item.InProgress && pass == item.Completed
            ? "omni-pass-tracker__mark omni-pass-tracker__mark--active"
            : "omni-pass-tracker__mark";
    }

    private string Announcement(OmniPassTrackerItem item) =>
        Localize(item.InProgress ? "PassTrackerItemInProgress" : "PassTrackerItem", item.Text, item.Completed, item.Passes);
}
