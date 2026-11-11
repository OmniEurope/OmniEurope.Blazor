using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// An Escape typed on the toggle right after it opened the panel, before <c>omni-focus.js</c> listens: the
/// script is attached only once its module is imported, after the render that opens the panel. The import
/// is held here, so the picker alone can close the panel; once the script listens, Escape is its own.
/// </summary>
public sealed class PickerEarlyEscapeTests : OmniBunitContext
{
    private static IRenderedComponent<PickerTestHost> RenderHost(BunitContext context) =>
        context.Render<PickerTestHost>(builder => builder.Add(component => component.Model, new PickerTestHost.PickerTestModel()));

    [Theory]
    [InlineData("date")]
    [InlineData("time")]
    [InlineData("moment")]
    public async Task Escape_on_the_toggle_before_the_script_listens_closes_the_panel(string id)
    {
        // The module import never answers: the panel is open and nothing is attached yet.
        Services.AddSingleton<IJSRuntime>(new HeldImports());
        var host = RenderHost(this);

        host.Find($"#{id} ~ .omni-date__toggle").Click();
        Assert.Single(host.FindAll(".omni-calendar"));

        host.Find($"#{id} ~ .omni-date__toggle").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Single(host.FindAll(".omni-calendar"));

        await host.Find($"#{id} ~ .omni-date__toggle").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(host.FindAll(".omni-calendar"));
        Assert.Equal("false", host.Find($"#{id} ~ .omni-date__toggle").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Escape_on_the_toggle_once_the_script_listens_is_left_to_the_script()
    {
        var module = JSInterop.SetupModule(OmniModules.Focus);
        module.Mode = JSRuntimeMode.Loose;
        var host = RenderHost(this);

        host.Find("#time ~ .omni-date__toggle").Click();
        Assert.Single(module.Invocations["attachPicker"]);

        host.Find("#time ~ .omni-date__toggle").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Single(host.FindAll(".omni-calendar"));
    }

    /// <summary>A script runtime whose module imports never complete, as on a slow first opening.</summary>
    private sealed class HeldImports : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => HeldScript.Never<TValue>();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => HeldScript.Never<TValue>();
    }
}

/// <summary>The never-completing import of the held runtime, kept apart from its script interface.</summary>
file static class HeldScript
{
    internal static ValueTask<TValue> Never<TValue>() => new(new TaskCompletionSource<TValue>().Task);
}
