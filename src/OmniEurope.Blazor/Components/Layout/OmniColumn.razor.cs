namespace OmniEurope.Blazor.Components;

/// <summary>
/// A column of an <see cref="OmniRow"/> on a twelve-part grid, whose share of the row can change with the
/// width of the window. Every span must be between 1 and 12, or the component throws.
/// </summary>
public partial class OmniColumn
{
    /// <summary>The content of the column. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The share of the row, in twelfths, at every width; 12 (the whole row) by default.</summary>
    [Parameter]
    public int Span { get; set; } = 12;

    /// <summary>The share of the row from a 40rem wide window up, over <see cref="Span"/>; null, the default, keeps it.</summary>
    [Parameter]
    public int? SmallSpan { get; set; }

    /// <summary>The share of the row from a 64rem wide window up, over the smaller spans; null, the default, keeps them.</summary>
    [Parameter]
    public int? MediumSpan { get; set; }

    /// <summary>The share of the row from an 80rem wide window up, over the smaller spans; null, the default, keeps them.</summary>
    [Parameter]
    public int? LargeSpan { get; set; }

    /// <summary>Rejects any span outside 1 to 12 with an <see cref="ArgumentOutOfRangeException"/>.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ValidateSpan(Span, nameof(Span));
        ValidateSpan(SmallSpan, nameof(SmallSpan));
        ValidateSpan(MediumSpan, nameof(MediumSpan));
        ValidateSpan(LargeSpan, nameof(LargeSpan));
    }

    private static string? SpanClass(string breakpoint, int? value) =>
        value is null ? null : $"omni-column--{breakpoint}-{value}";

    private static void ValidateSpan(int? value, string parameterName)
    {
        if (value is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Column spans must be between 1 and 12.");
        }
    }
}
