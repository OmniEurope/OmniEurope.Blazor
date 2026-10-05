using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The form controls at their edges: inputs and changes without a value, a number that does not parse or
/// is blank, the text box's delay, copy and changing form, validators outside a form or changed twice,
/// a preset named without a registry, a release without disposing, and the leave guard without a host.
/// </summary>
public sealed class FormsEdgeTests : OmniBunitContext
{
    private readonly string _text = string.Empty;
    private readonly bool _flag = false;
    private readonly int? _number = null;

    [Fact]
    public void TextInputs_ReadANullInputAsEmpty()
    {
        var changes = new List<string>();
        var password = Render<OmniPassword>(parameters => parameters
            .Add(component => component.Value, "x")
            .Add(component => component.ValueChanged, next => changes.Add(next))
            .Add(component => component.ValueExpression, () => _text));
        var area = Render<OmniTextArea>(parameters => parameters
            .Add(component => component.Value, "x")
            .Add(component => component.ValueChanged, next => changes.Add(next))
            .Add(component => component.ValueExpression, () => _text));
        var box = Render<OmniTextBox>(parameters => parameters
            .Add(component => component.Value, "x")
            .Add(component => component.ValueChanged, next => changes.Add(next))
            .Add(component => component.ValueExpression, () => _text));

        password.Find("input").Input((object?)null);
        area.Find("textarea").Input((object?)null);
        box.Find("input").Input((object?)null);

        Assert.Equal([string.Empty, string.Empty, string.Empty], changes);
    }

    [Fact]
    public void CheckBox_IgnoresAChangeThatIsNotABoolean()
    {
        var changes = new List<bool>();
        var box = Render<OmniCheckBox<bool>>(parameters => parameters
            .Add(component => component.ValueChanged, next => changes.Add(next))
            .Add(component => component.ValueExpression, () => _flag));

        box.Find("input").Change("oui");

        Assert.Empty(changes);
    }

    [Fact]
    public void Numeric_RefusesText_NamesTheField_AndReadsBlankAsNoNumber()
    {
        var changes = new List<int?>();
        var form = new EditContext(new object());
        var numeric = Render<OmniNumeric<int?>>(parameters => parameters
            .AddCascadingValue(form)
            .Add(component => component.Value, 5)
            .Add(component => component.Clamp, true)
            .Add(component => component.Minimum, 1)
            .Add(component => component.DisplayName, "Quantité")
            .Add(component => component.ValueChanged, next => changes.Add(next))
            .Add(component => component.ValueExpression, () => _number));

        numeric.Find("input").Change("abc");
        Assert.Contains("Quantité", string.Join(' ', form.GetValidationMessages()), StringComparison.Ordinal);
        numeric.Find("input").Change((object?)null);
        numeric.Find("input").Change(" ");

        Assert.Equal([null], changes);
    }

