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

    internal static RenderFragment Notifications(
        OmniOverlayService service,
        Func<string, string> localize,
        OmniNotificationOptions options) => builder =>
    {
        var notifications = service.Notifications;
        var position = options.Position.ToString().ToLowerInvariant();
        // Errors never fold into the pile: what went wrong stays readable in full.
        var errors = options.Group
            ? notifications.Where(notification => notification.Severity == OmniNotificationSeverity.Error).ToList()
            : [];
        var others = options.Group
            ? notifications.Where(notification => notification.Severity != OmniNotificationSeverity.Error).ToList()
            : [.. notifications];
        // A pile only stacks once there are two cards to stack, and only then carries a count.
        var stacked = options.Group && others.Count > 1;
        // A long message widens its card. In a pile every card takes the width of the widest, or
        // the cards stop lining up behind one another.
        var wide = notifications.Any(notification => notification.Message.Length > OmniNotificationStore.LongMessageThreshold);
        builder.OpenElement(0, "section");
        builder.AddAttribute(1, "class", CssClassBuilder.Combine([
            "omni-notification-region",
            $"omni-notification-region--{position}",
            options.Group ? "omni-notification-region--grouped" : null,
            wide ? "omni-notification-region--wide" : null]));
        builder.AddAttribute(2, "aria-label", localize("NotificationsRegionLabel"));

        if (!options.Group)
        {
            Cards(builder, 10, others, service, options);
        }
        else
        {
            Cards(builder, 20, errors, service, options);

            // The pile element is there as soon as grouping is on, even for a single card: a card
            // that changed parent when the second one arrived would be rebuilt, its tint restarting.
            builder.OpenElement(30, "div");
            builder.AddAttribute(31, "class", stacked
                ? "omni-notification-region__pile omni-notification-region__pile--stacked"
                : "omni-notification-region__pile");
            Cards(builder, 32, others, service, options);

            builder.CloseElement();

            if (stacked)
            {
                builder.OpenElement(40, "p");
                builder.AddAttribute(41, "class", "omni-notification-region__count");
                builder.AddContent(42, others.Count);
                builder.CloseElement();
            }
        }

        builder.CloseElement();
    };

    // One region around the whole list: the keys only match cards that share a sibling range, so a
    // region per card would rebuild every card after one that closes, restarting its tint.
    private static void Cards(RenderTreeBuilder builder, int sequence, IEnumerable<OmniNotificationMessage> notifications, OmniOverlayService service, OmniNotificationOptions options)
    {
        builder.OpenRegion(sequence);
        foreach (var notification in notifications)
        {
            Card(builder, notification, service, options);
        }

        builder.CloseRegion();
    }

    private static void Card(RenderTreeBuilder builder, OmniNotificationMessage notification, OmniOverlayService service, OmniNotificationOptions options)
    {
        builder.OpenComponent<OmniNotification>(0);
        builder.SetKey(notification.Id);
        builder.AddAttribute(1, nameof(OmniNotification.Message), notification.Message);
        builder.AddAttribute(2, nameof(OmniNotification.Title), notification.Title);
        builder.AddAttribute(3, nameof(OmniNotification.Severity), notification.Severity);
        builder.AddAttribute(4, nameof(OmniNotification.Dismissible), options.Dismissible);
        builder.AddAttribute(5, nameof(OmniNotification.ShowCountdown), options.ShowCountdown);
        builder.AddAttribute(6, nameof(OmniNotification.Duration), notification.Duration);
        builder.AddAttribute(7, nameof(OmniNotification.DetailsHref), notification.DetailsHref);
        builder.AddAttribute(8, nameof(OmniNotification.OnHeldChanged), EventCallback.Factory.Create<bool>(service, held =>
        {
            if (held)
            {
                service.PauseNotification(notification.Id);
            }
            else
            {
                service.ResumeNotification(notification.Id);
            }
        }));
        builder.AddAttribute(9, nameof(OmniNotification.OnDismiss), EventCallback.Factory.Create(service, () => { service.Dismiss(notification.Id); }));
        builder.CloseComponent();
    }

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
