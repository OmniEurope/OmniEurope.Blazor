using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Small internal helpers read on their own: e-mail domains too long or with a stray character, form
/// snapshots of awkward models, the back button, clipboard and disclosure scripts on a lost circuit or
/// after their owner went, form labels without a control, text matching without a query, unsafe
/// addresses, notification holds, colours, random choices and spreadsheet names.
/// </summary>
public sealed class MiscInternalsTests
{
    [Theory]
    [InlineData("a@b_c.org", false)]
    [InlineData("a@exemple.org", true)]
    public void EmailRule_RefusesAStrayCharacterInTheDomain(string text, bool valid) =>
        Assert.Equal(valid, EmailAddressRule.IsValid(text));

    [Fact]
    public void EmailRule_RefusesADomainLongerThanItsLimit() =>
        Assert.False(EmailAddressRule.IsValid("a@" + string.Join('.', Enumerable.Repeat(new string('d', 60), 5)) + ".org"));

    public sealed class Awkward
    {
        public Awkward? Self { get; set; }
        public string this[int index] => index.ToString(CultureInfo.InvariantCulture);
        public string Throws => throw new InvalidOperationException("lecture impossible");
        public string Name { get; set; } = "a";
        public string WriteOnly { set { } }
        public int Field = 1;
        public bool Fail;
        public string Flaky => Fail ? throw new InvalidOperationException("plus lisible") : "ok";
        public Child Nested { get; set; } = new();
        public List<int> Items { get; set; } = [1];
        public Uri Site { get; set; } = new("https://exemple.org");
        public EditContext? Form { get; set; }
    }

    public sealed class Child
    {
        public string Value { get; set; } = "v";
        public Child? Deeper { get; set; }
    }

    [Fact]
    public void FormSnapshot_OfACyclicModelWithIndexersAndFailingGetters_TracksWhatItCanRead()
    {
        var model = new Awkward();
        model.Self = model;
        model.Nested.Deeper = new Child();
        var snapshot = new FormSnapshot();
        snapshot.Take(model);

        model.Form = new EditContext(new object());

        // A field the snapshot never read (a public field, a failing getter) counts as changed.
        Assert.True(snapshot.Update(new FieldIdentifier(model, nameof(Awkward.Field))));
        Assert.True(snapshot.IsModified);
        Assert.True(snapshot.Update(new FieldIdentifier(model, nameof(Awkward.Field))) is false);

        var flaky = new Awkward();
        var second = new FormSnapshot();
        second.Take(flaky);
        flaky.Fail = true;
        Assert.True(second.Update(new FieldIdentifier(flaky, nameof(Awkward.Flaky))));
        flaky.Fail = false;
        Assert.True(second.Update(new FieldIdentifier(flaky, nameof(Awkward.Flaky))));
        Assert.False(second.IsModified);
    }

    [Fact]
    public async Task BackButton_OnALostCircuit_IsQuiet()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["historyBack"] = new JSDisconnectedException("perdu");

        await OmniBackNavigation.GoBackAsync(null!, runtime, null);

