using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>A comparison with its own navigation bar, and one that fills a frame under a bar owned by the page.</summary>
public partial class DiffViewerNavigationExample
{
    private OmniDiffViewer? _fillViewer;

    private Task PreviousChangeAsync() => _fillViewer?.GoToPreviousChangeAsync() ?? Task.CompletedTask;

    private Task NextChangeAsync() => _fillViewer?.GoToNextChangeAsync() ?? Task.CompletedTask;
}
