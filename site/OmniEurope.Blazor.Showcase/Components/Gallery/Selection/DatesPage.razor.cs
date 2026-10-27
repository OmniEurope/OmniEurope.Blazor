using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Selection;

/// <summary>The Dates page: the bounds, seconds and state of a date and a date-time picker, then more pickers, a time picker, pickers in a form and in a dialog.</summary>
public partial class DatesPage
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    [Inject]
    private IStringLocalizer<SelectionStrings> Text { get; set; } = default!;

    private DateOnly? MinimumBound { get; set; } = Today.AddDays(-15);

    private DateOnly? MaximumBound { get; set; } = Today.AddDays(45);

    private bool ShowSeconds { get; set; }

    private bool PickersReadOnly { get; set; }

    private bool PickersDisabled { get; set; }
}
