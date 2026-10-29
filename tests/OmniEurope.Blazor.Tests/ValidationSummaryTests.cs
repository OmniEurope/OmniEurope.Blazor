using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniValidationSummary over a real edit context and a real message store: the title agrees with the
/// number of errors in the language of the page, nothing is drawn without an error or when the host hides
/// the summary, the host's position picks which slot draws, and the summary follows the context.
/// </summary>
public sealed class ValidationSummaryTests : OmniBunitContext
{
    private sealed class Model
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }

    private readonly EditContext _context;
    private readonly ValidationMessageStore _store;

    public ValidationSummaryTests()
    {
        _context = new EditContext(new Model());
        _store = new ValidationMessageStore(_context);
    }

    private IRenderedComponent<ValidationSummaryTestHost> RenderHost(OmniValidationSummaryPosition position = OmniValidationSummaryPosition.Bottom, string? title = null)
        => Render<ValidationSummaryTestHost>(parameters => parameters
            .Add(host => host.Context, _context)
            .Add(host => host.Position, position)
            .Add(host => host.Title, title));

    private Task Raise(IRenderedComponent<ValidationSummaryTestHost> host, params (string Field, string Message)[] errors)
        => host.InvokeAsync(() =>
        {
            _store.Clear();
            foreach (var (field, message) in errors)
            {
                _store.Add(_context.Field(field), message);
            }

            _context.NotifyValidationStateChanged();
        });

    private static string TitleOf(IRenderedComponent<ValidationSummaryTestHost> host) => host.Find(".omni-alert__title").TextContent;

    private static T InCulture<T>(string language, Func<T> read)
    {
        var (culture, ui) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
            return read();
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, ui);
        }
    }

    [Fact]
    public async Task One_error_gets_the_singular_title()
    {
        var host = InCulture("en", () => RenderHost());

        await InCulture("en", () => Raise(host, (nameof(Model.Name), "The name is required.")));

        Assert.Equal("1 error to correct", TitleOf(host));
    }

    [Fact]
    public async Task Several_errors_get_the_plural_title()
    {
        var host = InCulture("en", () => RenderHost());

        await InCulture("en", () => Raise(host, (nameof(Model.Name), "The name is required."), (nameof(Model.Email), "The address is invalid.")));

        Assert.Equal("2 errors to correct", TitleOf(host));
    }

    [Theory]
    [InlineData("fr", 1, "1 erreur à corriger")]
    [InlineData("fr", 2, "2 erreurs à corriger")]
    [InlineData("pl", 1, "1 błąd do poprawienia")]
    [InlineData("pl", 3, "3 błędy do poprawienia")]
    [InlineData("pl", 5, "5 błędów do poprawienia")]
    [InlineData("de", 2, "2 Fehler zu korrigieren")]
    public async Task The_title_agrees_with_the_count_in_the_language_of_the_page(string language, int count, string expected)
    {
        var host = InCulture(language, () => RenderHost());
        var errors = Enumerable.Range(0, count).Select(index => (nameof(Model.Name), $"Error {index}")).ToArray();

        await InCulture(language, () => Raise(host, errors));

        Assert.Equal(expected, TitleOf(host));
    }

    [Fact]
    public void Nothing_is_drawn_without_an_error()
    {
        var host = RenderHost();

        Assert.Empty(host.FindAll(".omni-alert"));
        Assert.Empty(host.FindAll(".omni-validation-summary"));
    }

    [Theory]
    [InlineData(OmniValidationSummaryPosition.Top)]
    [InlineData(OmniValidationSummaryPosition.Bottom)]
    [InlineData(OmniValidationSummaryPosition.Hidden)]
    public async Task Only_one_slot_draws_and_hidden_draws_none(OmniValidationSummaryPosition position)
    {
        var host = RenderHost(position);

        await Raise(host, (nameof(Model.Name), "The name is required."));

        Assert.Equal(position == OmniValidationSummaryPosition.Hidden ? 0 : 1, host.FindAll(".omni-validation-summary").Count);
    }

    [Fact]
    public async Task Top_draws_before_the_fields()
    {
        var host = RenderHost(OmniValidationSummaryPosition.Top);

        await Raise(host, (nameof(Model.Name), "The name is required."));

        var form = host.Find("form");
        Assert.Contains("omni-validation-summary", form.FirstElementChild!.ClassName);
        Assert.Equal("test-fields", form.FirstElementChild.NextElementSibling!.ClassName);
    }

    [Fact]
    public async Task Bottom_draws_after_the_fields_just_above_the_actions()
    {
        var host = RenderHost(OmniValidationSummaryPosition.Bottom);

        await Raise(host, (nameof(Model.Name), "The name is required."));

        var actions = host.Find(".omni-form-actions");
        var summary = actions.PreviousElementSibling!;
        Assert.Contains("omni-validation-summary", summary.ClassName);
        Assert.Equal("test-fields", summary.PreviousElementSibling!.ClassName);
    }

    [Fact]
    public async Task Every_message_is_listed_in_order_inside_a_danger_alert()
    {
        var host = RenderHost();

        await Raise(host, (nameof(Model.Name), "The name is required."), (nameof(Model.Email), "The address is invalid."));

        var alert = host.Find(".omni-validation-summary");
        Assert.Contains("omni-alert--danger", alert.ClassName);
        Assert.Equal(["The name is required.", "The address is invalid."], host.FindAll(".omni-validation-summary li").Select(item => item.TextContent));
    }

    [Fact]
    public async Task Fixing_an_error_updates_the_title_and_the_last_fix_removes_the_alert()
    {
        var host = InCulture("en", () => RenderHost());
        await InCulture("en", () => Raise(host, (nameof(Model.Name), "The name is required."), (nameof(Model.Email), "The address is invalid.")));
        Assert.Equal("2 errors to correct", TitleOf(host));

        await InCulture("en", () => Raise(host, (nameof(Model.Email), "The address is invalid.")));
        Assert.Equal("1 error to correct", TitleOf(host));
        Assert.Equal(["The address is invalid."], host.FindAll(".omni-validation-summary li").Select(item => item.TextContent));

        await Raise(host);
        Assert.Empty(host.FindAll(".omni-alert"));
    }

    [Fact]
    public async Task A_title_given_by_the_host_replaces_the_counted_one()
    {
        var host = RenderHost(title: "Please correct the following");

        await Raise(host, (nameof(Model.Name), "The name is required."));

        Assert.Equal("Please correct the following", TitleOf(host));
    }

    [Fact]
    public void Outside_a_form_the_summary_refuses_to_render()
    {
        var exception = Assert.ThrowsAny<Exception>(() => Render<OmniValidationSummary>());

        Assert.Contains("EditContext", exception.ToString(), StringComparison.Ordinal);
    }
}
