using MediatR;
using ProductsApi.Application.Products.DTOs;
using ProductsApi.Domain.Interfaces;

namespace ProductsApi.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IProductRepository repository)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var (products, nextToken) = await repository.GetAllAsync(pageSize, request.NextPageToken, cancellationToken);

        var dtos = products
            .Select(p => new ProductDto(p.Id, p.Name, p.Description,
                p.Price.Amount, p.Price.Currency,
                p.IsActive, p.CreatedAt, p.UpdatedAt))
            .ToList()
            .AsReadOnly();

        return new PagedResult<ProductDto>(dtos, nextToken, dtos.Count);
    }
}
