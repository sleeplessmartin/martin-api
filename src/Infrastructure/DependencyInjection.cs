using Amazon;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductsApi.Domain.Interfaces;
using ProductsApi.Infrastructure.Persistence.DynamoDb;
using ProductsApi.Infrastructure.Persistence.InMemory;

namespace ProductsApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase");

        if (useInMemory)
        {
            // Local development: no AWS needed
            services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        }
        else
        {
            services.Configure<DynamoDbOptions>(configuration.GetSection(DynamoDbOptions.SectionName));

            var dynamoConfig = new AmazonDynamoDBConfig
            {
                // Allow local DynamoDB override (DynamoDB Local for integration tests)
                ServiceURL = configuration["DynamoDb:ServiceUrl"],
                RetryMode = Amazon.Runtime.RequestRetryMode.Standard,
                MaxErrorRetry = 3
            };

            if (dynamoConfig.ServiceURL is null)
                dynamoConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(
                    configuration["AWS:Region"] ?? "us-east-1");

            services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient(dynamoConfig));
            services.AddSingleton<IProductRepository, DynamoDbProductRepository>();
        }

        return services;
    }
}
