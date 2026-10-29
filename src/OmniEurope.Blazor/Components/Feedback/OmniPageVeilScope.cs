namespace OmniEurope.Blazor.Components;

/// <summary>How much of the window an <see cref="OmniPageVeil"/> covers.</summary>
public enum OmniPageVeilScope
{
    /// <summary>
    /// The content of the page only: inside an <see cref="OmniMain"/> the veil covers that main area, under the
    /// header and beside the sidebar, which stay visible and usable. Outside any main area there is no content to
    /// cover and the veil covers the window, as <see cref="Viewport"/>. The default.
    /// </summary>
    Content,

    /// <summary>The whole window, the header and the sidebars included, wherever the veil is declared.</summary>
    Viewport
}
