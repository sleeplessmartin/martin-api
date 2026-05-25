using ProductsApi.Application.Common.Interfaces;

namespace ProductsApi.Application.Common.Services;

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
