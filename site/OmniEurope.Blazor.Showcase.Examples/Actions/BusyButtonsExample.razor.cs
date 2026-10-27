using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Actions;

/// <summary>A button, a split button and a toggle button sharing one busy state.</summary>
public partial class BusyButtonsExample : IDisposable
{
    private readonly CancellationTokenSource _disposal = new();

    /// <summary>How long a save keeps the buttons busy.</summary>
    [Parameter]
    public TimeSpan WorkDuration { get; set; } = TimeSpan.FromSeconds(2);

    private bool Saving { get; set; }

    private bool Pinned { get; set; }

    private async Task SaveAsync()
    {
        Saving = true;
        try
        {
            await Task.Delay(WorkDuration, _disposal.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Saving = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposal.Cancel();
        _disposal.Dispose();
        GC.SuppressFinalize(this);
    }
}
