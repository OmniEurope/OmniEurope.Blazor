namespace OmniEurope.Blazor.Showcase.Components.Gallery;

/// <summary>The settings and the preview of a gallery page, side by side or stacked.</summary>
public partial class GalleryPageBody
{
    /// <summary>The settings, on the left on a wide screen.</summary>
    [Parameter]
    public RenderFragment? Settings { get; set; }

    /// <summary>What the settings change, on the right on a wide screen.</summary>
    [Parameter]
    public RenderFragment? Preview { get; set; }

    /// <summary>Whether the preview is drawn in the screen of a standard phone (360 x 780 CSS pixels).</summary>
    [Parameter]
    public bool Phone { get; set; }

    private string BodyClass => Settings is null ? "gallery-page__body gallery-page__body--single" : "gallery-page__body";
}
