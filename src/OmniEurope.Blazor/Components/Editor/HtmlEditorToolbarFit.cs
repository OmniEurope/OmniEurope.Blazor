using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The toolbar of an <see cref="OmniHtmlEditor"/> seen from the script: the package tooltips of its
/// controls (their name and a command's description, Astraia recette R-043), and, under
/// <see cref="OmniHtmlEditor.ToolbarRows"/>, the buttons moved to the "more" menu because they would
/// open a row beyond the limit (R-042). The script measures; this keeps the names it reports.
/// </summary>
internal sealed class HtmlEditorToolbarFit(OmniHtmlEditor owner, IJSRuntime javaScript) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<HtmlEditorToolbarFit>? _self;
    private bool _tooltips;
    private bool _fitting;
    private ElementReference _toolbar;

    /// <summary>The commands the script moved to the menu, by name; empty without a row limit.</summary>
    internal IReadOnlySet<string> Overflowed { get; private set; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Turns the tooltips on once, then holds the toolbar to its rows, or lets it wrap freely again.</summary>
    internal async Task SyncAsync(ElementReference toolbar, int? rows)
    {
        try
        {
            _module ??= await javaScript.InvokeAsync<IJSObjectReference>("import", OmniModules.HtmlEditor);
            if (!_tooltips)
            {
                await _module.InvokeVoidAsync("installPackageTooltips");
                _tooltips = true;
            }

            if (rows is { } limit)
            {
                _self ??= DotNetObjectReference.Create(this);
                _toolbar = toolbar;
                _fitting = true;
                await _module.InvokeVoidAsync("fitToolbar", toolbar, limit, _self);
            }
            else if (_fitting)
            {
                _fitting = false;
                await _module.InvokeVoidAsync("unfitToolbar", _toolbar);
                Overflowed = new HashSet<string>(StringComparer.Ordinal);
            }
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>The script moved these buttons to the menu (in toolbar order), or brought them back.</summary>
    [JSInvokable]
    public Task OnToolbarOverflow(string[] names) => owner.DispatchAsync(() =>
    {
        Overflowed = new HashSet<string>(names, StringComparer.Ordinal);
    });

    /// <summary>Stops the fitting, turns this editor's share of the tooltips off and releases the module.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                if (_fitting)
                {
                    await _module.InvokeVoidAsync("unfitToolbar", _toolbar);
                }

                if (_tooltips)
                {
                    await _module.InvokeVoidAsync("uninstallPackageTooltips");
                }

                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }

        _self?.Dispose();
    }
}
