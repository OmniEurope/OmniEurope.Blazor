using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniAutocomplete{TValue}.ReplaceTextOnType"/> and the Escape guard reach the field through the
/// focus module: the parameter is handed to <c>attachComboInput</c> on the first render and again when it
/// changes, and the field is released on disposal. What the script does to real keys and the mouse is proved
/// in a browser, by the <c>combo</c> part of the scripts probe (<c>eng/scripts-probe/combo.mjs</c>); these tests
/// cover the wiring only, with a module double that records the calls.
/// </summary>
public sealed class AutocompleteReplaceTextTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<string>> Models =
    [
        new("small", "model-small"),
        new("large", "model-large")
    ];

    private BunitJSModuleInterop FocusModule()
    {
        var module = JSInterop.SetupModule(OmniModules.Focus);
        module.SetupVoid("attachComboInput", _ => true);
        module.SetupVoid("detachComboInput", _ => true);
        return module;
    }

    private IRenderedComponent<OmniAutocomplete<string>> RenderModels(bool? replace = null, string value = "")
    {
        return Render<OmniAutocomplete<string>>(parameters =>
        {
            parameters
                .Add(component => component.Id, "model")
                .Add(component => component.Value, value)
                .Add(component => component.ValueExpression, () => value)
                .Add(component => component.AllowFreeText, true)
                .Add(component => component.Debounce, TimeSpan.Zero)
                .Add(component => component.Search, (text, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(
                    [.. Models.Where(model => model.Text.Contains(text, StringComparison.OrdinalIgnoreCase))]));
            if (replace is not null)
            {
                parameters.Add(component => component.ReplaceTextOnType, replace.Value);
            }
        });
    }

    [Fact]
    public void ReplaceTextOnType_IsOffByDefault_SoOtherAppsKeepTheCaretWhereTheUserPutsIt()
    {
        var module = FocusModule();

        var autocomplete = RenderModels();

        Assert.False(autocomplete.Instance.ReplaceTextOnType);
        var attach = Assert.Single(module.Invocations["attachComboInput"]);
        Assert.Equal(3, attach.Arguments.Count);
        Assert.Equal(false, attach.Arguments[2]);
    }

    [Fact]
    public void ReplaceTextOnType_IsHandedToTheScript_WithTheRootAndTheInput()
    {
        var module = FocusModule();

        RenderModels(replace: true);

        var attach = Assert.Single(module.Invocations["attachComboInput"]);
        Assert.Equal(true, attach.Arguments[2]);
        Assert.IsType<ElementReference>(attach.Arguments[0]);
        Assert.IsType<ElementReference>(attach.Arguments[1]);
    }

    [Fact]
    public void AChangeOfReplaceTextOnType_AttachesTheFieldAgainWithTheNewValue_AndOnlyThen()
    {
        var module = FocusModule();
        var autocomplete = RenderModels(replace: false);
        Assert.Single(module.Invocations["attachComboInput"]);

        autocomplete.Render(parameters => parameters.Add(component => component.ReplaceTextOnType, true));
        Assert.Equal(2, module.Invocations["attachComboInput"].Count);
        Assert.Equal(true, module.Invocations["attachComboInput"][1].Arguments[2]);

        // A render that changes nothing about it does not call the script again.
        autocomplete.Render(parameters => parameters.Add(component => component.ReplaceTextOnType, true));
        Assert.Equal(2, module.Invocations["attachComboInput"].Count);
    }

    [Fact]
    public async Task TheField_IsReleasedOnDisposal()
    {
        var module = FocusModule();
        RenderModels(replace: true);

        await DisposeComponentsAsync();

        Assert.Single(module.Invocations["detachComboInput"]);
    }

    [Fact]
    public async Task ALostCircuit_FailsNeitherTheRenderNorTheDisposal()
    {
        var module = JSInterop.SetupModule(OmniModules.Focus);
        var lost = new JSDisconnectedException("circuit perdu");
        module.SetupVoid("attachComboInput", _ => true).SetException(lost);
        module.SetupVoid("detachComboInput", _ => true).SetException(lost);

        var autocomplete = RenderModels(replace: true);

        Assert.NotNull(autocomplete.Find("input"));
        await DisposeComponentsAsync();
    }

    [Fact]
    public async Task Escape_LeavesTheTextAndTheValueAlone_WhetherTheListIsOpenOrNot()
    {
        FocusModule();
        var current = "model-small";
        string? changed = null;
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Id, "model")
            .Add(component => component.Value, current)
            .Add(component => component.ValueExpression, () => current)
            .Add(component => component.ValueChanged, (string next) => changed = next)
            .Add(component => component.AllowFreeText, true)
            .Add(component => component.Debounce, TimeSpan.Zero)
            .Add(component => component.Search, (text, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(
                [.. Models.Where(model => model.Text.Contains(text, StringComparison.OrdinalIgnoreCase))])));

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "model-l" });
        Assert.Equal("true", autocomplete.Find("input").GetAttribute("aria-expanded"));
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal("false", autocomplete.Find("input").GetAttribute("aria-expanded"));
        Assert.Equal("model-l", autocomplete.Find("input").GetAttribute("value"));

        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal("model-l", autocomplete.Find("input").GetAttribute("value"));
        Assert.Null(changed);
    }
}
