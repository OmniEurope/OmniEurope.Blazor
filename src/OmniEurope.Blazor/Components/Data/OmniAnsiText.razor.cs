namespace OmniEurope.Blazor.Components;

/// <summary>
/// A line of terminal output with its ANSI escape sequences read: colours, bold, faint, italic and
/// underline are drawn and no <c>[</c> or escape character is left in the text. For a cell of a grid or a
/// line outside an <see cref="OmniLogViewer"/>, which does the same for its own lines through
/// <see cref="OmniLogViewer.AnsiMode"/>. The text passed in is never changed; only what is displayed is.
/// </summary>
/// <remarks>
/// It renders one <c>span</c> holding plain text and a <c>span</c> per run that carries attributes; how
/// spaces and line breaks are handled is the host's, through its own class. Colours are the
/// <c>--omni-ansi-0</c> to <c>--omni-ansi-15</c> tokens, held at 4.5:1 on the log surface in every theme and
/// both modes; a background is a faint tint of its colour so the text stays readable on it. What is
/// recognised, and what becomes of the rest, is described on <see cref="OmniAnsi"/>.
/// </remarks>
public partial class OmniAnsiText
{
    /// <summary>The line as written, escape sequences included. Null or empty draws nothing.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary><see cref="OmniAnsiMode.Render"/> (the default) draws the attributes, <see cref="OmniAnsiMode.Hide"/> shows the plain text, <see cref="OmniAnsiMode.Raw"/> shows the line as written.</summary>
    [Parameter]
    public OmniAnsiMode Mode { get; set; } = OmniAnsiMode.Render;
}
