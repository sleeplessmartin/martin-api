namespace ProductsApi.Application.Common.Exceptions;

public sealed class NotFoundException(string name, object key)
    : Exception($"Entity '{name}' with key '{key}' was not found.");
