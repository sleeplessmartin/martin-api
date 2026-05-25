using FluentAssertions;
using Moq;
using ProductsApi.Application.Common.Exceptions;
using ProductsApi.Application.Common.Interfaces;
using ProductsApi.Application.Products.Commands.CreateProduct;
using ProductsApi.Domain.Entities;
using ProductsApi.Domain.Interfaces;
using Xunit;

namespace Application.UnitTests.Products.Commands;

public sealed class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeMock = new();
    private readonly CreateProductCommandHandler _sut;

    // Fixed point-in-time — tests must never use DateTime.Now/UtcNow directly
    private static readonly DateTime FixedUtcNow = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    public CreateProductCommandHandlerTests()
    {
        _dateTimeMock.Setup(d => d.UtcNow).Returns(FixedUtcNow);
        _sut = new CreateProductCommandHandler(_repositoryMock.Object, _dateTimeMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsProductDtoWithCorrectFields()
    {
        // Arrange
        var command = new CreateProductCommand("Widget Pro", "A great widget", 29.99m, "USD");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Widget Pro");
        result.Description.Should().Be("A great widget");
        result.Price.Should().Be(29.99m);
        result.Currency.Should().Be("USD");
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().Be(FixedUtcNow);
        result.UpdatedAt.Should().Be(FixedUtcNow);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsRepositoryAddAsyncOnce()
    {
        // Arrange
        var command = new CreateProductCommand("Widget", "Desc", 10m, "EUR");

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            r => r.AddAsync(It.Is<Product>(p => p.Name == "Widget"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithWhitespaceName_ThrowsAfterTrimming()
    {
        // The domain entity trims, so the command should arrive pre-validated by FluentValidation.
        // This test verifies the domain guard catches an empty string directly.
        var command = new CreateProductCommand("   ", "Description", 10m, "USD");

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NegativePrice_ThrowsDomainException()
    {
        var command = new CreateProductCommand("Widget", "Desc", -1m, "USD");

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PropagatesRepositoryException()
    {
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DynamoDB unavailable"));

        var command = new CreateProductCommand("Widget", "Desc", 10m, "USD");

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DynamoDB unavailable");
    }
}
