namespace OmniEurope.Blazor.Showcase.Back.Models;

/// <summary>An anonymous visitor of the showcase that saved at least one preference.</summary>
public sealed class Visitor
{
    /// <summary>
    /// The SHA-256 of the visitor id, in lower-case hexadecimal. The id itself is the visitor's only
    /// credential, so the database never holds it: a copy of the table gives no access to the API.
    /// </summary>
    public required string Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>The last read or write; the retention removes the visitors idle for too long.</summary>
    public DateTimeOffset LastSeenAt { get; set; }

    /// <summary>Raised on every write: two writes of the same visitor at once cannot both pass the quotas.</summary>
    public int Revision { get; set; }

    public List<VisitorPreference> Preferences { get; } = [];
}
