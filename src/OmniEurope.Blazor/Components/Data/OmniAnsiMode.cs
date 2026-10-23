namespace OmniEurope.Blazor.Components;

/// <summary>
/// What a log line does with the ANSI escape sequences it holds (<c>ESC [ 31 m</c> and the like). The line
/// is stored and exported as written in every mode; only what is displayed changes.
/// </summary>
public enum OmniAnsiMode
{
    /// <summary>Colours, bold, dim, italic and underline are drawn, and every sequence is removed from the text.</summary>
    Render,

    /// <summary>Every sequence is removed from the text and nothing is drawn: plain text.</summary>
    Hide,

    /// <summary>The text is shown exactly as written, sequences included (the escape character itself draws as nothing in a browser, the rest of the sequence stays visible).</summary>
    Raw
}
