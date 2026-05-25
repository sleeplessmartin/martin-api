using ProductsApi.Domain.Common;

namespace ProductsApi.Domain.Events;

public sealed record ProductUpdatedEvent(Guid ProductId, string Name, DateTime OccurredAt) : IDomainEvent;
