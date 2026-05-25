using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ProductsApi.Application.Common.Exceptions;
using ProductsApi.Application.Products.DTOs;
using Xunit;

namespace Api.UnitTests.Endpoints;

// Integration-style test using WebApplicationFactory — exercises the full HTTP pipeline
// (middleware, routing, model binding, exception handling) without a real network.
public sealed class ProductEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IMediator> _mediatorMock = new();

    private static readonly DateTime FixedUtcNow = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    public ProductEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace MediatR with a mock so we control responses without a real DB
                services.AddSingleton(_mediatorMock.Object);
            });
        });
    }

    [Fact]
    public async Task GetProduct_ExistingId_Returns200WithBody()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new ProductDto(id, "Widget", "Great widget", 29.99m, "USD", true, FixedUtcNow, FixedUtcNow);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<ProductDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var client = CreateAnonymousClient(); // auth disabled in test factory

        // Act
        var response = await client.GetAsync($"/api/v1/products/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProductDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(id);
        body.Name.Should().Be("Widget");
    }

    [Fact]
    public async Task GetProduct_NonExistingId_Returns404WithProblemDetails()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<ProductDto>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Product", id));

        var client = CreateAnonymousClient();

        // Act
        var response = await client.GetAsync($"/api/v1/products/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        body!.Title.Should().Be("Resource Not Found");
    }

    [Fact]
    public async Task CreateProduct_ValidPayload_Returns201WithLocationHeader()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new ProductDto(id, "New Widget", "Desc", 49.99m, "EUR", true, FixedUtcNow, FixedUtcNow);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<ProductDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var client = CreateAnonymousClient();
        var payload = new { Name = "New Widget", Description = "Desc", Price = 49.99m, Currency = "EUR" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/products", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(id.ToString());
    }

    [Fact]
    public async Task HealthLive_AlwaysReturns200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProduct_CorrelationIdFromRequest_EchoedInResponse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new ProductDto(id, "W", "D", 1m, "USD", true, FixedUtcNow, FixedUtcNow);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<ProductDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var client = CreateAnonymousClient();
        const string correlationId = "test-correlation-xyz";
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

        // Act
        var response = await client.GetAsync($"/api/v1/products/{id}");

        // Assert
        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        values!.First().Should().Be(correlationId);
    }

    // Creates a client with auth disabled — avoids needing a real JWT in unit tests
    private HttpClient CreateAnonymousClient() =>
        _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(s =>
            {
                s.AddAuthentication("Test")
                 .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
                     TestAuthHandler>("Test", _ => { });
                s.AddAuthorization(o =>
                {
                    o.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Test")
                        .RequireAuthenticatedUser()
                        .Build();
                    o.AddPolicy("ProductsRead", p => p.RequireAuthenticatedUser());
                    o.AddPolicy("ProductsWrite", p => p.RequireAuthenticatedUser());
                });
                s.AddSingleton(_mediatorMock.Object);
            });
        }).CreateClient();
}

// Minimal test auth handler that marks every request as authenticated
internal sealed class TestAuthHandler(
    Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
    Microsoft.Extensions.Logging.ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder)
    : Microsoft.AspNetCore.Authentication.AuthenticationHandler<
        Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "testuser") };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");
        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
    }
}
