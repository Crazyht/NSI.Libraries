namespace NSI.Specifications.Optimization;

/// <summary>
/// Resolves a short, normalized provider code (e.g. Pg, SqlServer, Sqlite, MySql, InMemory, EfCore)
/// from an EF Core <see cref="IQueryable"/> provider implementation type name.
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Inspect the provider type name to derive a concise identifier.</description></item>
///   <item><description>Fallback gracefully to a default bucket ("EfCore").</description></item>
///   <item><description>Detect PostgreSQL even when the base <c>EntityQueryProvider</c> is present but the
///     specific provider assembly is loaded.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use for logging, metrics tagging, or conditional optimizations.</description></item>
///   <item><description>Not intended for security decisions (purely informational).</description></item>
///   <item><description>Provider codes are stable; extend with caution to avoid churn in dashboards.</description></item>
/// </list>
/// </para>
/// <para>Performance: Lightweight string inspections + a one-time assembly scan per call path. For
/// hot loops consider caching the result externally keyed by provider type.</para>
/// <para>Thread-safety: All operations are pure / stateless.</para>
/// </remarks>
/// <example>
/// <code>
/// var code = ProviderNameResolver.Resolve(dbContext.Users); // e.g. "Pg" for Npgsql
/// logger.LogInformation("Executing query on provider {Provider}", code);
/// </code>
/// </example>
public static class ProviderNameResolver {
  /// <summary>
  /// Resolves a provider code from an <see cref="IQueryable"/> source's underlying provider.
  /// </summary>
  /// <typeparam name="T">Element type (ignored).</typeparam>
  /// <param name="source">Queryable source (non-null).</param>
  /// <returns>Normalized provider code.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="source"/> is null.</exception>
  public static string Resolve<T>(IQueryable<T> source) {
    ArgumentNullException.ThrowIfNull(source);
    var providerTypeName = source.Provider?.GetType().FullName ?? string.Empty;
    return Resolve(providerTypeName);
  }

  /// <summary>
  /// Resolves a provider code from a provider type name string.
  /// </summary>
  /// <param name="providerTypeName">Fully qualified provider type name (may be empty).</param>
  /// <returns>Normalized provider code string.</returns>
  public static string Resolve(string providerTypeName) {
    if (string.IsNullOrEmpty(providerTypeName)) {
      return "EfCore";
    }

    var name = providerTypeName;
    if (Matches(name, "Npgsql")) {
      return "Pg";
    }
    if (IsEntityQueryProvider(name) && IsAssemblyLoaded("Npgsql.EntityFrameworkCore.PostgreSQL")) {
      // Base EF provider type but Npgsql assembly present -> treat as PostgreSQL.
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
    return "EfCore"; // Default bucket.
  }

  /// <summary>Case-insensitive substring match.</summary>
  private static bool Matches(string source, string token) =>
    source.Contains(token, StringComparison.OrdinalIgnoreCase);

  /// <summary>Detects EF Core base query provider.</summary>
  private static bool IsEntityQueryProvider(string name) =>
    name.Contains("EntityQueryProvider", StringComparison.Ordinal);

  /// <summary>Determines whether an assembly starting with the specified prefix is loaded.</summary>
  private static bool IsAssemblyLoaded(string prefix) => AppDomain.CurrentDomain
    .GetAssemblies()
    .Any(a => a.GetName().Name?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true);
}
