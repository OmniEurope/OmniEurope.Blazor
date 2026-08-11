using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Catalog.Resources;

namespace OmniEurope.Blazor.Catalog.Components.Pages;

public partial class Home : IDisposable
{
    [Inject]
    private IStringLocalizer<CatalogStrings> Text { get; set; } = default!;

    private readonly OmniOverlayService Overlays = new();
    private CatalogModel Model { get; set; } = default!;
    private EditContext EditContext = default!;
    private bool ToggleValue;
    private string? Tab = "list";
    private IReadOnlyList<string> TreeSelection = Array.Empty<string>();
    private IReadOnlyList<object> GridSelection = Array.Empty<object>();
    private string Alpha { get; } = "europe";
    private string Beta { get; } = "belgium";
    private IReadOnlyList<string> TabKeys { get; } = ["list", "tree"];
    private IReadOnlyList<string> Names { get; } = ["Astraia", "Aetheus", "Pronoia"];
    private IReadOnlyList<string> AllowedTypes { get; } = ["text/plain", "image/png"];
    private IReadOnlyList<OmniOption<string>> Options { get; set; } = Array.Empty<OmniOption<string>>();
    private IReadOnlyList<CatalogRow> Rows { get; } = [new(1, "Alpha", 30), new(2, "Beta", 70), new(3, "Gamma", 45)];
    private IReadOnlyList<OmniChartPoint> ChartPoints { get; } = [new(10, 30, "A"), new(50, 70, "B"), new(90, 45, "C")];
    private IReadOnlyList<string> ChartLabels { get; } = ["A", "B", "C"];
    private DateTimeOffset SchedulerDate { get; } = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
    private IReadOnlyList<OmniSchedulerAppointment> Appointments { get; set; } = Array.Empty<OmniSchedulerAppointment>();
    private RenderFragment NameLabel => builder => builder.AddContent(0, Text["NameLabel"]);
    private RenderFragment<string> NameTemplate => value => builder => builder.AddContent(0, value);
    private RenderFragment ChartTable => builder =>
    {
        builder.OpenElement(0, "table");
        builder.OpenElement(1, "caption");
        builder.AddContent(2, Text["ChartDataCaption"]);
        builder.CloseElement();
        builder.CloseElement();
    };

    protected override void OnInitialized()
    {
        Model = new CatalogModel { Html = Text["EditorInitialHtml"] };
        Options = [new("one", Text["OptionFirst"]), new("two", Text["OptionSecond"])];
        Appointments = [new("demo", Text["SchedulerAppointment"], new DateTimeOffset(2026, 8, 10, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero))];
        EditContext = new EditContext(Model);
    }
    private Task<IReadOnlyList<OmniOption<string>>> SearchAsync(string text, CancellationToken token) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(Options.Where(option => option.Text.Contains(text, StringComparison.OrdinalIgnoreCase)).ToArray());
    private void OpenDialog() => Overlays.OpenDialog(CreateDialogRequest(Text["DialogTitle"], FirstDialogContent));
    private void OpenNestedDialog() => Overlays.OpenDialog(CreateDialogRequest(Text["NestedTitle"], builder => builder.AddContent(0, Text["NestedBody"])));
    private OmniDialogRequest CreateDialogRequest(string title, RenderFragment content) => new(title, content)
    {
        Footer = DialogFooter
    };
    private RenderFragment DialogFooter => builder =>
    {
        builder.OpenComponent<OmniButton>(0);
        builder.AddAttribute(1, nameof(OmniButton.Variant), OmniButtonVariant.Secondary);
        builder.AddAttribute(2, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, CloseDialog));
        builder.AddAttribute(3, nameof(OmniButton.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<OmniIcon>(0);
            content.AddAttribute(1, nameof(OmniIcon.Name), OmniIconName.Close);
            content.AddAttribute(2, nameof(OmniIcon.Size), OmniControlSize.Small);
            content.CloseComponent();
            content.AddContent(3, " ");
            content.OpenElement(4, "span");
            content.AddContent(5, Text["Close"]);
            content.CloseElement();
        }));
        builder.CloseComponent();
    };
    private void CloseDialog() => Overlays.CloseDialog();
    private RenderFragment FirstDialogContent => builder =>
    {
        builder.AddContent(0, Text["DialogBody"]);
        builder.OpenComponent<OmniButton>(1);
        builder.AddAttribute(2, nameof(OmniButton.Id), "open-nested-dialog");
        builder.AddAttribute(3, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, OpenNestedDialog));
        builder.AddAttribute(4, nameof(OmniButton.ChildContent), (RenderFragment)(content => content.AddContent(0, Text["NestedOpen"])));
        builder.CloseComponent();
    };
    private void Notify() => Overlays.Notify(Text["NotificationMessage"], OmniNotificationSeverity.Success);

    public void Dispose() => Overlays.Dispose();

    private sealed record CatalogRow(int Id, string Name, int Value);
    private sealed class CatalogModel
    {
        public string Name { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Choice { get; set; } = "one";
        public string Search { get; set; } = string.Empty;
        public DateOnly? Date { get; set; } = new DateOnly(2026, 8, 10);
        public double Amount { get; set; } = 55;
        public string Color { get; set; } = "#165DFF";
        public string Html { get; set; } = string.Empty;
    }
}
