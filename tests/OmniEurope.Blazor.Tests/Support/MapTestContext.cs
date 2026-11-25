using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>A test context over bUnit's double of the map script, for the map test classes.</summary>
public abstract class MapTestContext : OmniBunitContext
{
    protected static readonly OmniGeoPoint Brussels = new(50.8466, 4.3528);

    protected MapTestContext() => MapModule = JSInterop.SetupModule(OmniModules.Map);

    /// <summary>bUnit's double of the map script.</summary>
    protected BunitJSModuleInterop MapModule { get; }

    protected IRenderedComponent<OmniMap> RenderMap(Action<ComponentParameterCollectionBuilder<OmniMap>>? configure = null)
        => Render<OmniMap>(parameters => configure?.Invoke(parameters));
}
