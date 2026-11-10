using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// recette R-13: the text fields keep the browser's history of typed values. They write a <c>name</c>, under which
/// Chrome keeps that history, also in an interactive form where Blazor writes none: the <c>Id</c> when set, else the
/// type of the bound model and the member path, so that two forms whose models both have a <c>Name</c> keep two
/// histories. A
/// text box given no autocomplete writes none either: an invented token would fail the accessibility audits (axe
/// <c>autocomplete-valid</c>), and the browser's own behaviour keeps the history. What Chrome saves is measured in
/// Chrome (chrome://autofill-internals), not here.
/// </summary>
public sealed class FieldNameAutocompleteTests : OmniBunitContext
{
    private sealed class ProjectModel
    {
        public string Name { get; set; } = string.Empty;

        public ProjectModel? Parent { get; set; }

        public int Count { get; set; }
    }

    private sealed class AlertModel
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class Envelope<T>
    {
        public T? Content { get; set; }
    }

    private ProjectModel Project { get; } = new() { Parent = new ProjectModel() };

    private AlertModel Alert { get; } = new();

    private Envelope<string> Wrapped { get; } = new();

    [Fact]
    public void Two_models_whose_member_is_called_name_give_two_names()
    {
        var project = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Project.Name)).Find("input");
        var alert = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Alert.Name)).Find("input");

        Assert.Equal("ProjectModel.Name", project.GetAttribute("name"));
        Assert.Equal("AlertModel.Name", alert.GetAttribute("name"));
    }

    [Fact]
    public void An_id_wins_over_the_member_path()
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.Id, "project-name")
            .Add(component => component.ValueExpression, () => Project.Name)).Find("input");

        Assert.Equal("project-name", input.GetAttribute("name"));
    }

    [Fact]
    public void A_name_given_as_attribute_wins_over_the_id()
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.Id, "project-name")
            .Add(component => component.ValueExpression, () => Project.Name)
            .AddUnmatched("name", "raison-sociale")).Find("input");

        Assert.Equal("raison-sociale", input.GetAttribute("name"));
    }

    [Fact]
    public void A_generic_model_is_named_without_its_arity()
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Wrapped.Content!)).Find("input");

        Assert.Equal("Envelope.Content", input.GetAttribute("name"));
    }

    // The edit context of an interactive EditForm, where Blazor itself writes no name (it does only in a static render).
    private readonly EditContext _form = new(new object()) { ShouldUseFieldIdentifiers = false };

    [Theory]
    [InlineData(OmniTextBoxType.Text)]
    [InlineData(OmniTextBoxType.Search)]
    [InlineData(OmniTextBoxType.Email)]
    [InlineData(OmniTextBoxType.Tel)]
    [InlineData(OmniTextBoxType.Url)]
    public void A_text_box_given_no_parameter_is_named_by_the_bound_member_and_writes_no_autocomplete(OmniTextBoxType type)
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.Type, type)
            .Add(component => component.Value, Project.Name)
            .Add(component => component.ValueExpression, () => Project.Name)).Find("input");

        Assert.Equal("ProjectModel.Name", input.GetAttribute("name"));
        Assert.False(input.HasAttribute("autocomplete"));
    }

    [Fact]
    public void A_nested_member_gives_its_whole_path()
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Project.Parent!.Name)).Find("input");

        Assert.Equal("ProjectModel.Parent.Name", input.GetAttribute("name"));
    }

    [Fact]
    public void A_name_and_an_autocomplete_given_as_attributes_win()
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Project.Name)
            .AddUnmatched("name", "raison-sociale")
            .AddUnmatched("autocomplete", "organization")).Find("input");

        Assert.Equal("raison-sociale", input.GetAttribute("name"));
        Assert.Equal("organization", input.GetAttribute("autocomplete"));
    }

    [Theory]
    [InlineData("organization")]
    [InlineData("name")]
    [InlineData("off")]
    public void The_autocomplete_parameter_is_written_as_given(string given)
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Project.Name)
            .Add(component => component.Autocomplete, given)).Find("input");

        Assert.Equal(given, input.GetAttribute("autocomplete"));
    }

    [Fact]
    public void The_wrapped_text_box_writes_the_name_on_its_input()
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ShowCopy, true)
            .Add(component => component.ValueExpression, () => Project.Name)).Find("input");

        Assert.Equal("ProjectModel.Name", input.GetAttribute("name"));
        Assert.False(input.HasAttribute("autocomplete"));
    }

    [Fact]
    public void The_other_text_fields_are_named_too()
    {
        Assert.Equal("ProjectModel.Name", Render<OmniTextArea>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Project.Name)).Find("textarea").GetAttribute("name"));
        Assert.Equal("ProjectModel.Name", Render<OmniPassword>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Project.Name)).Find("input").GetAttribute("name"));
        Assert.Equal("ProjectModel.Count", Render<OmniNumeric<int>>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Project.Count)).Find("input").GetAttribute("name"));
    }

    [Fact]
    public void A_bound_value_that_is_not_a_member_chain_falls_back_to_the_id()
    {
        string[] names = ["a"];
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.Id, "project-name")
            .Add(component => component.ValueExpression, () => names[0])).Find("input");

        Assert.Equal("project-name", input.GetAttribute("name"));
    }

    [Fact]
    public void In_a_static_render_the_name_blazor_formats_is_kept()
    {
        _form.ShouldUseFieldIdentifiers = true;
        string[] names = ["a"];
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.Id, "project-name")
            .Add(component => component.ValueExpression, () => names[0])).Find("input");

        Assert.Equal("names[0]", input.GetAttribute("name"));
    }

    [Fact]
    public void A_field_keeps_its_name_across_renders_of_the_same_bound_value()
    {
        var bound = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Project.Name));
        bound.Render(parameters => parameters.Add(component => component.Placeholder, "Name"));

        Assert.Equal("ProjectModel.Name", bound.Find("input").GetAttribute("name"));
    }

    private static string Shared { get; } = string.Empty;

    private static ProjectModel Make() => new();

    [Fact]
    public void The_member_path_reads_through_a_conversion_and_refuses_anything_else()
    {
        Expression<Func<object>> converted = () => Project.Count;
        Expression<Func<string>> call = () => Project.Name.Trim();

        Assert.Equal("ProjectModel.Count", OmniInputBase<string>.MemberPath(converted.Body));
        Assert.Null(OmniInputBase<string>.MemberPath(call.Body));
        Expression<Func<string>> fromCall = () => Make().Name;
        Assert.Null(OmniInputBase<string>.MemberPath(fromCall.Body));
        Assert.Null(OmniInputBase<string>.MemberPath(null));
        Expression<Func<string>> staticMember = () => Shared;
        Assert.Equal("FieldNameAutocompleteTests.Shared", OmniInputBase<string>.MemberPath(staticMember.Body));
    }
}
