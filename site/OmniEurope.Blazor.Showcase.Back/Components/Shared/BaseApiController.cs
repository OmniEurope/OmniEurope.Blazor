using Microsoft.AspNetCore.Mvc;

namespace OmniEurope.Blazor.Showcase.Back.Components.Shared;

/// <summary>
/// The base of the showcase controllers. The showcase has no account: there is no user to read here,
/// only the client address that the rate limits and the visitor creation limit partition by.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>The partition of the caller's address (<see cref="ClientAddress.Partition"/>).</summary>
    protected string ClientPartition => ClientAddress.Partition(HttpContext.Connection.RemoteIpAddress);
}
