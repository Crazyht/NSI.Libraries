using System.Collections.Generic;
using System.Linq.Expressions;

namespace NSI.Specifications.Include;

/// <summary>
/// Represents a typed include chain, i.e., Include + ThenInclude steps.
/// </summary>
/// <typeparam name="TRoot">Root entity type.</typeparam>
public interface IIncludeChain<TRoot> {
  /// <summary>
  /// Gets the sequence of member access lambda expressions from the root to the terminal navigation.
  /// The first is applied with Include, the rest with ThenInclude.
  /// </summary>
  public IReadOnlyList<LambdaExpression> Steps { get; }

  /// <summary>
  /// Gets the root entity type for this chain (helps analyzers recognize generic usage).
  /// </summary>
  public Type RootType => typeof(TRoot);
}
