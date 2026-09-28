namespace OmniEurope.Blazor.Components;

/// <summary>
/// What an <see cref="OmniDialog"/> is for, told by its header and footer: both bands take the tint of
/// the intention and the title is led by a round mark carrying its icon. The body stays on the surface
/// of the dialog.
/// </summary>
public enum OmniDialogIntent
{
    /// <summary>No tint and no mark: the header and footer of a dialog as they always were. The default.</summary>
    None = 0,

    /// <summary>A form or a choice to make: the accent tint, an information mark.</summary>
    Accent,

    /// <summary>A question whose answer is hard to undo (stopping a run, a destructive confirmation): the warning tint and mark.</summary>
    Warning,

    /// <summary>What is about to be lost for good, or an error to read: the danger tint and an error mark.</summary>
    Danger
}
