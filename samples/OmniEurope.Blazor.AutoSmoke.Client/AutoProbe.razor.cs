using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.AutoSmoke.Client.Resources;

namespace OmniEurope.Blazor.AutoSmoke.Client;

public partial class AutoProbe
{
    [Inject]
    private IStringLocalizer<AutoSmokeStrings> Text { get; set; } = default!;

    private int Count { get; set; }

    private Task IncrementAsync()
    {
        Count++;
        return Task.CompletedTask;
    }
}
