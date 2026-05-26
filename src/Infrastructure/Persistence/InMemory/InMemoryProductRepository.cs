using System.Collections.Concurrent;
using ProductsApi.Domain.Entities;
using ProductsApi.Domain.Interfaces;

namespace ProductsApi.Infrastructure.Persistence.InMemory;

// Thread-safe in-memory store for local development and testing
public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _store = new();

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public Task<(IReadOnlyList<Product> Items, string? NextPageToken)> GetAllAsync(
        int pageSize, string? nextPageToken, CancellationToken cancellationToken = default)
    {
        var skip = 0;
        if (!string.IsNullOrEmpty(nextPageToken) && int.TryParse(nextPageToken, out var parsed))
        {
            skip = parsed;
        }

        var all = _store.Values.OrderBy(p => p.CreatedAt).ToList();
        var page = all.Skip(skip).Take(pageSize).ToList().AsReadOnly();
        var next = skip + pageSize < all.Count ? (skip + pageSize).ToString() : null;

        return Task.FromResult<(IReadOnlyList<Product>, string?)>((page, next));
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _store[product.Id] = product;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _store[product.Id] = product;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.ContainsKey(id));
}
