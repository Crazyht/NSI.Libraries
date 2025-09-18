using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NSI.Domains.StrongIdentifier;

namespace NSI.EntityFramework;

/// <summary>
/// Entity Framework Core model builder extensions for applying consistent naming conventions.
/// </summary>
/// <remarks>
/// <para>
/// Provides uniform snake_case naming across relational artifacts (tables, columns, keys, foreign
/// keys, indexes) to improve portability and readability in SQL environments (PostgreSQL / MySQL).
/// Strongly-typed identifier types (<see cref="IStronglyTypedId"/>) are excluded because they are
/// not mapped as entity sets.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Idempotent – safe to invoke multiple times (re-computes same names).</description></item>
///   <item><description>Skips abstract CLR types when generating key names (inheritance roots).</description></item>
///   <item><description>Only transforms artifacts with existing names (null names preserved).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Invoke near the end of <see cref="DbContext.OnModelCreating(ModelBuilder)"/> after custom overrides.</description></item>
///   <item><description>Apply before additional convention-based extensions that depend on final names.</description></item>
///   <item><description>Avoid mixing custom manual snake casing with this automated pass.</description></item>
/// </list>
/// </para>
/// <para>Performance: O(E + P + K + F + I) over model metadata; executed once during model caching and
/// not in query/runtime pipelines.</para>
/// <para>Thread-safety: Not required; EF Core performs model building on a single thread.</para>
/// </remarks>
public static partial class ModelBuilderExtension {
  /// <summary>
  /// NEW preferred API – applies snake_case naming conventions to tables, columns, keys, foreign keys and indexes.
  /// </summary>
  /// <param name="modelBuilder">Target model builder (non-null).</param>
  /// <returns>The same <see cref="ModelBuilder"/> for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">If <paramref name="modelBuilder"/> is null.</exception>
  /// <seealso cref="ApplySnakeCaseConvension(ModelBuilder)"/>
  public static ModelBuilder ApplySnakeCaseConvention(this ModelBuilder modelBuilder) =>
    ApplySnakeCaseInternal(modelBuilder);

  /// <summary>
  /// Legacy method (typo retained). Prefer <see cref="ApplySnakeCaseConvention(ModelBuilder)"/>.
  /// </summary>
  /// <param name="modelBuilder">Target model builder (non-null).</param>
  /// <returns>The same <see cref="ModelBuilder"/> instance.</returns>
  /// <exception cref="ArgumentNullException">If <paramref name="modelBuilder"/> is null.</exception>
#pragma warning disable S1133 // Deprecated code removal reminder
  [Obsolete("Use ApplySnakeCaseConvention(ModelBuilder) – will be removed in a future version.")]
  // Justification: Backward compatibility for existing callers; tracked via issue #NNN for removal.
  public static ModelBuilder ApplySnakeCaseConvension(this ModelBuilder modelBuilder) =>
    ApplySnakeCaseInternal(modelBuilder);
#pragma warning restore S1133

  /// <summary>
  /// Core implementation shared by public APIs.
  /// </summary>
  /// <param name="modelBuilder">Model builder.</param>
  /// <returns>The same <see cref="ModelBuilder"/>.</returns>
  private static ModelBuilder ApplySnakeCaseInternal(ModelBuilder modelBuilder) {
    ArgumentNullException.ThrowIfNull(modelBuilder);

    foreach (var entity in modelBuilder.Model.GetEntityTypes()) {
      var clrType = entity.ClrType;

      // Skip strongly typed id wrappers (not mapped as regular entities)
      if (typeof(IStronglyTypedId).IsAssignableFrom(clrType)) {
        continue;
      }

      ApplySnakeCaseToTableName(entity);
      ApplySnakeCaseToColumnNames(entity);

      if (!clrType.IsAbstract) {
        ApplySnakeCaseToKeys(entity);
      }

      ApplySnakeCaseToForeignKeys(entity);
      ApplySnakeCaseToIndexNames(entity);
    }
    return modelBuilder;
  }

  private static void ApplySnakeCaseToIndexNames(IMutableEntityType entity) {
    foreach (var index in entity.GetIndexes()) {
      index.SetDatabaseName(ToSnakeCase(BuildDefaultIndexName(index)));
    }
  }

  private static string BuildDefaultIndexName(IMutableIndex index) {
    var header = index.IsUnique ? "U" : "I"; // U = Unique, I = Non-unique
    return $"{header}X_{index.DeclaringEntityType.DisplayName()}_{string.Join("_", index.Properties.Select(p => p.Name))}";
  }

  private static void ApplySnakeCaseToForeignKeys(IMutableEntityType entity) {
    foreach (var foreignKey in entity.GetForeignKeys()) {
      var fkName = foreignKey.GetConstraintName();
      foreignKey.SetConstraintName(
        !string.IsNullOrEmpty(fkName)
          ? ToSnakeCase(fkName!)
          : ToSnakeCase(BuildDefaultForeignKeyName(foreignKey)));
    }
  }

  private static string BuildDefaultForeignKeyName(IMutableForeignKey fk) =>
    string.Join("_", fk.Properties.Select(p => p.Name));

  private static void ApplySnakeCaseToKeys(IMutableEntityType entity) {
    foreach (var key in entity.GetKeys()) {
      var keyName = key.GetName();
      if (keyName != null) {
        key.SetName(ToSnakeCase(keyName));
      }
    }
  }

  private static void ApplySnakeCaseToColumnNames(IMutableTypeBase entity) {
    foreach (var property in entity.GetProperties()) {
      var columnName = property.GetColumnName();
      if (columnName != null) {
        property.SetColumnName(ToSnakeCase(columnName));
      }
    }
  }

  private static void ApplySnakeCaseToTableName(IMutableEntityType entity) {
    var tableName = entity.GetTableName();
    if (tableName != null) {
      entity.SetTableName(ToSnakeCase(tableName));
    }
  }

  [SuppressMessage("Minor Code Smell", "S4040:Strings should be normalized to uppercase", Justification = "Snake case requires lowercase")]
  private static string ToSnakeCase(string input) {
    if (string.IsNullOrEmpty(input)) {
      return input;
    }
    var startUnderscores = LeadingUnderscoresRegex().Match(input);
    return startUnderscores + CamelToSnakeRegex().Replace(input, "$1_$2").ToLowerInvariant();
  }

  [GeneratedRegex(@"([a-z0-9])([A-Z])")]
  private static partial Regex CamelToSnakeRegex();

  [GeneratedRegex(@"^_+")]
  private static partial Regex LeadingUnderscoresRegex();
}
