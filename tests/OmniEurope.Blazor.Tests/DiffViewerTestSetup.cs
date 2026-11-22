using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>The diff viewer setups the diff viewer suites share: a Monaco load answered or held, and a viewer of one text.</summary>
internal static class DiffViewerTestSetup
{
    /// <summary>The code editor module, whose Monaco load answers <paramref name="loaded"/>, or waits when it is null.</summary>
    internal static BunitJSModuleInterop SetupMonacoLoad(this BunitJSInterop interop, bool? loaded)
    {
        var module = interop.SetupModule(OmniModules.CodeEditor);
        var load = module.Setup<bool>("load", _ => true);
        if (loaded is { } value)
        {
            load.SetResult(value);
        }

        return module;
    }

    /// <summary>A viewer whose original text is <paramref name="original"/>.</summary>
    internal static IRenderedComponent<OmniDiffViewer> RenderDiffOf(this BunitContext context, string original) =>
        context.Render<OmniDiffViewer>(parameters => parameters.Add(component => component.Original, original));

    /// <summary>A viewer of "a", once its Monaco editor has been mounted through <paramref name="module"/>.</summary>
    internal static IRenderedComponent<OmniDiffViewer> RenderMountedDiff(this BunitContext context, BunitJSModuleInterop module)
    {
        var viewer = context.RenderDiffOf("a");
        viewer.WaitForAssertion(() => Assert.Single(module.Invocations["mountDiff"]));
        return viewer;
    }
}
