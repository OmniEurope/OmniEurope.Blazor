namespace OmniEurope.Blazor.Components;

/// <summary>A key combination that runs a command of an <see cref="OmniHtmlEditor"/> in its visual face.</summary>
/// <param name="Keys">
/// The combination, modifiers first and joined by <c>+</c>: <c>Ctrl+Shift+M</c>, <c>Alt+F</c>, <c>F9</c>.
/// <c>Ctrl</c> also matches the Command key on a Mac. The history keys (Ctrl+Z, Ctrl+Y, Ctrl+Shift+Z) and
/// the link key (Ctrl+K) belong to the editor.
/// </param>
/// <param name="Command">The <see cref="OmniHtmlEditorCommand.Name"/> of the command to run.</param>
public sealed record OmniHtmlEditorShortcut(string Keys, string Command)
{
    private static readonly string[] Reserved = ["ctrl+z", "ctrl+y", "ctrl+shift+z", "ctrl+k"];

    /// <summary>The combination as the surface script matches it: <c>ctrl+shift+m</c>, modifiers in a fixed order.</summary>
    internal string Normalized => Normalize(Keys);

    internal static string Normalize(string keys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keys);
        var parts = keys.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Any(part => part.Length == 0))
        {
            throw new ArgumentException($"The shortcut '{keys}' has an empty key.", nameof(keys));
        }

        var modifiers = parts[..^1].Select(part => part.ToLowerInvariant() switch
        {
            "ctrl" or "control" or "cmd" or "meta" => "ctrl",
            "shift" => "shift",
            "alt" or "option" => "alt",
            _ => throw new ArgumentException($"The shortcut '{keys}' has an unknown modifier '{part}'.", nameof(keys))
        }).Distinct().ToHashSet(StringComparer.Ordinal);
        string[] order = ["ctrl", "alt", "shift"];
        var normalized = string.Join('+', order.Where(modifiers.Contains).Append(parts[^1].ToLowerInvariant()));
        if (Reserved.Contains(normalized, StringComparer.Ordinal))
        {
            throw new ArgumentException($"The shortcut '{keys}' belongs to the editor itself.", nameof(keys));
        }

        return normalized;
    }
}
