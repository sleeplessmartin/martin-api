using ProductsApi.Domain.Common;
using ProductsApi.Domain.Events;
using ProductsApi.Domain.ValueObjects;

namespace ProductsApi.Domain.Entities;

public sealed class Product : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Product() { }

    public static Product Create(Guid id, string name, string description, Money price, DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, 200, nameof(name));

        var product = new Product
        {
            Id = id,
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Price = price,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        product.RaiseDomainEvent(new ProductCreatedEvent(product.Id, product.Name, now));
        return product;
    }

    // Reconstitute from persistence without triggering domain events
    public static Product Reconstitute(
        Guid id, string name, string description, Money price,
        bool isActive, DateTime createdAt, DateTime updatedAt) =>
        new()
        {
            Id = id,
            Name = name,
            Description = description,
            Price = price,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

    public void Update(string name, string description, Money price, DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        UpdatedAt = now;

        RaiseDomainEvent(new ProductUpdatedEvent(Id, Name, now));
    }

    public void Deactivate(DateTime now)
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = now;
    }
}
