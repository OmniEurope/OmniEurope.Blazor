namespace OmniEurope.Blazor.Components;

/// <summary>How wide the content of <see cref="OmniMain"/> may grow.</summary>
public enum OmniLayoutWidth
{
    /// <summary>The whole width of the main area. The default.</summary>
    Full,

    /// <summary>Capped at 90rem (the <c>--omni-layout-wide-width</c> token) and centred.</summary>
    Wide,

    /// <summary>Capped at 72rem (the <c>--omni-layout-content-width</c> token) and centred, a comfortable reading width.</summary>
    Content
}
