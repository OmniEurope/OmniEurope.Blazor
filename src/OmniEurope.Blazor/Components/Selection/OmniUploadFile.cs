namespace OmniEurope.Blazor.Components;

/// <summary>
/// A file an <see cref="OmniUpload"/> field holds: one already stored by the application, or one the
/// user just picked. Only what the list shows; the content itself is never kept here.
/// </summary>
public sealed record OmniUploadFile(string Name, long Size, string? ContentType = null);
