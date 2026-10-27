using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Actions;

/// <summary>A single button whose variant, size, type, content and states are parameters.</summary>
public partial class ButtonsExample : IDisposable
{
    /// <summary>Long enough to watch the busy veil breathe, short enough not to bore.</summary>
    private static readonly TimeSpan WorkDuration = TimeSpan.FromSeconds(2);

    private readonly CancellationTokenSource _disposal = new();

    /// <summary>What the button means; the colour follows the role.</summary>
    [Parameter]
    public OmniButtonVariant Variant { get; set; } = OmniButtonVariant.Primary;

    /// <summary>The size of the button.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>The native type: Submit sends the form, Reset clears it, Button does neither.</summary>
    [Parameter]
    public OmniButtonType ButtonType { get; set; } = OmniButtonType.Button;

    /// <summary>Whether the button shows its icon alone, its name then carried by the accessible label.</summary>
    [Parameter]
    public bool IconOnly { get; set; }

    /// <summary>Whether the button is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the button is busy, set by hand.</summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>Whether each click simulates two seconds of work, the way a real save would.</summary>
    [Parameter]
    public bool BusyOnClick { get; set; } = true;

    /// <summary>The busy state a click starts, distinct from <see cref="Busy"/>.</summary>
    private bool Working { get; set; }

    private int ClickCount { get; set; }

    private int SubmitCount { get; set; }

    private ButtonSample Current => ButtonSample.Of(Variant);

    private async Task HandleClickAsync()
    {
        ClickCount++;
        if (!BusyOnClick)
        {
            return;
        }

        Working = true;
        try
        {
            await Task.Delay(WorkDuration, _disposal.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Working = false;
    }

    private void HandleSubmit() => SubmitCount++;

    private void ResetCounters()
    {
        ClickCount = 0;
        SubmitCount = 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposal.Cancel();
        _disposal.Dispose();
        GC.SuppressFinalize(this);
    }
}
