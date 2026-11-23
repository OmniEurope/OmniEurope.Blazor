namespace OmniEurope.Blazor.Components;

/// <summary>
/// What an <see cref="OmniWizard"/> and its <see cref="OmniWizardContext"/> read from, or ask of, a
/// registered step: its title, its gate, and a redraw when the wizard moves.
/// </summary>
internal interface IOmniWizardStepView
{
    /// <summary>Name of the step in the step list and in the announcement of the step.</summary>
    string Title { get; }

    /// <summary>Whether the user may leave this step forward.</summary>
    bool CanContinue { get; }

    /// <summary>Asked when the user leaves this step forward; null lets the step go at once.</summary>
    Func<Task<bool>>? Validate { get; }

    /// <summary>Redraws the step, whose content shows only while it is current.</summary>
    void Redraw();
}
