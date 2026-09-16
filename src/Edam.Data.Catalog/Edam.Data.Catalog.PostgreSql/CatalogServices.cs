using Edam.Data.Catalog.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Edam.Data.Catalog.PostgreSql;

/// <summary>
/// DI registration for the catalog surface (BL-7.2/BL-7.4), consistent with <c>AddWave1Services()</c>.
/// Consumers resolve <see cref="ICatalogStore"/> / <see cref="IContentStore"/> by contract and never
/// reference the PostgreSQL provider or its connection (the back-end is a variable, ADR-0007).
/// </summary>
public static class CatalogServices
{
    /// <summary>
    /// Register the PostgreSQL provider behind the catalog store/content seams.
    /// </summary>
    /// <param name="services">service collection</param>
    /// <param name="config">configuration; catalog connection from <c>ConnectionStrings:catalog</c>
    /// or <c>Edam:Catalog:ConnectionString</c></param>
    public static IServiceCollection AddCatalogServices(this IServiceCollection services, IConfiguration config)
    {
        var connection = config["ConnectionStrings:catalog"]
                         ?? config["Edam:Catalog:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connection))
        {
            // No relational back-end configured → no provider is registered (callers stay up;
            // DI reports the missing service on resolve). Consistent with the Wave-1 Postgres store.
            return services;
        }

        return services
            .AddSingleton<PostgreSqlCatalogStore>(sp => new PostgreSqlCatalogStore(connection))
            .AddSingleton<PostgreSqlContentStore>(sp => new PostgreSqlContentStore(connection))
            .AddSingleton<ICatalogStore>(sp => sp.GetRequiredService<PostgreSqlCatalogStore>())
            .AddSingleton<IContentStore>(sp => sp.GetRequiredService<PostgreSqlContentStore>())
            // BL-7.4 seed: Container -> provider resolution (PostgreSql target maps to the
            // store above; other targets resolve null). Callers depend on the contract only.
            .AddSingleton<ICatalogProviderResolver<ICatalogStore>, CatalogProviderResolver>()
            .AddSingleton<ICatalogProviderResolver<IContentStore>, CatalogProviderResolver>();
    }
}
