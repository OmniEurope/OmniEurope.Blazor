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

    /// <summary>The handler of a <see cref="OmniHtmlEditorAction.Custom"/> command.</summary>
    public Func<OmniHtmlEditorCommandContext, Task>? Execute { get; init; }

    /// <summary>A command that runs <paramref name="execute"/> when chosen.</summary>
    public static OmniHtmlEditorCommand Create(
        string name,
        string label,
        Func<OmniHtmlEditorCommandContext, Task> execute,
        OmniIconName? icon = null) =>
        new(name, OmniHtmlEditorAction.Custom) { Label = label, Execute = execute, Icon = icon };
}