    [Fact]
    public void TextArea_RefusesFewerThanOneRow_AndCountsAnEmptyValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniTextArea>(parameters => parameters
            .Add(component => component.Rows, 0)
            .Add(component => component.ValueExpression, () => _text)));

        var area = Render<OmniTextArea>(parameters => parameters
            .Add(component => component.Value, null!)
            .Add(component => component.MaxLength, 20)
            .Add(component => component.ShowCount, true)
            .Add(component => component.ValueExpression, () => _text));
        Assert.StartsWith("0", area.Find(".omni-textarea__count, .omni-text-area__count, [aria-live]").TextContent.Trim(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TextBox_CopiesTheTextStillInItsDelay_AndKeepsOneClipboard()
    {
        var module = JSInterop.SetupModule(OmniModules.Interop);
        module.Setup<bool>("copyText", _ => true).SetResult(true);
        var box = Render<OmniTextBox>(parameters => parameters
            .Add(component => component.Value, "valeur")
            .Add(component => component.Debounce, TimeSpan.FromDays(1))
            .Add(component => component.Copyable, true)
            .Add(component => component.ValueExpression, () => _text));

        _ = box.Find("input").InputAsync(new ChangeEventArgs { Value = "nouv" });
        _ = box.Find("input").InputAsync(new ChangeEventArgs { Value = null });
        await box.Find(".omni-text-box-field__copy").ClickAsync(new());
        await box.Find(".omni-text-box-field__copy").ClickAsync(new());

        Assert.Equal([string.Empty, string.Empty], module.Invocations["copyText"].Select(call => call.Arguments[0]));
    }

    [Fact]
    public void TextBox_HandsItsDelayedTextToTheFormOnValidation()
    {
        var form = new EditContext(new object());
        var changes = new List<string>();
        var box = Render<OmniTextBox>(parameters => parameters
            .AddCascadingValue(form)
            .Add(component => component.Debounce, TimeSpan.FromDays(1))
            .Add(component => component.ValueChanged, next => changes.Add(next))
            .Add(component => component.ValueExpression, () => _text));

        _ = box.Find("input").InputAsync(new ChangeEventArgs { Value = "attendu" });
        form.Validate();

        Assert.Equal(["attendu"], changes);
    }

    [Fact]
    public void InputsReleasedWithoutDisposing_KeepTheirState()
    {
        var box = Render<ReleasingTextBox>(parameters => parameters.Add(component => component.ValueExpression, () => _text));
        var check = Render<ReleasingCheckBox>(parameters => parameters.Add(component => component.ValueExpression, () => _flag));
        var toggle = Render<ReleasingSwitch>(parameters => parameters.Add(component => component.ValueExpression, () => _flag));

        box.Instance.Release();
        check.Instance.Release();
        toggle.Instance.Release();

        Assert.NotEmpty(box.FindAll("input"));
    }

    public sealed class ReleasingTextBox : OmniTextBox
    {
        public void Release() => Dispose(false);
    }

    public sealed class ReleasingCheckBox : OmniCheckBox<bool>
    {
        public void Release() => Dispose(false);
    }

    public sealed class ReleasingSwitch : OmniSwitch<bool>
    {
        public void Release() => Dispose(false);
    }

    [Fact]
    public void PresetNamedWithoutARegistry_IsRefused() =>
        Assert.ThrowsAny<InvalidOperationException>(() => Render<OmniTextBox>(parameters => parameters
            .Add(component => component.PresetName, "compact")
            .Add(component => component.ValueExpression, () => _text)));

    // ---- validators -------------------------------------------------------------------------------

    public sealed class Model
    {
        public string? Name { get; set; }
    }

    [Fact]
    public void Validator_OutsideAForm_IsRefused() =>
        Assert.Throws<InvalidOperationException>(() => Render<OmniRequiredValidator<string?>>(parameters => parameters
            .Add(component => component.For, () => _text)));

    [Fact]
    public void Validators_WithoutTheirMessage_StillMarkTheField_AndUseTheHostMessage()
    {
        var model = new Model();
        var form = new EditContext(model);
        var required = Render<OmniRequiredValidator<string?>>(parameters => parameters
            .AddCascadingValue(form)
            .Add(component => component.For, () => model.Name)
            .Add(component => component.Message, "Le nom manque.")
            .Add(component => component.ShowMessage, false));
        var length = Render<OmniLengthValidator>(parameters => parameters
            .AddCascadingValue(form)
            .Add(component => component.For, () => model.Name)
            .Add(component => component.Max, 5)
            .Add(component => component.ShowMessage, false));
        var email = Render<OmniEmailValidator>(parameters => parameters
            .AddCascadingValue(form)
            .Add(component => component.For, () => model.Name)
            .Add(component => component.ShowMessage, false));

        form.Validate();

        Assert.Contains("Le nom manque.", form.GetValidationMessages());
        Assert.Empty(required.FindAll("*"));
        Assert.Empty(length.FindAll("*"));
        Assert.Empty(email.FindAll("*"));
    }

    [Fact]
    public void ValidatorOfAWhitespaceText_AndAFieldChangedTwice_ValidatesTheLastChange()
    {
        var model = new Model { Name = "  " };
        var form = new EditContext(model);
        var validator = Render<FieldReadingValidator>(parameters => parameters
            .AddCascadingValue(form)
            .Add(component => component.For, () => model.Name));
        var field = FieldIdentifier.Create(() => model.Name);

        form.NotifyFieldChanged(field);
        form.NotifyFieldChanged(field);

        validator.WaitForAssertion(() => Assert.NotEmpty(form.GetValidationMessages(field)));
        Assert.Equal(field, validator.Instance.Read);
    }

    /// <summary>A required rule that also hands its field to the test, as a host's own validator reads it.</summary>
    public sealed class FieldReadingValidator : OmniRequiredValidator<string?>
    {
        public FieldIdentifier Read { get; private set; }

        protected override string? GetValidationError(string? value)
        {
            Read = Field;
            return base.GetValidationError(value);
        }
    }

    [Fact]
    public void FormFieldWithoutId_GivesItsHelpNoId()
    {
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "nom")
            .Add(component => component.Label, "Nom")
            .Add(component => component.Help, "Tel qu'écrit sur la pièce d'identité.")
            .AddChildContent("<input id=\"nom\" />"));

        Assert.NotEmpty(field.FindAll(".omni-form-field__help"));
    }

    [Fact]
    public async Task LeaveGuard_WithoutAHost_AsksTheBrowser_AndStaysWhenRefused()
    {
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(false);
        var navigation = Services.GetRequiredService<NavigationManager>();
        var before = navigation.Uri;
        Render<OmniUnsavedChangesGuard>(parameters => parameters.Add(component => component.HasChanges, true));

        await InvokeAsync(() => navigation.NavigateTo("/ailleurs"));

        Assert.Equal(before, navigation.Uri);
        Assert.Single(JSInterop.Invocations["confirm"]);
    }

    [Fact]
    public async Task LeaveGuard_AsksTheBrowserWithTheHostMessage()
    {
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);
        var navigation = Services.GetRequiredService<NavigationManager>();
        Render<OmniUnsavedChangesGuard>(parameters => parameters
            .Add(component => component.HasChanges, true)
            .Add(component => component.Message, "Quitter sans enregistrer ?"));

        await InvokeAsync(() => navigation.NavigateTo("/ailleurs"));

        Assert.Equal("Quitter sans enregistrer ?", Assert.Single(JSInterop.Invocations["confirm"]).Arguments[0]);
        Assert.EndsWith("/ailleurs", navigation.Uri, StringComparison.Ordinal);
    }

    private readonly int _count = 0;

    [Fact]
    public void Numeric_OfANumberThatCannotBeEmpty_RefusesAnEmptyInput()
    {
        var form = new EditContext(new object());
        var numeric = Render<OmniNumeric<int>>(parameters => parameters
            .AddCascadingValue(form)
            .Add(component => component.Value, 5)
            .Add(component => component.ValueExpression, () => _count));

        numeric.Find("input").Change((object?)null);

        Assert.NotEmpty(form.GetValidationMessages());
    }

    [Fact]
    public void RequiredValidator_OfANumber_AcceptsAnyValue()
    {
        var model = new Counted();
        var form = new EditContext(model);
        Render<OmniRequiredValidator<int>>(parameters => parameters
            .AddCascadingValue(form)
            .Add(component => component.For, () => model.Count));

        Assert.True(form.Validate());
    }

    public sealed class Counted
    {
        public int Count { get; set; }
    }

    [Fact]
    public void TextArea_AskedToCountWithoutAMaximum_ShowsNoCount()
    {
        var area = Render<OmniTextArea>(parameters => parameters
            .Add(component => component.Value, "abc")
            .Add(component => component.ShowCount, true)
            .Add(component => component.ValueExpression, () => _text));

        Assert.Empty(area.FindAll(".omni-text-area__count"));
    }

    private Task InvokeAsync(Action action) => Renderer.Dispatcher.InvokeAsync(action);
}
