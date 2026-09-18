namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class PagesDemo : IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();

    [Inject] private OmniBreadcrumbService Breadcrumb { get; set; } = null!;

    private OmniDetailState State { get; set; }

    private bool TokenShown { get; set; }

    private DateTimeOffset Heartbeat { get; } = TimeProvider.System.GetUtcNow().AddMinutes(-2);

    private DateTimeOffset CertificateExpiry { get; } = TimeProvider.System.GetUtcNow().AddDays(47);

    protected override Task OnInitializedAsync() => LoadAsync();

    /// <summary>Le fil est posé tout de suite, le nom de la fiche arrive après un chargement simulé.</summary>
    private async Task LoadAsync()
    {
        State = OmniDetailState.Loading;
        Breadcrumb.Set(
            new OmniBreadcrumbEntry("Composants", "composants"),
            new OmniBreadcrumbEntry("Serveurs", "composants/pages"),
            new OmniBreadcrumbEntry("Serveur", Loading: true));
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5), _lifetime.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        Breadcrumb.Replace(2, new OmniBreadcrumbEntry("srv-paris-01"));
        State = OmniDetailState.Found;
    }

    private void ShowMissing() => State = OmniDetailState.NotFound;

    public void Dispose()
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
