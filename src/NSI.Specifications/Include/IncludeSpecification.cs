using System.Collections.Generic;
using System.Collections.Immutable;

namespace NSI.Specifications.Include;

/// <summary>
/// Include specification containing typed chains and optional string paths.
/// </summary>
/// <typeparam name="T">Root entity type.</typeparam>
public sealed class IncludeSpecification<T>(IEnumerable<IIncludeChain<T>>? chains = null, IEnumerable<string>? stringPaths = null) : IIncludeSpecification<T>
{
    /// <summary>Typed include chains.</summary>
    public IReadOnlyList<IIncludeChain<T>> Chains { get; } = chains?.ToImmutableArray() ?? [];
    /// <summary>String-based include paths.</summary>
    public IReadOnlyList<string> StringPaths { get; } = stringPaths?.ToImmutableArray() ?? [];

    /// <summary>Returns a new spec with an additional chain.</summary>
    public IncludeSpecification<T> Append(IIncludeChain<T> chain) => new([.. Chains, chain], StringPaths);
    /// <summary>Returns a new spec with an additional string path.</summary>
    public IncludeSpecification<T> Append(string path) => new(Chains, [.. StringPaths, path]);
}
