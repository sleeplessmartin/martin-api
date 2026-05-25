namespace ProductsApi.Application.Products.DTOs;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    string? NextPageToken,
    int Count);
