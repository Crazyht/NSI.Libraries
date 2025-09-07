using System;
using System.Linq.Expressions;
using NSI.Specifications.Filtering.Text;

namespace NSI.Specifications.Optimization.Npgsql;

/// <summary>
/// Placeholder optimizations for Npgsql provider (PostgreSQL). Currently no-op examples to illustrate registration.
/// </summary>
public static class NpgsqlTextOptimizations
{
    /// <summary>
    /// Registers all known Npgsql-specific optimizations.
    /// </summary>
    public static void RegisterAll()
    {
        // Example: could rewrite case-insensitive contains/starts/ends using EF.Functions.ILike
        SpecOptimizationRegistry.Register("Pg", new ContainsOptimization<object>());
        SpecOptimizationRegistry.Register("Pg", new StartsWithOptimization<object>());
        SpecOptimizationRegistry.Register("Pg", new EndsWithOptimization<object>());
    }

    private sealed class ContainsOptimization<TEntity> : FilterOptimization<TEntity, ContainsSpecification<TEntity>> where TEntity : class
    {
        /// <inheritdoc />
        public override Expression<Func<TEntity, bool>>? TryRewrite(ContainsSpecification<TEntity> specification) => null; // placeholder
    }

    private sealed class StartsWithOptimization<TEntity> : FilterOptimization<TEntity, StartsWithSpecification<TEntity>> where TEntity : class
    {
        /// <inheritdoc />
        public override Expression<Func<TEntity, bool>>? TryRewrite(StartsWithSpecification<TEntity> specification) => null; // placeholder
    }

    private sealed class EndsWithOptimization<TEntity> : FilterOptimization<TEntity, EndsWithSpecification<TEntity>> where TEntity : class
    {
        /// <inheritdoc />
        public override Expression<Func<TEntity, bool>>? TryRewrite(EndsWithSpecification<TEntity> specification) => null; // placeholder
    }
}
