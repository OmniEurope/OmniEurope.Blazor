using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What a text-entry field does for <c>SelectOnFocus</c>: follows its input element with the
/// <c>omni-select-on-focus.js</c> module, which selects the whole content when the field is entered. Shared by
/// <c>OmniTextBox</c>, <c>OmniNumeric</c>, <c>OmniTextArea</c> and <c>OmniPassword</c>. The module is imported
/// the first time the parameter is on and never before; the owner calls <see cref="SyncAsync"/> after each
/// render and <see cref="DisposeAsync"/> when it is disposed.
/// </summary>
internal sealed class OmniSelectOnFocus(IJSRuntime javaScript) : IAsyncDisposable
{
    // Serialises the calls: on a Blazor Server circuit a later render can run while an earlier one still awaits the module.
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IJSObjectReference? _module;
    private ElementReference? _attached;
    private bool _disposed;
    private bool _everEnabled;

    /// <summary>
    /// Attaches the script to <paramref name="field"/> when <paramref name="enabled"/>, follows it when the element is
    /// drawn anew, and detaches it when the parameter is turned off. Nothing is imported while it has never been on.
    /// A lost circuit is ignored.
    /// </summary>
    internal async Task SyncAsync(bool enabled, ElementReference field)
    {
        _everEnabled |= enabled;
        if (!_everEnabled)
        {
            return;
        }

        await _gate.WaitAsync();
        try
        {
            if (_disposed)
            {
                return;
            }

            if (enabled)
            {
                _module ??= await OmniScriptModule.ImportAsync(javaScript, OmniModules.SelectOnFocus);
                if (_attached?.Id == field.Id)
                {
                    return;
                }

                await DetachAsync();
                await _module.InvokeVoidAsync("attach", field);
                _attached = field;
            }
            else
            {
                await DetachAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the field with it.
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task DetachAsync()
    {
        if (_attached is { } former && _module is not null)
        {
            _attached = null;
            await _module.InvokeVoidAsync("detach", former);
        }
    }

    /// <summary>Detaches the script and releases the module; a lost circuit is ignored.</summary>
    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            _disposed = true;
            if (_module is null)
            {
                return;
            }

            try
            {
                await DetachAsync();
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone, and the module with it.
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}
