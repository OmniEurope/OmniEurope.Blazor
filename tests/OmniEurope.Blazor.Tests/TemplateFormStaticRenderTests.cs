using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// An invalid submit of a statically rendered form (a sign-in page posted without a circuit) keeps
/// its validation messages and calls no script: the focus interop would throw outside interactivity.
/// </summary>
public sealed class TemplateFormStaticRenderTests : OmniBunitContext
{
    private sealed class RequiredModel
    {
        [Required]
        public string? Name { get; set; }
    }

    private IRenderedComponent<OmniTemplateForm<RequiredModel>> RenderInvalidForm(Action onInvalid)
        => Render<OmniTemplateForm<RequiredModel>>(parameters => parameters
            .Add(form => form.Model, new RequiredModel())
            .Add(form => form.OnInvalidSubmit, EventCallback.Factory.Create<EditContext>(this, _ => onInvalid()))
            .Add(form => form.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
            })));

    [Fact]
    public void InvalidSubmit_WithoutInteractivity_RaisesTheCallbackAndCallsNoScript()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        JSInterop.Mode = JSRuntimeMode.Strict;
        var invalid = 0;

        var form = RenderInvalidForm(() => invalid++);
        form.Find("form").Submit();

        Assert.Equal(1, invalid);
        Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "import");
    }

    [Fact]
    public void InvalidSubmit_WhenInteractive_StillFocusesTheFirstInvalidField()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;

        var form = RenderInvalidForm(() => { });
        form.Find("form").Submit();

        Assert.Contains(JSInterop.Invocations, invocation => invocation.Identifier == "focusFirstInvalid");
    }

    [Fact]
    public async Task FormOverAnEditContext_TracksItsModel_AndAFieldNotifiedUnchangedRendersNothingMore()
    {
        var model = new RequiredModel { Name = "Ada" };
        var context = new EditContext(model);
        var form = Render<OmniTemplateForm<RequiredModel>>(parameters => parameters
            .Add(component => component.EditContext, context)
            .Add(component => component.ChildContent, (RenderFragment<EditContext>)(_ => builder => builder.AddContent(0, "Champs"))));
        var renders = form.RenderCount;

        await form.InvokeAsync(() => context.NotifyFieldChanged(new FieldIdentifier(model, nameof(RequiredModel.Name))));

        Assert.Equal(renders, form.RenderCount);
        Assert.Equal("Champs", form.Find("form").TextContent);
    }

    [Fact]
    public void FormOverTheSameModel_KeepsItsEditContextAcrossRenders()
    {
        var model = new RequiredModel();
        EditContext? first = null;
        EditContext? second = null;
        var form = Render<OmniTemplateForm<RequiredModel>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.ChildContent, (RenderFragment<EditContext>)(context => builder => first ??= context)));

        form.Render(parameters => parameters.Add(component => component.ChildContent, (RenderFragment<EditContext>)(context => builder => second = context)));

        Assert.Same(first, second);
    }
}
