namespace OmniEurope.Blazor.Showcase.Back.Exceptions;

// The failures the API reports on purpose. ErrorHandlingMiddleware maps each one to its status code
// and writes its message as the ProblemDetails detail, so a message here is always meant for a client:
// never a value the client did not send, never an internal detail.

/// <summary>The request is malformed (400).</summary>
public sealed class BadRequestException(string message) : Exception(message);

/// <summary>The visitor's own quota is reached (422): too many keys or too much stored text.</summary>
public sealed class QuotaExceededException(string message) : Exception(message);

/// <summary>The client address created too many visitors in the current window (429).</summary>
public sealed class TooManyRequestsException(string message) : Exception(message);

/// <summary>The API holds as many visitors as it is configured for (503): no new visitor is accepted.</summary>
public sealed class CapacityReachedException(string message) : Exception(message);

/// <summary>The same rows kept changing under the request, even after its retries (409).</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Another request changed the same rows first; nothing was saved. Internal: the service retries.</summary>
public sealed class ConcurrentWriteException(Exception innerException)
    : Exception("Another request changed the same rows first.", innerException);
