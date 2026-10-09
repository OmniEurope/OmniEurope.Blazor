namespace OmniEurope.Blazor.Components;

/// <summary>Where <see cref="OmniDialog"/> puts the focus when it opens (<see cref="OmniDialog.InitialFocus"/>).</summary>
public enum OmniDialogInitialFocus
{
    /// <summary>
    /// The close button, the default and the behavior the dialog always had; without a close button,
    /// the first element that takes the focus, then the panel itself.
    /// </summary>
    CloseButton,

    /// <summary>
    /// The panel itself, without a visible focus ring: nothing looks selected until the reader clicks
    /// or presses Tab, and the first Tab goes to the first element that takes the focus. The focus
    /// trap of a modal dialog and the focus return on closing stay the same.
    /// </summary>
    Panel,

    /// <summary>
    /// The first element of the content that takes the focus (a field, a button); without one, the
    /// first in the dialog (header and footer included), then the panel itself.
    /// </summary>
    FirstFocusable
}
