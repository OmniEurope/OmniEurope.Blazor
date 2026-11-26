using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The <c>SelectOnFocus</c> state of a single-input text-entry field (<c>OmniNumeric</c>, <c>OmniTextArea</c>,
/// <c>OmniPassword</c>): the input element, captured by the field's <c>@ref</c>, and the
/// <see cref="OmniSelectOnFocus"/> script that follows it, created the first time the parameter is on.
/// </summary>
internal sealed class OmniSelectOnFocusField
{
    private OmniSelectOnFocus? _script;

    /// <summary>The input element, assigned by the field's <c>@ref</c>.</summary>
    internal ElementReference Element { get; set; }

    /// <summary>What the field's <c>OnAfterRenderAsync</c> does: see <see cref="OmniSelectOnFocus.FollowAsync"/>.</summary>
    internal Task FollowAsync(IJSRuntime javaScript, bool enabled) =>
        OmniSelectOnFocus.FollowAsync(ref _script, javaScript, enabled, Element);

    /// <summary>What the field's <c>DisposeAsync</c> does: see <see cref="OmniSelectOnFocus.DisposeWithInputAsync"/>.</summary>
    internal ValueTask DisposeWithInputAsync(IDisposable input) =>
        OmniSelectOnFocus.DisposeWithInputAsync(input, _script);
}
