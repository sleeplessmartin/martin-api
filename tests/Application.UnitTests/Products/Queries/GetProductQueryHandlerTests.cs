using FluentAssertions;
using Moq;
using ProductsApi.Application.Common.Exceptions;
using ProductsApi.Application.Products.Queries.GetProduct;
using ProductsApi.Domain.Entities;
using ProductsApi.Domain.Interfaces;
using ProductsApi.Domain.ValueObjects;
using Xunit;

namespace Application.UnitTests.Products.Queries;

public sealed class GetProductQueryHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly GetProductQueryHandler _sut;

    private static readonly DateTime FixedUtcNow = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    public GetProductQueryHandlerTests()
    {
        _sut = new GetProductQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingId_ReturnsMappedDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var product = Product.Reconstitute(id, "Widget", "Best widget", Money.Of(99.99m, "USD"),
            true, FixedUtcNow, FixedUtcNow);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _sut.Handle(new GetProductQuery(id), CancellationToken.None);

        // Assert
        result.Id.Should().Be(id);
        result.Name.Should().Be("Widget");
        result.Price.Should().Be(99.99m);
        result.Currency.Should().Be("USD");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var act = async () => await _sut.Handle(new GetProductQuery(id), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{id}*");
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var product = Product.Reconstitute(id, "Widget", "Desc", Money.Of(10m, "GBP"),
            true, FixedUtcNow, FixedUtcNow);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        await _sut.Handle(new GetProductQuery(id), CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
