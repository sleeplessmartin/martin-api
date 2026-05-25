using MediatR;
using ProductsApi.Application.Products.DTOs;

namespace ProductsApi.Application.Products.Queries.GetProduct;

public sealed record GetProductQuery(Guid Id) : IRequest<ProductDto>;
