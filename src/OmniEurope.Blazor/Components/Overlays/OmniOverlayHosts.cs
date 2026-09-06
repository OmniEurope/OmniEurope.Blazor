using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

internal static class OmniOverlayHosts
{
    private const string LegacyFrenchCloseLabel = "Fermer";

    internal static RenderFragment Dialog(OmniOverlayService service, Func<bool, Task> openChanged) => builder =>
    {
        var dialogs = service.Dialogs;
        if (dialogs.Count == 0)
        {
            return;
        }

        var sequence = 0;
        for (var index = 0; index < dialogs.Count; index++)
        {
            var dialog = dialogs[index];
            builder.OpenComponent<OmniDialog>(sequence++);
            builder.SetKey(dialog);
            builder.AddAttribute(sequence++, nameof(OmniDialog.Open), true);
            builder.AddAttribute(sequence++, nameof(OmniDialog.Title), dialog.Title);
            builder.AddAttribute(
                sequence++,
                nameof(OmniDialog.CloseLabel),
                string.Equals(dialog.CloseLabel, LegacyFrenchCloseLabel, StringComparison.Ordinal)
                    ? string.Empty
                    : dialog.CloseLabel);
            builder.AddAttribute(sequence++, nameof(OmniDialog.OpenChanged), EventCallback.Factory.Create<bool>(service, openChanged));
            builder.AddAttribute(sequence++, nameof(OmniDialog.ChildContent), dialog.Content);
            builder.AddAttribute(sequence++, nameof(OmniDialog.Footer), dialog.Footer);
            builder.AddAttribute(
                sequence++,
                nameof(OmniDialog.AdditionalAttributes),
                index < dialogs.Count - 1
                    ? new Dictionary<string, object?>
                    {
                        ["aria-hidden"] = "true",
                        ["inert"] = string.Empty
                    }
                    : null);
            builder.CloseComponent();
        }
    };

    internal static RenderFragment Notifications(OmniOverlayService service, Func<string, string> localize) => builder =>
    {
        builder.OpenElement(0, "section");
        builder.AddAttribute(1, "class", "omni-notification-region");
        builder.AddAttribute(2, "aria-label", localize("NotificationsRegionLabel"));
        var sequence = 3;
        foreach (var notification in service.Notifications)
        {
            builder.OpenComponent<OmniNotification>(sequence++);
            builder.AddAttribute(sequence++, nameof(OmniNotification.Message), notification.Message);
            builder.AddAttribute(sequence++, nameof(OmniNotification.Title), notification.Title);
            builder.AddAttribute(sequence++, nameof(OmniNotification.Severity), notification.Severity);
            builder.AddAttribute(sequence++, nameof(OmniNotification.OnDismiss), EventCallback.Factory.Create(service, () => { service.Dismiss(notification.Id); }));
            builder.CloseComponent();
        }
        builder.CloseElement();
    };

    internal static RenderFragment Portal(OmniOverlayCoordinator coordinator) => builder =>
    {
        if (coordinator.Entries.Count == 0)
        {
            return;
        }

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "omni-overlay-portal");
        builder.AddAttribute(2, "data-overlay-count", coordinator.Entries.Count);
        var sequence = 3;
        foreach (var entry in coordinator.Entries)
        {
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", $"omni-overlay-portal__entry omni-overlay-portal__entry--{entry.Kind.ToString().ToLowerInvariant()}");
            builder.AddContent(sequence++, entry.Content);
            builder.CloseElement();
        }
        builder.CloseElement();
    };
}
