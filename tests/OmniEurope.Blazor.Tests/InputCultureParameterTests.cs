using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The <c>Culture</c> parameter of <see cref="OmniDatePicker"/> and <see cref="OmniNumeric{TValue}"/>: set, it replaces the
/// culture of the page for the text the field writes and reads and for the calendar; null keeps the culture of the page. The
/// suite pins the page to fr-FR, so a culture of another family shows whether the parameter or the page decided.
/// </summary>
public sealed class InputCultureParameterTests : OmniBunitContext
{
    private static readonly CultureInfo American = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public InputCultureParameterTests()
    {
        JSInterop.SetupModule(OmniModules.Focus).Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<OmniDatePicker> RenderPicker(CultureInfo? culture, DateOnly? value, List<DateOnly?>? changes = null)
        => Render<OmniDatePicker>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.Culture, culture)
            .Add(component => component.ValueChanged, next => changes?.Add(next))
            .Add(component => component.ValueExpression, () => _date));

    private IRenderedComponent<OmniNumeric<decimal?>> RenderNumeric(CultureInfo? culture, decimal? value, List<decimal?>? changes = null)
        => Render<OmniNumeric<decimal?>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.Culture, culture)
            .Add(component => component.ValueChanged, next => changes?.Add(next))
            .Add(component => component.ValueExpression, () => _number));

    private readonly DateOnly? _date = null;
    private readonly decimal? _number = null;

    [Fact]
    public void DatePicker_WithACulture_WritesAndReadsTheDateInIt_WhateverThePageCulture()
    {
        var changes = new List<DateOnly?>();
        var american = RenderPicker(American, new DateOnly(2026, 9, 21), changes);
        var german = RenderPicker(German, new DateOnly(2026, 9, 21), changes);

        Assert.Equal("09/21/2026", american.Find("input").GetAttribute("value"));
        Assert.Equal("21.09.2026", german.Find("input").GetAttribute("value"));

        american.Find("input").Change("10/02/2026");
        Assert.Equal([new DateOnly(2026, 10, 2)], changes);

        german.Find("input").Change("02.10.2026");
        Assert.Equal([new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 2)], changes);
        Assert.Equal("02.10.2026", german.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void DatePicker_WithACulture_ReadsTheSameTextAsAnotherDateThanThePage()
    {
        var changes = new List<DateOnly?>();
        var american = RenderPicker(American, null, changes);
        var page = RenderPicker(null, null, changes);

        american.Find("input").Change("10/02/2026");
        page.Find("input").Change("10/02/2026");

        Assert.Equal([new DateOnly(2026, 10, 2), new DateOnly(2026, 2, 10)], changes);
    }

    [Fact]
    public void DatePicker_WithACulture_DrawsTheCalendarWithItsMonthNamesAndItsFirstDay()
    {
        var american = RenderPicker(American, new DateOnly(2026, 12, 21));
        var german = RenderPicker(German, new DateOnly(2026, 12, 21));
        var page = RenderPicker(null, new DateOnly(2026, 12, 21));

        american.Find("button.omni-date__toggle").Click();
        german.Find("button.omni-date__toggle").Click();
        page.Find("button.omni-date__toggle").Click();

        Assert.Equal("December 2026", american.Find(".omni-calendar__title").TextContent);
        Assert.Equal("Dezember 2026", german.Find(".omni-calendar__title").TextContent);
        Assert.Equal("décembre 2026", page.Find(".omni-calendar__title").TextContent);
        Assert.Equal("Sun", american.FindAll("[role='columnheader']").First().TextContent);
        Assert.Equal("Mo", german.FindAll("[role='columnheader']").First().TextContent);
        Assert.Equal("lun", page.FindAll("[role='columnheader']").First().TextContent);
    }

    [Fact]
    public void DatePicker_WithoutACulture_KeepsTheCultureOfThePage()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = American;
            var picker = RenderPicker(null, new DateOnly(2026, 9, 21));

            Assert.Equal("09/21/2026", picker.Find("input").GetAttribute("value"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Numeric_WithACulture_ReadsAComma_AsThatCultureDoes_WhateverThePageCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = American;
            var changes = new List<decimal?>();
            var german = RenderNumeric(German, null, changes);
            var page = RenderNumeric(null, null, changes);

            german.Find("input").Change("1,5");
            page.Find("input").Change("1,5");

            // de-DE: the comma is the decimal separator; en-US (the page): it groups thousands.
            Assert.Equal([1.5m, 15m], changes);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Numeric_WithACulture_StillReadsAPeriodInInvariant_AndWritesItsValueInInvariant()
    {
        var changes = new List<decimal?>();
        var german = RenderNumeric(German, 1.5m, changes);

        Assert.Equal("1.5", german.Find("input").GetAttribute("value"));

        german.Find("input").Change("2.25");
        Assert.Equal([2.25m], changes);
    }

    [Fact]
    public void Numeric_WithoutACulture_KeepsTheCultureOfThePage()
    {
        var changes = new List<decimal?>();
        var page = RenderNumeric(null, null, changes);

        page.Find("input").Change("1,5");

        Assert.Equal([1.5m], changes);
    }
}
