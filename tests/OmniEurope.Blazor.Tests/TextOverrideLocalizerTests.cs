using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Localization;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Aetheus recette R-130 and R-135: a text that belongs to the application lives in the host's
/// resources and replaces the package text; every other key keeps the package's.
/// </summary>
public sealed class TextOverrideLocalizerTests
{
    [Fact]
    public void HostKey_WinsOverThePackageText_OtherKeysKeepThePackageText()
    {
        var localizer = new OmniTextOverrideLocalizer(
            new MapLocalizer(new() { ["ConnectionReconnectNow"] = "Reconnect now", ["ConnectionReload"] = "Reload the page" }),
            new MapLocalizer(new() { ["Omni_ConnectionReconnectNow"] = "Reconnect" }),
            "Omni_");

        Assert.Equal("Reconnect", localizer["ConnectionReconnectNow"].Value);
        Assert.False(localizer["ConnectionReconnectNow"].ResourceNotFound);
        Assert.Equal("Reload the page", localizer["ConnectionReload"].Value);
    }

    [Fact]
    public void Registration_RoutesThePackageLocalizerThroughTheHost()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOmniEuropeBlazor();
        services.AddSingleton<IStringLocalizer<HostTexts>>(new MapLocalizer<HostTexts>(new() { ["Omni_ConnectionReconnectNow"] = "Reconnect" }));
        services.AddOmniEuropeTextOverrides<HostTexts>();

        using var provider = services.BuildServiceProvider();
        var localizer = provider.GetRequiredService<IStringLocalizer<AppStrings>>();

        Assert.IsType<OmniTextOverrideLocalizer>(localizer);
        Assert.Equal("Reconnect", localizer["ConnectionReconnectNow"].Value);
    }

    public sealed class HostTexts;

    private class MapLocalizer(Dictionary<string, string> texts) : IStringLocalizer
    {
        public LocalizedString this[string name] =>
            texts.TryGetValue(name, out var value) ? new(name, value) : new(name, name, true);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            texts.Select(pair => new LocalizedString(pair.Key, pair.Value));
    }

    private sealed class MapLocalizer<T>(Dictionary<string, string> texts) : MapLocalizer(texts), IStringLocalizer<T>;
}
