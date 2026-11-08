using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// recette R-13: the text fields keep the browser's history of typed values. They write a <c>name</c>, the path of
/// the bound member (Chrome keys the history by it), and a text field given no autocomplete writes a token no browser
/// recognises, which Chrome reads as "no autofill type": no card or address guessed from a label such as "Nom". The
/// browser's reading of the token is measured in Chrome (chrome://autofill-internals), not here.
/// </summary>
public sealed class FieldNameAutocompleteTests : OmniBunitContext
{
    private sealed class ProjectModel
    {
        public string Name { get; set; } = string.Empty;

        public ProjectModel? Parent { get; set; }

        public int Count { get; set; }
    }

    private ProjectModel Project { get; } = new() { Parent = new ProjectModel() };

    // The edit context of an interactive EditForm, where Blazor itself writes no name (it does only in a static render).
    private readonly EditContext _form = new(new object()) { ShouldUseFieldIdentifiers = false };

    [Fact]
    public void A_text_box_given_no_parameter_is_named_by_the_bound_member_and_writes_the_neutral_token()
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.Value, Project.Name)
            .Add(component => component.ValueExpression, () => Project.Name)).Find("input");

        Assert.Equal("Project.Name", input.GetAttribute("name"));
        Assert.Equal(OmniTextBox.NeutralAutocomplete, input.GetAttribute("autocomplete"));
    }

    [Fact]
    public void A_nested_member_gives_its_whole_path()
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Project.Parent!.Name)).Find("input");

        Assert.Equal("Project.Parent.Name", input.GetAttribute("name"));
    }

    [Fact]
    public void A_name_and_an_autocomplete_given_by_the_caller_win()
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Project.Name)
            .AddUnmatched("name", "raison-sociale")
            .AddUnmatched("autocomplete", "organization")).Find("input");

        Assert.Equal("raison-sociale", input.GetAttribute("name"));
        Assert.Equal("organization", input.GetAttribute("autocomplete"));
    }

    [Theory]
    [InlineData("organization", "organization")]
    [InlineData("off", "off")]
    public void The_autocomplete_parameter_wins_over_the_neutral_token(string given, string expected)
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Project.Name)
            .Add(component => component.Autocomplete, given)).Find("input");

        Assert.Equal(expected, input.GetAttribute("autocomplete"));
    }

    [Theory]
    [InlineData(OmniTextBoxType.Text, OmniTextBox.NeutralAutocomplete)]
    [InlineData(OmniTextBoxType.Search, OmniTextBox.NeutralAutocomplete)]
    [InlineData(OmniTextBoxType.Email, null)]
    [InlineData(OmniTextBoxType.Tel, null)]
    [InlineData(OmniTextBoxType.Url, null)]
    public void Only_text_and_search_fields_take_the_neutral_token(OmniTextBoxType type, string? expected)
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.Type, type)
            .Add(component => component.ValueExpression, () => Project.Name)).Find("input");

        Assert.Equal(expected, input.GetAttribute("autocomplete"));
    }

    [Fact]
    public void The_wrapped_text_box_writes_the_name_and_the_token_on_its_input()
    {
        var input = Render<OmniTextBox>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ShowCopy, true)
            .Add(component => component.ValueExpression, () => Project.Name)).Find("input");

        Assert.Equal("Project.Name", input.GetAttribute("name"));
        Assert.Equal(OmniTextBox.NeutralAutocomplete, input.GetAttribute("autocomplete"));
    }

    [Fact]
    public void The_other_text_fields_are_named_too()
    {
        Assert.Equal("Project.Name", Render<OmniTextArea>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Project.Name)).Find("textarea").GetAttribute("name"));
        Assert.Equal("Project.Name", Render<OmniPassword>(parameters => parameters.AddCascadingValue(_form)
            .Add(component => component.ValueExpression, () => Project.Name)).Find("input").GetAttribute("name"));
        Assert.Equal("Project.Count", Render<OmniNumeric<int>>(parameters => parameters.AddCascadingValue(_form)
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

    private static ProjectModel Make() => new();

    [Fact]
    public void The_member_path_reads_through_a_conversion_and_refuses_anything_else()
    {
        Expression<Func<object>> converted = () => Project.Count;
        Expression<Func<string>> call = () => Project.Name.Trim();

        Assert.Equal("Project.Count", OmniInputBase<string>.MemberPath(converted.Body));
        Assert.Null(OmniInputBase<string>.MemberPath(call.Body));
        Expression<Func<string>> fromCall = () => Make().Name;
        Assert.Null(OmniInputBase<string>.MemberPath(fromCall.Body));
        Assert.Null(OmniInputBase<string>.MemberPath(null));
    }
}