        Assert.Equal(["historyBack"], runtime.Module.Calls);
    }

    [Fact]
    public async Task Clipboard_RefusedByTheBrowser_ReportsAFailure_ThenGoesBackToIdle_AndReleasesQuietly()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        runtime.Module.CallFailures["copyText"] = new JSException("refus");
        var clock = new ManualTimeProvider();
        var redraws = 0;
        var clipboard = new OmniClipboardCopy(runtime, () => { redraws++; return Task.CompletedTask; });

        Assert.False(await clipboard.CopyAsync("x", TimeSpan.FromSeconds(2), clock));
        Assert.False(clipboard.Result);
        clock.Advance(TimeSpan.FromSeconds(3));
        await WaitUntilAsync(() => redraws == 1);
        Assert.Null(clipboard.Result);

        await clipboard.DisposeAsync();
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task Clipboard_OwnerGoneDuringTheImport_CopiesNothing()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        var clipboard = new OmniClipboardCopy(runtime, () => Task.CompletedTask);

        var copying = clipboard.CopyAsync("x", TimeSpan.FromSeconds(2), TimeProvider.System);
        await clipboard.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);

        Assert.False(await copying);
        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task Disclosure_OnALostCircuit_AndAfterItsOwnerWent_IsQuiet()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["disposeDisclosure"] = new JSDisconnectedException("perdu");
        var disclosure = new OmniDisclosureDismissal(runtime);
        await disclosure.ApplyAsync(new ElementReference("menu"), true, true);
        await disclosure.DisposeAsync();

        var held = new ManualJSRuntime { HoldImports = true, Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        var gone = new OmniDisclosureDismissal(held);
        var applying = gone.ApplyAsync(new ElementReference("menu"), true, false);
        await gone.DisposeAsync();
        held.PendingImport.SetResult(held.Module);
        await applying;

        Assert.Equal(["configureDisclosure", "disposeDisclosure"], runtime.Module.Calls);
        Assert.Empty(held.Module.Calls);
        Assert.True(held.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public void FormLabel_WithoutAControl_NamesNothing() =>
        Assert.Null(OmniFormFieldLabel.For(new OmniFormFieldLabel("nom", "nom-label"), " "));

    [Fact]
    public void TextMatch_WithoutAQuery_KeepsTheTextWhole()
    {
        Assert.True(OmniTextMatch.Contains("Liège", null));
        Assert.Equal([new OmniTextSegment("Liège", false)], OmniTextMatch.Split("Liège", null));
        Assert.Equal([new OmniTextSegment(string.Empty, false)], OmniTextMatch.Split(null!, "a"));
        Assert.Equal([new OmniTextSegment("Liège", false)], OmniTextMatch.Split("Liège", "zz"));
    }

    [Theory]
    [InlineData("https://exemple.org/\u0001")]
    [InlineData("http://[::1")]
    public void UriPolicy_RefusesControlCharactersAndAddressesThatDoNotParse(string value) =>
        Assert.Throws<InvalidOperationException>(() => OmniUriPolicy.EnsureSafe(value, "Href"));

    [Fact]
    public void NotificationStore_HoldsOnce_IgnoresUnknownOnes_AndStopsAfterItsDisposal()
    {
        var clock = new ManualTimeProvider();
        var store = new OmniNotificationStore(clock, () => { }, 5, TimeSpan.FromSeconds(5));
        var id = store.Add("Bonjour", OmniSeverity.Info, null, null);

        store.Pause(id);
        store.Pause(id);
        store.Pause(Guid.NewGuid());
        store.Resume(Guid.NewGuid());
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Single(store.Messages);
        Assert.True(store.Remove(id));

        store.Dispose();
        store.Dispose();
    }

    [Fact]
    public void ThemeColors_ReadShortLiterals_RefuseOddOnes_AndTellDarkFromLight()
    {
        Assert.Equal((255, 255, 255), ThemeColor.Parse("#fff"));
        Assert.Throws<FormatException>(() => ThemeColor.Parse("#ffff"));
        Assert.True(ThemeColor.IsDark("#101010"));
        Assert.False(ThemeColor.IsDark("#f0f0f0"));
    }

    [Fact]
    public void RandomChoice_OfASingleItem_OrOfAnUnknownCurrent_DrawsAmongAll()
    {
        IReadOnlyList<string> one = ["seul"];
        Assert.Equal("seul", AppearanceChoices.RandomOther(one, "seul"));
        Assert.Contains(AppearanceChoices.RandomOther<string>(["a", "b"], "c"), new[] { "a", "b" });
    }

    [Theory]
    [InlineData("   ", "Export")]
    [InlineData("Un titre de feuille beaucoup trop long pour Excel", "Un titre de feuille beaucoup tr")]
    public void SheetNames_FitExcel(string title, string expected) =>
        Assert.Equal(expected, XlsxWriter.SheetName(title));

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 200 && !condition(); attempt++)
        {
            await Task.Delay(10, Xunit.TestContext.Current.CancellationToken);
        }

        Assert.True(condition());
    }
}
