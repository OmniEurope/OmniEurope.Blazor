namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The Monaco plumbing that <see cref="Components.OmniCodeEditor"/> and <see cref="Components.OmniDiffViewer"/>
/// share: checking <c>Height</c> and <c>MonacoPath</c>, importing the script once, loading Monaco (one shared
/// load per page, kept by the script), setting the height, and releasing the mounted editor and the module.
/// What differs between the two (the mount and update calls and their options) stays in the components.
/// </summary>
internal sealed class MonacoHost(IJSRuntime javaScript, Func<string> modulePath, Func<bool> isDisposed)
{
    /// <summary>How long the script waits for Monaco before giving up, so the plain fallback stays.</summary>
    internal const int LoadTimeoutMilliseconds = 30000;

    /// <summary>The default <c>MonacoPath</c> of both components.</summary>
    internal const string DefaultPath = "lib/monaco-editor/min/vs";

    /// <summary>The imported script, once <see cref="EnsureModuleAsync"/> has run.</summary>
    internal IJSObjectReference? Module { get; private set; }

    /// <summary>Whether an editor is mounted in the host element.</summary>
    internal bool Mounted { get; set; }

    /// <summary>
    /// Refuses a <paramref name="height"/> that is not a number followed by px, rem, em, vh or %, and a
    /// <paramref name="monacoPath"/> that leaves the origin of the page.
    /// </summary>
    /// <param name="component">The component type name, written in the message.</param>
    /// <param name="height">The <c>Height</c> parameter; null is accepted.</param>
    /// <param name="monacoPath">The <c>MonacoPath</c> parameter.</param>
    /// <exception cref="ArgumentException">One of the two values is not accepted.</exception>
    internal static void RequireParameters(string component, string? height, string monacoPath)
    {
        if (height is not null && !CssLength.IsHeight(height))
        {
            throw new ArgumentException($"'{height}' is not a CSS length {component} accepts for Height (a number followed by px, rem, em, vh or %).", "Height");
        }

        OmniSameOriginPath.Require(monacoPath, "MonacoPath");
    }

    /// <summary>Refuses an <c>InteropModulePath</c> that leaves the origin of the page, with the rule of <c>MonacoPath</c>.</summary>
    /// <param name="interopModulePath">The <c>InteropModulePath</c> parameter.</param>
    /// <exception cref="ArgumentException">The path leaves the origin of the page.</exception>
    internal static void RequireInteropModulePath(string interopModulePath) =>
        OmniSameOriginPath.Require(interopModulePath, "InteropModulePath");

    /// <summary>
    /// Imports the script once. A component disposed while the import was pending releases the module at
    /// once rather than keeping one nothing will dispose; false then tells the caller to stop.
    /// </summary>
    internal async Task<bool> EnsureModuleAsync()
    {
        if (Module is not null)
        {
            return !isDisposed();
        }

        var module = await OmniScriptModule.ImportAsync(javaScript, modulePath(), isDisposed);
        if (module is null)
        {
            return false;
        }

        if (Module is null)
        {
            Module = module;
        }
        else
        {
            // Another render imported it meanwhile: keep that one.
            await module.DisposeAsync();
        }

        return true;
    }

    /// <summary>Applies the height to the root element; false when the component went while the script loaded.</summary>
    internal async Task<bool> SetHeightAsync(ElementReference root, string? height)
    {
        if (!await EnsureModuleAsync())
        {
            return false;
        }

        await Module!.InvokeVoidAsync("setHeight", root, height);
        return true;
    }

    /// <summary>
    /// Loads Monaco through the script (which keeps one load per page) with the culture of the UI and the timeout.
    /// Null when the component went while the script loaded; otherwise whether Monaco is available.
    /// </summary>
    internal async Task<bool?> LoadAsync(string monacoPath)
    {
        if (!await EnsureModuleAsync())
        {
            return null;
        }

        return await Module!.InvokeAsync<bool>("load", monacoPath, CultureInfo.CurrentUICulture.Name, LoadTimeoutMilliseconds);
    }

    /// <summary>Disposes the mounted editor, if any, through the script's <paramref name="disposeMethod"/>.</summary>
    internal async Task UnmountAsync(string disposeMethod, ElementReference host)
    {
        if (Mounted && Module is not null)
        {
            Mounted = false;
            await Module.InvokeVoidAsync(disposeMethod, host);
        }

        Mounted = false;
    }

    /// <summary>Disposes the mounted editor and the module; a lost circuit is ignored.</summary>
    internal async Task ReleaseAsync(string disposeMethod, ElementReference host)
    {
        if (Module is null)
        {
            return;
        }

        try
        {
            if (Mounted)
            {
                await Module.InvokeVoidAsync(disposeMethod, host);
            }

            await Module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already gone, and the editor with it.
        }
    }
}
