namespace OmniEurope.Blazor.Components;

/// <summary>Where the entity of a detail page stands, as <see cref="OmniDetailShell"/> shows it.</summary>
public enum OmniDetailState
{
    /// <summary>Not loaded yet: the header is painted, the content waits hidden.</summary>
    Loading,

    /// <summary>Loaded: header and content.</summary>
    Found,

    /// <summary>The entity does not exist or is out of reach: a not-found state with a way back.</summary>
    NotFound
}
