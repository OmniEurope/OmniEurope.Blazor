using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniAutocomplete{TValue}.AllowFreeText"/>: the text typed becomes the value when the field
/// is left or Enter is pressed without a highlighted suggestion, a suggestion whose text is the one typed
/// gives its own value, and without the parameter nothing typed is ever taken as the value.
/// </summary>
public sealed class AutocompleteFreeTextTests : OmniBunitContext
{
    private readonly CountModel _count = new();

    private static readonly IReadOnlyList<OmniOption<string>> Models =
    [
        new("model-small", "Petit modèle"),
        new("model-large", "Grand modèle")
    ];

    private IRenderedComponent<OmniAutocomplete<string>> RenderModels(List<string?> changes, bool allowFreeText = true, string value = "")
    {
        return Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Id, "model")
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, (string next) => changes.Add(next))
            .Add(component => component.AllowFreeText, allowFreeText)
            .Add(component => component.Debounce, TimeSpan.Zero)
            .Add(component => component.Search, (text, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(
                [.. Models.Where(model => model.Text.Contains(text, StringComparison.OrdinalIgnoreCase))])));
    }

    [Fact]
    public async Task LeavingTheField_TakesTheTypedText_AsTheValue()
    {
        var changes = new List<string?>();
        var autocomplete = RenderModels(changes);

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "modèle-maison-7b" });
        Assert.Empty(changes);

        await autocomplete.Find("input").BlurAsync(new FocusEventArgs());

        Assert.Equal(["modèle-maison-7b"], changes);
        Assert.Equal("modèle-maison-7b", autocomplete.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task Enter_WithoutAHighlightedSuggestion_TakesTheTypedText_AndClosesTheList()
    {
        var changes = new List<string?>();
        var autocomplete = RenderModels(changes);

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "modèle" });
        Assert.Equal("true", autocomplete.Find("input").GetAttribute("aria-expanded"));

        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(["modèle"], changes);
        Assert.Equal("false", autocomplete.Find("input").GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task Enter_OnAHighlightedSuggestion_StillPicksTheSuggestion()
    {
        var changes = new List<string?>();
        var autocomplete = RenderModels(changes);

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "grand" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
        await autocomplete.Find("input").BlurAsync(new FocusEventArgs());

        Assert.Equal(["model-large"], changes);
        Assert.Equal("Grand modèle", autocomplete.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task TypedTextEqualToASuggestion_GivesTheSuggestionValue_IgnoringCase()
    {
        var changes = new List<string?>();
        var autocomplete = RenderModels(changes);

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "petit MODÈLE" });
        await autocomplete.Find("input").BlurAsync(new FocusEventArgs());

        Assert.Equal(["model-small"], changes);
        Assert.Equal("Petit modèle", autocomplete.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task EmptiedField_ClearsTheValue()
    {
        var changes = new List<string?>();
        var autocomplete = RenderModels(changes, value: "ancien");

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "   " });
        await autocomplete.Find("input").BlurAsync(new FocusEventArgs());

        Assert.Equal([null], changes);
    }

    [Fact]
    public async Task UnchangedText_RaisesNothing()
    {
        var changes = new List<string?>();
        var autocomplete = RenderModels(changes, value: "modèle-maison");

        await autocomplete.Find("input").BlurAsync(new FocusEventArgs());

        Assert.Empty(changes);
    }

    [Fact]
    public async Task WithoutAllowFreeText_TheTypedTextIsNeverTheValue()
    {
        var changes = new List<string?>();
        var autocomplete = RenderModels(changes, allowFreeText: false);

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "modèle-maison" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
        await autocomplete.Find("input").BlurAsync(new FocusEventArgs());

        Assert.Empty(changes);
    }

    [Fact]
    public async Task ADisabledOrReadOnlyField_TakesNothing()
    {
        var changes = new List<string?>();
        var autocomplete = RenderModels(changes);
        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "modèle-maison" });

        autocomplete.Render(parameters => parameters.Add(component => component.Disabled, true));
        await autocomplete.Find("input").BlurAsync(new FocusEventArgs());
        autocomplete.Render(parameters => parameters
            .Add(component => component.Disabled, false)
            .AddUnmatched("readonly", true));
        await autocomplete.Find("input").BlurAsync(new FocusEventArgs());

        Assert.Empty(changes);
    }

    [Fact]
    public async Task ParseFreeText_ConvertsTheText_ForANonStringValue()
    {
        var changes = new List<int>();
        var autocomplete = Render<OmniAutocomplete<int>>(parameters => parameters
            .Add(component => component.Value, 0)
            .Add(component => component.ValueExpression, () => _count.Count)
            .Add(component => component.ValueChanged, (int next) => changes.Add(next))
            .Add(component => component.AllowFreeText, true)
            .Add(component => component.ParseFreeText, text => int.Parse(text, System.Globalization.CultureInfo.InvariantCulture))
            .Add(component => component.Debounce, TimeSpan.Zero)
            .Add(component => component.Search, (_, _) => Task.FromResult<IReadOnlyList<OmniOption<int>>>([new(1, "Un")])));

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "42" });
        await autocomplete.Find("input").BlurAsync(new FocusEventArgs());

        Assert.Equal([42], changes);
    }

    [Fact]
    public void AllowFreeText_WithoutParseFreeText_ForANonStringValue_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniAutocomplete<int>>(parameters => parameters
            .Add(component => component.ValueExpression, () => _count.Count)
            .Add(component => component.AllowFreeText, true)
            .Add(component => component.Search, (_, _) => Task.FromResult<IReadOnlyList<OmniOption<int>>>([]))));

        Assert.Contains(nameof(OmniAutocomplete<int>.ParseFreeText), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseFreeText_Failure_KeepsTheValue_AndShowsTheInvalidState()
    {
        var changes = new List<int>();
        var model = new CountModel { Count = 7 };
        var autocomplete = Render<OmniAutocomplete<int>>(parameters => parameters
            .AddCascadingValue(new Microsoft.AspNetCore.Components.Forms.EditContext(model))
            .Add(component => component.Value, 7)
            .Add(component => component.ValueExpression, () => model.Count)
            .Add(component => component.ValueChanged, (int next) => changes.Add(next))
            .Add(component => component.AllowFreeText, true)
            .Add(component => component.ParseFreeText, text => int.Parse(text, System.Globalization.CultureInfo.InvariantCulture))
            .Add(component => component.Debounce, TimeSpan.Zero)
            .Add(component => component.Search, (_, _) => Task.FromResult<IReadOnlyList<OmniOption<int>>>([])));

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "pas un nombre" });
        await autocomplete.Find("input").BlurAsync(new FocusEventArgs());

        Assert.Empty(changes);
        Assert.Equal("true", autocomplete.Find("input").GetAttribute("aria-invalid"));
    }

    private sealed class CountModel
    {
        public int Count { get; set; }
    }
}
