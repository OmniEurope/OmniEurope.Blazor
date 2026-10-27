using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>Every alert severity in every fill, then the same severities with the message alone.</summary>
public partial class AlertGalleryExample
{
    private static readonly OmniFill[] Fills = [OmniFill.Outline, OmniFill.Solid, OmniFill.Tonal];

    private readonly HashSet<(OmniFill Fill, OmniSeverity Severity)> _closed = [];

    /// <summary>Whether each alert carries a cross that closes it.</summary>
    [Parameter]
    public bool Dismissible { get; set; } = true;

    private bool IsClosed(OmniFill fill, OmniSeverity severity) => _closed.Contains((fill, severity));

    private void Close(OmniFill fill, OmniSeverity severity) => _closed.Add((fill, severity));

    private void Restore() => _closed.Clear();
}
