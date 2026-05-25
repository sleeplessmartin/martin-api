using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductsApi.Domain.Entities;
using ProductsApi.Domain.Interfaces;
using ProductsApi.Domain.ValueObjects;
using System.Text.Json;

namespace ProductsApi.Infrastructure.Persistence.DynamoDb;

public sealed class DynamoDbProductRepository(
    IAmazonDynamoDB dynamoDb,
    IOptions<DynamoDbOptions> options,
    ILogger<DynamoDbProductRepository> logger)
    : IProductRepository
{
    private readonly string _tableName = options.Value.TableName;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching product {ProductId} from DynamoDB", id);

        var response = await dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = PrimaryKey(id),
            ConsistentRead = true
        }, cancellationToken);

        return response.IsItemSet ? MapToProduct(response.Item) : null;
    }

    public async Task<(IReadOnlyList<Product> Items, string? NextPageToken)> GetAllAsync(
        int pageSize, string? nextPageToken, CancellationToken cancellationToken = default)
    {
        var request = new ScanRequest
        {
            TableName = _tableName,
            Limit = pageSize,
            FilterExpression = "IsActive = :active",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":active"] = new() { BOOL = true }
            }
        };

        if (!string.IsNullOrEmpty(nextPageToken))
            request.ExclusiveStartKey = DecodePageToken(nextPageToken);

        var response = await dynamoDb.ScanAsync(request, cancellationToken);
        var products = response.Items.Select(MapToProduct).ToList().AsReadOnly();
        var token = response.LastEvaluatedKey.Count > 0 ? EncodePageToken(response.LastEvaluatedKey) : null;

        return (products, token);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Adding product {ProductId} to DynamoDB", product.Id);

        await dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = MapToAttributes(product),
            ConditionExpression = "attribute_not_exists(PK)" // Optimistic insert guard
        }, cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Updating product {ProductId} in DynamoDB", product.Id);

        await dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = MapToAttributes(product),
            ConditionExpression = "attribute_exists(PK)"
        }, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Deleting product {ProductId} from DynamoDB", id);

        await dynamoDb.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = _tableName,
            Key = PrimaryKey(id)
        }, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = PrimaryKey(id),
            ProjectionExpression = "PK",
            ConsistentRead = true
        }, cancellationToken);

        return response.IsItemSet;
    }

    private static Dictionary<string, AttributeValue> PrimaryKey(Guid id) =>
        new() { ["PK"] = new AttributeValue { S = $"PRODUCT#{id}" } };

    private static Dictionary<string, AttributeValue> MapToAttributes(Product p) =>
        new()
        {
            ["PK"] = new AttributeValue { S = $"PRODUCT#{p.Id}" },
            ["Id"] = new AttributeValue { S = p.Id.ToString() },
            ["Name"] = new AttributeValue { S = p.Name },
            ["Description"] = new AttributeValue { S = p.Description },
            ["PriceAmount"] = new AttributeValue { N = p.Price.Amount.ToString("F4") },
            ["PriceCurrency"] = new AttributeValue { S = p.Price.Currency },
            ["IsActive"] = new AttributeValue { BOOL = p.IsActive },
            ["CreatedAt"] = new AttributeValue { S = p.CreatedAt.ToString("O") },
            ["UpdatedAt"] = new AttributeValue { S = p.UpdatedAt.ToString("O") }
        };

    private static Product MapToProduct(Dictionary<string, AttributeValue> item) =>
        Product.Reconstitute(
            id: Guid.Parse(item["Id"].S),
            name: item["Name"].S,
            description: item["Description"].S,
            price: Money.Of(decimal.Parse(item["PriceAmount"].N), item["PriceCurrency"].S),
            isActive: item["IsActive"].BOOL,
            createdAt: DateTime.Parse(item["CreatedAt"].S).ToUniversalTime(),
            updatedAt: DateTime.Parse(item["UpdatedAt"].S).ToUniversalTime());

    private static string EncodePageToken(Dictionary<string, AttributeValue> key)
    {
        var simple = key.ToDictionary(k => k.Key, k => k.Value.S ?? k.Value.N);
        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(simple));
    }

    private static Dictionary<string, AttributeValue> DecodePageToken(string token)
    {
        var bytes = Convert.FromBase64String(token);
        var simple = JsonSerializer.Deserialize<Dictionary<string, string>>(bytes)!;
        return simple.ToDictionary(k => k.Key, k => new AttributeValue { S = k.Value });
    }
}
