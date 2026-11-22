using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>Renders the components that show a severity side by side, for the tests that compare their glyphs.</summary>
internal static class SeverityRendering
{
    /// <summary>Renders a notification and an alert of <paramref name="severity"/>, both saying "Message".</summary>
    public static (IRenderedComponent<OmniNotification> Notification, IRenderedComponent<OmniAlert> Alert) RenderNotificationAndAlert(
        this BunitContext context, OmniSeverity severity) =>
        (context.Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, "Message")
            .Add(component => component.Severity, severity)),
        context.Render<OmniAlert>(parameters => parameters
            .Add(component => component.Severity, severity)
            .AddChildContent("Message")));
}
