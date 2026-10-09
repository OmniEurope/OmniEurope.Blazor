using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The behaviours the audit of 2026-10-07 found wrong: a scheduler whose loader is taken away
/// (RCL-013), a validator moved to another field (RCL-011), a return address before a fragment
/// (RCL-016), the paths Git quotes in a diff (RCL-012) and a tab drawn by its TitleContent (RCL-015).
/// </summary>
public sealed class BehaviourAuditTests : OmniBunitContext
{
    private static readonly DateTimeOffset Monday = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    // ---- scheduler ----------------------------------------------------------------------------------

    [Fact]
    public async Task SchedulerWhoseLoaderIsTakenAway_ShowsItsItems_AndALateFailureShowsNothing()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<OmniSchedulerAppointment>>();
        var token = CancellationToken.None;
        var errors = new List<Exception>();
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, Monday)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.View, OmniCalendarView.Week)
            .Add(component => component.OnLoadError, exception => errors.Add(exception))
            .Add(component => component.Load, (_, _, cancellation) =>
            {
                token = cancellation;
                return pending.Task;
            }));
        Assert.Equal("true", scheduler.Find("section.omni-scheduler").GetAttribute("aria-busy"));

        OmniSchedulerAppointment[] items = [new("1", "Réunion", Monday.AddHours(9), Monday.AddHours(10))];
        scheduler.Render(parameters => parameters
            .Add(component => component.Load, null)
            .Add(component => component.Items, items));

        Assert.True(token.IsCancellationRequested);
        Assert.Equal("false", scheduler.Find("section.omni-scheduler").GetAttribute("aria-busy"));
        Assert.Contains("Réunion", scheduler.Markup, StringComparison.Ordinal);

        await scheduler.InvokeAsync(() => pending.SetException(new InvalidOperationException("tard")));
        scheduler.Render(parameters => parameters.Add(component => component.Items, items));

        Assert.Empty(errors);
        Assert.Equal("false", scheduler.Find("section.omni-scheduler").GetAttribute("aria-busy"));
        Assert.DoesNotContain("tard", scheduler.Markup, StringComparison.Ordinal);
        Assert.Contains("Réunion", scheduler.Markup, StringComparison.Ordinal);
    }

    // ---- validators ---------------------------------------------------------------------------------

    public sealed class Person
    {
        public string? Name { get; set; }

        public string? City { get; set; } = "Namur";
    }

    [Fact]
    public void ValidatorMovedToAnotherField_ForgetsTheMessageOfTheFirst()
    {
        var person = new Person();
        var form = new EditContext(person);
        var required = Render<OmniRequiredValidator<string?>>(parameters => parameters
            .AddCascadingValue(form)
            .Add(component => component.For, () => person.Name)
            .Add(component => component.Message, "Le champ manque."));
        form.Validate();
        Assert.Equal("Le champ manque.", required.Find(".omni-validation-message").TextContent);

        required.Render(parameters => parameters
            .Add(component => component.For, () => person.City));

        Assert.Equal(string.Empty, required.Find(".omni-validation-message").TextContent);
        Assert.Empty(form.GetValidationMessages());

        // The same field given again keeps what it showed.
        form.Validate();
        required.Render(parameters => parameters
            .Add(component => component.For, () => person.City)
            .Add(component => component.Message, "Autre."));
        Assert.Empty(form.GetValidationMessages());
    }

    // ---- return address -----------------------------------------------------------------------------

    [Theory]
    [InlineData("/signin#section", "/signin?returnUrl=%2Forders#section")]
    [InlineData("/signin?lang=fr#section", "/signin?lang=fr&returnUrl=%2Forders#section")]
    [InlineData("/signin?lang=fr", "/signin?lang=fr&returnUrl=%2Forders")]
    [InlineData("/signin", "/signin?returnUrl=%2Forders")]
    public void ReturnAddress_GoesBeforeTheFragment(string path, string expected) =>
        Assert.Equal(expected, OmniReturnUrl.Append(path, "/orders"));

    // ---- quoted Git paths ---------------------------------------------------------------------------

    [Fact]
    public void QuotedGitPaths_AreDecoded_WithoutAnyOtherHeader()
    {
        const string diff = "diff --git \"a/r\\303\\251union.md\" \"b/r\\303\\251union.md\"\n"
            + "index 1111111..2222222 100644\n"
            + "Binary files \"a/r\\303\\251union.md\" and \"b/r\\303\\251union.md\" differ\n";

        var file = Assert.Single(OmniUnifiedDiffParser.Parse(diff));

        Assert.Equal("réunion.md", file.OldPath);
        Assert.Equal("réunion.md", file.Path);
        Assert.True(file.IsBinary);
    }

    [Fact]
    public void QuotedGitPaths_InTheFileAndRenameHeaders_AreDecoded()
    {
        const string diff = "diff --git a/plain.txt \"b/caf\\303\\251 \\\"1\\\".txt\"\n"
            + "similarity index 90%\n"
            + "rename from plain.txt\n"
            + "rename to \"caf\\303\\251 \\\"1\\\".txt\"\n"
            + "--- a/plain.txt\n"
            + "+++ \"b/caf\\303\\251 \\\"1\\\".txt\"\n"
            + "@@ -1 +1 @@\n"
            + "-a\n"
            + "+b\n";

        var file = Assert.Single(OmniUnifiedDiffParser.Parse(diff));

        Assert.Equal("plain.txt", file.OldPath);
        Assert.Equal("café \"1\".txt", file.NewPath);
        Assert.Equal(OmniDiffFileStatus.Renamed, file.Status);
    }

    [Fact]
    public void AGitHeaderQuotedOnTheLeftOnly_SplitsAfterTheQuote()
    {
        const string diff = "diff --git \"a/\\303\\251.txt\" b/e.txt\nBinary files differ\n";

        var file = Assert.Single(OmniUnifiedDiffParser.Parse(diff));

        Assert.Equal("é.txt", file.OldPath);
        Assert.Equal("e.txt", file.NewPath);
    }

    [Fact]
    public void AGitHeaderWhoseQuoteNeverCloses_FallsBackOnTheLastSide()
    {
        const string diff = "diff --git \"a/x b/y\nBinary files differ\n";

        var file = Assert.Single(OmniUnifiedDiffParser.Parse(diff));

        Assert.Equal("y", file.NewPath);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("\"", "\"")]
    [InlineData("\"\"", "")]
    [InlineData("\"a\\tb\\nc\\\\d\\\"e\"", "a\tb\nc\\d\"e")]
    [InlineData("\"\\a\\b\\v\\f\\r\"", "\a\b\v\f\r")]
    [InlineData("\"\\q\\12\"", "q12")]
    [InlineData("\"fin\\\"", "fin\\")]
    [InlineData("\"\U0001F600 é\"", "\U0001F600 é")]
    public void GitQuotedPath_DecodesTheEscapesOfAC_String(string token, string expected) =>
        Assert.Equal(expected, Internal.GitQuotedPath.Unquote(token));

    [Fact]
    public void GitQuotedPath_SplitsOnlyWhenASideIsQuoted()
    {
        Assert.False(Internal.GitQuotedPath.TrySplit("a/x b/y", out _, out _));
        Assert.False(Internal.GitQuotedPath.TrySplit("\"a/x\"", out _, out _));
        Assert.False(Internal.GitQuotedPath.TrySplit("b/y\"", out _, out _));
        Assert.True(Internal.GitQuotedPath.TrySplit("a/x \"b/\\303\\251\"", out var left, out var right));
        Assert.Equal(("a/x", "b/é"), (left, right));
    }

    // ---- tabs ---------------------------------------------------------------------------------------

    [Fact]
    public void ATabDrawnByItsTitleContent_KeepsItsTitleAsItsName()
    {
        var tabs = Render<OmniTabs>(parameters => parameters
            .Add(component => component.Label, "Sections")
            .Add(component => component.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniTabsItem>(0);
                builder.AddComponentParameter(1, nameof(OmniTabsItem.Key), "rapports");
                builder.AddComponentParameter(2, nameof(OmniTabsItem.Title), "Rapports");
                builder.AddComponentParameter(3, nameof(OmniTabsItem.TitleContent), (RenderFragment)(title => title.AddContent(0, "3")));
                builder.CloseComponent();
                builder.OpenComponent<OmniTabsItem>(4);
                builder.AddComponentParameter(5, nameof(OmniTabsItem.Key), "notes");
                builder.AddComponentParameter(6, nameof(OmniTabsItem.Title), "Notes");
                builder.CloseComponent();
            })));

        var buttons = tabs.FindAll("[role=tab]");
        Assert.Equal("Rapports", buttons[0].GetAttribute("aria-label"));
        Assert.Equal("3", buttons[0].TextContent.Trim());
        Assert.Null(buttons[1].GetAttribute("aria-label"));
        Assert.Equal("Notes", buttons[1].TextContent.Trim());
    }
}
