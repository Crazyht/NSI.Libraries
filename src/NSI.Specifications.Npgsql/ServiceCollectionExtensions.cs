using System;
using Microsoft.Extensions.DependencyInjection;

namespace NSI.Specifications.Npgsql;

/// <summary>
/// DI extensions to register Npgsql provider-specific specification optimizations.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Npgsql provider-specific optimizations for the specification pipeline.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection instance.</returns>
    public static IServiceCollection AddNpgsqlSpecifications(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Optimization.Npgsql.NpgsqlTextOptimizations.RegisterAll();
        return services;
    }
}
