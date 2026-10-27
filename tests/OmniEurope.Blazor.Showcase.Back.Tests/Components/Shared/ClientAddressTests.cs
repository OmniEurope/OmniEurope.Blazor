using System.Net;
using OmniEurope.Blazor.Showcase.Back.Components.Shared;
using OmniEurope.Blazor.Showcase.Back.Extensions;

namespace OmniEurope.Blazor.Showcase.Back.Tests.Components.Shared;

public sealed class ClientAddressTests
{
    [Theory]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("::ffff:203.0.113.7", "203.0.113.7")]
    [InlineData("2001:db8:0:1:aaaa:bbbb:cccc:dddd", "2001:db8:0:1::/64")]
    [InlineData("2001:db8:0:1::1", "2001:db8:0:1::/64")]
    public void Partition_KeepsAnIPv4Address_AndGroupsAnIPv6NetworkByItsSlash64(string address, string expected) =>
        Assert.Equal(expected, ClientAddress.Partition(IPAddress.Parse(address)));

    [Fact]
    public void Partition_WithoutAnAddress_IsOneSharedPartition() => Assert.Equal("unknown", ClientAddress.Partition(null));
}

public sealed class FrontOriginOptionsValidatorTests
{
    private static bool Valid(params string[] origins) => new FrontOriginOptionsValidator().Validate(null, new FrontOriginOptions { Origins = origins }).Succeeded;

    [Fact]
    public void Origins_AreRequired() => Assert.False(Valid());

    [Theory]
    [InlineData("https://showcase.example")]
    [InlineData("http://localhost:5280")]
    public void AnHttpOrigin_IsAccepted(string origin) => Assert.True(Valid(origin));

    [Theory]
    [InlineData("*")]
    [InlineData("https://showcase.example/")]
    [InlineData("https://showcase.example/app")]
    [InlineData("ftp://showcase.example")]
    [InlineData("showcase.example")]
    public void AnythingButAnOrigin_IsRefused(string origin) => Assert.False(Valid("https://ok.example", origin));
}
