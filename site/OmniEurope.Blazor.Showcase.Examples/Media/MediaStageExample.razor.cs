using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Media;

/// <summary>A media stage of up to seven participants, drawn as coloured placeholders, with the main one bound.</summary>
public partial class MediaStageExample
{
    private static readonly Participant[] Everyone =
    [
        new(1, "Ada", 1),
        new(2, "Grace", 2),
        new(3, "Linus", 3),
        new(4, "Margaret", 4),
        new(5, "Alan", 5),
        new(6, "Katherine", 6),
        new(7, "Dennis", 7)
    ];

    /// <summary>How many participants the stage shows, from 0 to 7: one main item and up to six thumbnails.</summary>
    [Parameter]
    public int Count { get; set; } = 7;

    private Participant? Main { get; set; }

    private IReadOnlyList<Participant> Shown => Everyone.Take(Math.Clamp(Count, 0, Everyone.Length)).ToArray();

    private void ResetMain() => Main = null;

    private sealed record Participant(int Id, string Name, int Tone);
}
