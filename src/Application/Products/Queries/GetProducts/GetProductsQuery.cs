using MediatR;
using ProductsApi.Application.Products.DTOs;

namespace ProductsApi.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery(
    int PageSize = 20,
    string? NextPageToken = null) : IRequest<PagedResult<ProductDto>>;
