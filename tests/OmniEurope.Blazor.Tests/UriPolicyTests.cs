using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniUriPolicy"/> keeps a link inside the application or on an allowed scheme: a relative
/// address that a browser reads as another host (<c>//host</c>, <c>/\host</c>) is refused, whatever the
/// platform the server runs on, while every ordinary relative path and allowed absolute address passes.
/// </summary>
public sealed class UriPolicyTests
{
    [Theory]
    [InlineData("//evil.example")]
    [InlineData("//evil.example/path")]
    [InlineData("/\\evil.example")]
    [InlineData("\\\\evil.example")]
    [InlineData("\\/evil.example")]
    [InlineData("\\evil.example")]
    [InlineData("  //evil.example")]
    [InlineData("//evil.example  ")]
    [InlineData(" //evil.example")]
    [InlineData("/%2Fevil.example")]
    [InlineData("/%2fevil.example")]
    [InlineData("%2F%2Fevil.example")]
    [InlineData("%2f/evil.example")]
    [InlineData("/%5Cevil.example")]
    [InlineData("/%5cevil.example")]
    [InlineData("%5Cevil.example")]
    [InlineData("///evil.example")]
    [InlineData("////evil.example")]
    public void EnsureSafe_RefusesAnAddressABrowserReadsAsAnotherHost(string value) =>
        Assert.Throws<InvalidOperationException>(() => OmniUriPolicy.EnsureSafe(value, "Href"));

    [Theory]
    [InlineData("/x")]
    [InlineData("x")]
    [InlineData("x/y")]
    [InlineData("./x")]
    [InlineData("../x")]
    [InlineData("../../x/y?z=1#a")]
    [InlineData("?q=1")]
    [InlineData("#a")]
    [InlineData("/")]
    [InlineData("/segment?returnUrl=%2Fhome")]
    [InlineData("/a%2Fb")]
    [InlineData("/a/b/")]
    [InlineData("/search?next=//elsewhere")]
    [InlineData("/go#//frag")]
    [InlineData("a%5Cb")]
    [InlineData("https://exemple.org/x")]
    [InlineData("http://exemple.org")]
    [InlineData("HTTPS://EXEMPLE.ORG")]
    [InlineData("mailto:contact@exemple.org")]
    [InlineData("tel:+3225551234")]
    public void EnsureSafe_KeepsOrdinaryRelativePathsAndAllowedAbsoluteAddresses(string value) =>
        Assert.Equal(value, OmniUriPolicy.EnsureSafe(value, "Href"));

    [Fact]
    public void EnsureSafe_TrimsTheAddressItReturns() =>
        Assert.Equal("/x", OmniUriPolicy.EnsureSafe("  /x  ", "Href"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureSafe_LeavesAnEmptyAddressAlone(string? value) =>
        Assert.Equal(value, OmniUriPolicy.EnsureSafe(value, "Href"));

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,x")]
    [InlineData("vbscript:x")]
    [InlineData("file:///etc/passwd")]
    public void EnsureSafe_RefusesDisallowedSchemes(string value) =>
        Assert.Throws<InvalidOperationException>(() => OmniUriPolicy.EnsureSafe(value, "Href"));

    [Theory]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    public async Task GoBack_RefusesAHostOutsideTheApplication(string href)
    {
        using var context = new BackTestContext();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var start = navigation.Uri;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            OmniBackNavigation.GoBackAsync(navigation, context.JSInterop.JSRuntime, href));

        Assert.Equal(start, navigation.Uri);
    }

    [Fact]
    public async Task GoBack_FollowsARelativePath()
    {
        using var context = new BackTestContext();
        var navigation = context.Services.GetRequiredService<NavigationManager>();

        await OmniBackNavigation.GoBackAsync(navigation, context.JSInterop.JSRuntime, "/retour");

        Assert.EndsWith("/retour", navigation.Uri, StringComparison.Ordinal);
    }

    private sealed class BackTestContext : OmniBunitContext;
}
