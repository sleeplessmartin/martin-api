using MediatR;
using ProductsApi.Application.Common.Interfaces;
using ProductsApi.Application.Products.DTOs;
using ProductsApi.Domain.Entities;
using ProductsApi.Domain.Interfaces;
using ProductsApi.Domain.ValueObjects;

namespace ProductsApi.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(
    IProductRepository repository,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(
            Guid.NewGuid(),
            request.Name,
            request.Description,
            Money.Of(request.Price, request.Currency),
            dateTimeProvider.UtcNow);

        await repository.AddAsync(product, cancellationToken);

        return MapToDto(product);
    }

    private static ProductDto MapToDto(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price.Amount, p.Price.Currency,
            p.IsActive, p.CreatedAt, p.UpdatedAt);
}
