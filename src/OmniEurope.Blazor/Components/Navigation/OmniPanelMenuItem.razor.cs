namespace OmniEurope.Blazor.Components;

public partial class OmniPanelMenuItem
{
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    [Parameter]
    public string? Href { get; set; }

    [Parameter]
    public bool Current { get; set; }

    [Parameter]
    public bool Expanded { get; set; }

    [Parameter]
    public Func<string, Task<bool>>? CanNavigate { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool IsActive
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Href)) return false;
            var currentUri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var targetUri = Navigation.ToAbsoluteUri(SafeHref!);
            if (targetUri.Scheme is not ("http" or "https") ||
                !string.Equals(currentUri.Scheme, targetUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(currentUri.Authority, targetUri.Authority, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var current = Uri.UnescapeDataString(currentUri.AbsolutePath).Trim('/');
            var target = Uri.UnescapeDataString(targetUri.AbsolutePath).Trim('/');
            return target.Length == 0 ? current.Length == 0 : current.Equals(target, StringComparison.OrdinalIgnoreCase) || current.StartsWith(target + '/', StringComparison.OrdinalIgnoreCase);
        }
    }

    protected override void OnInitialized() => Navigation.LocationChanged += HandleLocationChanged;
    private void HandleLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs args) => _ = InvokeAsync(StateHasChanged);

    private async Task HandleNavigateAsync()
    {
        var href = SafeHref;
        if (CanNavigate is not null && href is not null && await CanNavigate(href))
        {
            Navigation.NavigateTo(href);
        }
    }

    public void Dispose() => Navigation.LocationChanged -= HandleLocationChanged;

    private string? SafeHref => OmniUriPolicy.EnsureSafe(Href, nameof(Href));
}
