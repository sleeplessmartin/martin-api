using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ProductsApi.Domain.Entities;
using ProductsApi.Domain.ValueObjects;
using ProductsApi.Infrastructure.Persistence.DynamoDb;
using Xunit;

namespace Infrastructure.UnitTests.Persistence;

public sealed class DynamoDbProductRepositoryTests
{
    private readonly Mock<IAmazonDynamoDB> _dynamoMock = new();
    private readonly DynamoDbProductRepository _sut;

    private const string TableName = "test-products";
    private static readonly DateTime FixedUtcNow = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    public DynamoDbProductRepositoryTests()
    {
        var options = Options.Create(new DynamoDbOptions { TableName = TableName });
        _sut = new DynamoDbProductRepository(
            _dynamoMock.Object,
            options,
            NullLogger<DynamoDbProductRepository>.Instance);
    }

    [Fact]
    public async Task GetByIdAsync_ItemExists_ReturnsReconstitutedProduct()
    {
        // Arrange
        var id = Guid.NewGuid();
        _dynamoMock
            .Setup(d => d.GetItemAsync(
                It.Is<GetItemRequest>(r => r.TableName == TableName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse
            {
                IsItemSet = true,
                Item = BuildDynamoItem(id, "Widget", "Great widget", "29.99", "USD", true)
            });

        // Act
        var product = await _sut.GetByIdAsync(id);

        // Assert
        product.Should().NotBeNull();
        product!.Id.Should().Be(id);
        product.Name.Should().Be("Widget");
        product.Price.Amount.Should().Be(29.99m);
        product.Price.Currency.Should().Be("USD");
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_ItemNotFound_ReturnsNull()
    {
        // Arrange
        _dynamoMock
            .Setup(d => d.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse { IsItemSet = false });

        // Act
        var product = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        product.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_SendsCorrectPutItemRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var product = Product.Create(
            id, "Widget", "Desc",
            Money.Of(19.99m, "GBP"), FixedUtcNow);

        _dynamoMock
            .Setup(d => d.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutItemResponse());

        // Act
        await _sut.AddAsync(product);

        // Assert — verify exact table name and that PK is set correctly
        _dynamoMock.Verify(d => d.PutItemAsync(
            It.Is<PutItemRequest>(r =>
                r.TableName == TableName &&
                r.Item["PK"].S == $"PRODUCT#{id}" &&
                r.Item["Name"].S == "Widget" &&
                r.ConditionExpression == "attribute_not_exists(PK)"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_ItemPresent_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        _dynamoMock
            .Setup(d => d.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse { IsItemSet = true });

        // Act
        var exists = await _sut.ExistsAsync(id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_SendsCorrectDeleteRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        _dynamoMock
            .Setup(d => d.DeleteItemAsync(It.IsAny<DeleteItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteItemResponse());

        // Act
        await _sut.DeleteAsync(id);

        // Assert
        _dynamoMock.Verify(d => d.DeleteItemAsync(
            It.Is<DeleteItemRequest>(r =>
                r.TableName == TableName &&
                r.Key["PK"].S == $"PRODUCT#{id}"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Dictionary<string, AttributeValue> BuildDynamoItem(
        Guid id, string name, string description, string priceAmount, string currency, bool isActive) =>
        new()
        {
            ["PK"] = new AttributeValue { S = $"PRODUCT#{id}" },
            ["Id"] = new AttributeValue { S = id.ToString() },
            ["Name"] = new AttributeValue { S = name },
            ["Description"] = new AttributeValue { S = description },
            ["PriceAmount"] = new AttributeValue { N = priceAmount },
            ["PriceCurrency"] = new AttributeValue { S = currency },
            ["IsActive"] = new AttributeValue { BOOL = isActive },
            ["CreatedAt"] = new AttributeValue { S = FixedUtcNow.ToString("O") },
            ["UpdatedAt"] = new AttributeValue { S = FixedUtcNow.ToString("O") }
        };
}
