using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The picker internals read on their own: a panel closed twice, one rendered after its picker left,
/// its script released on a lost circuit, a disposed runtime or an unloading page, and time columns
/// whose every value is out of range or that have no value chosen yet.
/// </summary>
public sealed class PickerInternalsTests : OmniBunitContext
{
    [Fact]
    public async Task Popup_ClosedTwice_AndRenderedAfterItsPickerLeft_DoesNothingMore()
    {
        var runtime = new ManualJSRuntime();
        var popup = new PickerPopup(runtime, _ => Task.CompletedTask);

        popup.Close(restoreFocus: true);
        Assert.False(popup.IsOpen);

        popup.Release();
        popup.Open(".omni-calendar__day");
        await popup.AfterRenderAsync(default, default, default);

        Assert.Empty(runtime.Module.Calls);
    }

    public static TheoryData<Exception> ReleaseFailures => new()
    {
        new JSDisconnectedException("perdu"),
        new ObjectDisposedException("runtime"),
        new TaskCanceledException("déchargée")
    };

    [Theory]
    [MemberData(nameof(ReleaseFailures))]
    public async Task Popup_ReleasedWhenItsScriptCannotBeReached_IsQuiet(Exception failure)
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = failure } };
        var popup = new PickerPopup(runtime, _ => Task.CompletedTask);
        popup.Open(".omni-calendar__day");
        await popup.AfterRenderAsync(default, default, default);

        popup.Release();

        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        Assert.Contains("detachPicker", runtime.Module.Calls);
    }

    [Fact]
    public void TimeColumns_WithEveryValueOutOfRange_OfferNoStopAndIgnoreTheKeys()
    {
        var changes = new List<PickerTimeChange>();
        var columns = Render<PickerTimeColumns>(parameters => parameters
            .Add(component => component.IdPrefix, "heure")
            .Add(component => component.IsRangeAllowed, (_, _) => false)
            .Add(component => component.OnChange, change => changes.Add(change)));

        Assert.DoesNotContain(columns.FindAll(".omni-time__item"), item => item.GetAttribute("tabindex") == "0");
        columns.Find("#heure-hour").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.Empty(changes);
    }

    [Fact]
    public void TimeColumns_WhoseValueIsOutOfRange_PutTheTabStopOnTheFirstAllowedValue()
    {
        var columns = Render<PickerTimeColumns>(parameters => parameters
            .Add(component => component.IdPrefix, "heure")
            .Add(component => component.Value, new TimeOnly(23, 0))
            .Add(component => component.IsRangeAllowed, (from, _) => from.Hour < 12));

        var stop = columns.Find("#heure-hour [tabindex='0']");
        Assert.Equal("00", stop.TextContent);
        Assert.NotNull(columns.Find("#heure-hour [aria-selected='true']").GetAttribute("disabled"));
    }

    [Fact]
    public void TimeColumns_WithoutAValue_StartFromTheFirstHourOnArrowUp()
    {
        var changes = new List<PickerTimeChange>();
        var columns = Render<PickerTimeColumns>(parameters => parameters
            .Add(component => component.IdPrefix, "heure")
            .Add(component => component.OnChange, change => changes.Add(change)));

        columns.Find("#heure-hour").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        Assert.Equal(0, Assert.Single(changes).Time.Hour);
    }
}
