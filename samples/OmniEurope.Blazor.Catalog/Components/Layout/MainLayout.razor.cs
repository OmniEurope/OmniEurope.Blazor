using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Catalog.Resources;

namespace OmniEurope.Blazor.Catalog.Components.Layout;

public partial class MainLayout
{
    [Inject]
    private IStringLocalizer<CatalogStrings> Text { get; set; } = default!;

    private ErrorBoundary? _errorBoundary;

    private Task RecoverAsync()
    {
        _errorBoundary?.Recover();
        return Task.CompletedTask;
    }
}
