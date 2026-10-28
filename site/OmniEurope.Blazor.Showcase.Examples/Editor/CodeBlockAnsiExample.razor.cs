using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>A command output with ANSI colour sequences, drawn, stripped or kept raw.</summary>
public partial class CodeBlockAnsiExample
{
    // A command output as a terminal writes it: the escape character (U+001B) opens each colour sequence.
    private const string CommandOutput = "\u001b[1;32m●\u001b[0m nginx.service - A high performance web server\n     Loaded: loaded (/lib/systemd/system/nginx.service; \u001b[32menabled\u001b[0m)\n     Active: \u001b[1;32mactive (running)\u001b[0m since Fri 2026-10-23 08:14:02 CEST\n    \u001b[33mWarning\u001b[0m: \u001b[31mfailed\u001b[0m to reload \u001b[4mnginx.conf\u001b[0m";

    private static readonly IReadOnlyList<OmniOption<OmniAnsiMode>> AnsiModes =
    [
        new(OmniAnsiMode.Render, "Render colours"),
        new(OmniAnsiMode.Hide, "Hide codes"),
        new(OmniAnsiMode.Raw, "Raw text")
    ];

    private OmniAnsiMode AnsiMode { get; set; } = OmniAnsiMode.Render;
}
