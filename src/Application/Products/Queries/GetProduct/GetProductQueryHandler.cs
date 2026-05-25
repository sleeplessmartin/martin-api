using MediatR;
using ProductsApi.Application.Common.Exceptions;
using ProductsApi.Application.Products.DTOs;
using ProductsApi.Domain.Interfaces;

namespace ProductsApi.Application.Products.Queries.GetProduct;

public sealed class GetProductQueryHandler(IProductRepository repository)
    : IRequestHandler<GetProductQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Product), request.Id);

        return new ProductDto(product.Id, product.Name, product.Description,
            product.Price.Amount, product.Price.Currency,
            product.IsActive, product.CreatedAt, product.UpdatedAt);
    }
}
