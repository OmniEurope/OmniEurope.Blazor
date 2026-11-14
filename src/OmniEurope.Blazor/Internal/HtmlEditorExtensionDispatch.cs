using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The calls of the surface script that an <see cref="OmniHtmlEditor"/> hands to its extensions: the
/// proposal of a sentence ending, a keyboard shortcut an extension registered, and the activation of an
/// inline element an extension owns. Each acts only on the visual face.
/// </summary>
internal sealed class HtmlEditorExtensionDispatch(OmniHtmlEditor owner)
{
    /// <summary>The first proposal of the extensions that suggest text, or null.</summary>
    internal async Task<string?> SuggestAsync(string textBeforeCaret)
    {
        if (owner.IsLocked || owner.CurrentMode != OmniHtmlEditorMode.Visual || string.IsNullOrWhiteSpace(textBeforeCaret))
        {
            return null;
        }

        foreach (var extension in owner.ExtensionSet.Source.Where(extension => extension.SuggestsText))
        {
            var proposal = await extension.SuggestAsync(textBeforeCaret);
            if (!string.IsNullOrEmpty(proposal))
            {
                return proposal;
            }
        }

        return null;
    }

    /// <summary>Runs the command the shortcut at <paramref name="index"/> names; an unknown index or the source face does nothing.</summary>
    internal async Task HandleShortcutAsync(int index)
    {
        var extensions = owner.ExtensionSet;
        if (index < 0 || index >= extensions.Shortcuts.Count || owner.CurrentMode != OmniHtmlEditorMode.Visual)
        {
            return;
        }

        var command = extensions.Find(extensions.Shortcuts[index].Command, owner.ArrangedCommands);
        if (command is not null)
        {
            await owner.RunAsync(command);
        }
    }

    /// <summary>Gives the inline element at <paramref name="index"/> its activation, with the node it was activated on.</summary>
    internal async Task HandleElementActivatedAsync(int index, string element, string text)
    {
        if (owner.Disabled || index < 0 || index >= owner.ExtensionSet.InlineElements.Count || owner.CurrentMode != OmniHtmlEditorMode.Visual)
        {
            return;
        }

        var node = OmniHtmlEditorSelection.Parse($$"""{"ancestors":[{{element}}]}""").Ancestors.FirstOrDefault();
        if (node is null)
        {
            return;
        }

        await owner.CaptureVisualAsync();
        await owner.ExtensionSet.InlineElements[index].Activate(new OmniHtmlEditorElementContext(owner, node, text));
    }
}
