using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniDynamicForm's rules on values the host passes: numbers that are not numbers, out of range or
/// missing, choices outside the options, booleans that are not booleans, and fields that come and go.
/// </summary>
public sealed class DynamicFormRulesTests : OmniBunitContext
{
    private IRenderedComponent<OmniDynamicForm> RenderForm(IReadOnlyList<OmniDynamicField> fields, IReadOnlyDictionary<string, string>? values) =>
        Render<OmniDynamicForm>(parameters => parameters
            .Add(form => form.Fields, fields)
            .Add(form => form.Values, values));

    // Validate renders the messages: it runs on the renderer's dispatcher, as an event handler would.
    private static bool Validate(IRenderedComponent<OmniDynamicForm> form) => form.InvokeAsync(form.Instance.Validate).GetAwaiter().GetResult();

    private static IReadOnlyList<string> Messages(IRenderedComponent<OmniDynamicForm> form) =>
        [.. form.FindAll("[id$='-error']").Select(error => error.TextContent.Trim())];

    [Fact]
    public void TwoFieldsWithOneName_AreRefused_IgnoringCase()
    {
        var error = Assert.Throws<ArgumentException>(() => RenderForm([new("port", "Port"), new("PORT", "Port bis")], null));
        Assert.Equal("Fields", error.ParamName);
    }

    [Theory]
    [InlineData("abc", "Port doit être un nombre.")]
    [InlineData("70000", "Port doit valoir au plus 65535.")]
    [InlineData("0", "Port doit valoir au moins 1.")]
    [InlineData("8080", null)]
    public void Number_IsCheckedAgainstItsRange(string value, string? message)
    {
        var form = RenderForm(
            [new OmniDynamicField("port", "Port", OmniDynamicFieldKind.Number) { Minimum = 1, Maximum = 65535 }],
            new Dictionary<string, string> { ["port"] = value });

        Assert.Equal(message is null, Validate(form));
        Assert.Equal(message is null ? [] : [message], Messages(form));
        // A value that is not a number is kept as typed.
        Assert.Equal(value, form.Instance.CurrentValues["port"]);
    }

    [Fact]
    public void Number_WithoutBounds_TakesAnyNumber()
    {
        var form = RenderForm([new OmniDynamicField("port", "Port", OmniDynamicFieldKind.Number)], new Dictionary<string, string> { ["port"] = "-5" });

        Assert.True(Validate(form));
    }

    [Theory]
    [InlineData(true, "Port est obligatoire.")]
    [InlineData(false, null)]
    public void EmptyNumber_IsMissingOnlyWhenRequired(bool required, string? message)
    {
        var form = RenderForm([new OmniDynamicField("port", "Port", OmniDynamicFieldKind.Number) { Required = required }], null);

        Assert.Equal(message is null, Validate(form));
        Assert.Equal(message is null ? [] : [message], Messages(form));
        Assert.False(form.Instance.CurrentValues.ContainsKey("port"));
    }

    [Theory]
    [InlineData("eu-west", true, null)]
    [InlineData("mars", true, "Région doit être l'une des valeurs proposées.")]
    [InlineData("", true, "Région est obligatoire.")]
    [InlineData("", false, null)]
    public void Choice_MustBeOneOfTheOptions(string value, bool required, string? message)
    {
        var form = RenderForm(
            [new OmniDynamicField("region", "Région", OmniDynamicFieldKind.Choice) { Required = required, Options = [new OmniOption<string>("eu-west", "Europe ouest")] }],
            new Dictionary<string, string> { ["region"] = value });

        Assert.Equal(message is null, Validate(form));
        Assert.Equal(message is null ? [] : [message], Messages(form));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("oui", false)]
    public void Boolean_IsAnsweredOnlyByAReadableValue(string value, bool answered)
    {
        var form = RenderForm(
            [new OmniDynamicField("confirm", "Confirmer", OmniDynamicFieldKind.Boolean) { Required = true }],
            new Dictionary<string, string> { ["confirm"] = value });

        Assert.Equal(answered, Validate(form));
        Assert.Equal(answered, form.Instance.CurrentValues.ContainsKey("confirm"));
    }

    [Fact]
    public void RemovedField_LeavesItsValueAndMessage_AndShownMessagesAreCheckedAgain()
    {
        var name = new OmniDynamicField("name", "Nom") { Required = true };
        var port = new OmniDynamicField("port", "Port", OmniDynamicFieldKind.Number) { Required = true };
        var form = RenderForm([name, port], new Dictionary<string, string> { ["name"] = "srv" });
        Assert.False(Validate(form));
        Assert.Equal(["Port est obligatoire."], Messages(form));

        // The port goes; the new values fill the name: its shown state is checked again, still valid.
        form.Render(parameters => parameters
            .Add(component => component.Fields, [name])
            .Add(component => component.Values, new Dictionary<string, string> { ["name"] = "srv-02" }));

        Assert.Empty(Messages(form));
        Assert.Equal(["name"], form.Instance.CurrentValues.Keys);
        Assert.True(form.Instance.IsValid);

        form.Render(parameters => parameters.Add(component => component.Values, new Dictionary<string, string>()));
        Assert.Equal(["Nom est obligatoire."], Messages(form));
    }
}
