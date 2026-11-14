using System.Net;

using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What an <see cref="OmniHtmlEditor"/> does with the panels under its toolbar: it opens and applies the
/// link field, picks a special character from the character panel or the palette, and reads and inserts an
/// imported table. The state of the panels is <see cref="HtmlEditorPanels"/>; the work reaches the document
/// through the editor, on whichever face is showing.
/// </summary>
internal sealed class HtmlEditorPanelActions(OmniHtmlEditor owner, HtmlEditorPanels panels)
{
    /// <summary>Opens the link field, unless the editor is read only or disabled.</summary>
    internal Task OpenLinkAsync()
    {
        if (!owner.IsLocked)
        {
            panels.OpenLink();
        }

        return Task.CompletedTask;
    }

    /// <summary>Applies the address typed in the link field: an empty one closes the field, an unsafe one keeps it open.</summary>
    internal async Task ApplyLinkAsync()
    {
        var url = panels.LinkUrl.Trim();
        if (url.Length == 0)
        {
            panels.CloseLink();
            return;
        }

        if (panels.SafeLinkUrl(url) is not { } safe)
        {
            return;
        }

        panels.HideLink();
        await owner.RunBuiltInAsync(OmniHtmlEditorAction.Link, safe);
    }

    /// <summary>Closes the link field.</summary>
    internal Task CloseLinkAsync()
    {
        panels.CloseLink();
        return Task.CompletedTask;
    }

    /// <summary>Applies the link on Enter and closes the field on Escape.</summary>
    internal Task HandleLinkKeyAsync(KeyboardEventArgs args) => args.Key switch
    {
        "Enter" => ApplyLinkAsync(),
        "Escape" => CloseLinkAsync(),
        _ => Task.CompletedTask
    };

    /// <summary>Shows a panel (special characters or table import).</summary>
    internal Task OpenPanel(HtmlEditorPanel panel)
    {
        panels.Open(panel);
        return Task.CompletedTask;
    }

    /// <summary>Closes the panels and types a character at the caret of the face showing.</summary>
    internal async Task InsertCharacterAsync(string character)
    {
        panels.Close();
        if (owner.CurrentMode == OmniHtmlEditorMode.Visual)
        {
            await owner.Visual.ExecuteAsync("inserttext", character);
        }
        else
        {
            await owner.SourceFace.WrapSelectionAsync(WebUtility.HtmlEncode(character), string.Empty);
        }
    }

    /// <summary>A character of the palette under the surface, typed at the caret through the surface's own text insertion.</summary>
    internal Task InsertPaletteCharacterAsync(string character) =>
        owner.IsLocked || owner.CurrentMode != OmniHtmlEditorMode.Visual ? Task.CompletedTask : owner.InsertTextAsync(character);

    /// <summary>The file extensions the table import accepts, the extensions' readers included.</summary>
    internal string TableAccept => HtmlEditorPanels.TableAccept(owner.ExtensionSet.TableReaders);

    /// <summary>Reads the chosen file into the table preview.</summary>
    internal Task ReadTableFileAsync(IReadOnlyList<IBrowserFile> files) => panels.ReadTableFileAsync(files, owner.ExtensionSet.TableReaders);

    /// <summary>Inserts the previewed table into the document.</summary>
    internal async Task InsertTableAsync()
    {
        if (panels.TakeTable() is { } html)
        {
            await owner.InsertHtmlAsync(html);
        }
    }
}
