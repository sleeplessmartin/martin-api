using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProductsApi.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
    {
        // Liveness: is the process up?
        routes.MapHealthChecks("/health/live", new()
        {
            Predicate = _ => false, // only built-in liveness
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        }).WithTags("Health");

        // Readiness: can the process serve traffic? (checks all registered checks)
        routes.MapHealthChecks("/health/ready", new()
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        }).WithTags("Health");

        // Convenience alias used by many platform probes
        routes.MapHealthChecks("/health").WithTags("Health");

        return routes;
    }
}
