using ProductsApi.Domain.Common;

namespace ProductsApi.Domain.Events;

public sealed record ProductCreatedEvent(Guid ProductId, string Name, DateTime OccurredAt) : IDomainEvent;
