using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NSI.Domains;

namespace NSI.EntityFramework;

/// <summary>
/// Provides Entity Framework Core model builder extensions for applying consistent naming conventions.
/// </summary>
/// <remarks>
/// Current features:
/// <list type="bullet">
///   <item><description>Snake_case transformation for tables, columns, keys, indexes and foreign keys.</description></item>
///   <item><description>Skips strongly typed ID types and respects TPT strategy markers.</description></item>
/// </list>
/// </remarks>
public static partial class ModelBuilderExtension {

  /// <summary>
  /// Applies snake_case naming convention to all entity set artifacts (tables, columns, keys, FKs, indexes).
  /// </summary>
  /// <param name="modelBuilder">The model builder instance.</param>
  /// <returns>The same <see cref="ModelBuilder"/> for chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelBuilder"/> is null.</exception>
  public static ModelBuilder ApplySnakeCaseConvension(this ModelBuilder modelBuilder) {
    ArgumentNullException.ThrowIfNull(modelBuilder);

    foreach (var entity in modelBuilder.Model.GetEntityTypes()) {

      // Ignore entities that implement IStronglyTypedId
      var clrType = entity.ClrType;
      if (typeof(Domains.StrongIdentifier.IStronglyTypedId).IsAssignableFrom(clrType)) {
        continue;
      }

      ApplySnakeCaseToTableName(entity);

      ApplySnakeCaseToColumnNames(entity);

      // Ignore entities that implement ITptStrategy (TPT inheritance strategy)
      if (!typeof(ITptStrategy).IsAssignableFrom(clrType)) {
        ApplySnakeCaseToKeys(entity);
      }

      ApplySnakeCaseToForeignKeys(entity);

      ApplySnakeCaseToIndexNames(entity);
    }

    return modelBuilder;
  }

  private static void ApplySnakeCaseToIndexNames(IMutableEntityType entity) {
    // Indexes
    foreach (var index in entity.GetIndexes()) {
      index.SetDatabaseName(
        ToSnakeCase(BuildDefaultIndexName(index)));
    }
  }

  private static string BuildDefaultIndexName(IMutableIndex index) {
    var header = index.IsUnique ? "U" : "I";
    return $"{header}X_{index.DeclaringEntityType.DisplayName()}_{string.Join("_", index.Properties.Select(p => p.Name))}";
  }

  private static void ApplySnakeCaseToForeignKeys(IMutableEntityType entity) {
    // Foreign keys
    foreach (var foreignKey in entity.GetForeignKeys()) {
      var fkName = foreignKey.GetConstraintName();
      foreignKey.SetConstraintName(
        !string.IsNullOrEmpty(fkName)
          ? ToSnakeCase(fkName)
          : ToSnakeCase(BuildDefaultForeignKeyName(foreignKey)));
    }
  }

  private static string BuildDefaultForeignKeyName(IMutableForeignKey fk) => string.Join("_", fk.Properties.Select(p => p.Name));

  private static void ApplySnakeCaseToKeys(IMutableEntityType entity) {
    // Keys
    foreach (var key in entity.GetKeys()) {
      var keyName = key.GetName();
      if (keyName != null) {
        key.SetName(ToSnakeCase(keyName));
      }
    }
  }

  private static void ApplySnakeCaseToColumnNames(IMutableTypeBase entity) {
    // Properties
    foreach (var property in entity.GetProperties()) {
      var columnName = property.GetColumnName();
      if (columnName != null) {
        property.SetColumnName(ToSnakeCase(columnName));
      }
    }
  }

  private static void ApplySnakeCaseToTableName(IMutableEntityType entity) {
    // Table name
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
