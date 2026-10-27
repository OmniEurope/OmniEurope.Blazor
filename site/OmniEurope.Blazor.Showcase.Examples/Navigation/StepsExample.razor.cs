using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>Four steps with a navigation guard, a disabled step and previous, next and restart buttons.</summary>
public partial class StepsExample
{
    private const int StepCount = 4;
    private const int LastStep = StepCount - 1;

    /// <summary>Whether a click can go back or to the next step, never further.</summary>
    [Parameter]
    public bool LockAhead { get; set; }

    /// <summary>Whether the second step is disabled.</summary>
    [Parameter]
    public bool DisableReview { get; set; }

    private int CurrentStep { get; set; }

    /// <summary>The last step the guard refused, cleared as soon as a navigation succeeds.</summary>
    private int? RefusedStep { get; set; }

    /// <summary>
    /// Locked, the strip lets the reader go back or to the next step: jumping ahead is refused, which the
    /// Previous and Next buttons do not suffer since they set the value themselves.
    /// </summary>
    private Func<int, Task<bool>>? NavigationGuard => LockAhead ? CanNavigateTo : null;

    private Task<bool> CanNavigateTo(int index)
    {
        var allowed = index <= CurrentStep + 1;
        RefusedStep = allowed ? null : index;
        return Task.FromResult(allowed);
    }

    private void Previous() => Move(CurrentStep - 1);

    private void Next() => Move(CurrentStep + 1);

    private void Restart() => Move(0);

    private void Move(int index)
    {
        CurrentStep = Math.Clamp(index, 0, LastStep);
        RefusedStep = null;
    }
}
