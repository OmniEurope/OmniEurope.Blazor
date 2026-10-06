namespace OmniEurope.Blazor.Components;

/// <summary>
/// The visual face of an <see cref="OmniHtmlEditor"/> as <c>omni-html-editor.js</c> drives it: the module
/// and the script's reference to the editor, the mounting of the surface and the options pushed to it,
/// and every read or change made through the script. What the surface gives back is sanitised and
/// committed through the editor, so the history and <c>ValueChanged</c> stay the editor's.
/// </summary>
internal sealed class HtmlEditorVisualSurface(OmniHtmlEditor owner, IJSRuntime javaScript) : IAsyncDisposable
{
    private const string VisualModulePath = OmniModules.HtmlEditor;

    private IJSObjectReference? _module;
    private DotNetObjectReference<HtmlEditorInteropBridge>? _bridge;
    private string? _value;
    private int _rows;
    private bool _tracking;
    private HtmlEditorExtensionSet? _set;
    private bool _locked;

    /// <summary>Whether the surface is mounted, or being mounted.</summary>
    internal bool Mounted { get; private set; }

    private bool Ready => Mounted && _module is not null;

    private bool Visual => owner.CurrentMode == OmniHtmlEditorMode.Visual;

    /// <summary>Whether the surface is mounted, shown and open to changes.</summary>
    private bool Editable => !owner.IsLocked && Visual && Ready;

    /// <summary>
    /// What the surface script needs besides the rows: the classes it keeps while tidying (null
    /// for any), and whether a policy is in force, in which case a span carrying an allowed
    /// attribute is kept rather than unwrapped.
    /// </summary>
    private Dictionary<string, object?> Options
    {
        get
        {
            var set = owner.ExtensionSet;
            var options = new Dictionary<string, object?>(StringComparer.Ordinal) { ["rows"] = owner.Rows };
            if (set.Policy is not null)
            {
                options["policy"] = true;
                options["classes"] = OmniHtmlSanitizer.ClassesOf(set.Policy);
            }

            if (set.ShortcutKeys.Count > 0)
            {
                options["shortcuts"] = set.ShortcutKeys;
            }

            if (set.InlineElements.Count > 0)
            {
                options["inline"] = set.InlineElements.Select(element => element.Selector).ToArray();
            }

            if (set.ContextMenu.Count > 0)
            {
                options["menu"] = true;
            }

            if (set.Source.Any(extension => extension.SuggestsText))
            {
                options["suggest"] = true;
            }

            if (set.Proofreaders.Count > 0)
            {
                options["proofread"] = true;
            }

            if (owner.TracksSelection)
            {
                options["selection"] = true;
            }

            return options;
        }
    }

    /// <summary>
    /// Mounts the surface with the sanitised value, or pushes to it a value or options that changed
    /// since, and asks the proofreading again when the editor was just unlocked. Called after each
    /// render of the visual face.
    /// </summary>
    internal async Task SyncAsync()
    {
        if (!Mounted)
        {
            // Claimed before the first await: a render arriving while the module loads must not
            // mount the surface a second time.
            Mounted = true;
            _rows = owner.Rows;
            _module ??= await javaScript.InvokeAsync<IJSObjectReference>("import", VisualModulePath);
            _bridge ??= DotNetObjectReference.Create(new HtmlEditorInteropBridge(owner));
            _value = owner.EditorValue;
            _tracking = owner.TracksSelection;
            _set = owner.ExtensionSet;
            _locked = owner.IsLocked;
            await _module.InvokeVoidAsync("mount", owner.SurfaceElement, _bridge, owner.Clean(owner.EditorValue), Options);
        }
        else if (_module is not null)
        {
            if (!string.Equals(owner.EditorValue, _value, StringComparison.Ordinal))
            {
                _value = owner.EditorValue;
                await _module.InvokeVoidAsync("setHtml", owner.SurfaceElement, owner.Clean(owner.EditorValue));
            }

            // The policy belongs to the extension set: the same set is the same policy.
            if (_rows != owner.Rows || _tracking != owner.TracksSelection || !ReferenceEquals(_set, owner.ExtensionSet))
            {
                _rows = owner.Rows;
                _tracking = owner.TracksSelection;
                _set = owner.ExtensionSet;
                await _module.InvokeVoidAsync("configure", owner.SurfaceElement, Options);
            }

            // A locked editor answers the proofreading script "nothing checked", which it keeps no answer
            // for: once unlocked, its blocks are asked again now rather than at the next keystroke.
            if (_locked != owner.IsLocked)
            {
                _locked = owner.IsLocked;
                if (!_locked)
                {
                    await _module.InvokeVoidAsync("proofreadRecheck", owner.SurfaceElement);
                }
            }
        }
    }

    /// <summary>What the surface reported was typed, pasted or dropped; ignored while locked or in the source face.</summary>
    internal Task TakeInputAsync(string html)
    {
        if (owner.IsLocked || !Visual)
        {
            return Task.CompletedTask;
        }

        return CommitResultAsync(html);
    }

    /// <summary>Runs an action of the surface script and commits the document it returns, if any.</summary>
    internal async Task ExecuteAsync(string action, string? argument)
    {
        if (!Ready)
        {
            return;
        }

        var html = await _module!.InvokeAsync<string?>("exec", owner.SurfaceElement, action, argument);
        if (html is not null)
        {
            await CommitResultAsync(html);
        }
    }

