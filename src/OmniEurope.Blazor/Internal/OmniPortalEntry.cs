using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

internal sealed record OmniPortalEntry(object Owner, OmniPortalKind Kind, RenderFragment Content, Func<Task> CloseAsync);
