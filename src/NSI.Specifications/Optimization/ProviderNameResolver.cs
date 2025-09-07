using System;
using System.Linq;

namespace NSI.Specifications.Optimization;

/// <summary>
/// Resolves a short provider code from an IQueryable's provider type name.
/// </summary>
public static class ProviderNameResolver
{
    /// <summary>
    /// Resolve provider code from an <see cref="IQueryable"/> instance.
    /// </summary>
    public static string Resolve<T>(IQueryable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var providerTypeName = source.Provider?.GetType().FullName ?? string.Empty;
        return Resolve(providerTypeName);
    }

    /// <summary>
    /// Resolve provider code from a provider type name.
    /// </summary>
    public static string Resolve(string providerTypeName)
    {
        if (string.IsNullOrEmpty(providerTypeName))
        {
            return "EfCore";
        }

        var name = providerTypeName;
        if (name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            return "Pg";
        }
        if (name.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return "SqlServer";
        }
        if (name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return "Sqlite";
        }
        if (name.Contains("MySql", StringComparison.OrdinalIgnoreCase) || name.Contains("Pomelo", StringComparison.OrdinalIgnoreCase))
        {
            return "MySql";
        }
        if (name.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return "InMemory";
        }
        // Default bucket for unknown EF providers
        return "EfCore";
    }
}
