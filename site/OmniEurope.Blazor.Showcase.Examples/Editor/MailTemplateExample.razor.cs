using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>A mail template editor: subject, body, variables, a preview rendered for mail clients and a test hand-off.</summary>
public partial class MailTemplateExample
{
    /// <summary>The preview wears the light half of the default theme and palette, as the mail would be sent.</summary>
    private static readonly OmniMailTheme MailTheme = OmniMailTheme.From(OmniThemePresets.All[0]);

    private static readonly IReadOnlyList<OmniMailVariable> MailVariables =
    [
        new("client", "Client name") { SampleValue = "Anne Martin" },
        new("file", "File number") { SampleValue = "D-2026-0142" },
        new("due", "Due date") { SampleValue = new DateOnly(2026, 11, 30).ToString("d", CultureInfo.CurrentCulture) },
        new("link", "File link") { SampleValue = "https://example.eu/files/D-2026-0142", Kind = OmniMailVariableKind.Link },
        // A block variable: the application gives a fragment it rendered itself, here the list of the expected documents.
        new("pieces", "Expected documents")
        {
            Kind = OmniMailVariableKind.Block,
            SampleValue = "<ul><li>D-2026-0142-A</li><li>D-2026-0142-B</li></ul>",
        },
    ];

    private string? MailBody { get; set; } =
        "<h2>Reminder</h2><p>Hello {{client}},</p><p>File {{file}} awaits your answer before {{due}}.</p><p><a href=\"{{link}}\">Open the file</a></p>";

    private string? MailSubject { get; set; } = "Reminder for file {{file}}";

    private string? MailTestNotice { get; set; }

    /// <summary>Stands in for the application's mail sender: says which mail it received.</summary>
    private void SendMailTest(OmniMailRenderResult mail) => MailTestNotice = $"Test handed to the application, subject “{mail.Subject}”.";
}
