using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The required mark of a form field follows the property its control is bound to (recette R3-6): a
/// property marked [Required] shows it without Required written by hand, Required written wins (true
/// forces, false hides), and a control bound to no expression shows none.
/// </summary>
public sealed class FormFieldRequiredFromModelTests : OmniBunitContext
{
    private sealed class Profile
    {
        [Required]
        public string Model { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;
    }

    private IRenderedComponent<EditForm> RenderField(Expression<Func<string>> bound, bool? required = null, string forId = "field", string controlId = "field")
    {
        return Render<EditForm>(parameters => parameters
            .Add(form => form.Model, Sample)
            .Add(form => form.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenComponent<OmniFormField>(0);
                builder.AddComponentParameter(1, nameof(OmniFormField.For), forId);
                builder.AddComponentParameter(2, nameof(OmniFormField.Label), "Modèle");
                if (required is not null)
                {
                    builder.AddComponentParameter(3, nameof(OmniFormField.Required), required);
                }

                builder.AddComponentParameter(4, nameof(OmniFormField.ChildContent), (RenderFragment)(inner =>
                {
                    inner.OpenComponent<OmniTextBox>(0);
                    inner.AddComponentParameter(1, nameof(OmniTextBox.Id), controlId);
                    inner.AddComponentParameter(2, nameof(OmniTextBox.Value), bound.Compile()());
                    inner.AddComponentParameter(3, nameof(OmniTextBox.ValueExpression), bound);
                    inner.CloseComponent();
                }));
                builder.CloseComponent();
            })));
    }

    private static Profile Sample { get; } = new();

    [Fact]
    public void APropertyMarkedRequired_ShowsTheMark_WithoutRequiredWritten()
    {
        var form = RenderField(() => Sample.Model);

        form.WaitForAssertion(() => Assert.Single(form.FindAll(".omni-label__required")));
    }

    [Fact]
    public void APropertyWithoutRequired_ShowsNoMark()
    {
        var form = RenderField(() => Sample.Note);

        Assert.Empty(form.FindAll(".omni-label__required"));
    }

    [Fact]
    public void RequiredFalse_HidesTheMarkOfARequiredProperty()
    {
        var form = RenderField(() => Sample.Model, required: false);

        Assert.Empty(form.FindAll(".omni-label__required"));
    }

    [Fact]
    public void RequiredTrue_ForcesTheMarkOfAnOptionalProperty()
    {
        var form = RenderField(() => Sample.Note, required: true);

        Assert.Single(form.FindAll(".omni-label__required"));
    }

    [Fact]
    public void AControlTheLabelDoesNotName_HasNoSay()
    {
        var form = RenderField(() => Sample.Model, forId: "other");

        Assert.Empty(form.FindAll(".omni-label__required"));
    }

    [Fact]
    public void AControlWithoutExpression_ShowsNoMark()
    {
        // A package control always has an expression (Blazor's InputBase demands one): what has none is
        // the host's own markup, which reports nothing.
        var form = Render<OmniFormField>(parameters => parameters
            .Add(field => field.For, "free")
            .Add(field => field.Label, "Libre")
            .AddChildContent("<input id=\"free\" />"));

        Assert.Empty(form.FindAll(".omni-label__required"));
    }

    [Fact]
    public void TheMarkLeavesWithTheControl()
    {
        var shown = true;
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "field")
            .Add(component => component.Label, "Modèle")
            .Add(component => component.ChildContent, (RenderFragment)(builder =>
            {
                if (!shown)
                {
                    return;
                }

                builder.OpenComponent<OmniTextBox>(0);
                builder.AddComponentParameter(1, nameof(OmniTextBox.Id), "field");
                builder.AddComponentParameter(2, nameof(OmniTextBox.Value), Sample.Model);
                builder.AddComponentParameter(3, nameof(OmniTextBox.ValueExpression), (Expression<Func<string>>)(() => Sample.Model));
                builder.CloseComponent();
            })));
        field.WaitForAssertion(() => Assert.Single(field.FindAll(".omni-label__required")));

        shown = false;
        field.Render();

        field.WaitForAssertion(() => Assert.Empty(field.FindAll(".omni-label__required")));
    }
}
