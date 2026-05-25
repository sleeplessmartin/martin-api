using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductsApi.Application.Products.Commands.CreateProduct;
using ProductsApi.Application.Products.Commands.DeleteProduct;
using ProductsApi.Application.Products.Commands.UpdateProduct;
using ProductsApi.Application.Products.DTOs;
using ProductsApi.Application.Products.Queries.GetProduct;
using ProductsApi.Application.Products.Queries.GetProducts;

namespace ProductsApi.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/products")
            .WithTags("Products");

        group.MapGet("/", GetProductsAsync)
            .WithName("GetProducts")
            .WithSummary("List products (paginated)")
            .Produces<PagedResult<ProductDto>>()
            .RequireAuthorization("ProductsRead");

        group.MapGet("/{id:guid}", GetProductAsync)
            .WithName("GetProduct")
            .WithSummary("Get a product by ID")
            .Produces<ProductDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("ProductsRead");

        group.MapPost("/", CreateProductAsync)
            .WithName("CreateProduct")
            .WithSummary("Create a new product")
            .Produces<ProductDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization("ProductsWrite");

        group.MapPut("/{id:guid}", UpdateProductAsync)
            .WithName("UpdateProduct")
            .WithSummary("Update an existing product")
            .Produces<ProductDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization("ProductsWrite");

        group.MapDelete("/{id:guid}", DeleteProductAsync)
            .WithName("DeleteProduct")
            .WithSummary("Delete a product")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("ProductsWrite");

        return routes;
    }

    private static async Task<IResult> GetProductsAsync(
        [FromQuery] int pageSize,
        [FromQuery] string? nextPageToken,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new GetProductsQuery(pageSize == 0 ? 20 : pageSize, nextPageToken), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetProductAsync(
        Guid id, ISender sender, CancellationToken ct)
    {
        var product = await sender.Send(new GetProductQuery(id), ct);
        return Results.Ok(product);
    }

    private static async Task<IResult> CreateProductAsync(
        CreateProductRequest req, ISender sender, HttpContext ctx, CancellationToken ct)
    {
        var command = new CreateProductCommand(req.Name, req.Description, req.Price, req.Currency);
        var product = await sender.Send(command, ct);
        return Results.CreatedAtRoute("GetProduct", new { id = product.Id }, product);
    }

    private static async Task<IResult> UpdateProductAsync(
        Guid id, UpdateProductRequest req, ISender sender, CancellationToken ct)
    {
        var command = new UpdateProductCommand(id, req.Name, req.Description, req.Price, req.Currency);
        var product = await sender.Send(command, ct);
        return Results.Ok(product);
    }

    private static async Task<IResult> DeleteProductAsync(
        Guid id, ISender sender, CancellationToken ct)
    {
        await sender.Send(new DeleteProductCommand(id), ct);
        return Results.NoContent();
    }
}

// Explicit request models keep the API surface decoupled from Application commands
public sealed record CreateProductRequest(string Name, string Description, decimal Price, string Currency);
public sealed record UpdateProductRequest(string Name, string Description, decimal Price, string Currency);
