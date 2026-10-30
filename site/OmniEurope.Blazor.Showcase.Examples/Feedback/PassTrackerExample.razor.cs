using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>A session of five words, three passes each, that advances one pass at a time.</summary>
public partial class PassTrackerExample
{
    private const int WordCount = 5;
    private const int PassCount = 3;

    private static readonly int[] Start = [2, 1, 0, 0, 0];

    private readonly int[] _completed = [.. Start];

    private int? Current { get; set; } = 0;

    private bool Finished => Current is null;

    private IReadOnlyList<OmniPassTrackerItem> Items =>
        [.. _completed.Select((completed, index) =>
            new OmniPassTrackerItem($"Word {index + 1}", PassCount, completed, index == Current && completed < PassCount))];

    private void Advance()
    {
        if (Current is not { } word)
        {
            return;
        }

        _completed[word]++;
        var next = (word + 1) % WordCount;
        var remaining = Enumerable.Range(0, WordCount).Select(offset => (next + offset) % WordCount)
            .Where(index => _completed[index] < PassCount).ToList();
        Current = remaining.Count == 0 ? null : remaining[0];
    }

    private void Reset()
    {
        Start.CopyTo(_completed, 0);
        Current = 0;
    }
}
