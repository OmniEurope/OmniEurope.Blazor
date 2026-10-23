namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The sixteen ANSI colours of a mode as tokens (<c>--omni-ansi-0</c> to <c>--omni-ansi-15</c>), for the
/// text of a log line (<c>OmniAnsiText</c>, <c>OmniLogViewer</c>).
/// </summary>
/// <remarks>
/// <para>
/// Every token is a text colour: it is moved, in 2 % steps away from the surface, until it reads at 4.5:1
/// on every ground it can be drawn on. The farthest of them is the surface of a card or grid a theme tints
/// (<see cref="CardAllowance"/> towards the text) carrying a background colour, which draws
/// <see cref="BackgroundShare"/> of its colour: held against pure black (light mode) or white (dark mode)
/// as that colour, it bounds the background of every colour in the sixteen. The warning and danger tints of
/// a log row and the accent tint of a search match are grounds too. One token therefore serves the log
/// surface of any theme, a foreground colour laid on any background colour, and the default text laid on one.
/// </para>
/// <para>
/// The six hues (red, green, yellow, blue, magenta, cyan) are authored per mode, one normal shade each.
/// In light mode the bright shades are the vivid authored colours held at 4.5:1 (lighter is not possible on a
/// light surface) and each normal shade is its bright one darkened by 20 %; in dark mode the normal shades
/// come out at the floor of the ratio and each bright shade is its normal one lightened by 25 %. So a
/// bright colour is always lighter than its normal one.
/// The neutrals (0 black, 7 white, 8 bright black, 15 bright white) are steps between the lightest readable
/// grey of the palette's text and surface and the strongest ink: black is the strong ink in light mode and
/// the dimmest readable grey in dark mode, and white the reverse.
/// </para>
/// </remarks>
internal static class AnsiTokenBuilder
{
    /// <summary>The share of the colour a background draws over the surface, mirrored by <c>.omni-ansi-bg-N</c> in the stylesheet.</summary>
    internal const double BackgroundShare = 0.12;

    private const double TextRatio = 4.5;
    private const double MatchTint = 0.24;
    private const double CardAllowance = 0.08;
    private const double BrightLift = 0.25;
    private const double NormalShade = 0.2;
    private const double Step = 0.02;

    // Red, green, yellow, blue, magenta, cyan: the vivid shade of light mode (the bright one, indices 9 to 14) and the shade of dark mode (the normal one, indices 1 to 6).
    private static readonly string[] LightVivid = ["#e53935", "#43a047", "#d99200", "#1e88e5", "#ba68c8", "#00acc1"];
    private static readonly string[] DarkNormal = ["#e57373", "#81c784", "#d4b44a", "#64b5f6", "#ce93d8", "#4dd0e1"];

    /// <summary>Adds <c>--omni-ansi-0</c> to <c>--omni-ansi-15</c> to the tokens of a mode, which must already hold its surface, text and tints.</summary>
    internal static void AddTo(Dictionary<string, string> tokens, bool dark)
    {
        var surface = tokens["--omni-color-surface"];
        var text = tokens["--omni-color-text"];
        var pole = dark ? "#ffffff" : "#000000";
        string[] grounds =
        [
            surface,
            ThemeColor.Mix(pole, ThemeColor.Mix(pole, surface, CardAllowance), BackgroundShare),
            tokens["--omni-color-warning-subtle"],
            tokens["--omni-color-danger-subtle"],
            ThemeColor.Mix(tokens["--omni-color-accent"], surface, MatchTint),
        ];

        var colors = new string[16];
        for (var hue = 0; hue < 6; hue++)
        {
            if (dark)
            {
                colors[1 + hue] = Settle(DarkNormal[hue], grounds, TextRatio);
                colors[9 + hue] = ThemeColor.Mix("#ffffff", colors[1 + hue], BrightLift);
            }
            else
            {
                colors[9 + hue] = Settle(LightVivid[hue], grounds, TextRatio);
                colors[1 + hue] = ThemeColor.Mix("#000000", colors[9 + hue], NormalShade);
            }
        }

        // The lightest readable grey is the "white" of light mode and the "black" of dark mode; the other
        // neutrals are steps from it towards the strongest ink, bright white, past the text colour.
        var floor = Settle(ThemeColor.Mix(text, surface, 0.6), grounds, TextRatio);
        var strongest = ThemeColor.Mix(pole, text, dark ? 0.6 : 0.55);
        colors[0] = dark ? floor : ThemeColor.Mix(strongest, floor, 0.6);
        colors[7] = dark ? ThemeColor.Mix(strongest, floor, 0.55) : floor;
        colors[8] = ThemeColor.Mix(strongest, floor, dark ? 0.2 : 0.25);
        colors[15] = strongest;
        for (var index = 0; index < colors.Length; index++)
        {
            tokens[$"--omni-ansi-{index}"] = colors[index];
        }
    }

    /// <summary>
    /// Moves a colour away from the first ground, in small steps, until it clears the ratio on all of them;
    /// the colour is bent no further than the ratio requires.
    /// </summary>
    private static string Settle(string color, string[] grounds, double ratio)
    {
        var target = ThemeColor.Luminance(grounds[0]) > 0.35 ? "#000000" : "#ffffff";
        var candidate = color;
        for (var step = 0; step < 200 && grounds.Any(ground => ThemeColor.Contrast(candidate, ground) < ratio); step++)
        {
            candidate = ThemeColor.Mix(target, candidate, Step);
        }

        return candidate;
    }
}
