namespace OmniEurope.Blazor.Components;

/// <summary>
/// How far a task has gone (<c>role="progressbar"</c>), as a line or a ring; indeterminate while the end
/// is not known.
/// </summary>
public partial class OmniProgressBar
{
    // The share of the track the moving indicator of an indeterminate bar covers: drawing only, never
    // a value, so it is neither announced nor written.
    private const double IndeterminateShare = 25;

    /// <summary>The progress, from 0 to <see cref="Maximum"/>; a value outside is drawn at the nearest end.</summary>
    [Parameter]
    public double Value { get; set; }

    /// <summary>The value of a finished task; 100 by default. Must be finite and greater than zero.</summary>
    [Parameter]
    public double Maximum { get; set; } = 100;

    /// <summary>Accessible name of the bar; the localized "Progress" when null or blank.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// The progress in words (<c>aria-valuetext</c>, and the text <see cref="ShowValue"/> writes), for a
    /// value a percentage does not say: "3 of 12 files". Null, the default, reads the percentage.
    /// </summary>
    [Parameter]
    public string? ValueText { get; set; }

    /// <summary>
    /// Writes the progress beside the bar: <see cref="ValueText"/>, else the percentage, else, for an
    /// indeterminate bar, the localized "In progress" (an indeterminate bar has no number to show).
    /// </summary>
    [Parameter]
    public bool ShowValue { get; set; }

    /// <summary>The end is not known: the indicator moves along the track and no value is announced.</summary>
    [Parameter]
    public bool Indeterminate { get; set; }

    /// <summary>The colour intention of the indicator; <see cref="OmniTone.Accent"/> by default.</summary>
    [Parameter]
    public OmniTone Tone { get; set; } = OmniTone.Accent;

    /// <summary>A line (the default) or a ring.</summary>
    [Parameter]
    public OmniProgressShape Shape { get; set; }

    /// <summary>
    /// Several values stacked on the one total, <see cref="Maximum"/>: the bar draws them one after the other
    /// from the start, each in its own <see cref="OmniProgressSegment.Tone"/>, as discovered, known and
    /// mastered words of a lesson. Null or empty, the default, draws the single <see cref="Value"/>. With
    /// segments <see cref="Value"/> and <see cref="Tone"/> are not read, and an <see cref="Indeterminate"/>
    /// bar draws none. The segments are counted in order and together never pass <see cref="Maximum"/>: what
    /// would pass it is cut. A line draws each boundary to the nearest 5 % of the track, like the single
    /// indicator, so a segment under 2.5 % of the total is not drawn; it stays in the announced text and the
    /// legend. The bar announces the sum as its value and, unless <see cref="ValueText"/> says otherwise, each
    /// segment as its label and amount ("Known 8, Mastered 3"). Each segment has its label as tooltip.
    /// </summary>
    [Parameter]
    public IReadOnlyList<OmniProgressSegment>? Segments { get; set; }

    /// <summary>
    /// Lists the <see cref="Segments"/> under the bar with a swatch of their colour, their label and their
    /// amount, so that the colours are not the only way to tell them apart. False by default; without
    /// segments it draws nothing.
    /// </summary>
    [Parameter]
    public bool ShowLegend { get; set; }

    private IReadOnlyList<SegmentPart> _parts = [];

    private bool HasSegments => !Indeterminate && Segments is { Count: > 0 };
    private bool HasLegend => HasSegments && ShowLegend;
    private double SegmentTotal => Math.Round(_parts.Sum(part => part.Amount), 9);
    private double NormalizedValue => HasSegments ? SegmentTotal : Math.Clamp(Value, 0, Maximum);
    private double Percentage => NormalizedValue / Maximum * 100;
    private double DrawnPercentage => Indeterminate ? IndeterminateShare : Percentage;
    private int PercentageBucket => Bucket(DrawnPercentage);
    private string MaximumText => Maximum.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private string? CurrentValueText => Indeterminate ? null : NormalizedValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private string EffectiveLabel => LocalizeOr(Label, "ProgressLabel");
    private string DisplayValue => ValueText ?? (Indeterminate ? Localize("ProgressIndeterminate") : Localize("ProgressValue", Percentage));
    private string DashArray => Indeterminate ? "25 75" : $"{Percentage.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} 100";
    private string LinearIndicatorClass => $"omni-progress__indicator omni-progress__indicator--{PercentageBucket}";

    // What a screen reader gets in place of the number: the host's words, else the segments one by one.
    private string? AccessibleValueText => ValueText ?? (HasSegments ? string.Join(", ", _parts.Select(part => part.Text)) : null);

    // The width of a linear indicator or boundary, rounded to a step of 5 %: the stylesheet has one class per step,
    // since a style attribute would break a strict CSP.
    private static int Bucket(double percentage) =>
        Math.Clamp((int)(Math.Round(percentage / 5, MidpointRounding.AwayFromZero) * 5), 0, 100);

    private static string Number(double value, CultureInfo culture) => value.ToString("0.##", culture);

    /// <summary>
    /// Refuses a <see cref="Maximum"/> that is not positive, values that are not finite, and a segment that is
    /// missing, not finite or without a label.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!double.IsFinite(Maximum) || !double.IsFinite(Value) || Maximum <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Maximum), "Progress values must be finite and Maximum must be greater than zero.");
        }

        _parts = HasSegments ? Layout(Segments!, Maximum) : [];
    }

    // The segments laid end to end on the total. Each boundary is rounded once, from the running sum, so the
    // widths add up to the rounded sum whatever the rounding of each segment on its own.
    private static List<SegmentPart> Layout(IReadOnlyList<OmniProgressSegment> segments, double maximum)
    {
        var parts = new List<SegmentPart>(segments.Count);
        var sum = 0d;
        var previousBoundary = 0;
        foreach (var segment in segments)
        {
            if (segment is null || !double.IsFinite(segment.Value) || string.IsNullOrWhiteSpace(segment.Label))
            {
                throw new ArgumentException("Every progress segment needs a finite value and a label.", nameof(Segments));
            }

            var amount = Math.Clamp(segment.Value, 0, Math.Max(0, maximum - sum));
            var boundary = Bucket((sum + amount) / maximum * 100);
            parts.Add(new SegmentPart(segment, amount, sum / maximum * 100, amount / maximum * 100, boundary - previousBoundary));
            sum += amount;
            previousBoundary = boundary;
        }

        return parts;
    }

    private sealed record SegmentPart(OmniProgressSegment Segment, double Amount, double StartPercent, double LengthPercent, int WidthBucket)
    {
        public string ToneName => Segment.Tone.ToString().ToLowerInvariant();

        public string Text => $"{Segment.Label} {Number(Amount, CultureInfo.CurrentCulture)}";

        // The dash pattern starts at the offset taken away from the start of the path: a negative offset moves it forward.
        public string OffsetText => StartPercent > 0 ? "-" + Number(StartPercent, CultureInfo.InvariantCulture) : "0";

        public string LengthText => Number(LengthPercent, CultureInfo.InvariantCulture);
    }
}
