namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A five-star rating bound to a nullable integer, then the same rating read only and disabled.</summary>
public partial class RatingExample
{
    private int? Rating { get; set; } = 3;

    private int? FrozenRating { get; set; } = 3;
}
