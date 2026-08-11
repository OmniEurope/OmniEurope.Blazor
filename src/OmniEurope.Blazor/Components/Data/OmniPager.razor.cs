namespace OmniEurope.Blazor.Components;

public partial class OmniPager
{
    [Parameter]
    public int Page { get; set; } = 1;

    [Parameter]
    public int PageCount { get; set; } = 1;

    [Parameter]
    public EventCallback<int> PageChanged { get; set; }

    [Parameter]
    public string Label { get; set; } = string.Empty;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("PagerLabel")
        : Label;

    [Parameter]
    public bool Disabled { get; set; }

    private Task SelectAsync(int page) => Disabled || page < 1 || page > PageCount ? Task.CompletedTask : PageChanged.InvokeAsync(page);
}
