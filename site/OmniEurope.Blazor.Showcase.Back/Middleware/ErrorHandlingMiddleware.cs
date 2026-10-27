using Microsoft.AspNetCore.Mvc;
using OmniEurope.Blazor.Showcase.Back.Exceptions;

namespace OmniEurope.Blazor.Showcase.Back.Middleware;

/// <summary>
/// Turns every exception into an RFC 9457 ProblemDetails. The detail is the message of an exception
/// the API raises on purpose (<see cref="Exceptions"/>), and a fixed sentence for anything else: no
/// stack trace, exception type or inner message ever reaches a client, in any environment.
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IProblemDetailsService problems)
{
    internal const string UnexpectedDetail = "An unexpected error occurred.";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away: there is nobody to answer.
            logger.LogDebug("The client aborted {Method} {Path}.", context.Request.Method, context.Request.Path);
        }
        catch (Exception exception)
        {
            await WriteProblemAsync(context, exception).ConfigureAwait(false);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            QuotaExceededException => (StatusCodes.Status422UnprocessableEntity, "Quota exceeded", exception.Message),
            TooManyRequestsException => (StatusCodes.Status429TooManyRequests, "Too Many Requests", exception.Message),
            CapacityReachedException => (StatusCodes.Status503ServiceUnavailable, "Capacity reached", exception.Message),
            // Kestrel's own refusals (a body over the size limit, a malformed request) carry their status.
            BadHttpRequestException badRequest => (badRequest.StatusCode, "Bad Request", "The request was refused by the server."),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", UnexpectedDetail),
        };

        if (status >= StatusCodes.Status500InternalServerError && exception is not CapacityReachedException)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}.", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning("{Method} {Path} refused with {Status}: {Detail}", context.Request.Method, context.Request.Path, status, detail);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = null,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
        }).ConfigureAwait(false);
    }
}
