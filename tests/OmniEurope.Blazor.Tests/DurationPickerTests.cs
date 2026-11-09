using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniDurationPicker"/> binds hours and minutes to a nullable <see cref="TimeSpan"/>, carries
/// minutes into hours, is null when both fields are empty, and takes part in the edit context like the
/// other fields.
/// </summary>
public sealed class DurationPickerTests : OmniBunitContext
{
    private TimeSpan? _value;

    private IRenderedComponent<OmniDurationPicker> RenderPicker(TimeSpan? initial = null, Action<ComponentParameterCollectionBuilder<OmniDurationPicker>>? more = null)
    {
        _value = initial;
        return Render<OmniDurationPicker>(parameters =>
        {
            parameters
                .Add(component => component.Value, _value)
                .Add(component => component.ValueExpression, () => _value)
                .Add(component => component.ValueChanged, EventCallback.Factory.Create<TimeSpan?>(this, changed => _value = changed));
            more?.Invoke(parameters);
        });
    }

    [Fact]
    public void Hours_and_minutes_bind_to_the_time_span()
    {
        var picker = RenderPicker();

        picker.FindAll("input")[0].Change("2");
        picker.FindAll("input")[1].Change("30");

        Assert.Equal(new TimeSpan(2, 30, 0), _value);
    }

    [Fact]
    public void A_value_from_outside_fills_both_fields()
    {
        var picker = RenderPicker(new TimeSpan(1, 5, 0));

        var inputs = picker.FindAll("input");
        Assert.Equal("1", inputs[0].GetAttribute("value"));
        Assert.Equal("5", inputs[1].GetAttribute("value"));
    }

    [Fact]
    public void Minutes_of_an_hour_or_more_carry_into_the_hours()
    {
        var picker = RenderPicker(new TimeSpan(1, 0, 0));

        picker.FindAll("input")[1].Change("75");

        Assert.Equal(new TimeSpan(2, 15, 0), _value);
        var inputs = picker.FindAll("input");
        Assert.Equal("2", inputs[0].GetAttribute("value"));
        Assert.Equal("15", inputs[1].GetAttribute("value"));
    }

    [Fact]
    public void Emptying_both_fields_gives_null()
    {
        var picker = RenderPicker(new TimeSpan(0, 45, 0));

        picker.FindAll("input")[1].Change(string.Empty);
        // The hours field holds 0 and the group is not empty until both fields are.
        Assert.Equal(TimeSpan.Zero, _value);
        picker.FindAll("input")[0].Change(string.Empty);
        picker.FindAll("input")[1].Change(string.Empty);

        Assert.Null(_value);
        Assert.All(picker.FindAll("input"), input => Assert.True(string.IsNullOrEmpty(input.GetAttribute("value"))));
    }

    [Fact]
    public void Max_and_min_bring_the_entry_back()
    {
        var picker = RenderPicker(null, parameters => parameters
            .Add(component => component.Max, TimeSpan.FromHours(8))
            .Add(component => component.Min, TimeSpan.FromMinutes(15)));

        picker.FindAll("input")[0].Change("20");
        Assert.Equal(TimeSpan.FromHours(8), _value);

        picker.FindAll("input")[0].Change("0");
        Assert.Equal(TimeSpan.FromMinutes(15), _value);
    }

    [Fact]
    public void Group_is_named_and_each_field_has_a_localized_name()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            var picker = RenderPicker();

            Assert.Equal("Durée", picker.Find("[role=group]").GetAttribute("aria-label"));
            var inputs = picker.FindAll("input");
            Assert.Equal("Heures", inputs[0].GetAttribute("aria-label"));
            Assert.Equal("Minutes", inputs[1].GetAttribute("aria-label"));
            Assert.Equal(["h", "min"], picker.FindAll(".omni-duration__unit").Select(unit => unit.TextContent));

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var english = RenderPicker();
            Assert.Equal("Duration", english.Find("[role=group]").GetAttribute("aria-label"));
            Assert.Equal("Hours", english.FindAll("input")[0].GetAttribute("aria-label"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Label_names_the_group_and_minute_step_reaches_the_field()
    {
        var picker = RenderPicker(null, parameters => parameters
            .Add(component => component.Label, "Temps passé")
            .Add(component => component.MinuteStep, 5));

        Assert.Equal("Temps passé", picker.Find("[role=group]").GetAttribute("aria-label"));
        Assert.Equal("5", picker.FindAll("input")[1].GetAttribute("step"));
    }

    [Fact]
    public void Disabled_and_read_only_reach_both_fields_and_a_read_only_entry_changes_nothing()
    {
        var disabled = RenderPicker(null, parameters => parameters.Add(component => component.Disabled, true));
        Assert.All(disabled.FindAll("input"), input => Assert.True(input.HasAttribute("disabled")));

        var readOnly = RenderPicker(TimeSpan.FromHours(1), parameters => parameters.Add(component => component.ReadOnly, true));
        Assert.All(readOnly.FindAll("input"), input => Assert.True(input.HasAttribute("readonly")));
        readOnly.FindAll("input")[0].Change("5");
        Assert.Equal(TimeSpan.FromHours(1), _value);
    }

    [Fact]
    public async Task Edit_context_validation_state_is_applied_to_the_group()
    {
        var model = new Model();
        var context = new EditContext(model);
        var messages = new ValidationMessageStore(context);
        var form = Render<EditForm>(parameters => parameters
            .Add(component => component.EditContext, context)
            .Add(component => component.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenComponent<OmniDurationPicker>(0);
                builder.AddComponentParameter(1, nameof(OmniDurationPicker.Value), model.Spent);
                builder.AddComponentParameter(2, nameof(OmniDurationPicker.ValueChanged), EventCallback.Factory.Create<TimeSpan?>(this, changed => model.Spent = changed));
                builder.AddComponentParameter(3, nameof(OmniDurationPicker.ValueExpression), (System.Linq.Expressions.Expression<Func<TimeSpan?>>)(() => model.Spent));
                builder.CloseComponent();
            })));

        Assert.DoesNotContain("modified", form.Find("[role=group]").ClassName);

        form.FindAll("input")[0].Change("3");
        Assert.Equal(TimeSpan.FromHours(3), model.Spent);
        Assert.Contains("modified", form.Find("[role=group]").ClassName);

        messages.Add(context.Field(nameof(Model.Spent)), "Trop long.");
        await form.InvokeAsync(context.NotifyValidationStateChanged);
        var group = form.Find("[role=group]");
        Assert.Contains("invalid", group.ClassName);
        Assert.Equal("true", group.GetAttribute("aria-invalid"));
    }

    private sealed class Model
    {
        public TimeSpan? Spent { get; set; }
    }
}
