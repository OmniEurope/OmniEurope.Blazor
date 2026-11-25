using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>The grid script as the grid's other collaborators reach it, from <see cref="GridScriptBridge{TItem}"/>.</summary>
internal interface IGridScriptBridge
{
    IJSObjectReference? Module { get; }

    bool Veiled { get; }

    bool CanAutoFit { get; }

    Task<IJSObjectReference> ModuleAsync();

    Task AutoFitColumnAsync(string key);

    Task ScrollToOffsetAsync(double offset);

    Task EnsureRowKeysAsync();

    Task FocusMoveControlAsync(ElementReference list, string key, int step);

    Task CloseFilterMenusAsync();
}
