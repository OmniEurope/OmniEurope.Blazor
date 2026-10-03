namespace OmniEurope.Blazor.Components;

/// <summary>One entry of the toolbar of <see cref="OmniHtmlEditor"/>.</summary>
/// <remarks>
/// The toolbar is the list given to <see cref="OmniHtmlEditor.Commands"/>, rendered in order, so an
/// application adds, removes or reorders commands by building its own list, usually from
/// <see cref="OmniHtmlEditorCommands.Default"/>. A built-in command needs only its
/// <see cref="Action"/>: its label and icon come from the editor. A <see cref="OmniHtmlEditorAction.Custom"/>
/// command supplies <see cref="Label"/> and <see cref="Execute"/>, and reaches the document through the
/// <see cref="OmniHtmlEditorCommandContext"/> it receives.
/// </remarks>
/// <param name="Name">A stable identifier, rendered as <c>data-command</c> on the control.</param>
/// <param name="Action">What the command does.</param>
public sealed record OmniHtmlEditorCommand(string Name, OmniHtmlEditorAction Action)
{
    /// <summary>The accessible name and tooltip. Null keeps the localized label of a built-in action.</summary>
    public string? Label { get; init; }

    /// <summary>The icon. Null keeps the icon of a built-in action; a custom command without one shows its label.</summary>
    public OmniIconName? Icon { get; init; }

    /// <summary>
    /// What the command is for, shown under its name in the package tooltip of its toolbar control and
    /// given to assistive technologies as its description (<c>aria-description</c>). Null: the name alone.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Places the command in the "more" menu (⋮) at the end of the toolbar rather than in the bar: the
    /// secondary commands of a long toolbar. False, the default: in the bar, where
    /// <see cref="OmniHtmlEditor.ToolbarRows"/> may still move it to the menu when it does not fit. The
    /// lists (<see cref="OmniHtmlEditorAction.BlockFormat"/>, <see cref="OmniHtmlEditorAction.FontSize"/>,
    /// <see cref="OmniHtmlEditorAction.ChangeCase"/>) always stay in the bar.
    /// </summary>
    public bool Overflow { get; init; }

    /// <summary>The handler of a <see cref="OmniHtmlEditorAction.Custom"/> command.</summary>
    public Func<OmniHtmlEditorCommandContext, Task>? Execute { get; init; }

    /// <summary>
    /// Makes the command a toggle: its button carries <c>aria-pressed</c>, true when this returns
    /// true for the selection at hand (null in the source face or before the first report). Setting
    /// it makes the surface report the selection, and it replaces the built-in pressed state.
    /// </summary>
    public Func<OmniHtmlEditorSelection?, bool>? Pressed { get; init; }

    /// <summary>
    /// When the command can run: its button is disabled while this returns false for the selection at hand
    /// (null in the source face or before the first report), so a contextual tool (table rows, a formula)
    /// keeps its place in the toolbar instead of appearing and vanishing as the caret moves. Setting it makes
    /// the surface report the selection. Null: enabled as before.
    /// </summary>
    public Func<OmniHtmlEditorSelection?, bool>? Enabled { get; init; }

    /// <summary>A command that runs <paramref name="execute"/> when chosen.</summary>
    public static OmniHtmlEditorCommand Create(
        string name,
        string label,
        Func<OmniHtmlEditorCommandContext, Task> execute,
        OmniIconName? icon = null) =>
        new(name, OmniHtmlEditorAction.Custom) { Label = label, Execute = execute, Icon = icon };
}
