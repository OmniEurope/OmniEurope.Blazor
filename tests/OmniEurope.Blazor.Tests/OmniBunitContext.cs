using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// bUnit context wired the way a host application wires the library: the components resolve their
/// localizer from the container instead of reaching around it.
/// </summary>
public abstract class OmniBunitContext : BunitContext
{
    private bool _rendererInfoSet;

    protected OmniBunitContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddOmniEuropeBlazor();
    }

    public new void SetRendererInfo(RendererInfo info)
    {
        base.SetRendererInfo(info);
        _rendererInfoSet = true;
    }

    public override IRenderedComponent<TComponent> Render<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>>? parameterBuilder = null)
    {
        EnsureRendererInfo();
        return base.Render(parameterBuilder);
    }

    public override IRenderedComponent<TComponent> Render<TComponent>(RenderFragment renderFragment)
    {
        EnsureRendererInfo();
        return base.Render<TComponent>(renderFragment);
    }

    public override IRenderedComponent<ContainerFragment> Render(RenderFragment renderFragment)
    {
        EnsureRendererInfo();
        return base.Render(renderFragment);
    }

    private void EnsureRendererInfo()
    {
        // Wait until rendering so the test can register services. Explicit static modes win.
        if (!_rendererInfoSet)
        {
            SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        }
    }
}
