using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Catalog.Resources;

namespace OmniEurope.Blazor.Catalog.Components.Pages;

public partial class NotFound
{
    [Inject]
    private IStringLocalizer<CatalogStrings> Text { get; set; } = default!;

    [Parameter]
    public string? Path { get; set; }
}
