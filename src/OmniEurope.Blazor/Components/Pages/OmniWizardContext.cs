namespace OmniEurope.Blazor.Components;

/// <summary>
/// What an <see cref="OmniWizard"/> shares with its steps: the steps in declaration order, and which
/// one is current. A step registers itself when it is initialized and leaves when it is disposed.
/// </summary>
internal sealed class OmniWizardContext(Action changed)
{
    private readonly List<OmniWizardStep> _steps = [];

    internal IReadOnlyList<OmniWizardStep> Steps => _steps;

    internal int CurrentIndex { get; set; }

    internal bool IsCurrent(OmniWizardStep step) => _steps.IndexOf(step) == CurrentIndex;

    internal void Register(OmniWizardStep step)
    {
        _steps.Add(step);
        changed();
    }

    internal void Unregister(OmniWizardStep step)
    {
        if (_steps.Remove(step))
        {
            changed();
        }
    }

    /// <summary>A step's title or gate changed: the wizard redraws its step list and its buttons.</summary>
    internal void StepChanged() => changed();
}
