using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The text size and the size of the controls of <see cref="Components.OmniAppearanceState"/>, laid on the
/// document root (<c>data-oe-text-size</c>, <c>data-oe-control-size</c>, which the package stylesheet
/// reads) through <c>omni-appearance.js</c>, while at least one theme scope follows the state. The
/// default level leaves the attribute out, as a page that never set it; the last scope to leave
/// removes both.
/// </summary>
internal sealed class AppearanceDocumentRoot(IJSRuntime javaScript, ILogger logger)
{
    private IJSObjectReference? _module;
    private int _scopes;
    private bool _written;
    private int _text;
    private int _control;

    /// <summary>A scope that follows the state starts applying it; its next <see cref="SyncAsync"/> writes both levels.</summary>
    internal void Attach()
    {
        _scopes++;
        // The root may carry levels from elsewhere (the boot copy of the snapshot): written again in full.
        _written = false;
    }

    /// <summary>Writes the levels that changed since the last call, when a scope applies them.</summary>
    internal async Task SyncAsync(int textSizeLevel, int controlSizeLevel)
    {
        if (_scopes == 0 || (_written && textSizeLevel == _text && controlSizeLevel == _control))
        {
            return;
        }

        _module ??= await OmniScriptModule.ImportAsync(javaScript, OmniModules.Appearance);
        if (!_written || textSizeLevel != _text)
        {
            await WriteAsync("TextSizeLevel", textSizeLevel);
        }

        if (!_written || controlSizeLevel != _control)
        {
            await WriteAsync("ControlSizeLevel", controlSizeLevel);
        }

        _text = textSizeLevel;
        _control = controlSizeLevel;
        _written = true;
    }

    /// <summary>A scope stops applying the state; once none does, both attributes leave the root.</summary>
    internal async Task DetachAsync()
    {
        _scopes = Math.Max(0, _scopes - 1);
        if (_scopes > 0 || _module is null)
        {
            return;
        }

        _written = false;
        try
        {
            await _module.InvokeVoidAsync("clearTextSizeLevel");
            await _module.InvokeVoidAsync("clearControlSizeLevel");
        }
        catch (JSDisconnectedException)
        {
            // The page is gone, and its root with it.
        }
        catch (JSException exception)
        {
            // The script refused: the levels may stay on the root, the scope still finishes leaving.
            logger.LogWarning(exception, "The text and control size levels could not be removed from the document root.");
        }
    }

    private ValueTask WriteAsync(string setting, int level) => level == AppearanceLook.DefaultLevel
        ? _module!.InvokeVoidAsync("clear" + setting)
        : _module!.InvokeVoidAsync("set" + setting, level);
}
