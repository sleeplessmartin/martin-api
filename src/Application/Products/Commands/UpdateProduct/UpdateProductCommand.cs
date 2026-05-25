using MediatR;
using ProductsApi.Application.Products.DTOs;

namespace ProductsApi.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency) : IRequest<ProductDto>;
