using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The selection inputs through their life in a form: released from the edit context when they leave
/// it, following a value their parent replaces, and holding still while disabled.
/// </summary>
public sealed class SelectionLifecycleTests : OmniBunitContext
{
    private static readonly OmniOption<string>[] Options = [new("a", "Alpha"), new("b", "Beta")];

    [Fact]
    public async Task SelectBar_LeavingThePage_UnsubscribesFromTheEditContext()
    {
        JSInterop.SetupModule("./_content/OmniEurope.Blazor/omni-focus.js").Mode = JSRuntimeMode.Loose;
        var model = new Model();
        var context = new EditContext(model);
        Render<OmniSelectBar<string>>(parameters => parameters
            .AddCascadingValue(context)
            .Add(component => component.Options, Options)
            .Add(component => component.Value, model.Text)
            .Add(component => component.ValueExpression, () => model.Text));
        Assert.Equal(1, ValidationStateSubscribers(context));

        await DisposeComponentsAsync();

        Assert.Equal(0, ValidationStateSubscribers(context));
    }

    [Fact]
    public async Task Autocomplete_LeavingThePage_UnsubscribesFromTheEditContext()
    {
        var model = new Model();
        var context = new EditContext(model);
        Render<OmniAutocomplete<string>>(parameters => parameters
            .AddCascadingValue(context)
            .Add(component => component.Search, (_, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(Options))
            .Add(component => component.Value, model.Text)
            .Add(component => component.ValueExpression, () => model.Text));
        Assert.Equal(1, ValidationStateSubscribers(context));

        await DisposeComponentsAsync();

        Assert.Equal(0, ValidationStateSubscribers(context));
    }

    [Fact]
    public async Task MultiSelect_LeavingThePage_UnsubscribesFromTheEditContext()
    {
        var model = new Model();
        var context = new EditContext(model);
        Render<OmniMultiSelect<string>>(parameters => parameters
            .AddCascadingValue(context)
            .Add(component => component.Options, Options)
            .Add(component => component.Value, model.Many)
            .Add(component => component.ValueExpression, () => model.Many));
        Assert.Equal(1, ValidationStateSubscribers(context));

        await DisposeComponentsAsync();

        Assert.Equal(0, ValidationStateSubscribers(context));
    }

    [Fact]
    public void Autocomplete_ValueReplacedByTheParent_ShowsTheNewValueOrNothing()
    {
        var value = "a";
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Search, (_, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(Options))
            .Add(component => component.FormatValue, key => key == "a" ? "Alpha" : "Beta")
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
        Assert.Equal("Alpha", autocomplete.Find("input").GetAttribute("value"));

        autocomplete.Render(parameters => parameters.Add(component => component.Value, "b"));
        Assert.Equal("Beta", autocomplete.Find("input").GetAttribute("value"));

        autocomplete.Render(parameters => parameters.Add(component => component.Value, (string?)null));
        Assert.True(string.IsNullOrEmpty(autocomplete.Find("input").GetAttribute("value")));
    }

    [Fact]
    public async Task Autocomplete_ChoiceEchoedBackByTheParent_KeepsTheChosenText()
    {
        string? value = null;
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.DebounceMilliseconds, 0)
            .Add(component => component.Search, (_, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(Options))
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value!)
            .Add(component => component.ValueChanged, next => value = next));

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "be" });
        autocomplete.FindAll(".omni-autocomplete__option")[1].Click();
        autocomplete.Render(parameters => parameters.Add(component => component.Value, value));

        Assert.Equal("b", value);
        Assert.Equal("Beta", autocomplete.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task Autocomplete_Disabled_RefusesTheSuggestionsStillShown()
    {
        var value = "a";
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.DebounceMilliseconds, 0)
            .Add(component => component.Search, (_, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(Options))
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, next => value = next));
        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "be" });

        autocomplete.Render(parameters => parameters.Add(component => component.Disabled, true));
        var option = autocomplete.FindAll(".omni-autocomplete__option")[1];
        Assert.True(option.HasAttribute("disabled"));
        await autocomplete.InvokeAsync(() => option.Click());

        Assert.Equal("a", value);
    }

    /// <summary>The handlers an input base registers on its edit context to follow validation.</summary>
    private static int ValidationStateSubscribers(EditContext context) =>
        (typeof(EditContext).GetField(nameof(EditContext.OnValidationStateChanged), BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(context) as Delegate)?.GetInvocationList().Length ?? 0;

    private sealed class Model
    {
        public string Text { get; set; } = string.Empty;

        public IReadOnlyList<string> Many { get; set; } = [];
    }
}
