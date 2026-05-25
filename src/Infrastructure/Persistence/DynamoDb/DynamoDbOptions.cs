namespace ProductsApi.Infrastructure.Persistence.DynamoDb;

public sealed class DynamoDbOptions
{
    public const string SectionName = "DynamoDb";

    public string TableName { get; set; } = "products";
    public string? ServiceUrl { get; set; } // For local DynamoDB Local override
}