    /// <summary>
    /// Takes in what was typed in the visual surface and not yet reported, so a command, the history
    /// or a mode switch never acts on a value a quarter of a second old.
    /// </summary>
    internal async Task CaptureAsync()
    {
        if (!Visual || !Ready)
        {
            return;
        }

        var html = await _module!.InvokeAsync<string?>("read", owner.SurfaceElement);
        if (html is not null)
        {
            await CommitResultAsync(html);
        }
    }

    /// <summary>
    /// Takes in a surface the host changed through its own script: read, sanitised, committed, and
    /// redrawn after the next render when the sanitiser removed something.
    /// </summary>
    internal async Task CommitSurfaceAsync()
    {
        if (!Editable)
        {
            return;
        }

        var html = await _module!.InvokeAsync<string?>("read", owner.SurfaceElement);
        if (html is null)
        {
            return;
        }

        var clean = owner.Clean(html);
        // A surface that differs from its sanitised value is redrawn after the next render.
        _value = html;
        await owner.CommitAsync(clean);
        if (!string.Equals(clean, html, StringComparison.Ordinal))
        {
            owner.Rerender();
        }
    }

    /// <summary>Replaces the closest element matching the selector around the caret; false when there is none.</summary>
    internal async Task<bool> ReplaceClosestAsync(string selector, string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        if (!Editable)
        {
            return false;
        }

        await CaptureAsync();
        var result = await _module!.InvokeAsync<string?>("replaceClosest", owner.SurfaceElement, selector, owner.Clean(html));
        if (result is null)
        {
            return false;
        }

        await CommitResultAsync(result);
        return true;
    }

    /// <summary>Replaces the inline element last activated by a click.</summary>
    internal Task ReplaceActivatedAsync(string html) => ChangeActivatedAsync("replaceActivated", () => owner.Clean(html));

    /// <summary>Sets the text of the inline element last activated by a click.</summary>
    internal Task SetActivatedTextAsync(string text) => ChangeActivatedAsync("setActivatedText", () => text ?? string.Empty);

    private async Task ChangeActivatedAsync(string function, Func<string> argument)
    {
        if (!Editable)
        {
            return;
        }

        await CaptureAsync();
        var result = await _module!.InvokeAsync<string?>(function, owner.SurfaceElement, argument());
        if (result is not null)
        {
            await CommitResultAsync(result);
        }
    }

    /// <summary>The sanitised document before and after the caret; the whole value before it outside the visual face.</summary>
    internal async Task<OmniHtmlCaretSplit> GetHtmlAroundCaretAsync()
    {
        if (!Visual || !Ready)
        {
            return new(owner.CurrentHtml, string.Empty);
        }

        var json = await _module!.InvokeAsync<string?>("aroundCaret", owner.SurfaceElement);
        if (string.IsNullOrEmpty(json))
        {
            return new(owner.CurrentHtml, string.Empty);
        }

        using var parts = System.Text.Json.JsonDocument.Parse(json);
        return new(owner.Clean(parts.RootElement.GetProperty("before").GetString()), owner.Clean(parts.RootElement.GetProperty("after").GetString()));
    }

    /// <summary>The text selected in the surface; empty outside the visual face.</summary>
    internal async Task<string> GetSelectedTextAsync() =>
        Visual && Ready
            ? await _module!.InvokeAsync<string?>("selectedText", owner.SurfaceElement) ?? string.Empty
            : string.Empty;

    /// <summary>Inserts text at the caret; nothing outside the visual face.</summary>
    internal Task InsertTextAsync(string text) =>
        string.IsNullOrEmpty(text) || !Visual
            ? Task.CompletedTask
            : ExecuteAsync("inserttext", text);

    /// <summary>A correction replaces the flagged passage the context menu was opened on.</summary>
    internal async Task ProofreadReplaceAsync(string text)
    {
        if (Editable)
        {
            await _module!.InvokeVoidAsync("proofreadReplace", owner.SurfaceElement, text);
        }
    }

    /// <summary>The flagged passage the context menu was opened on is no longer underlined.</summary>
    internal async Task ProofreadIgnoreAsync()
    {
        if (Ready)
        {
            await _module!.InvokeVoidAsync("proofreadIgnore", owner.SurfaceElement);
        }
    }

    /// <summary>Every block is checked again (a proofreader's word lists changed).</summary>
    internal async Task ProofreadRecheckAsync()
    {
        if (Ready)
        {
            await _module!.InvokeVoidAsync("proofreadRecheck", owner.SurfaceElement);
        }
    }

    /// <summary>Puts back the selection the context menu was opened on.</summary>
    internal async Task RestoreMenuSelectionAsync()
    {
        if (Ready)
        {
            await _module!.InvokeVoidAsync("restoreMenuSelection", owner.SurfaceElement);
        }
    }

    /// <summary>Releases the surface when the editor leaves the visual face.</summary>
    internal async Task UnmountAsync()
    {
        if (Ready)
        {
            Mounted = false;
            await _module!.InvokeVoidAsync("dispose", owner.SurfaceElement);
        }

        Mounted = false;
    }

    /// <summary>
    /// Disposes the surface and releases the module, a lost circuit ignored, then the script's
    /// reference to the editor.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                if (Mounted)
                {
                    await _module.InvokeVoidAsync("dispose", owner.SurfaceElement);
                }

                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone, and the listeners with it.
            }
        }

        _bridge?.Dispose();
    }

    private Task CommitResultAsync(string html)
    {
        var clean = owner.Clean(html);
        _value = clean;
        return owner.CommitAsync(clean);
    }
}
