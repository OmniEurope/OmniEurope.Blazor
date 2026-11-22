using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The HTML editor setups the editor suites share: the surface module that reads nothing back, and an
/// editor bound to a one-paragraph value.
/// </summary>
internal static class HtmlEditorRenderSetup
{
    /// <summary>The value the editors of the suites start from.</summary>
    internal const string StartingValue = "<p>A</p>";

    /// <summary>The editor surface module, whose first read of the browser content answers nothing.</summary>
    internal static BunitJSModuleInterop SetupEditorSurface(this BunitJSInterop interop)
    {
        var module = interop.SetupModule(OmniModules.HtmlEditor);
        module.Setup<string?>("read", _ => true).SetResult(null);
        return module;
    }

    /// <summary>An editor bound to <see cref="StartingValue"/>; <paramref name="extra"/> adds the rest.</summary>
    internal static IRenderedComponent<OmniHtmlEditor> RenderHtmlEditor(
        this BunitContext context,
        Action<ComponentParameterCollectionBuilder<OmniHtmlEditor>> extra)
    {
        var value = StartingValue;
        return context.Render<OmniHtmlEditor>(parameters =>
        {
            parameters
                .Add(component => component.Value, value)
                .Add(component => component.ValueExpression, () => value);
            extra(parameters);
        });
    }
}
