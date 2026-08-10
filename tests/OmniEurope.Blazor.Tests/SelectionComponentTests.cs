using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class SelectionComponentTests : BunitContext
{
    [Fact]
    public void NativeSelectors_UpdateTheirBoundValues()
    {
        var form = Render<SelectionTestHost>();

        form.Find("#drop-down").Change("1");
        form.Find("#multi-select").TriggerEvent("onchange", new Microsoft.AspNetCore.Components.ChangeEventArgs
        {
            Value = new[] { "0", "2" }
        });
        form.Find("#list-box").Change("2");
        form.Find("#checkbox-list input[type=checkbox]").Change(true);
        form.Find("#radio-list input[value=beta]").Change(true);
        form.FindAll("#select-bar button")[2].Click();

        Assert.Equal("beta", form.Instance.Model.Single);
        Assert.Equal(["alpha", "gamma"], form.Instance.Model.Multiple);
        Assert.Equal("gamma", form.Instance.Model.ListValue);
        Assert.Equal(["alpha"], form.Instance.Model.Checked);
        Assert.Equal("beta", form.Instance.Model.Radio);
        Assert.Equal("gamma", form.Instance.Model.Bar);
        Assert.DoesNotContain("style=", form.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DateSliderAndColor_UpdateTheirBoundValues()
    {
        var form = Render<SelectionTestHost>();

        form.Find("#date").Change("2026-08-10");
        form.Find("#slider").Input("7.5");
        form.Find("#color").Input("#12abef");

        Assert.Equal(new DateOnly(2026, 8, 10), form.Instance.Model.Date);
        Assert.Equal(7.5, form.Instance.Model.Amount);
        Assert.Equal("#12ABEF", form.Instance.Model.Color);
        var holder = new SliderHolder { Value = 2 };
        Assert.Equal("vertical", Render<OmniSlider>(parameters => parameters
            .Add(component => component.Value, holder.Value)
            .Add(component => component.ValueExpression, () => holder.Value)
            .Add(component => component.Vertical, true)).Find("input").GetAttribute("aria-orientation"));
    }

    [Fact]
    public async Task Autocomplete_DebouncesAnnouncesAndSelectsAResult()
    {
        var form = Render<SelectionTestHost>();

        await form.Find("#autocomplete").InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "alp" });
        form.WaitForAssertion(() => Assert.Equal(1, form.Instance.SearchCount));
        Assert.Contains("1 résultat disponible.", form.Markup, StringComparison.Ordinal);

        form.Find(".omni-autocomplete__option").Click();

        Assert.Equal("alpha", form.Instance.Model.Autocomplete);
        Assert.Contains("Alpha sélectionné.", form.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Autocomplete_IgnoresAnOlderSearchThatCompletesLast()
    {
        var value = string.Empty;
        var stale = new TaskCompletionSource<IReadOnlyList<OmniOption<string>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var latest = new TaskCompletionSource<IReadOnlyList<OmniOption<string>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.DebounceMilliseconds, 0)
            .Add(component => component.Search, (_, _) => ++callCount == 1 ? stale.Task : latest.Task));

        var input = autocomplete.Find("input");
        var staleSearch = input.InputAsync(new ChangeEventArgs { Value = "first" });
        Assert.Equal(1, callCount);
        var latestSearch = input.InputAsync(new ChangeEventArgs { Value = "second" });
        Assert.Equal(2, callCount);

        latest.SetResult([new("latest", "Récent")]);
        await latestSearch;
        stale.SetResult([new("stale", "Obsolète")]);
        await staleSearch;

        Assert.Contains("Récent", autocomplete.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Obsolète", autocomplete.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Upload_ValidatesTypeAndReportsSuccessfulProgress()
    {
        var uploadCalled = false;
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.AllowedContentTypes, ["text/plain"])
            .Add(component => component.Upload, request =>
            {
                uploadCalled = true;
                request.ReportProgress(40);
                return Task.CompletedTask;
            }));

        upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt", contentType: "text/plain"));

        Assert.True(uploadCalled);
        Assert.Contains("Téléversement terminé.", upload.Markup, StringComparison.Ordinal);
        Assert.Contains("aria-valuenow=\"100\"", upload.Markup, StringComparison.Ordinal);

        var invalid = Render<OmniUpload>(parameters => parameters
            .Add(component => component.AllowedContentTypes, ["image/png"]));
        invalid.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt", contentType: "text/plain"));

        Assert.Contains("n'est pas autorisé", invalid.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", invalid.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LargeSelector_ReloadsAndSelectsTheLastTypedValue()
    {
        var selector = Render<LargeSelectionTestHost>();
        Assert.Equal(10_000, selector.FindAll("option").Count);

        selector.Find("#large-selector").Change("9999");

        Assert.Equal(9_999, selector.Instance.Model.Value);
    }

    private sealed class SliderHolder
    {
        public double Value { get; set; }
    }
}
