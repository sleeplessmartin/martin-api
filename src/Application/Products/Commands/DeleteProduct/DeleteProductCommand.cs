using MediatR;

namespace ProductsApi.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest;
