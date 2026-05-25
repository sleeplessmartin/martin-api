using MediatR;
using ProductsApi.Application.Common.Exceptions;
using ProductsApi.Domain.Interfaces;

namespace ProductsApi.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler(IProductRepository repository)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        if (!await repository.ExistsAsync(request.Id, cancellationToken))
            throw new NotFoundException(nameof(Domain.Entities.Product), request.Id);

        await repository.DeleteAsync(request.Id, cancellationToken);
    }
}
