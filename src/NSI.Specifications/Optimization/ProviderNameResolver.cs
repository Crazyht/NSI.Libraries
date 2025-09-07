using System;
using System.Linq;

namespace NSI.Specifications.Optimization;

/// <summary>
/// Resolves a short provider code from an IQueryable's provider type name.
/// </summary>
public static class ProviderNameResolver {
  /// <summary>
  /// Resolve provider code from an <see cref="IQueryable"/> instance.
  /// </summary>
  public static string Resolve<T>(IQueryable<T> source) {
    ArgumentNullException.ThrowIfNull(source);
    var providerTypeName = source.Provider?.GetType().FullName ?? string.Empty;
    return Resolve(providerTypeName);
  }

  /// <summary>
  /// Resolve provider code from a provider type name.
  /// </summary>
  public static string Resolve(string providerTypeName) {
    if (string.IsNullOrEmpty(providerTypeName)) {
      return "EfCore";
    }
    var name = providerTypeName;
    if (Matches(name, "Npgsql")) {
      return "Pg";
    }
    if (IsEntityQueryProvider(name) && IsAssemblyLoaded("Npgsql.EntityFrameworkCore.PostgreSQL")) {
      return "Pg";
    }
    if (Matches(name, "SqlServer")) {
      return "SqlServer";
    }
    if (Matches(name, "Sqlite")) {
      return "Sqlite";
    }
    if (Matches(name, "MySql") || Matches(name, "Pomelo")) {
      return "MySql";
    }
    if (Matches(name, "InMemory")) {
      return "InMemory";
    }
    return "EfCore"; // default bucket
  }

  private static bool Matches(string source, string token) => source.Contains(token, StringComparison.OrdinalIgnoreCase);
  private static bool IsEntityQueryProvider(string name) => name.Contains("EntityQueryProvider", StringComparison.Ordinal);
  private static bool IsAssemblyLoaded(string prefix) => AppDomain.CurrentDomain.GetAssemblies()
      .Any(a => a.GetName().Name?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true);
}
