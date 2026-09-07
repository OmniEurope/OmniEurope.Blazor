using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Internal;

internal sealed record OmniPortalEntry(object Owner, OmniPortalKind Kind, RenderFragment Content, Func<Task> CloseAsync);
