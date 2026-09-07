namespace OmniEurope.Blazor.Components;

public partial class OmniGridLines
{
    [Parameter] public int Count { get; set; } = 5;

    protected override void OnParametersSet()
    {
        if (Count <= 0) throw new ArgumentOutOfRangeException(nameof(Count), "Count must be greater than zero.");
    }
}
