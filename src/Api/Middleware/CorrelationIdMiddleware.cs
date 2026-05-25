using Serilog.Context;

namespace ProductsApi.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[Header].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");

        context.Response.Headers[Header] = correlationId;
        context.Items["CorrelationId"] = correlationId;

        // Push into Serilog's log context so every log in this request carries it
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
