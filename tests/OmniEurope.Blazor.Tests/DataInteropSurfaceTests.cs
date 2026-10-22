using System.Reflection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Tests.Guards;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// What the Data scripts may call in .NET is an internal bridge, never a public method of the component:
/// the callable surface stays out of the public API, as it does for the board (KanbanInteropBridge).
/// </summary>
public sealed class DataInteropSurfaceTests
{
    public static TheoryData<Type> Components => new()
    {
        typeof(OmniDataGrid<>),
        typeof(OmniDataList<>),
        typeof(OmniLogViewer)
    };

    [Theory]
    [MemberData(nameof(Components))]
    public void TheComponent_ExposesNoPublicJavaScriptCallback(Type component)
    {
        var callable = component.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<JSInvokableAttribute>() is not null)
            .Select(method => method.Name);

        Assert.Empty(callable);
    }

    [Fact]
    public void TheBridges_AnswerEveryCallbackTheScriptsMake()
    {
        var scripts = string.Join('\n', RepositoryScan
            .Enumerate(Path.Combine(RepositoryScan.Root, "src", "OmniEurope.Blazor", "wwwroot"), "*.js")
            .Select(File.ReadAllText));

        foreach (var bridge in new[] { typeof(GridInteropBridge<int>), typeof(ListInteropBridge), typeof(LogViewerInteropBridge) })
        {
            var callable = bridge.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttribute<JSInvokableAttribute>() is not null)
                .Select(method => method.Name)
                .ToList();

            Assert.NotEmpty(callable);
            foreach (var name in callable)
            {
                Assert.Contains("'" + name + "'", scripts, StringComparison.Ordinal);
            }
        }

        var grid = typeof(GridInteropBridge<int>).GetMethods().Select(method => method.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var name in new[]
        {
            "OnHorizontalScrollChangedAsync", "OnContentResizedAsync", "OnViewportChangedAsync",
            "GetColumnAutoFitTexts", "OnColumnAutoFitAsync", "OnColumnResizedAsync", "OnColumnDroppedAsync"
        })
        {
            Assert.Contains(name, grid);
        }
    }
}
