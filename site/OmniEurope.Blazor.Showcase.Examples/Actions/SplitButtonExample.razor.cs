using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Actions;

/// <summary>A split button whose variant, size, states and main text are parameters.</summary>
public partial class SplitButtonExample : IDisposable
{
    /// <summary>Long enough to watch the busy veil breathe, short enough not to bore.</summary>
    private static readonly TimeSpan WorkDuration = TimeSpan.FromSeconds(2);

    private readonly CancellationTokenSource _disposal = new();

    /// <summary>What the button means; both halves take the colour of the variant.</summary>
    [Parameter]
    public OmniButtonVariant Variant { get; set; } = OmniButtonVariant.Secondary;

    /// <summary>The size of both halves.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>Whether both halves stop reacting.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the button is busy, set by hand.</summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>Whether the main action simulates two seconds of work.</summary>
    [Parameter]
    public bool BusyOnClick { get; set; } = true;

    /// <summary>Whether the last entry of the menu, the deletion, is unavailable.</summary>
    [Parameter]
    public bool DeleteDisabled { get; set; } = true;

    /// <summary>The text of the main half.</summary>
    [Parameter]
    public string MainText { get; set; } = "Publish";

    private bool Working { get; set; }

    private bool MenuOpen { get; set; }

    private string? LastAction { get; set; }

    private int ActionCount { get; set; }

    private string LastActionEcho => LastAction is null
        ? "No action chosen yet."
        : $"Last action: {LastAction} ({ActionCount} in total).";

    private async Task HandleMainAsync()
    {
        Choose(MainText);
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

    private void Choose(string action)
    {
        LastAction = action;
        ActionCount++;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposal.Cancel();
        _disposal.Dispose();
        GC.SuppressFinalize(this);
    }
}
