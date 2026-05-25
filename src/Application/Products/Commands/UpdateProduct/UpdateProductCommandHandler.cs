using MediatR;
using ProductsApi.Application.Common.Exceptions;
using ProductsApi.Application.Common.Interfaces;
using ProductsApi.Application.Products.DTOs;
using ProductsApi.Domain.Interfaces;
using ProductsApi.Domain.ValueObjects;

namespace ProductsApi.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IProductRepository repository,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Product), request.Id);

        product.Update(
            request.Name,
            request.Description,
            Money.Of(request.Price, request.Currency),
            dateTimeProvider.UtcNow);

        await repository.UpdateAsync(product, cancellationToken);

        return new ProductDto(product.Id, product.Name, product.Description,
            product.Price.Amount, product.Price.Currency,
            product.IsActive, product.CreatedAt, product.UpdatedAt);
    }
}
