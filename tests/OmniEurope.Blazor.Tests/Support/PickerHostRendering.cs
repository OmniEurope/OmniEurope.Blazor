using AngleSharp.Dom;
using Bunit;

namespace OmniEurope.Blazor.Tests;

/// <summary>Renders the picker test host over a fresh model and finds its parts, for the picker test classes.</summary>
internal static class PickerHostRendering
{
    public static IRenderedComponent<PickerTestHost> RenderPickerHost(
        this BunitContext context,
        Action<PickerTestHost.PickerTestModel>? arrange = null,
        Action<ComponentParameterCollectionBuilder<PickerTestHost>>? parameters = null)
    {
        var model = new PickerTestHost.PickerTestModel();
        arrange?.Invoke(model);
        return context.Render<PickerTestHost>(builder =>
        {
            builder.Add(component => component.Model, model);
            parameters?.Invoke(builder);
        });
    }

    /// <summary>The toggle button next to the field <paramref name="id"/>.</summary>
    public static IElement PickerToggle(this IRenderedComponent<PickerTestHost> host, string id) => host.Find($"#{id} ~ .omni-date__toggle");
}
