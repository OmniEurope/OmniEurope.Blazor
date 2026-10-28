using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Showcase.Examples.Shell;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The Preferences example of the gallery says what it really holds. The browser (<see cref="PreferenceBrowser"/>)
/// and the store (<see cref="MemoryPreferenceStore"/>) are test doubles here: these tests prove the example
/// reports "no store", "saved", "not sent yet" and "store unreachable" correctly and writes the key through
/// <c>OmniPreferences</c>. That the showcase API and PostgreSQL really keep the value is proved by the API
/// tests and by the running site.
/// </summary>
public sealed class ShowcasePreferencesExampleTests : OmniBunitContext
{
    private readonly PreferenceBrowser _browser = new();
    private readonly MemoryPreferenceStore _store = new();

    private void UseStore() => Services.AddSingleton<IOmniPreferenceStore>(_store);

    private IRenderedComponent<PreferencesExample> RenderExample()
    {
        Services.AddSingleton<IJSRuntime>(_browser);
        Services.AddSingleton<TimeProvider>(new PreferenceClock(new DateTimeOffset(2026, 10, 28, 8, 0, 0, TimeSpan.Zero)));
        return Render<PreferencesExample>();
    }

    private static void Save(IRenderedComponent<PreferencesExample> example, string value)
    {
        example.Find("#preferences-value").Input(value);
        example.Find("#preferences-save").Click();
    }

    [Fact]
    public void WithoutAStore_TheExampleSaysTheValueStaysInTheBrowser()
    {
        var example = RenderExample();

        Save(example, "hello");

        example.WaitForAssertion(() => Assert.Equal("hello", example.Find("#preferences-stored").TextContent));
        Assert.Equal("(no store)", example.Find("#preferences-database").TextContent);
        Assert.Contains("No preference store is registered", example.Find("#preferences-status").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void WithAStore_SavingWritesTheKeyThroughThePreferenceServiceAndReadsItBackFromTheStore()
    {
        UseStore();
        var example = RenderExample();

        Save(example, "hello");

        example.WaitForAssertion(() => Assert.Equal("hello", example.Find("#preferences-database").TextContent));
        Assert.Equal("hello", example.Find("#preferences-stored").TextContent);
        Assert.Equal("\"hello\"", _store["example.preference"]?.Value);
        Assert.Contains("The store holds the value this browser holds", example.Find("#preferences-status").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenTheStoreRefusesTheWrite_TheExampleDoesNotClaimItIsSaved()
    {
        UseStore();
        _store.FailWrites = true;
        var example = RenderExample();

        Save(example, "hello");

        example.WaitForAssertion(() => Assert.Equal("hello", example.Find("#preferences-stored").TextContent));
        Assert.Equal("(none)", example.Find("#preferences-database").TextContent);
        Assert.Contains("does not hold this value yet", example.Find("#preferences-status").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("The store holds the value", example.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenTheStoreCannotBeRead_TheExampleSaysSoWithTheReason()
    {
        UseStore();
        _store.FailReads = true;

        var example = RenderExample();

        example.WaitForAssertion(() => Assert.Contains("could not be reached (database down)", example.Find("#preferences-status").TextContent, StringComparison.Ordinal));
        Assert.DoesNotContain("The store holds the value", example.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenTheSynchronizationFails_TheExampleReportsIt()
    {
        UseStore();
        var example = RenderExample();
        _store.FailReads = true;

        example.Find("#preferences-sync").Click();

        example.WaitForAssertion(() => Assert.Contains("synchronization is unavailable", example.Find("#preferences-sync-failure").TextContent, StringComparison.Ordinal));
    }
}
