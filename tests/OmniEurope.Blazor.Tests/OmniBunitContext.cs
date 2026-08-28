using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// bUnit context wired the way a host application wires the library: the components resolve their
/// localizer from the container instead of reaching around it.
/// </summary>
public abstract class OmniBunitContext : BunitContext
{
    protected OmniBunitContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddOmniEuropeBlazor();
    }
}
