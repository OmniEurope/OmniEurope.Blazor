using System.Reflection;
using Bunit;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Tests;

/// <summary>The dismissal the menu script reports, played through the .NET reference the first opened menu handed it.</summary>
internal static class MenuScriptDismissal
{
    /// <summary>
    /// Finds the invokable dismissal of the first <c>openMenu</c> call of <paramref name="focusModule"/>, checks the
    /// name the script calls it by, and calls it as the script does for a press outside the menu.
    /// </summary>
    internal static void DismissAsTheScriptDoes<TComponent>(IRenderedComponent<TComponent> rendered, BunitJSModuleInterop focusModule)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        var reference = focusModule.Invocations["openMenu"][0].Arguments[8]!;
        var interop = reference.GetType().GetProperty("Value")!.GetValue(reference)!;
        var dismiss = interop.GetType().GetMethod("DismissAsync", BindingFlags.Public | BindingFlags.Instance)!;
        Assert.Equal("OmniMenu.Dismiss", dismiss.GetCustomAttribute<JSInvokableAttribute>()!.Identifier);
        rendered.InvokeAsync(() => (Task)dismiss.Invoke(interop, [false])!);
    }
}
