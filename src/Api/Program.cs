using Amazon.Lambda.AspNetCoreServer.Hosting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProductsApi.Api.Endpoints;
using ProductsApi.Api.Middleware;
using ProductsApi.Application;
using ProductsApi.Infrastructure;
using Serilog;
using Serilog.Formatting.Json;

// Bootstrap logger captures any startup errors before full configuration loads
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Structured Logging ────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "ProductsApi")
        .WriteTo.Console(new JsonFormatter()));

    // ── Lambda ───────────────────────────────────────────────────────────────
    // Detects AWS_LAMBDA_FUNCTION_NAME env var; no-ops when running locally
    builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

    // ── API Versioning ────────────────────────────────────────────────────────
    builder.Services.AddApiVersioning(o =>
    {
        o.DefaultApiVersion = new ApiVersion(1);
        o.AssumeDefaultVersionWhenUnspecified = true;
        o.ReportApiVersions = true;
        o.ApiVersionReader = new UrlSegmentApiVersionReader();
    });

    // ── Application + Infrastructure ─────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── Authentication / Authorization ────────────────────────────────────────
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.Authority = builder.Configuration["Auth:Authority"];
            o.Audience = builder.Configuration["Auth:Audience"];
            // In Lambda behind API GW, the request path is already https
            o.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        });

    builder.Services.AddAuthorization(o =>
    {
        o.AddPolicy("ProductsRead", p =>
            p.RequireAuthenticatedUser());

        o.AddPolicy("ProductsWrite", p =>
            p.RequireAuthenticatedUser()
             .RequireClaim("scope", "products:write"));
    });

    // ── Observability ─────────────────────────────────────────────────────────
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService(
            serviceName: "ProductsApi",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0"))
        .WithTracing(t =>
        {
            t.AddAspNetCoreInstrumentation()
             .AddSource("ProductsApi.*");
            if (builder.Environment.IsDevelopment())
                t.AddConsoleExporter();
        })
        .WithMetrics(m =>
        {
            m.AddAspNetCoreInstrumentation();
            if (builder.Environment.IsDevelopment())
                m.AddConsoleExporter();
        });

    // ── Health Checks ─────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    // ── Problem Details + Exception Handling ─────────────────────────────────
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    var app = builder.Build();

    // ── Middleware Pipeline ───────────────────────────────────────────────────
    app.UseExceptionHandler();
    app.UseSerilogRequestLogging(o =>
    {
        o.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        o.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set("RequestHost", ctx.Request.Host.Value);
            diag.Set("RequestScheme", ctx.Request.Scheme);
            if (ctx.Items.TryGetValue("CorrelationId", out var id))
                diag.Set("CorrelationId", id);
        };
    });

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();

    // ── Endpoints ─────────────────────────────────────────────────────────────
    app.MapHealthEndpoints();

    var versionSet = app.NewApiVersionSet()
        .HasApiVersion(new ApiVersion(1))
        .ReportApiVersions()
        .Build();

    var v1 = app.MapGroup("/api/v{version:apiVersion}")
        .WithApiVersionSet(versionSet)
        .MapToApiVersion(new ApiVersion(1));

    v1.MapProductEndpoints();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Exposed for WebApplicationFactory in integration tests
public partial class Program { }
