using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>Log lines written as a program writes them to a terminal, shown rendered, stripped or raw.</summary>
public partial class DataListAnsiExample
{
    private IReadOnlyList<OmniOption<OmniAnsiMode>> AnsiModes { get; } =
    [
        new(OmniAnsiMode.Render, "Render colours"),
        new(OmniAnsiMode.Hide, "Hide codes"),
        new(OmniAnsiMode.Raw, "Raw text")
    ];

    // Lines as a program writes them to a terminal: the escape character (U+001B) opens each sequence.
    private IReadOnlyList<OmniLogLine> AnsiLines { get; } =
    [
        new("\u001b[90m3:27PM\u001b[0m \u001b[32mINF\u001b[0m \u001b[1mno leaks found\u001b[0m"),
        new("\u001b[90m3:27PM\u001b[0m \u001b[33mWRN\u001b[0m \u001b[3mslow query, 1.2 s\u001b[0m", OmniLogLevel.Warning),
        new("\u001b[90m3:28PM\u001b[0m \u001b[1;31mERR\u001b[0m \u001b[31mconnection refused\u001b[0m \u001b[2;4mretrying in 5 s\u001b[0m", OmniLogLevel.Error),
        new("\u001b[2K\u001b[38;5;208m3:28PM\u001b[0m \u001b[38;2;0;153;204mDBG\u001b[0m \u001b[97;44m no leaks found \u001b[0m", OmniLogLevel.Debug)
    ];

    private OmniAnsiMode AnsiMode { get; set; } = OmniAnsiMode.Render;
}
