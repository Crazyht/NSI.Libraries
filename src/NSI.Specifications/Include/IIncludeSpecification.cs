using System.Collections.Generic;

namespace NSI.Specifications.Include;

/// <summary>
/// Aggregates include chains to apply on queries.
/// </summary>
/// <typeparam name="T">Root entity type.</typeparam>
public interface IIncludeSpecification<T>
{
    /// <summary>Gets typed include chains.</summary>
    public IReadOnlyList<IIncludeChain<T>> Chains { get; }
    /// <summary>Gets string-based include paths.</summary>
    public IReadOnlyList<string> StringPaths { get; }
}
