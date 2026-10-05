using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The components that splat the host's attributes and then name the element themselves: without a
/// <c>Label</c>, the <c>aria-label</c> the host passed is kept (the component's null value used to
/// erase it); with one, the <c>Label</c> wins.
/// </summary>
public sealed class HostAriaLabelTests : OmniBunitContext
{
    private const string Host = "Nom de l'hôte";
    private const string Own = "Nom du composant";

    private readonly string _text = string.Empty;

    private static readonly IReadOnlyList<OmniOption<string>> Options = [new("a", "Alpha"), new("b", "Beta")];

    public HostAriaLabelTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private void Check<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>> parameters, Func<string?, Action<ComponentParameterCollectionBuilder<TComponent>>> label)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        var hostOnly = Render<TComponent>(builder =>
        {
            parameters(builder);
            builder.AddUnmatched("aria-label", Host);
        });
        Assert.NotEmpty(hostOnly.FindAll($"[aria-label=\"{Host}\"]"));

        var both = Render<TComponent>(builder =>
        {
            parameters(builder);
            label(Own)(builder);
            builder.AddUnmatched("aria-label", Host);
        });
        Assert.Empty(both.FindAll($"[aria-label=\"{Host}\"]"));
        Assert.NotEmpty(both.FindAll($"[aria-label=\"{Own}\"]"));
    }

    [Fact]
    public void Button() => Check<OmniButton>(p => p.AddChildContent("x"), l => p => p.Add(c => c.Label, l));

    [Fact]
    public void ToggleButton() => Check<OmniToggleButton>(p => p.AddChildContent("x"), l => p => p.Add(c => c.Label, l));

    [Fact]
    public void Icon() => Check<OmniIcon>(p => p.Add(c => c.Name, OmniIconName.Info), l => p => p.Add(c => c.Label, l));

    [Fact]
    public void Link() => Check<OmniLink>(p => p.Add(c => c.Href, "/aide").AddChildContent("Aide"), l => p => p.Add(c => c.Label, l));

    [Fact]
    public void CodeEditor() => Check<OmniCodeEditor>(
        p => p.Add(c => c.Value, "x").Add(c => c.ValueExpression, () => _text),
        l => p => p.Add(c => c.Label, l));

    [Fact]
    public void HtmlEditor() => Check<OmniHtmlEditor>(
        p => p.Add(c => c.Value, "<p>x</p>").Add(c => c.ValueExpression, () => _text),
        l => p => p.Add(c => c.Label, l));

    [Fact]
    public void DropDown() => Check<OmniDropDown<string>>(
        p => p.Add(c => c.Options, Options).Add(c => c.ValueExpression, () => _text),
        l => p => p.Add(c => c.Label, l));

    [Fact]
    public void SelectBar() => Check<OmniSelectBar<string>>(
        p => p.Add(c => c.Options, Options).Add(c => c.ValueExpression, () => _text),
        l => p => p.Add(c => c.Label, l));

    [Fact]
    public void Autocomplete() => Check<OmniAutocomplete<string>>(
        p => p.Add(c => c.ValueExpression, () => _text),
        l => p => p.Add(c => c.Label, l));

    [Fact]
    public void SelectableCardGroup() => Check<OmniSelectableCardGroup<string, string>>(
        p => p.Add(c => c.Options, Options).Add(c => c.ValueExpression, () => _text),
        l => p => p.Add(c => c.Label, l));
}
