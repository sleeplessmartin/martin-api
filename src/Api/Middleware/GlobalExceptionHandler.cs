using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProductsApi.Application.Common.Exceptions;
using ValidationException = ProductsApi.Application.Common.Exceptions.ValidationException;

namespace ProductsApi.Api.Middleware;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
            ValidationException => (StatusCodes.Status422UnprocessableEntity, "Validation Failed"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "Client Closed Request"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
        else
            logger.LogWarning(exception, "{ExceptionType} on {Method} {Path}",
                exception.GetType().Name, context.Request.Method, context.Request.Path);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path,
        };

        problem.Extensions["requestId"] = context.TraceIdentifier;

        if (context.Items.TryGetValue("CorrelationId", out var correlationId))
            problem.Extensions["correlationId"] = correlationId;

        if (exception is ValidationException ve)
            problem.Extensions["errors"] = ve.Errors;

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
