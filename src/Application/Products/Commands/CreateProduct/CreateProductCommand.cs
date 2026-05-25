using MediatR;
using ProductsApi.Application.Products.DTOs;

namespace ProductsApi.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Currency) : IRequest<ProductDto>;
