using System.Net;
using System.Net.Sockets;

namespace OmniEurope.Blazor.Showcase.Back.Components.Shared;

/// <summary>The key a per-client limit is counted under.</summary>
public static class ClientAddress
{
    /// <summary>
    /// The IPv4 address itself, or the /64 network of an IPv6 address: a single IPv6 subscriber usually
    /// holds a whole /64 and could otherwise take a fresh address, hence a fresh quota, for every request.
    /// Behind the reverse proxy the address is the client's only once <c>ForwardedHeaders:Enabled</c> is
    /// set; otherwise every request shares the proxy's address and one partition.
    /// </summary>
    /// <param name="address">The remote address of the connection; null in tests without a connection.</param>
    /// <returns>A partition key such as <c>203.0.113.7</c> or <c>2001:db8:0:1::/64</c>.</returns>
    public static string Partition(IPAddress? address)
    {
        if (address is null)
        {
            return "unknown";
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return address.ToString();
        }

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes) + "/64";
    }
}
