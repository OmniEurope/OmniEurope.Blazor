using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="InkStrokeCodec"/>, the flat form in which strokes cross to <c>omni-ink-canvas.js</c> and come back:
/// a stroke packs to numbers and unpacks to the same stroke, and what the script reports is checked before it
/// becomes a stroke.
/// </summary>
public sealed class InkStrokeCodecTests
{
    private static readonly double[] OnePoint = [0.5, 0.5, -1];

    [Fact]
    public void Pack_WritesOneColourWidthAndFlatPointArrayPerStroke_NoPressureAsMinusOne()
    {
        OmniInkStroke[] strokes =
        [
            new("#112233", 2, [new OmniInkPoint(0.1, 0.2, null), new OmniInkPoint(0.3, 0.4, 0.5)]),
            new(OmniInkStroke.ThemeColor, 6, [new OmniInkPoint(1, 1, 0)])
        ];

        var (colors, widths, points) = InkStrokeCodec.Pack(strokes);

        Assert.Equal(["#112233", "currentColor"], colors);
        Assert.Equal([2d, 6d], widths);
        Assert.Equal([0.1, 0.2, -1d, 0.3, 0.4, 0.5], points[0]);
        Assert.Equal([1d, 1d, 0d], points[1]);
    }

    [Fact]
    public void Pack_OfNothing_IsEmpty()
    {
        var (colors, widths, points) = InkStrokeCodec.Pack([]);

        Assert.Empty(colors);
        Assert.Empty(widths);
        Assert.Empty(points);
    }

    [Fact]
    public void TryUnpack_ReadsWhatPackWrote()
    {
        OmniInkStroke original = new("#112233", 2.5, [new OmniInkPoint(0.1, 0.2, null), new OmniInkPoint(0.3, 0.4, 0.5)]);
        var (_, _, points) = InkStrokeCodec.Pack([original]);

        var ok = InkStrokeCodec.TryUnpack("#112233", 2.5, points[0], out var stroke);

        Assert.True(ok);
        Assert.Equal(original.Color, stroke.Color);
        Assert.Equal(original.Width, stroke.Width);
        Assert.Equal(original.Points, stroke.Points);
    }

    [Fact]
    public void TryUnpack_BringsCoordinatesBackIntoTheArea_AndPressureIntoZeroToOne()
    {
        var ok = InkStrokeCodec.TryUnpack("red", 3, [-0.2, 1.7, 2, 0.5, 0.5, -0.0001], out var stroke);

        Assert.True(ok);
        Assert.Equal(new OmniInkPoint(0, 1, 1), stroke.Points[0]);
        Assert.Equal(new OmniInkPoint(0.5, 0.5, null), stroke.Points[1]);
    }

    [Theory]
    [InlineData(0.001, InkStrokeCodec.MinWidth)]
    [InlineData(2, 2)]
    [InlineData(500, InkStrokeCodec.MaxWidth)]
    public void TryUnpack_KeepsTheWidthWithinTheAllowedRange(double width, double expected)
    {
        Assert.True(InkStrokeCodec.TryUnpack("red", width, OnePoint, out var stroke));

        Assert.Equal(expected, stroke.Width);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryUnpack_TurnsAMissingColourIntoTheThemeColour(string? color)
    {
        Assert.True(InkStrokeCodec.TryUnpack(color, 2, OnePoint, out var stroke));

        Assert.Equal(OmniInkStroke.ThemeColor, stroke.Color);
    }

    [Fact]
    public void TryUnpack_TurnsAnExcessivelyLongColourIntoTheThemeColour_AndKeepsOneOfTheLimit()
    {
        Assert.True(InkStrokeCodec.TryUnpack(new string('a', 65), 2, OnePoint, out var tooLong));
        Assert.True(InkStrokeCodec.TryUnpack(new string('a', 64), 2, OnePoint, out var atTheLimit));

        Assert.Equal(OmniInkStroke.ThemeColor, tooLong.Color);
        Assert.Equal(new string('a', 64), atTheLimit.Color);
    }

    [Fact]
    public void TryUnpack_RefusesWhatCannotBeAStroke()
    {
        Assert.False(InkStrokeCodec.TryUnpack("red", 2, null, out _));
        Assert.False(InkStrokeCodec.TryUnpack("red", 2, [], out _));
        Assert.False(InkStrokeCodec.TryUnpack("red", 2, [0.1, 0.2], out _));
        Assert.False(InkStrokeCodec.TryUnpack("red", 2, [0.1, 0.2, 0.3, 0.4], out _));
        Assert.False(InkStrokeCodec.TryUnpack("red", double.NaN, OnePoint, out _));
        Assert.False(InkStrokeCodec.TryUnpack("red", double.PositiveInfinity, OnePoint, out _));
        Assert.False(InkStrokeCodec.TryUnpack("red", 2, [0.1, double.NaN, -1], out _));
        Assert.False(InkStrokeCodec.TryUnpack("red", 2, [0.1, 0.2, double.NegativeInfinity], out _));
    }

    [Fact]
    public void TryUnpack_RefusesMoreThanTwentyThousandPoints_ButAcceptsThatMany()
    {
        Assert.True(InkStrokeCodec.TryUnpack("red", 2, new double[20_000 * 3], out var most));
        Assert.False(InkStrokeCodec.TryUnpack("red", 2, new double[(20_000 * 3) + 3], out _));

        Assert.Equal(20_000, most.Points.Count);
    }
}
