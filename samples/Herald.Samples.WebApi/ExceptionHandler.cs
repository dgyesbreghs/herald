using Herald.Samples.WebApi.Behaviors;
using Microsoft.AspNetCore.Diagnostics;

namespace Herald.Samples.WebApi;

/// <summary>Maps validation and not-found exceptions to problem details responses.</summary>
public sealed class ExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError,
        };

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Detail = exception.Message },
        });
    }
}
