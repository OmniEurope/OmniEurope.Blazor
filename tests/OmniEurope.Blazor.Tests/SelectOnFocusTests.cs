using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <c>SelectOnFocus</c> of the four text-entry fields (<see cref="OmniTextBox"/>, <see cref="OmniNumeric{TValue}"/>,
/// <see cref="OmniTextArea"/>, <see cref="OmniPassword"/>): off, the markup is what it always was and the script is never
/// imported; on, the script is imported once and attached to the field element, follows it when it is drawn anew, is
/// detached when the parameter goes off and when the field is disposed. What the script then does in a browser (the
/// selection, a real press, Tab) is proved by the unit tests of the script and by the showcase probe.
/// </summary>
public sealed class SelectOnFocusTests : OmniBunitContext
{
    private readonly BunitJSModuleInterop _module;
    private string _text = "Camille";
    private int _number = 12;

    public SelectOnFocusTests()
    {
        _module = JSInterop.SetupModule(OmniModules.SelectOnFocus);
    }

    public static TheoryData<string> Fields => ["text box", "numeric", "text area", "password"];

    private static string Normalised(string markup) =>
        Regex.Replace(Regex.Replace(markup, " blazor:(elementReference|on[a-z]+)=\"[^\"]*\"", string.Empty), @">\s+<", "><");

    private sealed record Field(IRenderedComponent<IComponent> Rendered, Action<bool> SetSelectOnFocus, Func<ValueTask> Dispose)
    {
        public string ElementReference => Rendered.Find("input,textarea").GetAttribute("blazor:elementReference")!;
    }

    private Field RenderField(string kind, bool? initial) => kind switch
    {
        "text box" => RenderField<OmniTextBox, string>(_text, () => _text, component => component.SelectOnFocus, initial),
        "numeric" => RenderField<OmniNumeric<int>, int>(_number, () => _number, component => component.SelectOnFocus, initial),
        "text area" => RenderField<OmniTextArea, string>(_text, () => _text, component => component.SelectOnFocus, initial),
        _ => RenderField<OmniPassword, string>(_text, () => _text, component => component.SelectOnFocus, initial),
    };

    /// <summary>One of the four fields bound to the test's value, <c>SelectOnFocus</c> given only when <paramref name="initial"/> is set.</summary>
    private Field RenderField<TComponent, TValue>(
        TValue value, Expression<Func<TValue>> valueExpression, Expression<Func<TComponent, bool>> selectOnFocus, bool? initial)
        where TComponent : InputBase<TValue>, IAsyncDisposable
    {
        var field = this.RenderBound<TComponent, TValue>(value, valueExpression, parameters =>
        {
            if (initial is { } on)
            {
                parameters.Add(selectOnFocus, on);
            }
        });
        return new(field, on => field.Render(parameters => parameters.Add(selectOnFocus, on)), field.Instance.DisposeAsync);
    }

    private bool Imported => JSInterop.Invocations.Any(invocation =>
        invocation.Identifier == "import" && Equals(invocation.Arguments[0], OmniModules.SelectOnFocus));

    [Theory]
    [MemberData(nameof(Fields))]
    public void ByDefault_TheScriptIsNeverImported_AndTheMarkupIsUnchanged(string kind)
    {
        var plain = RenderField(kind, null);
        var off = RenderField(kind, false);

        Assert.False(Imported);
        Assert.Empty(_module.Invocations["attach"]);
        Assert.Equal(Normalised(plain.Rendered.Markup), Normalised(off.Rendered.Markup));
        Assert.DoesNotContain("select", Normalised(plain.Rendered.Markup), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(Fields))]
    public void On_TheScriptIsImportedAndAttachedToTheFieldElement_AndTheMarkupGainsNothing(string kind)
    {
        var off = RenderField(kind, null);
        var on = RenderField(kind, true);

        Assert.True(Imported);
        var attach = _module.Invocations["attach"].Single(invocation => Equals(((ElementReference)invocation.Arguments[0]!).Id, on.ElementReference));
        Assert.Single(attach.Arguments);
        Assert.Equal(Normalised(off.Rendered.Markup), Normalised(on.Rendered.Markup));
    }

    [Theory]
    [MemberData(nameof(Fields))]
    public void TurningItOn_AttachesOnce_AndTurningItOffDetachesTheSameElement(string kind)
    {
        var field = RenderField(kind, false);
        Assert.False(Imported);

        field.SetSelectOnFocus(true);
        Assert.Equal(field.ElementReference, ((ElementReference)Assert.Single(_module.Invocations["attach"]).Arguments[0]!).Id);

        // A render that changes nothing about the element does not attach it again.
        field.SetSelectOnFocus(true);
        Assert.Single(_module.Invocations["attach"]);
        Assert.Empty(_module.Invocations["detach"]);

        field.SetSelectOnFocus(false);
        Assert.Equal(field.ElementReference, ((ElementReference)Assert.Single(_module.Invocations["detach"]).Arguments[0]!).Id);

        field.SetSelectOnFocus(true);
        Assert.Equal(2, _module.Invocations["attach"].Count);
        Assert.Single(_module.Invocations["detach"]);
    }

    [Theory]
    [MemberData(nameof(Fields))]
    public async Task Disposing_DetachesTheScript_AndAFieldThatNeverAskedForItCallsNothing(string kind)
    {
        var plain = RenderField(kind, false);
        await plain.Dispose();
        Assert.False(Imported);
        Assert.Empty(_module.Invocations["detach"]);

        var on = RenderField(kind, true);
        await on.Dispose();

        Assert.Equal(on.ElementReference, ((ElementReference)Assert.Single(_module.Invocations["detach"]).Arguments[0]!).Id);
    }

    [Theory]
    [MemberData(nameof(Fields))]
    public async Task ALostCircuit_IsIgnored_WhileAttachingAndWhileDetaching(string kind)
    {
        _module.SetupVoid("attach", _ => true).SetException(new JSDisconnectedException("circuit perdu"));
        _module.SetupVoid("detach", _ => true).SetException(new JSDisconnectedException("circuit perdu"));

        var field = RenderField(kind, true);
        Assert.Single(_module.Invocations["attach"]);

        await field.Dispose();
        Assert.Empty(_module.Invocations["detach"]);
    }

    [Fact]
    public void TheTextBox_FollowsItsInputWhenTheIconRedrawsIt()
    {
        var box = Render<OmniTextBox>(parameters => parameters
            .Add(c => c.SelectOnFocus, true).Add(c => c.Value, _text).Add(c => c.ValueExpression, () => _text));
        var first = box.Find("input").GetAttribute("blazor:elementReference");

        // The input moves into a wrapper: Blazor draws a new element, which the script must now follow.
        box.Render(parameters => parameters.Add(c => c.ShowCopy, true));

        var second = box.Find("input").GetAttribute("blazor:elementReference");
        Assert.NotEqual(first, second);
        Assert.Equal(first, ((ElementReference)_module.Invocations["detach"].Single().Arguments[0]!).Id);
        Assert.Equal(second, ((ElementReference)_module.Invocations["attach"].Last().Arguments[0]!).Id);
    }
}
